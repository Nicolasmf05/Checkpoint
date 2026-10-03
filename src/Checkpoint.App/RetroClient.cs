using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Checkpoint.Core;
namespace Checkpoint.App;
public sealed record RetroSession(string Username,string ApiKey,bool Hardcore);
public sealed class RetroClient : IDisposable
{
    private readonly HttpClient http;private readonly string file;
    public RetroSession? Session {get;private set;}
    public RetroClient(string directory,HttpMessageHandler? handler=null)
    {
        file=Path.Combine(directory,"retro-session.dat");http=new(handler??new HttpClientHandler{AllowAutoRedirect=false}){Timeout=TimeSpan.FromSeconds(25),MaxResponseContentBufferSize=12_000_000};
        try{if(File.Exists(file))Session=JsonSerializer.Deserialize<RetroSession>(ProtectedData.Unprotect(File.ReadAllBytes(file),null,DataProtectionScope.CurrentUser),DataJson.Options);}
        catch(Exception ex)when(ex is IOException or CryptographicException or JsonException){Session=null;}
    }
    public void Save(string username,string key,bool hardcore)
    {
        if(string.IsNullOrWhiteSpace(username)||username.Length>80||string.IsNullOrWhiteSpace(key)||key.Length>256)throw new ArgumentException(I18n.T("Introduce tu usuario y clave web de RetroAchievements."));
        var session=new RetroSession(username.Trim(),key.Trim(),hardcore);
        var bytes=ProtectedData.Protect(JsonSerializer.SerializeToUtf8Bytes(session,DataJson.Options),null,DataProtectionScope.CurrentUser);
        File.WriteAllBytes(file+".tmp",bytes);File.Move(file+".tmp",file,true);Session=session;
    }
    public void Disconnect(){Session=null;if(File.Exists(file))File.Delete(file);}
    public async Task<List<Achievement>> Achievements(int id,CancellationToken cancellation=default)
    {
        if(Session is not {} session)throw new InvalidOperationException(I18n.T("Vincula RetroAchievements en Ajustes para consultar sus logros."));
        if(id<=0)throw new ArgumentException(I18n.T("El ID de RetroAchievements debe ser un número positivo."));
        var url="https://retroachievements.org/API/API_GetGameInfoAndUserProgress.php?g="+id+"&u="+Uri.EscapeDataString(session.Username)+"&y="+Uri.EscapeDataString(session.ApiKey);
        try
        {
            using var request=new HttpRequestMessage(HttpMethod.Get,url);request.Headers.UserAgent.ParseAdd("Checkpoint/0.8");
            using var response=await http.SendAsync(request,cancellation);if(!response.IsSuccessStatusCode)throw new InvalidOperationException(I18n.T("No se pudo consultar RetroAchievements. Revisa tu clave y conserva el último progreso."));
            using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellation));var root=json.RootElement;
            if(root.ValueKind!=JsonValueKind.Object||!root.TryGetProperty("ID",out var gameId)||gameId.ToString()!=id.ToString()||!root.TryGetProperty("Achievements",out var achievements))throw new InvalidDataException(I18n.T("RetroAchievements devolvió datos no válidos."));
            var results=new List<Achievement>();
            if(achievements.ValueKind==JsonValueKind.Object)foreach(var entry in achievements.EnumerateObject())
            {
                var a=entry.Value;if(a.ValueKind!=JsonValueKind.Object)throw new InvalidDataException(I18n.T("RetroAchievements devolvió datos no válidos."));string Read(string name)=>a.TryGetProperty(name,out var p)&&p.ValueKind==JsonValueKind.String?p.GetString()??"":"";
                string earned=session.Hardcore?Read("DateEarnedHardcore"):Read("DateEarned");if(!session.Hardcore&&earned.Length==0)earned=Read("DateEarnedHardcore");
                results.Add(new(){Id=entry.Name,Name=Read("Title"),Description=Read("Description"),Unlocked=DateTimeOffset.TryParse(earned,CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal,out _),UnlockedAt=DateTimeOffset.TryParse(earned,CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal,out var date)?date:null});
                if(results.Count>10000)throw new InvalidDataException(I18n.T("RetroAchievements devolvió datos no válidos."));
            }
            else if(achievements.ValueKind!=JsonValueKind.Array||achievements.GetArrayLength()!=0)throw new InvalidDataException(I18n.T("RetroAchievements devolvió datos no válidos."));
            var validation=new Game{Title="RetroAchievements",RetroAchievements=results};
            try{GameRules.Validate(validation);}catch(ArgumentException){throw new InvalidDataException(I18n.T("RetroAchievements devolvió datos no válidos."));}
            return results;
        }
        catch(HttpRequestException){throw new InvalidOperationException(I18n.T("RetroAchievements no responde. Se conserva el progreso anterior."));}
        catch(JsonException){throw new InvalidDataException(I18n.T("RetroAchievements devolvió datos no válidos."));}
    }
    public void Dispose()=>http.Dispose();
}
