using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Checkpoint.Core;

namespace Checkpoint.App;

public partial class MainWindow
{
    internal SocialApi? Social;
    internal SocialOutbox? Outbox;
    internal FriendsPanel FriendsView = null!;
    private readonly DispatcherTimer socialTimer = new() { Interval = TimeSpan.FromSeconds(60) };
    private readonly DispatcherTimer publicationTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    private bool socialSyncing, friendsVisible;
    private string SocialSessionPath => Path.Combine(Store.DirectoryPath,"checkpoint-session.dat");
    private string OutboxPath(Guid user) => Path.Combine(Store.DirectoryPath,"social",user.ToString("N") + ".json");
    private string? socialStartupError;
    private void InitializeSocial()
    {
        try
        {
            var config = Path.Combine(AppContext.BaseDirectory,"supabase-config.json");
            if (File.Exists(config))
            {
                var project = JsonSerializer.Deserialize<SocialProject>(File.ReadAllText(config),SocialApi.Json)!;
                SocialSession? session = null;
                try
                {
                    if (File.Exists(SocialSessionPath)) session = JsonSerializer.Deserialize<SocialSession>(
                        ProtectedData.Unprotect(File.ReadAllBytes(SocialSessionPath),null,DataProtectionScope.CurrentUser),SocialApi.Json);
                }
                catch (Exception ex) when (ex is IOException or CryptographicException or JsonException) { }
                AttachSocial(new(project,session:session));
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or ArgumentException) { socialStartupError = "Amigos: " + ex.Message; }
        FriendsView = new(this); FriendsHost.Child = FriendsView;
        publicationTimer.Tick += async (_, _) => { publicationTimer.Stop(); await PublishSocial(); };
        socialTimer.Tick += async (_, _) => { await PublishSocial(); if (friendsVisible && !FriendsView.Busy) await FriendsView.Reload(); };
    }
    internal void AttachSocial(SocialApi api)
    {
        Social?.Dispose(); Social = api;
        api.SessionChanged += session =>
        {
            if (session is null)
            {
                if (File.Exists(SocialSessionPath)) File.Delete(SocialSessionPath);
                Outbox = null;
            }
            else
            {
                var bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(session,SocialApi.Json)),null,DataProtectionScope.CurrentUser);
                File.WriteAllBytes(SocialSessionPath + ".tmp",bytes); File.Move(SocialSessionPath + ".tmp",SocialSessionPath,true);
                if (Outbox?.UserId != session.UserId) Outbox = SocialOutbox.Load(OutboxPath(session.UserId),session.ProjectUrl,session.UserId);
            }
        };
        if (api.Session is { } current) Outbox = SocialOutbox.Load(OutboxPath(current.UserId),current.ProjectUrl,current.UserId);
    }
    private void StartSocial()
    {
        socialTimer.Start();
        if (socialStartupError is not null) Notice(socialStartupError);
        if (Social?.Session is not null) { SchedulePublications(); _ = PublishSocial(); }
    }
    internal void SaveOutbox() { if (Outbox is not null) Outbox.Save(OutboxPath(Outbox.UserId)); }
    private (SharedGamePayload Payload, string? LocalCover) ProjectGame(Game game)
    {
        string? remote = null, local = null;
        if (BackupFiles.IsCustomCoverName(game.CustomCover))
        {
            string file = Path.Combine(Covers.DirectoryPath,game.CustomCover!);
            if (File.Exists(file))
            {
                if (new FileInfo(file).Length > 2097152) throw new ArgumentException("La carátula compartida debe ocupar menos de 2 MiB.");
                remote = Outbox!.UserId + "/covers/" + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))).ToLowerInvariant() + ".png";
                local = game.CustomCover;
            }
        }
        return (SharedGamePayload.From(game,remote),local);
    }
    internal void SetShared(Game game, bool share)
    {
        if (Outbox is null) throw new InvalidOperationException("Entra en Checkpoint antes de compartir.");
        if (share) { var projection = ProjectGame(game); Outbox.SetDesired(game.Id,projection.Payload,projection.LocalCover); }
        else Outbox.SetDesired(game.Id,null);
        SaveOutbox(); SchedulePublications();
    }
    internal void SchedulePublications()
    {
        if (Outbox is null) return;
        try
        {
            foreach (var pair in Outbox.Games.ToArray())
            {
                if (!pair.Value.Selected) continue;
                var game = Games.FirstOrDefault(g => g.Id == pair.Key);
                if (game is null) Outbox.SetDesired(pair.Key,null);
                else { var projection = ProjectGame(game); Outbox.SetDesired(game.Id,projection.Payload,projection.LocalCover); }
            }
            SaveOutbox(); publicationTimer.Stop(); publicationTimer.Start();
        }
        catch (Exception ex) when (ex is IOException or ArgumentException) { Notice("Publicación pendiente: " + ex.Message); }
    }
    internal async Task PublishSocial()
    {
        if (socialSyncing || Social?.Session is null || Outbox is null || shutdown.IsCancellationRequested) return;
        socialSyncing = true; var api = Social; var box = Outbox;
        try
        {
            foreach (var id in box.Games.Keys.ToArray())
            {
                // Acknowledge a sent operation before preparing a subsequent edit/withdrawal.
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    if (Outbox != box || api.Session?.UserId != box.UserId) return;
                    var operation = box.Prepare(id); if (operation is null) break;
                    SaveOutbox();
                    try
                    {
                        if (operation.Payload?.CoverPath is string path && operation.LocalCover is string local)
                        {
                            if (!BackupFiles.IsCustomCoverName(local)) throw new InvalidDataException("Carátula local no válida.");
                            var bytes = File.ReadAllBytes(Path.Combine(Covers.DirectoryPath,local));
                            if (!path.EndsWith(Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant() + ".png",StringComparison.Ordinal))
                                throw new InvalidDataException("La carátula cambió; revisa la publicación.");
                            await api.UploadCover(path,bytes,shutdown.Token);
                        }
                        long revision = await api.Publish(id,operation,shutdown.Token);
                        box.Acknowledge(id,revision); SaveOutbox();
                    }
                    catch (SocialApiException ex) when (ex.IsConflict)
                    {
                        var remote = (await api.Publications(box.UserId,shutdown.Token)).FirstOrDefault(g => g.GameId == id);
                        if (remote is null) throw;
                        box.Reconcile(id,remote); SaveOutbox();
                        if (box.Entry(id).Conflict) { Notice("Hay un conflicto de publicación. Revísalo en Amigos → Compartir."); break; }
                    }
                }
            }
        }
        catch (Exception ex) when (ex is System.Net.Http.HttpRequestException or IOException or InvalidOperationException or OperationCanceledException)
        { if (!shutdown.IsCancellationRequested) Notice("Checkpoint: " + ex.Message); }
        finally { socialSyncing = false; }
    }
    internal bool SocialSyncing => socialSyncing;
    private async void FriendsClick(object sender, RoutedEventArgs e)
    {
        friendsVisible = true; Refresh(); await FriendsView.Reload();
    }
    private void ApplySocialTab()
    {
        FilterArea.Visibility = friendsVisible ? Visibility.Collapsed : Visibility.Visible;
        GameArea.Visibility = friendsVisible ? Visibility.Collapsed : Visibility.Visible;
        FriendsHost.Visibility = friendsVisible ? Visibility.Visible : Visibility.Collapsed;
        SummaryTitle.Text = friendsVisible ? "Tu gente, tus aventuras" : "Tu próxima aventura";
        FriendsButton.Foreground = (System.Windows.Media.Brush)Application.Current.Resources[friendsVisible ? "AccentBrush" : "TextBrush"];
        if (friendsVisible) Summary.Text = "Progreso compartido desde Checkpoint";
    }
}
