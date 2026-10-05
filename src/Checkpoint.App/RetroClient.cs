// Cliente de RetroAchievements con sesión local protegida y validación de respuestas.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Checkpoint.Core;

namespace Checkpoint.App;

public sealed record RetroSession(string Username, string ApiKey, bool Hardcore);

public sealed class RetroClient : IDisposable
{
    private readonly HttpClient http;
    private readonly AchievementRequests<List<Achievement>> achievementRequests = new();
    private readonly string file;
    public RetroSession? Session { get; private set; }

    public RetroClient(string directory, HttpMessageHandler? handler = null)
    {
        file = Path.Combine(directory, "retro-session.dat");
        http = new(handler ?? new HttpClientHandler { AllowAutoRedirect = false })
        {
            Timeout = TimeSpan.FromSeconds(25),
            MaxResponseContentBufferSize = 12_000_000,
        };
        try
        {
            if (File.Exists(file))
                Session = JsonSerializer.Deserialize<RetroSession>(
                    ProtectedData.Unprotect(
                        File.ReadAllBytes(file),
                        null,
                        DataProtectionScope.CurrentUser
                    ),
                    DataJson.Options
                );
        }
        catch (Exception ex) when (ex is IOException or CryptographicException or JsonException)
        {
            Session = null;
        }
    }

    public void Save(string username, string key, bool hardcore)
    {
        if (
            string.IsNullOrWhiteSpace(username)
            || username.Length > 80
            || string.IsNullOrWhiteSpace(key)
            || key.Length > 256
        )
            throw new ArgumentException(
                I18n.T("Introduce tu usuario y clave web de RetroAchievements.")
            );
        var session = new RetroSession(username.Trim(), key.Trim(), hardcore);
        var bytes = ProtectedData.Protect(
            JsonSerializer.SerializeToUtf8Bytes(session, DataJson.Options),
            null,
            DataProtectionScope.CurrentUser
        );
        File.WriteAllBytes(file + ".tmp", bytes);
        File.Move(file + ".tmp", file, true);
        Session = session;
    }

    public void Disconnect()
    {
        Session = null;
        if (File.Exists(file))
            File.Delete(file);
    }

    public Task<List<Achievement>> Achievements(int id, CancellationToken cancellation = default)
    {
        if (Session is not { } session)
            throw new InvalidOperationException(
                I18n.T("Vincula RetroAchievements en Ajustes para consultar sus logros.")
            );
        if (id <= 0)
            throw new ArgumentException(
                I18n.T("El ID de RetroAchievements debe ser un número positivo.")
            );
        return achievementRequests.Run(
            session.Username + "|" + session.ApiKey + "|" + session.Hardcore + "|" + id,
            token => FetchAchievements(id, session, token),
            cancellation
        );
    }

    private async Task<HttpResponseMessage> Send(string url, CancellationToken cancellation)
    {
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.UserAgent.ParseAdd("Checkpoint/0.8");
                var response = await http.SendAsync(request, cancellation);
                if (
                    attempt == 0
                    && response.StatusCode
                        is HttpStatusCode.BadGateway
                            or HttpStatusCode.GatewayTimeout
                )
                {
                    response.Dispose();
                    await Task.Delay(500, cancellation);
                    continue;
                }
                return response;
            }
            catch (Exception error)
                when (error is HttpRequestException
                    || error is OperationCanceledException && !cancellation.IsCancellationRequested
                )
            {
                if (attempt > 0)
                    throw new AchievementServiceException(
                        I18n.T("RetroAchievements no responde. Se conserva el progreso anterior."),
                        HttpStatusCode.GatewayTimeout,
                        true
                    );
                await Task.Delay(500, cancellation);
            }
        }
    }

    private async Task<List<Achievement>> FetchAchievements(
        int id,
        RetroSession session,
        CancellationToken cancellation
    )
    {
        if (Session != session)
            throw new OperationCanceledException(cancellation);
        var url =
            "https://retroachievements.org/API/API_GetGameInfoAndUserProgress.php?g="
            + id
            + "&u="
            + Uri.EscapeDataString(session.Username)
            + "&y="
            + Uri.EscapeDataString(session.ApiKey);
        try
        {
            using var response = await Send(url, cancellation);
            if (!response.IsSuccessStatusCode)
                throw new AchievementServiceException(
                    I18n.T(
                        response.StatusCode == HttpStatusCode.TooManyRequests
                            ? "Demasiadas consultas. Espera un minuto y vuelve a intentarlo."
                            : "No se pudo consultar RetroAchievements. Revisa tu clave y conserva el último progreso."
                    ),
                    response.StatusCode,
                    response.StatusCode
                        is HttpStatusCode.Unauthorized
                            or HttpStatusCode.Forbidden
                            or HttpStatusCode.TooManyRequests
                            or HttpStatusCode.ServiceUnavailable
                );
            using var json = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(cancellation)
            );
            var root = json.RootElement;
            if (
                root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("ID", out var gameId)
                || gameId.ToString() != id.ToString()
                || !root.TryGetProperty("Achievements", out var achievements)
            )
                throw new InvalidDataException(
                    I18n.T("RetroAchievements devolvió datos no válidos.")
                );
            var results = new List<Achievement>();
            if (achievements.ValueKind == JsonValueKind.Object)
                foreach (var entry in achievements.EnumerateObject())
                {
                    var a = entry.Value;
                    if (a.ValueKind != JsonValueKind.Object)
                        throw new InvalidDataException(
                            I18n.T("RetroAchievements devolvió datos no válidos.")
                        );
                    string Read(string name) =>
                        a.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String
                            ? p.GetString() ?? ""
                            : "";
                    string earned = session.Hardcore
                        ? Read("DateEarnedHardcore")
                        : Read("DateEarned");
                    if (!session.Hardcore && earned.Length == 0)
                        earned = Read("DateEarnedHardcore");
                    results.Add(
                        new()
                        {
                            Id = entry.Name,
                            Name = Read("Title"),
                            Description = Read("Description"),
                            Unlocked = DateTimeOffset.TryParse(
                                earned,
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.AssumeUniversal,
                                out _
                            ),
                            UnlockedAt = DateTimeOffset.TryParse(
                                earned,
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.AssumeUniversal,
                                out var date
                            )
                                ? date
                                : null,
                        }
                    );
                    if (results.Count > 10000)
                        throw new InvalidDataException(
                            I18n.T("RetroAchievements devolvió datos no válidos.")
                        );
                }
            else if (
                achievements.ValueKind != JsonValueKind.Array
                || achievements.GetArrayLength() != 0
            )
                throw new InvalidDataException(
                    I18n.T("RetroAchievements devolvió datos no válidos.")
                );
            var validation = new Game { Title = "RetroAchievements", RetroAchievements = results };
            try
            {
                GameRules.Validate(validation);
            }
            catch (ArgumentException)
            {
                throw new InvalidDataException(
                    I18n.T("RetroAchievements devolvió datos no válidos.")
                );
            }
            AchievementData.Validate(results);
            return results;
        }
        catch (HttpRequestException)
        {
            throw new InvalidOperationException(
                I18n.T("RetroAchievements no responde. Se conserva el progreso anterior.")
            );
        }
        catch (JsonException)
        {
            throw new InvalidDataException(I18n.T("RetroAchievements devolvió datos no válidos."));
        }
    }

    public void Dispose() => http.Dispose();
}
