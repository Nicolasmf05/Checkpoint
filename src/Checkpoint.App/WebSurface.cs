using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Checkpoint.Core;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace Checkpoint.App;

// The browser owns presentation. The native application exposes only explicit UI commands.
internal sealed class WebSurface : IDisposable
{
    internal const string Origin = "https://checkpoint.local/";
    internal readonly WebView2CompositionControl Browser = new() { DefaultBackgroundColor = System.Drawing.Color.Transparent };
    private readonly Window window;
    private readonly Func<object> snapshot;
    private readonly Action<JsonElement> command;
    private readonly DispatcherTimer updates = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private string previous = "";
    private bool loaded, disposed;
    private int acknowledged;
    private readonly Func<bool> poll;
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<BitmapSource,EncodedImage> imageCache = new();
    private sealed record EncodedImage(string Value);
    internal Task Initialization { get; }
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    internal WebSurface(Window window, string directory, Func<object> snapshot, Action<JsonElement> command, Func<bool>? poll = null)
    {
        this.window = window; this.snapshot = snapshot; this.command = command; this.poll = poll ?? (() => true);
        var original = (UIElement)window.Content;
        window.Content = null; original.Visibility = Visibility.Collapsed;
        var host = new Grid(); host.Children.Add(original); host.Children.Add(Browser); window.Content = host;
        window.Closed += (_, _) => Dispose();
        updates.Tick += (_, _) => { if (this.poll()) Publish(); };
        Initialization = Initialize(directory);
    }
    private async Task Initialize(string directory)
    {
        try
        {
            var environment = await CoreWebView2Environment.CreateAsync(null, Path.Combine(directory,"webview-profile"));
            if (disposed) return;
            await Browser.EnsureCoreWebView2Async(environment);
            var core = Browser.CoreWebView2;
            core.Settings.AreHostObjectsAllowed = false;
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.AreDevToolsEnabled = Environment.GetCommandLineArgs().Contains("--diagnostics");
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.IsZoomControlEnabled = false;
            core.Settings.AreBrowserAcceleratorKeysEnabled = false;
            core.SetVirtualHostNameToFolderMapping("checkpoint.local", Path.Combine(AppContext.BaseDirectory,"Web"), CoreWebView2HostResourceAccessKind.DenyCors);
            core.NavigationStarting += (_, e) => { if (e.Uri != Origin + "index.html") e.Cancel = true; };
            core.FrameNavigationStarting += (_, e) => e.Cancel = true;
            core.NewWindowRequested += (_, e) => e.Handled = true;
            core.PermissionRequested += (_, e) => e.State = CoreWebView2PermissionState.Deny;
            core.DownloadStarting += (_, e) => e.Cancel = true;
            core.WebMessageReceived += (_, e) =>
            {
                if (disposed || e.Source != Origin + "index.html" || e.WebMessageAsJson.Length > 65536) return;
                try
                {
                    using var document = JsonDocument.Parse(e.WebMessageAsJson);
                    if (document.RootElement.ValueKind != JsonValueKind.Object) return;
                    var message = document.RootElement.Clone();
                    // Leave the browser callback before opening a modal nested message loop.
                    window.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (disposed) return;
                        try { if (message.TryGetProperty("requestId",out var request) && request.TryGetInt32(out int number) && number > 0) acknowledged = Math.Max(acknowledged,number); command(message); Publish(); }
                        catch (Exception error) when (error is ArgumentException or InvalidOperationException or IOException or JsonException or FormatException or KeyNotFoundException)
                        { if (Application.Current.MainWindow is MainWindow main) main.Notice(I18n.Error(error)); }
                    }));
                }
                catch (Exception error) when (error is ArgumentException or InvalidOperationException or IOException or JsonException or FormatException)
                { if (Application.Current.MainWindow is MainWindow main) main.Notice(I18n.Error(error)); }
            };
            core.NavigationCompleted += (_, e) => { if (e.IsSuccess) { loaded = true; previous = ""; Publish(); updates.Start(); } };
            core.Navigate(Origin + "index.html");
        }
        catch (Exception error)
        {
            if (disposed) return;
            // A browser cannot render its own missing-runtime bootstrap notice.
            var panel = new StackPanel { Margin = new Thickness(24) };
            panel.Children.Add(new TextBlock { Text = I18n.T("Checkpoint necesita Microsoft Edge WebView2 Runtime para mostrar la interfaz. Instálalo y vuelve a abrir la aplicación."), TextWrapping = TextWrapping.Wrap });
            var install = new Button { Content = I18n.T("Descargar WebView2"), Margin = new Thickness(0,16,0,0) };
            install.Click += (_, _) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://developer.microsoft.com/microsoft-edge/webview2/") { UseShellExecute = true });
            panel.Children.Add(install); window.Content = panel;
            if (Environment.GetCommandLineArgs().Contains("--diagnostics")) Console.Error.WriteLine(error);
        }
    }
    internal void Publish()
    {
        if (!loaded || disposed || !window.IsVisible || window.WindowState == WindowState.Minimized) return;
        string value = JsonSerializer.Serialize(snapshot(),Json);
        value = value[..^1]+",\"ack\":"+acknowledged+"}";
        if (value == previous) return;
        previous = value; Browser.CoreWebView2.PostWebMessageAsJson(value);
    }
    internal void Event(object value) { if (loaded && !disposed) Browser.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(value,Json)); }
    internal static void AttachDialog(Window window)
    {
        if (window.Tag is WebSurface || window.Content is not FrameworkElement root) return;
        var controls = new WebControls();
        if (window.SizeToContent != SizeToContent.Manual) { window.SizeToContent = SizeToContent.Manual; window.Height = 240; }
        string directory = (Application.Current.MainWindow as MainWindow)?.Store.DirectoryPath
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Checkpoint");
        var surface = new WebSurface(window,directory,() => new { kind="dialog", language=I18n.Language,
            light=(Application.Current.MainWindow as MainWindow)?.Preferences.LightTheme == true, title=window.Title, root=controls.Capture(root) },message => { if (message.TryGetProperty("action",out var action) && action.GetString() == "cancel-dialog") window.Close(); else controls.Dispatch(message); });
        window.Tag = surface;
    }
    internal static string ImageUri(BitmapSource image) => imageCache.GetValue(image,key => new EncodedImage("data:image/png;base64,"+Convert.ToBase64String(ImageBytes(key)))).Value;
    internal static byte[] ImageBytes(BitmapSource image)
    {
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image));
        using var output = new MemoryStream(); encoder.Save(output); return output.ToArray();
    }
    public void Dispose() { if (disposed) return; disposed = true; updates.Stop(); Browser.Dispose(); }
}

