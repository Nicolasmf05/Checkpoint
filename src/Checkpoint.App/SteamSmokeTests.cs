// Pruebas de acceso y sincronización Steam con un servicio HTTP simulado.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Checkpoint.Core;

namespace Checkpoint.App;

public partial class MainWindow
{
    internal async Task RenderSteamSmokeTest(string output, Action<bool, string> check)
    {
        const string endpoint =
            "https://fumdnvvvoiwoiziwtmsu.supabase.co/functions/v1/checkpoint-steam/";
        const string token = "ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ";
        const string steamId = "76561198000000000";
        check(
            SteamClient.ValidateServiceUrl(endpoint.TrimEnd('/')).AbsoluteUri == endpoint,
            "Steam client accepts the Supabase function endpoint"
        );
        foreach (
            string invalid in new[]
            {
                "https://evil.example/functions/v1/checkpoint-steam/",
                "https://test.supabase.co/functions/v1/other/",
                endpoint + "?key=secret",
                "https://user:pass@test.supabase.co/",
                "http://test.supabase.co/",
            }
        )
        {
            bool rejected = false;
            try
            {
                SteamClient.ValidateServiceUrl(invalid);
            }
            catch (ArgumentException)
            {
                rejected = true;
            }
            check(
                rejected,
                "Steam endpoint rejects unsafe address: "
                    + invalid.Replace("?key=secret", "?key=[redacted]")
            );
        }
        string folder = Path.Combine(output, "steam-fixture");
        Directory.CreateDirectory(folder);
        bool authenticated = false;
        string? language = null;
        using var client = new SteamClient(
            folder,
            new NativeSteamHandler(request =>
            {
                var uri = request.RequestUri!;
                if (uri.AbsolutePath.EndsWith("/v1/auth/start"))
                {
                    check(
                        request.Headers.Authorization is null,
                        "Steam login does not send an unrelated bearer token"
                    );
                    return SteamResponse(
                        new
                        {
                            flowId = token,
                            pollSecret = token,
                            authorizeUrl = "https://steamcommunity.com/openid/login",
                        }
                    );
                }
                authenticated = request.Headers.Authorization?.Parameter == token;
                if (uri.AbsolutePath.EndsWith("/v1/library"))
                    return SteamResponse(
                        new
                        {
                            games = new[]
                            {
                                new
                                {
                                    appId = 620,
                                    name = "Portal 2",
                                    playtimeMinutes = 70,
                                },
                            },
                        }
                    );
                if (uri.AbsolutePath.EndsWith("/achievements"))
                {
                    language = uri.Query;
                    return SteamResponse(
                        new
                        {
                            achievements = new[]
                            {
                                new
                                {
                                    id = "FIRST",
                                    name = "First",
                                    description = "",
                                    hidden = false,
                                    unlocked = true,
                                },
                            },
                        }
                    );
                }
                if (uri.AbsolutePath.EndsWith("/v1/auth/logout"))
                    return SteamResponse(new { ok = true });
                throw new InvalidOperationException("Unexpected Steam native route");
            })
        );
        await client.BeginLogin(endpoint, CancellationToken.None);
        client.SaveSession(endpoint, new LoginResult("complete", token, steamId));
        using (var restored = new SteamClient(folder))
            check(
                restored.Session?.Token == token && restored.Session.SteamId == steamId,
                "Steam DPAPI session persists and restores"
            );
        check(
            !Encoding
                .UTF8.GetString(File.ReadAllBytes(Path.Combine(folder, "steam-session.dat")))
                .Contains(token),
            "Steam session is encrypted on disk"
        );
        var library = await client.Library(endpoint, CancellationToken.None);
        check(
            authenticated && library.Games[0].AppId == 620,
            "Steam library request uses its bound session and parses the response"
        );
        I18n.SetLanguage("en");
        await client.Achievements(endpoint, 620, CancellationToken.None);
        check(language == "?lang=en", "English Steam requests select English achievements");
        I18n.SetLanguage("es");
        await client.Achievements(endpoint, 620, CancellationToken.None);
        check(language == "?lang=es", "Spanish Steam requests select Spanish achievements");
        using (
            var errorClient = new SteamClient(
                Path.Combine(output, "steam-error-fixture"),
                new NativeSteamHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(
                        "{\"error\":\"Unexpected foreign server message\"}"
                    ),
                })
            )
        )
        {
            foreach (var selectedLanguage in new[] { "es", "en" })
            {
                I18n.SetLanguage(selectedLanguage);
                bool localized = false;
                try
                {
                    await errorClient.BeginLogin(endpoint, CancellationToken.None);
                }
                catch (InvalidOperationException error)
                {
                    localized =
                        error.Message
                        == I18n.T(
                            "No se pudo consultar Steam. Se conserva el último progreso guardado."
                        );
                }
                check(
                    localized,
                    "unknown Steam errors retain the selected language " + selectedLanguage
                );
            }
            I18n.SetLanguage("es");
        }
        bool wrongServer = false;
        foreach (string selectedLanguage in new[] { "es", "en" })
        {
            I18n.SetLanguage(selectedLanguage);
            foreach (
                var failure in new[]
                {
                    (
                        HttpStatusCode.Forbidden,
                        "Steam no permite consultar estos logros. Revisa la privacidad de tus detalles de juegos."
                    ),
                    (
                        HttpStatusCode.Unauthorized,
                        "La sesión ha caducado. Vuelve a vincular Steam."
                    ),
                }
            )
            {
                using var errorClient = new SteamClient(
                    Path.Combine(output, "steam-error-fixture"),
                    new NativeSteamHandler(_ => new HttpResponseMessage(failure.Item1)
                    {
                        Content = new StringContent(
                            JsonSerializer.Serialize(new { error = failure.Item2 })
                        ),
                    })
                );
                bool localized = false;
                try
                {
                    await errorClient.BeginLogin(endpoint, CancellationToken.None);
                }
                catch (InvalidOperationException error)
                {
                    localized = I18n.Error(error) == I18n.T(failure.Item2);
                }
                check(
                    localized,
                    "Steam privacy and expired-session causes are localized "
                        + selectedLanguage
                        + " "
                        + (int)failure.Item1
                );
            }
        }
        I18n.SetLanguage("es");
        try
        {
            await client.Library("https://another.example/", CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            wrongServer = true;
        }
        check(wrongServer, "Steam session cannot be sent to a different service");
        bool malformed = false;
        try
        {
            client.SaveSession(endpoint, new LoginResult("complete", "bad", steamId));
        }
        catch (InvalidDataException)
        {
            malformed = true;
        }
        check(
            malformed && client.Session?.Token == token,
            "Malformed Steam sessions preserve the existing session"
        );
        int attempts = 0;
        string retryFolder = Path.Combine(output, "steam-retry-fixture");
        Directory.CreateDirectory(retryFolder);
        using (
            var retryClient = new SteamClient(
                retryFolder,
                new NativeSteamHandler(_ =>
                    ++attempts == 1
                        ? new HttpResponseMessage(HttpStatusCode.BadGateway)
                        {
                            Content = new StringContent("{}"),
                        }
                        : SteamResponse(new { achievements = Array.Empty<Achievement>() })
                )
            )
        )
        {
            retryClient.SaveSession(endpoint, new LoginResult("complete", token, steamId));
            var result = await retryClient.Achievements(endpoint, 620, CancellationToken.None);
            check(
                attempts == 2 && result.Achievements.Length == 0,
                "Steam retries a transient read once and accepts a known empty achievement list"
            );
        }
        attempts = 0;
        using (
            var limitedClient = new SteamClient(
                retryFolder,
                new NativeSteamHandler(_ =>
                {
                    attempts++;
                    var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                    {
                        Content = new StringContent("{}"),
                    };
                    response.Headers.RetryAfter =
                        new System.Net.Http.Headers.RetryConditionHeaderValue(
                            TimeSpan.FromSeconds(60)
                        );
                    return response;
                })
            )
        )
        {
            for (int id = 1; id <= 2; id++)
            {
                bool blocked = false;
                try
                {
                    await limitedClient.Achievements(endpoint, id, CancellationToken.None);
                }
                catch (AchievementServiceException error)
                {
                    blocked = error.StopsBatch && error.Status == HttpStatusCode.TooManyRequests;
                }
                check(blocked, "Steam rate failures stop a batch and retain a typed reason");
            }
            check(
                attempts == 1,
                "Steam cooldown prevents subsequent games from sending more requests"
            );
        }
        using (
            var malformedClient = new SteamClient(
                retryFolder,
                new NativeSteamHandler(_ => SteamResponse(new { ok = true }))
            )
        )
        {
            bool rejected = false;
            try
            {
                await malformedClient.Achievements(endpoint, 620, CancellationToken.None);
            }
            catch (InvalidDataException)
            {
                rejected = true;
            }
            check(
                rejected,
                "Steam rejects missing achievement payloads instead of clearing existing progress"
            );
        }
        await client.Disconnect();
        check(
            client.Session is null && !File.Exists(Path.Combine(folder, "steam-session.dat")),
            "Steam unlink revokes and removes the local session"
        );
    }

    private sealed class NativeSteamHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) => Task.FromResult(respond(request));
    }

    private static HttpResponseMessage SteamResponse(object value) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(value, DataJson.Options),
                Encoding.UTF8,
                "application/json"
            ),
        };
}
