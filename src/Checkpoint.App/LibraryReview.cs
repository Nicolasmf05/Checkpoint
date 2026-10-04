// Coordina el repaso de logros de la biblioteca y muestra progreso y cancelación.
// Los resultados remotos se aplican solo a juegos que siguen presentes en la colección.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Checkpoint.Core;

namespace Checkpoint.App;

internal static partial class Dialogs
{
    internal static void ReviewAchievements(MainWindow owner, Window parent)
    {
        var window = Modal(owner, I18n.T("Repasar todos los logros"), 600, 620);
        Parent(window, parent);
        var body = Panel();
        Layout(window, body, out var footer);
        Heading(
            body,
            I18n.T("Repasar todos los logros"),
            I18n.T(
                "Incluye toda la Biblioteca, también los juegos privados y los que no están en Mi lista. Los cambios manuales se conservan."
            )
        );
        var ids = owner
            .Games.OrderBy(g => g.Title, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Id)
            .ToArray();
        int index = 0;
        var position = new TextBlock();
        var title = new TextBlock { FontSize = 22, TextWrapping = TextWrapping.Wrap };
        var summary = new TextBlock { TextWrapping = TextWrapping.Wrap };
        var message = new TextBlock { TextWrapping = TextWrapping.Wrap };
        body.Children.Add(position);
        body.Children.Add(title);
        body.Children.Add(summary);
        body.Children.Add(message);
        Game? Current() =>
            index < ids.Length ? owner.Games.FirstOrDefault(g => g.Id == ids[index]) : null;
        Button view = null!,
            update = null!,
            previous = null!,
            next = null!;
        view = Button(
            I18n.T("Ver logros"),
            (_, _) =>
            {
                if (Current() is { } game)
                    Achievements(owner, game, false, window);
                Reload();
            }
        );
        body.Children.Add(view);
        body.Children.Add(
            new TextBlock
            {
                Text = I18n.T(
                    "La actualización continúa en segundo plano. Puedes usar Checkpoint y detenerla desde el indicador de progreso."
                ),
                TextWrapping = TextWrapping.Wrap,
            }
        );
        update = Button(
            I18n.T("Actualizar todos los logros"),
            (_, _) =>
            {
                if (owner.AchievementSyncBusy)
                    return;
                owner.StartAchievementReview();
                window.Close();
                parent.Close();
            }
        );
        body.Children.Add(update);
        previous = Button(
            I18n.T("Anterior"),
            (_, _) =>
            {
                index--;
                Reload();
            }
        );
        next = Button(
            I18n.T("Siguiente juego"),
            (_, _) =>
            {
                index++;
                Reload();
            }
        );
        footer.Children.Add(previous);
        footer.Children.Add(next);
        footer.Children.Add(Button(I18n.T("Cerrar"), (_, _) => window.Close()));
        void Reload()
        {
            index = Math.Clamp(index, 0, Math.Max(0, ids.Length - 1));
            var game = Current();
            var items = game is null ? [] : AchievementTracking.Items(game).ToArray();
            position.Text =
                I18n.T("Juego") + " " + (ids.Length == 0 ? 0 : index + 1) + " / " + ids.Length;
            title.Text = game?.Title ?? I18n.T("Sin juegos en Biblioteca");
            summary.Text =
                I18n.T("Logros")
                + ": "
                + items.Count(a => a.Completed)
                + " / "
                + items.Length
                + " · "
                + I18n.T("Pendientes")
                + ": "
                + items.Count(a => !a.Completed)
                + (
                    game is not null
                    && (
                        game.SteamAppId.HasValue && game.Achievements is null
                        || game.RetroGameId.HasValue && game.RetroAchievements is null
                    )
                        ? " · " + I18n.T("Sin sincronizar")
                        : ""
                );
            view.IsEnabled = game is not null;
            previous.IsEnabled = index > 0;
            next.IsEnabled = index + 1 < ids.Length;
            message.Text = owner.AchievementReviewText;
            update.IsEnabled =
                !owner.AchievementSyncBusy && owner.Games.Any(owner.CanReviewAchievements);
        }
        Reload();
        ShowPage(window);
    }