// Temporary form controllers retain existing validation and account flows.
// Their visual representation is an HTML schema, never the native WPF templates.
internal sealed class WebControls
{
    private readonly Dictionary<FrameworkElement,int> identities = new();
    private readonly Dictionary<int,FrameworkElement> active = new();
    private int next;
    internal object? Capture(FrameworkElement root)
    {
        active.Clear(); var result = Node(root,true);
        foreach (var stale in identities.Keys.Where(key => !active.ContainsKey(identities[key])).ToArray()) identities.Remove(stale);
        return result;
    }
    private object? Node(FrameworkElement element, bool root = false)
    {
        if (!root && element.Visibility != Visibility.Visible) return null;
        if (!identities.TryGetValue(element,out int id)) identities[element] = id = ++next;
        active[id] = element;
        var node = new Dictionary<string,object?> { ["id"]=id, ["enabled"]=element.IsEnabled,
            ["name"]=System.Windows.Automation.AutomationProperties.GetName(element), ["tip"]=element.ToolTip?.ToString(),
            ["row"]=Grid.GetRow(element)+1, ["column"]=Grid.GetColumn(element)+1 };
        switch (element)
        {
            case TextBlock text: node["type"]="text"; node["text"]=text.Text; node["heading"]=text.FontSize >= 18; node["muted"]=text.FontSize <= 11; break;
            case PasswordBox password: node["type"]="password"; node["empty"]=password.Password.Length == 0; node["max"]=password.MaxLength; break;
            case TextBox text: node["type"]=text.AcceptsReturn ? "textarea" : "input"; node["value"]=text.Text; node["max"]=text.MaxLength; break;
            case CheckBox check: node["type"]="check"; node["text"]=Text(check.Content); node["checked"]=check.IsChecked == true; break;
            case Button button: node["type"]="button"; node["text"]=Text(button.Content); node["accent"]=ReferenceEquals(button.Style,Application.Current.TryFindResource("AccentButton")); node["dock"]=DockPanel.GetDock(button).ToString(); break;
            case ComboBox combo: node["type"]="select"; node["value"]=combo.SelectedIndex; node["options"]=combo.Items.Cast<object>().Select(item => item is ComboBoxItem entry ? Text(entry.Content) : item.ToString()).ToArray(); break;
            case Slider slider: node["type"]="slider"; node["value"]=slider.Value; node["min"]=slider.Minimum; node["max"]=slider.Maximum; node["step"]=slider.TickFrequency; break;
            case Expander expander: node["type"]="details"; node["text"]=Text(expander.Header); node["open"]=expander.IsExpanded; node["children"]=Children(expander.Content); break;
            case Image image:
                node["type"]="image"; if (image.Source is BitmapSource bitmap) node["src"]=WebSurface.ImageUri(bitmap); break;
            case Grid grid: node["type"]="grid"; node["columns"]=Math.Max(1,grid.ColumnDefinitions.Count); node["columnWidths"]=grid.ColumnDefinitions.Select(column => column.Width.IsAuto ? "auto" : column.Width.IsStar ? "minmax(0,"+column.Width.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)+"fr)" : column.Width.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)+"px").ToArray(); node["rows"]=grid.RowDefinitions.Select(row => row.Height.IsAuto ? "auto" : "minmax(0,1fr)").ToArray(); node["children"]=grid.Children.OfType<FrameworkElement>().Select(child => Node(child)).Where(child => child is not null).ToArray(); break;
            case Panel panel: node["type"]=panel is DockPanel ? "dock" : panel is WrapPanel || panel is StackPanel { Orientation: Orientation.Horizontal } ? "row" : "stack"; node["children"]=panel.Children.OfType<FrameworkElement>().Select(child => Node(child)).Where(child => child is not null).ToArray(); break;
            case Decorator decorator: node["type"]="card"; node["children"]=Children(decorator.Child); break;
            case ScrollViewer scroll: node["type"]="scroll"; node["children"]=Children(scroll.Content); break;
            default: node["type"]="stack"; node["children"]=Children((element as ContentControl)?.Content); break;
        }
        return node;
    }
    private object[] Children(object? content) => content is FrameworkElement element && Node(element) is { } node ? [node] : [];
    private static string Text(object? content) => content is TextBlock text ? text.Text : content?.ToString() ?? "";
    internal void Dispatch(JsonElement message)
    {
        if (!message.TryGetProperty("control",out var control) || !control.TryGetInt32(out int id) || !active.TryGetValue(id,out var element) || !element.IsEnabled) return;
        string action = message.GetProperty("action").GetString() ?? "";
        if (action == "click" && element is Button button) button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        else if (message.TryGetProperty("value",out var value))
        {
            switch (element)
            {
                case TextBox text when value.ValueKind == JsonValueKind.String: text.Text = (value.GetString() ?? "")[..Math.Min(value.GetString()!.Length,text.MaxLength > 0 ? text.MaxLength : 25000)]; break;
                case PasswordBox password when value.ValueKind == JsonValueKind.String: password.Password = (value.GetString() ?? "")[..Math.Min(value.GetString()!.Length,password.MaxLength > 0 ? password.MaxLength : 200)]; break;
                case CheckBox check when value.ValueKind is JsonValueKind.True or JsonValueKind.False: check.IsChecked=value.GetBoolean(); check.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent)); break;
                case ComboBox combo when value.TryGetInt32(out int index) && index >= 0 && index < combo.Items.Count: combo.SelectedIndex=index; break;
                case Slider slider when value.TryGetDouble(out double number) && double.IsFinite(number): slider.Value=Math.Clamp(number,slider.Minimum,slider.Maximum); break;
                case Expander expander when value.ValueKind is JsonValueKind.True or JsonValueKind.False: expander.IsExpanded=value.GetBoolean(); break;
            }
        }
    }
}
