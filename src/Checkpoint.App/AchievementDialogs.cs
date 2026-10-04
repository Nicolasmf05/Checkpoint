using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Checkpoint.Core;
namespace Checkpoint.App;
internal static partial class Dialogs
{
    internal static Window Achievements(MainWindow owner, Game initial, bool automatic = false, Window? parent = null)
    {
        var window = Modal(owner, I18n.T("Logros · ") + initial.Title, 620, 720);
        if(parent is not null)Parent(window,parent);
        if(automatic&&!App.UseCss) { window.Owner=null; window.ShowInTaskbar=true; window.ShowActivated=false; window.WindowStartupLocation=WindowStartupLocation.CenterScreen; }
        var body=Panel(); Layout(window,body,out var footer);
        Heading(body,I18n.T("Logros de ")+initial.Title,I18n.T("Los cambios manuales solo afectan a Checkpoint. No desbloquean logros en Steam ni RetroAchievements."));
        var summary=new TextBlock { FontSize=24, FontWeight=FontWeights.Bold };
        var bar=new ProgressBar { Minimum=0, Maximum=100, Height=10 };
        var remaining=new TextBlock();
        var overview=new StackPanel();overview.Children.Add(summary);overview.Children.Add(bar);overview.Children.Add(remaining);
        body.Children.Add(new Border { Child=overview, Tag="achievement-summary" });
        var expanded=new System.Collections.Generic.HashSet<string>();
        var spoilers=Check(body,I18n.T("Mostrar nombres y descripciones de logros secretos"),false);
        var locked=Check(body,I18n.T("Mostrar solo los pendientes"),true);
        var list=new StackPanel();body.Children.Add(list);
        Game Current()=>owner.Games.FirstOrDefault(g=>g.Id==initial.Id)??initial;
        void Save() { owner.Persist();owner.Refresh();Render(); }
        void Change(Action action) { try { action();Save(); } catch(Exception ex) { owner.Notice(I18n.Error(ex));Render(); } }
        void Render()
        {
            var game=Current();var items=AchievementTracking.Items(game).ToArray();list.Children.Clear();
            int done=items.Count(a=>a.Completed);
            summary.Text=done+" / "+items.Length+I18n.T(" completados en Checkpoint");
            bar.Value=items.Length==0?0:done*100.0/items.Length;
            System.Windows.Automation.AutomationProperties.SetName(bar,summary.Text);
            remaining.Text=items.Length==0?I18n.T("Todavía no hay logros. Sincroniza o añade un objetivo manual."):(items.Length-done)+I18n.T(" logros pendientes");
            foreach(var item in items.Where(a=>locked.IsChecked!=true||!a.Completed).OrderBy(a=>a.Completed))
            {
                bool secret=item.Data.Hidden&&!item.Data.Unlocked&&spoilers.IsChecked!=true;
                string name=secret?I18n.T("Logro secreto"):item.Data.Name;
                var panel=new StackPanel{Margin=new Thickness(0,12,0,8)};
                panel.Children.Add(new TextBlock{Text=(item.Completed?"✓  ":"○  ")+name,FontSize=18,FontWeight=FontWeights.SemiBold,TextWrapping=TextWrapping.Wrap});
                if(secret)panel.Children.Add(new TextBlock{Text=I18n.T("Activa la opción superior para revelar este logro."),TextWrapping=TextWrapping.Wrap});
                else
                {
                    var details=Button(expanded.Contains(item.Key)?I18n.T("Ocultar descripción"):I18n.T("Ver descripción"),(_,_)=>{if(!expanded.Add(item.Key))expanded.Remove(item.Key);Render();});
                    System.Windows.Automation.AutomationProperties.SetName(details,(expanded.Contains(item.Key)?I18n.T("Ocultar descripción"):I18n.T("Ver descripción"))+" · "+name);
                    panel.Children.Add(details);
                    if(expanded.Contains(item.Key))panel.Children.Add(new TextBlock { Text=string.IsNullOrWhiteSpace(item.Data.Description)?I18n.T(item.Provider=="steam"&&item.Data.Hidden?"Steam no ha enviado la descripción de este logro oculto. Prueba a actualizar los logros; si sigue vacía, no está disponible en la API de Steam.":"Este logro no tiene descripción."):item.Data.Description,TextWrapping=TextWrapping.Wrap,Tag="achievement-description" });
                }
                panel.Children.Add(new TextBlock{Text=item.Provider=="steam"?"Steam":item.Provider=="retro"?"RetroAchievements":I18n.T("Manual"),FontSize=11});
                var completed=Check(panel,I18n.T("Completado en Checkpoint"),item.Completed);
                completed.Checked+=(_,_)=>Change(()=>game.AchievementOverrides[item.Key]=true);
                completed.Unchecked+=(_,_)=>Change(()=>game.AchievementOverrides[item.Key]=false);
                panel.Children.Add(Button(I18n.T("Quitar de mi lista"),(_,_)=>Change(()=>AchievementTracking.Remove(game,item))));
                if(item.ManualOverride&&item.Provider!="manual")
                {
                    panel.Children.Add(new TextBlock{Text=I18n.T("Cambio manual en Checkpoint"),FontSize=11});
                    panel.Children.Add(Button(I18n.T("Usar estado de la API"),(_,_)=>Change(()=>game.AchievementOverrides.Remove(item.Key))));
                }
                list.Children.Add(new Border { Child=panel, Tag="achievement-card" });
            }
        }
        spoilers.Checked+=(_,_)=>Render();spoilers.Unchecked+=(_,_)=>Render();locked.Checked+=(_,_)=>Render();locked.Unchecked+=(_,_)=>Render();
        body.Children.Add(Button(I18n.T("Restaurar logros quitados"),(_,_)=>Change(()=>Current().RemovedAchievements.Clear())));
        var nameBox=Input(body,I18n.T("Nombre del logro manual"),"");nameBox.MaxLength=250;
        var description=Input(body,I18n.T("Descripción del logro manual"),"",true);description.MaxLength=2000;
        body.Children.Add(Button(I18n.T("Añadir logro manual"),(_,_)=>Change(()=>{AchievementTracking.Add(Current(),nameBox.Text,description.Text);nameBox.Clear();description.Clear();})));
        var sync=Button(I18n.T("Actualizar"),async(_,_)=>{await owner.RefreshGameAchievements(Current());Render();});
        sync.IsEnabled=(owner.Steam.Session is not null&&initial.SteamAppId.HasValue)||(owner.Retro?.Session is not null&&initial.RetroGameId.HasValue);footer.Children.Add(sync);
        footer.Children.Add(Button(I18n.T("Cerrar"),(_,_)=>window.Close(),true));Render();
        if(automatic)
        {
            bool closed=false;var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(60)};
            async System.Threading.Tasks.Task Update() { if(closed)return;await owner.RefreshGameAchievements(Current());if(!closed)Render(); }
            timer.Tick+=async(_,_)=>await Update();window.Loaded+=async(_,_)=>{timer.Start();await Update();};window.Closed+=(_,_)=>{closed=true;timer.Stop();};ShowPage(window,true);
        }
        else ShowPage(window);
        return window;
    }
    internal static void RetroSettings(MainWindow owner,Window parent)
    {
        var window=Modal(owner,I18n.T("Configurar RetroAchievements"),540,560);Parent(window,parent);
        var body=Panel();Layout(window,body,out var footer);
        Heading(body,"RetroAchievements",I18n.T("Introduce tu usuario y tu clave web personal. La clave se cifra en Windows y no se incluye en las copias de seguridad."));
        var username=Input(body,I18n.T("Usuario de RetroAchievements"),owner.Retro.Session?.Username??"");
        Label(body,I18n.T("Clave web de RetroAchievements"));var key=new PasswordBox{MaxLength=256};System.Windows.Automation.AutomationProperties.SetName(key,I18n.T("Clave web de RetroAchievements"));body.Children.Add(key);
        var hardcore=Check(body,I18n.T("Contar solo logros en modo Hardcore"),owner.Retro.Session?.Hardcore??false);
        body.Children.Add(new TextBlock{Text=I18n.T("En cada juego, indica su ID de RetroAchievements y el ejecutable del emulador. El ID de Steam es diferente."),TextWrapping=TextWrapping.Wrap});
        body.Children.Add(Button(I18n.T("Desvincular"),(_,_)=>{try{owner.Retro.Disconnect();window.Close();}catch(Exception ex){owner.Notice(I18n.Error(ex));}}));
        footer.Children.Add(Button(I18n.T("Cancelar"),(_,_)=>window.Close()));
        footer.Children.Add(Button(I18n.T("Guardar"),(_,_)=>
        {
            try { string value=key.Password;if(value.Length==0&&owner.Retro.Session is {} previous&&previous.Username==username.Text.Trim())value=previous.ApiKey;
                owner.Retro.Save(username.Text,value,hardcore.IsChecked==true);key.Clear();owner.ResetAchievementRefresh();window.Close(); }
            catch(Exception ex){owner.Notice(I18n.Error(ex));}
        },true));ShowPage(window);
    }
}
