using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Checkpoint.Core;

namespace Checkpoint.App;

internal static partial class Dialogs
{
    internal static void ManageLists(MainWindow owner,Window? parent=null)
    {
        var window=Modal(owner,I18n.T("Gestionar listas"),540,600);if(parent is not null)Parent(window,parent);
        var body=Panel();Layout(window,body,out var footer);
        Heading(body,I18n.T("Gestionar listas"),I18n.T("Un juego puede estar en varias listas sin duplicarse. Los privados se consultan en Privados."));
        var selection=new ComboBox();Label(body,I18n.T("Lista de juegos"));body.Children.Add(selection);
        System.Windows.Automation.AutomationProperties.SetName(selection,I18n.T("Lista de juegos"));
        var name=Input(body,I18n.T("Nombre de la lista"),"");name.MaxLength=40;
        var defaultsPrivate=Check(body,I18n.T("Crear juegos nuevos como privados"),owner.Preferences.NewGamesPrivate);
        body.Children.Add(new TextBlock{Text=I18n.T("Quitar una lista conserva sus juegos, su privacidad y el resto de listas."),TextWrapping=TextWrapping.Wrap});
        var notice=new TextBlock{TextWrapping=TextWrapping.Wrap};body.Children.Add(notice);
        void Reload(string? selected=null){selection.ItemsSource=new[]{I18n.T("Nueva lista")}.Concat(owner.Preferences.GameLists).ToList();selection.SelectedIndex=selected is null?0:owner.Preferences.GameLists.IndexOf(selected)+1;}
        selection.SelectionChanged+=(_,_)=>name.Text=selection.SelectedIndex>0?(string)selection.SelectedItem:"";
        bool Change(bool rename)
        {
            try
            {
                string? previous=rename&&selection.SelectedIndex>0?(string)selection.SelectedItem:null;
                if(rename&&previous is null)return false;
                string next=GameLists.ValidateName(name.Text,owner.Preferences.GameLists,previous);
                if(previous is null)owner.Preferences.GameLists.Add(next);
                else{owner.Preferences.GameLists[owner.Preferences.GameLists.IndexOf(previous)]=next;GameLists.Rename(owner.Games,previous,next);}
                owner.Preferences.ActiveList="custom:"+next;if(previous is not null)owner.PersistListChange(previous,next);owner.SelectGameList("custom:"+next);Reload(next);notice.Text=I18n.T("Lista guardada.");return true;
            }catch(Exception ex){notice.Text=I18n.Error(ex);return false;}
        }
        var actions=new WrapPanel();body.Children.Add(actions);
        actions.Children.Add(Button(I18n.T("Crear lista"),(_,_)=>Change(false)));
        actions.Children.Add(Button(I18n.T("Renombrar lista"),(_,_)=>Change(true)));
        actions.Children.Add(Button(I18n.T("Ver ficha de la lista"),(_,_)=>{if(selection.SelectedIndex>0)ListDetails(owner,"custom:"+(string)selection.SelectedItem);}));
        actions.Children.Add(Button(I18n.T("Quitar lista"),(_,_)=>
        {
            try{if(selection.SelectedIndex<=0)return;string selected=(string)selection.SelectedItem;
            owner.Preferences.GameLists.Remove(selected);foreach(var game in owner.Games)game.Lists.RemoveAll(n=>n.Equals(selected,StringComparison.OrdinalIgnoreCase));
            owner.Preferences.ActiveList="all";owner.PersistListChange(selected,null);owner.Refresh();Reload();notice.Text=I18n.T("Lista quitada. Tus juegos se conservan.");}catch(Exception ex){notice.Text=I18n.Error(ex);}
        }));
        footer.Children.Add(Button(I18n.T("Cancelar"),(_,_)=>window.Close()));
        footer.Children.Add(Button(I18n.T("Guardar"),(_,_)=>{try{string? previous=selection.SelectedIndex>0?selection.SelectedItem as string:null;if(!string.IsNullOrWhiteSpace(name.Text)&&name.Text.Trim()!=previous&&!Change(previous is not null))return;owner.Preferences.NewGamesPrivate=defaultsPrivate.IsChecked==true;owner.Persist();owner.Refresh();window.Close();}catch(Exception ex){notice.Text=I18n.Error(ex);}},true));
        Reload();ShowPage(window);
    }
}
