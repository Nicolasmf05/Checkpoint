// La ficha de Figma usa los mismos controles y guardados que el resto de Checkpoint.
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Checkpoint.Core;

namespace Checkpoint.App;

internal static partial class Dialogs
{
    private static void GameDetailsSheet(MainWindow owner, Game game, Window? parent)
    {
        var window = Modal(owner, I18n.T("Ficha del juego") + " · " + game.Title, 620, 760);
        if (parent is not null)
            Parent(window, parent);
        var body = Panel();
        body.Tag = "game-detail-sheet";
        var layout = Layout(window, body, out var footer);
        layout.Tag = "game-detail-page";
        var scroll = (ScrollViewer)layout.Children[0];
        TextBlock Text(string value, string? style = null, bool heading = false) =>
            new()
            {
                Text = value,
                Tag = style,
                TextWrapping = TextWrapping.Wrap,
                FontSize = heading ? 22 : 15,
            };
        StackPanel Group(string? style, params UIElement[] children)
        {
            var panel = new StackPanel { Tag = style };
            foreach (var child in children)
                panel.Children.Add(child);
            return panel;
        }
        Border Card(string style, params UIElement[] children) =>
            new() { Tag = style, Child = Group(null, children) };
        Grid Columns(string style, double first, double second)
        {
            var grid = new Grid { Tag = style };
            grid.ColumnDefinitions.Add(
                new ColumnDefinition { Width = new GridLength(first, GridUnitType.Star) }
            );
            grid.ColumnDefinitions.Add(
                new ColumnDefinition { Width = new GridLength(second, GridUnitType.Star) }
            );
            return grid;
        }
        string Date(DateTimeOffset? value) =>
            value
                ?.ToLocalTime()
                .ToString("g", CultureInfo.GetCultureInfo(I18n.IsEnglish ? "en-US" : "es-ES"))
            ?? I18n.T("Sin sincronizar");
        void Render()
        {
            body.Children.Clear();
            window.Title = I18n.T("Ficha del juego") + " · " + game.Title;
            var actions = new WrapPanel { Tag = "game-actions" };
            if (game.SteamAppId is > 0)
                actions.Children.Add(
                    Button(
                        I18n.T("Jugar"),
                        (_, _) => LaunchSteam(window, game.SteamAppId.Value),
                        true
                    )
                );
            actions.Children.Add(
                Button(
                    I18n.T("Ver logros"),
                    (_, _) =>
                    {
                        Achievements(owner, game, false, window);
                        Render();
                    }
                )
            );
            actions.Children.Add(
                Button(
                    I18n.T("Editar juego"),
                    (_, _) =>
                    {
                        Edit(owner, game);
                        if (owner.Games.FirstOrDefault(g => g.Id == game.Id) is { } current)
                        {
                            game = current;
                            Render();
                        }
                        else
                            window.Close();
                    }
                )
            );
            var intro = Group(
                "game-detail-intro",
                Text(game.Title, heading: true),
                actions,
                Text(game.Platform)
            );
            if (game.GoalVisible)
                intro.Children.Add(Text(I18n.T("Objetivo") + ": " + game.GoalText));
            body.Children.Add(intro);
            var main = Group("game-detail-main");
            body.Children.Add(main);
            var hero = Columns("game-detail-hero", 174, 253);
            main.Children.Add(hero);
            var coverSlot = Text(game.Platform, "game-detail-cover-empty");
            hero.Children.Add(coverSlot);
            if (!owner.Preferences.LightweightMode)
            {
                var image = new Image
                {
                    Tag = "cover-preview",
                    Height = 263,
                    Visibility = Visibility.Collapsed,
                };
                hero.Children.Add(image);
                async System.Threading.Tasks.Task LoadCover()
                {
                    try
                    {
                        image.Source = await owner.Covers.Get(game);
                        if (image.Source is not null)
                        {
                            image.Visibility = Visibility.Visible;
                            coverSlot.Visibility = Visibility.Collapsed;
                        }
                    }
                    catch (Exception error) when (error is not OutOfMemoryException) { }
                }
                _ = LoadCover();
            }
            var stats = Columns("game-detail-stats", 1, 1);
            stats.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            stats.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetColumn(stats, 1);
            hero.Children.Add(stats);
            stats.Children.Add(
                Card(
                    "game-playtime",
                    Text(
                        (game.PlaytimeMinutes / 60d).ToString(
                            "0.#",
                            CultureInfo.GetCultureInfo(I18n.IsEnglish ? "en-US" : "es-ES")
                        ) + " h",
                        heading: true
                    ),
                    Text(I18n.T("Tiempo jugado"))
                )
            );
            var visibility = Card(
                "game-detail-visibility",
                Text(I18n.T(game.FriendsPrivate == true ? "Privado" : "Visible"), heading: true),
                Text(I18n.T("Solo amigos"))
            );
            Grid.SetColumn(visibility, 1);
            stats.Children.Add(visibility);
            var achievements = AchievementTracking.Items(game).ToArray();
            int completed = achievements.Count(a => a.Completed);
            int? percent =
                achievements.Length > 0
                    ? (int)
                        Math.Round(
                            completed * 100d / achievements.Length,
                            MidpointRounding.AwayFromZero
                        )
                    : null;
            var progressLine = new WrapPanel { Tag = "game-detail-progress-line" };
            progressLine.Children.Add(
                new ProgressBar
                {
                    Minimum = 0,
                    Maximum = 100,
                    Value = percent ?? 0,
                }
            );
            progressLine.Children.Add(
                Text(
                    completed + " / " + achievements.Length + " " + I18n.T("logros"),
                    "game-detail-count"
                )
            );
            var progress = Card(
                "game-detail-progress",
                Text(percent is int value ? value + "%" : "—", "game-detail-percent", true),
                progressLine,
                Text(I18n.T("Estado") + ": " + game.StatusText)
            );
            if (game.StoryPercent is int story)
                ((StackPanel)progress.Child).Children.Add(
                    Text(I18n.T("Historia") + ": " + story + "%")
                );
            Grid.SetRow(progress, 1);
            Grid.SetColumnSpan(progress, 2);
            stats.Children.Add(progress);
            var services = Columns("game-detail-services", 254, 170);
            main.Children.Add(services);
            var providers = Group(
                "game-detail-providers",
                Text(
                    I18n.T(owner.Steam.Session is null ? "Desvinculado" : "Vinculado"),
                    "game-detail-steam"
                ),
                Text(
                    I18n.T(owner.Retro.Session is null ? "Desvinculado" : "Vinculado"),
                    "game-detail-retro"
                )
            );
            var syncBody = new WrapPanel { Tag = "game-detail-sync-body" };
            syncBody.Children.Add(providers);
            bool canSync = owner.CanReviewAchievements(game);
            var connect = Button(
                I18n.T(canSync ? "Actualizar" : "Conectar"),
                async (_, _) =>
                {
                    if (canSync)
                        await owner.RefreshGameAchievements(game);
                    else if (game.RetroGameId is > 0 && game.SteamAppId is not > 0)
                        RetroSettings(owner, window);
                    else
                        await owner.ConnectSteam();
                    Render();
                }
            );
            connect.IsEnabled =
                !owner.AchievementSyncBusy
                && (
                    canSync
                    || owner.Steam.Session is null
                    || game.RetroGameId is > 0 && owner.Retro.Session is null
                );
            syncBody.Children.Add(connect);
            var sync = Card(
                "game-detail-sync",
                Text(I18n.T("Sincronización"), "game-detail-panel-heading", true),
                syncBody
            );
            var syncContent = (StackPanel)sync.Child;
            if (game.Achievements is not null)
                syncContent.Children.Add(
                    Text("Steam: " + game.UnlockedCount + " / " + game.Achievements.Count)
                );
            if (game.RetroAchievements is not null)
                syncContent.Children.Add(
                    Text(
                        "RetroAchievements: "
                            + game.RetroAchievements.Count(a => a.Unlocked)
                            + " / "
                            + game.RetroAchievements.Count
                    )
                );
            if (game.ManualAchievements.Count > 0)
                syncContent.Children.Add(
                    Text(I18n.T("Objetivos manuales") + ": " + game.ManualAchievements.Count)
                );
            services.Children.Add(sync);
            var listItems = Group("game-detail-list-items");
            foreach (string name in game.Lists)
                listItems.Children.Add(
                    Button("· " + name, (_, _) => ListDetails(owner, "custom:" + name))
                );
            if (game.Lists.Count == 0)
                listItems.Children.Add(Text(I18n.T("Sin listas adicionales")));
            var lists = Card(
                "game-detail-lists",
                Text(I18n.T("Listas"), "game-detail-panel-heading", true),
                listItems
            );
            Grid.SetColumn(lists, 1);
            services.Children.Add(lists);
            main.Children.Add(Text(I18n.T("Tareas"), "game-detail-section-title", true));
            if (game.Tasks.Count == 0)
                main.Children.Add(Text(I18n.T("Sin tareas")));
            foreach (var task in game.Tasks)
            {
                var check = new CheckBox { Content = task.Title, IsChecked = task.Done };
                check.Click += (_, _) =>
                {
                    task.Done = check.IsChecked == true;
                    owner.Persist();
                    owner.Refresh();
                };
                main.Children.Add(Card("game-detail-task", check));
            }
            main.Children.Add(Text(I18n.T("Notas"), "game-detail-section-title", true));
            var notes = new TextBox
            {
                Text = game.Notes,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MaxLength = 20000,
                Tag = "game-detail-notes",
                ToolTip = I18n.T("Las notas se guardan automáticamente."),
            };
            System.Windows.Automation.AutomationProperties.SetName(notes, I18n.T("Notas"));
            notes.TextChanged += (_, _) =>
            {
                game.Notes = notes.Text;
                owner.Persist();
            };
            main.Children.Add(notes);
            var top = Button(I18n.T("Volver arriba"), (_, _) => scroll.ScrollToTop());
            top.Tag = "game-detail-top";
            main.Children.Add(top);
            var metadata = Group("game-detail-metadata");
            void Detail(string title, string value, string style) =>
                metadata.Children.Add(Group(style, Text(title + ":"), Text(value)));
            Detail(I18n.T("Añadido"), Date(game.AddedAt), "game-detail-added");
            Detail(
                I18n.T("Historia terminada"),
                game.FinishedAt is { } finished ? Date(finished) : I18n.T("Sin completar"),
                "game-detail-finished"
            );
            if (game.SteamAppId is { } steam)
                Detail("Steam ID", steam.ToString(), "game-detail-steam-id");
            Detail(I18n.T("Última sincronización"), Date(game.SyncedAt), "game-detail-synced");
            if (game.RetroGameId is { } retro)
                Detail("RetroAchievements ID", retro.ToString(), "game-detail-retro-id");
            body.Children.Add(
                Group(
                    "game-detail-additional",
                    Group(
                        "game-detail-additional-title",
                        I18n.T("Información adicional")
                            .Split(' ')
                            .Select(part => (UIElement)Text(part, heading: true))
                            .ToArray()
                    ),
                    metadata
                )
            );
        }
        Render();
        footer.Children.Add(Button(I18n.T("Cerrar"), (_, _) => window.Close()));
        ShowPage(window);
    }
}
