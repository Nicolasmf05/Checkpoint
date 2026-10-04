using Checkpoint.Core;

namespace Checkpoint.App;

internal static partial class Dialogs
{
    internal static void Credits(MainWindow owner)
    {
        var window = Modal(owner, I18n.T("Créditos"), 480, 420);
        var body = Panel();
        Layout(window, body, out var footer);
        Heading(body, "Yus", I18n.T("El mejor beta tester"));
        footer.Children.Add(Button(I18n.T("Cerrar"), (_, _) => window.Close()));
        ShowPage(window);
    }
}
