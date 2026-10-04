// Pruebas de carátulas con respuestas simuladas y archivos locales de diagnóstico.
// Comprueban preparación de PNG, aceptación de propuestas y conservación de rechazos.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Checkpoint.Core;

namespace Checkpoint.App;

public partial class MainWindow
{
    private static byte[] CoverFixturePng()
    {
        var pixels = new byte[16 * 24 * 4];
        for (int n = 0; n < pixels.Length; n += 4)
        {
            pixels[n] = 100;
            pixels[n + 1] = 80;
            pixels[n + 2] = 180;
            pixels[n + 3] = 255;
        }
        var image = BitmapSource.Create(16, 24, 96, 96, PixelFormats.Bgra32, null, pixels, 16 * 4);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private static IgdbClient CoverFixtureClient(
        Action<JsonElement>? capture = null,
        bool shared = false
    ) =>
        new(
            new SocialProject("https://covers-fixture.supabase.co", "sb_publishable_fixture"),
            new NativeSteamHandler(request =>
            {
                if (request.RequestUri!.AbsolutePath.EndsWith("/search"))
                {
                    using var body = JsonDocument.Parse(
                        request.Content!.ReadAsStringAsync().GetAwaiter().GetResult()
                    );
                    capture?.Invoke(body.RootElement);
                    bool next = body
                        .RootElement.GetProperty("excluded")
                        .ToString()
                        .Contains("fixture_first");
                    return SteamResponse(
                        new
                        {
                            candidate = new IgdbCover(
                                next ? 2 : 1,
                                next ? "Second cover fixture" : "Closest cover fixture",
                                next ? "fixture_second" : "fixture_first",
                                2020,
                                0.95
                            ),
                        }
                    );
                }
                if (request.RequestUri!.AbsolutePath.EndsWith("/shared"))
                {
                    using var body = JsonDocument.Parse(
                        request.Content!.ReadAsStringAsync().GetAwaiter().GetResult()
                    );
                    capture?.Invoke(body.RootElement);
                    return SteamResponse(
                        new
                        {
                            candidate = shared
                            && !body
                                .RootElement.GetProperty("excluded")
                                .ToString()
                                .Contains("fixture_second")
                                ? new IgdbCover(
                                    2,
                                    "Second cover fixture",
                                    "fixture_second",
                                    2020,
                                    .95
                                )
                                : null,
                        }
                    );
                }
                if (request.RequestUri!.AbsolutePath.EndsWith("/confirm"))
                {
                    using var body = JsonDocument.Parse(
                        request.Content!.ReadAsStringAsync().GetAwaiter().GetResult()
                    );
                    capture?.Invoke(body.RootElement);
                    return SteamResponse(new { shared = true });
                }
                var content = new ByteArrayContent(CoverFixturePng());
                content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
            })
        );

    internal async Task RenderCoverSmokeTest(string output, Action<bool, string> check)
    {
        bool refresh = false,
            excluded = false;
        using var client = CoverFixtureClient(body =>
        {
            refresh = body.GetProperty("refresh").GetBoolean();
            excluded = body.GetProperty("excluded").ToString().Contains("fixture_first");
        });
        var candidate = await client.Search("Fixture", ["fixture_first"], refresh: true);
        check(
            candidate?.ImageId == "fixture_second" && refresh && excluded,
            "native IGDB manual lookup excludes rejected covers and requests fresh provider results"
        );
        var png = CoverCache.PrepareRemote(await client.Image(candidate!.ImageId));
        check(
            BackupFiles.IsPng(png) && CoverCache.ReadPrepared(png).PixelWidth > 0,
            "IGDB images are decoded and prepared for offline custom-cover storage"
        );
        using var sharedClient = CoverFixtureClient(shared: true);
        var sharedGame = new Game
        {
            Title = "Fixture",
            Platform = "PC",
            SteamAppId = 620,
            Tracked = false,
        };
        check(
            (await sharedClient.Shared(sharedGame))?.ImageId == "fixture_second",
            "native community lookup works for an untracked library game"
        );
        sharedGame.RejectedIgdbCovers.Add("fixture_second");
        check(
            await sharedClient.Shared(sharedGame) is null,
            "native community lookup retains rejected-cover exclusions"
        );
        bool textOnly = false;
        using var reporter = CoverFixtureClient(body =>
            textOnly =
                body.GetProperty("imageId").GetString() == "fixture_second"
                && !body.TryGetProperty("customCover", out _)
        );
        sharedGame.IgdbCoverImageId = "fixture_second";
        await reporter.Confirm(sharedGame);
        check(
            textOnly,
            "native saved-cover confirmation sends an IGDB reference without image bytes"
        );
        bool accepted = true;
        RunModal(
            () => accepted = Dialogs.IgdbCoverPreview(this, this, "Fixture", candidate, png),
            window =>
            {
                check(
                    Texts(window).Contains(I18n.T("¿Quieres usar esta carátula?")),
                    "native cover preview requires an explicit user choice"
                );
                RenderElement(window, Path.Combine(output, "dialog-igdb-cover.png"));
                Click(window, I18n.T("No usar esta carátula"));
            }
        );
        check(!accepted, "rejecting a native IGDB preview does not accept or install the image");
        RunModal(
            () => accepted = Dialogs.IgdbCoverPreview(this, this, "Fixture", candidate, png),
            window => Click(window, I18n.T("Usar esta carátula"))
        );
        check(accepted, "native IGDB preview accepts only after the Use action");
    }
}
