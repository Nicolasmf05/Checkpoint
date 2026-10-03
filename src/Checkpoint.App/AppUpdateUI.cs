using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Checkpoint.Core;
namespace Checkpoint.App;
public partial class MainWindow
{
    internal readonly UpdateClient Updates=new();
    internal AppUpdate? AvailableUpdate;
    internal static Version CurrentVersion=>typeof(MainWindow).Assembly.GetName().Version!;
    internal static string UpdateRuntime=>RuntimeInformation.ProcessArchitecture==Architecture.Arm64?"win-arm64":"win-x64";
    private async Task CheckUpdatesOnStartup()
    {
        if(App.Diagnostics)return;
        string result=Path.Combine(Store.DirectoryPath,"updates","result.txt");
        try{if(File.Exists(result)){string status=File.ReadAllText(result).Trim();File.Delete(result);Notice(I18n.T(status=="ok"?"Checkpoint se ha actualizado. Tu biblioteca se conserva.":"No se pudo instalar la actualización. Se conserva la biblioteca; revisa Actualizaciones en Ajustes."));return;}}
        catch(IOException){}
        if(!Preferences.AutomaticUpdates||DateTimeOffset.UtcNow-Preferences.LastUpdateCheck<TimeSpan.FromHours(24))return;
        try
        {
            Preferences.LastUpdateCheck=DateTimeOffset.UtcNow;Store.SaveSettings(Preferences);
            AvailableUpdate=await Updates.Check(CurrentVersion,UpdateRuntime,shutdown.Token);
            if(!shutdown.IsCancellationRequested&&AvailableUpdate is {} update)Notice(I18n.T("Hay una actualización de Checkpoint: ")+update.Version+I18n.T(". Abre Ajustes → Actualizaciones para instalarla."));
        }
        catch(Exception ex)when(ex is IOException or System.Net.Http.HttpRequestException or InvalidOperationException or OperationCanceledException or System.Text.Json.JsonException){}
    }
    internal static string UpdateScript(int processId,string installer,string digest,string dataDirectory,string currentExe,string installedExe,bool startup=false)
    {
        static string Quote(string value)=>"'"+value.Replace("'","''")+"'";
        string result=Path.Combine(dataDirectory,"updates","result.txt");
        string shortcuts=" $checkpointShell=New-Object -ComObject WScript.Shell; $checkpointShortcut=$checkpointShell.CreateShortcut("+Quote(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),"Checkpoint","Checkpoint.lnk"))+"); $checkpointShortcut.TargetPath="+Quote(installedExe)+"; $checkpointShortcut.WorkingDirectory="+Quote(Path.GetDirectoryName(installedExe)!)+"; $checkpointShortcut.Arguments='--data-dir \"'+"+Quote(dataDirectory)+"+'\"'; $checkpointShortcut.Save();";
        if(startup)shortcuts+=" $checkpointStartup=$checkpointShell.CreateShortcut("+Quote(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup),"Checkpoint.lnk"))+"); $checkpointStartup.TargetPath="+Quote(installedExe)+"; $checkpointStartup.WorkingDirectory="+Quote(Path.GetDirectoryName(installedExe)!)+"; $checkpointStartup.Arguments='--data-dir \"'+"+Quote(dataDirectory)+"+'\"'; $checkpointStartup.Save();";
        return "$ErrorActionPreference='Stop'; $checkpointStatus='failed'; $checkpointLaunch="+Quote(currentExe)+"; try { $checkpointParent=Get-Process -Id "+processId+" -ErrorAction SilentlyContinue; if($checkpointParent){$checkpointParent | Wait-Process -Timeout 90}; if((Get-FileHash -LiteralPath "+Quote(installer)+" -Algorithm SHA256).Hash -ne "+Quote(digest)+"){throw 'Integrity'}; $checkpointInstaller=Start-Process -FilePath (Join-Path $env:SystemRoot 'System32/msiexec.exe') -ArgumentList @('/i',('\"'+"+Quote(installer)+"+'\"'),'/passive','/norestart') -PassThru -Wait; if($checkpointInstaller.ExitCode -in @(0,3010)){$checkpointStatus='ok'; $checkpointLaunch="+Quote(installedExe)+";"+shortcuts+"} } catch {} ; Set-Content -LiteralPath "+Quote(result)+" -Value $checkpointStatus -Encoding UTF8; if(Test-Path -LiteralPath $checkpointLaunch){Start-Process -FilePath $checkpointLaunch -ArgumentList @('--data-dir',('\"'+"+Quote(dataDirectory)+"+'\"')) -WindowStyle Normal}";
    }
    internal void InstallUpdate(AppUpdate update,string file)
    {
        if(App.Diagnostics)throw new InvalidOperationException("Installer launch disabled in diagnostics");
        Persist();string folder=Path.Combine(Store.DirectoryPath,"updates");
        ExportBackup(Path.Combine(folder,"before-"+update.Version+"-"+DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss")+".checkpoint"));
        string installed=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","Checkpoint","Checkpoint.exe");
        var start=new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"WindowsPowerShell","v1.0","powershell.exe")){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,WorkingDirectory=folder};
        start.ArgumentList.Add("-NoProfile");start.ArgumentList.Add("-NonInteractive");start.ArgumentList.Add("-Command");
        start.ArgumentList.Add(UpdateScript(Environment.ProcessId,file,update.Digest,Store.DirectoryPath,Environment.ProcessPath!,installed,Preferences.StartWithWindows));
        using var helper=Process.Start(start)??throw new InvalidOperationException(I18n.T("No se pudo iniciar el instalador. Puedes descargar la versión desde GitHub."));
        Exit();
    }
}
internal static partial class Dialogs
{
    internal static void AppUpdatesDialog(MainWindow owner,Window? parent=null)
    {
        var window=Modal(owner,I18n.T("Actualizaciones · Checkpoint"),560,600);if(parent is not null)window.Owner=parent;
        var body=Panel();Layout(window,body,out var footer);
        Heading(body,I18n.T("Actualizaciones"),I18n.T("Versión instalada: ")+MainWindow.CurrentVersion.ToString(3));
        var automatic=Check(body,I18n.T("Comprobar actualizaciones al abrir (una vez al día)"),owner.Preferences.AutomaticUpdates);
        automatic.Checked+=(_,_)=>{owner.Preferences.AutomaticUpdates=true;owner.Store.SaveSettings(owner.Preferences);};automatic.Unchecked+=(_,_)=>{owner.Preferences.AutomaticUpdates=false;owner.Store.SaveSettings(owner.Preferences);};
        body.Children.Add(new TextBlock{Text=I18n.T("Solo se consulta GitHub. Descargar e instalar requiere pulsar el botón. Incluye las versiones preliminares publicadas de Checkpoint."),TextWrapping=TextWrapping.Wrap});
        body.Children.Add(new TextBlock{Text=I18n.T("Se guarda una copia antes de instalar. La actualización usa el MSI por usuario y reinicia Checkpoint conservando tu carpeta de datos. Si usas el portable, abre después la aplicación del menú Inicio."),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,0)});
        body.Children.Add(new TextBlock{Text=I18n.T("El instalador sigue sin certificado de firma. La verificación SHA-256 no elimina los avisos de SmartScreen."),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,0)});
        var status=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,16,0,12)};body.Children.Add(status);
        using var cancellation=new CancellationTokenSource();bool busy=false,closed=false,checkedVersion=false;
        Button install=null!,check=null!;
        install=Button(I18n.T("Descargar e instalar"),async(_,_)=>
        {
            if(busy||owner.AvailableUpdate is not {} update)return;busy=true;installState();
            try
            {
                status.Text=I18n.T("Descargando actualización…");
                var progress=new Progress<int>(percent=>{if(!closed)status.Text=I18n.T("Descargando actualización…")+" "+percent+" %";});
                string file=await owner.Updates.Download(update,Path.Combine(owner.Store.DirectoryPath,"updates"),progress,cancellation.Token);
                if(closed)return;owner.InstallUpdate(update,file);
            }
            catch(Exception ex){if(!closed)status.Text=I18n.Error(ex);}
            finally{busy=false;if(!closed)installState();}
        },true);
        check=Button(I18n.T("Buscar actualizaciones"),async(_,_)=>
        {
            if(busy)return;busy=true;installState();status.Text=I18n.T("Buscando actualizaciones…");
            try{var update=await owner.Updates.Check(MainWindow.CurrentVersion,MainWindow.UpdateRuntime,cancellation.Token);if(closed)return;owner.AvailableUpdate=update;checkedVersion=true;owner.Preferences.LastUpdateCheck=DateTimeOffset.UtcNow;owner.Store.SaveSettings(owner.Preferences);Render();}
            catch(Exception ex){if(!closed)status.Text=I18n.Error(ex);}
            finally{busy=false;if(!closed)installState();}
        });
        void installState(){install.IsEnabled=!busy&&owner.AvailableUpdate is not null;check.IsEnabled=!busy;automatic.IsEnabled=!busy;}
        void Render(){status.Text=owner.AvailableUpdate is {} update?I18n.T("Versión disponible: ")+update.Version:I18n.T(checkedVersion?"Checkpoint está actualizado.":"Busca actualizaciones para comprobar si hay una versión nueva.");}
        body.Children.Add(check);body.Children.Add(install);
        body.Children.Add(Button(I18n.T("Ver versiones en GitHub"),(_,_)=>Process.Start(new ProcessStartInfo(owner.AvailableUpdate?.ReleaseUrl??"https://github.com/Nicolasmf05/Checkpoint/releases"){UseShellExecute=true})));
        footer.Children.Add(Button(I18n.T("Cerrar"),(_,_)=>window.Close()));window.Closed+=(_,_)=>{closed=true;cancellation.Cancel();};Render();installState();window.ShowDialog();
    }
}
