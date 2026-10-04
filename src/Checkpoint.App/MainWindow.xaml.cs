// Coordina biblioteca, preferencias, navegación, sincronización y ciclo de vida de la ventana.
// Las clases parciales distribuyen los controladores por funcionalidad sin duplicar el estado.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Checkpoint.Core;
using Forms = System.Windows.Forms;

namespace Checkpoint.App;

public partial class MainWindow : Window
{
    internal readonly SqliteStore Store;
    internal readonly List<Game> Games;
    internal List<DeletedGame> DeletedGames;
    internal readonly Settings Preferences;
    internal readonly SteamClient Steam;
    internal readonly CoverCache Covers;
    private readonly Forms.NotifyIcon tray;
    private readonly DispatcherTimer timer = new();
    private readonly DispatcherTimer boundsTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(400),
    };
    private readonly CancellationTokenSource shutdown = new();
    private readonly Dictionary<bool, (string Search, int Filter)> sectionFilters = new();
    internal bool ShowingLibrary => allLibrary;
    private bool ready,
        allLibrary,
        syncing,
        exiting;
    private HwndSource? source;
    private bool hotkeyRegistered;
    private const int HotkeyId = 4021;
    private const string DragFormat = "Checkpoint.GameId";
    private readonly DispatcherTimer dragScrollTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(75),
    };
    private List<CardView> visibleCards = [];
    private int gridColumns;
    private Guid? pendingDrag,
        draggingId;
    private Point dragOrigin;
    private Border? dropBorder;
    private int dragScrollDirection;
    private bool? miniatureApplied;
    private bool fullWindowApplied;
    internal bool MiniatureUsesLibrary =>
        Preferences.MiniatureView
        && Preferences.ActiveList == "all"
        && !Games.Any(g => GameLists.Visible(g, "all"));
    internal bool IsFullWindow => Preferences.FullWindow && !Preferences.MiniatureView;
    internal double EffectiveOpacity =>
        IsFullWindow ? 1
        : Preferences.MiniatureView ? .35
        : Preferences.BackgroundOpacity;

    // Cada modo conserva sus propias medidas y preferencias para poder volver al tamaño anterior.
    internal void SetWindowMode(int mode)
    {
        if (mode < 0 || mode > 2)
            return;
        // Restore from a maximized Windows state before assigning per-mode dimensions.
        if (WindowState != WindowState.Normal)
            WindowState = WindowState.Normal;
        Preferences.FullWindow = mode == 0;
        Preferences.MiniatureView = mode == 2;
        ApplyPreferences();
        Persist();
        Refresh();
    }

    private void FillWorkArea(Forms.Screen? target = null)
    {
        var scale = VisualTreeHelper.GetDpi(this);
        var area = (
            target ?? Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle)
        ).WorkingArea;
        Left = area.Left / scale.DpiScaleX;
        Top = area.Top / scale.DpiScaleY;
        Width = area.Width / scale.DpiScaleX;
        Height = area.Height / scale.DpiScaleY;
    }

    public MainWindow(string directory, bool demo)
    {
        Store = new(directory);
        Preferences = Store.LoadSettings();
        I18n.SetLanguage(Preferences.Language);
        Games = Store.LoadGames();
        DeletedGames = Store.LoadDeletedGames();
        Steam = new(directory);
        Covers = new(directory);
        if (string.IsNullOrEmpty(Preferences.ServiceUrl))
        {
            var config = Path.Combine(AppContext.BaseDirectory, "service-config.json");
            if (File.Exists(config))
            {
                using var json = JsonDocument.Parse(File.ReadAllText(config));
                if (json.RootElement.TryGetProperty("serviceUrl", out var url))
                    Preferences.ServiceUrl = url.GetString() ?? "";
            }
        }
        LoadLanguageResources(Preferences.Language);
        InitializeComponent();
        StateChanged += (_, _) =>
        {
            if (WindowState == WindowState.Minimized && Preferences.MinimizeToTray)
                Hide();
        };
        InitializeSocial();
        boundsTimer.Tick += (_, _) =>
        {
            boundsTimer.Stop();
            CaptureBounds();
            Store.SaveSettings(Preferences);
        };
        PreviewMouseMove += DragHandleMove;
        PreviewMouseLeftButtonUp += (_, _) => pendingDrag = null;
        dragScrollTimer.Tick += (_, _) =>
        {
            var scroll = FindVisual<ScrollViewer>(GameList);
            if (scroll is not null)
                scroll.ScrollToVerticalOffset(scroll.VerticalOffset + dragScrollDirection * 24);
        };
        GameList.PreviewDragOver += (_, e) =>
        {
            if (draggingId is null)
                return;
            double y = e.GetPosition(GameList).Y;
            dragScrollDirection =
                y < 35 ? -1
                : y > GameList.ActualHeight - 35 ? 1
                : 0;
        };
        GameList.DragLeave += (_, e) =>
        {
            var point = e.GetPosition(GameList);
            if (
                point.Y < 0
                || point.Y > GameList.ActualHeight
                || point.X < 0
                || point.X > GameList.ActualWidth
            )
                dragScrollDirection = 0;
        };
        MinWidth = Preferences.MiniatureView ? 240 : 365;
        MinHeight = Preferences.MiniatureView ? 90 : 440;
        Width = Math.Clamp(
            Preferences.MiniatureView ? Preferences.MiniatureWidth : Preferences.Width,
            MinWidth,
            SystemParameters.VirtualScreenWidth
        );
        Height = Math.Clamp(
            Preferences.MiniatureView ? Preferences.MiniatureHeight : Preferences.Height,
            MinHeight,
            SystemParameters.VirtualScreenHeight
        );
        if (Preferences.Left is double left && Preferences.Top is double top)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            var screens = Forms.Screen.AllScreens;
            var screen =
                screens.FirstOrDefault(s =>
                    left >= s.WorkingArea.Left
                    && left < s.WorkingArea.Right
                    && top >= s.WorkingArea.Top
                    && top < s.WorkingArea.Bottom
                ) ?? Forms.Screen.PrimaryScreen!;
            // Stored WPF device-independent coordinates are clamped again after obtaining the current DPI.
            Left = left;
            Top = top;
        }
        tray = new Forms.NotifyIcon
        {
            Text = "Checkpoint · Ctrl+Alt+C",
            Icon =
                System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!)
                ?? System.Drawing.SystemIcons.Application,
            Visible = true,
        };
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(
            I18n.T("Mostrar / ocultar"),
            null,
            (_, _) => Dispatcher.Invoke(ToggleVisible)
        );
        menu.Items.Add(
            I18n.T("Añadir juego"),
            null,
            (_, _) =>
                Dispatcher.Invoke(() =>
                {
                    ShowWidget();
                    Dialogs.Edit(this, null);
                })
        );
        menu.Items.Add(I18n.T("Salir"), null, (_, _) => Dispatcher.Invoke(Exit));
        tray.ContextMenuStrip = menu;
        tray.DoubleClick += (_, _) => Dispatcher.Invoke(ShowWidget);
        Loaded += (_, _) =>
        {
            ClampToScreen();
            if (demo && Games.Count == 0)
                AddExamples();
            ready = true;
            ApplyPreferences();
            Refresh();
            if (Preferences.StartWithWindows)
            {
                try
                {
                    Dialogs.RestoreStartupIfMissing();
                }
                catch (Exception ex)
                {
                    Notice(I18n.T("No se pudo restaurar el inicio con Windows: ") + I18n.Error(ex));
                }
            }
            timer.Tick += async (_, _) =>
            {
                if (Steam.Session is not null && !syncing)
                    await Sync(true);
            };
            timer.Start();
            StartSocial();
            StartGameDetection();
            if (App.UseCss)
                StartWebInterface();
            _ = CheckUpdatesOnStartup();
            if (Steam.Session is not null)
                _ = Sync(true);
        };
        PreviewKeyDown += (_, e) =>
        {
            if (App.UseCss)
                return;
            if (e.Key == Key.Escape && IsFullWindow)
            {
                SetWindowMode(1);
                e.Handled = true;
                return;
            }
            if (MatchesShortcut(e, "add"))
            {
                Dialogs.Edit(this, null);
                e.Handled = true;
            }
            if (MatchesShortcut(e, "search"))
            {
                FocusCollectionSearch();
                e.Handled = true;
            }
            if (MatchesShortcut(e, "view"))
            {
                CycleView();
                e.Handled = true;
            }
            if (MatchesShortcut(e, "undo") && Keyboard.FocusedElement is not TextBoxBase)
            {
                UndoLastDeletion();
                e.Handled = true;
            }
            if (MatchesShortcut(e, "hide"))
            {
                Hide();
                e.Handled = true;
            }
        };
        SourceInitialized += (_, _) =>
        {
            source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source.AddHook(WindowMessage);
            ChangeGlobalShortcut(Shortcuts.Effective(Preferences.Shortcuts)["global"]);
        };
    }

    internal static string ShortcutGesture(KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        var name = key switch
        {
            Key.Up => "ArrowUp",
            Key.Down => "ArrowDown",
            Key.PageUp => "PageUp",
            Key.PageDown => "PageDown",
            Key.Return => "Enter",
            Key.Space => "Space",
            _ => key.ToString(),
        };
        if (key >= Key.D0 && key <= Key.D9)
            name = ((int)key - (int)Key.D0).ToString();
        var mods = Keyboard.Modifiers;
        return string.Join(
            '+',
            new[]
            {
                mods.HasFlag(ModifierKeys.Control) ? "Ctrl" : null,
                mods.HasFlag(ModifierKeys.Alt) ? "Alt" : null,
                mods.HasFlag(ModifierKeys.Shift) ? "Shift" : null,
            }
                .Where(x => x is not null)
                .Append(name)
        );
    }

    private bool MatchesShortcut(KeyEventArgs e, string action) =>
        !Keyboard.Modifiers.HasFlag(ModifierKeys.Windows)
        && ShortcutGesture(e) == Shortcuts.Effective(Preferences.Shortcuts)[action];

    internal bool ChangeGlobalShortcut(string gesture)
    {
        if (source is null)
            return true;
        bool Register(string value)
        {
            var parts = value.Split('+');
            uint modifiers = 0x4000;
            if (parts.Contains("Ctrl"))
                modifiers |= 2;
            if (parts.Contains("Alt"))
                modifiers |= 1;
            if (parts.Contains("Shift"))
                modifiers |= 4;
            var name = parts[^1] switch
            {
                "ArrowUp" => "Up",
                "ArrowDown" => "Down",
                "Enter" => "Return",
                _ => parts[^1],
            };
            if (name.Length == 1 && char.IsDigit(name[0]))
                name = "D" + name;
            return Enum.TryParse<Key>(name, out var key)
                && RegisterHotKey(
                    source.Handle,
                    HotkeyId,
                    modifiers,
                    (uint)KeyInterop.VirtualKeyFromKey(key)
                );
        }
        var previous = Shortcuts.Effective(Preferences.Shortcuts)["global"];
        if (hotkeyRegistered)
            UnregisterHotKey(source.Handle, HotkeyId);
        hotkeyRegistered = Register(gesture);
        if (hotkeyRegistered)
            return true;
        hotkeyRegistered = Register(previous);
        return false;
    }

    private void ClampToScreen(Forms.Screen? targetScreen = null)
    {
        var scale = VisualTreeHelper.GetDpi(this);
        var screen = (
            targetScreen ?? Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle)
        ).WorkingArea;
        double left = screen.Left / scale.DpiScaleX,
            top = screen.Top / scale.DpiScaleY;
        Width = Math.Min(Width, screen.Width / scale.DpiScaleX);
        Height = Math.Min(Height, screen.Height / scale.DpiScaleY);
        Left = Math.Clamp(
            double.IsFinite(Left) ? Left : left,
            left,
            Math.Max(left, (screen.Right / scale.DpiScaleX) - Width)
        );
        Top = Math.Clamp(
            double.IsFinite(Top) ? Top : top,
            top,
            Math.Max(top, (screen.Bottom / scale.DpiScaleY) - Height)
        );
    }

    private IntPtr WindowMessage(
        IntPtr hwnd,
        int msg,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled
    )
    {
        if (msg == 0x0312 && wParam.ToInt32() == HotkeyId)
        {
            ToggleVisible();
            handled = true;
        }
        return IntPtr.Zero;
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hwnd, int id);

    internal void ApplyPreferences()
    {
        if (miniatureApplied != Preferences.MiniatureView || fullWindowApplied != IsFullWindow)
        {
            var currentScreen = ready
                ? Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle)
                : null;
            if (miniatureApplied.HasValue)
                CaptureBounds();
            bool leavingFull = fullWindowApplied;
            miniatureApplied = Preferences.MiniatureView;
            fullWindowApplied = IsFullWindow;
            MinWidth = Preferences.MiniatureView ? 240 : 365;
            MinHeight = Preferences.MiniatureView ? 90 : 440;
            Width = Math.Clamp(
                Preferences.MiniatureView ? Preferences.MiniatureWidth : Preferences.Width,
                MinWidth,
                SystemParameters.VirtualScreenWidth
            );
            Height = Math.Clamp(
                Preferences.MiniatureView ? Preferences.MiniatureHeight : Preferences.Height,
                MinHeight,
                SystemParameters.VirtualScreenHeight
            );
            if (fullWindowApplied)
                FillWorkArea(currentScreen);
            else
            {
                if (leavingFull)
                {
                    Left = Preferences.Left ?? Left;
                    Top = Preferences.Top ?? Top;
                }
                if (ready)
                    ClampToScreen(currentScreen);
            }
            if (Preferences.MiniatureView)
            {
                friendsVisible = false;
                allLibrary = false;
                Search.Clear();
                StatusFilter.SelectedIndex = 0;
            }
        }
        bool miniature = Preferences.MiniatureView;
        HeaderArea.Visibility =
            SummaryArea.Visibility =
            NavigationArea.Visibility =
            FooterArea.Visibility =
                miniature ? Visibility.Collapsed : Visibility.Visible;
        MiniDragHandle.Visibility = miniature ? Visibility.Visible : Visibility.Collapsed;
        MiniDragHandle.Cursor = Preferences.PositionLocked ? Cursors.Arrow : Cursors.SizeAll;
        Shell.Padding = miniature ? new Thickness(10) : new Thickness(22, 17, 22, 16);
        ResizeGrip.Margin = miniature ? new Thickness(0, 0, -6, -6) : new Thickness(0, 0, -17, -12);
        if (miniature)
        {
            var menu = new System.Windows.Controls.ContextMenu();
            AddMiniatureWindowActions(menu);
            Shell.ContextMenu = menu;
        }
        else
            Shell.ContextMenu = null;
        var resources = Application.Current.Resources;
        Preferences.MiniatureTextSize = Math.Clamp(Preferences.MiniatureTextSize, 12, 20);
        resources["MiniatureNameSize"] = (double)Preferences.MiniatureTextSize;
        resources["MiniatureStateSize"] = (double)Preferences.MiniatureTextSize - 2;
        resources["MiniatureRowHeight"] = (double)Preferences.MiniatureTextSize + 16;
        bool light = Themes.IsLight(Preferences);
        resources["TextBrush"] = Brush(light ? "#FF152338" : "#FFF2F4FA");
        resources["MutedBrush"] = Brush(light ? "#FF4B5D73" : "#FFADB6CA");
        resources["PanelBrush"] = Brush(light ? "#60FFFFFF" : "#40242E44");
        resources["InputBrush"] = Brush(light ? "#FFE7EEF4" : "#FF212A3D");
        resources["LineBrush"] = Brush(light ? "#607D8EA1" : "#405F6B85");
        resources["AccentBrush"] = Brush(light ? "#FF0B7554" : "#FF8CEBC6");
        var rgb = light ? Color.FromRgb(238, 244, 248) : Color.FromRgb(17, 24, 39);
        rgb.A = (byte)Math.Round(Math.Clamp(EffectiveOpacity, .35, 1) * 255);
        Shell.Background = new SolidColorBrush(rgb);
        Topmost = miniature || Preferences.AlwaysOnTop;
        PinButton.Content = Topmost ? "◆" : "◇";
        ResizeGrip.Visibility =
            Preferences.PositionLocked || IsFullWindow ? Visibility.Collapsed : Visibility.Visible;
        timer.Interval = TimeSpan.FromMinutes(Math.Clamp(Preferences.SyncMinutes, 15, 120));
        Covers.SetEnabled(!Preferences.LightweightMode && !miniature);
        ViewButton.Content =
            miniature ? "☷"
            : Preferences.GridView ? "▦"
            : Preferences.Compact ? "≡"
            : "▤";
        string view =
            miniature ? I18n.T("Miniatura")
            : Preferences.GridView ? I18n.T("Cuadrícula")
            : Preferences.Compact ? I18n.T("Compacta")
            : I18n.T("Lista");
        ViewButton.ToolTip = (
            I18n.IsEnglish ? $"View: {view} · switch with F6" : $"Vista: {view} · cambiar con F6"
        );
        System.Windows.Automation.AutomationProperties.SetName(
            ViewButton,
            (I18n.IsEnglish ? $"Change view, current: {view}" : $"Cambiar vista, actual: {view}")
        );
        web?.Publish();
    }

    internal static SolidColorBrush Brush(string color) =>
        (SolidColorBrush)new BrushConverter().ConvertFromString(color)!;

    internal void Persist()
    {
        CaptureBounds();
        Store.Save(Games, Preferences);
        SchedulePublications();
        ScheduleCoverContributions();
    }

    internal void PersistListChange(string previous, string? next)
    {
        CaptureBounds();
        Store.SaveListChange(Games, Preferences, previous, next);
        DeletedGames = Store.LoadDeletedGames();
        SchedulePublications();
    }

    // Guarda las medidas del modo activo sin sobrescribir las dimensiones de los otros modos.
    private void CaptureBounds()
    {
        if (
            WindowState == WindowState.Minimized
            || fullWindowApplied
            || inlineMiniatureBounds is not null
        )
            return;
        if (miniatureApplied == true)
        {
            Preferences.MiniatureWidth = Width;
            Preferences.MiniatureHeight = Height;
        }
        else
        {
            Preferences.Width = Width;
            Preferences.Height = Height;
        }
        Preferences.Left = Left;
        Preferences.Top = Top;
    }

    // Reconstruye los modelos visibles a partir de filtros, lista y preferencias, y publica el estado HTML.
    internal void Refresh()
    {
        if (!ready)
            return;
        Preferences.GameLists = GameLists.Normalize(
            (Preferences.GameLists ?? []).Concat(Games.SelectMany(g => g.Lists))
        );
        if (
            Preferences.ActiveList != "all"
            && Preferences.ActiveList != "private"
            && !Preferences.GameLists.Any(n => "custom:" + n == Preferences.ActiveList)
        )
            Preferences.ActiveList = "all";
        var selectedId = Preferences.MiniatureView
            ? (GameList.SelectedItem as CardView)?.Model.Id
            : null;
        bool restoreFocus = Preferences.MiniatureView && GameList.IsKeyboardFocusWithin;
        var sectionGames = Games
            .Where(g => allLibrary || GameLists.Visible(g, Preferences.ActiveList))
            .ToArray();
        int finished = sectionGames.Count(g => g.Status == GameStatus.Finished);
        Summary.Text = I18n.IsEnglish
            ? $"{sectionGames.Length} {(allLibrary ? "in Library" : "in your list")}  ·  {finished} stories finished"
            : $"{sectionGames.Length} {(allLibrary ? "en Biblioteca" : "en tu lista")}  ·  {finished} historias terminadas";
        bool showLibrary = allLibrary || MiniatureUsesLibrary;
        var filtered = GameRules
            .InDisplayOrder(
                Games
                    .Where(g => showLibrary || GameLists.Visible(g, Preferences.ActiveList))
                    .Where(g =>
                        g.Title.Contains(Search.Text, StringComparison.CurrentCultureIgnoreCase)
                    )
                    .Where(g =>
                        StatusFilter.SelectedIndex <= 0
                        || (int)g.Status == StatusFilter.SelectedIndex - 1
                    )
            )
            .ToList();
        visibleCards = filtered
            .Select(g => new CardView(
                g,
                Preferences.Compact,
                Themes.IsLight(Preferences),
                Preferences.LightweightMode || Preferences.MiniatureView
            ))
            .ToList();
        BindCards();
        EmptyPanel.Visibility =
            !Preferences.MiniatureView && filtered.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        EmptyTitle.Text = I18n.T(
            allLibrary ? "Sin juegos en Biblioteca" : "Sin juegos en esta lista"
        );
        EmptyText.Text =
            Games.Count == 0
                ? I18n.T(
                    "Añade un juego o importa tu biblioteca de Steam. Elige después cuáles quieres tener a mano."
                )
            : allLibrary
                ? I18n.T("No hay juegos con esos filtros. Prueba otra búsqueda o añade un juego.")
            : I18n.T(
                "Añade juegos a Mi lista desde su ficha en la biblioteca, o prueba otra búsqueda."
            );
        ExampleButton.Visibility = Games.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        TrackedButton.Foreground = (System.Windows.Media.Brush)
            Application.Current.Resources[allLibrary ? "TextBrush" : "AccentBrush"];
        LibraryButton.Foreground = (System.Windows.Media.Brush)
            Application.Current.Resources[allLibrary ? "AccentBrush" : "TextBrush"];
        ConnectionText.Text = Steam.Session is null
            ? I18n.T("● Biblioteca local")
            : I18n.T("● Steam vinculado");
        if (Social?.Session is not null)
            ConnectionText.Text = Steam.Session is null
                ? I18n.T("● Checkpoint conectado")
                : I18n.T("● Checkpoint y Steam conectados");
        SyncButton.IsEnabled = !syncing;
        UndoButton.Visibility = DeletedGames.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        ApplySocialTab();
        if (
            selectedId.HasValue
            && visibleCards.FirstOrDefault(c => c.Model.Id == selectedId) is { } selected
        )
        {
            GameList.SelectedItem = selected;
            if (restoreFocus)
                FocusMiniatureCard(
                    selected,
                    expectedFocus: Keyboard.FocusedElement,
                    guardFocus: true
                );
        }
        web?.Publish();
    }

    private async void CoverLoaded(object sender, RoutedEventArgs e)
    {
        var image = (FrameworkElement)sender;
        if (Preferences.LightweightMode || Preferences.MiniatureView)
        {
            CoverUnloaded(sender, e);
            return;
        }
        if (image.Tag is CardView previous && previous != image.DataContext)
            previous.ReleaseCover();
        if (image.DataContext is CardView card)
        {
            image.Tag = card;
            await card.LoadCover(Covers);
        }
    }

    private void CoverUnloaded(object sender, RoutedEventArgs e)
    {
        var image = (FrameworkElement)sender;
        if (image.Tag is CardView card)
            card.ReleaseCover();
        image.Tag = null;
    }

    private void CoverContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (((FrameworkElement)sender).IsLoaded)
            CoverLoaded(sender, new RoutedEventArgs());
    }

    internal void Notice(string text)
    {
        NoticeText.Text = text;
        web?.Publish();
    }

    private void FilterChanged(object sender, RoutedEventArgs e) => Refresh();

    private void SwitchCollection(bool library)
    {
        if (allLibrary != library)
        {
            sectionFilters[allLibrary] = (Search.Text, StatusFilter.SelectedIndex);
            allLibrary = library;
            var saved = sectionFilters.GetValueOrDefault(library, ("", 0));
            Search.Text = saved.Item1;
            StatusFilter.SelectedIndex = saved.Item2;
        }
        friendsVisible = false;
        Refresh();
    }

    private void OpenCollection(bool library)
    {
        bool home = !friendsVisible && allLibrary == library;
        SwitchCollection(library);
        if (!home)
            return;
        bool save = !library && Preferences.ActiveList != "all";
        if (!library)
            Preferences.ActiveList = "all";
        Search.Clear();
        StatusFilter.SelectedIndex = 0;
        sectionFilters[library] = ("", 0);
        GameList.SelectedItems.Clear();
        FindVisual<ScrollViewer>(GameList)?.ScrollToTop();
        if (save)
            Persist();
        Refresh();
        web?.Event(new { kind = "section-home" });
    }

    private void TrackedClick(object sender, RoutedEventArgs e) => OpenCollection(false);

    private void LibraryClick(object sender, RoutedEventArgs e) => OpenCollection(true);

    private void AddClick(object sender, RoutedEventArgs e) => Dialogs.Edit(this, null);

    private void SettingsClick(object sender, RoutedEventArgs e) => Dialogs.Settings(this);

    private void ViewClick(object sender, RoutedEventArgs e) => CycleView();

    private void FocusMiniatureCard(
        CardView card,
        int retry = 0,
        IInputElement? expectedFocus = null,
        bool guardFocus = false
    )
    {
        GameList.SelectedItem = card;
        GameList.ScrollIntoView(card);
        GameList.UpdateLayout();
        if (
            GameList.ItemContainerGenerator.ContainerFromItem(card) is ListBoxItem container
            && FindVisual<Grid>(container) is { } row
            && row.DataContext == card
        )
        {
            row.Focus();
            return;
        }
        // A distant virtualized container may be created on the next layout pass.
        if (retry < 3)
            Dispatcher.BeginInvoke(
                () =>
                {
                    if (
                        Preferences.MiniatureView
                        && GameList.SelectedItem == card
                        && (
                            !guardFocus
                            || Keyboard.FocusedElement == expectedFocus
                            || GameList.IsKeyboardFocusWithin
                        )
                    )
                        FocusMiniatureCard(card, retry + 1, expectedFocus, guardFocus);
                },
                System.Windows.Threading.DispatcherPriority.ContextIdle
            );
    }

    private void MiniatureKeyDown(object sender, KeyEventArgs e)
    {
        if (
            !Preferences.MiniatureView
            || !GameList.IsKeyboardFocusWithin
            || visibleCards.Count == 0
        )
            return;
        var current =
            (Keyboard.FocusedElement as FrameworkElement)?.DataContext as CardView
            ?? GameList.SelectedItem as CardView;
        int index = current is null ? -1 : visibleCards.IndexOf(current);
        var configured = Shortcuts.Effective(Preferences.Shortcuts);
        var command = configured.FirstOrDefault(p => p.Value == ShortcutGesture(e)).Key;
        var navigation = command switch
        {
            "up" => Key.Up,
            "down" => Key.Down,
            "first" => Key.Home,
            "last" => Key.End,
            "pageUp" => Key.PageUp,
            "pageDown" => Key.PageDown,
            _ => Key.None,
        };
        if (navigation != Key.None)
        {
            int pageRows = 1;
            if (navigation is Key.PageUp or Key.PageDown)
            {
                var container = current is null
                    ? null
                    : GameList.ItemContainerGenerator.ContainerFromItem(current) as ListBoxItem;
                double rowHeight = container is { ActualHeight: > 0 }
                    ? container.ActualHeight + container.Margin.Top + container.Margin.Bottom
                    : Preferences.MiniatureTextSize + 31;
                double viewport =
                    FindVisual<ScrollViewer>(GameList)?.ViewportHeight ?? GameList.ActualHeight;
                pageRows = Math.Max(1, (int)Math.Floor(viewport / rowHeight));
            }
            int target = navigation switch
            {
                Key.Home => 0,
                Key.End => visibleCards.Count - 1,
                Key.Up => Math.Max(0, index - 1),
                Key.Down => Math.Min(visibleCards.Count - 1, index + 1),
                Key.PageUp => Math.Max(0, index - pageRows),
                _ => Math.Min(visibleCards.Count - 1, Math.Max(0, index) + pageRows),
            };
            FocusMiniatureCard(visibleCards[target]);
            e.Handled = true;
        }
        else if (
            command == "gameMenu"
            || e.Key == Key.Space && Keyboard.Modifiers == ModifierKeys.None
        )
        {
            var card = index < 0 ? visibleCards[0] : visibleCards[index];
            FocusMiniatureCard(card);
            if (
                GameList.ItemContainerGenerator.ContainerFromItem(card) is ListBoxItem container
                && FindVisual<Grid>(container) is { ContextMenu: { } menu } row
            )
            {
                menu.PlacementTarget = row;
                menu.IsOpen = true;
                e.Handled = true;
            }
        }
        else if (MatchesShortcut(e, "edit") && current is not null)
        {
            e.Handled = true;
            EditMiniatureGame(current.Model.Id);
        }
    }

    private void EditMiniatureGame(Guid gameId)
    {
        if (Games.FirstOrDefault(g => g.Id == gameId) is not { } game)
            return;
        Dialogs.Edit(this, game);
        if (
            Preferences.MiniatureView
            && visibleCards.FirstOrDefault(c => c.Model.Id == gameId) is { } current
        )
            FocusMiniatureCard(current, expectedFocus: Keyboard.FocusedElement, guardFocus: true);
    }

    private void MiniatureMenuClosed(object sender, RoutedEventArgs e)
    {
        var menu = (System.Windows.Controls.ContextMenu)sender;
        if (menu.Tag is not Guid gameId)
            return;
        menu.Tag = null;
        var selectionAtClose = (GameList.SelectedItem as CardView)?.Model.Id ?? gameId;
        var focusAtClose = Keyboard.FocusedElement;
        Dispatcher.BeginInvoke(
            () =>
            {
                if (
                    Preferences.MiniatureView
                    && (GameList.SelectedItem as CardView)?.Model.Id == selectionAtClose
                    && (
                        Keyboard.FocusedElement == focusAtClose
                        || Keyboard.FocusedElement is null
                        || (Keyboard.FocusedElement as FrameworkElement)?.DataContext
                            is CardView focused
                            && focused.Model.Id == gameId
                    )
                    && visibleCards.FirstOrDefault(c => c.Model.Id == gameId) is { } current
                )
                    FocusMiniatureCard(
                        current,
                        expectedFocus: Keyboard.FocusedElement,
                        guardFocus: true
                    );
            },
            System.Windows.Threading.DispatcherPriority.ContextIdle
        );
    }

    private void MiniatureMenuOpened(object sender, RoutedEventArgs e)
    {
        var menu = (System.Windows.Controls.ContextMenu)sender;
        menu.Items.Clear();
        if (
            menu.PlacementTarget is not FrameworkElement { DataContext: CardView card }
            || !Preferences.MiniatureView
        )
            return;
        GameList.SelectedItem = card;
        menu.Tag = card.Model.Id;
        foreach (var state in Enum.GetValues<GameStatus>())
        {
            var item = new System.Windows.Controls.MenuItem
            {
                Header = Labels.Status(state),
                Tag = state,
                IsCheckable = true,
                IsChecked = card.Model.Status == state,
            };
            item.Click += (_, _) =>
            {
                var game = Games.FirstOrDefault(g => g.Id == card.Model.Id);
                if (game is null || game.Status == state)
                    return;
                GameRules.SetStatus(game, state);
                Persist();
                Refresh();
            };
            menu.Items.Add(item);
        }
        menu.Items.Add(new Separator());
        var edit = new System.Windows.Controls.MenuItem
        {
            Header = I18n.T("Editar juego"),
            InputGestureText = Shortcuts.Effective(Preferences.Shortcuts)["edit"],
        };
        edit.Click += (_, _) =>
        {
            menu.IsOpen = false;
            EditMiniatureGame(card.Model.Id);
        };
        menu.Items.Add(edit);
        menu.Items.Add(new Separator());
        AddMiniatureWindowActions(menu);
    }

    private void AddMiniatureWindowActions(System.Windows.Controls.ContextMenu menu)
    {
        var restore = new System.Windows.Controls.MenuItem
        {
            Header = I18n.T("Salir de miniatura"),
        };
        restore.Click += (_, _) =>
        {
            menu.IsOpen = false;
            Preferences.MiniatureView = false;
            ApplyPreferences();
            Persist();
            Refresh();
        };
        var settings = new System.Windows.Controls.MenuItem { Header = I18n.T("Ajustes") };
        settings.Click += (_, _) => Dialogs.Settings(this);
        menu.Items.Add(restore);
        menu.Items.Add(settings);
        var search = new System.Windows.Controls.MenuItem
        {
            Header = I18n.T("Buscar juego"),
            InputGestureText = "Ctrl+F",
        };
        search.Click += (_, _) =>
        {
            menu.IsOpen = false;
            FocusCollectionSearch();
        };
        menu.Items.Add(search);
        var add = new System.Windows.Controls.MenuItem
        {
            Header = I18n.T("Añadir juego"),
            InputGestureText = "Ctrl+N",
        };
        add.Click += (_, _) =>
        {
            menu.IsOpen = false;
            Dialogs.Edit(this, null);
        };
        menu.Items.Add(add);
        menu.Items.Add(new Separator());
        var pin = new System.Windows.Controls.MenuItem
        {
            Header = I18n.T("Mantener siempre visible"),
            IsCheckable = true,
            IsChecked = Topmost,
            IsEnabled = !Preferences.MiniatureView,
        };
        pin.Click += (_, _) =>
        {
            if (Preferences.MiniatureView)
                return;
            Preferences.AlwaysOnTop = pin.IsChecked;
            ApplyPreferences();
            Persist();
        };
        var position = new System.Windows.Controls.MenuItem
        {
            Header = I18n.T("Bloquear posición y tamaño"),
            IsCheckable = true,
            IsChecked = Preferences.PositionLocked,
        };
        position.Click += (_, _) =>
        {
            Preferences.PositionLocked = position.IsChecked;
            ApplyPreferences();
            Persist();
        };
        menu.Items.Add(pin);
        menu.Items.Add(position);
    }

    private void FocusCollectionSearch()
    {
        friendsVisible = false;
        if (Preferences.MiniatureView)
        {
            Preferences.MiniatureView = false;
            Preferences.GridView = false;
            Preferences.Compact = false;
            ApplyPreferences();
            CaptureBounds();
            Store.SaveSettings(Preferences);
        }
        Refresh();
        Search.Focus();
        Search.SelectAll();
    }

    private void CycleView()
    {
        if (Preferences.MiniatureView)
        {
            Preferences.MiniatureView = false;
            Preferences.GridView = false;
            Preferences.Compact = false;
        }
        else if (Preferences.GridView)
        {
            Preferences.MiniatureView = true;
        }
        else if (Preferences.Compact)
        {
            Preferences.GridView = true;
            Preferences.Compact = false;
        }
        else
            Preferences.Compact = true;
        ApplyPreferences();
        CaptureBounds();
        Store.SaveSettings(Preferences);
        Refresh();
    }

    private int CalculateColumns() =>
        Math.Clamp((int)Math.Floor(Math.Max(280, GameList.ActualWidth - 20) / 145), 2, 12);

    private void BindCards()
    {
        gridColumns = CalculateColumns();
        GameList.ItemContainerStyle = Preferences.MiniatureView
            ? (Style)Resources["MiniatureItemStyle"]
            : (Style)Application.Current.Resources[typeof(ListBoxItem)];
        GameList.ItemTemplate = (DataTemplate)
            Resources[
                Preferences.MiniatureView ? "MiniatureTemplate"
                : Preferences.GridView ? "GridRowTemplate"
                : "GameTemplate"
            ];
        // Virtualize rows so a large library does not create a control for every cover.
        GameList.ItemsSource =
            Preferences.GridView && !Preferences.MiniatureView
                ? visibleCards
                    .Chunk(gridColumns)
                    .Select(cards => new CardRow(cards, gridColumns))
                    .ToList()
                : visibleCards;
    }

    private void GameListSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (
            ready
            && Preferences.GridView
            && !Preferences.MiniatureView
            && gridColumns != CalculateColumns()
        )
            BindCards();
    }

    internal sealed record CardRow(CardView[] Cards, int Columns);

    private void DragHandleDown(object sender, MouseButtonEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is not Game game)
            return;
        ((FrameworkElement)sender).Focus();
        pendingDrag = game.Id;
        dragOrigin = e.GetPosition(this);
        e.Handled = true;
    }

    private void DragHandleMove(object sender, MouseEventArgs e)
    {
        if (pendingDrag is not Guid id || e.LeftButton != MouseButtonState.Pressed)
        {
            pendingDrag = null;
            return;
        }
        var position = e.GetPosition(this);
        if (
            Math.Abs(position.X - dragOrigin.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(position.Y - dragOrigin.Y) < SystemParameters.MinimumVerticalDragDistance
        )
            return;
        pendingDrag = null;
        draggingId = id;
        dragScrollTimer.Start();
        try
        {
            var data = new System.Windows.DataObject();
            data.SetData(DragFormat, id.ToString(), false);
            System.Windows.DragDrop.DoDragDrop(this, data, System.Windows.DragDropEffects.Move);
        }
        finally
        {
            draggingId = null;
            dragScrollDirection = 0;
            dragScrollTimer.Stop();
            ClearDropHint();
        }
    }

    private bool CanDrop(System.Windows.DragEventArgs e, Game target) =>
        CanMovePayload(e.Data, target);

    private bool CanMovePayload(System.Windows.IDataObject data, Game target) =>
        draggingId is Guid id
        && id != target.Id
        && data.GetDataPresent(DragFormat, false)
        && data.GetData(DragFormat, false) is string value
        && value == id.ToString()
        && Games.Any(g => g.Id == id && g.Favorite == target.Favorite);

    private void CardDragOver(object sender, System.Windows.DragEventArgs e)
    {
        var border = (Border)sender;
        bool valid = border.Tag is Game target && CanDrop(e, target);
        e.Effects = valid
            ? System.Windows.DragDropEffects.Move
            : System.Windows.DragDropEffects.None;
        e.Handled = true;
        ClearDropHint();
        if (valid)
        {
            dropBorder = border;
            border.SetResourceReference(Border.BorderBrushProperty, "AccentBrush");
            bool after = e.GetPosition(border).Y >= border.ActualHeight / 2;
            border.BorderThickness = after ? new Thickness(1, 1, 1, 3) : new Thickness(1, 3, 1, 1);
        }
        else if (
            draggingId is Guid id
            && border.Tag is Game game
            && Games.Any(g => g.Id == id && g.Favorite != game.Favorite)
        )
            Notice(I18n.T("Los favoritos permanecen arriba. Reordena dentro del mismo grupo."));
    }

    private void ClearDropHint()
    {
        if (dropBorder is null)
            return;
        dropBorder.SetResourceReference(Border.BorderBrushProperty, "LineBrush");
        dropBorder.BorderThickness = new Thickness(1);
        dropBorder = null;
    }

    private void CardDragLeave(object sender, System.Windows.DragEventArgs e) => ClearDropHint();

    private void CardDrop(object sender, System.Windows.DragEventArgs e)
    {
        var border = (Border)sender;
        bool after = e.GetPosition(border).Y >= border.ActualHeight / 2;
        ClearDropHint();
        if (border.Tag is Game target && CanDrop(e, target))
        {
            e.Effects = System.Windows.DragDropEffects.Move;
            MoveCard(draggingId!.Value, target.Id, after);
        }
        else
            e.Effects = System.Windows.DragDropEffects.None;
        e.Handled = true;
    }

    internal bool MoveCard(Guid sourceId, Guid targetId, bool after)
    {
        if (!GameRules.Move(Games, sourceId, targetId, after))
            return false;
        Persist();
        Refresh();
        Notice(I18n.T("Orden guardado."));
        return true;
    }

    private void DragHandleKey(object sender, KeyEventArgs e)
    {
        Key key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (
            (!MatchesShortcut(e, "moveUp") && !MatchesShortcut(e, "moveDown"))
            || ((FrameworkElement)sender).Tag is not Game game
        )
            return;
        int index = visibleCards.FindIndex(c => c.Model.Id == game.Id),
            step = MatchesShortcut(e, "moveUp") ? -1 : 1;
        int next = index + step;
        e.Handled = true;
        if (index < 0 || next < 0 || next >= visibleCards.Count)
            return;
        if (MoveCard(game.Id, visibleCards[next].Model.Id, step > 0))
        {
            var item = Preferences.GridView
                ? GameList.Items.Cast<CardRow>().First(r => r.Cards.Any(c => c.Model.Id == game.Id))
                : (object)visibleCards.First(c => c.Model.Id == game.Id);
            GameList.ScrollIntoView(item);
            UpdateLayout();
            var handle = VisualChildren(GameList)
                .OfType<Button>()
                .FirstOrDefault(b =>
                    b.Tag is Game g && g.Id == game.Id && (string?)b.Content == "⠿"
                );
            handle?.Focus();
        }
        else
            Notice(I18n.T("Los favoritos permanecen arriba. Reordena dentro del mismo grupo."));
    }

    internal static IEnumerable<DependencyObject> VisualChildren(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            yield return child;
            foreach (var descendant in VisualChildren(child))
                yield return descendant;
        }
    }

    private static T? FindVisual<T>(DependencyObject parent)
        where T : DependencyObject => VisualChildren(parent).OfType<T>().FirstOrDefault();

    private void PinClick(object sender, RoutedEventArgs e)
    {
        if (Preferences.MiniatureView)
            return;
        Preferences.AlwaysOnTop = !Preferences.AlwaysOnTop;
        ApplyPreferences();
        Store.SaveSettings(Preferences);
    }

    internal void MinimizeWidget() => WindowState = WindowState.Minimized;

    private void MinimizeClick(object sender, RoutedEventArgs e) => MinimizeWidget();

    private void CloseClick(object sender, RoutedEventArgs e) => Close();

    private void DragTitle(object sender, MouseButtonEventArgs e)
    {
        if (!Preferences.PositionLocked && e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
            CaptureBounds();
            Store.SaveSettings(Preferences);
        }
    }

    private void ResizeDrag(object sender, DragDeltaEventArgs e)
    {
        if (Preferences.PositionLocked)
            return;
        Width = Math.Max(MinWidth, Width + e.HorizontalChange);
        Height = Math.Max(MinHeight, Height + e.VerticalChange);
        boundsTimer.Stop();
        boundsTimer.Start();
    }

    private void EditClick(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is Game game)
            Dialogs.Edit(this, game);
    }

    private void CardClick(object sender, MouseButtonEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is Game game)
            Dialogs.Edit(this, game);
    }

    private void FinishClick(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is Game game)
        {
            GameRules.SetStatus(
                game,
                game.Status == GameStatus.Finished ? GameStatus.Playing : GameStatus.Finished
            );
            Persist();
            Refresh();
        }
    }

    private void ExampleClick(object sender, RoutedEventArgs e)
    {
        AddExamples();
        Refresh();
    }

    private void AddExamples()
    {
        if (Games.Count != 0)
            return;
        Games.AddRange(
            new[]
            {
                new Game
                {
                    Title = "Hollow Knight",
                    SteamAppId = 367520,
                    Platform = "Steam",
                    Status = GameStatus.Playing,
                    Favorite = true,
                    SortOrder = 0,
                    Tasks = [new() { Title = I18n.T("Explorar una nueva zona") }],
                },
                new Game
                {
                    Title = "Hades",
                    SteamAppId = 1145360,
                    Platform = "Steam",
                    Status = GameStatus.Pending,
                    SortOrder = 1,
                    Goal = GameGoal.Story,
                },
                new Game
                {
                    Title = "Portal 2",
                    SteamAppId = 620,
                    Platform = "Steam",
                    Status = GameStatus.Paused,
                    SortOrder = 2,
                    Tasks = [new() { Title = I18n.T("Continuar la campaña cooperativa") }],
                },
            }
        );
        Persist();
        Notice(I18n.T("Juegos de ejemplo añadidos. El progreso de Steam aún no se ha consultado."));
    }

    internal void ShowWidget()
    {
        Show();
        WindowState = WindowState.Normal;
        if (ready)
        {
            if (IsFullWindow)
                FillWorkArea();
            else
                ClampToScreen();
        }
        Activate();
    }

    private void ToggleVisible()
    {
        if (IsVisible && WindowState != WindowState.Minimized)
            Hide();
        else
            ShowWidget();
    }

    internal void Exit()
    {
        exiting = true;
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!exiting && Preferences.CloseToTray)
        {
            e.Cancel = true;
            Hide();
            Persist();
            return;
        }
        try
        {
            Persist();
        }
        catch (Exception ex)
        {
            LocalizedNotice.Show(I18n.T("No se pudo guardar: ") + I18n.Error(ex), "Checkpoint");
            e.Cancel = true;
            return;
        }
        base.OnClosing(e);
    }

    // Cancela trabajo pendiente y libera temporizadores, clientes y recursos de la bandeja.
    protected override void OnClosed(EventArgs e)
    {
        shutdown.Cancel();
        CloseInlinePages();
        Updates.Dispose();
        detectionTimer.Stop();
        foreach (var window in achievementWindows.Values.ToArray())
            window.Close();
        Retro?.Dispose();
        timer.Stop();
        boundsTimer.Stop();
        dragScrollTimer.Stop();
        if (hotkeyRegistered && source is not null)
            UnregisterHotKey(source.Handle, HotkeyId);
        socialTimer.Stop();
        publicationTimer.Stop();
        Social?.Dispose();
        tray.Dispose();
        Steam.Dispose();
        Covers.Dispose();
        Store.Dispose();
        base.OnClosed(e);
        Application.Current.Shutdown(Environment.ExitCode);
    }

    private async void ConnectClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Preferences.ServiceUrl))
            Dialogs.Settings(this);
        if (!string.IsNullOrWhiteSpace(Preferences.ServiceUrl))
            await ConnectSteam();
    }

    internal async Task ConnectSteam()
    {
        if (syncing)
            return;
        syncing = true;
        Refresh();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(shutdown.Token);
        cancellation.CancelAfter(TimeSpan.FromMinutes(5));
        try
        {
            var service = SteamClient.ValidateServiceUrl(Preferences.ServiceUrl).AbsoluteUri;
            var flow = await Steam.BeginLogin(service, cancellation.Token);
            if (
                !Uri.TryCreate(flow.AuthorizeUrl, UriKind.Absolute, out var authorize)
                || authorize.Scheme != "https"
                || authorize.Host != "steamcommunity.com"
                || authorize.AbsolutePath != "/openid/login"
            )
                throw new InvalidDataException(
                    I18n.T("El servicio no ha devuelto una dirección oficial de Steam.")
                );
            Process.Start(new ProcessStartInfo(authorize.AbsoluteUri) { UseShellExecute = true });
            Notice(
                I18n.T("Completa la vinculación en tu navegador. Puedes seguir usando el widget.")
            );
            while (!cancellation.IsCancellationRequested)
            {
                await Task.Delay(2500, cancellation.Token);
                var result = await Steam.Poll(service, flow, cancellation.Token);
                if (result.Status != "complete")
                    continue;
                Steam.SaveSession(service, result);
                Preferences.SteamId = result.SteamId;
                Persist();
                Notice(I18n.T("Steam vinculado. Importando biblioteca…"));
                break;
            }
        }
        catch (OperationCanceledException)
        {
            if (!shutdown.IsCancellationRequested)
                Notice(I18n.T("Vinculación cancelada o agotada. Puedes volver a intentarlo."));
        }
        catch (Exception ex)
            when (ex
                    is System.Net.Http.HttpRequestException
                        or InvalidOperationException
                        or ArgumentException
                        or IOException
            )
        {
            Notice(I18n.Error(ex));
        }
        finally
        {
            syncing = false;
            if (!shutdown.IsCancellationRequested)
                Refresh();
        }
        if (!shutdown.IsCancellationRequested && Steam.Session is not null)
            await Sync(true);
    }

    private async void SyncClick(object sender, RoutedEventArgs e)
    {
        if (Steam.Session is null)
            await ConnectSteam();
        else
            await Sync(true);
    }

    // Evita sincronizaciones simultáneas y combina los datos remotos con los campos editados localmente.
    internal async Task Sync(bool importLibrary, Game? single = null)
    {
        if (syncing || Steam.Session is null)
            return;
        syncing = true;
        Refresh();
        var errors = new List<string>();
        try
        {
            if (importLibrary)
            {
                var library = await Steam.Library(Preferences.ServiceUrl, shutdown.Token);
                int added = GameRules.MergeSteamLibrary(Games, library.Games);
                Notice(
                    (
                        I18n.IsEnglish
                            ? $"{added} games imported. Add them to My list from their cards."
                            : $"{added} juegos importados. Añádelos a Mi lista desde su ficha."
                    )
                );
                if (Games.All(g => !g.Tracked))
                    allLibrary = true;
            }
            var selected = single is null
                ? Games
                    .Where(g => g.Tracked && g.SteamAppId.HasValue)
                    .OrderBy(g => g.SyncedAt ?? DateTimeOffset.MinValue)
                    .Take(20)
                    .ToList()
                : new List<Game> { single };
            foreach (var game in selected)
            {
                if (game.SteamAppId is not int appId)
                    continue;
                try
                {
                    var result = await Steam.Achievements(
                        Preferences.ServiceUrl,
                        appId,
                        shutdown.Token
                    );
                    game.Achievements = result.Achievements.ToList();
                    game.SyncedAt = DateTimeOffset.UtcNow;
                }
                catch (InvalidOperationException ex)
                {
                    errors.Add(game.Title + ": " + I18n.Error(ex));
                }
            }
            Persist();
            if (errors.Count > 0)
                Notice(errors[0]);
            else if (selected.Count > 0)
                Notice(I18n.T("Última sincronización: ") + DateTime.Now.ToString("HH:mm") + ".");
        }
        catch (OperationCanceledException)
        {
            if (!shutdown.IsCancellationRequested)
                Notice(I18n.T("Se agotó el tiempo. Se conserva el progreso anterior."));
        }
        catch (Exception ex)
            when (ex
                    is System.Net.Http.HttpRequestException
                        or InvalidOperationException
                        or ArgumentException
                        or IOException
            )
        {
            Notice(I18n.Error(ex));
        }
        finally
        {
            syncing = false;
            if (!shutdown.IsCancellationRequested)
                Refresh();
        }
    }

    public sealed class CardView : INotifyPropertyChanged
    {
        public Game Model { get; }
        public string Initial => Model.Title[..1].ToUpperInvariant();
        public double CoverWidth { get; }
        public double CoverHeight { get; }
        public Visibility DetailVisibility { get; }
        public Visibility NextTaskVisibility =>
            Model.NextTask.Length > 0 && DetailVisibility == Visibility.Visible
                ? Visibility.Visible
                : Visibility.Collapsed;
        public Visibility GridNextTaskVisibility =>
            Model.NextTask.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility CoverVisibility { get; }
        public System.Windows.Media.Brush CoverBackground { get; }
        public System.Windows.Media.Brush StatusBrush { get; }
        public string Meta =>
            (Model.Favorite ? "★  " : "")
            + Model.Platform
            + (Model.PlaytimeMinutes > 0 ? $"  ·  {Model.PlaytimeMinutes / 60d:0.#} h" : "");
        public string ProgressCaption =>
            Model.Goal == GameGoal.Story && Model.StoryPercent.HasValue
                ? I18n.T("Historia · avance manual")
            : Model.Goal == GameGoal.Custom && Model.Tasks.Count > 0
                ? (
                    I18n.IsEnglish
                        ? $"{Model.Tasks.Count(t => t.Done)} / {Model.Tasks.Count} tasks"
                        : $"{Model.Tasks.Count(t => t.Done)} / {Model.Tasks.Count} tareas"
                )
            : Model.SteamAppId is null ? I18n.T("Objetivos locales")
            : Model.Achievements is null ? I18n.T("Logros sin sincronizar")
            : Model.Achievements.Count == 0 ? I18n.T("Sin logros de Steam")
            : (
                I18n.IsEnglish
                    ? $"{Model.UnlockedCount} / {Model.Achievements.Count} achievements"
                    : $"{Model.UnlockedCount} / {Model.Achievements.Count} logros"
            );
        public string GridProgressCaption =>
            ProgressCaption + (PercentText.Length > 0 ? " · " + PercentText : "");
        public string ReorderName => I18n.T("Reordenar ") + Model.Title;
        public string MiniatureName => Model.Title + " · " + Model.StatusText;
        private int? ProgressPercent =>
            Model.Goal == GameGoal.Story && Model.StoryPercent.HasValue ? Model.StoryPercent
            : Model.Goal == GameGoal.Custom && Model.Tasks.Count > 0
                ? (int)Math.Round(Model.Tasks.Count(t => t.Done) * 100d / Model.Tasks.Count)
            : Model.AchievementPercent;
        public int Percentage => ProgressPercent ?? 0;
        public string PercentText => ProgressPercent is int percent ? percent + "%" : "";
        public Visibility ProgressVisibility =>
            ProgressPercent.HasValue ? Visibility.Visible : Visibility.Collapsed;
        public string FinishIcon => Model.Status == GameStatus.Finished ? "✓" : "○";
        public BitmapImage? Cover { get; private set; }
        public event PropertyChangedEventHandler? PropertyChanged;

        public CardView(Game game, bool compact, bool light, bool lightweight = false)
        {
            CoverVisibility = lightweight ? Visibility.Collapsed : Visibility.Visible;
            Model = game;
            CoverWidth = compact ? 43 : 60;
            CoverHeight = compact ? 66 : 94;
            StatusBrush = Brush(
                Model.Status switch
                {
                    GameStatus.Playing => light ? "#FF0B7554" : "#FF8CEBC6",
                    GameStatus.Finished => light ? "#FF4F5CC1" : "#FFA9B9FF",
                    GameStatus.Paused => light ? "#FF866328" : "#FFE6CB90",
                    _ => light ? "#FF58677F" : "#FFADB6CA",
                }
            );
            DetailVisibility = compact ? Visibility.Collapsed : Visibility.Visible;
            CoverBackground = Brush(
                new[] { "#FF344C61", "#FF4D385C", "#FF3C5951" }[
                    (int)((uint)game.Id.GetHashCode() % 3)
                ]
            );
        }

        private Task? loading;
        private int coverGeneration;

        public Task LoadCover(CoverCache cache) =>
            CoverVisibility == Visibility.Collapsed
                ? Task.CompletedTask
                : loading ??= LoadOnce(cache);

        private async Task LoadOnce(CoverCache cache)
        {
            int current = coverGeneration;
            var image = await cache.Get(Model);
            if (current != coverGeneration)
                return;
            Cover = image;
            PropertyChanged?.Invoke(this, new(nameof(Cover)));
        }

        internal void ReleaseCover()
        {
            coverGeneration++;
            loading = null;
            Cover = null;
            PropertyChanged?.Invoke(this, new(nameof(Cover)));
        }
    }
}
