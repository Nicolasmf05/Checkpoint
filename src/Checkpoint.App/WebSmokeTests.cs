using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Checkpoint.Core;
using Microsoft.Web.WebView2.Core;

namespace Checkpoint.App;

public partial class MainWindow
{
    internal async Task RenderWebSmokeTest(string output)
    {
        var checks=new List<string>();
        void Check(bool pass,string name) { if (!pass) throw new InvalidOperationException("FAILED: "+name); checks.Add(name); Console.WriteLine("PASS "+name); }
        async Task Wait(Func<Task<bool>> condition)
        {
            for (int attempt=0;attempt<200;attempt++) { if (await condition()) return; await Task.Delay(50); }
            throw new TimeoutException("CSS UI condition timed out");
        }
        async Task<bool> Script(WebSurface surface,string script) => await surface.Browser.CoreWebView2.ExecuteScriptAsync(script) == "true";
        async Task Run(string script) { await web!.Browser.CoreWebView2.ExecuteScriptAsync(script); await Task.Delay(350); }
        async Task<WebSurface> Dialog()
        {
            WebSurface? found=null;
            await Wait(async () => { found=Application.Current.Windows.OfType<Window>().Where(w=>w!=this).Select(w=>w.Tag).OfType<WebSurface>().FirstOrDefault();
                return found?.Browser.CoreWebView2 is not null && await Script(found,"window.checkpointState?.kind === 'dialog'"); });
            return found!;
        }
        async Task Capture(WebSurface surface,string name)
        { using var file=File.Create(Path.Combine(output,name)); await surface.Browser.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png,file); }
        try
        {
            Directory.CreateDirectory(output);
            await Wait(async () => web?.Browser.CoreWebView2 is not null && await Script(web,"window.checkpointState?.kind === 'main'"));
            Check(await Script(web!,"getComputedStyle(document.querySelector('.window')).display === 'flex' && !!document.querySelector('link[href=\"app.css\"]')"),"main window is rendered with local HTML and CSS");
            Check(web!.Browser.DefaultBackgroundColor.A == 0 && AllowsTransparency,"composition web control retains transparent window support");
            Check(!web.Browser.CoreWebView2.Settings.AreHostObjectsAllowed && !web.Browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled,"web host disables native object access and browser context menus");
            Check(await Script(web,"document.documentElement.lang === 'es' && document.querySelector('[data-tab=list]').textContent === 'Mi lista'"),"CSS main navigation uses Spanish");
            var coverGame=Games.First();
            var fixture=System.Windows.Media.Imaging.BitmapSource.Create(1,1,96,96,System.Windows.Media.PixelFormats.Bgra32,null,new byte[] {130,210,170,255},4);
            coverGame.CustomCover=Covers.SavePrepared(WebSurface.ImageBytes(fixture)); Persist(); Refresh();
            await Wait(()=>Script(web,"!![...document.querySelectorAll('.cover')].find(img=>img.tagName==='IMG' && img.complete && img.naturalWidth>0)"));
            Check(await Script(web,"document.querySelector('.game img').src.startsWith('https://checkpoint-images.invalid/game-cover/')"),"CSS covers load through an app-owned image endpoint without filesystem access");
            await Capture(web,"css-widget-es.png");
            await Run("const search=document.querySelector('#search');search.value='Hades';search.dispatchEvent(new Event('input',{bubbles:true}));");
            await Wait(()=>Script(web,"window.checkpointState.games.length===1"));
            Check(visibleCards.Count == 1 && visibleCards[0].Model.Title == "Hades","HTML search updates the existing collection controller");
            await Run("const search=document.querySelector('#search');search.value='';search.dispatchEvent(new Event('input',{bubbles:true}));");
            await Run("document.querySelector('[data-label=add]').click();");
            var add=await Dialog();
            await add.Browser.CoreWebView2.ExecuteScriptAsync("const title=document.querySelector('input[aria-label=\"Nombre del juego\"]');title.value='CSS test game';title.dispatchEvent(new Event('input',{bubbles:true}));const notes=document.querySelector('textarea');notes.value='Offline CSS notes';notes.dispatchEvent(new Event('input',{bubbles:true}));");
            await Task.Delay(350);
            Check(await Script(add,"document.querySelector('textarea').value==='Offline CSS notes'"),"CSS form values survive native acknowledgement and rerendering");
            await Capture(add,"css-editor-es.png");
            await add.Browser.CoreWebView2.ExecuteScriptAsync("[...document.querySelectorAll('button')].find(b=>b.textContent==='Guardar').click();");
            await Wait(()=>Task.FromResult(Games.Any(g=>g.Title=="CSS test game")));
            Check(Store.LoadGames().Single(g=>g.Title=="CSS test game").Notes=="Offline CSS notes","CSS editor saves a game and notes to existing SQLite storage");
            await Run("document.querySelector('[data-label=settings]').click();");
            var settings=await Dialog();
            Check(await Script(settings,"!!document.querySelector('select[aria-label=\"Idioma\"]') && !document.querySelector('input[aria-label=\"Language / Idioma\"]')"),"settings controls render as localized HTML elements");
            await settings.Browser.CoreWebView2.ExecuteScriptAsync("const language=document.querySelector('select[aria-label=\"Idioma\"]');language.value=1;language.dispatchEvent(new Event('change',{bubbles:true}));");
            await Task.Delay(200);
            await settings.Browser.CoreWebView2.ExecuteScriptAsync("[...document.querySelectorAll('button')].find(b=>b.textContent==='Guardar').click();");
            await Wait(()=>Script(web,"document.documentElement.lang==='en'"));
            Check(Preferences.Language=="en" && Store.LoadSettings().Language=="en" && await Script(web,"document.querySelector('[data-tab=library]').textContent==='Library'"),"CSS settings save language and update the main interface immediately");
            await Capture(web,"css-widget-en.png");
            await Run("document.querySelector('[data-tab=friends]').click();");
            await Wait(()=>Script(web,"!!document.querySelector('input[aria-label=\"Checkpoint username\"]')"));
            Check(await Script(web,"!!document.querySelector('input[type=password]') && !JSON.stringify(window.checkpointState).includes('refresh_token')"),"friends login is rendered with CSS without exposing authentication tokens");
            await Run("const password=document.querySelector('input[type=password]');password.value='PASSWORD-FIXTURE';password.dispatchEvent(new Event('input',{bubbles:true}));const name=document.querySelector('input[aria-label=\"Checkpoint username\"]');name.value='css_fixture';name.focus();name.dispatchEvent(new Event('input',{bubbles:true}));");
            Check(await Script(web,"document.querySelector('input[type=password]').value==='PASSWORD-FIXTURE' && !JSON.stringify(window.checkpointState).includes('PASSWORD-FIXTURE')"),"passwords survive form refresh without being included in outbound snapshots");
            await Run("document.querySelector('input[type=password]').value='';document.querySelector('input[type=password]').dispatchEvent(new Event('input',{bubbles:true}));");
            await Capture(web,"css-friends-login-en.png");
            await Run("document.querySelector('[data-tab=list]').click();document.querySelector('[data-label=settings]').click();");
            settings=await Dialog();
            await Capture(settings,"css-settings-en.png");
            await settings.Browser.CoreWebView2.ExecuteScriptAsync("const view=document.querySelector('select[aria-label=\"Collection view\"]');view.value=3;view.dispatchEvent(new Event('change',{bubbles:true}));");
            await Task.Delay(200);
            await settings.Browser.CoreWebView2.ExecuteScriptAsync("[...document.querySelectorAll('button')].find(b=>b.textContent==='Save').click();");
            await Wait(()=>Script(web,"window.checkpointState.mini"));
            Check(await Script(web,"getComputedStyle(document.querySelector('.header')).display==='none' && document.querySelectorAll('.minirow').length===4 && !document.querySelector('.minirow img')"),"CSS Miniature displays only game names and states");
            await Run("document.dispatchEvent(new KeyboardEvent('keydown',{key:'Home',bubbles:true}));document.dispatchEvent(new KeyboardEvent('keydown',{key:'ArrowDown',bubbles:true}));");
            Check(await Script(web,"document.activeElement.matches('.minirow')"),"CSS Miniature keyboard navigation focuses an HTML game row");
            var activeGameId=Guid.Parse(JsonSerializer.Deserialize<string>(await web.Browser.CoreWebView2.ExecuteScriptAsync("document.activeElement.dataset.game"))!);
            await Run("document.dispatchEvent(new KeyboardEvent('keydown',{key:'Enter',bubbles:true}));");
            Check(await Script(web,"document.querySelectorAll('.menu button[role=menuitemradio]').length===7"),"Miniature state and window actions render in an HTML context menu");
            await Run("[...document.querySelectorAll('.menu button')].find(b=>b.textContent.includes('Playing')).click();");
            Check(Store.LoadGames().Single(g=>g.Id==activeGameId).Status==GameStatus.Playing,"HTML state actions persist through the native controller");
            await Capture(web,"css-miniature-en.png");
            Preferences.Language="es"; I18n.SetLanguage("es"); ApplyPreferences(); Refresh(); await Task.Delay(300);
            Check(await Script(web,"document.documentElement.lang==='es' && [...document.querySelectorAll('.minirow .status')].some(node=>node.textContent==='Jugando')"),"CSS Miniature changes all state labels to Spanish");
            await Capture(web,"css-miniature-es.png");
            Preferences.Language="en"; I18n.SetLanguage("en"); ApplyPreferences(); Refresh(); await Task.Delay(300);
            await Run("document.dispatchEvent(new KeyboardEvent('keydown',{key:'F2',bubbles:true}));");
            var edit=await Dialog();
            Check(await Script(edit,"!!document.querySelector('input[aria-label=\"Game title\"]')"),"Miniature F2 opens the CSS game editor");
            await edit.Browser.CoreWebView2.ExecuteScriptAsync("[...document.querySelectorAll('button')].find(b=>b.textContent==='Cancel').click();");
            await Task.Delay(350);
            Check(await Script(web,"document.activeElement.matches('.minirow')"),"canceling a CSS game editor restores Miniature row focus");
            int before=Games.Count;
            await Run("window.chrome.webview.postMessage({action:'state',id:'00000000-0000-0000-0000-000000000000',value:99});");
            Check(Games.Count==before && Games.All(g=>Enum.IsDefined(g.Status)),"invalid game and state commands do not mutate the collection");
            Games.AddRange(Enumerable.Range(0,1000).Select(i=>new Game { Title="CSS scale "+i.ToString("D4"), SortOrder=100+i })); Persist(); Refresh(); await Task.Delay(400);
            Check(await Script(web,"window.checkpointState.games.length===1004 && document.querySelectorAll('.minirow').length<40"),"CSS Miniature virtualizes a collection of 1004 games");
            await Run("document.dispatchEvent(new KeyboardEvent('keydown',{key:'End',bubbles:true}));");
            Check(await Script(web,"document.activeElement.textContent.includes('CSS scale 0999')"),"CSS End reaches a distant virtualized game");
            await Run("document.dispatchEvent(new KeyboardEvent('keydown',{key:'PageUp',bubbles:true}));");
            Check(await Script(web,"document.activeElement.matches('.minirow') && !document.activeElement.textContent.includes('0999')"),"CSS PageUp moves by the viewport and retains focus");
            var malicious=new Game { Title="<img src=x onerror=alert(1)>", SortOrder=-10, Favorite=true }; Games.Add(malicious); Persist(); Refresh();
            await Run("document.dispatchEvent(new KeyboardEvent('keydown',{key:'Home',bubbles:true}));");
            Check(await Script(web,"document.querySelector('.minirow').textContent.includes('<img') && !document.querySelector('.minirow img')"),"game titles render as text rather than executable HTML");
            web.Browser.CoreWebView2.Navigate("https://example.invalid/"); await Task.Delay(200);
            Check(web.Browser.CoreWebView2.Source==WebSurface.Origin+"index.html","navigation to remote content is blocked before loading");
            await Run("document.dispatchEvent(new KeyboardEvent('keydown',{key:'Enter',bubbles:true}));[...document.querySelectorAll('.menu button')].find(b=>b.textContent==='Exit miniature view').click();");
            Check(!Preferences.MiniatureView && await Script(web,"!window.checkpointState.mini && getComputedStyle(document.querySelector('.header')).display==='flex'"),"HTML exit action restores the normal view and window dimensions");
            Games.RemoveAll(g=>g.Title.StartsWith("CSS scale ",StringComparison.Ordinal) || g.Id==malicious.Id); Persist();
            Preferences.GridView=true; Preferences.Compact=false; Preferences.LightTheme=false; ApplyPreferences(); Refresh(); await Task.Delay(300);
            Check(await Script(web,"document.querySelectorAll('.game.grid').length===4 && !window.checkpointState.mini"),"CSS grid renders the normal collection after Miniature");
            await Capture(web,"css-grid-en.png");
            Preferences.GridView=false; Preferences.Compact=true; Preferences.LightTheme=true; ApplyPreferences(); Refresh(); await Task.Delay(300);
            Check(await Script(web,"document.documentElement.dataset.theme==='light' && document.querySelectorAll('.game.compact').length===4"),"CSS compact view and light theme follow native preferences");
            await Capture(web,"css-compact-light-en.png");
            var movedId=visibleCards.Last().Model.Id;
            await Run("document.querySelector('.game:last-child .game-tools button:last-child').dispatchEvent(new KeyboardEvent('keydown',{key:'ArrowUp',altKey:true,bubbles:true}));");
            Check(visibleCards[^2].Model.Id==movedId && Store.LoadGames().Single(g=>g.Id==movedId).SortOrder==Games.Single(g=>g.Id==movedId).SortOrder,"CSS keyboard ordering changes and persists the collection");
            Preferences.Compact=false; Preferences.LightTheme=false; ApplyPreferences(); Refresh();
            _ = Dispatcher.BeginInvoke(new Action(()=>LocalizedNotice.Show(this,I18n.T("No se pudo completar la operación. Vuelve a intentarlo."),"Checkpoint")));
            var notice=await Dialog();
            Check(await Script(notice,"document.body.innerText.includes('The operation could not be completed') && [...document.querySelectorAll('button')].some(b=>b.textContent==='OK')"),"app notices render English text and buttons in CSS");
            await Capture(notice,"css-notice-en.png");
            await notice.Browser.CoreWebView2.ExecuteScriptAsync("document.querySelector('button').click();"); await Task.Delay(200);
            File.WriteAllText(Path.Combine(output,"web-smoke.json"),JsonSerializer.Serialize(new { ok=true, checks=checks.Count, names=checks },DataJson.Options));
            Console.WriteLine("CSS smoke test passed: "+checks.Count+" checks, "+output);
        }
        catch (Exception error)
        { Console.Error.WriteLine(error); Environment.ExitCode=1; File.WriteAllText(Path.Combine(output,"web-smoke.json"),JsonSerializer.Serialize(new {ok=false,checks=checks.Count,error=error.ToString()},DataJson.Options)); }
        finally { Exit(); }
    }
}
