// Comprueba el contrato social, privacidad, conflictos y reintentos con solicitudes simuladas.

using System.Net;
using System.Text;
using System.Text.Json;
using Checkpoint.Core;

internal static class SocialChecks
{
    internal static async Task Run(
        string root,
        Action<bool, string> check,
        Action<Action, string> reject
    )
    {
        var user = Guid.NewGuid();
        var game = new Game
        {
            Title = "Social test",
            Notes = "NEVER UPLOAD NOTES",
            CustomGoal = "Goal",
            Goal = GameGoal.Custom,
            StoryPercent = 40,
            Tasks = [new() { Title = "NEVER UPLOAD TASK", Done = true }],
            Achievements =
            [
                new()
                {
                    Id = "PRIVATE-ID",
                    Name = "PRIVATE-NAME",
                    Unlocked = true,
                },
            ],
        };
        game.CustomCover = "C:/private/local-cover.png";
        var payload = SharedGamePayload.From(game);
        check(payload.CoverPath is null, "local custom covers are excluded from shared progress");
        string json = JsonSerializer.Serialize(payload, SocialApi.Json);
        check(
            !json.Contains("NEVER")
                && !json.Contains("PRIVATE-")
                && !json.Contains("notes", StringComparison.OrdinalIgnoreCase),
            "social projection excludes notes, task labels and achievement details"
        );
        check(
            payload.TasksDone == 1
                && payload.AchievementsUnlocked == 1
                && payload.StoryPercent == 40,
            "shared counters stay separate from story percentage"
        );
        reject(
            () => GameRules.Validate(new Game { Title = "Invalid", StoryPercent = 101 }),
            "invalid manual story progress rejected"
        );
        var box = new SocialOutbox { ProjectUrl = "https://example.supabase.co/", UserId = user };
        box.SetDesired(game.Id, payload);
        var operation = box.Prepare(game.Id)!;
        box.SetDesired(game.Id, payload with { StoryPercent = 60 });
        check(
            box.Prepare(game.Id)!.Id == operation.Id && operation.Payload!.StoryPercent == 40,
            "new edits preserve the in-flight operation for idempotency"
        );
        operation.Payload = operation.Payload! with { CoverPath = "legacy/image.png" };
        operation.LocalCover = "C:/private/image.png";
        box.Entry(game.Id).Desired = box.Entry(game.Id).Desired! with
        {
            CoverPath = "legacy/image.png",
        };
        string path = Path.Combine(root, "social-queue.json");
        box.Save(path);
        box = SocialOutbox.Load(path, box.ProjectUrl, user);
        check(
            box.Prepare(game.Id)!.Id == operation.Id
                && box.Entry(game.Id).Desired!.StoryPercent == 60,
            "outbox survives a restart with operation and later desired state"
        );
        check(
            box.Entry(game.Id).Desired!.CoverPath is null
                && box.Prepare(game.Id)!.Payload!.CoverPath is null
                && box.Prepare(game.Id)!.LocalCover is null,
            "legacy queues discard image paths without losing pending progress"
        );
        reject(
            () => SocialOutbox.Load(path, box.ProjectUrl, Guid.NewGuid()),
            "publication consent is bound to its account"
        );
        var remote = new SocialPublication(
            user,
            game.Id,
            1,
            operation.Id,
            true,
            payload,
            DateTimeOffset.UtcNow
        );
        box.Reconcile(game.Id, remote);
        check(
            !box.Entry(game.Id).Conflict
                && box.Entry(game.Id).Revision == 1
                && box.Prepare(game.Id)!.Payload!.StoryPercent == 60,
            "lost acknowledgement reconciles before publishing a newer edit"
        );
        box.Reconcile(
            game.Id,
            remote with
            {
                OperationId = Guid.NewGuid(),
                Payload = payload with { StoryPercent = 60, CoverPath = "legacy/image.png" },
            }
        );
        check(
            !box.Entry(game.Id).Conflict && box.Entry(game.Id).Published!.CoverPath is null,
            "legacy remote covers do not cause false progress conflicts"
        );
        box.Reconcile(
            game.Id,
            remote with
            {
                Revision = 2,
                OperationId = Guid.NewGuid(),
                Payload = payload with { StoryPercent = 70 },
            }
        );
        check(
            box.Entry(game.Id).Conflict && box.Prepare(game.Id) is null,
            "a different device causes an explicit publication conflict"
        );
        box.ResolveWithLocal(game.Id);
        check(
            box.Prepare(game.Id)!.ExpectedRevision == 2,
            "explicit local resolution uses latest remote revision"
        );
        box.SetDesired(game.Id, null);
        box.Reconcile(game.Id, remote with { Revision = 3, OperationId = Guid.NewGuid() });
        var withdrawal = box.Prepare(game.Id)!;
        check(
            !box.Entry(game.Id).Conflict
                && withdrawal.Payload is null
                && withdrawal.ExpectedRevision == 3,
            "withdrawal overrides stale sharing without restoring access"
        );
        box.Acknowledge(game.Id, 4);
        check(
            !box.Entry(game.Id).HasWork && !box.Entry(game.Id).Selected,
            "acknowledged withdrawal stays private"
        );

        var project = new SocialProject("https://example.supabase.co", "sb_publishable_test");
        reject(
            () =>
                new SocialProject(
                    "https://example.supabase.co/attacker",
                    "sb_publishable_test"
                ).Validate(),
            "social config rejects extra URL routes"
        );
        reject(
            () => new SocialProject(project.Url, "service_role_secret").Validate(),
            "desktop rejects privileged keys"
        );
        var calls = new List<(string Path, string? Body, string? Token)>();
        using var api = new SocialApi(
            project,
            new Handler(async request =>
            {
                string? body = request.Content is null
                    ? null
                    : await request.Content.ReadAsStringAsync();
                calls.Add(
                    (
                        request.RequestUri!.PathAndQuery,
                        body,
                        request.Headers.Authorization?.Parameter
                    )
                );
                check(
                    request.Headers.GetValues("apikey").Single() == project.PublishableKey,
                    "public key attached to social request"
                );
                if (request.RequestUri.AbsolutePath.EndsWith("/token"))
                    return Response(
                        new
                        {
                            access_token = "ACCESS-FIXTURE",
                            refresh_token = "REFRESH-FIXTURE",
                            expires_in = 3600,
                            user = new { id = user },
                        }
                    );
                if (request.RequestUri.AbsolutePath.EndsWith("/signup"))
                    return Response(new { id = user });
                if (request.RequestUri.AbsolutePath.EndsWith("/cp_publish_game"))
                    return Response(1);
                if (request.RequestUri.AbsolutePath.EndsWith("/logout"))
                    return new(HttpStatusCode.NoContent);
                return Response(Array.Empty<object>());
            })
        );
        check(
            SocialApi.AccountAddress("  TEST_User  ") == "test_user@accounts.checkpoint.invalid",
            "username maps to a normalized internal address without a real email"
        );
        reject(
            () => SocialApi.AccountAddress("person@example.com"),
            "username input rejects email addresses"
        );
        check(
            !await api.Register("test_user", "PASSWORD-FIXTURE", "Test"),
            "unexpected confirmation settings are detected"
        );
        await api.Login("test_user", "PASSWORD-FIXTURE");
        check(
            api.Session!.UserId == user && calls.Last().Token is null,
            "login does not attach a previous account token"
        );
        check(
            FriendCodes.Display("cp-ABCDEF012345") == "checkpoint-abcdef012345",
            "historical friend codes display the complete Checkpoint name"
        );
        check(
            FriendCodes.Display(" CHECKPOINT-ABCDEF012345 ") == "checkpoint-abcdef012345",
            "complete friend codes normalize case and surrounding spaces"
        );
        reject(
            () => FriendCodes.Display("checkpoint-abcdef01234"),
            "short friend codes are rejected"
        );
        reject(
            () => FriendCodes.Display("checkpoint-abcdef0123456"),
            "long friend codes are rejected"
        );
        reject(
            () => FriendCodes.Display("checkpoint-abcdef01234z"),
            "nonhex friend codes are rejected"
        );
        reject(
            () => FriendCodes.Display("other-abcdef012345"),
            "unrecognized friend prefixes are rejected"
        );
        await api.Find(" CHECKPOINT-ABCDEF012345 ");
        using (var lookup = JsonDocument.Parse(calls.Last().Body!))
            check(
                lookup.RootElement.GetProperty("p_code").GetString() == "cp-abcdef012345",
                "complete friend codes resolve existing service identities"
            );
        await api.Find("cp-abcdef012345");
        using (var lookup = JsonDocument.Parse(calls.Last().Body!))
            check(
                lookup.RootElement.GetProperty("p_code").GetString() == "cp-abcdef012345",
                "previously copied friend codes still resolve the same identity"
            );
        await api.Publish(
            game.Id,
            new()
            {
                ExpectedRevision = 0,
                Payload = payload with { CoverPath = "legacy/covers/image.png" },
            }
        );
        using (var publication = JsonDocument.Parse(calls.Last().Body!))
            check(
                publication.RootElement.GetProperty("p_game").GetProperty("coverPath").ValueKind
                    == JsonValueKind.Null,
                "API strips legacy image references before publication"
            );
        check(
            calls.Last().Token == "ACCESS-FIXTURE" && !calls.Last().Body!.Contains("NEVER"),
            "publication carries owner authentication and an allowlisted body"
        );
        await api.Publish(game.Id, new() { ExpectedRevision = 1, Payload = null });
        using (var body = JsonDocument.Parse(calls.Last().Body!))
            check(
                body.RootElement.GetProperty("p_game").ValueKind == JsonValueKind.Null,
                "withdrawal sends explicit SQL null argument"
            );
        await api.Logout();
        check(
            api.Session is null && calls.Last().Path.EndsWith("scope=local"),
            "logout clears only the current device session"
        );
        bool failed = false;
        try
        {
            await api.Profiles();
        }
        catch (SocialApiException)
        {
            failed = true;
        }
        check(failed, "social reads require a session");
        using var denied = new SocialApi(
            project,
            new Handler(_ =>
                Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent(
                            "{\"code\":\"40001\",\"message\":\"DO NOT ECHO PRIVATE SERVER DATA\"}"
                        ),
                    }
                )
            ),
            new(
                project.Validate().AbsoluteUri,
                user,
                "TOKEN",
                "REFRESH",
                DateTimeOffset.UtcNow.AddHours(1)
            )
        );
        try
        {
            await denied.Publish(game.Id, new());
        }
        catch (SocialApiException ex)
        {
            check(
                ex.IsConflict && !ex.Message.Contains("PRIVATE"),
                "API exposes a safe conflict message without echoing server data"
            );
        }
        var refreshes = 0;
        using var refreshing = new SocialApi(
            project,
            new Handler(request =>
            {
                if (request.RequestUri!.AbsolutePath.EndsWith("/token"))
                {
                    refreshes++;
                    return Task.FromResult(
                        Response(
                            new
                            {
                                access_token = "NEW",
                                refresh_token = "NEW-REFRESH",
                                expires_in = 3600,
                                user = new { id = user },
                            }
                        )
                    );
                }
                return Task.FromResult(Response(Array.Empty<object>()));
            }),
            new(
                project.Validate().AbsoluteUri,
                user,
                "OLD",
                "OLD-REFRESH",
                DateTimeOffset.UtcNow.AddMinutes(-1)
            )
        );
        await Task.WhenAll(refreshing.Profiles(), refreshing.Requests());
        check(
            refreshes == 1 && refreshing.Session!.AccessToken == "NEW",
            "concurrent requests rotate an expired refresh token once"
        );
        var spanishProjection = SharedGamePayload.From(game);
        I18n.SetLanguage("en");
        check(
            Labels.Status(GameStatus.Playing) == "Playing"
                && Labels.Goal(GameGoal.Story) == "Story",
            "English model labels use the built-in catalog"
        );
        check(
            SharedGamePayload.From(game) == spanishProjection,
            "language changes do not change shared wire data"
        );
        check(
            I18n.T("Unknown user text") == "Unknown user text",
            "localization leaves user text unchanged"
        );
        I18n.SetLanguage("es");
        check(
            Labels.Status(GameStatus.Playing) == "Jugando",
            "Spanish remains available after language switching"
        );
    }

    internal sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) => respond(request);
    }

    private static HttpResponseMessage Response(object value) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(value, SocialApi.Json),
                Encoding.UTF8,
                "application/json"
            ),
        };
}
