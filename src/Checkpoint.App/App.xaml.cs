// Arranque de la aplicación: idioma guardado, instancia única y apertura de la biblioteca.
// Los modos de diagnóstico usan directorios aislados para ejecutar las pruebas de interfaz.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using Checkpoint.Core;

namespace Checkpoint.App;

public partial class App : Application
{
    internal static bool Diagnostics { get; private set; }
    internal static bool UseCss { get; private set; } = true;
    private Mutex? singleInstance;

    // Determina el directorio de datos antes de crear el mutex; dos bibliotecas aisladas pueden ejecutarse por separado.
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Diagnostics = e.Args.Any(a => a is "--smoke-test" or "--web-smoke-test" or "--demo");
        UseCss = !e.Args.Contains("--smoke-test");
        int index = Array.IndexOf(e.Args, "--data-dir");
        int smokeIndex = Array.IndexOf(e.Args, "--smoke-test");
        int webSmokeIndex = Array.IndexOf(e.Args, "--web-smoke-test");
        bool explicitDirectory =
            index >= 0
            && index + 1 < e.Args.Length
            && !e.Args[index + 1].StartsWith("--", StringComparison.Ordinal);
        string directory = explicitDirectory
            ? e.Args[index + 1]
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Checkpoint"
            );
        if (smokeIndex >= 0 || webSmokeIndex >= 0)
        {
            if (!explicitDirectory)
                directory = Path.Combine(
                    Path.GetTempPath(),
                    "Checkpoint-smoke-" + Guid.NewGuid().ToString("N")
                );
            if (
                File.Exists(Path.Combine(directory, "checkpoint.db"))
                || File.Exists(Path.Combine(directory, "steam-session.dat"))
                || File.Exists(Path.Combine(directory, "retro-session.dat"))
                || File.Exists(Path.Combine(directory, "checkpoint-session.dat"))
                || Directory.Exists(Path.Combine(directory, "social"))
            )
            {
                Console.Error.WriteLine(
                    I18n.T(
                        "La prueba nativa requiere una carpeta nueva sin biblioteca, sesiones ni publicaciones."
                    )
                );
                Shutdown(1);
                return;
            }
        }
        // Read only the saved language before notices that precede the main window.
        try
        {
            if (File.Exists(Path.Combine(directory, "checkpoint.db")))
            {
                using var connection = new Microsoft.Data.Sqlite.SqliteConnection(
                    new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
                    {
                        DataSource = Path.Combine(directory, "checkpoint.db"),
                        Mode = Microsoft.Data.Sqlite.SqliteOpenMode.ReadOnly,
                        Pooling = false,
                    }.ToString()
                );
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT payload FROM settings WHERE id=1";
                if (command.ExecuteScalar() is string json)
                    I18n.SetLanguage(
                        System
                            .Text.Json.JsonSerializer.Deserialize<Settings>(json, DataJson.Options)
                            ?.Language
                    );
            }
        }
        catch (Exception error)
            when (error
                    is Microsoft.Data.Sqlite.SqliteException
                        or System.Text.Json.JsonException
                        or IOException
            ) { }
        string instanceKey = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(Path.GetFullPath(directory).ToUpperInvariant())
            )
        )[..20];
        singleInstance = new Mutex(true, "Local\\Checkpoint-" + instanceKey, out bool first);
        if (!first)
        {
            LocalizedNotice.Show(
                I18n.T(
                    "Checkpoint ya está abierto. Puedes mostrarlo desde la bandeja o con Ctrl+Alt+C."
                ),
                "Checkpoint"
            );
            Shutdown();
            return;
        }
        DispatcherUnhandledException += (_, args) =>
        {
            args.Handled = true;
            if (e.Args.Contains("--diagnostics"))
            {
                Console.Error.WriteLine(args.Exception);
                Environment.Exit(1);
            }
            if (Current.MainWindow is MainWindow main)
                main.Notice(
                    I18n.T("No se pudo completar la operación: ") + I18n.Error(args.Exception)
                );
        };
        try
        {
            var window = new MainWindow(directory, e.Args.Contains("--demo"));
            MainWindow = window;
            window.Show();
            if (smokeIndex >= 0 && smokeIndex + 1 < e.Args.Length)
                _ = window.RenderSmokeTest(e.Args[smokeIndex + 1]);
            int webIndex = Array.IndexOf(e.Args, "--web-smoke-test");
            if (webIndex >= 0 && webIndex + 1 < e.Args.Length)
                _ = window.RenderWebSmokeTest(e.Args[webIndex + 1]);
        }
        catch (Exception ex)
        {
            LocalizedNotice.Show(
                I18n.T("No se puede abrir la biblioteca. Los archivos se han conservado.\n")
                    + I18n.Error(ex),
                "Checkpoint",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        singleInstance?.Dispose();
        base.OnExit(e);
    }
}
