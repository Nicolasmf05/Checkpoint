using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Checkpoint.Core;
namespace Checkpoint.App;
public partial class MainWindow
{
    internal async Task RenderUpdateSmokeTest(string output,Action<bool,string> check)
    {
        byte[] bytes=Encoding.UTF8.GetBytes("Not an installer: isolated update fixture");string digest=Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        string name="Checkpoint-9.0.0-win-x64.msi",origin="https://github.com/Nicolasmf05/Checkpoint/releases/download/v9.0.0/";
        var update=new AppUpdate(new Version(9,0,0),"v9.0.0",name,new(origin+name),new(origin+name+".sha256"),bytes.Length,digest,true);
        var folder=Path.Combine(output,"updates-fixture");
        using var client=new UpdateClient(new NativeSteamHandler(request=>new HttpResponseMessage(HttpStatusCode.OK){Content=request.RequestUri!.AbsolutePath.EndsWith(".sha256")?new StringContent(digest+"  "+name):new ByteArrayContent(bytes)}));
        string file=await client.Download(update,folder);check(File.ReadAllBytes(file).AsSpan().SequenceEqual(bytes)&&!File.Exists(file+".partial"),"updater downloads and promotes only the size- and SHA-256-verified installer");
        check(File.ReadAllText(file+":Zone.Identifier").Contains("ZoneId=3"),"updater preserves the Windows Internet zone marker instead of suppressing security checks");
        using var tampered=new UpdateClient(new NativeSteamHandler(request=>new HttpResponseMessage(HttpStatusCode.OK){Content=request.RequestUri!.AbsolutePath.EndsWith(".sha256")?new StringContent(digest+"  "+name):new ByteArrayContent(new byte[bytes.Length])}));
        bool rejected=false;try{await tampered.Download(update,Path.Combine(folder,"tampered"));}catch(InvalidDataException){rejected=true;}
        check(rejected&&!File.Exists(Path.Combine(folder,"tampered",name))&&!File.Exists(Path.Combine(folder,"tampered",name+".partial")),"tampered update is rejected and partial files are removed without launching anything");
        using var redirect=new UpdateClient(new NativeSteamHandler(_=>{var response=new HttpResponseMessage(HttpStatusCode.Found);response.Headers.Location=new Uri("https://evil.example/installer");return response;}));
        rejected=false;try{await redirect.Check(new Version(1,0,0),"win-x64");}catch(InvalidDataException){rejected=true;}
        check(rejected,"updater rejects redirects to unrelated hosts");
        string data=Path.Combine(folder,"O'Brien $(no-code)");Directory.CreateDirectory(Path.Combine(data,"updates"));
        var start=new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"WindowsPowerShell","v1.0","powershell.exe")){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardError=true,RedirectStandardOutput=true};
        start.ArgumentList.Add("-NoProfile");start.ArgumentList.Add("-NonInteractive");start.ArgumentList.Add("-Command");
        start.ArgumentList.Add(UpdateScript(int.MaxValue,file,new string('0',64),data,Path.Combine(data,"missing.exe"),Path.Combine(data,"missing-new.exe")));
        using var process=Process.Start(start)!;await process.WaitForExitAsync();
        check(process.ExitCode==0&&File.ReadAllText(Path.Combine(data,"updates","result.txt")).Trim()=="failed","update helper handles quoted paths and stops before installation when the second integrity check fails");
        var previous=AvailableUpdate;AvailableUpdate=update;
        RunModal(()=>Dialogs.AppUpdatesDialog(this),window=>
        {
            check(Texts(window).Contains(I18n.T("Actualizaciones"))&&Texts(window).Contains(I18n.T("Versión disponible: ")+"9.0.0"),"native update dialog displays installed and available versions");
            RenderElement(window,Path.Combine(output,"dialog-updates.png"));Click(window,I18n.T("Cerrar"));
        });AvailableUpdate=previous;
    }
}
