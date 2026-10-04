// Adapta los controladores de ventanas a una pila de páginas dentro de WebView2.
// Cada página conserva sus controles y su ciclo de cierre, incluso al abrir páginas hijas.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using Checkpoint.Core;

namespace Checkpoint.App;

internal static partial class Dialogs
{
    internal static void Parent(Window page, Window parent)
    {
        if (!App.UseCss)
            page.Owner = parent;
    }

    internal static void Finish(Window page)
    {
        if (App.UseCss)
            page.Close();
        else
            page.DialogResult = true;
    }

    internal static void ShowPage(Window page, bool modeless = false)
    {
        if (App.UseCss && Application.Current.MainWindow is MainWindow main && main.HasWebInterface)
            main.ShowInlinePage(page, modeless);
        else if (modeless)
            page.Show();
        else
            page.ShowDialog();
    }

    internal static Window PickerOwner(Window page) =>
        App.UseCss && Application.Current.MainWindow is MainWindow main ? main : page;
}

public partial class MainWindow
{
    private sealed class InlinePage(Window controller, FrameworkElement root)
    {
        internal readonly Window Controller = controller;
        internal readonly FrameworkElement Root = root;
        internal readonly WebControls Controls = new();
        internal readonly string Id = Guid.NewGuid().ToString("N");
        internal readonly DispatcherFrame Frame = new();
    }

    private readonly List<InlinePage> inlinePages = [];
    private (double Width, double Height, double Left, double Top)? inlineMiniatureBounds;
    internal bool HasWebInterface => web is not null;
    internal bool HasInlinePage => inlinePages.Count > 0;

    internal bool PageIsOpen(Window page) =>
        page.IsVisible || inlinePages.Any(p => p.Controller == page);

    // El cierre de una página retira también sus hijas y restaura las dimensiones previas de Miniatura.
    internal void ShowInlinePage(Window controller, bool modeless)
    {
        if (!IsVisible || WindowState == WindowState.Minimized)
            ShowWidget();
        if (controller.Content is not FrameworkElement root)
            throw new InvalidOperationException("Page content is missing");
        if (!HasInlinePage && Preferences.MiniatureView)
        {
            inlineMiniatureBounds = (Width, Height, Left, Top);
            Width = Math.Max(510, Preferences.Width);
            Height = Math.Max(600, Preferences.Height);
            ClampToScreen();
        }
        var entry = new InlinePage(controller, root);
        inlinePages.Add(entry);
        controller.Tag = web;
        controller.Closing += (_, _) =>
        {
            int index = inlinePages.IndexOf(entry);
            if (index < 0)
                return;
            foreach (var child in inlinePages.Skip(index + 1).Reverse().ToArray())
                child.Controller.Close();
            inlinePages.Remove(entry);
            entry.Frame.Continue = false;
            if (!HasInlinePage && inlineMiniatureBounds is { } bounds)
            {
                inlineMiniatureBounds = null;
                if (Preferences.MiniatureView)
                {
                    Width = bounds.Width;
                    Height = bounds.Height;
                    Left = bounds.Left;
                    Top = bounds.Top;
                    ClampToScreen();
                }
                else
                    ApplyPreferences();
            }
            web?.Publish();
        };
        web?.Publish();
        controller.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
        if (!modeless)
            Dispatcher.PushFrame(entry.Frame);
    }

    private object InlineSnapshot()
    {
        var page = inlinePages[^1];
        var root = page.Controls.Capture(page.Root);
        if (root is Dictionary<string, object?> node)
            node["pageId"] = page.Id;
        return new
        {
            kind = "dialog",
            inline = true,
            language = I18n.Language,
            theme = Themes.Id(Preferences),
            light = Themes.IsLight(Preferences),
            title = page.Controller.Title,
            navigation = new
            {
                back = I18n.T("Volver"),
                home = I18n.T("Volver a la colección"),
                close = I18n.T("Cerrar Checkpoint"),
                minimize = I18n.T("Minimizar"),
                trail = inlinePages.Select(p => p.Controller.Title).ToArray(),
            },
            achievementReview = achievementReviewVisible
                ? new
                {
                    text = AchievementReviewText,
                    cancelText = I18n.T(
                        achievementReviewCancellation is not null
                            ? "Detener repaso"
                            : "Ocultar progreso"
                    ),
                    running = achievementReviewCancellation is not null,
                }
                : null,
            root = root,
        };
    }

    private bool InlineCommand(JsonElement message)
    {
        if (!HasInlinePage)
            return false;
        string? action = message.TryGetProperty("action", out var a) ? a.GetString() : null;
        if (action == "close-app")
            Close();
        else if (action == "minimize")
            MinimizeWidget();
        else if (action == "drag")
        {
            if (!Preferences.PositionLocked)
                DragMove();
        }
        else if (action is "cancel-dialog" or "navigation-back")
            inlinePages[^1].Controller.Close();
        else if (action == "navigation-home")
            CloseInlinePages();
        else if (action == "stop-achievement-review")
            StopAchievementReview();
        else
            inlinePages[^1].Controls.Dispatch(message);
        web?.Publish();
        return true;
    }

    internal void CloseInlinePages()
    {
        foreach (var entry in inlinePages.AsEnumerable().Reverse().ToArray())
            entry.Controller.Close();
    }
}
