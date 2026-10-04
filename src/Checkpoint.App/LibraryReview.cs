using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Checkpoint.Core;
namespace Checkpoint.App;
internal static partial class Dialogs
{
    internal static void ReviewAchievements(MainWindow owner, Window parent)
    {
        var window=Modal(owner,I18n.T("Repasar todos los logros"),600,620);window.Owner=parent;
        var body=Panel();Layout(window,body,out var footer);
        Heading(body,I18n.T("Repasar todos los logros"),I18n.T("Incluye toda la Biblioteca, también los juegos privados y los que no están en Mi lista. Los cambios manuales se conservan."));
        var ids=owner.Games.OrderBy(g=>g.Title,StringComparer.OrdinalIgnoreCase).Select(g=>g.Id).ToArray();int index=0;
        var position=new TextBlock();var title=new TextBlock{FontSize=22,TextWrapping=TextWrapping.Wrap};var summary=new TextBlock{TextWrapping=TextWrapping.Wrap};var message=new TextBlock{TextWrapping=TextWrapping.Wrap};
        body.Children.Add(position);body.Children.Add(title);body.Children.Add(summary);body.Children.Add(message);
        using var cancellation=new CancellationTokenSource();bool busy=false;
        Game? Current()=>index<ids.Length?owner.Games.FirstOrDefault(g=>g.Id==ids[index]):null;
        Button view=null!,update=null!,previous=null!,next=null!;
        view=Button(I18n.T("Ver logros"),(_,_)=>{if(Current() is {} game)Achievements(owner,game,false,window);Reload();});body.Children.Add(view);
        update=Button(I18n.T("Actualizar todos los logros"),async(_,_)=>
        {
            if(busy||owner.AchievementSyncBusy)return;busy=true;Reload();
            try {var result=await owner.ReviewAchievementSync((done,total)=>{if(window.IsVisible)message.Text=I18n.T("Juegos revisados:")+" "+done+" / "+total;},cancellation.Token);
                if(window.IsVisible)message.Text=I18n.T("Juegos revisados:")+" "+result.Updated+" · "+I18n.T("No se pudieron actualizar:")+" "+result.Errors;}
            catch(OperationCanceledException){}
            finally{busy=false;if(window.IsVisible)Reload();}
        });body.Children.Add(update);
        previous=Button(I18n.T("Anterior"),(_,_)=>{index--;Reload();});next=Button(I18n.T("Siguiente juego"),(_,_)=>{index++;Reload();});
        footer.Children.Add(previous);footer.Children.Add(next);footer.Children.Add(Button(I18n.T("Cerrar"),(_,_)=>window.Close()));
        void Reload()
        {
            index=Math.Clamp(index,0,Math.Max(0,ids.Length-1));var game=Current();var items=game is null?[]:AchievementTracking.Items(game).ToArray();
            position.Text=I18n.T("Juego")+" "+(ids.Length==0?0:index+1)+" / "+ids.Length;title.Text=game?.Title??I18n.T("Sin juegos en Biblioteca");
            summary.Text=I18n.T("Logros")+": "+items.Count(a=>a.Completed)+" / "+items.Length+" · "+I18n.T("Pendientes")+": "+items.Count(a=>!a.Completed)+(game is not null&&(game.SteamAppId.HasValue&&game.Achievements is null||game.RetroGameId.HasValue&&game.RetroAchievements is null)?" · "+I18n.T("Sin sincronizar"):"");
            view.IsEnabled=game is not null&&!busy;previous.IsEnabled=index>0&&!busy;next.IsEnabled=index+1<ids.Length&&!busy;
            update.IsEnabled=!busy&&!owner.AchievementSyncBusy&&owner.Games.Any(owner.CanReviewAchievements);
        }
        window.Closed+=(_,_)=>cancellation.Cancel();Reload();window.ShowDialog();
    }

