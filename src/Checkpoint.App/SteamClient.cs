// Cliente del servicio Steam: inicio OpenID, consulta de biblioteca y logros.
// La sesión se protege con Windows y las direcciones del servicio se validan antes de usarlas.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Checkpoint.Core;

namespace Checkpoint.App;

public sealed record LoginStart(string FlowId, string PollSecret, string AuthorizeUrl);

public sealed record LoginResult(string Status, string? Token, string? SteamId);

public sealed record LibraryResult(SteamGame[] Games);

public sealed record AchievementsResult(Achievement[] Achievements);

public sealed record SavedSession(string ServiceUrl, string Token, string SteamId);

public sealed class SteamClient : IDisposable
{
    private readonly HttpClient http;
    private readonly string tokenFile;
    public SavedSession? Session { get; private set; }

    public SteamClient(string directory, HttpMessageHandler? handler = null)
    {
        http = new HttpClient(handler ?? new HttpClientHandler { AllowAutoRedirect = false })
        {
            Timeout = TimeSpan.FromSeconds(35),
            MaxResponseContentBufferSize = 12_000_000,
        };
        tokenFile = Path.Combine(directory, "steam-session.dat");
        try
        {
            if (File.Exists(tokenFile))
                Session = JsonSerializer.Deserialize<SavedSession>(
                    Encoding.UTF8.GetString(
                        ProtectedData.Unprotect(
                            File.ReadAllBytes(tokenFile),
                            null,
                            DataProtectionScope.CurrentUser
                        )
                    ),
                    DataJson.Options
                );
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or IOException)
        {
            Session = null;
        }
    }

    // Acepta únicamente el origen HTTPS o el endpoint Supabase soportado, sin credenciales ni parámetros en la URL.
    public static Uri ValidateServiceUrl(string address)
    {
        if (
            !Uri.TryCreate(address.Trim().TrimEnd('/') + "/", UriKind.Absolute, out var uri)
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment)
            || (uri.Scheme != "https" && !(uri.Scheme == "http" && uri.IsLoopback))
        )
            throw new ArgumentException(
                I18n.T(
                    "Usa la dirección HTTPS del servicio. HTTP solo se permite para un servidor local."
                )
            );
        if (
            uri.AbsolutePath != "/"
            && !(
                uri.Scheme == "https"
                && uri.Host.EndsWith(".supabase.co", StringComparison.OrdinalIgnoreCase)
                && uri.AbsolutePath == "/functions/v1/checkpoint-steam/"
            )
        )
            throw new ArgumentException(
                I18n.T("La dirección del servicio debe ser su origen, sin rutas adicionales.")
            );
        return uri;
    }

    private async Task<T> Request<T>(
        string service,
        string path,
        object? body = null,
        bool authenticated = true,
        CancellationToken cancellation = default
    )
    {
        using var request = new HttpRequestMessage(
            body is null ? HttpMethod.Get : HttpMethod.Post,
            new Uri(ValidateServiceUrl(service), path)
        );
        if (authenticated)
        {
            if (
                Session is null
                || !string.Equals(
                    Session.ServiceUrl,
                    ValidateServiceUrl(service).AbsoluteUri,
                    StringComparison.OrdinalIgnoreCase
                )
            )
                throw new InvalidOperationException(
                    I18n.T("Vincula Steam con este servicio antes de sincronizar.")
                );
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);
        }
        if (body is not null)
            request.Content = JsonContent.Create(body);
        using var response = await http.SendAsync(request, cancellation);
        if (!response.IsSuccessStatusCode)
        {
            string message = response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => I18n.T(
                    "La sesión ha caducado. Vuelve a vincular Steam."
                ),
                HttpStatusCode.TooManyRequests => I18n.T(
                    "Demasiadas consultas. Espera un minuto y vuelve a intentarlo."
                ),
                HttpStatusCode.ServiceUnavailable => I18n.T(
                    "El servicio de Steam todavía no está configurado o no está disponible."
                ),
                _ => I18n.T("No se pudo consultar Steam. Se conserva el último progreso guardado."),
            };
            try
            {
                using var error = JsonDocument.Parse(
                    await response.Content.ReadAsStringAsync(cancellation)
                );
                if (
                    error.RootElement.TryGetProperty("error", out var field)
                    && field.GetString() is { Length: > 0 and < 300 } detail
                )
                {
                    if (I18n.TryTranslateKnown(detail, out var localized))
                        message = localized;
                }
            }
            catch (JsonException) { }
            throw new InvalidOperationException(message);
        }
        return await response.Content.ReadFromJsonAsync<T>(DataJson.Options, cancellation)
            ?? throw new InvalidDataException(I18n.T("Respuesta vacía del servicio."));
    }

    public Task<LoginStart> BeginLogin(string service, CancellationToken cancellation) =>
        Request<LoginStart>(service, "v1/auth/start", new { }, false, cancellation);

    public Task<LoginResult> Poll(
        string service,
        LoginStart flow,
        CancellationToken cancellation
    ) =>
        Request<LoginResult>(
            service,
            "v1/auth/poll",
            new { flow.FlowId, flow.PollSecret },
            false,
            cancellation
        );

    // Protege la sesión con DPAPI para que solo el mismo usuario de Windows pueda recuperarla.
    public void SaveSession(string service, LoginResult result)
    {
        if (
            result.Status != "complete"
            || result.Token is null
            || result.SteamId is null
            || !System.Text.RegularExpressions.Regex.IsMatch(result.Token, "^[A-Za-z0-9_-]{43}$")
            || !System.Text.RegularExpressions.Regex.IsMatch(result.SteamId, "^7656119[0-9]{10}$")
        )
            throw new InvalidDataException(I18n.T("La sesión recibida no es válida."));
        var session = new SavedSession(
            ValidateServiceUrl(service).AbsoluteUri,
            result.Token,
            result.SteamId
        );
        var protectedBytes = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(session, DataJson.Options)),
            null,
            DataProtectionScope.CurrentUser
        );
        var temporary = tokenFile + ".tmp";
        try
        {
            File.WriteAllBytes(temporary, protectedBytes);
            File.Move(temporary, tokenFile, true);
            Session = session;
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }

    public Task<LibraryResult> Library(string service, CancellationToken cancellation) =>
        Request<LibraryResult>(service, "v1/library", cancellation: cancellation);

    public Task<AchievementsResult> Achievements(
        string service,
        int appId,
        CancellationToken cancellation
    ) =>
        Request<AchievementsResult>(
            service,
            $"v1/games/{appId}/achievements?lang={(I18n.IsEnglish ? "en" : "es")}",
            cancellation: cancellation
        );

    public async Task Disconnect()
    {
        try
        {
            if (Session is not null)
                await Request<JsonElement>(Session.ServiceUrl, "v1/auth/logout", new { });
        }
        catch (Exception ex)
            when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException)
        { }
        Session = null;
        if (File.Exists(tokenFile))
            File.Delete(tokenFile);
    }

    public void Dispose() => http.Dispose();
}
