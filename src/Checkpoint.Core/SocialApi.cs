// Cliente de Supabase para autenticación, amistad y publicación de progreso.
// Centraliza solicitudes, renovación de sesión y conversión de errores remotos.

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

    public SocialApi(
        SocialProject project,
        HttpMessageHandler? handler = null,
        SocialSession? session = null
    )
    {
        this.project = project;
        origin = project.Validate();
        http = new(handler ?? new HttpClientHandler { AllowAutoRedirect = false })
        {
            Timeout = TimeSpan.FromSeconds(25),
            MaxResponseContentBufferSize = 4_000_000,
        };
        if (session?.ProjectUrl == origin.AbsoluteUri)
            Session = session;
    }

    private sealed record AuthUser(Guid Id);

    private sealed record AuthResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        AuthUser? User
    );

    private void Accept(AuthResponse response)
    {
        if (
            response.User?.Id is not Guid id
            || id == Guid.Empty
            || string.IsNullOrEmpty(response.AccessToken)
            || string.IsNullOrEmpty(response.RefreshToken)
            || response.ExpiresIn <= 0
        )
            throw new InvalidDataException(
                I18n.T("La respuesta de inicio de sesión no es válida.")
            );
        var candidate = new SocialSession(
            origin.AbsoluteUri,
            id,
            response.AccessToken,
            response.RefreshToken,
            DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn)
        );
        SessionChanged?.Invoke(candidate);
        Session = candidate;
    }

    private async Task RefreshSession(CancellationToken ct)
    {
        await sessionGate.WaitAsync(ct);
        try
        {
            if (Session is null)
                throw new SocialApiException(I18n.T("Entra en tu cuenta de Checkpoint."));
            if (Session.ExpiresAt > DateTimeOffset.UtcNow.AddSeconds(60))
                return;
            var refreshed = await Send<AuthResponse>(
                HttpMethod.Post,
                "auth/v1/token?grant_type=refresh_token",
                new { refresh_token = Session.RefreshToken },
                false,
                ct
            );
            if (refreshed.User?.Id != Session.UserId)
                throw new InvalidDataException(I18n.T("La cuenta de la sesión cambió."));
            Accept(refreshed);
        }
        finally
        {
            sessionGate.Release();
        }
    }

    private async Task<T> Send<T>(
        HttpMethod method,
        string path,
        object? body,
        bool auth,
        CancellationToken ct
    )
    {
        if (auth)
            await RefreshSession(ct);
        using var request = new HttpRequestMessage(method, new Uri(origin, path));
        request.Headers.Add("apikey", project.PublishableKey);
        if (auth)
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                Session!.AccessToken
            );
        if (body is not null)
            request.Content = JsonContent.Create(body, options: Json);
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            string? code = null;
            try
            {
                using var error = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                if (error.RootElement.TryGetProperty("code", out var c))
                    code = c.GetString();
                if (code is null && error.RootElement.TryGetProperty("error_code", out c))
                    code = c.GetString();
            }
            catch (JsonException) { }
            string message = code switch
            {
                "40001" => I18n.T(
                    "El progreso cambió en otro equipo. Revisa el conflicto antes de publicar."
                ),
                "email_not_confirmed" => I18n.T("Confirma tu correo antes de entrar."),
                "invalid_credentials" => I18n.T("El usuario o la contraseña no son correctos."),
                "user_already_exists" or "email_exists" => I18n.T(
                    "Ese nombre de usuario ya está en uso."
                ),
                "weak_password" => I18n.T("La contraseña no cumple los requisitos del servicio."),
                "over_email_send_rate_limit" => I18n.T(
                    "Se ha alcanzado el límite de correo. Espera antes de intentarlo."
                ),
                "P0001" => I18n.T(
                    "La solicitud no está disponible o se ha alcanzado el límite. Actualiza e inténtalo más tarde."
                ),
                _ => response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => I18n.T(
                        "La sesión no es válida. Vuelve a entrar en Checkpoint."
                    ),
                    HttpStatusCode.Forbidden => I18n.T(
                        "No tienes permiso para consultar o cambiar estos datos."
                    ),
                    HttpStatusCode.TooManyRequests => I18n.T(
                        "Demasiadas consultas. Espera antes de volver a intentarlo."
                    ),
                    _ => I18n.T(
                        "No se pudo completar la operación. Los cambios pendientes se conservan en este PC."
                    ),
                },
            };
            throw new SocialApiException(message, code);
        }
        if (typeof(T) == typeof(bool))
            return (T)(object)true;
        return await response.Content.ReadFromJsonAsync<T>(Json, ct)
            ?? throw new InvalidDataException(I18n.T("Respuesta vacía de Checkpoint."));
    }

    public static string AccountAddress(string username)
    {
        string normalized = username.Trim().ToLowerInvariant();
        if (!System.Text.RegularExpressions.Regex.IsMatch(normalized, "^[a-z0-9_]{3,24}$"))
            throw new ArgumentException(
                I18n.T(
                    "El usuario debe tener de 3 a 24 letras, números o guiones bajos, sin espacios."
                )
            );
        return normalized + "@accounts.checkpoint.invalid";
    }

    public async Task Login(string username, string password, CancellationToken ct = default) =>
        Accept(
            await Send<AuthResponse>(
                HttpMethod.Post,
                "auth/v1/token?grant_type=password",
                new { email = AccountAddress(username), password },
                false,
                ct
            )
        );

    public async Task<bool> Register(
        string username,
        string password,
        string displayName,
        CancellationToken ct = default
    )
    {
        if (displayName.Trim().Length is < 1 or > 50 || password.Length < 8)
            throw new ArgumentException(
                I18n.T(
                    "Usa un nombre de 1 a 50 caracteres y una contraseña de al menos 8 caracteres."
                )
            );
        var response = await Send<AuthResponse>(
            HttpMethod.Post,
            "auth/v1/signup",
            new
            {
                email = AccountAddress(username),
                password,
                data = new { display_name = displayName.Trim() },
            },
            false,
            ct
        );
        if (response.AccessToken is null)
            return false;
        Accept(response);
        return true;
    }

    public async Task Logout(CancellationToken ct = default)
    {
        try
        {
            if (Session is not null)
                await Send<bool>(HttpMethod.Post, "auth/v1/logout?scope=local", new { }, true, ct);
        }
        finally
        {
            Session = null;
            SessionChanged?.Invoke(null);
        }
    }

    public Task<bool> ChangePassword(string password, CancellationToken ct = default)
    {
        if (password.Length < 8)
            throw new ArgumentException(I18n.T("La contraseña debe tener al menos 8 caracteres."));
        return Send<bool>(HttpMethod.Put, "auth/v1/user", new { password }, true, ct);
    }

    public Task<bool> UpdateName(string name, CancellationToken ct = default)
    {
        if (name.Trim().Length is < 1 or > 50 || name.Any(char.IsControl))
            throw new ArgumentException(I18n.T("Usa un nombre de 1 a 50 caracteres."));
        return Send<bool>(
            HttpMethod.Patch,
            $"rest/v1/cp_profiles?user_id=eq.{Session?.UserId}",
            new { display_name = name.Trim() },
            true,
            ct
        );
    }

    public Task<SocialProfile[]> Profiles(CancellationToken ct = default) =>
        Send<SocialProfile[]>(
            HttpMethod.Get,
            "rest/v1/cp_profiles?select=user_id,display_name,friend_code&limit=500",
            null,
            true,
            ct
        );

    public Task<SocialFriendship[]> Friendships(CancellationToken ct = default) =>
        Send<SocialFriendship[]>(
            HttpMethod.Get,
            "rest/v1/cp_friendships?select=user_low,user_high&limit=500",
            null,
            true,
            ct
        );

    public Task<SocialRequest[]> Requests(CancellationToken ct = default) =>
        Send<SocialRequest[]>(
            HttpMethod.Get,
            "rest/v1/cp_friend_requests?select=id,sender_id,recipient_id,status&status=eq.pending&limit=100",
            null,
            true,
            ct
        );

    public Task<SocialProfile[]> Find(string code, CancellationToken ct = default)
    {
        return Send<SocialProfile[]>(
            HttpMethod.Post,
            "rest/v1/rpc/cp_find_friend",
            new { p_code = FriendCodes.ForService(code) },
            true,
            ct
        );
    }

    public Task<bool> Invite(Guid recipient, CancellationToken ct = default) =>
        Send<bool>(
            HttpMethod.Post,
            "rest/v1/cp_friend_requests",
            new
            {
                sender_id = Session?.UserId
                    ?? throw new SocialApiException(I18n.T("Entra en Checkpoint.")),
                recipient_id = recipient,
            },
            true,
            ct
        );

    public Task<bool> Answer(Guid request, bool accept, CancellationToken ct = default) =>
        Send<bool>(
            HttpMethod.Patch,
            $"rest/v1/cp_friend_requests?id=eq.{request}",
            new { status = accept ? "accepted" : "rejected" },
            true,
            ct
        );

    public Task<bool> Cancel(Guid request, CancellationToken ct = default) =>
        Send<bool>(
            HttpMethod.Delete,
            $"rest/v1/cp_friend_requests?id=eq.{request}",
            null,
            true,
            ct
        );

    public Task<bool> Unfriend(Guid other, CancellationToken ct = default)
    {
        var own = Session?.UserId ?? throw new SocialApiException(I18n.T("Entra en Checkpoint."));
        // UUID textual order matches PostgreSQL's byte order.
        var low = string.CompareOrdinal(own.ToString(), other.ToString()) < 0 ? own : other;
        var high = low == own ? other : own;
        return Send<bool>(
            HttpMethod.Delete,
            $"rest/v1/cp_friendships?user_low=eq.{low}&user_high=eq.{high}",
            null,
            true,
            ct
        );
    }

    public Task<bool> Block(Guid other, CancellationToken ct = default) =>
        Send<bool>(
            HttpMethod.Post,
            "rest/v1/cp_blocks",
            new
            {
                blocker_id = Session?.UserId
                    ?? throw new SocialApiException(I18n.T("Entra en Checkpoint.")),
                blocked_id = other,
            },
            true,
            ct
        );

    public Task<bool> Unblock(Guid other, CancellationToken ct = default) =>
        Send<bool>(HttpMethod.Delete, $"rest/v1/cp_blocks?blocked_id=eq.{other}", null, true, ct);

    public Task<JsonElement[]> Blocks(CancellationToken ct = default) =>
        Send<JsonElement[]>(
            HttpMethod.Get,
            "rest/v1/cp_blocks?select=blocked_id&limit=500",
            null,
            true,
            ct
        );

    public Task<SocialPublication[]> Publications(Guid owner, CancellationToken ct = default) =>
        Send<SocialPublication[]>(
            HttpMethod.Get,
            $"rest/v1/cp_game_publications?owner_id=eq.{owner}&select=owner_id,game_id,revision,operation_id,is_shared,operation_payload,updated_at&limit=10000",
            null,
            true,
            ct
        );

    public Task<long> Publish(Guid id, ShareOperation operation, CancellationToken ct = default) =>
        Send<long>(
            HttpMethod.Post,
            "rest/v1/rpc/cp_publish_game",
            new
            {
                p_game_id = id,
                p_expected_revision = operation.ExpectedRevision,
                p_operation_id = operation.Id,
                p_game = operation.Payload is { } payload
                    ? payload with
                    {
                        CoverPath = null,
                    }
                    : null,
            },
            true,
            ct
        );

    public Task<SocialGroup[]> Groups(CancellationToken ct = default) =>
        Send<SocialGroup[]>(
            HttpMethod.Get,
            "rest/v1/cp_groups?select=id,name,owner_id,game_title,steam_app_id,goals&order=created_at.desc&limit=100",
            null,
            true,
            ct
        );

    public Task<SocialGroupMember[]> GroupMembers(CancellationToken ct = default) =>
        Send<SocialGroupMember[]>(
            HttpMethod.Get,
            "rest/v1/cp_group_members?select=group_id,user_id&limit=1000",
            null,
            true,
            ct
        );

    public Task<SocialGroupInvite[]> GroupInvites(CancellationToken ct = default) =>
        Send<SocialGroupInvite[]>(
            HttpMethod.Get,
            $"rest/v1/cp_group_invites?select=id,group_id,inviter_id,invitee_id,status&invitee_id=eq.{Session?.UserId}&status=eq.pending&limit=100",
            null,
            true,
            ct
        );

    public Task<Guid> CreateGroup(
        string name,
        Game game,
        SharedGroupGoal[] goals,
        CancellationToken ct = default
    ) =>
        Send<Guid>(
            HttpMethod.Post,
            "rest/v1/rpc/cp_create_group",
            new
            {
                p_name = name,
                p_game_title = game.Title,
                p_steam_app_id = game.SteamAppId,
                p_goals = goals,
            },
            true,
            ct
        );

    public Task<Guid> InviteGroupFriend(Guid group, Guid friend, CancellationToken ct = default) =>
        Send<Guid>(
            HttpMethod.Post,
            "rest/v1/rpc/cp_invite_group_friend",
            new { p_group_id = group, p_friend_id = friend },
            true,
            ct
        );

    public Task<bool> AnswerGroupInvite(Guid invite, bool accept, CancellationToken ct = default) =>
        Send<bool>(
            HttpMethod.Post,
            "rest/v1/rpc/cp_answer_group_invite",
            new { p_invite_id = invite, p_accept = accept },
            true,
            ct
        );

    public Task<bool> UpdateGroupGoal(
        Guid group,
        string goal,
        bool completed,
        CancellationToken ct = default
    ) =>
        Send<bool>(
            HttpMethod.Post,
            "rest/v1/rpc/cp_update_group_goal",
            new
            {
                p_group_id = group,
                p_goal_id = goal,
                p_completed = completed,
            },
            true,
            ct
        );

    public Task<bool> AddGroupGoal(
        Guid group,
        string id,
        string name,
        string description,
        CancellationToken ct = default
    ) =>
        Send<bool>(
            HttpMethod.Post,
            "rest/v1/rpc/cp_add_group_goal",
            new
            {
                p_group_id = group,
                p_goal_id = id,
                p_name = name,
                p_description = description,
            },
            true,
            ct
        );

    public Task<bool> RemoveGroupGoal(Guid group, string goal, CancellationToken ct = default) =>
        Send<bool>(
            HttpMethod.Post,
            "rest/v1/rpc/cp_remove_group_goal",
            new { p_group_id = group, p_goal_id = goal },
            true,
            ct
        );

    public void Dispose()
    {
        http.Dispose();
        sessionGate.Dispose();
    }
}
