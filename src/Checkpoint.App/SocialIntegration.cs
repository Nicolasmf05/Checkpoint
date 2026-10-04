// Vincula la sesión social protegida, la cola persistente y los temporizadores de publicación.
// Cada cambio local recalcula el progreso compartible según el seguimiento y la privacidad.

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
    private readonly DispatcherTimer publicationTimer = new()
    {
        Interval = TimeSpan.FromSeconds(2),
    };
    private bool socialSyncing,
        friendsVisible;
    private string SocialSessionPath => Path.Combine(Store.DirectoryPath, "checkpoint-session.dat");

    private string OutboxPath(Guid user) =>
        Path.Combine(Store.DirectoryPath, "social", user.ToString("N") + ".json");

    private string? socialStartupError;

    private void InitializeSocial()
    {
        try
        {
            var config = Path.Combine(AppContext.BaseDirectory, "supabase-config.json");
            if (File.Exists(config))
            {
                var project = JsonSerializer.Deserialize<SocialProject>(
                    File.ReadAllText(config),
                    SocialApi.Json
                )!;
                SocialSession? session = null;
                try
                {
                    if (File.Exists(SocialSessionPath))
                        session = JsonSerializer.Deserialize<SocialSession>(
                            ProtectedData.Unprotect(
                                File.ReadAllBytes(SocialSessionPath),
                                null,
                                DataProtectionScope.CurrentUser
                            ),
                            SocialApi.Json
                        );
                }
                catch (Exception ex)
                    when (ex is IOException or CryptographicException or JsonException) { }
                AttachSocial(new(project, session: session));
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or ArgumentException)
        {
            socialStartupError = I18n.T("Amigos: ") + I18n.Error(ex);
        }
        FriendsView = new(this);
        FriendsHost.Child = FriendsView;
        publicationTimer.Tick += async (_, _) =>
        {
            publicationTimer.Stop();
            await PublishSocial();
        };
        socialTimer.Tick += async (_, _) =>
        {
            await PublishSocial();
            if (ShouldRefreshFriends && !FriendsView.Busy)
                await FriendsView.Reload();
        };
    }

    internal bool ShouldRefreshFriends =>
        friendsVisible && IsVisible && WindowState != WindowState.Minimized;

    // La sesión se cifra para el usuario de Windows y la cola se carga para la cuenta que inicia sesión.
    internal void AttachSocial(SocialApi api)
    {
        Social?.Dispose();
        Social = api;
        api.SessionChanged += session =>
        {
            if (session is null)
            {
                if (File.Exists(SocialSessionPath))
                    File.Delete(SocialSessionPath);
                Outbox = null;
            }
            else
            {
                var bytes = ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(JsonSerializer.Serialize(session, SocialApi.Json)),
                    null,
                    DataProtectionScope.CurrentUser
                );
                File.WriteAllBytes(SocialSessionPath + ".tmp", bytes);
                File.Move(SocialSessionPath + ".tmp", SocialSessionPath, true);
                if (Outbox?.UserId != session.UserId)
                {
                    Outbox = SocialOutbox.Load(
                        OutboxPath(session.UserId),
                        session.ProjectUrl,
                        session.UserId
                    );
                    MigrateVisibility();
                }
            }
        };
        if (api.Session is { } current)
        {
            Outbox = SocialOutbox.Load(
                OutboxPath(current.UserId),
                current.ProjectUrl,
                current.UserId
            );
            MigrateVisibility();
        }
    }

    private void MigrateVisibility()
    {
        if (Outbox is null)
            return;
        bool changed = false;
        foreach (var game in Games.Where(g => g.FriendsPrivate is null))
        {
            game.FriendsPrivate =
                Outbox.Games.TryGetValue(game.Id, out var entry) && !entry.Selected;
            changed = true;
        }
        if (changed)
            Store.Save(Games, Preferences);
    }

    private void StartSocial()
    {
        socialTimer.Start();
        if (socialStartupError is not null)
            Notice(socialStartupError);
        if (Social?.Session is not null)
        {
            SchedulePublications();
            _ = PublishSocial();
        }
    }

    internal void SaveOutbox()
    {
        if (Outbox is not null)
            Outbox.Save(OutboxPath(Outbox.UserId));
    }

    private (SharedGamePayload Payload, string? LocalCover) ProjectGame(Game game)
    {
        return (SharedGamePayload.From(game), null);
    }

    internal void SetShared(Game game, bool share)
    {
        if (Outbox is null)
            throw new InvalidOperationException(I18n.T("Entra en Checkpoint antes de compartir."));
        game.FriendsPrivate = !share;
        if (share)
            game.Tracked = true;
        Store.Save(Games, Preferences);
        if (share)
        {
            var projection = ProjectGame(game);
            Outbox.SetDesired(game.Id, projection.Payload, projection.LocalCover);
        }
        else
            Outbox.SetDesired(game.Id, null);
        SaveOutbox();
        SchedulePublications();
    }

    internal void SchedulePublications()
    {
        if (Outbox is null)
            return;
        try
        {
            MigrateVisibility();
            foreach (var game in Games)
            {
                if (GameLists.ShouldShare(game))
                {
                    var projection = ProjectGame(game);
                    Outbox.SetDesired(game.Id, projection.Payload, projection.LocalCover);
                }
                else if (Outbox.Games.ContainsKey(game.Id))
                    Outbox.SetDesired(game.Id, null);
            }
            foreach (var id in Outbox.Games.Keys.Where(id => Games.All(g => g.Id != id)).ToArray())
                Outbox.SetDesired(id, null);
            SaveOutbox();
            publicationTimer.Stop();
            publicationTimer.Start();
        }
        catch (Exception ex) when (ex is IOException or ArgumentException)
        {
            Notice(I18n.T("Publicación pendiente: ") + I18n.Error(ex));
        }
    }

    // Procesa la cola guardada y reconcilia revisiones sin sobrescribir automáticamente cambios de otro equipo.
    internal async Task PublishSocial()
    {
        if (
            socialSyncing
            || Social?.Session is null
            || Outbox is null
            || shutdown.IsCancellationRequested
        )
            return;
        socialSyncing = true;
        var api = Social;
        var box = Outbox;
        try
        {
            foreach (var id in box.Games.Keys.ToArray())
            {
                // Acknowledge a sent operation before preparing a subsequent edit/withdrawal.
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    if (Outbox != box || api.Session?.UserId != box.UserId)
                        return;
                    var operation = box.Prepare(id);
                    if (operation is null)
                        break;
                    SaveOutbox();
                    if (
                        operation.Payload is not null
                        && (
                            Games.FirstOrDefault(g => g.Id == id) is not { } localGame
                            || !GameLists.ShouldShare(localGame)
                        )
                    )
                    {
                        var remote = (
                            await api.Publications(box.UserId, shutdown.Token)
                        ).FirstOrDefault(g => g.GameId == id);
                        if (remote is not null)
                            box.Reconcile(id, remote);
                        else
                        {
                            var entry = box.Entry(id);
                            entry.Pending = null;
                            entry.Published = null;
                            entry.Revision = 0;
                            entry.Conflict = false;
                        }
                        SaveOutbox();
                        continue;
                    }
                    try
                    {
                        if (operation.Payload is not null && box.Entry(id).Desired is null)
                            continue;
                        long revision = await api.Publish(id, operation, shutdown.Token);
                        box.Acknowledge(id, revision);
                        SaveOutbox();
                    }
                    catch (SocialApiException ex) when (ex.IsConflict)
                    {
                        var remote = (
                            await api.Publications(box.UserId, shutdown.Token)
                        ).FirstOrDefault(g => g.GameId == id);
                        if (remote is null)
                            throw;
                        box.Reconcile(id, remote);
                        SaveOutbox();
                        if (box.Entry(id).Conflict)
                        {
                            Notice(
                                I18n.T(
                                    "Hay un conflicto de publicación. Revísalo en Amigos → Compartir."
                                )
                            );
                            break;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
            when (ex
                    is System.Net.Http.HttpRequestException
                        or IOException
                        or InvalidOperationException
                        or OperationCanceledException
            )
        {
            if (!shutdown.IsCancellationRequested)
                Notice("Checkpoint: " + I18n.Error(ex));
        }
        finally
        {
            socialSyncing = false;
        }
    }

    internal bool SocialSyncing => socialSyncing;

    private async void FriendsClick(object sender, RoutedEventArgs e)
    {
        if (!friendsVisible)
            FriendsView.OpenHome();
        friendsVisible = true;
        Refresh();
        await FriendsView.Reload();
    }

    private void ApplySocialTab()
    {
        FilterArea.Visibility =
            friendsVisible || Preferences.MiniatureView ? Visibility.Collapsed : Visibility.Visible;
        GameArea.Visibility = friendsVisible ? Visibility.Collapsed : Visibility.Visible;
        FriendsHost.Visibility = friendsVisible ? Visibility.Visible : Visibility.Collapsed;
        SummaryTitle.Text =
            friendsVisible ? I18n.T("Amigos")
            : allLibrary ? I18n.T("Biblioteca")
            : I18n.T("Mi lista");
        FriendsButton.Foreground = (System.Windows.Media.Brush)
            Application.Current.Resources[friendsVisible ? "AccentBrush" : "TextBrush"];
        if (friendsVisible)
            Summary.Text = I18n.T("Progreso compartido desde Checkpoint");
    }
}
