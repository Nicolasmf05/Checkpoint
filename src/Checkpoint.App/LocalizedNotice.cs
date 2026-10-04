// Muestra avisos traducidos y configura su representación en la interfaz disponible.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Checkpoint.Core;

namespace Checkpoint.App;

// App-owned notices follow Checkpoint's language instead of Windows' button labels.
internal static class LocalizedNotice
{
    internal static void Show(
        string text,
        string title,
        MessageBoxButton buttons = MessageBoxButton.OK,
        MessageBoxImage image = MessageBoxImage.None
    ) => Show(null, text, title);

    internal static void Show(Window? owner, string text, string title)
    {
        var window = new Window
        {
            Title = title,
            Width = 420,
            SizeToContent = SizeToContent.Height,
            MaxHeight = SystemParameters.WorkArea.Height - 35,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = owner is null,
            WindowStartupLocation = owner is null
                ? WindowStartupLocation.CenterScreen
                : WindowStartupLocation.CenterOwner,
        };
        if (owner is not null)
            Dialogs.Parent(window, owner);
        if (Application.Current.TryFindResource("InputBrush") is Brush background)
            window.Background = background;
        if (Application.Current.TryFindResource("TextBrush") is Brush foreground)
            window.Foreground = foreground;
        var panel = new DockPanel { Margin = new Thickness(20) };
        var close = new Button
        {
            Content = I18n.T("Entendido"),
            IsDefault = true,
            IsCancel = true,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0),
            MinWidth = 90,
        };
        close.Click += (_, _) => window.Close();
        DockPanel.SetDock(close, Dock.Bottom);
        panel.Children.Add(close);
        panel.Children.Add(
            new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap },
            }
        );
        window.Content = panel;
        Dialogs.ShowPage(window);
    }
}
