using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Checkpoint.Core;
using Microsoft.Win32;

namespace Checkpoint.App;

internal static partial class Dialogs
{
    private static Window Modal(MainWindow owner, string title, double width = 540, double height = 710)
    {
        var window = new Window { Owner = owner, Title = title, Width = width, Height = height,
            MinWidth = 400, MinHeight = 420, WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInTaskbar = false, FontFamily = new FontFamily("Segoe UI"), FontSize = 13,
            Background = (Brush)Application.Current.Resources["InputBrush"], Foreground = (Brush)Application.Current.Resources["TextBrush"] };
        window.Height = Math.Min(height, SystemParameters.WorkArea.Height - 35);
        if (App.UseCss) window.Loaded += (_, _) => WebSurface.AttachDialog(window);
        return window;
    }
    private static StackPanel Panel() => new() { Margin = new Thickness(23, 18, 23, 18) };
    private static void Heading(Panel panel, string text, string? subtitle = null)
    {
        panel.Children.Add(new TextBlock { Text = text, FontSize = 22, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 7) });
        if (subtitle is not null) panel.Children.Add(new TextBlock { Text = subtitle, TextWrapping = TextWrapping.Wrap, Foreground = (Brush)Application.Current.Resources["MutedBrush"], Margin = new Thickness(0, 0, 0, 12) });
    }
    private static void Label(Panel panel, string text)
    {
        panel.Children.Add(new TextBlock { Text = text, FontSize = 11, FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)Application.Current.Resources["MutedBrush"], Margin = new Thickness(0, 15, 0, 6) });
    }
    private static TextBox Input(Panel panel, string label, string value, bool multiline = false)
    {
        Label(panel, label); var box = new TextBox { Text = value, AcceptsReturn = multiline,
            MinHeight = multiline ? 85 : 34, TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
            VerticalScrollBarVisibility = multiline ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled };
        System.Windows.Automation.AutomationProperties.SetName(box, label); panel.Children.Add(box); return box;
    }
    private static CheckBox Check(Panel panel, string title, bool value)
    {
        var check = new CheckBox { Content = title, IsChecked = value }; panel.Children.Add(check); return check;
    }
    private static Button Button(string text, RoutedEventHandler click, bool accent = false)
    {
        var button = new Button { Content = text, Margin = new Thickness(0, 0, 7, 0) };
        if (accent) button.Style = (Style)Application.Current.Resources["AccentButton"];
        button.Click += click; return button;
    }
    private static Grid Layout(Window window, StackPanel body, out StackPanel footer)
    {
        var grid = new Grid(); grid.RowDefinitions.Add(new RowDefinition()); grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var scroll = new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        grid.Children.Add(scroll); footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(20, 13, 17, 17) };
        Grid.SetRow(footer, 1); grid.Children.Add(footer); window.Content = grid; return grid;
    }

    internal static void Edit(MainWindow owner, Game? original)
    {
        bool creating = original is null;
        var game = original is null ? new Game { SortOrder = owner.Games.Count, FriendsPrivate=owner.Preferences.NewGamesPrivate||owner.Preferences.ActiveList=="private", Lists=owner.Preferences.ActiveList.StartsWith("custom:")?[owner.Preferences.ActiveList[7..]]:[] } : JsonSerializer.Deserialize<Game>(JsonSerializer.Serialize(original, DataJson.Options), DataJson.Options)!;
        var window = Modal(owner, creating ? I18n.T("Añadir juego · Checkpoint") : game.Title + " · Checkpoint");
        var body = Panel(); Layout(window, body, out var footer);
        Heading(body, creating ? I18n.T("Añadir juego") : game.Title, I18n.T("La historia y los logros se guardan como objetivos independientes."));
        var title = Input(body, I18n.T("Nombre del juego"), game.Title); title.MaxLength = 140;
        var steamId = Input(body, I18n.T("ID del juego en Steam (opcional)"), game.SteamAppId?.ToString() ?? "");
        var retroId = Input(body, I18n.T("ID del juego en RetroAchievements (opcional)"), game.RetroGameId?.ToString() ?? "");
        var detectionProcess = Input(body, I18n.T("Ejecutable para detectar (opcional, por ejemplo retroarch.exe)"), game.DetectionProcess);
        var detectionTitle = Input(body, I18n.T("Texto del título de ventana (opcional, para distinguir juegos del emulador)"), game.DetectionWindowTitle);
        body.Children.Add(new TextBlock { Text = I18n.T("Steam se detecta por su carpeta de instalación. Para emuladores, indica el ejecutable y un texto del título específico del juego."), TextWrapping = TextWrapping.Wrap });
        var platform = Input(body, I18n.T("Plataforma"), game.Platform); platform.MaxLength = 60;
        Label(body, I18n.T("Estado")); var state = new ComboBox { ItemsSource = Enum.GetValues<GameStatus>().Select(Labels.Status).ToList(), SelectedIndex = (int)game.Status }; body.Children.Add(state);
        Label(body, I18n.T("Objetivo")); var goal = new ComboBox { ItemsSource = Enum.GetValues<GameGoal>().Select(Labels.Goal).ToList(), SelectedIndex = (int)game.Goal }; body.Children.Add(goal);
        var customGoal = Input(body, I18n.T("Objetivo personalizado"), game.CustomGoal); customGoal.MaxLength = 500;
        var storyPercent = Input(body, I18n.T("Historia completada (0–100 %, opcional y manual)"), game.StoryPercent?.ToString() ?? "");
        storyPercent.MaxLength = 3;
        var tracked = Check(body, I18n.T("Mostrar en Mi lista"), game.Tracked);
        var friendsPrivate=Check(body,I18n.T("Privado para mis amigos"),game.FriendsPrivate==true);
        body.Children.Add(new TextBlock{Text=I18n.T("Los juegos de tus listas son visibles para tus amigos salvo que los marques privados. Notas y nombres de tareas siguen siendo privados."),TextWrapping=TextWrapping.Wrap});
        body.Children.Add(new TextBlock{Text=I18n.T("Sin conexión, los cambios de visibilidad se aplican a tus amigos cuando vuelva la conexión."),TextWrapping=TextWrapping.Wrap});
        var listChecks=new Dictionary<string,CheckBox>();
        foreach(var name in owner.Preferences.GameLists)listChecks[name]=Check(body,I18n.T("Lista: ")+name,game.Lists.Contains(name,StringComparer.OrdinalIgnoreCase));
        var favorite = Check(body, I18n.T("Destacar como favorito"), game.Favorite);
        var priority = Input(body, I18n.T("Orden en la lista (los números menores aparecen antes)"), game.SortOrder.ToString());
        var notes = Input(body, I18n.T("Notas · dónde lo dejaste"), game.Notes, true); notes.MaxLength = 20000;
        Label(body, I18n.T("Tareas")); var tasks = new StackPanel(); body.Children.Add(tasks);
        void RenderTasks()
        {
            tasks.Children.Clear();
            foreach (var item in game.Tasks)
            {
                var row = new DockPanel(); var remove = Button("×", (_, _) => { game.Tasks.Remove(item); RenderTasks(); });
                remove.Padding = new Thickness(7, 2, 7, 2); remove.ToolTip = I18n.T("Quitar tarea"); DockPanel.SetDock(remove, Dock.Right); row.Children.Add(remove);
                var checkbox = new CheckBox { Content = item.Title, IsChecked = item.Done, VerticalAlignment = VerticalAlignment.Center };
                checkbox.Checked += (_, _) => item.Done = true; checkbox.Unchecked += (_, _) => item.Done = false; row.Children.Add(checkbox); tasks.Children.Add(row);
            }
        }
        RenderTasks(); var nextTask = new TextBox { MaxLength = 500, Margin = new Thickness(0, 8, 0, 6) }; body.Children.Add(nextTask);
        System.Windows.Automation.AutomationProperties.SetName(nextTask, I18n.T("Nueva tarea"));
        void AddTask() { if (!string.IsNullOrWhiteSpace(nextTask.Text) && game.Tasks.Count < 200) { game.Tasks.Add(new() { Title = nextTask.Text.Trim() }); nextTask.Clear(); RenderTasks(); } }
        body.Children.Add(Button(I18n.T("+ Añadir tarea"), (_, _) => AddTask()));
        nextTask.KeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.Enter) { AddTask(); e.Handled = true; } };
        Label(body, I18n.T("Carátula"));
        var coverNotice = new TextBlock { Text = game.CustomCover is null ? I18n.T("Automática para juegos de Steam; puedes elegir una imagen propia.") : I18n.T("Carátula personalizada guardada."), FontSize = 11, TextWrapping = TextWrapping.Wrap }; body.Children.Add(coverNotice);
        var imageButtons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) }; body.Children.Add(imageButtons);
        imageButtons.Children.Add(Button(I18n.T("Elegir imagen"), (_, _) =>
        {
            var picker = new OpenFileDialog { Filter = I18n.T("Imágenes|*.png;*.jpg;*.jpeg;*.webp;*.bmp"), Title = I18n.T("Elegir carátula") };
            if (picker.ShowDialog(window) == true)
            {
                try { game.CustomCover = owner.Covers.Import(picker.FileName);game.IgdbCoverImageId=""; coverNotice.Text = I18n.T("Carátula personalizada preparada."); }
                catch (Exception ex) { LocalizedNotice.Show(window, I18n.T("No se pudo leer la imagen: ") + I18n.Error(ex), "Checkpoint"); }
            }
        }));
        var igdbButton=Button(I18n.T("Buscar otra carátula en IGDB"),async(sender,_)=>
        {
            var button=(Button)sender;button.IsEnabled=false;
            try{await owner.FindIgdbCover(game,window,title.Text);coverNotice.Text=game.CustomCover is null?I18n.T("Se conserva la carátula anterior."):I18n.T("Carátula personalizada preparada.");}
            finally{button.IsEnabled=true;}
        });imageButtons.Children.Add(igdbButton);
        imageButtons.Children.Add(Button(I18n.T("Automática"), (_, _) => { game.CustomCover = null;game.IgdbCoverImageId=""; coverNotice.Text = I18n.T("Se usará la carátula de Steam, si está disponible."); }));
        if (!creating)
        {
            Label(body, "Steam");
            body.Children.Add(new TextBlock { Text = game.SyncedAt is { } date ? I18n.T("Última consulta: ") + date.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : I18n.T("Este juego aún no se ha sincronizado."), FontSize = 11 });
            var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) }; body.Children.Add(actions);
            actions.Children.Add(Button(I18n.T("Ver logros"), (_, _) => Achievements(owner, original!)));
            if (game.SteamAppId is int appId)
            {
                actions.Children.Add(Button(I18n.T("Abrir en Steam"), (_, _) => Process.Start(new ProcessStartInfo($"https://store.steampowered.com/app/{appId}/") { UseShellExecute = true })));
                actions.Children.Add(Button(I18n.T("Jugar"), (_, _) => Process.Start(new ProcessStartInfo($"steam://rungameid/{appId}") { UseShellExecute = true })));
            }
            Label(body, I18n.T("Biblioteca"));
            body.Children.Add(Button(I18n.T("Eliminar juego"), (_, _) =>
            {
                try { owner.DeleteGame(original!); window.Close(); }
                catch (Exception ex) { LocalizedNotice.Show(window, I18n.Error(ex), I18n.T("No se pudo eliminar el juego")); }
            }));
        }
        footer.Children.Add(Button(I18n.T("Cancelar"), (_, _) => window.Close()));
        footer.Children.Add(Button(I18n.T("Guardar"), (_, _) =>
        {
            try
            {
                game.Title = title.Text; game.Platform = platform.Text; game.CustomGoal = customGoal.Text; game.Notes = notes.Text;
                if (string.IsNullOrWhiteSpace(storyPercent.Text)) game.StoryPercent = null;
                else if (int.TryParse(storyPercent.Text,out int percent) && percent is >= 0 and <= 100) game.StoryPercent = percent;
                else throw new ArgumentException(I18n.T("La historia completada debe estar entre 0 y 100, o dejarse vacía."));
                if (string.IsNullOrWhiteSpace(steamId.Text)) game.SteamAppId = null;
                else if (int.TryParse(steamId.Text.Trim(), out int id) && id > 0) game.SteamAppId = id;
                else throw new ArgumentException(I18n.T("El ID de Steam debe ser un número positivo."));
                if (string.IsNullOrWhiteSpace(retroId.Text)) game.RetroGameId = null;
                else if (int.TryParse(retroId.Text.Trim(), out int retroNumber) && retroNumber > 0) game.RetroGameId = retroNumber;
                else throw new ArgumentException(I18n.T("El ID de RetroAchievements debe ser un número positivo."));
                game.DetectionProcess = detectionProcess.Text.Trim(); game.DetectionWindowTitle = detectionTitle.Text.Trim();
                if (owner.Games.Any(g => g.Id != game.Id && g.SteamAppId is not null && g.SteamAppId == game.SteamAppId)) throw new ArgumentException(I18n.T("Ese juego de Steam ya está en la biblioteca."));
                if (!int.TryParse(priority.Text, out int order)) throw new ArgumentException(I18n.T("El orden debe ser un número entero."));
                game.FriendsPrivate=friendsPrivate.IsChecked==true; game.Lists=listChecks.Where(p=>p.Value.IsChecked==true).Select(p=>p.Key).ToList();
                game.SortOrder = order; game.Goal = (GameGoal)goal.SelectedIndex; game.Tracked = tracked.IsChecked == true; game.Favorite = favorite.IsChecked == true;
                GameRules.SetStatus(game, (GameStatus)state.SelectedIndex); GameRules.Validate(game);
                if (original?.SteamAppId != game.SteamAppId) { game.Achievements = null; game.SyncedAt = null; }
                else if (original is not null)
                {
                    // A background refresh may have completed while this editor was open.
                    game.Achievements = original.Achievements; game.SyncedAt = original.SyncedAt;
                    game.PlaytimeMinutes = original.PlaytimeMinutes;
                }
                if (original is not null)
                {
                    game.ManualAchievements = original.ManualAchievements;
                    game.RemovedAchievements = original.RemovedAchievements.ToList();
                    game.AchievementOverrides = new(original.AchievementOverrides);
                    game.RetroAchievements = original.RetroGameId == game.RetroGameId ? original.RetroAchievements : null;
                    foreach(var provider in new[]{"steam", "retro"})
                        if(provider == "steam" ? original.SteamAppId != game.SteamAppId : original.RetroGameId != game.RetroGameId)
                        {
                            game.RemovedAchievements.RemoveAll(k => k.StartsWith(provider+":"));
                            foreach(var key in game.AchievementOverrides.Keys.Where(k=>k.StartsWith(provider+":")).ToArray()) game.AchievementOverrides.Remove(key);
                        }
                }
                AddTask();
                if (creating) owner.Games.Add(game); else owner.Games[owner.Games.IndexOf(original!)] = game;
                owner.Persist(); owner.Refresh(); window.DialogResult = true;
            }
            catch (Exception ex) when (ex is ArgumentException or IOException or InvalidOperationException) { LocalizedNotice.Show(window, I18n.Error(ex), I18n.T("Revisa el juego")); }
        }, true));
        window.Loaded += (_, _) => title.Focus(); window.ShowDialog();
    }

    internal static void DeletedGames(MainWindow owner)
    {
        var window = Modal(owner, I18n.T("Juegos eliminados · Checkpoint"), 540, 660); var body = Panel(); Layout(window, body, out var footer);
        Heading(body, I18n.T("Recuperar juegos"), I18n.T("Se conservan los últimos 20 juegos eliminados, incluso después de reiniciar. Recuperarlos restaura también sus notas, tareas y carátulas."));
        var notice = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = (Brush)Application.Current.Resources["AccentBrush"], Margin = new Thickness(0, 10, 0, 0) }; body.Children.Add(notice);
        var rows = new StackPanel(); body.Children.Add(rows);
        void Render()
        {
            rows.Children.Clear();
            if (owner.DeletedGames.Count == 0) rows.Children.Add(new TextBlock { Text = I18n.T("No hay juegos pendientes de recuperar."), Margin = new Thickness(0, 20, 0, 0) });
            foreach (var deleted in owner.DeletedGames)
            {
                var row = new Grid { Margin = new Thickness(0, 16, 0, 6) }; row.ColumnDefinitions.Add(new()); row.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
                var details = new StackPanel { Margin = new Thickness(0, 0, 12, 0) };
                details.Children.Add(new TextBlock { Text = deleted.Game.Title, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
                details.Children.Add(new TextBlock { Text = deleted.Game.StatusText + " · " + deleted.DeletedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"), FontSize = 11, Foreground = (Brush)Application.Current.Resources["MutedBrush"], Margin = new Thickness(0, 5, 0, 0) });
                details.Children.Add(new TextBlock { Text = deleted.Game.Tasks.Count + I18n.T(" tareas · ") + (string.IsNullOrWhiteSpace(deleted.Game.Notes) ? I18n.T("Sin notas") : I18n.T("Notas guardadas")), FontSize = 11, Margin = new Thickness(0, 4, 0, 0) }); row.Children.Add(details);
                var restore = Button(I18n.T("Recuperar"), (_, _) =>
                {
                    try { var game = owner.RestoreDeleted(deleted.RecoveryId); notice.Text = "«" + game.Title + I18n.T("» recuperado."); Render(); }
                    catch (Exception ex) { notice.Text = I18n.Error(ex); }
                }, true);
                System.Windows.Automation.AutomationProperties.SetName(restore, I18n.T("Recuperar ") + deleted.Game.Title);
                restore.Tag = deleted.RecoveryId; restore.VerticalAlignment = VerticalAlignment.Center; Grid.SetColumn(restore, 1); row.Children.Add(restore); rows.Children.Add(row);
            }
        }
        Render(); footer.Children.Add(Button(I18n.T("Cerrar"), (_, _) => window.Close(), true)); window.ShowDialog();
    }

    internal static void Settings(MainWindow owner)
    {
        var prefs = owner.Preferences; var window = Modal(owner, I18n.T("Ajustes · Checkpoint"), 540, 700); var body = Panel(); Layout(window, body, out var footer);
        Heading(body, I18n.T("A tu manera"), I18n.T("Ajusta el widget para que encaje en tu escritorio."));
        Label(body, I18n.T("Opacidad del fondo")); var opacity = new Slider { Minimum = .35, Maximum = 1, Value = prefs.BackgroundOpacity, TickFrequency = .05, IsSnapToTickEnabled = true }; body.Children.Add(opacity);
        var opacityText = new TextBlock { FontSize = 11, Margin = new Thickness(0, 6, 0, 8) }; body.Children.Add(opacityText);
        Label(body, I18n.T("Idioma"));
        var language = new ComboBox { ItemsSource = new[] { I18n.T("Español"), I18n.T("Inglés") }, SelectedIndex = prefs.Language == "en" ? 1 : 0 };
        System.Windows.Automation.AutomationProperties.SetName(language, I18n.T("Idioma")); body.Children.Add(language);
        string previousTheme = prefs.Theme; bool previousLight = prefs.LightTheme;
        Label(body, I18n.T("Tema"));
        var theme = new ComboBox { ItemsSource = Themes.Ids.Select(Themes.Name).ToArray(), SelectedIndex = Array.IndexOf(Themes.Ids.ToArray(), Themes.Id(prefs)) };
        System.Windows.Automation.AutomationProperties.SetName(theme, I18n.T("Tema")); body.Children.Add(theme);
        body.Children.Add(new TextBlock { Text = I18n.T("Vista previa inmediata. Guarda para conservar el tema; Cancelar recupera el anterior."), FontSize = 11, TextWrapping = TextWrapping.Wrap });
        theme.SelectionChanged += (_, _) => { if (theme.SelectedIndex < 0 || theme.SelectedIndex >= Themes.Ids.Count) return; prefs.Theme = Themes.Ids[theme.SelectedIndex]; prefs.LightTheme = Themes.IsLight(prefs); owner.ApplyPreferences(); owner.Refresh(); };
        double previousOpacity = prefs.BackgroundOpacity;
        opacity.ValueChanged += (_, _) => { prefs.BackgroundOpacity = opacity.Value; opacityText.Text = (int)(opacity.Value * 100) + I18n.T("% · textos y carátulas permanecen legibles"); owner.ApplyPreferences(); };
        opacityText.Text = (int)(opacity.Value * 100) + I18n.T("% · textos y carátulas permanecen legibles");
        var top = Check(body, I18n.T("Mantener siempre visible"), prefs.AlwaysOnTop); var position = Check(body, I18n.T("Bloquear posición y tamaño"), prefs.PositionLocked);
        Label(body, I18n.T("Modo de ventana"));
        int initialMode = prefs.MiniatureView ? 2 : prefs.FullWindow ? 0 : 1;
        var windowMode = new ComboBox { ItemsSource = new[] { I18n.T("Ventana completa"), I18n.T("Ventana pequeña"), I18n.T("Miniatura") }, SelectedIndex = initialMode };
        System.Windows.Automation.AutomationProperties.SetName(windowMode, I18n.T("Modo de ventana")); body.Children.Add(windowMode);
        body.Children.Add(new TextBlock { Text = I18n.T("La ventana completa ocupa el área de trabajo y utiliza opacidad al 100 %. La pequeña y Miniatura conservan tu opacidad."), TextWrapping = TextWrapping.Wrap, FontSize = 11 });
        Label(body, I18n.T("Vista de la colección"));
        var view = new ComboBox { ItemsSource = new[] { I18n.T("Lista"), I18n.T("Compacta"), I18n.T("Cuadrícula de carátulas"), I18n.T("Miniatura") }, SelectedIndex = prefs.MiniatureView ? 3 : prefs.GridView ? 2 : prefs.Compact ? 1 : 0 };
        System.Windows.Automation.AutomationProperties.SetName(view, I18n.T("Vista de la colección")); body.Children.Add(view);
        Label(body, I18n.T("Tamaño de texto en Miniatura"));
        var miniatureText = new ComboBox { ItemsSource = Enumerable.Range(12,9).ToArray(), SelectedItem = Math.Clamp(prefs.MiniatureTextSize,12,20) };
        System.Windows.Automation.AutomationProperties.SetName(miniatureText, I18n.T("Tamaño de texto en Miniatura")); body.Children.Add(miniatureText);
        body.Children.Add(new TextBlock { Text = I18n.T("Amplía Miniatura si los nombres se recortan. El texto de las otras vistas no cambia."), FontSize = 11, TextWrapping = TextWrapping.Wrap });
        var lightweight = Check(body, I18n.T("Modo ligero (sin carátulas)"), prefs.LightweightMode);
        body.Children.Add(new TextBlock { Text = I18n.T("Oculta las carátulas de tu lista y de amigos, evita nuevas descargas de imágenes y libera su caché. Conserva los juegos, objetivos y progreso."), FontSize = 11, TextWrapping = TextWrapping.Wrap });
        var tray = Check(body, I18n.T("Ocultar en la bandeja al cerrar"), prefs.CloseToTray); var startup = Check(body, I18n.T("Iniciar con Windows"), prefs.StartWithWindows);
        Label(body, I18n.T("Atajos")); body.Children.Add(new TextBlock { Text = I18n.T("Ctrl+Alt+C · mostrar / ocultar\nCtrl+N · añadir juego     Ctrl+F · buscar\nF6 · cambiar vista     Escape · ocultar\nAlt+↑ / Alt+↓ · reordenar desde el asa ⠿\nCtrl+Z · recuperar el último juego eliminado"), FontSize = 12, LineHeight = 20 });
        var detectGames = Check(body, I18n.T("Detectar juegos y abrir sus logros automáticamente"), prefs.DetectGames);
        body.Children.Add(Button(I18n.T("Configurar RetroAchievements"), (_, _) => RetroSettings(owner, window)));
        body.Children.Add(Button(I18n.T("Repasar todos los logros"), (_, _) => ReviewAchievements(owner, window)));
        body.Children.Add(Button(I18n.T("Buscar carátulas que faltan"), (_, _) => ReviewCovers(owner, window)));
        Label(body, "Steam"); var steamSummary = new TextBlock { Text = owner.Steam.Session is null ? I18n.T("Cuenta sin vincular. El inicio de sesión se realiza en Steam.") : I18n.T("Cuenta vinculada: ") + owner.Steam.Session.SteamId, TextWrapping = TextWrapping.Wrap, FontSize = 12 }; body.Children.Add(steamSummary);
        var steamActions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) }; body.Children.Add(steamActions);
        var advanced = new StackPanel(); var endpoint = Input(advanced, I18n.T("Dirección del servicio"), prefs.ServiceUrl);
        var expander = new Expander { Header = I18n.T("Conexión avanzada"), Content = advanced, Foreground = (Brush)Application.Current.Resources["TextBrush"], Margin = new Thickness(0, 12, 0, 0) }; body.Children.Add(expander);
        Label(body, I18n.T("Sincronizar biblioteca y logros al abrir y cada…")); var interval = new ComboBox { ItemsSource = new[] { I18n.T("15 minutos"), I18n.T("30 minutos"), I18n.T("60 minutos"), I18n.T("120 minutos") }, SelectedIndex = Array.IndexOf(new[] { 15, 30, 60, 120 }, prefs.SyncMinutes) }; if (interval.SelectedIndex < 0) interval.SelectedIndex = 1; body.Children.Add(interval);
        steamActions.Children.Add(Button(I18n.T("Vincular Steam"), async (_, _) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(endpoint.Text)) { expander.IsExpanded = true; LocalizedNotice.Show(window, I18n.T("Esta edición todavía no tiene configurado el servicio de Steam. Introduce su dirección en Conexión avanzada o usa la biblioteca local."), "Steam"); return; }
                prefs.ServiceUrl = SteamClient.ValidateServiceUrl(endpoint.Text).AbsoluteUri; owner.Persist(); window.Close(); await owner.ConnectSteam();
            }
            catch (ArgumentException ex) { LocalizedNotice.Show(window, I18n.Error(ex), "Steam"); }
        }, true));
        steamActions.Children.Add(Button(I18n.T("Desvincular"), async (_, _) => { await owner.Steam.Disconnect(); prefs.SteamId = null; steamSummary.Text = I18n.T("Cuenta desvinculada. Se conserva tu biblioteca local."); owner.Persist(); owner.Refresh(); }));
        Label(body, I18n.T("Copias de seguridad")); body.Children.Add(new TextBlock { Text = I18n.T("La copia completa incluye tu colección actual, notas, tareas, último progreso y carátulas personalizadas. No incluye la sesión de Steam. También puedes usar el formato JSON anterior, sin imágenes."), FontSize = 11, TextWrapping = TextWrapping.Wrap });
        var backups = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) }; body.Children.Add(backups);
        var backupNotice = new TextBlock { FontSize = 11, TextWrapping = TextWrapping.Wrap, Foreground = (Brush)Application.Current.Resources["AccentBrush"], Margin = new Thickness(0, 10, 0, 0) }; body.Children.Add(backupNotice);
        backups.Children.Add(Button(I18n.T("Exportar"), (_, _) =>
        {
            var picker = new SaveFileDialog { Filter = I18n.T("Copia completa de Checkpoint|*.checkpoint|JSON compatible (sin imágenes)|*.json"), DefaultExt = ".checkpoint", FileName = "checkpoint-" + DateTime.Now.ToString("yyyy-MM-dd") };
            if (picker.ShowDialog(window) == true)
                try { owner.ExportBackup(picker.FileName); backupNotice.Text = I18n.T("Copia exportada: ") + Path.GetFileName(picker.FileName); }
                catch (Exception ex) { backupNotice.Text = I18n.T("No se pudo exportar: ") + I18n.Error(ex); }
        }));
        backups.Children.Add(Button(I18n.T("Importar"), (_, _) =>
        {
            var picker = new OpenFileDialog { Filter = I18n.T("Copias de Checkpoint|*.checkpoint;*.json;*.zip") }; if (picker.ShowDialog(window) != true) return;
            try
            {
                var result = owner.ImportBackup(picker.FileName); backupNotice.Text = (I18n.IsEnglish ? $"{result.Added} games imported · {result.Skipped} already in your library." : $"{result.Added} juegos importados · {result.Skipped} ya estaban en tu biblioteca.");
            }
            catch (Exception ex) { backupNotice.Text = I18n.T("No se pudo importar: ") + I18n.Error(ex); }
        }));
        Label(body, I18n.T("Juegos eliminados")); body.Children.Add(new TextBlock { Text = I18n.T("Los últimos 20 pueden recuperarse con sus datos completos. Los juegos existentes en tu biblioteca se conservan."), FontSize = 11, TextWrapping = TextWrapping.Wrap });
        body.Children.Add(Button(I18n.T("Ver juegos eliminados"), (_, _) => DeletedGames(owner)));
        Label(body, "Checkpoint " + typeof(MainWindow).Assembly.GetName().Version?.ToString(3)); body.Children.Add(new TextBlock { Text = I18n.T("Los datos se guardan en tu PC. Sin publicidad ni telemetría. Aplicación independiente, sin afiliación con Valve."), TextWrapping = TextWrapping.Wrap, FontSize = 11 });
        body.Children.Add(Button(I18n.T("Gestionar listas"),(_,_)=>ManageLists(owner,window)));
        body.Children.Add(Button(I18n.T("Configurar atajos"), (_, _) => ShortcutSettings(owner,window)));
        body.Children.Add(Button(I18n.T("Actualizaciones"), (_, _) => AppUpdatesDialog(owner,window)));
        bool saved = false;
        footer.Children.Add(Button(I18n.T("Cancelar"), (_, _) => window.Close()));
        footer.Children.Add(Button(I18n.T("Guardar"), (_, _) =>
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(endpoint.Text)) SteamClient.ValidateServiceUrl(endpoint.Text);
                if ((startup.IsChecked == true) != prefs.StartWithWindows) SetStartup(startup.IsChecked == true);
                prefs.BackgroundOpacity = opacity.Value; prefs.AlwaysOnTop = top.IsChecked == true; prefs.PositionLocked = position.IsChecked == true;
                prefs.FullWindow = windowMode.SelectedIndex == 0;
                prefs.MiniatureView = windowMode.SelectedIndex != initialMode ? windowMode.SelectedIndex == 2 : view.SelectedIndex == 3;
                if (!prefs.MiniatureView && view.SelectedIndex != 3) { prefs.Compact = view.SelectedIndex == 1; prefs.GridView = view.SelectedIndex == 2; }
                prefs.Theme = Themes.Ids[Math.Clamp(theme.SelectedIndex, 0, Themes.Ids.Count - 1)]; prefs.LightTheme = Themes.IsLight(prefs); prefs.CloseToTray = tray.IsChecked == true; prefs.StartWithWindows = startup.IsChecked == true;
                prefs.ServiceUrl = endpoint.Text.Trim(); prefs.SyncMinutes = new[] { 15, 30, 60, 120 }[interval.SelectedIndex];
                prefs.Language = language.SelectedIndex == 1 ? "en" : "es";
                prefs.LightweightMode = lightweight.IsChecked == true; prefs.DetectGames = detectGames.IsChecked == true;
                prefs.MiniatureTextSize = (int)miniatureText.SelectedItem;
                owner.ApplyPreferences(); owner.Persist(); saved = true; window.Close(); owner.ApplyLanguage();
            }
            catch (Exception ex) { LocalizedNotice.Show(window, I18n.Error(ex), I18n.T("No se pudieron guardar los ajustes")); }
        }, true));
        window.Closed += (_, _) => { if (!saved) { prefs.BackgroundOpacity = previousOpacity; prefs.Theme = previousTheme; prefs.LightTheme = previousLight; owner.ApplyPreferences(); owner.Refresh(); owner.Store.SaveSettings(prefs); } }; window.ShowDialog();
    }
    internal static void ShortcutSettings(MainWindow owner, Window? parent = null)
    {
        var window=Modal(owner,I18n.T("Configurar atajos")); if(parent is not null)window.Owner=parent;
        var body=Panel(); Layout(window,body,out var footer);
        Heading(body,I18n.T("Configurar atajos"),I18n.T("Pulsa una combinación en cada campo. Tab cambia de campo; Escape cancela. No se permiten atajos repetidos."));
        var values=Shortcuts.Effective(owner.Preferences.Shortcuts); var fields=new Dictionary<string,TextBox>();
        foreach(var key in Shortcuts.Defaults.Keys)
        {
            var input=Input(body,I18n.T(Shortcuts.Labels[key]),values[key]); input.Tag="shortcut"; fields[key]=input;
            input.PreviewKeyDown+=(_,e)=>{if(e.Key is System.Windows.Input.Key.Tab or System.Windows.Input.Key.Escape or System.Windows.Input.Key.LeftCtrl or System.Windows.Input.Key.RightCtrl or System.Windows.Input.Key.LeftAlt or System.Windows.Input.Key.RightAlt or System.Windows.Input.Key.LeftShift or System.Windows.Input.Key.RightShift)return; input.Text=MainWindow.ShortcutGesture(e);e.Handled=true;};
        }
        footer.Children.Add(Button(I18n.T("Restablecer predeterminados"),(_,_)=>{foreach(var pair in fields)pair.Value.Text=Shortcuts.Defaults[pair.Key];}));
        footer.Children.Add(Button(I18n.T("Cancelar"),(_,_)=>window.Close()));
        footer.Children.Add(Button(I18n.T("Guardar"),(_,_)=>
        {
            try
            {
                var candidate=Shortcuts.Validate(fields.ToDictionary(p=>p.Key,p=>p.Value.Text));
                if(candidate["global"]!=Shortcuts.Effective(owner.Preferences.Shortcuts)["global"] && !owner.ChangeGlobalShortcut(candidate["global"]))throw new InvalidOperationException(I18n.T("No se pudo registrar el atajo global. Otra aplicación puede estar usándolo."));
                var previous=owner.Preferences.Shortcuts; owner.Preferences.Shortcuts=candidate;
                try{owner.Persist();}catch{owner.Preferences.Shortcuts=previous;owner.ChangeGlobalShortcut(Shortcuts.Effective(previous)["global"]);throw;}
                owner.Refresh();window.Close();
            }
            catch(Exception ex){LocalizedNotice.Show(window,I18n.Error(ex),I18n.T("Configurar atajos"));}
        },true));
        window.ShowDialog();
    }
    internal static void RestoreStartupIfMissing()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Checkpoint.lnk");
        if (!File.Exists(path)) SetStartup(true);
    }
    private static void SetStartup(bool enabled)
    {
        string shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Checkpoint.lnk");
        if (!enabled) { if (File.Exists(shortcutPath)) File.Delete(shortcutPath); return; }
        var type = Type.GetTypeFromProgID("WScript.Shell") ?? throw new InvalidOperationException(I18n.T("No se puede crear el acceso de inicio."));
        dynamic shell = Activator.CreateInstance(type)!;
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        try
        {
            shortcut.TargetPath = Environment.ProcessPath;
            shortcut.WorkingDirectory = AppContext.BaseDirectory;
            shortcut.Description = "Checkpoint";
            shortcut.Save();
        }
        finally { Marshal.FinalReleaseComObject(shortcut); Marshal.FinalReleaseComObject(shell); }
    }
}
