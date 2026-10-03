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
        async Task Run(string script) { await web!.Browser.CoreWebView2.ExecuteScriptAsync("(() => {"+script+"})()"); await Task.Delay(350); }
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
            Check(await Script(web,"document.querySelector('.shortcutbar').textContent.includes('Ctrl+N') && document.querySelector('.shortcutbar').textContent.includes('F6') && document.querySelector('[data-label=add]').getAttribute('aria-keyshortcuts')==='Control+N'"),"useful shortcuts are visible and controls expose accessible key gestures");
            await Run("document.querySelector('.shortcut-button').click();");
            Check(await Script(web,"document.querySelector('.shortcut-help:modal h2').textContent==='Atajos de teclado' && document.querySelector('.shortcut-help').textContent.includes('Intro / Espacio') && document.querySelector('.shortcut-help').textContent.includes('Ctrl+Alt+C')"),"shortcut button opens a localized Spanish guide with global and Miniature keys");
            var helpGameCount=Games.Count;
            await Run("document.querySelector('.shortcut-help').dispatchEvent(new KeyboardEvent('keydown',{key:'n',ctrlKey:true,bubbles:true}));");
            Check(Games.Count==helpGameCount && Application.Current.Windows.Count==1 && await Script(web,"!!document.querySelector('.shortcut-help:modal') && document.querySelector('.shortcut-help').contains(document.activeElement)"),"shortcut guide is modal and prevents underlying game commands");
            await Capture(web,"css-shortcuts-es.png");
            await Run("document.dispatchEvent(new KeyboardEvent('keydown',{key:'Escape',bubbles:true}));");
            Check(IsVisible && await Script(web,"!document.querySelector('.shortcut-help') && document.activeElement.matches('.shortcut-button')"),"Escape closes shortcut help and restores focus without hiding the widget");
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
            var achievementFixture=Games.Single(g=>g.Title=="CSS test game");
            achievementFixture.Achievements=[new(){Id="api-first",Name="API fixture",Description="Official pending"}];
            Refresh();await Wait(()=>Script(web,"!!document.querySelector('.achievement-link')"));
            await Run("document.querySelector('[data-game=\""+achievementFixture.Id+"\"] .achievement-link').click();");var achievementDialog=await Dialog();
            Check(await Script(achievementDialog,"!!document.querySelector('.achievement-summary progress') && !document.body.innerText.includes('Official pending')"),"library opens achievement overview directly with collapsed descriptions");
            await achievementDialog.Browser.CoreWebView2.ExecuteScriptAsync("[...document.querySelectorAll('button')].find(b=>b.textContent==='Ver descripción').click();");
            await Wait(()=>Script(achievementDialog,"document.body.innerText.includes('Official pending')"));
            Check(await Script(achievementDialog,"!!document.querySelector('.achievement-description')"),"achievement description expands inside its emphasized CSS card");
            Check(await Script(achievementDialog,"document.body.innerText.includes('No desbloquean logros en Steam') && !!document.querySelector('input[aria-label=\"Nombre del logro manual\"]')"),"detected achievement window renders localized manual controls in CSS");
            await achievementDialog.Browser.CoreWebView2.ExecuteScriptAsync("const name=document.querySelector('input[aria-label=\"Nombre del logro manual\"]');name.value='CSS manual goal';name.dispatchEvent(new Event('input',{bubbles:true}));[...document.querySelectorAll('button')].find(b=>b.textContent==='Añadir logro manual').click();");
            await Wait(()=>Task.FromResult(achievementFixture.ManualAchievements.Count==1));
            Check(Store.LoadGames().Single(g=>g.Id==achievementFixture.Id).ManualAchievements.Count==1,"CSS manual achievement add persists to SQLite");
            await achievementDialog.Browser.CoreWebView2.ExecuteScriptAsync("[...document.querySelectorAll('input[type=checkbox]')].at(-1).click();");
            await Wait(()=>Task.FromResult(AchievementTracking.Items(achievementFixture).Last().Completed));
            Check(!achievementFixture.ManualAchievements[0].Unlocked,"CSS completion retains official source status separately");
            await achievementDialog.Browser.CoreWebView2.ExecuteScriptAsync("[...document.querySelectorAll('button')].find(b=>b.textContent==='Quitar de mi lista').click();");
            await Wait(()=>Task.FromResult(achievementFixture.RemovedAchievements.Count==1));
            Check(achievementFixture.Achievements.Count==1,"CSS removal hides an API achievement without altering its raw data");
            await achievementDialog.Browser.CoreWebView2.ExecuteScriptAsync("[...document.querySelectorAll('button')].find(b=>b.textContent==='Restaurar logros quitados').click();");
            await Wait(()=>Task.FromResult(achievementFixture.RemovedAchievements.Count==0));
            await Capture(achievementDialog,"css-achievements-es.png");
            await achievementDialog.Browser.CoreWebView2.ExecuteScriptAsync("[...document.querySelectorAll('button')].find(b=>b.textContent==='Cerrar').click();");
            await Run("document.querySelector('[data-label=settings]').click();");
            var settings=await Dialog();
            Check(await Script(settings,"!!document.querySelector('select[aria-label=\"Idioma\"]') && document.querySelector('select[aria-label=\"Tema\"]').options[3].textContent==='Océano' && !document.querySelector('input[aria-label=\"Language / Idioma\"]')"),"settings controls render as localized HTML elements");
            await settings.Browser.CoreWebView2.ExecuteScriptAsync("const language=document.querySelector('select[aria-label=\"Idioma\"]');language.value=1;language.dispatchEvent(new Event('change',{bubbles:true}));");
            await Task.Delay(200);
            await settings.Browser.CoreWebView2.ExecuteScriptAsync("[...document.querySelectorAll('button')].find(b=>b.textContent==='Guardar').click();");
            await Wait(()=>Script(web,"document.documentElement.lang==='en'"));
            Check(Preferences.Language=="en" && Store.LoadSettings().Language=="en" && await Script(web,"document.querySelector('[data-tab=library]').textContent==='Library'"),"CSS settings save language and update the main interface immediately");
            var englishAchievements=new Game{Title="Achievement example",Achievements=[new(){Id="first",Name="First steps",Description="Complete the first chapter"}],RetroAchievements=[new(){Id="retro-first",Name="Retro goal",Description="Finish the first level"}]};
            Games.Add(englishAchievements);var personal=AchievementTracking.Add(englishAchievements,"Personal challenge","Finish without hints");englishAchievements.AchievementOverrides["manual:"+personal.Id]=true;
            OpenDetectedAchievements(englishAchievements);var englishAchievementDialog=await Dialog();
            Controls<System.Windows.Controls.CheckBox>(achievementWindows[englishAchievements.Id]).Single(c=>(string?)c.Content=="Show pending only").IsChecked=false;
            await Wait(()=>Script(englishAchievementDialog,"document.body.innerText.includes('Personal challenge') && document.body.innerText.includes('Completed in Checkpoint')"));
            Check(await Script(englishAchievementDialog,"document.body.innerText.includes('Steam') && document.body.innerText.includes('RetroAchievements') && !document.body.innerText.includes('Completado en')"),"English achievement window separates providers and local completion with translated controls");
            await englishAchievementDialog.Browser.CoreWebView2.ExecuteScriptAsync("[...document.querySelectorAll('button')].find(b=>b.textContent==='Show description').click();");
            await Wait(()=>Script(englishAchievementDialog,"document.body.innerText.includes('Complete the first chapter')"));
            await Capture(englishAchievementDialog,"css-achievements-en.png");achievementWindows[englishAchievements.Id].Close();Games.Remove(englishAchievements);
            await Run("document.dispatchEvent(new KeyboardEvent('keydown',{key:'F1',bubbles:true}));");
            Check(await Script(web,"document.querySelector('.shortcut-help:modal h2').textContent==='Keyboard shortcuts' && document.querySelector('.shortcut-help').textContent.includes('Enter / Space') && !document.querySelector('.shortcut-help').textContent.includes('Atajos')"),"F1 opens shortcut help entirely in the selected English language");
            await Capture(web,"css-shortcuts-en.png");
            await Run("document.dispatchEvent(new KeyboardEvent('keydown',{key:'Escape',bubbles:true}));");
            await Run("document.querySelector('[data-label=settings]').click();");
            var themeSettings=await Dialog();
            await themeSettings.Browser.CoreWebView2.ExecuteScriptAsync("[...document.querySelectorAll('button')].find(b=>b.textContent==='Updates').click();");
            WebSurface? updateDialog=null;
            await Wait(async()=>{updateDialog=Application.Current.Windows.OfType<Window>().FirstOrDefault(w=>w.Title=="Updates · Checkpoint")?.Tag as WebSurface;return updateDialog?.Browser.CoreWebView2 is not null&&await Script(updateDialog,"window.checkpointState?.kind==='dialog'");});
            Check(await Script(updateDialog!,"document.body.innerText.includes('Installed version:') && document.body.innerText.includes('once a day') && [...document.querySelectorAll('button')].some(b=>b.textContent==='Download and install' && b.disabled) && !document.body.innerText.includes('Descargar e instalar')"),"CSS update dialog renders English daily checking, installed version and disabled installation without a release");
            await Capture(updateDialog!,"css-updates-en.png");await updateDialog!.Browser.CoreWebView2.ExecuteScriptAsync("[...document.querySelectorAll('button')].find(b=>b.textContent==='Close').click();");
            Check(await Script(themeSettings,"document.querySelector('select[aria-label=\"Theme\"]').options.length===8 && document.body.innerText.includes('Instant preview') && [...document.querySelector('select[aria-label=\"Theme\"]').options].some(option=>option.textContent==='High contrast')"),"settings offer eight localized themes with preview instructions");
            string initialTheme=Themes.Id(Preferences);
            var paletteColors=new HashSet<string>();
            for(int index=0;index<Themes.Ids.Count;index++)
            {
                string id=Themes.Ids[index];
                await themeSettings.Browser.CoreWebView2.ExecuteScriptAsync("(() => {const theme=document.querySelector('select[aria-label=\"Theme\"]');theme.value="+index+";theme.dispatchEvent(new Event('change',{bubbles:true}));})()");
                await Wait(async ()=> await Script(web,"document.documentElement.dataset.theme==='"+id+"'") && await Script(themeSettings,"document.documentElement.dataset.theme==='"+id+"'"));
                Check(Themes.Id(Preferences)==id && await Script(themeSettings,"document.querySelector('select[aria-label=\"Theme\"]').value==='"+index+"'"),"theme previews immediately in both main and settings windows: "+id);
                paletteColors.Add((await web.Browser.CoreWebView2.ExecuteScriptAsync("getComputedStyle(document.querySelector('.window')).backgroundColor")));
                await Capture(web,"css-theme-"+id+"-en.png");
            }
            Check(paletteColors.Count==8,"all eight theme palettes have distinct rendered backgrounds");
            await themeSettings.Browser.CoreWebView2.ExecuteScriptAsync("[...document.querySelectorAll('button')].find(button=>button.textContent==='Cancel').click();");
            await Wait(()=>Script(web,"document.documentElement.dataset.theme==='"+initialTheme+"'"));
            Check(Themes.Id(Preferences)==initialTheme && Themes.Id(Store.LoadSettings())==initialTheme,"canceling preview restores and persists the previous theme");
            await Run("document.querySelector('[data-label=settings]').click();");
            themeSettings=await Dialog();
            await themeSettings.Browser.CoreWebView2.ExecuteScriptAsync("const theme=document.querySelector('select[aria-label=\"Theme\"]');theme.value=3;theme.dispatchEvent(new Event('change',{bubbles:true}));");
            await Wait(()=>Script(web,"document.documentElement.dataset.theme==='ocean'"));
            await themeSettings.Browser.CoreWebView2.ExecuteScriptAsync("[...document.querySelectorAll('button')].find(button=>button.textContent==='Save').click();");
            await Run("document.querySelector('[data-label=settings]').click();");
            themeSettings=await Dialog();
            Check(Themes.Id(Store.LoadSettings())=="ocean" && await Script(themeSettings,"document.querySelector('select[aria-label=\"Theme\"]').value==='3'"),"saving a theme persists it and restores the selected option when reopening settings");
            await Capture(themeSettings,"css-theme-settings-en.png");
            await themeSettings.Browser.CoreWebView2.ExecuteScriptAsync("[...document.querySelectorAll('button')].find(button=>button.textContent==='Cancel').click();");
            await Capture(web,"css-widget-en.png");
            await Run("document.querySelector('[data-tab=friends]').click();");
            await Wait(()=>Script(web,"!!document.querySelector('input[aria-label=\"Checkpoint username\"]')"));
            Check(await Script(web,"document.documentElement.dataset.theme==='ocean' && !!document.querySelector('input[type=password]') && !JSON.stringify(window.checkpointState).includes('refresh_token')"),"friends login is rendered with CSS without exposing authentication tokens");
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
            Check(await Script(web,"document.documentElement.dataset.theme==='ocean' && getComputedStyle(document.querySelector('.window')).getPropertyValue('--accent').trim()==='#6edcf7'"),"Miniature retains the selected custom theme");
            Check(await Script(web,"getComputedStyle(document.querySelector('.header')).display==='none' && document.querySelectorAll('.minirow').length===4 && !document.querySelector('.minirow img')"),"CSS Miniature displays only game names and states");
            await Run("document.dispatchEvent(new KeyboardEvent('keydown',{key:'Home',bubbles:true}));document.dispatchEvent(new KeyboardEvent('keydown',{key:'ArrowDown',bubbles:true}));");
            Check(await Script(web,"document.activeElement.matches('.minirow')"),"CSS Miniature keyboard navigation focuses an HTML game row");
            var activeGameId=Guid.Parse(JsonSerializer.Deserialize<string>(await web.Browser.CoreWebView2.ExecuteScriptAsync("document.activeElement.dataset.game"))!);
            await Run("document.dispatchEvent(new KeyboardEvent('keydown',{key:'Enter',bubbles:true}));");
            Check(await Script(web,"document.querySelectorAll('.menu button[role=menuitemradio]').length===10+window.checkpointState.collections.length"),"Miniature state and window actions render in an HTML context menu");
            await Run("[...document.querySelectorAll('.menu button')].find(b=>b.textContent.includes('Playing')).click();");
            Check(Store.LoadGames().Single(g=>g.Id==activeGameId).Status==GameStatus.Playing,"HTML state actions persist through the native controller");
            Check(await Script(web,"document.querySelector('.mini-exit').getAttribute('aria-label')==='Exit miniature view' && document.querySelector('.mini-exit').getBoundingClientRect().bottom<=document.querySelector('.viewport').getBoundingClientRect().top"),"Miniature offers a visible English exit button above the game list");
            await Capture(web,"css-miniature-en.png");
            Preferences.Language="es"; I18n.SetLanguage("es"); ApplyPreferences(); Refresh(); await Task.Delay(300);
            Check(await Script(web,"document.documentElement.lang==='es' && [...document.querySelectorAll('.minirow .status')].some(node=>node.textContent==='Jugando')"),"CSS Miniature changes all state labels to Spanish");
            Check(await Script(web,"document.querySelector('.mini-exit').textContent.includes('Salir de miniatura') && !document.querySelector('.mini-exit').textContent.includes('Exit')"),"Miniature exit button follows the selected Spanish language");
            await Capture(web,"css-miniature-es.png");
            bool previousGrid=Preferences.GridView,previousCompact=Preferences.Compact;
            await Run("document.querySelector('.mini-exit').click();");
            Check(!Preferences.MiniatureView && !Store.LoadSettings().MiniatureView && Preferences.GridView==previousGrid && Preferences.Compact==previousCompact && await Script(web,"!window.checkpointState.mini && getComputedStyle(document.querySelector('.mini-controls')).display==='none'"),"clicking visible Miniature exit restores and saves the previous normal layout");
            Preferences.MiniatureView=true;ApplyPreferences();Persist();Refresh();await Task.Delay(300);
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
            Preferences.GridView=true; Preferences.Compact=false; Preferences.LightTheme=false; Preferences.Theme="dark"; ApplyPreferences(); Refresh(); await Task.Delay(300);
            Check(await Script(web,"document.querySelectorAll('.game.grid').length===4 && !window.checkpointState.mini"),"CSS grid renders the normal collection after Miniature");
            await Capture(web,"css-grid-en.png");
            Preferences.GridView=false; Preferences.Compact=true; Preferences.LightTheme=true; Preferences.Theme="light"; ApplyPreferences(); Refresh(); await Task.Delay(300);
            Check(await Script(web,"document.documentElement.dataset.theme==='light' && document.querySelectorAll('.game.compact').length===4"),"CSS compact view and light theme follow native preferences");
            await Capture(web,"css-compact-light-en.png");
            var movedId=visibleCards.Last().Model.Id;
            await Run("document.querySelector('.game:last-child .game-tools button:last-child').dispatchEvent(new KeyboardEvent('keydown',{key:'ArrowUp',altKey:true,bubbles:true}));");
            Check(visibleCards[^2].Model.Id==movedId && Store.LoadGames().Single(g=>g.Id==movedId).SortOrder==Games.Single(g=>g.Id==movedId).SortOrder,"CSS keyboard ordering changes and persists the collection");
            Preferences.Compact=false; Preferences.LightTheme=false; Preferences.Theme="dark"; ApplyPreferences(); Refresh();
            _ = Dispatcher.BeginInvoke(new Action(()=>LocalizedNotice.Show(this,I18n.T("No se pudo completar la operación. Vuelve a intentarlo."),"Checkpoint")));
            var notice=await Dialog();
            Preferences.Theme="plum"; ApplyPreferences(); Refresh();
            await Wait(()=>Script(notice,"document.documentElement.dataset.theme==='plum'"));
            Check(await Script(notice,"getComputedStyle(document.querySelector('.dialog-page')).backgroundColor==='rgb(66, 40, 75)'"),"app notices follow the chosen custom theme");
            Check(await Script(notice,"document.body.innerText.includes('The operation could not be completed') && [...document.querySelectorAll('button')].some(b=>b.textContent==='OK')"),"app notices render English text and buttons in CSS");
            await Capture(notice,"css-notice-en.png");
            await notice.Browser.CoreWebView2.ExecuteScriptAsync("document.querySelector('button').click();"); await Task.Delay(200);
            Preferences.Theme="dark"; ApplyPreferences(); Refresh();
            double smallWidth=Width, smallHeight=Height;
            Preferences.BackgroundOpacity=.61; Persist(); Refresh();
            await Run("const mode=document.querySelector('.window-mode');mode.value=0;mode.dispatchEvent(new Event('change',{bubbles:true}));");
            var area=System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle).WorkingArea;
            var dpi=System.Windows.Media.VisualTreeHelper.GetDpi(this);
            Check(IsFullWindow && Math.Abs(Width-area.Width/dpi.DpiScaleX)<2 && Math.Abs(Height-area.Height/dpi.DpiScaleY)<2 && await Script(web,"window.checkpointState.opacity===1 && getComputedStyle(document.querySelector('.window')).borderRadius==='0px'"),"full window fills monitor work area and renders at maximum opacity");
            Check(Store.LoadSettings().FullWindow && Math.Abs(Store.LoadSettings().Width-smallWidth)<2 && Math.Abs(Store.LoadSettings().BackgroundOpacity-.61)<.001,"full window saves its mode without overwriting small dimensions or translucency");
            await Capture(web,"css-full-window-en.png");
            await Run("const mode=document.querySelector('.window-mode');mode.value=1;mode.dispatchEvent(new Event('change',{bubbles:true}));");
            Check(!IsFullWindow && Math.Abs(Width-smallWidth)<2 && Math.Abs(Height-smallHeight)<2 && await Script(web,"window.checkpointState.opacity===0.61 && document.querySelector('.window-mode').value==='1'"),"small window restores saved dimensions and translucency");
            await Run("const mode=document.querySelector('.window-mode');mode.value=2;mode.dispatchEvent(new Event('change',{bubbles:true}));");
            await Run("document.dispatchEvent(new KeyboardEvent('keydown',{key:'Home',bubbles:true}));document.dispatchEvent(new KeyboardEvent('keydown',{key:'Enter',bubbles:true}));");
            await Run("[...document.querySelectorAll('.menu button')].find(button=>button.getAttribute('aria-keyshortcuts')==='F1').click();");
            Check(await Script(web,"!!document.querySelector('.shortcut-help:modal') && !document.querySelector('.menu') && getComputedStyle(document.querySelector('.shortcutbar')).display==='none'"),"Miniature context menu opens the guide without adding controls to its list");
            await Run("document.dispatchEvent(new KeyboardEvent('keydown',{key:'Escape',bubbles:true}));");
            Check(Preferences.MiniatureView && await Script(web,"!document.querySelector('.shortcut-help') && document.activeElement.matches('.minirow')"),"closing Miniature help restores the selected row focus");
            await Run("document.dispatchEvent(new KeyboardEvent('keydown',{key:'Enter',bubbles:true}));");
            Check(Preferences.MiniatureView && await Script(web,"[...document.querySelectorAll('.menu button')].some(button=>button.textContent.includes('Full window')) && window.checkpointState.opacity===0.61"),"Miniature keeps translucency and offers all three window modes");
            await Run("[...document.querySelectorAll('.menu button')].find(button=>button.textContent.includes('Full window')).click();");
            Check(IsFullWindow && !Preferences.MiniatureView && Store.LoadSettings().FullWindow,"Miniature context menu restores the opaque full window");
            await Run("document.querySelector('[data-label=settings]').click();");
            var modesSettings=await Dialog();
            Check(await Script(modesSettings,"document.querySelector('select[aria-label=\"Window mode\"]').value==='0' && document.body.innerText.includes('100% opacity')"),"settings expose localized window modes and the full-opacity explanation");
            await modesSettings.Browser.CoreWebView2.ExecuteScriptAsync("const mode=document.querySelector('select[aria-label=\"Window mode\"]');mode.value=1;mode.dispatchEvent(new Event('change',{bubbles:true}));"); await Task.Delay(200);
            await modesSettings.Browser.CoreWebView2.ExecuteScriptAsync("[...document.querySelectorAll('button')].find(button=>button.textContent==='Save').click();"); await Task.Delay(350);
            Check(!IsFullWindow && !Preferences.MiniatureView && !Store.LoadSettings().FullWindow,"CSS settings save the small window mode");
            _ = Dispatcher.BeginInvoke(new Action(()=>Dialogs.ShortcutSettings(this)));
            var shortcutsDialog=await Dialog();
            Check(await Script(shortcutsDialog,"document.querySelectorAll('input').length===17"),"shortcut configuration exposes local, Miniature, reorder and global actions");
            await shortcutsDialog.Browser.CoreWebView2.ExecuteScriptAsync("document.querySelector('input[aria-label=\"Add game\"]').dispatchEvent(new KeyboardEvent('keydown',{key:'N',ctrlKey:true,shiftKey:true,bubbles:true}));"); await Task.Delay(300);
            await Capture(shortcutsDialog,"css-configure-shortcuts-en.png");
            await shortcutsDialog.Browser.CoreWebView2.ExecuteScriptAsync("[...document.querySelectorAll('button')].find(b=>b.textContent==='Save').click();"); await Task.Delay(400);
            Check(Store.LoadSettings().Shortcuts.GetValueOrDefault("add")=="Ctrl+Shift+N","CSS shortcut editor saves the captured combination to SQLite");
            Check(await Script(web,"document.querySelector('.shortcutbar').textContent.includes('Ctrl+Shift+N')"),"shortcut hints update to the configured combination");
            await Run("document.dispatchEvent(new KeyboardEvent('keydown',{key:'N',ctrlKey:true,shiftKey:true,bubbles:true}));");
            var shortcutEditor=await Dialog();
            Check(await Script(shortcutEditor,"!!document.querySelector('input[aria-label=\"Game title\"]')"),"custom desktop shortcut opens the game editor");
            await shortcutEditor.Browser.CoreWebView2.ExecuteScriptAsync("[...document.querySelectorAll('button')].find(b=>b.textContent==='Cancel').click();");await Task.Delay(200);
            await Run("document.querySelector('.manage-lists').click();");
            var listsDialog=await Dialog();
            Check(await Script(listsDialog,"!!document.querySelector('input[aria-label=\"List name\"]') && document.body.innerText.includes('without duplication')"),"CSS list manager explains membership and renders localized controls");
            await listsDialog.Browser.CoreWebView2.ExecuteScriptAsync("const n=document.querySelector('input[aria-label=\"List name\"]');n.value='Weekend';n.dispatchEvent(new Event('input',{bubbles:true}));[...document.querySelectorAll('button')].find(b=>b.textContent==='Create list').click();");await Task.Delay(350);
            Check(Store.LoadSettings().GameLists.Contains("Weekend") && Preferences.ActiveList=="custom:Weekend","CSS list creation persists the catalog and active selection");
            await Capture(listsDialog,"css-lists-en.png");
            await listsDialog.Browser.CoreWebView2.ExecuteScriptAsync("[...document.querySelectorAll('button')].find(b=>b.textContent==='Save').click();");await Task.Delay(250);
            Check(await Script(web,"document.querySelector('.collection-selector').value==='custom:Weekend' && !window.checkpointState.games.length"),"an empty custom list is independently selectable");
            await Run("document.querySelector('[data-label=add]').click();");
            var privateEditor=await Dialog();
            await privateEditor.Browser.CoreWebView2.ExecuteScriptAsync("const n=document.querySelector('input[aria-label=\"Game title\"]');n.value='Private list fixture';n.dispatchEvent(new Event('input',{bubbles:true}));document.querySelector('input[aria-label=\"Private to my friends\"]').click();[...document.querySelectorAll('button')].find(b=>b.textContent==='Save').click();");await Task.Delay(400);
            var privateFixture=Games.Single(g=>g.Title=="Private list fixture");
            Check(privateFixture.FriendsPrivate==true && privateFixture.Lists.Contains("Weekend") && Store.LoadGames().Single(g=>g.Id==privateFixture.Id).FriendsPrivate==true,"CSS initial save persists privacy and the active custom membership");
            Check(await Script(web,"!window.checkpointState.games.some(g=>g.title==='Private list fixture')"),"private games are excluded from public custom lists");
            await Run("const c=document.querySelector('.collection-selector');c.value='private';c.dispatchEvent(new Event('change',{bubbles:true}));");
            Check(await Script(web,"window.checkpointState.games.some(g=>g.title==='Private list fixture') && document.querySelector('.collection-selector').value==='private'"),"the separate private view shows games hidden from friends");
            await Capture(web,"css-private-games-en.png");
            Igdb?.Dispose();Igdb=CoverFixtureClient();
            await Run("document.querySelector('[data-game=\""+privateFixture.Id+"\"] .game-tools button:nth-child(2)').click();");
            var coverEditor=await Dialog();
            async Task<WebSurface> CoverPreview()
            {
                WebSurface? found=null;
                await Wait(async()=>{found=Application.Current.Windows.Cast<Window>().Where(w=>w.Title==I18n.T("Carátula de IGDB")).Select(w=>w.Tag).OfType<WebSurface>().FirstOrDefault();return found?.Browser.CoreWebView2 is not null&&await Script(found,"window.checkpointState?.kind==='dialog'");});return found!;
            }
            await coverEditor.Browser.CoreWebView2.ExecuteScriptAsync("[...document.querySelectorAll('button')].find(b=>b.textContent==='Search IGDB for another cover').click();");
            var coverPreview=await CoverPreview();
            Check(await Script(coverPreview,"!!document.querySelector('img.cover-preview') && document.body.innerText.includes('Closest cover fixture')"),"CSS game editor opens an IGDB candidate preview with its matched name");
            await Capture(coverPreview,"css-igdb-cover-en.png");
            await coverPreview.Browser.CoreWebView2.ExecuteScriptAsync("[...document.querySelectorAll('button')].find(b=>b.textContent==='Do not use this cover').click();");await Task.Delay(300);
            await coverEditor.Browser.CoreWebView2.ExecuteScriptAsync("[...document.querySelectorAll('button')].find(b=>b.textContent==='Cancel').click();");await Task.Delay(300);
            Check(Store.LoadGames().Single(g=>g.Id==privateFixture.Id).RejectedIgdbCovers.Contains("fixture_first")&&!CoverSuggestions.ShouldSuggest(privateFixture)&&privateFixture.CustomCover is null,"explicit IGDB rejection persists even after cancelling the game editor and suppresses automatic repetition");
            await Run("document.querySelector('[data-game=\""+privateFixture.Id+"\"] .game-tools button:nth-child(2)').click();");coverEditor=await Dialog();
            await coverEditor.Browser.CoreWebView2.ExecuteScriptAsync("[...document.querySelectorAll('button')].find(b=>b.textContent==='Search IGDB for another cover').click();");coverPreview=await CoverPreview();
            Check(await Script(coverPreview,"document.body.innerText.includes('Second cover fixture')&&!document.body.innerText.includes('Closest cover fixture')"),"manual IGDB retry proposes a different image and excludes the declined one");
            await coverPreview.Browser.CoreWebView2.ExecuteScriptAsync("[...document.querySelectorAll('button')].find(b=>b.textContent==='Use this cover').click();");await Task.Delay(300);
            await coverEditor.Browser.CoreWebView2.ExecuteScriptAsync("[...document.querySelectorAll('button')].find(b=>b.textContent==='Save').click();");await Task.Delay(350);
            var acceptedCover=Store.LoadGames().Single(g=>g.Id==privateFixture.Id);
            Check(acceptedCover.IgdbCoverImageId=="fixture_second"&&BackupFiles.IsCustomCoverName(acceptedCover.CustomCover)&&File.Exists(Path.Combine(Covers.DirectoryPath,acceptedCover.CustomCover!)),"accepted IGDB cover is saved locally and survives SQLite reload");
            File.WriteAllText(Path.Combine(output,"web-smoke.json"),JsonSerializer.Serialize(new { ok=true, checks=checks.Count, names=checks },DataJson.Options));
            Console.WriteLine("CSS smoke test passed: "+checks.Count+" checks, "+output);
        }
        catch (Exception error)
        { Console.Error.WriteLine(error); Environment.ExitCode=1; File.WriteAllText(Path.Combine(output,"web-smoke.json"),JsonSerializer.Serialize(new {ok=false,checks=checks.Count,error=error.ToString()},DataJson.Options)); }
        finally { Exit(); }
    }
}