    internal static void ReviewCovers(MainWindow owner, Window parent)
    {
        var window = Modal(owner, I18n.T("Buscar carátulas que faltan"), 620, 730);
        Parent(window, parent);
        var body = Panel();
        Layout(window, body, out var footer);
        Heading(
            body,
            I18n.T("Buscar carátulas que faltan"),
            I18n.T(
                "Se comprueba Steam antes de buscar en IGDB. Siguiente carátula descarta la propuesta; Siguiente juego continúa sin añadirla. Las decisiones se guardan al instante."
            )
        );
        var ids = owner
            .Games.OrderBy(g => g.Title, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Id)
            .ToArray();
        int index = -1;
        bool busy = false,
            closed = false;
        var position = new TextBlock();
        var title = new TextBlock { FontSize = 22, TextWrapping = TextWrapping.Wrap };
        var image = new Image { Height = 240, Tag = "cover-preview" };
        var match = new TextBlock { TextWrapping = TextWrapping.Wrap };
        var message = new TextBlock { TextWrapping = TextWrapping.Wrap };
        foreach (var node in new UIElement[] { position, title, image, match, message })
            body.Children.Add(node);
        IgdbCover? candidate = null;
        byte[]? prepared = null;
        using var cancellation = new CancellationTokenSource();
        Game? Current() =>
            index >= 0 && index < ids.Length
                ? owner.Games.FirstOrDefault(g => g.Id == ids[index])
                : null;
        void SaveDecision(bool accept)
        {
            if (Current() is not { } game)
                return;
            game.IgdbCoverSearchTitle = game.Title;
            if (candidate is not null)
            {
                if (accept && prepared is not null)
                {
                    game.CustomCover = owner.Covers.SavePrepared(prepared);
                    game.IgdbCoverImageId = candidate.ImageId;
                }
                else
                    CoverSuggestions.Reject(game, candidate.ImageId);
            }
            owner.Persist();
            owner.Refresh();
        }
        Button accept = null!,
            another = null!,
            next = null!;
        accept = Button(
            I18n.T("Aceptar"),
            async (_, _) =>
            {
                if (busy || candidate is null)
                    return;
                try
                {
                    SaveDecision(true);
                    await Load(true);
                }
                catch (Exception error)
                {
                    message.Text = I18n.Error(error);
                }
            }
        );
        another = Button(
            I18n.T("Siguiente carátula"),
            async (_, _) =>
            {
                if (busy)
                    return;
                try
                {
                    SaveDecision(false);
                    await Load(false);
                }
                catch (Exception error)
                {
                    message.Text = I18n.Error(error);
                }
            }
        );
        next = Button(
            I18n.T("Siguiente juego"),
            async (_, _) =>
            {
                if (busy)
                    return;
                try
                {
                    SaveDecision(false);
                    await Load(true);
                }
                catch (Exception error)
                {
                    message.Text = I18n.Error(error);
                }
            }
        );
        // Keep the three review choices inside the scroll area so small windows do not clip them.
        var choices = new WrapPanel();
        choices.Children.Add(accept);
        choices.Children.Add(another);
        choices.Children.Add(next);
        body.Children.Add(choices);
        footer.Children.Add(Button(I18n.T("Cerrar"), (_, _) => window.Close()));
        void Buttons()
        {
            accept.IsEnabled = !busy && candidate is not null;
            another.IsEnabled = !busy && Current() is not null;
            next.IsEnabled = !busy && Current() is not null;
        }
        async Task Load(bool advance)
        {
            if (closed || busy)
                return;
            busy = true;
            candidate = null;
            prepared = null;
            image.Source = null;
            match.Text = "";
            message.Text = I18n.T("Buscando carátula…");
            Buttons();
            try
            {
                if (advance)
                {
                    while (++index < ids.Length)
                    {
                        cancellation.Token.ThrowIfCancellationRequested();
                        var item = Current();
                        if (item is null)
                            continue;
                        title.Text = item.Title;
                        position.Text = I18n.T("Juego") + " " + (index + 1) + " / " + ids.Length;
                        if (await owner.Covers.Get(item, true) is null)
                            break;
                        if (closed)
                            return;
                    }
                }
                if (closed)
                    return;
                if (Current() is not { } game)
                {
                    title.Text = I18n.T("Revisión terminada");
                    position.Text = I18n.T("Juegos revisados:") + " " + ids.Length;
                    message.Text = I18n.T("No quedan juegos sin carátula por revisar.");
                    return;
                }
                if (owner.Igdb is null)
                    throw new InvalidOperationException(
                        I18n.T("El responsable de esta edición debe configurar IGDB en Supabase.")
                    );
                if (game.RejectedIgdbCovers.Count >= 200)
                    throw new InvalidOperationException(
                        I18n.T(
                            "Has rechazado 200 carátulas para este juego. No se harán más búsquedas."
                        )
                    );
                var found = await owner.Igdb.Search(
                    game.Title,
                    game.RejectedIgdbCovers,
                    cancellation.Token,
                    true
                );
                if (found is null)
                {
                    message.Text = I18n.T(
                        "No hay otra carátula de IGDB disponible para este nombre."
                    );
                    return;
                }
                var bytes = CoverCache.PrepareRemote(
                    await owner.Igdb.Image(found.ImageId, cancellation.Token)
                );
                if (closed)
                    return;
                candidate = found;
                prepared = bytes;
                image.Source = CoverCache.ReadPrepared(bytes);
                match.Text = found.Name + (found.Year is { } year ? " · " + year : "");
                message.Text = I18n.T(
                    "Coincidencia más cercana por nombre. Comprueba que sea tu juego. Carátula: IGDB."
                );
            }
            catch (OperationCanceledException) { }
            catch (Exception error)
            {
                if (!closed)
                    message.Text = I18n.Error(error);
            }
            finally
            {
                busy = false;
                if (!closed)
                    Buttons();
            }
        }
        window.Loaded += async (_, _) => await Load(true);
        window.Closed += (_, _) =>
        {
            closed = true;
            cancellation.Cancel();
        };
        Buttons();
        ShowPage(window);
    }
}

