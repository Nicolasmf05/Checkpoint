using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Checkpoint.Core;
using Microsoft.Web.WebView2.Core;

namespace Checkpoint.App;

public partial class MainWindow
{
    private const string CoverOrigin = "https://checkpoint-images.invalid/";
    private WebSurface? web;
    private readonly WebControls friendControls = new();
    private void StartWebInterface()
    {
        InitializeCoverSuggestions();
        web = new WebSurface(this,Store.DirectoryPath,WebSnapshot,WebCommand,() => friendsVisible);
        _ = ConfigureWebResources();
    }
    private object WebSnapshot() => new
    {
        kind="main", collection=Preferences.ActiveList, collections=new[]{new {value="all",label=I18n.T("Mi lista")},new{value="private",label=I18n.T("Privados")}}.Concat(Preferences.GameLists.Select(n=>new{value="custom:"+n,label=n})).ToArray(), shortcuts=Shortcuts.Effective(Preferences.Shortcuts), language=I18n.Language, light=Themes.IsLight(Preferences), theme=Themes.Id(Preferences),
        opacity=EffectiveOpacity, full=IsFullWindow, mini=Preferences.MiniatureView, compact=Preferences.Compact,
        grid=Preferences.GridView, textSize=Preferences.MiniatureTextSize, locked=Preferences.PositionLocked,
        pinned=Preferences.AlwaysOnTop, globalHotkey=hotkeyRegistered, lightweight=Preferences.LightweightMode, busy=syncing,
        tab=friendsVisible ? "friends" : allLibrary ? "library" : "list", search=Search.Text, filter=StatusFilter.SelectedIndex,
        summary=Summary.Text, notice=NoticeText.Text, connection=ConnectionText.Text,
        emptyTitle=EmptyTitle.Text, emptyText=EmptyText.Text, examples=Games.Count == 0, undo=DeletedGames.Count > 0,
        labels=new { title=I18n.T("Mi lista"),
            friendsTitle=I18n.T("Amigos"), list=I18n.T("Mi lista"), library=I18n.T("Biblioteca"), friends=I18n.T("Amigos"),
            add=I18n.T("Añadir juego"), settings=I18n.T("Ajustes"), hide=I18n.T("Ocultar widget"), close=I18n.T("Cerrar"),
            pin=I18n.T("Mantener siempre visible"), search=I18n.T("Buscar juego"), sync=I18n.T("Actualizar"),
            details=I18n.T("Ver ficha completa"), achievements=I18n.T("Ver logros"), steam=I18n.T("Conectar Steam"), edit=I18n.T("Editar juego"), exitMini=I18n.T("Salir de miniatura"),
            locked=I18n.T("Bloquear posición y tamaño"), view=I18n.T("Cambiar vista"), undo=I18n.T("Recuperar último juego eliminado"),
            examples=I18n.T("Añadir ejemplos"), finish=I18n.T("Marcar o desmarcar historia terminada"),
            windowMode=I18n.T("Modo de ventana"), fullWindow=I18n.T("Ventana completa"), smallWindow=I18n.T("Ventana pequeña"), miniature=I18n.T("Miniatura"),
            collection=I18n.T("Lista de juegos"),manageLists=I18n.T("Gestionar listas"),makePrivate=I18n.T("Mover a Privados"),makeVisible=I18n.T("Hacer visible para amigos"),configureShortcuts=I18n.T("Configurar atajos"), shortcuts=I18n.T("Atajos de teclado"), generalKeys=I18n.T("En la ventana principal"), miniKeys=I18n.T("En miniatura"), orderKeys=I18n.T("Al enfocar el botón de reordenar"),
            helpHint=I18n.T("Pulsa F1 para ver todos los atajos."), closeHelp=I18n.T("Cerrar ayuda"),
            selectGame=I18n.T("Seleccionar juego"), firstLast=I18n.T("Primer o último juego"), pageGame=I18n.T("Avanzar o retroceder una página"),
            gameMenu=I18n.T("Abrir menú del juego"), reorder=I18n.T("Reordenar juego"),
            undoContext=I18n.T("Fuera de los campos de texto"), hideContext=I18n.T("Sin menús ni ayuda abiertos"),
            globalKeys=I18n.T("Mostrar / ocultar desde cualquier aplicación"), globalUnavailable=I18n.T("No disponible: otra aplicación puede estar usando este atajo."),
            homeEnd=I18n.T("Inicio / Fin"), pageKeys=I18n.T("RePág / AvPág"), enterSpace=I18n.T("Intro / Espacio"),
            all=I18n.T("Todos"), statuses=Enum.GetValues<GameStatus>().Select(Labels.Status).ToArray() },
        games=(friendsVisible ? Enumerable.Empty<CardView>() : visibleCards).Select(card => new { id=card.Model.Id, title=card.Model.Title, platform=card.Model.Platform,
            state=(int)card.Model.Status, status=card.Model.StatusText, next=card.Model.NextTask,
            achievementCaption=AchievementCaption(card.Model), progress=card.ProgressCaption, percent=card.ProgressVisibility == Visibility.Visible ? (int?)card.Percentage : null, favorite=card.Model.Favorite, friendsPrivate=card.Model.FriendsPrivate==true,
            cover=Preferences.LightweightMode || Preferences.MiniatureView ? null : CoverOrigin+"game-cover/"+card.Model.Id+"?v="+Uri.EscapeDataString(card.Model.CustomCover ?? "steam") }).ToArray(),
        friendsBusy=FriendsView.Busy, friends=friendsVisible ? friendControls.Capture(FriendsView) : null
    };
    private static string AchievementCaption(Game game)
    {
        var items=AchievementTracking.Items(game).ToArray();
        return items.Length==0?I18n.T("Ver logros"):I18n.T("Logros")+" · "+items.Count(a=>a.Completed)+" / "+items.Length;
    }
    private void WebCommand(JsonElement message)
    {
        if (message.TryGetProperty("control",out _)) { if (friendsVisible) friendControls.Dispatch(message); return; }
        string action=message.GetProperty("action").GetString() ?? "";
        Game? game=null;
        if (message.TryGetProperty("id",out var id) && Guid.TryParse(id.GetString(),out var gameId)) game=Games.FirstOrDefault(g => g.Id == gameId);
        void Click(Button button) { if (button.IsEnabled) button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); }
        switch (action)
        {
            case "ready": break;
            case "cover-missing" when game is not null: QueueCoverSuggestion(game);break;
            case "window-mode": if (message.GetProperty("value").TryGetInt32(out int mode)) SetWindowMode(mode); break;
            case "tab":
                switch (message.GetProperty("value").GetString()) { case "friends": Click(FriendsButton); break; case "library": Click(LibraryButton); break; case "list": Click(TrackedButton); break; }
                break;
            case "add": Dialogs.Edit(this,null); break;
            case "achievements" when game is not null: Dialogs.Achievements(this,game); break;
            case "details" when game is not null: Dialogs.GameDetails(this,game); web?.Event(new { kind="focus-game",id=game.Id }); break;
            case "edit" when game is not null: Dialogs.Edit(this,game); web?.Event(new { kind="focus-game",id=game.Id }); break;
            case "configure-shortcuts": Dialogs.ShortcutSettings(this); break;
            case "manage-lists": Dialogs.ManageLists(this);break;
            case "collection": var collection=message.GetProperty("value").GetString()??"all";if(collection=="all"||collection=="private"||Preferences.GameLists.Any(n=>"custom:"+n==collection)){Preferences.ActiveList=collection;allLibrary=false;friendsVisible=false;Search.Clear();StatusFilter.SelectedIndex=0;Persist();Refresh();}break;
            case "privacy" when game is not null: game.FriendsPrivate=message.GetProperty("value").GetBoolean();Persist();Refresh();break;
            case "settings": Dialogs.Settings(this); break;
            case "state" when game is not null:
                if (message.GetProperty("value").TryGetInt32(out int state) && state >= 0 && state <= 4) { GameRules.SetStatus(game,(GameStatus)state); Persist(); Refresh(); }
                break;
            case "finish" when game is not null: FinishClick(new Button { Tag=game },new RoutedEventArgs()); break;
            case "search-change": var query=message.GetProperty("value").GetString() ?? ""; Search.Text=query[..Math.Min(query.Length,140)]; break;
            case "filter": if (message.GetProperty("value").TryGetInt32(out int filter) && filter >= 0 && filter <= 5) StatusFilter.SelectedIndex=filter; break;
            case "search": FocusCollectionSearch(); web?.Event(new { kind="focus-search" }); break;
            case "cycle": CycleView(); break;
            case "undo": UndoLastDeletion(); break;
            case "sync": Click(SyncButton); break;
            case "steam": _=ConnectSteam(); break;
            case "examples": AddExamples(); Persist(); Refresh(); break;
            case "pin": Preferences.AlwaysOnTop=!Preferences.AlwaysOnTop; ApplyPreferences(); Persist(); break;
            case "lock": Preferences.PositionLocked=!Preferences.PositionLocked; ApplyPreferences(); Persist(); break;
            case "exit-mini": Preferences.MiniatureView=false; ApplyPreferences(); Persist(); Refresh(); break;
            case "hide": Hide(); break;
            case "close": Close(); break;
            case "drag": if (!IsFullWindow && !Preferences.PositionLocked && System.Windows.Input.Mouse.LeftButton == System.Windows.Input.MouseButtonState.Pressed) DragMove(); break;
            case "resize":
                if (!IsFullWindow && !Preferences.PositionLocked && message.GetProperty("x").TryGetDouble(out double x) && message.GetProperty("y").TryGetDouble(out double y) && double.IsFinite(x) && double.IsFinite(y))
                { Width=Math.Clamp(Width+Math.Clamp(x,-200,200),MinWidth,SystemParameters.VirtualScreenWidth); Height=Math.Clamp(Height+Math.Clamp(y,-200,200),MinHeight,SystemParameters.VirtualScreenHeight); }
                break;
            case "move" when game is not null:
                if (Guid.TryParse(message.GetProperty("target").GetString(),out var target) && GameRules.Move(Games,game.Id,target,message.GetProperty("after").GetBoolean())) { Persist(); Refresh(); }
                break;
        }
    }
    private async System.Threading.Tasks.Task ConfigureWebResources()
    {
        if (web is null) return;
        await web.Initialization;
        if (web.Browser.CoreWebView2 is not { } core) return;
        core.AddWebResourceRequestedFilter(CoverOrigin+"game-cover/*",CoreWebView2WebResourceContext.Image);
        core.WebResourceRequested += async (_,e) =>
        {
            using var pending=e.GetDeferral();
            try
            {
                e.Response=core.Environment.CreateWebResourceResponse(null,404,"Not Found","");
                var uri=new Uri(e.Request.Uri);
                if (uri.Host != "checkpoint-images.invalid" || !uri.AbsolutePath.StartsWith("/game-cover/",StringComparison.Ordinal)) return;
                if (!Guid.TryParse(uri.AbsolutePath["/game-cover/".Length..],out var id)) return;
                var game=Games.FirstOrDefault(g => g.Id == id);
                if (game is null || Preferences.LightweightMode || Preferences.MiniatureView) return;
                var image=await Covers.Get(game);
                if (image is BitmapSource bitmap) e.Response=core.Environment.CreateWebResourceResponse(new MemoryStream(WebSurface.ImageBytes(bitmap)),200,"OK","Content-Type: image/png\r\nCache-Control: no-store\r\n");
                else e.Response=core.Environment.CreateWebResourceResponse(null,404,"Not Found","");
            }
            catch (Exception error) when (error is IOException or OperationCanceledException or InvalidOperationException) { }
        };
    }
}
