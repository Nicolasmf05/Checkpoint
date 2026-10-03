using System;
using System.Collections.Generic;
using System.Linq;

namespace Checkpoint.Core;

public static class GameLists
{
    public static List<string> Normalize(IEnumerable<string>? names) => (names ?? []).Where(n=>!string.IsNullOrWhiteSpace(n))
        .Select(n=>n.Trim()).Where(n=>n.Length<=40&&!n.Any(char.IsControl)).Distinct(StringComparer.OrdinalIgnoreCase).Take(30).ToList();
    public static string ValidateName(string name,IEnumerable<string> existing,string? previous=null)
    {
        name=(name??"").Trim();
        if(name.Length is <1 or >40 || name.Any(char.IsControl))throw new ArgumentException(I18n.T("El nombre de la lista debe tener entre 1 y 40 caracteres."));
        if(new[]{"Mi lista","My list","Privados","Private games","Biblioteca","Library"}.Contains(name,StringComparer.OrdinalIgnoreCase)
            ||existing.Any(n=>n.Equals(name,StringComparison.OrdinalIgnoreCase)&&!n.Equals(previous,StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException(I18n.T("Ya existe una lista con ese nombre."));
        if(previous is null && Normalize(existing).Count>=30)throw new ArgumentException(I18n.T("Puedes crear hasta 30 listas."));
        return name;
    }
    public static bool Visible(Game game,string selection) => selection=="private"?game.FriendsPrivate==true:
        game.Tracked&&game.FriendsPrivate!=true&&(selection=="all"||selection.StartsWith("custom:",StringComparison.Ordinal)&&game.Lists.Contains(selection[7..],StringComparer.OrdinalIgnoreCase));
    public static bool ShouldShare(Game game)=>game.Tracked&&game.FriendsPrivate!=true;
    public static void Rename(List<Game> games,string oldName,string newName)
    {foreach(var game in games)game.Lists=Normalize(game.Lists.Select(n=>n.Equals(oldName,StringComparison.OrdinalIgnoreCase)?newName:n));}
}
