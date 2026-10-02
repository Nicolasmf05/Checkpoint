using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Checkpoint.Core;

public sealed class SocialApi : IDisposable
{
    private readonly HttpClient http;
    private readonly SocialProject project;
    private readonly Uri origin;
    private readonly SemaphoreSlim sessionGate = new(1);
    public SocialSession? Session { get; private set; }
    public event Action<SocialSession?>? SessionChanged;
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public SocialApi(SocialProject project, HttpMessageHandler? handler = null, SocialSession? session = null)
    {
        this.project = project; origin = project.Validate();
        http = new(handler ?? new HttpClientHandler { AllowAutoRedirect = false })
            { Timeout = TimeSpan.FromSeconds(25), MaxResponseContentBufferSize = 4_000_000 };
        if (session?.ProjectUrl == origin.AbsoluteUri) Session = session;
    }
    private sealed record AuthUser(Guid Id);
    private sealed record AuthResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn, AuthUser? User);
    private void Accept(AuthResponse response)
    {
        if (response.User?.Id is not Guid id || id == Guid.Empty || string.IsNullOrEmpty(response.AccessToken)
            || string.IsNullOrEmpty(response.RefreshToken) || response.ExpiresIn <= 0)
            throw new InvalidDataException("La respuesta de inicio de sesión no es válida.");
        var candidate = new SocialSession(origin.AbsoluteUri, id, response.AccessToken, response.RefreshToken,
            DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn));
        SessionChanged?.Invoke(candidate); Session = candidate;
    }
    private async Task RefreshSession(CancellationToken ct)
    {
        await sessionGate.WaitAsync(ct);
        try
        {
            if (Session is null) throw new SocialApiException("Entra en tu cuenta de Checkpoint.");
            if (Session.ExpiresAt > DateTimeOffset.UtcNow.AddSeconds(60)) return;
            var refreshed = await Send<AuthResponse>(HttpMethod.Post,"auth/v1/token?grant_type=refresh_token",
                new { refresh_token = Session.RefreshToken }, false, ct);
            if (refreshed.User?.Id != Session.UserId) throw new InvalidDataException("La cuenta de la sesión cambió.");
            Accept(refreshed);
        }
        finally { sessionGate.Release(); }
    }
    private async Task<T> Send<T>(HttpMethod method, string path, object? body, bool auth, CancellationToken ct,
        byte[]? bytes = null)
    {
        if (auth) await RefreshSession(ct);
        using var request = new HttpRequestMessage(method, new Uri(origin, path));
        request.Headers.Add("apikey", project.PublishableKey);
        if (auth) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session!.AccessToken);
        if (bytes is not null)
        {
            request.Content = new ByteArrayContent(bytes); request.Content.Headers.ContentType = new("image/png");
            request.Headers.Add("x-upsert", "true");
        }
        else if (body is not null) request.Content = JsonContent.Create(body, options: Json);
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            string? code = null;
            try
            {
                using var error = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                if (error.RootElement.TryGetProperty("code", out var c)) code = c.GetString();
                if (code is null && error.RootElement.TryGetProperty("error_code", out c)) code = c.GetString();
            }
            catch (JsonException) { }
            string message = code switch {
                "40001" => "El progreso cambió en otro equipo. Revisa el conflicto antes de publicar.",
                "email_not_confirmed" => "Confirma tu correo antes de entrar.",
                "invalid_credentials" => "El usuario o la contraseña no son correctos.",
                "user_already_exists" or "email_exists" => "Ese nombre de usuario ya está en uso.",
                "weak_password" => "La contraseña no cumple los requisitos del servicio.",
                "over_email_send_rate_limit" => "Se ha alcanzado el límite de correo. Espera antes de intentarlo.",
                "P0001" => "La solicitud no está disponible o se ha alcanzado el límite. Actualiza e inténtalo más tarde.",
                _ => response.StatusCode switch {
                    HttpStatusCode.Unauthorized => "La sesión no es válida. Vuelve a entrar en Checkpoint.",
                    HttpStatusCode.Forbidden => "No tienes permiso para consultar o cambiar estos datos.",
                    HttpStatusCode.TooManyRequests => "Demasiadas consultas. Espera antes de volver a intentarlo.",
                    _ => "No se pudo completar la operación. Los cambios pendientes se conservan en este PC."
                }
            };
            throw new SocialApiException(message, code);
        }
        if (typeof(T) == typeof(bool)) return (T)(object)true;
        return await response.Content.ReadFromJsonAsync<T>(Json, ct) ?? throw new InvalidDataException("Respuesta vacía de Checkpoint.");
    }
    public static string AccountAddress(string username)
    {
        string normalized = username.Trim().ToLowerInvariant();
        if (!System.Text.RegularExpressions.Regex.IsMatch(normalized,"^[a-z0-9_]{3,24}$"))
            throw new ArgumentException("El usuario debe tener de 3 a 24 letras, números o guiones bajos, sin espacios.");
        return normalized + "@accounts.checkpoint.invalid";
    }
    public async Task Login(string username, string password, CancellationToken ct = default) => Accept(
        await Send<AuthResponse>(HttpMethod.Post,"auth/v1/token?grant_type=password",new { email = AccountAddress(username), password },false,ct));
    public async Task<bool> Register(string username, string password, string displayName, CancellationToken ct = default)
    {
        if (displayName.Trim().Length is < 1 or > 50 || password.Length < 8) throw new ArgumentException("Usa un nombre de 1 a 50 caracteres y una contraseña de al menos 8 caracteres.");
        var response = await Send<AuthResponse>(HttpMethod.Post,"auth/v1/signup",new { email = AccountAddress(username), password,
            data = new { display_name = displayName.Trim() } },false,ct);
        if (response.AccessToken is null) return false;
        Accept(response); return true;
    }
    public async Task Logout(CancellationToken ct = default)
    {
        try { if (Session is not null) await Send<bool>(HttpMethod.Post,"auth/v1/logout?scope=local",new { },true,ct); }
        finally { Session = null; SessionChanged?.Invoke(null); }
    }
    public Task<bool> ChangePassword(string password, CancellationToken ct = default)
    {
        if (password.Length < 8) throw new ArgumentException("La contraseña debe tener al menos 8 caracteres.");
        return Send<bool>(HttpMethod.Put,"auth/v1/user",new { password },true,ct);
    }
    public Task<bool> UpdateName(string name, CancellationToken ct = default)
    {
        if (name.Trim().Length is < 1 or > 50 || name.Any(char.IsControl)) throw new ArgumentException("Usa un nombre de 1 a 50 caracteres.");
        return Send<bool>(HttpMethod.Patch,$"rest/v1/cp_profiles?user_id=eq.{Session?.UserId}",new { display_name = name.Trim() },true,ct);
    }
    public Task<SocialProfile[]> Profiles(CancellationToken ct = default) => Send<SocialProfile[]>(HttpMethod.Get,
        "rest/v1/cp_profiles?select=user_id,display_name,friend_code&limit=500",null,true,ct);
    public Task<SocialFriendship[]> Friendships(CancellationToken ct = default) => Send<SocialFriendship[]>(HttpMethod.Get,
        "rest/v1/cp_friendships?select=user_low,user_high&limit=500",null,true,ct);
    public Task<SocialRequest[]> Requests(CancellationToken ct = default) => Send<SocialRequest[]>(HttpMethod.Get,
        "rest/v1/cp_friend_requests?select=id,sender_id,recipient_id,status&status=eq.pending&limit=100",null,true,ct);
    public Task<SocialProfile[]> Find(string code, CancellationToken ct = default)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(code.Trim(), "^cp-[a-fA-F0-9]{12}$")) throw new ArgumentException("El código tiene el formato cp- seguido de 12 caracteres.");
        return Send<SocialProfile[]>(HttpMethod.Post,"rest/v1/rpc/cp_find_friend",new { p_code = code.Trim().ToLowerInvariant() },true,ct);
    }
    public Task<bool> Invite(Guid recipient, CancellationToken ct = default) => Send<bool>(HttpMethod.Post,"rest/v1/cp_friend_requests",
        new { sender_id = Session?.UserId ?? throw new SocialApiException("Entra en Checkpoint."), recipient_id = recipient },true,ct);
    public Task<bool> Answer(Guid request, bool accept, CancellationToken ct = default) => Send<bool>(HttpMethod.Patch,
        $"rest/v1/cp_friend_requests?id=eq.{request}",new { status = accept ? "accepted" : "rejected" },true,ct);
    public Task<bool> Cancel(Guid request, CancellationToken ct = default) => Send<bool>(HttpMethod.Delete,
        $"rest/v1/cp_friend_requests?id=eq.{request}",null,true,ct);
    public Task<bool> Unfriend(Guid other, CancellationToken ct = default)
    {
        var own = Session?.UserId ?? throw new SocialApiException("Entra en Checkpoint.");
        // UUID textual order matches PostgreSQL's byte order.
        var low = string.CompareOrdinal(own.ToString(),other.ToString()) < 0 ? own : other;
        var high = low == own ? other : own;
        return Send<bool>(HttpMethod.Delete,$"rest/v1/cp_friendships?user_low=eq.{low}&user_high=eq.{high}",null,true,ct);
    }
    public Task<bool> Block(Guid other, CancellationToken ct = default) => Send<bool>(HttpMethod.Post,"rest/v1/cp_blocks",
        new { blocker_id = Session?.UserId ?? throw new SocialApiException("Entra en Checkpoint."), blocked_id = other },true,ct);
    public Task<bool> Unblock(Guid other, CancellationToken ct = default) => Send<bool>(HttpMethod.Delete,
        $"rest/v1/cp_blocks?blocked_id=eq.{other}",null,true,ct);
    public Task<JsonElement[]> Blocks(CancellationToken ct = default) => Send<JsonElement[]>(HttpMethod.Get,"rest/v1/cp_blocks?select=blocked_id&limit=500",null,true,ct);
    public Task<SocialPublication[]> Publications(Guid owner, CancellationToken ct = default) => Send<SocialPublication[]>(HttpMethod.Get,
        $"rest/v1/cp_game_publications?owner_id=eq.{owner}&select=owner_id,game_id,revision,operation_id,is_shared,operation_payload,updated_at&limit=10000",null,true,ct);
    public Task<long> Publish(Guid id, ShareOperation operation, CancellationToken ct = default) => Send<long>(HttpMethod.Post,"rest/v1/rpc/cp_publish_game",
        new { p_game_id = id, p_expected_revision = operation.ExpectedRevision, p_operation_id = operation.Id, p_game = operation.Payload },true,ct);
    public Task<bool> UploadCover(string path, byte[] png, CancellationToken ct = default)
    {
        ValidateCoverPath(path, Session?.UserId ?? Guid.Empty);
        if (png.Length > 2097152) throw new ArgumentException("La carátula supera 2 MiB.");
        return Send<bool>(HttpMethod.Post,"storage/v1/object/checkpoint-assets/" + path,null,true,ct,png);
    }
    public async Task<byte[]> DownloadCover(string path, Guid owner, CancellationToken ct = default)
    {
        ValidateCoverPath(path, owner); await RefreshSession(ct);
        using var request = new HttpRequestMessage(HttpMethod.Get,new Uri(origin,"storage/v1/object/authenticated/checkpoint-assets/" + path));
        request.Headers.Add("apikey",project.PublishableKey); request.Headers.Authorization = new("Bearer",Session!.AccessToken);
        using var response = await http.SendAsync(request,ct);
        if (!response.IsSuccessStatusCode) throw new SocialApiException("La carátula ya no está disponible.");
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        if (bytes.Length > 2097152) throw new InvalidDataException("Carátula demasiado grande.");
        return bytes;
    }
    private static void ValidateCoverPath(string path, Guid owner)
    {
        if (owner == Guid.Empty || !System.Text.RegularExpressions.Regex.IsMatch(path,"^" + owner + "/covers/[a-zA-Z0-9_-]+\\.(png|jpg|jpeg|webp)$"))
            throw new ArgumentException("La referencia de carátula no es válida.");
    }
    public void Dispose() { http.Dispose(); sessionGate.Dispose(); }
}
