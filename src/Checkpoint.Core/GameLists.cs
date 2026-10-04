// Define nombres, pertenencia, visibilidad y operaciones por lotes de las listas.
// Las reglas se comparten entre los controladores sin depender de controles de interfaz.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Checkpoint.Core;

public static class GameLists
{
    // Deduplica sin distinguir mayúsculas, conserva el primer nombre y limita el catálogo a treinta listas.
    public static List<string> Normalize(IEnumerable<string>? names) =>
        (names ?? [])
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .Where(n => n.Length <= 40 && !n.Any(char.IsControl))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(30)
            .ToList();

    public static string ValidateName(
        string name,
        IEnumerable<string> existing,
        string? previous = null
    )
    {
        name = (name ?? "").Trim();
        if (name.Length is < 1 or > 40 || name.Any(char.IsControl))
            throw new ArgumentException(
                I18n.T("El nombre de la lista debe tener entre 1 y 40 caracteres.")
            );
        if (
            new[]
            {
                "Mi lista",
                "My list",
                "Privados",
                "Private games",
                "Biblioteca",
                "Library",
            }.Contains(name, StringComparer.OrdinalIgnoreCase)
            || existing.Any(n =>
                n.Equals(name, StringComparison.OrdinalIgnoreCase)
                && !n.Equals(previous, StringComparison.OrdinalIgnoreCase)
            )
        )
            throw new ArgumentException(I18n.T("Ya existe una lista con ese nombre."));
        if (previous is null && Normalize(existing).Count >= 30)
            throw new ArgumentException(I18n.T("Puedes crear hasta 30 listas."));
        return name;
    }

    public static bool Visible(Game game, string selection) =>
        selection == "private"
            ? game.FriendsPrivate == true
            : game.Tracked
                && game.FriendsPrivate != true
                && (
                    selection == "all"
                    || selection.StartsWith("custom:", StringComparison.Ordinal)
                        && game.Lists.Contains(selection[7..], StringComparer.OrdinalIgnoreCase)
                );

    public static bool ShouldShare(Game game) => game.Tracked && game.FriendsPrivate != true;

    public static void Rename(List<Game> games, string oldName, string newName)
    {
        foreach (var game in games)
            game.Lists = Normalize(
                game.Lists.Select(n =>
                    n.Equals(oldName, StringComparison.OrdinalIgnoreCase) ? newName : n
                )
            );
    }

    // Las fichas de listas incluyen sus miembros aunque la privacidad los oculte en la vista habitual.
    public static IEnumerable<Game> Members(IEnumerable<Game> games, string selection) =>
        selection.StartsWith("custom:", StringComparison.Ordinal)
            ? games.Where(g => g.Lists.Contains(selection[7..], StringComparer.OrdinalIgnoreCase))
            : games.Where(g => selection == "private" ? g.FriendsPrivate == true : g.Tracked);

    // Valida selección, origen y destino antes de cambiar cualquier juego del lote.
    public static void Apply(
        List<Game> games,
        IEnumerable<Guid> ids,
        IEnumerable<string> catalog,
        string operation,
        string source,
        string? target = null
    )
    {
        var keys = ids.Distinct().ToHashSet();
        var chosen = games.Where(g => keys.Contains(g.Id)).ToArray();
        if (keys.Count is < 1 or > 500 || chosen.Length != keys.Count)
            throw new ArgumentException(I18n.T("Selecciona entre 1 y 500 juegos existentes."));
        if (operation is not ("move" or "add" or "track" or "remove" or "private" or "public"))
            throw new ArgumentException(I18n.T("Acción de lista no válida."));
        string? destination = null;
        if (operation is "move" or "add")
        {
            destination = catalog.FirstOrDefault(n =>
                n.Equals(target, StringComparison.OrdinalIgnoreCase)
            );
            if (destination is null)
                throw new ArgumentException(I18n.T("La lista de destino ya no existe."));
        }
        if (
            source is not ("all" or "library" or "private")
                && !source.StartsWith("custom:", StringComparison.Ordinal)
            || source.StartsWith("custom:", StringComparison.Ordinal)
                && !catalog.Contains(source[7..], StringComparer.OrdinalIgnoreCase)
            || operation == "remove" && source == "private"
        )
            throw new ArgumentException(I18n.T("Acción de lista no válida."));
        foreach (var game in chosen)
        {
            switch (operation)
            {
                case "move":
                    game.Lists =
                        source.StartsWith("custom:", StringComparison.Ordinal)
                            ? Normalize(
                                game.Lists.Where(n =>
                                    !n.Equals(source[7..], StringComparison.OrdinalIgnoreCase)
                                )
                            )
                        : source == "private" ? Normalize(game.Lists)
                        : [];
                    goto case "add";
                case "add":
                    game.Lists = Normalize(game.Lists.Append(destination!));
                    goto case "track";
                case "track":
                    game.Tracked = true;
                    break;
                case "remove":
                    if (source.StartsWith("custom:", StringComparison.Ordinal))
                        game.Lists.RemoveAll(n =>
                            n.Equals(source[7..], StringComparison.OrdinalIgnoreCase)
                        );
                    else
                        game.Tracked = false;
                    break;
                case "private":
                    game.FriendsPrivate = true;
                    break;
                case "public":
                    game.FriendsPrivate = false;
                    game.Tracked = true;
                    break;
            }
        }
    }
}