public partial class MainWindow
{
    internal bool AchievementSyncBusy => syncing;

    internal bool CanReviewAchievements(Game game) =>
        (game.SteamAppId.HasValue && Steam.Session is not null)
        || (game.RetroGameId.HasValue && Retro.Session is not null);

    private CancellationTokenSource? achievementReviewCancellation;
    private int achievementReviewDone,
        achievementReviewTotal,
        achievementReviewErrors;
    private bool achievementReviewVisible,
        achievementReviewStopped;
    private string achievementReviewError = "";
    internal string AchievementReviewText =>
        !achievementReviewVisible
            ? ""
            : I18n.T(
                achievementReviewCancellation is not null ? "Actualizando logros en segundo plano:"
                : achievementReviewStopped ? "Repaso de logros detenido:"
                : "Repaso de logros terminado:"
            )
                + " "
                + achievementReviewDone
                + " / "
                + achievementReviewTotal
                + " · "
                + I18n.T("No se pudieron actualizar:")
                + " "
                + achievementReviewErrors
                + (achievementReviewError.Length > 0 ? " · " + achievementReviewError : "");

    internal void StopAchievementReview()
    {
        if (achievementReviewCancellation is not null)
            achievementReviewCancellation.Cancel();
        else
        {
            achievementReviewVisible = false;
            Refresh();
        }
    }

