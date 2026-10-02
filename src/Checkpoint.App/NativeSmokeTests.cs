using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
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
                Render(window,"dialog-settings-en.png");
                Controls<ComboBox>(window).Single(c => AutomationProperties.GetName(c) == "Language / Idioma").SelectedIndex = 0;
                Click(window,"Save");
            });
            Check(Preferences.Language == "es" && (string?)FriendsButton.Content == "Amigos", "switching back to Spanish restores the interface");
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
