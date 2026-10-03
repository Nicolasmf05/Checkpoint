using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Checkpoint.Core;

namespace Checkpoint.App;

public partial class MainWindow
{
    internal async Task RenderSteamSmokeTest(string output, Action<bool,string> check)
    {
        const string endpoint = "https://fumdnvvvoiwoiziwtmsu.supabase.co/functions/v1/checkpoint-steam/";
        const string token = "ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ";
        const string steamId = "76561198000000000";
        check(SteamClient.ValidateServiceUrl(endpoint.TrimEnd('/')).AbsoluteUri == endpoint,"Steam client accepts the Supabase function endpoint");
        foreach (string invalid in new[] { "https://evil.example/functions/v1/checkpoint-steam/", "https://test.supabase.co/functions/v1/other/", endpoint+"?key=secret", "https://user:pass@test.supabase.co/", "http://test.supabase.co/" })
        {
            bool rejected = false; try { SteamClient.ValidateServiceUrl(invalid); } catch (ArgumentException) { rejected = true; }
            check(rejected,"Steam endpoint rejects unsafe address: " + invalid.Replace("?key=secret","?key=[redacted]"));
        }
        string folder=Path.Combine(output,"steam-fixture"); Directory.CreateDirectory(folder);
        bool authenticated=false; string? language=null;
        using var client=new SteamClient(folder,new NativeSteamHandler(request=>{
            var uri=request.RequestUri!;
            if(uri.AbsolutePath.EndsWith("/v1/auth/start"))
            {
                check(request.Headers.Authorization is null,"Steam login does not send an unrelated bearer token");
                return SteamResponse(new {flowId=token,pollSecret=token,authorizeUrl="https://steamcommunity.com/openid/login"});
            }
            authenticated=request.Headers.Authorization?.Parameter==token;
            if(uri.AbsolutePath.EndsWith("/v1/library")) return SteamResponse(new {games=new[]{new {appId=620,name="Portal 2",playtimeMinutes=70}}});
            if(uri.AbsolutePath.EndsWith("/achievements"))
            {
                language=uri.Query;return SteamResponse(new {achievements=new[]{new {id="FIRST",name="First",description="",hidden=false,unlocked=true}}});
            }
            if(uri.AbsolutePath.EndsWith("/v1/auth/logout")) return SteamResponse(new {ok=true});
            throw new InvalidOperationException("Unexpected Steam native route");
        }));
        await client.BeginLogin(endpoint,CancellationToken.None);
        client.SaveSession(endpoint,new LoginResult("complete",token,steamId));
        using(var restored=new SteamClient(folder)) check(restored.Session?.Token==token && restored.Session.SteamId==steamId,"Steam DPAPI session persists and restores");
        check(!Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(folder,"steam-session.dat"))).Contains(token),"Steam session is encrypted on disk");
        var library=await client.Library(endpoint,CancellationToken.None);
        check(authenticated && library.Games[0].AppId==620,"Steam library request uses its bound session and parses the response");
        I18n.SetLanguage("en");await client.Achievements(endpoint,620,CancellationToken.None);
        check(language=="?lang=en","English Steam requests select English achievements");
        I18n.SetLanguage("es");await client.Achievements(endpoint,620,CancellationToken.None);
        check(language=="?lang=es","Spanish Steam requests select Spanish achievements");
        using (var errorClient = new SteamClient(Path.Combine(output,"steam-error-fixture"), new NativeSteamHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("{\"error\":\"Unexpected foreign server message\"}") })))
        {
            foreach (var selectedLanguage in new[] { "es", "en" })
            {
                I18n.SetLanguage(selectedLanguage);
                bool localized = false;
                try { await errorClient.BeginLogin(endpoint,CancellationToken.None); }
                catch (InvalidOperationException error) { localized = error.Message == I18n.T("No se pudo consultar Steam. Se conserva el último progreso guardado."); }
                check(localized, "unknown Steam errors retain the selected language " + selectedLanguage);
            }
            I18n.SetLanguage("es");
        }
        bool wrongServer=false;try{await client.Library("https://another.example/",CancellationToken.None);}catch(InvalidOperationException){wrongServer=true;}
        check(wrongServer,"Steam session cannot be sent to a different service");
        bool malformed=false;try{client.SaveSession(endpoint,new LoginResult("complete","bad",steamId));}catch(InvalidDataException){malformed=true;}
        check(malformed && client.Session?.Token==token,"Malformed Steam sessions preserve the existing session");
        await client.Disconnect();check(client.Session is null && !File.Exists(Path.Combine(folder,"steam-session.dat")),"Steam unlink revokes and removes the local session");
    }
    private sealed class NativeSteamHandler(Func<HttpRequestMessage,HttpResponseMessage> respond):HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)=>Task.FromResult(respond(request));
    }
    private static HttpResponseMessage SteamResponse(object value)=>new(HttpStatusCode.OK)
    {Content=new StringContent(JsonSerializer.Serialize(value,DataJson.Options),Encoding.UTF8,"application/json")};
}
