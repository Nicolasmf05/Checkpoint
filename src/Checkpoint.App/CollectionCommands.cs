using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Checkpoint.Core;

namespace Checkpoint.App;

public partial class MainWindow
{
    internal void DeleteGame(Game game)
    {
        CaptureBounds();
        var remaining = Games.Where(g => g.Id != game.Id).ToList();
        Store.DeleteGame(game, remaining, Preferences);
        Games.RemoveAll(g => g.Id == game.Id);
        SchedulePublications();
        DeletedGames = Store.LoadDeletedGames(); Refresh();
        Notice((I18n.IsEnglish ? $"«{game.Title}» deleted. Restore it with ↶ or from Settings." : $"«{game.Title}» eliminado. Recupéralo con ↶ o desde Ajustes."));
    }
    internal Game RestoreDeleted(Guid recoveryId)
    {
        CaptureBounds();
        var game = Store.RestoreDeletedGame(recoveryId, Games, Preferences);
        Games.Add(game); DeletedGames = Store.LoadDeletedGames(); Refresh();
        Notice((I18n.IsEnglish ? $"«{game.Title}» restored with its notes and tasks." : $"«{game.Title}» recuperado con sus notas y tareas.")); return game;
    }
    private void UndoClick(object sender, System.Windows.RoutedEventArgs e) => UndoLastDeletion();
    private void UndoLastDeletion()
    {
        if (DeletedGames.Count == 0) return;
        try { RestoreDeleted(DeletedGames[0].RecoveryId); }
        catch (Exception ex) { Notice(ex.Message); }
    }
    internal void ExportBackup(string path)
    {
        if (string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase))
        { Store.Export(path, Games); Notice(I18n.T("Copia JSON exportada. Este formato no incluye imágenes.")); }
        else
        {
            int covers = BackupFiles.WriteComplete(path, Games, Covers.DirectoryPath);
            Notice((I18n.IsEnglish ? $"Complete backup exported: {Games.Count} games and {covers} custom covers." : $"Copia completa exportada: {Games.Count} juegos y {covers} carátulas personalizadas."));
        }
    }
    internal (int Added, int Skipped) ImportBackup(string path)
    {
        var backup = BackupFiles.Read(path);
        var ids = Games.Select(g => g.Id).ToHashSet();
        var steamIds = Games.Where(g => g.SteamAppId.HasValue).Select(g => g.SteamAppId!.Value).ToHashSet();
        var added = backup.Games.Where(g => !ids.Contains(g.Id) && (g.SteamAppId is null || !steamIds.Contains(g.SteamAppId.Value))).ToList();
        int skipped = backup.Games.Count - added.Count;
        // Decode every new image before creating files or changing the database.
        var prepared = new Dictionary<byte[], byte[]>();
        foreach (var game in added)
            if (backup.CustomCovers.TryGetValue(game.Id, out var bytes) && !prepared.ContainsKey(bytes))
                prepared[bytes] = CoverCache.PrepareImport(bytes);
        var created = new Dictionary<byte[], string>();
        try
        {
            foreach (var game in added)
            {
                if (!backup.CustomCovers.TryGetValue(game.Id, out var bytes)) continue;
                if (!created.TryGetValue(bytes, out var name)) { name = Covers.SavePrepared(prepared[bytes]); created.Add(bytes, name); }
                game.CustomCover = name;
            }
            if (added.Count > 0) { CaptureBounds(); Store.Save(Games.Concat(added), Preferences); Games.AddRange(added); }
        }
        catch
        {
            foreach (string name in created.Values)
                try { Covers.RemoveCreated(name); } catch (IOException) { /* An unused cache image does not alter the saved collection. */ }
            throw;
        }
        Refresh(); Notice((I18n.IsEnglish ? $"{added.Count} games imported · {skipped} already in the library." : $"{added.Count} juegos importados · {skipped} ya estaban en la biblioteca."));
        return (added.Count, skipped);
    }
}
