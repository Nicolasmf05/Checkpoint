using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Checkpoint.Core;
namespace Checkpoint.App;
internal static partial class Dialogs
{
    private static void LaunchSteam(Window owner,int appId)
    {
        if(appId<=0)return;
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo($"steam://rungameid/{appId}"){UseShellExecute=true}); }
        catch(Exception error) when(error is System.ComponentModel.Win32Exception or InvalidOperationException)
        { LocalizedNotice.Show(owner,I18n.T("No se pudo abrir Steam. Comprueba que esté instalado en este equipo."),"Checkpoint"); }
    }
    internal static void GameDetails(MainWindow owner,Game game,Window? parent=null)
    {
        var window=Modal(owner,I18n.T("Ficha del juego")+" · "+game.Title,620,760);
        if(parent is not null)Parent(window,parent);
        var body=Panel();Layout(window,body,out var footer);
        void Render()
        {
            body.Children.Clear();window.Title=I18n.T("Ficha del juego")+" · "+game.Title;
        Heading(body,game.Title,I18n.T("Ficha del juego"));
        var actions=new WrapPanel { Tag="game-actions" };body.Children.Add(actions);
        if(game.SteamAppId is >0)actions.Children.Add(Button(I18n.T("Jugar"),(_,_)=>LaunchSteam(window,game.SteamAppId.Value),true));
        actions.Children.Add(Button(I18n.T("Ver logros"),(_,_)=>{if(!App.UseCss){window.Close();Achievements(owner,game);return;}Achievements(owner,game,false,window);Render();},game.SteamAppId is not >0));
        actions.Children.Add(Button(I18n.T("Editar juego"),(_,_)=>{if(!App.UseCss){window.Close();Edit(owner,game);return;}Edit(owner,game);if(owner.Games.FirstOrDefault(g=>g.Id==game.Id) is {} current){game=current;Render();}else window.Close();}));
        var image=new Image { Height=240,Tag="cover-preview",Visibility=Visibility.Collapsed };
        if(!owner.Preferences.LightweightMode)body.Children.Add(image);
        async System.Threading.Tasks.Task LoadCover(){if(owner.Preferences.LightweightMode)return;try{image.Source=await owner.Covers.Get(game);if(image.Source is not null)image.Visibility=Visibility.Visible;}catch(Exception error) when(error is not OutOfMemoryException){} }
        _=LoadCover();
        void Line(string text)=>body.Children.Add(new TextBlock { Text=text,TextWrapping=TextWrapping.Wrap });
        void Section(string title)=>body.Children.Add(new TextBlock { Text=I18n.T(title),FontSize=22,FontWeight=FontWeights.SemiBold,TextWrapping=TextWrapping.Wrap });
        Line(game.Platform+" · "+game.StatusText);
        if(game.GoalVisible)Line(I18n.T("Objetivo")+": "+game.GoalText);
        if(game.StoryPercent is {} percent){Line(I18n.T("Historia")+": "+percent+"%");body.Children.Add(new ProgressBar { Minimum=0,Maximum=100,Value=percent,Height=10 });}
        var achievements=AchievementTracking.Items(game).ToArray();
        Section("Logros");Line(achievements.Count(a=>a.Completed)+" / "+achievements.Length+I18n.T(" completados en Checkpoint"));
        if(achievements.Length>0)body.Children.Add(new ProgressBar { Minimum=0,Maximum=100,Value=achievements.Count(a=>a.Completed)*100d/achievements.Length,Height=10 });
        Line("Steam: "+(game.Achievements is null?I18n.T("Sin sincronizar"):game.UnlockedCount+" / "+game.Achievements.Count));
        Line("RetroAchievements: "+(game.RetroAchievements is null?I18n.T("Sin sincronizar"):game.RetroAchievements.Count(a=>a.Unlocked)+" / "+game.RetroAchievements.Count));
        Line(I18n.T("Objetivos manuales")+": "+game.ManualAchievements.Count);
        Line(I18n.T("Tiempo jugado")+": "+(game.PlaytimeMinutes/60d).ToString("0.#",I18n.IsEnglish?System.Globalization.CultureInfo.GetCultureInfo("en-US"):System.Globalization.CultureInfo.GetCultureInfo("es-ES"))+" h");
        Line(I18n.T("Visibilidad")+": "+I18n.T(game.FriendsPrivate==true?"Privado para mis amigos":"Visible para mis amigos"));
        Line(I18n.T("Listas")+": "+(game.Lists.Count>0?string.Join(", ",game.Lists):I18n.T("Sin listas adicionales")));
        Section("Tareas");if(game.Tasks.Count==0)Line(I18n.T("Sin tareas"));foreach(var task in game.Tasks)Line((task.Done?"✓ ":"○ ")+task.Title);
        Section("Notas");Line(string.IsNullOrWhiteSpace(game.Notes)?I18n.T("Sin notas"):game.Notes);
        string Date(DateTimeOffset value)=>value.ToLocalTime().ToString("g",System.Globalization.CultureInfo.GetCultureInfo(I18n.IsEnglish?"en-US":"es-ES"));
        Line(I18n.T("Añadido")+": "+Date(game.AddedAt));Line(I18n.T("Última sincronización")+": "+(game.SyncedAt is {} synced?Date(synced):I18n.T("Sin sincronizar")));
        if(game.FinishedAt is {} finished)Line(I18n.T("Historia terminada")+": "+Date(finished));
        if(game.SteamAppId is {} steam)Line("Steam ID: "+steam);if(game.RetroGameId is {} retro)Line("RetroAchievements ID: "+retro);
        }
        Render();
        footer.Children.Add(Button(I18n.T("Cerrar"),(_,_)=>window.Close()));
        ShowPage(window);
    }
}
