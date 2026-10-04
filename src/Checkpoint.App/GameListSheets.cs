using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Checkpoint.Core;
namespace Checkpoint.App;
internal static partial class Dialogs
{
    internal static void ChooseList(MainWindow owner,Guid[] ids,string source,string mode="move",Window? parent=null)
    {
        if(ids.Length is <1 or >500||ids.Any(id=>!owner.Games.Any(g=>g.Id==id)))throw new ArgumentException(I18n.T("Selecciona entre 1 y 500 juegos existentes."));
        var window=Modal(owner,I18n.T(mode=="add"?"Añadir a otra lista":"Cambiar de lista"),580,540);if(parent is not null)Parent(window,parent);
        var body=Panel();Layout(window,body,out var footer);
        Heading(body,I18n.T(mode=="add"?"Añadir a otra lista":"Cambiar de lista"),I18n.T("Mover desde una lista quita solo esa pertenencia. Desde Biblioteca o Mi lista reemplaza las listas actuales. Añadir conserva las demás. La privacidad no cambia."));
        body.Children.Add(new TextBlock{Text=I18n.T("Juegos seleccionados:")+" "+ids.Length});
        Label(body,I18n.T("Lista de destino"));var target=new ComboBox();System.Windows.Automation.AutomationProperties.SetName(target,I18n.T("Lista de destino"));body.Children.Add(target);
        var notice=new TextBlock{TextWrapping=TextWrapping.Wrap};body.Children.Add(notice);
        var apply=Button(I18n.T(mode=="add"?"Añadir a otra lista":"Mover juegos"),(_,_)=>{try{owner.ApplyListAction(ids,mode,source,target.SelectedItem as string);window.Close();}catch(Exception error){notice.Text=I18n.Error(error);}},true);
        void Reload(){target.ItemsSource=owner.Preferences.GameLists.ToArray();target.SelectedIndex=owner.Preferences.GameLists.Count>0?0:-1;apply.IsEnabled=target.SelectedIndex>=0;notice.Text=apply.IsEnabled?"":I18n.T("Crea una lista antes de mover juegos.");}
        body.Children.Add(Button(I18n.T("Crear lista"),(_,_)=>{ManageLists(owner,window);Reload();}));
        footer.Children.Add(Button(I18n.T("Cancelar"),(_,_)=>window.Close()));footer.Children.Add(apply);Reload();ShowPage(window);
    }
    internal static void ListDetails(MainWindow owner,string source)
    {
        if(source is not ("all" or "private")&&!owner.Preferences.GameLists.Contains(source.StartsWith("custom:")?source[7..]:"",StringComparer.OrdinalIgnoreCase))return;
        string title=source=="all"?I18n.T("Mi lista"):source=="private"?I18n.T("Privados"):source[7..];
        var window=Modal(owner,I18n.T("Ficha de la lista")+" · "+title,720,790);
        var body=Panel();Layout(window,body,out var footer);Heading(body,title,I18n.T("Ficha de la lista"));
        var summary=new TextBlock{TextWrapping=TextWrapping.Wrap};body.Children.Add(summary);
        body.Children.Add(new TextBlock{Text=I18n.T("La ficha incluye los juegos privados de esta lista. Quitar no elimina el juego de Biblioteca."),TextWrapping=TextWrapping.Wrap});
        var search=Input(body,I18n.T("Buscar juego"),"");var count=new TextBlock();body.Children.Add(count);
        Label(body,I18n.T("Filtrar por estado"));var status=new ComboBox{ItemsSource=new[]{I18n.T("Todos")}.Concat(Enum.GetValues<GameStatus>().Select(Labels.Status)).ToArray(),SelectedIndex=0};System.Windows.Automation.AutomationProperties.SetName(status,I18n.T("Filtrar por estado"));body.Children.Add(status);var results=new TextBlock();body.Children.Add(results);
        var selected=new HashSet<Guid>();int page=0;var actions=new WrapPanel();body.Children.Add(actions);
        var rows=new StackPanel();body.Children.Add(rows);var paging=new WrapPanel();body.Children.Add(paging);var pagination=new TextBlock();body.Children.Add(pagination);
        var notice=new TextBlock{TextWrapping=TextWrapping.Wrap};body.Children.Add(notice);
        Button Action(string text,System.Action run){var button=Button(I18n.T(text),(_,_)=>{try{run();}catch(Exception error){notice.Text=I18n.Error(error);}});actions.Children.Add(button);return button;}
        void Selection(){count.Text=I18n.T("Juegos seleccionados:")+" "+selected.Count+" · "+I18n.T("Hasta 500 juegos a la vez");foreach(Button action in actions.Children)action.IsEnabled=selected.Count>0;}
        Game[] Matches()=>GameLists.Members(owner.Games,source).Where(g=>(status.SelectedIndex==0||g.Status==(GameStatus)(status.SelectedIndex-1))&&g.Title.Contains(search.Text,StringComparison.OrdinalIgnoreCase)).OrderBy(g=>g.Title,StringComparer.OrdinalIgnoreCase).ToArray();
        void Reload()
        {
            var members=GameLists.Members(owner.Games,source).ToArray();selected.RemoveWhere(id=>!members.Any(g=>g.Id==id));var matches=Matches();page=Math.Clamp(page,0,Math.Max(0,(matches.Length+49)/50-1));rows.Children.Clear();results.Text=I18n.T("Resultados")+": "+matches.Length+" / "+members.Length;
            summary.Text=I18n.T("Juegos")+": "+members.Length+" · "+I18n.T("Historia terminada")+": "+members.Count(g=>g.Status==GameStatus.Finished)+" · "+I18n.T("Jugando")+": "+members.Count(g=>g.Status==GameStatus.Playing)+" · "+I18n.T("Privados")+": "+members.Count(g=>g.FriendsPrivate==true);
            foreach(var game in matches.Skip(page*50).Take(50))
            {
                var card=new StackPanel();var check=Check(card,I18n.T("Seleccionar juego")+": "+game.Title,selected.Contains(game.Id));
                check.Click+=(_,_)=>{if(check.IsChecked==true&&selected.Count<500)selected.Add(game.Id);else{selected.Remove(game.Id);check.IsChecked=false;}Selection();};
                card.Children.Add(new TextBlock{Text=game.Platform+" · "+game.StatusText+" · "+I18n.T(game.FriendsPrivate==true?"Privado para mis amigos":"Visible para mis amigos"),TextWrapping=TextWrapping.Wrap});
                var achievements=AchievementTracking.Items(game).ToArray();card.Children.Add(new TextBlock{Text=I18n.T("Logros")+": "+achievements.Count(a=>a.Completed)+" / "+achievements.Length+(game.StoryPercent is {} percent?" · "+I18n.T("Historia")+": "+percent+"%":""),TextWrapping=TextWrapping.Wrap});
                card.Children.Add(new TextBlock{Text=I18n.T("Listas")+": "+(game.Lists.Count>0?string.Join(", ",game.Lists):I18n.T("Sin listas adicionales")),TextWrapping=TextWrapping.Wrap});
                card.Children.Add(Button(I18n.T("Ver ficha completa"),(_,_)=>{GameDetails(owner,game,window);Reload();}));rows.Children.Add(new Border{Child=card});
            }
            if(matches.Length==0)rows.Children.Add(new TextBlock{Text=I18n.T(members.Length>0?"Ningún juego coincide con los filtros.":"Sin juegos en esta lista")});
            pagination.Text=I18n.T("Página")+" "+(page+1)+" / "+Math.Max(1,(matches.Length+49)/50);Selection();
        }
        void Batch(string operation){owner.ApplyListAction(selected.ToArray(),operation,source);selected.Clear();Reload();}
        Action("Cambiar de lista",()=>{ChooseList(owner,selected.ToArray(),source,"move",window);Reload();});Action("Añadir a otra lista",()=>{ChooseList(owner,selected.ToArray(),source,"add",window);Reload();});
        if(source!="private")Action("Quitar de esta lista",()=>Batch("remove"));Action("Mover a Privados",()=>Batch("private"));Action("Hacer visible para amigos",()=>Batch("public"));
        paging.Children.Add(Button(I18n.T("Anterior"),(_,_)=>{page--;Reload();}));paging.Children.Add(Button(I18n.T("Siguiente"),(_,_)=>{page++;Reload();}));
        paging.Children.Add(Button(I18n.T("Seleccionar esta página"),(_,_)=>{foreach(var game in Matches().Skip(page*50).Take(50)){if(selected.Count<500)selected.Add(game.Id);}Reload();}));
        paging.Children.Add(Button(I18n.T("Limpiar selección"),(_,_)=>{selected.Clear();Reload();}));
        search.TextChanged+=(_,_)=>{page=0;selected.Clear();Reload();};status.SelectionChanged+=(_,_)=>{page=0;selected.Clear();Reload();};footer.Children.Add(Button(I18n.T("Cerrar"),(_,_)=>window.Close()));Reload();ShowPage(window);
    }
}
