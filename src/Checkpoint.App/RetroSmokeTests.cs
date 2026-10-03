using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Checkpoint.Core;
namespace Checkpoint.App;
public partial class MainWindow
{
    internal async Task RenderRetroSmokeTest(string output,Action<bool,string> check)
    {
        string folder=Path.Combine(output,"retro-fixture");Directory.CreateDirectory(folder);
        const string key="RETRO-KEY-FIXTURE";bool correct=false;
        using var client=new RetroClient(folder,new NativeSteamHandler(request=>
        {
            correct=request.RequestUri!.Host=="retroachievements.org"&&request.RequestUri.AbsolutePath=="/API/API_GetGameInfoAndUserProgress.php"&&request.RequestUri.Query.Contains("g=1")&&request.RequestUri.Query.Contains("u=test_user")&&request.RequestUri.Query.Contains("y="+key);
            return new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("""
            {"ID":1,"Achievements":{"1":{"Title":"First","Description":"One","DateEarned":"2026-01-01 12:00:00"},"2":{"Title":"Hardcore","Description":"Two","DateEarnedHardcore":"2026-01-02 12:00:00"},"3":{"Title":"Pending","Description":"Three"}}}
            """)};
        }));
        client.Save("test_user",key,false);
        using(var restored=new RetroClient(folder))check(restored.Session?.Username=="test_user"&&restored.Session.ApiKey==key,"RetroAchievements DPAPI credentials restore");
        check(!Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(folder,"retro-session.dat"))).Contains(key),"RetroAchievements key is encrypted on disk");
        var achievements=await client.Achievements(1);check(correct&&achievements.Count==3&&achievements[0].Unlocked&&achievements[1].Unlocked&&!achievements[2].Unlocked,"RetroAchievements request uses official endpoint and parses earned and pending achievements");
        client.Save("test_user",key,true);achievements=await client.Achievements(1);
        check(!achievements[0].Unlocked&&achievements[1].Unlocked,"RetroAchievements Hardcore excludes softcore-only completion");
        using var invalid=new RetroClient(folder,new NativeSteamHandler(_=>new HttpResponseMessage(HttpStatusCode.Unauthorized){Content=new StringContent(key)}));
        bool safe=false;try{await invalid.Achievements(1);}catch(InvalidOperationException ex){safe=!ex.Message.Contains(key);}
        check(safe,"RetroAchievements failures do not expose keys or response bodies");
        client.Disconnect();check(client.Session is null&&!File.Exists(Path.Combine(folder,"retro-session.dat")),"RetroAchievements disconnect removes local credentials");
        var game=Games[0];OpenDetectedAchievements(game);OpenDetectedAchievements(game);
        check(achievementWindows.Count==1&&achievementWindows[game.Id].Owner is null&&achievementWindows[game.Id].ShowInTaskbar,"game detection opens one independent nonmodal achievement window");
        achievementWindows[game.Id].Close();check(achievementWindows.Count==0,"closing a detected achievement window releases its registration");
    }
}