    internal static void ReviewCovers(MainWindow owner, Window parent)
    {
        var window=Modal(owner,I18n.T("Buscar carátulas que faltan"),620,730);window.Owner=parent;
        var body=Panel();Layout(window,body,out var footer);
        Heading(body,I18n.T("Buscar carátulas que faltan"),I18n.T("Se comprueba Steam antes de buscar en IGDB. Siguiente carátula descarta la propuesta; Siguiente juego continúa sin añadirla. Las decisiones se guardan al instante."));
        var ids=owner.Games.OrderBy(g=>g.Title,StringComparer.OrdinalIgnoreCase).Select(g=>g.Id).ToArray();int index=-1;bool busy=false,closed=false;
        var position=new TextBlock();var title=new TextBlock{FontSize=22,TextWrapping=TextWrapping.Wrap};var image=new Image{Height=240,Tag="cover-preview"};var match=new TextBlock{TextWrapping=TextWrapping.Wrap};var message=new TextBlock{TextWrapping=TextWrapping.Wrap};
        foreach(var node in new UIElement[]{position,title,image,match,message})body.Children.Add(node);
        IgdbCover? candidate=null;byte[]? prepared=null;
        using var cancellation=new CancellationTokenSource();Game? Current()=>index>=0&&index<ids.Length?owner.Games.FirstOrDefault(g=>g.Id==ids[index]):null;
        void SaveDecision(bool accept)
        {
            if(Current() is not {} game)return;
            game.IgdbCoverSearchTitle=game.Title;
            if(candidate is not null){if(accept&&prepared is not null){game.CustomCover=owner.Covers.SavePrepared(prepared);game.IgdbCoverImageId=candidate.ImageId;}else CoverSuggestions.Reject(game,candidate.ImageId);}
            owner.Persist();owner.Refresh();
        }
        Button accept=null!,another=null!,next=null!;
        accept=Button(I18n.T("Aceptar"),async(_,_)=>{if(busy||candidate is null)return;try{SaveDecision(true);await Load(true);}catch(Exception error){message.Text=I18n.Error(error);}});
        another=Button(I18n.T("Siguiente carátula"),async(_,_)=>{if(busy)return;try{SaveDecision(false);await Load(false);}catch(Exception error){message.Text=I18n.Error(error);}});
        next=Button(I18n.T("Siguiente juego"),async(_,_)=>{if(busy)return;try{SaveDecision(false);await Load(true);}catch(Exception error){message.Text=I18n.Error(error);}});
        // Keep the three review choices inside the scroll area so small windows do not clip them.
        var choices=new WrapPanel();choices.Children.Add(accept);choices.Children.Add(another);choices.Children.Add(next);body.Children.Add(choices);
        footer.Children.Add(Button(I18n.T("Cerrar"),(_,_)=>window.Close()));
        void Buttons(){accept.IsEnabled=!busy&&candidate is not null;another.IsEnabled=!busy&&Current() is not null;next.IsEnabled=!busy&&Current() is not null;}
        async Task Load(bool advance)
        {
            if(closed||busy)return;busy=true;candidate=null;prepared=null;image.Source=null;match.Text="";message.Text=I18n.T("Buscando carátula…");Buttons();
            try
            {
                if(advance)
                {
                    while(++index<ids.Length)
                    {
                        cancellation.Token.ThrowIfCancellationRequested();var item=Current();if(item is null)continue;
                        title.Text=item.Title;position.Text=I18n.T("Juego")+" "+(index+1)+" / "+ids.Length;
                        if(await owner.Covers.Get(item,true) is null)break;
                        if(closed)return;
                    }
                }
                if(closed)return;
                if(Current() is not {} game){title.Text=I18n.T("Revisión terminada");position.Text=I18n.T("Juegos revisados:")+" "+ids.Length;message.Text=I18n.T("No quedan juegos sin carátula por revisar.");return;}
                if(owner.Igdb is null)throw new InvalidOperationException(I18n.T("El responsable de esta edición debe configurar IGDB en Supabase."));
                if(game.RejectedIgdbCovers.Count>=200)throw new InvalidOperationException(I18n.T("Has rechazado 200 carátulas para este juego. No se harán más búsquedas."));
                var found=await owner.Igdb.Search(game.Title,game.RejectedIgdbCovers,cancellation.Token,true);
                if(found is null){message.Text=I18n.T("No hay otra carátula de IGDB disponible para este nombre.");return;}
                var bytes=CoverCache.PrepareRemote(await owner.Igdb.Image(found.ImageId,cancellation.Token));if(closed)return;
                candidate=found;prepared=bytes;image.Source=CoverCache.ReadPrepared(bytes);match.Text=found.Name+(found.Year is {} year?" · "+year:"");message.Text=I18n.T("Coincidencia más cercana por nombre. Comprueba que sea tu juego. Carátula: IGDB.");
            }
            catch(OperationCanceledException){}
            catch(Exception error){if(!closed)message.Text=I18n.Error(error);}
            finally{busy=false;if(!closed)Buttons();}
        }
        window.Loaded+=async(_,_)=>await Load(true);window.Closed+=(_,_)=>{closed=true;cancellation.Cancel();};Buttons();window.ShowDialog();
    }
}
public partial class MainWindow
{
    internal bool AchievementSyncBusy=>syncing;
    internal bool CanReviewAchievements(Game game)=>(game.SteamAppId.HasValue&&Steam.Session is not null)||(game.RetroGameId.HasValue&&Retro.Session is not null);
    internal async Task<(int Updated,int Errors)> ReviewAchievementSync(Action<int,int> progress,CancellationToken cancellation)
    {
        if(syncing)return(0,0);syncing=true;Refresh();int updated=0,errors=0;
        using var linked=CancellationTokenSource.CreateLinkedTokenSource(cancellation,shutdown.Token);
        var games=Games.Where(CanReviewAchievements).ToArray();var steamSession=Steam.Session;var retroSession=Retro.Session;
        try
        {
            foreach(var game in games)
            {
                linked.Token.ThrowIfCancellationRequested();bool failed=false;
                if(game.SteamAppId is int appId&&steamSession is not null&&Steam.Session==steamSession)
                {
                    try{var result=await Steam.Achievements(Preferences.ServiceUrl,appId,linked.Token);linked.Token.ThrowIfCancellationRequested();if(Steam.Session==steamSession&&Games.Contains(game)){game.Achievements=result.Achievements.ToList();game.SyncedAt=DateTimeOffset.UtcNow;}}
                    catch(OperationCanceledException) when(!linked.IsCancellationRequested){failed=true;}
                    catch(Exception error) when(error is not OutOfMemoryException and not OperationCanceledException){failed=true;}
                }
                if(game.RetroGameId is int retroId&&retroSession is not null&&Retro.Session==retroSession)
                {
                    try{var result=await Retro.Achievements(retroId,linked.Token);linked.Token.ThrowIfCancellationRequested();GameRules.Validate(new Game{Title=game.Title,RetroAchievements=result});if(Retro.Session==retroSession&&Games.Contains(game))game.RetroAchievements=result;}
                    catch(OperationCanceledException) when(!linked.IsCancellationRequested){failed=true;}
                    catch(Exception error) when(error is not OutOfMemoryException and not OperationCanceledException){failed=true;}
                }
                Persist();if(failed)errors++;else updated++;progress(updated+errors,games.Length);
            }
            return(updated,errors);
        }
        finally{syncing=false;if(!shutdown.IsCancellationRequested)Refresh();}
    }
}
