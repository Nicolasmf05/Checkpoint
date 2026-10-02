using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Checkpoint.Core;
using Microsoft.Data.Sqlite;

namespace Checkpoint.App;

// Exercises the real WPF templates and modal dialogs in an isolated --data-dir.
// No Windows settings, external login, file picker or deletion confirmation is automated.
public partial class MainWindow
{
    internal async Task RenderSmokeTest(string outputDirectory)
    {
        var checks = new List<string>();
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("FAILED: " + name);
            checks.Add(name); Console.WriteLine("PASS " + name);
        }
        void Render(FrameworkElement element, string name) => RenderElement(element, Path.Combine(outputDirectory, name));
        async Task SetView(bool grid, bool compact, bool light, double width, double height)
        {
            Width = width; Height = height;
            Preferences.GridView = grid; Preferences.Compact = compact; Preferences.LightTheme = light;
            ApplyPreferences(); Refresh();
            await Dispatcher.InvokeAsync(UpdateLayout, DispatcherPriority.ContextIdle);
        }
        try
        {
            Directory.CreateDirectory(outputDirectory);
            await Task.Delay(1200);
            await SetView(false, false, false, 510, 740);
            Search.Text = "Hades";
            Check(visibleCards.Count == 1 && visibleCards[0].Model.SteamAppId == 1145360, "list search");
            Search.Clear(); StatusFilter.SelectedIndex = 2;
            Check(visibleCards.Count == 1 && visibleCards[0].Model.Status == GameStatus.Playing, "status filter");
            StatusFilter.SelectedIndex = 0;
            await Task.WhenAll(visibleCards.Select(card => card.LoadCover(Covers)));
            await Dispatcher.InvokeAsync(UpdateLayout, DispatcherPriority.Render);
            Render(this, "widget-dark.png");

            await SetView(true, false, false, 510, 740);
            Check(gridColumns == 3 && GameList.Items.Count == 1 && visibleCards.Count == 3, "grid row grouping");
            Check(VisualChildren(GameList).OfType<Image>().Count() == 3, "grid templates create cover controls");
            Render(this, "widget-grid-dark.png");
            Search.Text = "Hades";
            Check(visibleCards.Count == 1 && GameList.Items.Count == 1, "grid search counts cards rather than rows");
            Search.Clear();
            await SetView(true, false, true, 375, 640);
            Check(gridColumns == 2 && GameList.Items.Count == 2, "narrow grid uses two columns");
            Render(this, "widget-grid-narrow-light.png");
            await SetView(true, false, false, 760, 800);
            Check(gridColumns == 4, "wide grid responds to resizing");
            Render(this, "widget-grid-wide-dark.png");

            var hades = Games.Single(g => g.SteamAppId == 1145360);
            var portal = Games.Single(g => g.SteamAppId == 620);
            var pinned = Games.Single(g => g.Favorite);
            Check(MoveCard(portal.Id, hades.Id, false), "native reorder saves the move");
            Check(GameRules.InDisplayOrder(Store.LoadGames()).Select(g => g.Id).SequenceEqual(GameRules.InDisplayOrder(Games).Select(g => g.Id)), "native reorder persists to SQLite");
            Check(!MoveCard(hades.Id, pinned.Id, false), "native reorder keeps favorites pinned");
            var data = new System.Windows.DataObject(); data.SetData(DragFormat, portal.Id.ToString(), false);
            draggingId = portal.Id;
            Check(CanMovePayload(data, hades) && !CanMovePayload(data, pinned), "drag payload authorizes only the same favorite group");
            draggingId = null;
            Check(!CanMovePayload(data, hades), "external drag payload is rejected");
            MoveCard(portal.Id, hades.Id, true);

            await SetView(false, false, false, 510, 740);
            RunModal(() => Dialogs.Edit(this, null), window =>
            {
                Input(window, "Nombre del juego").Text = "Prueba ñ · 日本語";
                Input(window, "Plataforma").Text = "GOG";
                Input(window, "Notas · dónde lo dejaste").Text = "Capítulo 3 — guardar sin perder Unicode";
                Input(window, "Nueva tarea").Text = "Terminar el capítulo";
                Click(window, "+ Añadir tarea");
                Check(Controls<CheckBox>(window).Any(c => (string?)c.Content == "Terminar el capítulo"), "editor adds a checklist task");
                Render(window, "dialog-editor.png");
                Click(window, "Guardar");
            });
            var added = Games.Single(g => g.Title == "Prueba ñ · 日本語");
            Check(added.Platform == "GOG" && added.Tasks.Count == 1 && added.Notes.Contains("Unicode"), "editor saves a game and Unicode notes");
            RunModal(() => Dialogs.Edit(this, added), window =>
            {
                Input(window, "Nombre del juego").Text = "No guardar";
                Controls<CheckBox>(window).Single(c => (string?)c.Content == "Terminar el capítulo").IsChecked = true;
                Click(window, "Cancelar");
            });
            Check(added.Title == "Prueba ñ · 日本語" && !added.Tasks[0].Done, "canceling editor preserves the original game and task");
            RunModal(() => Dialogs.Edit(this, added), window =>
            {
                Controls<CheckBox>(window).Single(c => (string?)c.Content == "Terminar el capítulo").IsChecked = true;
                Click(window, "Guardar");
            });
            added = Games.Single(g => g.Id == added.Id);
            Check(added.Tasks[0].Done, "editor saves completed tasks");
            added.Achievements = [
                new() { Id = "hidden", Name = "Secreto de prueba", Description = "Spoiler de prueba", Hidden = true },
                new() { Id = "public", Name = "Logro visible", Description = "Descripción visible" },
                new() { Id = "done", Name = "Logro desbloqueado", Unlocked = true }
            ];
            RunModal(() => Dialogs.Achievements(this, added), window =>
            {
                Check(Texts(window).Contains("○  Logro secreto") && !Texts(window).Contains("○  Secreto de prueba") && !Texts(window).Contains("✓  Logro desbloqueado"), "achievement dialog hides secrets and completed entries");
                Render(window, "dialog-achievements.png");
                Controls<CheckBox>(window).Single(c => ((string?)c.Content)?.StartsWith("Mostrar nombres") == true).IsChecked = true;
                Check(Texts(window).Contains("○  Secreto de prueba") && Texts(window).Contains("Spoiler de prueba"), "achievement spoiler toggle reveals details");
                Controls<CheckBox>(window).Single(c => (string?)c.Content == "Mostrar solo los pendientes").IsChecked = false;
                Check(Texts(window).Contains("✓  Logro desbloqueado"), "achievement pending filter includes unlocked entries when disabled");
                Click(window, "Cerrar");
            });
            RunModal(() => Dialogs.Settings(this), window =>
            {
                Controls<Slider>(window).Single().Value = .6;
                Controls<ComboBox>(window).Single(c => AutomationProperties.GetName(c) == "Vista de la colección").SelectedIndex = 2;
                Render(window, "dialog-settings.png");
                Click(window, "Cancelar");
            });
            Check(Preferences.BackgroundOpacity == .88 && !Preferences.GridView, "canceling settings restores live opacity and view");
            RunModal(() => Dialogs.Settings(this), window =>
            {
                Controls<ComboBox>(window).Single(c => AutomationProperties.GetName(c) == "Vista de la colección").SelectedIndex = 2;
                Click(window, "Guardar");
            });
            Check(Preferences.GridView && Store.LoadSettings().GridView, "settings save and persist the grid selection");

            string imageFixture = Path.Combine(outputDirectory, "custom-cover-fixture.png");
            var pixels = new byte[220 * 330 * 4];
            for (int i = 0; i < pixels.Length; i += 4) { pixels[i] = 91; pixels[i + 1] = 132; pixels[i + 2] = 51; pixels[i + 3] = 255; }
            var coverFixture = BitmapSource.Create(220, 330, 96, 96, PixelFormats.Bgra32, null, pixels, 220 * 4);
            var coverEncoder = new PngBitmapEncoder(); coverEncoder.Frames.Add(BitmapFrame.Create(coverFixture));
            using (var file = File.Create(imageFixture)) coverEncoder.Save(file);
            added.CustomCover = Covers.Import(imageFixture); Persist();
            string originalCover = added.CustomCover;
            string completeBackup = Path.Combine(outputDirectory, "complete-backup.checkpoint");
            ExportBackup(completeBackup);
            Check(BackupFiles.Read(completeBackup).CustomCovers.Count == 1, "native export embeds the personalized cover");
            RunModal(() => Dialogs.Edit(this, added), window => Click(window, "Eliminar juego"));
            Check(!Games.Any(g => g.Id == added.Id) && DeletedGames.Count == 1 && UndoButton.Visibility == Visibility.Visible, "editor deletion exposes undo without losing its recovery entry");
            await Dispatcher.InvokeAsync(UpdateLayout, DispatcherPriority.ContextIdle);
            Render(this, "widget-undo.png");
            Check(File.Exists(Path.Combine(Covers.DirectoryPath, originalCover)), "deletion retains the personalized cover");
            UndoButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            added = Games.Single(g => g.Id == added.Id);
            Check(added.Tasks[0].Done && added.Notes.Contains("Unicode") && added.CustomCover == originalCover && DeletedGames.Count == 0, "undo button restores notes tasks and cover");
            RunModal(() => Dialogs.Edit(this, added), window => Click(window, "Eliminar juego"));
            RunModal(() => Dialogs.DeletedGames(this), window =>
            {
                Check(Texts(window).Contains(added.Title), "recovery dialog lists the deleted game");
                Render(window, "dialog-recovery.png");
                Controls<Button>(window).Single(b => AutomationProperties.GetName(b) == "Recuperar " + added.Title).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check(Texts(window).Contains("No hay juegos pendientes de recuperar."), "recovery dialog removes a restored entry");
                Click(window, "Cerrar");
            });
            added = Games.Single(g => g.Id == added.Id);
            RunModal(() => Dialogs.Edit(this, added), window => Click(window, "Eliminar juego"));
            var result = ImportBackup(completeBackup); added = Games.Single(g => g.Id == added.Id);
            var restoredCover = await Covers.Get(added);
            Check(result.Added == 1 && result.Skipped == 3 && added.CustomCover != originalCover && restoredCover?.PixelWidth == 220 && restoredCover.PixelHeight == 330, "complete import restores an offline cover under a new local name");
            var coverCard = new CardView(added, false, false);
            await coverCard.LoadCover(Covers);
            Check(coverCard.Cover is not null, "visible card loads its offline cover");
            coverCard.ReleaseCover();
            Check(coverCard.Cover is null, "recycled card releases its cover reference");
            await coverCard.LoadCover(Covers);
            Check(coverCard.Cover is not null, "recycled card can reload its cover");
            Covers.SetEnabled(false); coverCard.ReleaseCover(); await coverCard.LoadCover(Covers);
            Check(Covers.MemoryBytes == 0 && coverCard.Cover is null, "disabled covers clear decoded cache and skip local image loading");
            Covers.SetEnabled(true); coverCard.ReleaseCover(); await coverCard.LoadCover(Covers);
            Check(coverCard.Cover is not null, "reenabling covers restores local image loading");
            var cacheFixtures = new List<string>();
            try
            {
                byte[] png = File.ReadAllBytes(Path.Combine(Covers.DirectoryPath, originalCover!));
                for (int i = 0; i < 40; i++)
                {
                    string name = Covers.SavePrepared(png); cacheFixtures.Add(name);
                    await Covers.Get(new Game { Title = "Cache fixture", CustomCover = name });
                }
                Check(Covers.MemoryBytes > 0 && Covers.MemoryBytes <= CoverCache.MaxMemoryBytes, "decoded cover cache stays within 8 MiB across many images");
            }
            finally { foreach (string name in cacheFixtures) Covers.RemoveCreated(name); }
            added.Notes = "Conservar estos datos actuales"; Persist();
            int filesBefore = Directory.GetFiles(Covers.DirectoryPath, "custom-*.png").Length;
            result = ImportBackup(completeBackup);
            Check(result.Added == 0 && added.Notes == "Conservar estos datos actuales" && Directory.GetFiles(Covers.DirectoryPath, "custom-*.png").Length == filesBefore, "repeated import preserves current data and creates no duplicate images");
            UndoButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(DeletedGames.Count == 1 && added.Notes == "Conservar estos datos actuales", "undo conflict preserves the already imported game");

            var rejected = new Game { Title = "Rollback fixture", CustomCover = originalCover };
            string validFixture = Path.Combine(outputDirectory, "rollback.checkpoint");
            BackupFiles.WriteComplete(validFixture, [rejected], Covers.DirectoryPath);
            string connectionString = new SqliteConnectionStringBuilder { DataSource = Path.Combine(Store.DirectoryPath, "checkpoint.db"), Pooling = false }.ToString();
            void ExecuteSql(string text)
            {
                using var connection = new SqliteConnection(connectionString); connection.Open();
                using var command = connection.CreateCommand(); command.CommandText = text; command.ExecuteNonQuery();
            }
            ExecuteSql($"CREATE TRIGGER reject_fixture BEFORE INSERT ON games WHEN NEW.id='{rejected.Id}' BEGIN SELECT RAISE(ABORT, 'fixture'); END;");
            bool failed = false;
            try { ImportBackup(validFixture); } catch (SqliteException) { failed = true; }
            finally { ExecuteSql("DROP TRIGGER reject_fixture"); }
            Check(failed && Games.Count == 4 && Store.LoadGames().Count == 4 && Directory.GetFiles(Covers.DirectoryPath, "custom-*.png").Length == filesBefore, "failed database import rolls back games and removes staged covers");
            using (var archive = System.IO.Compression.ZipFile.Open(validFixture, System.IO.Compression.ZipArchiveMode.Update))
            {
                var entry = archive.Entries.Single(e => e.FullName.StartsWith("covers/")); string name = entry.FullName; entry.Delete();
                using var output = archive.CreateEntry(name).Open(); output.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
            }
            failed = false;
            try { ImportBackup(validFixture); } catch (Exception ex) when (ex is InvalidDataException or IOException or NotSupportedException or FormatException) { failed = true; }
            Check(failed && Games.Count == 4 && Store.LoadGames().Count == 4 && Directory.GetFiles(Covers.DirectoryPath, "custom-*.png").Length == filesBefore, "invalid PNG is rejected before changing data or creating covers");
            var legacyGame = new Game { Title = "Compatible JSON", Notes = "Importación anterior", CustomCover = originalCover };
            string legacyBackup = Path.Combine(outputDirectory, "compatible.json"); Store.Export(legacyBackup, [legacyGame]);
            result = ImportBackup(legacyBackup);
            Check(result.Added == 1 && Games.Single(g => g.Id == legacyGame.Id).CustomCover is null, "native importer still accepts previous JSON backups");
            Games.RemoveAll(g => g.Id == legacyGame.Id);
            // Return the fixture to the three real demo games; no simulated logs are delivered in the app.
            Games.Remove(added); Persist();
            await RenderSteamSmokeTest(outputDirectory,Check);
            await RenderSocialSmokeTest(outputDirectory,Check);

            var bulk = Enumerable.Range(0, 1000).Select(i => new Game { Title = $"Virtual game {i:0000}", SortOrder = i + 3, CustomCover = originalCover }).ToList();
            Games.AddRange(bulk); await SetView(true, false, false, 510, 740);
            int realized = VisualChildren(GameList).OfType<ListBoxItem>().Count();
            Check(GameList.Items.Count > 300 && realized is > 0 and < 30, "large grid virtualizes rows");
            Check(visibleCards.Count(c => c.Cover is not null) is > 0 and < 100, "large grid only retains covers for realized cards");
            GameList.ScrollIntoView(GameList.Items[^1]);
            await Dispatcher.InvokeAsync(UpdateLayout, DispatcherPriority.ContextIdle);
            Check(VisualChildren(GameList).OfType<ListBoxItem>().Count() < 30, "grid recycling stays bounded after scrolling");
            Check(visibleCards.Count(c => c.Cover is not null) < 100, "scrolling releases offscreen cover references");
            Games.RemoveAll(g => bulk.Contains(g));
            await SetView(false, true, true, 375, 540);
            Render(this, "widget-compact-light.png");
            Check(Store.LoadGames().Count == 3, "dialog fixtures do not remain in the persisted library");
            RunModal(() => Dialogs.Settings(this), window =>
            {
                Controls<CheckBox>(window).Single(c => (string?)c.Content == "Modo ligero (sin carátulas)").IsChecked = true;
                Click(window, "Guardar");
            });
            await Dispatcher.InvokeAsync(UpdateLayout, DispatcherPriority.ContextIdle);
            Check(Preferences.LightweightMode && Store.LoadSettings().LightweightMode && Covers.MemoryBytes == 0, "lightweight mode persists and clears image cache");
            Check(visibleCards.All(c => c.CoverVisibility == Visibility.Collapsed && c.Cover is null) &&
                !VisualChildren(GameList).OfType<Image>().Any(i => i.IsVisible), "lightweight mode hides collection images and preserves game count");
            Render(this, "widget-lightweight.png");
            RunModal(() => Dialogs.Settings(this), window =>
            {
                Controls<CheckBox>(window).Single(c => (string?)c.Content == "Modo ligero (sin carátulas)").IsChecked = false;
                Click(window, "Cancelar");
            });
            Check(Preferences.LightweightMode && Store.LoadSettings().LightweightMode, "canceling settings preserves lightweight mode");
            RunModal(() => Dialogs.Settings(this), window =>
            {
                Controls<CheckBox>(window).Single(c => (string?)c.Content == "Modo ligero (sin carátulas)").IsChecked = false;
                Click(window, "Guardar");
            });
            Check(!Preferences.LightweightMode && visibleCards.All(c => c.CoverVisibility == Visibility.Visible) && Store.LoadGames().Count == 3, "disabling lightweight mode restores cover layout without changing games");
            RunModal(() => Dialogs.Settings(this), window =>
            {
                Controls<ComboBox>(window).Single(c => AutomationProperties.GetName(c) == "Language / Idioma").SelectedIndex = 1;
                Click(window,"Guardar");
            });
            Check(Preferences.Language == "en" && Store.LoadSettings().Language == "en" && (string?)LibraryButton.Content == "Library" && Summary.Text.Contains("in your list"), "settings save English and update the interface immediately");
            await SetView(true,false,false,700,740);
            Render(this,"widget-grid-wide-dark-en.png");
            RunModal(() => Dialogs.Edit(this,Games.First()),window =>
            {
                Check(Controls<TextBox>(window).Any(c => AutomationProperties.GetName(c) == "Game title"), "English game editor has localized accessible controls");
                Render(window,"dialog-editor-en.png"); Click(window,"Cancel");
            });
            RunModal(() => Dialogs.Settings(this),window =>
            {
                Check(Texts(window).Contains("Language") && Texts(window).Contains("Background opacity"), "English settings translate labels");
                Check(Controls<CheckBox>(window).Any(c => (string?)c.Content == "Lightweight mode (no covers)"), "English lightweight setting is localized");
                Render(window,"dialog-settings-en.png");
                Controls<ComboBox>(window).Single(c => AutomationProperties.GetName(c) == "Language / Idioma").SelectedIndex = 0;
                Click(window,"Save");
            });
            Check(Preferences.Language == "es" && (string?)FriendsButton.Content == "Amigos", "switching back to Spanish restores the interface");
            double normalWidth = Width, normalHeight = Height;
            RunModal(() => Dialogs.Settings(this), window =>
            {
                Controls<ComboBox>(window).Single(c => AutomationProperties.GetName(c) == "Vista de la colección").SelectedIndex = 3;
                Click(window,"Guardar");
            });
            await Dispatcher.InvokeAsync(UpdateLayout, DispatcherPriority.ContextIdle);
            GameList.ScrollIntoView(GameList.Items[0]);
            await Dispatcher.InvokeAsync(UpdateLayout, DispatcherPriority.ContextIdle);
            Check(Preferences.MiniatureView && Store.LoadSettings().MiniatureView && Width == 300 && Height == 220, "miniature view saves and uses independent small dimensions");
            Check(GameList.Items.Count == Games.Count(g => g.Tracked) && !VisualChildren(GameList).OfType<Image>().Any() &&
                !VisualChildren(GameList).OfType<Button>().Any() && !VisualChildren(GameList).OfType<ProgressBar>().Any(), "miniature rows contain no covers actions or progress bars");
            Check(HeaderArea.Visibility == Visibility.Collapsed && SummaryArea.Visibility == Visibility.Collapsed &&
                NavigationArea.Visibility == Visibility.Collapsed && FooterArea.Visibility == Visibility.Collapsed && FilterArea.Visibility == Visibility.Collapsed &&
                Covers.MemoryBytes == 0, "miniature hides surrounding chrome and clears cover cache");
            Check(VisualChildren(GameList).OfType<TextBlock>().Any(t => t.Text == Games.First().Title) &&
                VisualChildren(GameList).OfType<TextBlock>().Any(t => t.Text == Games.First().StatusText), "miniature shows game names and states");
            Check(VisualChildren(GameList).OfType<TextBlock>().Where(t => Games.Any(g => g.Title == t.Text)).All(t =>
                t.Foreground is SolidColorBrush brush && brush.Color == ((SolidColorBrush)Application.Current.Resources["TextBrush"]).Color), "miniature names use the readable theme foreground");
            Render(this,"widget-miniature.png");
            void MiniatureKey(Key key)
            {
                GameList.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(GameList),0,key) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
            }
            FocusMiniatureCard(visibleCards[0]); MiniatureKey(Key.End);
            Check(GameList.SelectedItem == visibleCards[^1] && (Keyboard.FocusedElement as FrameworkElement)?.DataContext == visibleCards[^1], "miniature End selects and focuses the last game");
            MiniatureKey(Key.Up);
            Check(GameList.SelectedItem == visibleCards[^2], "miniature Up moves to the previous game");
            MiniatureKey(Key.Home); MiniatureKey(Key.Up);
            Check(GameList.SelectedItem == visibleCards[0], "miniature Home and Up stop at the first game");
            Render(this,"widget-miniature-keyboard.png");
            MiniatureKey(Key.Enter);
            var keyboardRow = VisualChildren(GameList).OfType<Grid>().First(g => g.ContextMenu?.IsOpen == true);
            Check(keyboardRow.ContextMenu.Items.OfType<MenuItem>().Count(i => i.Tag is GameStatus) == 5, "miniature Enter opens the selected game state menu");
            keyboardRow.ContextMenu.IsOpen = false;
            await Dispatcher.InvokeAsync(UpdateLayout, DispatcherPriority.ContextIdle);
            Check((Keyboard.FocusedElement as FrameworkElement)?.DataContext is CardView focused && focused.Model.Id == visibleCards[0].Model.Id, "closing miniature menu restores row focus");
            MiniatureKey(Key.Space);
            keyboardRow = VisualChildren(GameList).OfType<Grid>().First(g => g.ContextMenu?.IsOpen == true);
            Check(keyboardRow.ContextMenu.IsOpen, "miniature Space also opens the state menu");
            keyboardRow.ContextMenu.IsOpen = false;
            await Dispatcher.InvokeAsync(UpdateLayout, DispatcherPriority.ContextIdle);
            FocusMiniatureCard(visibleCards[^1]);
            var retainedGame = visibleCards[^1].Model;
            Refresh();
            await Dispatcher.InvokeAsync(UpdateLayout, DispatcherPriority.ContextIdle);
            Check((GameList.SelectedItem as CardView)?.Model.Id == retainedGame.Id &&
                (Keyboard.FocusedElement as FrameworkElement)?.DataContext == GameList.SelectedItem, "miniature refresh retains selection and row focus by game identity");
            int retainedOrder = retainedGame.SortOrder;
            bool retainedFavorite = retainedGame.Favorite;
            retainedGame.Favorite = true; retainedGame.SortOrder = -100; Refresh();
            await Dispatcher.InvokeAsync(UpdateLayout, DispatcherPriority.ContextIdle);
            Check(GameList.SelectedItem == visibleCards[0] && (Keyboard.FocusedElement as FrameworkElement)?.DataContext == visibleCards[0], "miniature retains the selected game when display order changes");
            retainedGame.Favorite = retainedFavorite; retainedGame.SortOrder = retainedOrder;
            Keyboard.ClearFocus(); Refresh();
            await Dispatcher.InvokeAsync(UpdateLayout, DispatcherPriority.ContextIdle);
            Check((GameList.SelectedItem as CardView)?.Model.Id == retainedGame.Id && Keyboard.FocusedElement is null, "miniature refresh preserves selection without acquiring absent keyboard focus");
            retainedGame.Tracked = false; Refresh();
            Check(GameList.SelectedItem is null, "miniature refresh clears a selection that is no longer in the list");
            retainedGame.Tracked = true; Refresh(); FocusMiniatureCard(visibleCards[0]);
            var keyboardBulk = Enumerable.Range(0,1000).Select(i => new Game { Title = $"Keyboard game {i:0000}", SortOrder = i + 100 }).ToList();
            Games.AddRange(keyboardBulk); Refresh(); FocusMiniatureCard(visibleCards[0]); MiniatureKey(Key.End);
            for (int i = 0; i < 10 && (Keyboard.FocusedElement as FrameworkElement)?.DataContext != visibleCards[^1]; i++)
            { await Task.Delay(10); await Dispatcher.InvokeAsync(UpdateLayout, DispatcherPriority.ContextIdle); }
            Console.WriteLine($"Virtual keyboard: selected={GameList.SelectedItem == visibleCards[^1]}, focused={(Keyboard.FocusedElement as FrameworkElement)?.DataContext == visibleCards[^1]}, realized={VisualChildren(GameList).OfType<ListBoxItem>().Count()}");
            Check(GameList.SelectedItem == visibleCards[^1] && (Keyboard.FocusedElement as FrameworkElement)?.DataContext == visibleCards[^1] &&
                VisualChildren(GameList).OfType<ListBoxItem>().Count() < 30, "miniature End focuses a distant virtualized row in a large collection");
            var distantId = visibleCards[^1].Model.Id;
            Refresh();
            for (int i = 0; i < 10 && (Keyboard.FocusedElement as FrameworkElement)?.DataContext != GameList.SelectedItem; i++)
            { await Task.Delay(10); await Dispatcher.InvokeAsync(UpdateLayout, DispatcherPriority.ContextIdle); }
            Check((GameList.SelectedItem as CardView)?.Model.Id == distantId &&
                (Keyboard.FocusedElement as FrameworkElement)?.DataContext == GameList.SelectedItem &&
                VisualChildren(GameList).OfType<ListBoxItem>().Count() < 30, "miniature refresh restores a distant row without disabling virtualization");
            Games.RemoveAll(g => keyboardBulk.Contains(g)); Refresh(); FocusMiniatureCard(visibleCards[0]);
            var miniatureRow = VisualChildren(GameList).OfType<Grid>().First(g => g.ContextMenu is not null && g.DataContext is CardView);
            var miniatureGame = ((CardView)miniatureRow.DataContext).Model;
            var priorState = miniatureGame.Status;
            miniatureRow.ContextMenu.PlacementTarget = miniatureRow; miniatureRow.ContextMenu.IsOpen = true;
            await Dispatcher.InvokeAsync(UpdateLayout, DispatcherPriority.ContextIdle);
            Check(miniatureRow.ContextMenu.Items.OfType<MenuItem>().Count(i => i.Tag is GameStatus) == 5 &&
                miniatureRow.ContextMenu.Items.OfType<MenuItem>().Single(i => i.Tag is GameStatus s && s == priorState).IsChecked, "miniature menu exposes all states and checks the current one");
            RenderElement(miniatureRow.ContextMenu,Path.Combine(outputDirectory,"miniature-state-menu.png"));
            var miniatureMenu = miniatureRow.ContextMenu;
            var finishState = miniatureMenu.Items.OfType<MenuItem>().Single(i => i.Tag is GameStatus s && s == GameStatus.Finished);
            finishState.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); miniatureMenu.IsOpen = false;
            Check(miniatureGame.Status == GameStatus.Finished && miniatureGame.FinishedAt.HasValue &&
                Store.LoadGames().Single(g => g.Id == miniatureGame.Id).Status == GameStatus.Finished && Preferences.MiniatureView, "miniature state action persists story completion without leaving the view");
            await Dispatcher.InvokeAsync(UpdateLayout, DispatcherPriority.ContextIdle);
            for (int i = 0; i < 10 && !((Keyboard.FocusedElement as FrameworkElement)?.DataContext is CardView stateFocus && stateFocus.Model.Id == miniatureGame.Id); i++)
            { await Task.Delay(10); await Dispatcher.InvokeAsync(UpdateLayout, DispatcherPriority.ContextIdle); }
            Check((Keyboard.FocusedElement as FrameworkElement)?.DataContext is CardView updatedFocus && updatedFocus.Model.Id == miniatureGame.Id, "miniature keeps keyboard focus on the game after its state changes");
            miniatureRow = VisualChildren(GameList).OfType<Grid>().First(g => g.ContextMenu is not null && g.DataContext is CardView c && c.Model.Id == miniatureGame.Id);
            miniatureRow.ContextMenu.PlacementTarget = miniatureRow; miniatureRow.ContextMenu.IsOpen = true;
            await Dispatcher.InvokeAsync(UpdateLayout, DispatcherPriority.ContextIdle);
            miniatureMenu = miniatureRow.ContextMenu;
            var playingState = miniatureMenu.Items.OfType<MenuItem>().Single(i => i.Tag is GameStatus s && s == GameStatus.Playing);
            playingState.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); miniatureMenu.IsOpen = false;
            Check(miniatureGame.Status == GameStatus.Playing && miniatureGame.FinishedAt is null &&
                Store.LoadGames().Single(g => g.Id == miniatureGame.Id).FinishedAt is null, "miniature reopening clears story completion date");
            GameRules.SetStatus(miniatureGame, priorState); Persist(); Refresh();
            Preferences.Language = "en"; ApplyLanguage();
            await Dispatcher.InvokeAsync(UpdateLayout, DispatcherPriority.ContextIdle);
            Check(VisualChildren(GameList).OfType<TextBlock>().Any(t => t.Text == "Playing") &&
                (string?)((MenuItem)Shell.ContextMenu.Items[0]).Header == "Exit miniature view", "miniature states and exit menu switch to English");
            Render(this,"widget-miniature-en.png");
            miniatureRow = VisualChildren(GameList).OfType<Grid>().First(g => g.ContextMenu is not null && g.DataContext is CardView);
            miniatureRow.ContextMenu.PlacementTarget = miniatureRow; miniatureRow.ContextMenu.IsOpen = true;
            await Dispatcher.InvokeAsync(UpdateLayout, DispatcherPriority.ContextIdle);
            Check(miniatureRow.ContextMenu.Items.OfType<MenuItem>().Any(i => (string?)i.Header == "Story finished") &&
                miniatureRow.ContextMenu.Items.OfType<MenuItem>().Any(i => (string?)i.Header == "Exit miniature view"), "miniature game menu opens with localized English states and exit");
            RenderElement(miniatureRow.ContextMenu,Path.Combine(outputDirectory,"miniature-state-menu-en.png"));
            miniatureRow.ContextMenu.IsOpen = false;
            Preferences.Language = "es"; ApplyLanguage();
            Width = 280; Height = 180; Persist();
            Check(Store.LoadSettings().MiniatureWidth == 280 && Store.LoadSettings().MiniatureHeight == 180 &&
                Preferences.Width == normalWidth && Preferences.Height == normalHeight, "miniature resize preserves normal dimensions");
            double priorLeft = Left, priorTop = Top;
            var workArea = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle).WorkingArea;
            var dpi = VisualTreeHelper.GetDpi(this);
            Left = workArea.Right / dpi.DpiScaleX - Width; Top = workArea.Bottom / dpi.DpiScaleY - Height;
            ((MenuItem)Shell.ContextMenu.Items[0]).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Check(!Preferences.MiniatureView && Width == Math.Max(MinWidth,Math.Min(normalWidth,workArea.Width / dpi.DpiScaleX)) &&
                Height == Math.Max(MinHeight,Math.Min(normalHeight,workArea.Height / dpi.DpiScaleY)), "context menu exits miniature and restores normal size within the available work area");
            bool InsideWorkArea() => Left >= workArea.Left / dpi.DpiScaleX - 1 && Top >= workArea.Top / dpi.DpiScaleY - 1 &&
                Left + Width <= workArea.Right / dpi.DpiScaleX + 1 && Top + Height <= workArea.Bottom / dpi.DpiScaleY + 1;
            Check(InsideWorkArea(), "expanding miniature at the screen corner keeps the normal window in the work area");
            Left = workArea.Right / dpi.DpiScaleX - 30; Top = workArea.Bottom / dpi.DpiScaleY - 30;
            workArea = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle).WorkingArea;
            dpi = VisualTreeHelper.GetDpi(this);
            Preferences.MiniatureView = true; ApplyPreferences(); Refresh();
            Check(InsideWorkArea(), "entering miniature corrects a partly offscreen window");
            Hide(); Left = workArea.Right / dpi.DpiScaleX + 50;
            workArea = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle).WorkingArea;
            dpi = VisualTreeHelper.GetDpi(this); ShowWidget();
            Check(InsideWorkArea(), "showing the widget corrects an offscreen saved position");
            Preferences.MiniatureView = false; ApplyPreferences(); Refresh(); Left = priorLeft; Top = priorTop; Persist();
            Preferences.GridView = true; Preferences.Compact = false; ApplyPreferences(); Refresh();
            CycleView(); Check(Preferences.MiniatureView, "view cycle enters miniature after grid");
            CycleView(); Check(!Preferences.MiniatureView && !Preferences.GridView && !Preferences.Compact, "view cycle exits miniature to list");
            File.WriteAllText(Path.Combine(outputDirectory, "smoke.json"), JsonSerializer.Serialize(new { ok = true, checks = checks.Count, assertions = checks }, DataJson.Options));
            Console.WriteLine($"WPF smoke test passed: {checks.Count} checks, {outputDirectory}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex); Environment.ExitCode = 1;
            File.WriteAllText(Path.Combine(outputDirectory, "smoke.json"), JsonSerializer.Serialize(new { ok = false, checks = checks.Count, error = ex.ToString() }, DataJson.Options));
        }
        finally { Exit(); }
    }
    private void RunModal(Action open, Action<Window> exercise)
    {
        Exception? failure = null;
        Dispatcher.BeginInvoke(() =>
        {
            var window = OwnedWindows.Cast<Window>().Last(w => w.IsVisible);
            try { window.UpdateLayout(); exercise(window); }
            catch (Exception ex) { failure = ex; }
            finally { if (window.IsVisible) window.Close(); }
        }, DispatcherPriority.ContextIdle);
        open();
        if (failure is not null) throw new InvalidOperationException("Modal dialog test failed.", failure);
    }
    private static IEnumerable<T> Controls<T>(Window window) where T : DependencyObject
    { window.UpdateLayout(); return VisualChildren(window).OfType<T>(); }
    private static TextBox Input(Window window, string name) => Controls<TextBox>(window).Single(c => AutomationProperties.GetName(c) == name);
    private static IEnumerable<string> Texts(Window window) => Controls<TextBlock>(window).Select(t => t.Text);
    private static void Click(Window window, string text) => Controls<Button>(window).Single(b => (string?)b.Content == text).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static void RenderElement(FrameworkElement element, string path)
    {
        element.UpdateLayout();
        var image = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth), (int)Math.Ceiling(element.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        image.Render(element); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image));
        using var file = File.Create(path); encoder.Save(file);
    }
}
