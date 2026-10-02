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

internal static class Dialogs
{
    private static Window Modal(MainWindow owner, string title, double width = 540, double height = 710)
    {
        var window = new Window { Owner = owner, Title = title, Width = width, Height = height,
            MinWidth = 400, MinHeight = 420, WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInTaskbar = false, FontFamily = new FontFamily("Segoe UI"), FontSize = 13,
            Background = (Brush)Application.Current.Resources["InputBrush"], Foreground = (Brush)Application.Current.Resources["TextBrush"] };
        window.Height = Math.Min(height, SystemParameters.WorkArea.Height - 35);
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
        var game = original is null ? new Game { SortOrder = owner.Games.Count } : JsonSerializer.Deserialize<Game>(JsonSerializer.Serialize(original, DataJson.Options), DataJson.Options)!;
        var window = Modal(owner, creating ? "Añadir juego · Checkpoint" : game.Title + " · Checkpoint");
        var body = Panel(); Layout(window, body, out var footer);
        Heading(body, creating ? "Una nueva aventura" : game.Title, "La historia y los logros se guardan como objetivos independientes.");
        var title = Input(body, "Nombre del juego", game.Title); title.MaxLength = 140;
        var steamId = Input(body, "ID del juego en Steam (opcional)", game.SteamAppId?.ToString() ?? "");
        var platform = Input(body, "Plataforma", game.Platform); platform.MaxLength = 60;
        Label(body, "Estado"); var state = new ComboBox { ItemsSource = Enum.GetValues<GameStatus>().Select(Labels.Status).ToList(), SelectedIndex = (int)game.Status }; body.Children.Add(state);
        Label(body, "Objetivo"); var goal = new ComboBox { ItemsSource = Enum.GetValues<GameGoal>().Select(Labels.Goal).ToList(), SelectedIndex = (int)game.Goal }; body.Children.Add(goal);
        var customGoal = Input(body, "Objetivo personalizado", game.CustomGoal); customGoal.MaxLength = 500;
        var storyPercent = Input(body, "Historia completada (0–100 %, opcional y manual)", game.StoryPercent?.ToString() ?? "");
        storyPercent.MaxLength = 3;
        var tracked = Check(body, "Mostrar en Mi lista", game.Tracked);
        var favorite = Check(body, "Destacar como favorito", game.Favorite);
        var priority = Input(body, "Orden en la lista (los números menores aparecen antes)", game.SortOrder.ToString());
        var notes = Input(body, "Notas · dónde lo dejaste", game.Notes, true); notes.MaxLength = 20000;
        Label(body, "Tareas"); var tasks = new StackPanel(); body.Children.Add(tasks);
        void RenderTasks()
        {
            tasks.Children.Clear();
            foreach (var item in game.Tasks)
            {
                var row = new DockPanel(); var remove = Button("×", (_, _) => { game.Tasks.Remove(item); RenderTasks(); });
                remove.Padding = new Thickness(7, 2, 7, 2); remove.ToolTip = "Quitar tarea"; DockPanel.SetDock(remove, Dock.Right); row.Children.Add(remove);
                var checkbox = new CheckBox { Content = item.Title, IsChecked = item.Done, VerticalAlignment = VerticalAlignment.Center };
                checkbox.Checked += (_, _) => item.Done = true; checkbox.Unchecked += (_, _) => item.Done = false; row.Children.Add(checkbox); tasks.Children.Add(row);
            }
        }
        RenderTasks(); var nextTask = new TextBox { MaxLength = 500, Margin = new Thickness(0, 8, 0, 6) }; body.Children.Add(nextTask);
        System.Windows.Automation.AutomationProperties.SetName(nextTask, "Nueva tarea");
        void AddTask() { if (!string.IsNullOrWhiteSpace(nextTask.Text) && game.Tasks.Count < 200) { game.Tasks.Add(new() { Title = nextTask.Text.Trim() }); nextTask.Clear(); RenderTasks(); } }
        body.Children.Add(Button("+ Añadir tarea", (_, _) => AddTask()));
        nextTask.KeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.Enter) { AddTask(); e.Handled = true; } };
        Label(body, "Carátula");
        var coverNotice = new TextBlock { Text = game.CustomCover is null ? "Automática para juegos de Steam; puedes elegir una imagen propia." : "Carátula personalizada guardada.", FontSize = 11, TextWrapping = TextWrapping.Wrap }; body.Children.Add(coverNotice);
        var imageButtons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) }; body.Children.Add(imageButtons);
        imageButtons.Children.Add(Button("Elegir imagen", (_, _) =>
        {
            var picker = new OpenFileDialog { Filter = "Imágenes|*.png;*.jpg;*.jpeg;*.webp;*.bmp", Title = "Elegir carátula" };
            if (picker.ShowDialog(window) == true)
            {
                try { game.CustomCover = owner.Covers.Import(picker.FileName); coverNotice.Text = "Carátula personalizada preparada."; }
                catch (Exception ex) { MessageBox.Show(window, "No se pudo leer la imagen: " + ex.Message, "Checkpoint"); }
            }
        }));
        imageButtons.Children.Add(Button("Automática", (_, _) => { game.CustomCover = null; coverNotice.Text = "Se usará la carátula de Steam, si está disponible."; }));
        if (!creating)
        {
            Label(body, "Steam");
            body.Children.Add(new TextBlock { Text = game.SyncedAt is { } date ? "Última consulta: " + date.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : "Este juego aún no se ha sincronizado.", FontSize = 11 });
            var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) }; body.Children.Add(actions);
            actions.Children.Add(Button("Ver logros", (_, _) => Achievements(owner, original!)));
            if (game.SteamAppId is int appId)
            {
                actions.Children.Add(Button("Abrir en Steam", (_, _) => Process.Start(new ProcessStartInfo($"https://store.steampowered.com/app/{appId}/") { UseShellExecute = true })));
                actions.Children.Add(Button("Jugar", (_, _) => Process.Start(new ProcessStartInfo($"steam://rungameid/{appId}") { UseShellExecute = true })));
            }
            Label(body, "Biblioteca");
            body.Children.Add(Button("Eliminar juego", (_, _) =>
            {
                try { owner.DeleteGame(original!); window.Close(); }
                catch (Exception ex) { MessageBox.Show(window, ex.Message, "No se pudo eliminar el juego"); }
            }));
        }
        footer.Children.Add(Button("Cancelar", (_, _) => window.Close()));
        footer.Children.Add(Button("Guardar", (_, _) =>
        {
            try
            {
                game.Title = title.Text; game.Platform = platform.Text; game.CustomGoal = customGoal.Text; game.Notes = notes.Text;
                if (string.IsNullOrWhiteSpace(storyPercent.Text)) game.StoryPercent = null;
                else if (int.TryParse(storyPercent.Text,out int percent) && percent is >= 0 and <= 100) game.StoryPercent = percent;
                else throw new ArgumentException("La historia completada debe estar entre 0 y 100, o dejarse vacía.");
                if (string.IsNullOrWhiteSpace(steamId.Text)) game.SteamAppId = null;
                else if (int.TryParse(steamId.Text.Trim(), out int id) && id > 0) game.SteamAppId = id;
                else throw new ArgumentException("El ID de Steam debe ser un número positivo.");
                if (owner.Games.Any(g => g.Id != game.Id && g.SteamAppId is not null && g.SteamAppId == game.SteamAppId)) throw new ArgumentException("Ese juego de Steam ya está en la biblioteca.");
                if (!int.TryParse(priority.Text, out int order)) throw new ArgumentException("El orden debe ser un número entero.");
                game.SortOrder = order; game.Goal = (GameGoal)goal.SelectedIndex; game.Tracked = tracked.IsChecked == true; game.Favorite = favorite.IsChecked == true;
                GameRules.SetStatus(game, (GameStatus)state.SelectedIndex); GameRules.Validate(game);
                if (original?.SteamAppId != game.SteamAppId) { game.Achievements = null; game.SyncedAt = null; }
                else if (original is not null)
                {
                    // A background refresh may have completed while this editor was open.
                    game.Achievements = original.Achievements; game.SyncedAt = original.SyncedAt;
                    game.PlaytimeMinutes = original.PlaytimeMinutes;
                }
                AddTask();
                if (creating) owner.Games.Add(game); else owner.Games[owner.Games.IndexOf(original!)] = game;
                owner.Persist(); owner.Refresh(); window.DialogResult = true;
            }
            catch (Exception ex) when (ex is ArgumentException or IOException or InvalidOperationException) { MessageBox.Show(window, ex.Message, "Revisa el juego"); }
        }, true));
        window.Loaded += (_, _) => title.Focus(); window.ShowDialog();
    }

    internal static void DeletedGames(MainWindow owner)
    {
        var window = Modal(owner, "Juegos eliminados · Checkpoint", 540, 660); var body = Panel(); Layout(window, body, out var footer);
        Heading(body, "Recupera una aventura", "Se conservan los últimos 20 juegos eliminados, incluso después de reiniciar. Recuperarlos restaura también sus notas, tareas y carátulas.");
        var notice = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = (Brush)Application.Current.Resources["AccentBrush"], Margin = new Thickness(0, 10, 0, 0) }; body.Children.Add(notice);
        var rows = new StackPanel(); body.Children.Add(rows);
        void Render()
        {
            rows.Children.Clear();
            if (owner.DeletedGames.Count == 0) rows.Children.Add(new TextBlock { Text = "No hay juegos pendientes de recuperar.", Margin = new Thickness(0, 20, 0, 0) });
            foreach (var deleted in owner.DeletedGames)
            {
                var row = new Grid { Margin = new Thickness(0, 16, 0, 6) }; row.ColumnDefinitions.Add(new()); row.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
                var details = new StackPanel { Margin = new Thickness(0, 0, 12, 0) };
                details.Children.Add(new TextBlock { Text = deleted.Game.Title, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
                details.Children.Add(new TextBlock { Text = deleted.Game.StatusText + " · " + deleted.DeletedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"), FontSize = 11, Foreground = (Brush)Application.Current.Resources["MutedBrush"], Margin = new Thickness(0, 5, 0, 0) });
                details.Children.Add(new TextBlock { Text = deleted.Game.Tasks.Count + " tareas · " + (string.IsNullOrWhiteSpace(deleted.Game.Notes) ? "Sin notas" : "Notas guardadas"), FontSize = 11, Margin = new Thickness(0, 4, 0, 0) }); row.Children.Add(details);
                var restore = Button("Recuperar", (_, _) =>
                {
                    try { var game = owner.RestoreDeleted(deleted.RecoveryId); notice.Text = "«" + game.Title + "» recuperado."; Render(); }
                    catch (Exception ex) { notice.Text = ex.Message; }
                }, true);
                System.Windows.Automation.AutomationProperties.SetName(restore, "Recuperar " + deleted.Game.Title);
                restore.Tag = deleted.RecoveryId; restore.VerticalAlignment = VerticalAlignment.Center; Grid.SetColumn(restore, 1); row.Children.Add(restore); rows.Children.Add(row);
            }
        }
        Render(); footer.Children.Add(Button("Cerrar", (_, _) => window.Close(), true)); window.ShowDialog();
    }

    internal static void Achievements(MainWindow owner, Game game)
    {
        var window = Modal(owner, "Logros · " + game.Title, 540, 660); var body = Panel(); Layout(window, body, out var footer);
        Heading(body, "Logros de " + game.Title);
        var summary = new TextBlock { Margin = new Thickness(0, 0, 0, 8) }; body.Children.Add(summary);
        var spoilers = Check(body, "Mostrar nombres y descripciones de logros secretos", false);
        var locked = Check(body, "Mostrar solo los pendientes", true); var list = new StackPanel(); body.Children.Add(list);
        void Render()
        {
            list.Children.Clear(); summary.Text = game.Achievements is null ? "Sin datos sincronizados." : game.Achievements.Count == 0 ? "Este juego no tiene logros disponibles." : $"{game.UnlockedCount} de {game.Achievements.Count} desbloqueados · {game.AchievementPercent}%";
            foreach (var item in (game.Achievements ?? []).Where(a => locked.IsChecked != true || !a.Unlocked).OrderBy(a => a.Unlocked))
            {
                bool secret = item.Hidden && !item.Unlocked && spoilers.IsChecked != true;
                var panel = new StackPanel { Margin = new Thickness(0, 12, 0, 8) };
                panel.Children.Add(new TextBlock { Text = (item.Unlocked ? "✓  " : "○  ") + (secret ? "Logro secreto" : item.Name), FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
                panel.Children.Add(new TextBlock { Text = secret ? "Activa la opción superior para revelar este logro." : item.Description, FontSize = 11, TextWrapping = TextWrapping.Wrap, Foreground = (Brush)Application.Current.Resources["MutedBrush"], Margin = new Thickness(0, 5, 0, 0) });
                if (item.UnlockedAt is { } date) panel.Children.Add(new TextBlock { Text = date.ToLocalTime().ToString("dd/MM/yyyy"), FontSize = 10, Margin = new Thickness(0, 4, 0, 0) });
                list.Children.Add(panel);
            }
        }
        spoilers.Checked += (_, _) => Render(); spoilers.Unchecked += (_, _) => Render(); locked.Checked += (_, _) => Render(); locked.Unchecked += (_, _) => Render(); Render();
        var sync = Button("Actualizar", async (_, _) => { await owner.Sync(false, game); Render(); }); sync.IsEnabled = owner.Steam.Session is not null && game.SteamAppId.HasValue; footer.Children.Add(sync);
        footer.Children.Add(Button("Cerrar", (_, _) => window.Close(), true)); window.ShowDialog();
    }

    internal static void Settings(MainWindow owner)
    {
        var prefs = owner.Preferences; var window = Modal(owner, "Ajustes · Checkpoint", 540, 700); var body = Panel(); Layout(window, body, out var footer);
        Heading(body, "A tu manera", "Ajusta el widget para que encaje en tu escritorio.");
        Label(body, "Opacidad del fondo"); var opacity = new Slider { Minimum = .35, Maximum = 1, Value = prefs.BackgroundOpacity, TickFrequency = .05, IsSnapToTickEnabled = true }; body.Children.Add(opacity);
        var opacityText = new TextBlock { FontSize = 11, Margin = new Thickness(0, 6, 0, 8) }; body.Children.Add(opacityText);
        double previousOpacity = prefs.BackgroundOpacity;
        opacity.ValueChanged += (_, _) => { prefs.BackgroundOpacity = opacity.Value; opacityText.Text = (int)(opacity.Value * 100) + "% · textos y carátulas permanecen legibles"; owner.ApplyPreferences(); };
        opacityText.Text = (int)(opacity.Value * 100) + "% · textos y carátulas permanecen legibles";
        var top = Check(body, "Mantener siempre visible", prefs.AlwaysOnTop); var position = Check(body, "Bloquear posición y tamaño", prefs.PositionLocked);
        Label(body, "Vista de la colección");
        var view = new ComboBox { ItemsSource = new[] { "Lista", "Compacta", "Cuadrícula de carátulas" }, SelectedIndex = prefs.GridView ? 2 : prefs.Compact ? 1 : 0 };
        System.Windows.Automation.AutomationProperties.SetName(view, "Vista de la colección"); body.Children.Add(view);
        var light = Check(body, "Tema claro", prefs.LightTheme);
        var tray = Check(body, "Ocultar en la bandeja al cerrar", prefs.CloseToTray); var startup = Check(body, "Iniciar con Windows", prefs.StartWithWindows);
        Label(body, "Atajos"); body.Children.Add(new TextBlock { Text = "Ctrl+Alt+C · mostrar / ocultar\nCtrl+N · añadir juego     Ctrl+F · buscar\nF6 · cambiar vista     Escape · ocultar\nAlt+↑ / Alt+↓ · reordenar desde el asa ⠿\nCtrl+Z · recuperar el último juego eliminado", FontSize = 12, LineHeight = 20 });
        Label(body, "Steam"); var steamSummary = new TextBlock { Text = owner.Steam.Session is null ? "Cuenta sin vincular. El inicio de sesión se realiza en Steam." : "Cuenta vinculada: " + owner.Steam.Session.SteamId, TextWrapping = TextWrapping.Wrap, FontSize = 12 }; body.Children.Add(steamSummary);
        var steamActions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) }; body.Children.Add(steamActions);
        var advanced = new StackPanel(); var endpoint = Input(advanced, "Dirección del servicio", prefs.ServiceUrl);
        var expander = new Expander { Header = "Conexión avanzada", Content = advanced, Foreground = (Brush)Application.Current.Resources["TextBrush"], Margin = new Thickness(0, 12, 0, 0) }; body.Children.Add(expander);
        Label(body, "Actualizar progreso cada…"); var interval = new ComboBox { ItemsSource = new[] { "15 minutos", "30 minutos", "60 minutos", "120 minutos" }, SelectedIndex = Array.IndexOf(new[] { 15, 30, 60, 120 }, prefs.SyncMinutes) }; if (interval.SelectedIndex < 0) interval.SelectedIndex = 1; body.Children.Add(interval);
        steamActions.Children.Add(Button("Vincular Steam", async (_, _) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(endpoint.Text)) { expander.IsExpanded = true; MessageBox.Show(window, "Esta edición todavía no tiene configurado el servicio de Steam. Introduce su dirección en Conexión avanzada o usa la biblioteca local.", "Steam"); return; }
                prefs.ServiceUrl = SteamClient.ValidateServiceUrl(endpoint.Text).AbsoluteUri; owner.Persist(); window.Close(); await owner.ConnectSteam();
            }
            catch (ArgumentException ex) { MessageBox.Show(window, ex.Message, "Steam"); }
        }, true));
        steamActions.Children.Add(Button("Desvincular", async (_, _) => { await owner.Steam.Disconnect(); prefs.SteamId = null; steamSummary.Text = "Cuenta desvinculada. Se conserva tu biblioteca local."; owner.Persist(); owner.Refresh(); }));
        Label(body, "Copias de seguridad"); body.Children.Add(new TextBlock { Text = "La copia completa incluye tu colección actual, notas, tareas, último progreso y carátulas personalizadas. No incluye la sesión de Steam. También puedes usar el formato JSON anterior, sin imágenes.", FontSize = 11, TextWrapping = TextWrapping.Wrap });
        var backups = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) }; body.Children.Add(backups);
        var backupNotice = new TextBlock { FontSize = 11, TextWrapping = TextWrapping.Wrap, Foreground = (Brush)Application.Current.Resources["AccentBrush"], Margin = new Thickness(0, 10, 0, 0) }; body.Children.Add(backupNotice);
        backups.Children.Add(Button("Exportar", (_, _) =>
        {
            var picker = new SaveFileDialog { Filter = "Copia completa de Checkpoint|*.checkpoint|JSON compatible (sin imágenes)|*.json", DefaultExt = ".checkpoint", FileName = "checkpoint-" + DateTime.Now.ToString("yyyy-MM-dd") };
            if (picker.ShowDialog(window) == true)
                try { owner.ExportBackup(picker.FileName); backupNotice.Text = "Copia exportada: " + Path.GetFileName(picker.FileName); }
                catch (Exception ex) { backupNotice.Text = "No se pudo exportar: " + ex.Message; }
        }));
        backups.Children.Add(Button("Importar", (_, _) =>
        {
            var picker = new OpenFileDialog { Filter = "Copias de Checkpoint|*.checkpoint;*.json;*.zip" }; if (picker.ShowDialog(window) != true) return;
            try
            {
                var result = owner.ImportBackup(picker.FileName); backupNotice.Text = $"{result.Added} juegos importados · {result.Skipped} ya estaban en tu biblioteca.";
            }
            catch (Exception ex) { backupNotice.Text = "No se pudo importar: " + ex.Message; }
        }));
        Label(body, "Juegos eliminados"); body.Children.Add(new TextBlock { Text = "Los últimos 20 pueden recuperarse con sus datos completos. Los juegos existentes en tu biblioteca se conservan.", FontSize = 11, TextWrapping = TextWrapping.Wrap });
        body.Children.Add(Button("Ver juegos eliminados", (_, _) => DeletedGames(owner)));
        Label(body, "Checkpoint " + typeof(MainWindow).Assembly.GetName().Version?.ToString(3)); body.Children.Add(new TextBlock { Text = "Los datos se guardan en tu PC. Sin publicidad ni telemetría. Aplicación independiente, sin afiliación con Valve.", TextWrapping = TextWrapping.Wrap, FontSize = 11 });
        bool saved = false;
        footer.Children.Add(Button("Cancelar", (_, _) => window.Close()));
        footer.Children.Add(Button("Guardar", (_, _) =>
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(endpoint.Text)) SteamClient.ValidateServiceUrl(endpoint.Text);
                if ((startup.IsChecked == true) != prefs.StartWithWindows) SetStartup(startup.IsChecked == true);
                prefs.BackgroundOpacity = opacity.Value; prefs.AlwaysOnTop = top.IsChecked == true; prefs.PositionLocked = position.IsChecked == true;
                prefs.Compact = view.SelectedIndex == 1; prefs.GridView = view.SelectedIndex == 2; prefs.LightTheme = light.IsChecked == true; prefs.CloseToTray = tray.IsChecked == true; prefs.StartWithWindows = startup.IsChecked == true;
                prefs.ServiceUrl = endpoint.Text.Trim(); prefs.SyncMinutes = new[] { 15, 30, 60, 120 }[interval.SelectedIndex];
                owner.ApplyPreferences(); owner.Persist(); owner.Refresh(); saved = true; window.Close();
            }
            catch (Exception ex) { MessageBox.Show(window, ex.Message, "No se pudieron guardar los ajustes"); }
        }, true));
        window.Closed += (_, _) => { if (!saved) { prefs.BackgroundOpacity = previousOpacity; owner.ApplyPreferences(); } }; window.ShowDialog();
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
        var type = Type.GetTypeFromProgID("WScript.Shell") ?? throw new InvalidOperationException("No se puede crear el acceso de inicio.");
        dynamic shell = Activator.CreateInstance(type)!;
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        try
        {
            shortcut.TargetPath = Environment.ProcessPath;
            shortcut.WorkingDirectory = AppContext.BaseDirectory;
            shortcut.Description = "Checkpoint · Un juego cada vez";
            shortcut.Save();
        }
        finally { Marshal.FinalReleaseComObject(shortcut); Marshal.FinalReleaseComObject(shell); }
    }
}
