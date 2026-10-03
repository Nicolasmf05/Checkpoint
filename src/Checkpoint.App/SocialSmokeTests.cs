using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Checkpoint.Core;

namespace Checkpoint.App;

public partial class MainWindow
{
    internal async Task RenderSocialSmokeTest(string output, Action<bool,string> check)
    {
        socialTimer.Stop(); publicationTimer.Stop();
        Guid own = Guid.NewGuid(), friend = Guid.NewGuid(), incoming = Guid.NewGuid(), requestId = Guid.NewGuid();
        Guid loginUser = own; bool accepted = false, blocked = false;
        var shared = new Dictionary<Guid,SocialPublication>();
        string? lastPublishedBody = null;
        bool privateCoverAuthenticated = false;
        int privateCoverRequests = 0;
        byte[] sharedCover = File.ReadAllBytes(Directory.GetFiles(Covers.DirectoryPath,"custom-*.png").First());
        var project = new SocialProject("https://fixture.supabase.co","sb_publishable_native_fixture");
        var handler = new NativeSocialHandler(async request =>
        {
            string path = request.RequestUri!.AbsolutePath;
            string? body = request.Content is null ? null : await request.Content.ReadAsStringAsync();
            if (path.EndsWith("/token")) return SocialResponse(new { access_token = "NATIVE-ACCESS-FIXTURE",refresh_token = "NATIVE-REFRESH-FIXTURE",expires_in = 3600,user = new { id = loginUser } });
            if (path.EndsWith("/logout")) return new(HttpStatusCode.NoContent);
            if (path.Contains("/storage/v1/object/authenticated/"))
            {
                privateCoverRequests++;
                privateCoverAuthenticated = request.Headers.Authorization?.Parameter == "NATIVE-ACCESS-FIXTURE";
                return new(HttpStatusCode.OK) { Content = new ByteArrayContent(sharedCover) };
            }
            if (path.EndsWith("/cp_profiles")) return SocialResponse(new[] {
                new SocialProfile(own,"Mi perfil","cp-111111111111"),new SocialProfile(friend,"Ana","cp-222222222222"),new SocialProfile(incoming,"Carlos","cp-333333333333") });
            if (path.EndsWith("/cp_friendships")) return SocialResponse(new[] {
                blocked ? null : new SocialFriendship(own,friend), accepted ? new SocialFriendship(own,incoming) : null
            }.Where(x => x is not null).ToArray());
            if (path.EndsWith("/cp_friend_requests"))
            {
                if (request.Method == HttpMethod.Patch) { accepted = true; return new(HttpStatusCode.NoContent); }
                return SocialResponse(accepted ? Array.Empty<SocialRequest>() : new[] { new SocialRequest(requestId,incoming,own,"pending") });
            }
            if (path.EndsWith("/cp_blocks"))
            {
                if (request.Method == HttpMethod.Post) { blocked = true; return new(HttpStatusCode.NoContent); }
                return SocialResponse(blocked ? new[] { new { blocked_id = friend } } : Array.Empty<object>());
            }
            if (path.EndsWith("/cp_game_publications"))
            {
                if (request.RequestUri.Query.Contains(friend.ToString())) return SocialResponse(blocked ? Array.Empty<SocialPublication>() : new[] {
                    new SocialPublication(friend,Guid.NewGuid(),1,Guid.NewGuid(),true,
                        SharedGamePayload.From(new Game { Title = "Celeste", Status = GameStatus.Playing, StoryPercent = 60 },friend + "/covers/fixture.png"),DateTimeOffset.UtcNow) });
                return SocialResponse(shared.Values.ToArray());
            }
            if (path.EndsWith("/cp_publish_game"))
            {
                lastPublishedBody = body;
                using var json = JsonDocument.Parse(body!); var obj = json.RootElement;
                Guid id = obj.GetProperty("p_game_id").GetGuid(), op = obj.GetProperty("p_operation_id").GetGuid();
                long expected = obj.GetProperty("p_expected_revision").GetInt64(); shared.TryGetValue(id,out var previous);
                if (previous?.OperationId == op) return SocialResponse(previous.Revision);
                if (expected != (previous?.Revision ?? 0)) return new(HttpStatusCode.Conflict) { Content = new StringContent("{\"code\":\"40001\"}") };
                var payload = obj.GetProperty("p_game").ValueKind == JsonValueKind.Null ? null : obj.GetProperty("p_game").Deserialize<SharedGamePayload>(SocialApi.Json);
                shared[id] = new(own,id,expected + 1,op,payload is not null,payload,DateTimeOffset.UtcNow); return SocialResponse(expected + 1);
            }
            throw new InvalidOperationException("Unexpected native social route: " + path);
        });
        AttachSocial(new(project,handler));
        friendsVisible = true; Width = 375; Height = 740; Refresh(); await FriendsView.Reload();
        await Dispatcher.InvokeAsync(UpdateLayout,DispatcherPriority.ContextIdle);
        check(Controls<PasswordBox>(this).Count() == 1 && Texts(this).Contains("Una cuenta de Checkpoint"),"friends tab has a real account form without Steam");
        RenderElement(this,Path.Combine(output,"widget-friends-login.png"));
        Preferences.Language = "en"; ApplyLanguage();
        await Dispatcher.InvokeAsync(UpdateLayout,DispatcherPriority.ContextIdle);
        check(Texts(this).Contains("A Checkpoint account") && (string?)FriendsButton.Content == "Friends", "English account form and dynamic navigation render");
        RenderElement(this,Path.Combine(output,"widget-friends-login-en.png"));
        Preferences.Language = "es"; ApplyLanguage();
        check(FriendsButton.TranslatePoint(new Point(FriendsButton.ActualWidth,0),this).X < Width - 22,"friends navigation fits the narrow widget");
        await Social!.Login("native_user","PASSWORD-FIXTURE"); await FriendsView.Reload();
        var encrypted = Encoding.UTF8.GetString(File.ReadAllBytes(SocialSessionPath));
        check(!encrypted.Contains("NATIVE-ACCESS-FIXTURE") && !encrypted.Contains("NATIVE-REFRESH-FIXTURE"),"Checkpoint tokens are saved encrypted with DPAPI");
        async Task ClickSocial(string content)
        {
            var button = Controls<Button>(this).Single(b => b != FriendsButton && (string?)b.Content == content);
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            for (int i = 0; FriendsView.Busy && i < 100; i++) await Task.Delay(10);
            await Dispatcher.InvokeAsync(UpdateLayout,DispatcherPriority.ContextIdle);
            check(!FriendsView.Busy,"social action completed: " + content);
        }
        await ClickSocial("Cuenta");
        check(Texts(this).Contains("checkpoint-111111111111") && !Texts(this).Any(t => t.StartsWith("cp-",StringComparison.OrdinalIgnoreCase)), "account view shows the complete friend code without the old abbreviation");
        RenderElement(this,Path.Combine(output,"widget-friends-account.png"));
        Preferences.Language = "en"; ApplyLanguage(); await ClickSocial("Account");
        check(Texts(this).Contains("checkpoint-111111111111"), "English account view retains the complete Checkpoint friend code");
        RenderElement(this,Path.Combine(output,"widget-friends-account-en.png"));
        await ClickSocial("Friends");
        var codeInput = Controls<TextBox>(this).Single(c => System.Windows.Automation.AutomationProperties.GetName(c) == "Friend code (checkpoint-…)");
        check(codeInput.MaxLength == 23, "friend input accepts the complete 23-character code");
        Preferences.Language = "es"; ApplyLanguage();
        await ClickSocial("Solicitudes");
        check(Texts(this).Contains("Carlos") && Texts(this).Contains("Quiere añadirte como amigo"),"pending Checkpoint requests are rendered");
        await ClickSocial("Aceptar"); check(accepted,"accept button sends the receiver's answer");
        var game = Games.First(); string oldNotes = game.Notes; int? oldStory = game.StoryPercent;
        game.Notes = "PRIVATE-NATIVE-NOTE"; game.StoryPercent = 45;
        await ClickSocial("Compartir");
        var shareControl = Controls<CheckBox>(this).Single(c => System.Windows.Automation.AutomationProperties.GetName(c) == "Compartir " + game.Title);
        shareControl.IsChecked = true; shareControl.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent)); publicationTimer.Stop();
        await PublishSocial();
        check(shared[game.Id].Payload!.StoryPercent == 45 && !lastPublishedBody!.Contains("PRIVATE-NATIVE-NOTE"),"real sharing control publishes safe progress");
        check(SocialOutbox.Load(OutboxPath(own),project.Validate().AbsoluteUri,own).Entry(game.Id).Selected,"sharing consent and revision persist for this account");
        await ClickSocial("Amigos"); await ClickSocial("Ver progreso de Ana");
        check(Texts(this).Contains("Celeste") && Texts(this).Contains("Historia: 60%"),"friend view shows progress from Checkpoint publications");
        check(privateCoverAuthenticated && VisualChildren(FriendsView).OfType<Image>().Any(i => i.Source is not null),"private friend cover downloads with account authorization and renders");
        RenderElement(this,Path.Combine(output,"widget-friends-progress.png"));
        int requestsBeforeLightweight = privateCoverRequests;
        Preferences.LightweightMode = true; ApplyPreferences(); FriendsView.RefreshLanguage();
        await ClickSocial("Ver progreso de Ana");
        check(privateCoverRequests == requestsBeforeLightweight && Texts(this).Contains("Historia: 60%") &&
            !VisualChildren(FriendsView).OfType<Image>().Any(i => i.IsVisible || i.Source is not null), "lightweight friends preserve progress without requesting private covers");
        Preferences.LightweightMode = false; ApplyPreferences(); FriendsView.RefreshLanguage();
        await ClickSocial("Ver progreso de Ana");
        check(privateCoverRequests > requestsBeforeLightweight && VisualChildren(FriendsView).OfType<Image>().Any(i => i.Source is not null), "disabling lightweight mode restores friend covers");
        var publicationBeforeLanguage = shared[game.Id].Payload;
        Preferences.Language = "en"; ApplyLanguage(); await ClickSocial("View progress for Ana");
        check(Texts(this).Contains("Story: 60%") && Texts(this).Contains("Goal: finish the story"), "English friend progress translates status, goal and counters");
        check(shared[game.Id].Payload == publicationBeforeLanguage && game.Notes == "PRIVATE-NATIVE-NOTE", "changing language preserves user content and publication payloads");
        RenderElement(this,Path.Combine(output,"widget-friends-progress-en.png"));
        Preferences.Language = "es"; ApplyLanguage(); await ClickSocial("Ver progreso de Ana");
        await ClickSocial("← Volver a mis amigos");
        var block = Controls<Button>(this).First(b => (string?)b.Content == "Bloquear"); block.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        for (int i = 0; FriendsView.Busy && i < 100; i++) await Task.Delay(10);
        check(blocked && !Texts(this).Contains("Celeste") && !Controls<Button>(this).Any(b => (string?)b.Content == "Ver progreso de Ana"),"blocking removes friend progress from the visible UI");
        SetShared(game,false); publicationTimer.Stop(); await PublishSocial();
        check(!shared[game.Id].IsShared && shared[game.Id].Payload is null && !Outbox!.Entry(game.Id).HasWork,"unsharing publishes a tombstone and clears content");
        await Social.Logout(); loginUser = Guid.NewGuid(); await Social.Login("other_user","PASSWORD-FIXTURE");
        check(Outbox!.UserId == loginUser && Outbox.Games.Count == 0,"switching accounts never inherits publication consent");
        await Social.Logout(); await FriendsView.Reload();
        check(!File.Exists(SocialSessionPath) && Texts(this).Contains("Una cuenta de Checkpoint"),"logout clears the saved Checkpoint session and remote view");
        game.Notes = oldNotes; game.StoryPercent = oldStory; friendsVisible = false; Persist(); Refresh();
    }
    private sealed class NativeSocialHandler(Func<HttpRequestMessage,Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken) => respond(request);
    }
    private static HttpResponseMessage SocialResponse(object value) => new(HttpStatusCode.OK)
    { Content = new StringContent(JsonSerializer.Serialize(value,SocialApi.Json),Encoding.UTF8,"application/json") };
}
