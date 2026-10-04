using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Checkpoint.Core;
namespace Checkpoint.App;
internal static partial class Dialogs
{
    internal static bool IgdbCoverPreview(MainWindow owner,Window parent,string title,IgdbCover candidate,byte[] prepared)
    {
        var window=Modal(owner,I18n.T("Carátula de IGDB"),460,590);
        Parent(window,parent);var body=Panel();Layout(window,body,out var footer);
        Heading(body,I18n.T("¿Quieres usar esta carátula?"),title);
        body.Children.Add(new Image { Source=CoverCache.ReadPrepared(prepared),Height=240,Tag="cover-preview" });
        body.Children.Add(new TextBlock { Text=candidate.Name+(candidate.Year is {} year?" · "+year:""),FontSize=18,TextWrapping=TextWrapping.Wrap });
        body.Children.Add(new TextBlock { Text=I18n.T("Coincidencia más cercana por nombre. Comprueba que sea tu juego. Carátula: IGDB."),TextWrapping=TextWrapping.Wrap });
        body.Children.Add(new TextBlock { Text=I18n.T("Si la rechazas no volveremos a ofrecerla. Puedes buscar otra desde el editor del juego."),TextWrapping=TextWrapping.Wrap });
        bool accepted=false;
        footer.Children.Add(Button(I18n.T("No usar esta carátula"),(_,_)=>window.Close()));
        footer.Children.Add(Button(I18n.T("Usar esta carátula"),(_,_)=>{accepted=true;window.Close();},true));
        ShowPage(window);return accepted;
    }
}
public partial class MainWindow
{
    internal IgdbClient? Igdb;
    private readonly Queue<Guid> missingCovers=new();
    private readonly HashSet<Guid> coverLookups=new();
    private readonly DispatcherTimer coverTimer=new() { Interval=TimeSpan.FromSeconds(3) };
    private bool coverSuggestionBusy;
    private void InitializeCoverSuggestions()
    {
        try {
            var path=Path.Combine(AppContext.BaseDirectory,"supabase-config.json");
            if(File.Exists(path))Igdb=new(JsonSerializer.Deserialize<SocialProject>(File.ReadAllText(path),DataJson.Options)!);
        }catch(Exception error) when(error is IOException or JsonException or ArgumentException){}
        coverTimer.Tick+=async(_,_)=>{
            if(HasInlinePage||coverSuggestionBusy||!IsActive||Preferences.MiniatureView||Preferences.LightweightMode||friendsVisible||Application.Current.Windows.Cast<Window>().Any(w=>w!=this&&w.IsVisible))return;
            if(!missingCovers.TryDequeue(out var id))return;
            var game=Games.FirstOrDefault(g=>g.Id==id);if(game is null||!CoverSuggestions.ShouldSuggest(game))return;
            coverSuggestionBusy=true;
            try { await FindIgdbCover(game,this,game.Title,true); }finally{coverSuggestionBusy=false;}
        };
        if(!App.Diagnostics)coverTimer.Start();
        Closed+=(_,_)=>{coverTimer.Stop();Igdb?.Dispose();};
    }
    private void QueueCoverSuggestion(Game game)
    {
        if(App.Diagnostics||Igdb is null||Preferences.LightweightMode||Preferences.MiniatureView||!CoverSuggestions.ShouldSuggest(game)||coverLookups.Count>=10||!coverLookups.Add(game.Id))return;
        missingCovers.Enqueue(game.Id);
    }
    internal async Task FindIgdbCover(Game game,Window parent,string title,bool automatic=false)
    {
        try
        {
            if(Igdb is null)throw new InvalidOperationException(I18n.T("El responsable de esta edición debe configurar IGDB en Supabase."));
            if(game.RejectedIgdbCovers.Count>=200)throw new InvalidOperationException(I18n.T("Has rechazado 200 carátulas para este juego. No se harán más búsquedas."));
            var candidate=await Igdb.Search(title,game.RejectedIgdbCovers,shutdown.Token,!automatic);
            if(candidate is null){game.IgdbCoverSearchTitle=title.Trim();if(!automatic)LocalizedNotice.Show(parent,I18n.T("No hay otra carátula de IGDB disponible para este nombre."),"Checkpoint");}
            else
            {
                var prepared=CoverCache.PrepareRemote(await Igdb.Image(candidate.ImageId,shutdown.Token));
                if(automatic&&(Games.All(g=>g.Id!=game.Id)||game.CustomCover is not null||game.Title!=title||!IsActive||Preferences.MiniatureView||Preferences.LightweightMode||Application.Current.Windows.Cast<Window>().Any(w=>w!=this&&w.IsVisible)))return;
                if(!PageIsOpen(parent))return;
                game.IgdbCoverSearchTitle=title.Trim();
                if(Dialogs.IgdbCoverPreview(this,parent,title,candidate,prepared)){game.CustomCover=Covers.SavePrepared(prepared);game.IgdbCoverImageId=candidate.ImageId;}
                else CoverSuggestions.Reject(game,candidate.ImageId);
            }
            if(automatic){Persist();Refresh();}
            else if(Games.FirstOrDefault(g=>g.Id==game.Id) is {} existing)
            {
                // An explicit rejection survives cancelling the game editor; accepted images wait for Save.
                existing.RejectedIgdbCovers=game.RejectedIgdbCovers.ToList();existing.IgdbCoverSearchTitle=game.IgdbCoverSearchTitle;Persist();
            }
        }
        catch(Exception error) when(error is not OutOfMemoryException)
        { if(!automatic&&!shutdown.IsCancellationRequested)LocalizedNotice.Show(parent,I18n.Error(error),I18n.T("Carátula de IGDB")); }
    }
}
