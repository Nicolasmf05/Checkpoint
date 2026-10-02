using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;

using Checkpoint.Core;

namespace Checkpoint.App;

public partial class App : Application
{
    private Mutex? singleInstance;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        int index = Array.IndexOf(e.Args, "--data-dir");
        int smokeIndex = Array.IndexOf(e.Args, "--smoke-test");
        bool explicitDirectory = index >= 0 && index + 1 < e.Args.Length && !e.Args[index + 1].StartsWith("--", StringComparison.Ordinal);
        string directory = explicitDirectory ? e.Args[index + 1]
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Checkpoint");
        if (smokeIndex >= 0)
        {
            if (!explicitDirectory) directory = Path.Combine(Path.GetTempPath(), "Checkpoint-smoke-" + Guid.NewGuid().ToString("N"));
            if (File.Exists(Path.Combine(directory, "checkpoint.db")) || File.Exists(Path.Combine(directory, "steam-session.dat"))
                || File.Exists(Path.Combine(directory,"checkpoint-session.dat")) || Directory.Exists(Path.Combine(directory,"social")))
            { Console.Error.WriteLine(I18n.T("La prueba nativa requiere una carpeta nueva sin biblioteca, sesiones ni publicaciones.")); Shutdown(1); return; }
        }
        string instanceKey = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Path.GetFullPath(directory).ToUpperInvariant())))[..20];
        singleInstance = new Mutex(true, "Local\\Checkpoint-" + instanceKey, out bool first);
        if (!first) { MessageBox.Show(I18n.T("Checkpoint ya está abierto. Puedes mostrarlo desde la bandeja o con Ctrl+Alt+C."), "Checkpoint"); Shutdown(); return; }
        DispatcherUnhandledException += (_, args) =>
        {
            args.Handled = true;
            if (e.Args.Contains("--diagnostics")) { Console.Error.WriteLine(args.Exception); Environment.Exit(1); }
            if (Current.MainWindow is MainWindow main) main.Notice(I18n.T("No se pudo completar la operación: ") + args.Exception.Message);
        };
        try
        {
            var window = new MainWindow(directory, e.Args.Contains("--demo")); MainWindow = window; window.Show();
            if (smokeIndex >= 0 && smokeIndex + 1 < e.Args.Length) _ = window.RenderSmokeTest(e.Args[smokeIndex + 1]);
        }
        catch (Exception ex)
        {
            MessageBox.Show(I18n.T("No se puede abrir la biblioteca. Los archivos se han conservado.\n") + ex.Message, "Checkpoint", MessageBoxButton.OK, MessageBoxImage.Error); Shutdown(1);
        }
    }
    protected override void OnExit(ExitEventArgs e) { singleInstance?.Dispose(); base.OnExit(e); }
}