    internal void StartAchievementReview()
    {
        if (syncing)
            return;
        var selected = Games.Where(CanReviewAchievements).ToArray();
        if (selected.Length == 0)
            return;
        achievementReviewCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            shutdown.Token
        );
        achievementReviewDone = 0;
        achievementReviewErrors = 0;
        achievementReviewError = "";
        achievementReviewTotal = selected.Length;
        achievementReviewVisible = true;
        achievementReviewStopped = false;
        _ = RunAchievementReview(selected, achievementReviewCancellation);
    }

    private async Task RunAchievementReview(Game[] selected, CancellationTokenSource cancellation)
    {
        syncing = true;
        Refresh();
        var steamSession = Steam.Session;
        var retroSession = Retro.Session;
        try
        {
            await AchievementReviewQueue.Run(
                selected,
                async (game, token) =>
                {
                    bool failed = false;
                    if (
                        steamSession is not null && Steam.Session != steamSession
                        || retroSession is not null && Retro.Session != retroSession
                    )
                    {
                        cancellation.Cancel();
                        token.ThrowIfCancellationRequested();
                    }
                    var current = Games.FirstOrDefault(g => g.Id == game.Id);
                    if (current is null)
                    {
                        achievementReviewDone++;
                        Refresh();
                        return;
                    }
                    var steamId = current.SteamAppId;
                    var retroId = current.RetroGameId;
                    if (
                        steamId is int appId
                        && steamSession is not null
                        && Steam.Session == steamSession
                    )
                    {
                        try
                        {
                            var result = await Steam.Achievements(
                                Preferences.ServiceUrl,
                                appId,
                                token
                            );
                            token.ThrowIfCancellationRequested();
                            var target = Games.FirstOrDefault(g => g.Id == game.Id);
                            if (
                                Steam.Session == steamSession
                                && target is not null
                                && target.SteamAppId == steamId
                            )
                            {
                                target.Achievements = result.Achievements.ToList();
                                target.SyncedAt = DateTimeOffset.UtcNow;
                                Store.SaveExistingGame(target);
                            }
                        }
                        catch (OperationCanceledException error)
                            when (!token.IsCancellationRequested)
                        {
                            failed = true;
                            RecordError(current, error);
                        }
                        catch (Exception error)
                            when (error
                                    is not OutOfMemoryException
                                        and not OperationCanceledException
                            )
                        {
                            failed = true;
                            RecordError(current, error);
                        }
                    }
                    if (
                        retroId is int id
                        && retroSession is not null
                        && Retro.Session == retroSession
                    )
                    {
                        try
                        {
                            var result = await Retro.Achievements(id, token);
                            token.ThrowIfCancellationRequested();
                            GameRules.Validate(
                                new Game { Title = game.Title, RetroAchievements = result }
                            );
                            var target = Games.FirstOrDefault(g => g.Id == game.Id);
                            if (
                                Retro.Session == retroSession
                                && target is not null
                                && target.RetroGameId == retroId
                            )
                            {
                                target.RetroAchievements = result;
                                Store.SaveExistingGame(target);
                            }
                        }
                        catch (OperationCanceledException error)
                            when (!token.IsCancellationRequested)
                        {
                            failed = true;
                            RecordError(current, error);
                        }
                        catch (Exception error)
                            when (error
                                    is not OutOfMemoryException
                                        and not OperationCanceledException
                            )
                        {
                            failed = true;
                            RecordError(current, error);
                        }
                    }
                    if (failed)
                        achievementReviewErrors++;
                    achievementReviewDone++;
                    if (achievementReviewDone % 10 == 0)
                        SchedulePublications();
                    Refresh();
                },
                cancellation.Token
            );
        }
        catch (OperationCanceledException)
        {
            achievementReviewStopped = true;
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            achievementReviewStopped = true;
            if (!shutdown.IsCancellationRequested)
                Notice(I18n.Error(error));
        }
        finally
        {
            achievementReviewCancellation = null;
            cancellation.Dispose();
            syncing = false;
            if (!shutdown.IsCancellationRequested)
            {
                SchedulePublications();
                Refresh();
            }
        }
    }

    private void RecordError(Game game, Exception error)
    {
        if (achievementReviewError.Length == 0)
            achievementReviewError = game.Title + ": " + I18n.Error(error);
    }
}
