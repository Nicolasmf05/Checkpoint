// Combina logros de Steam, RetroAchievements y entradas manuales mediante claves por proveedor.
// También contiene las reglas puras para identificar procesos de juegos instalados.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Checkpoint.Core;

public sealed record TrackedAchievement(
    string Key,
    string Provider,
    Achievement Data,
    bool Completed,
    bool ManualOverride
);

public static class AchievementTracking
{
    // Una corrección manual prevalece sobre el proveedor; las eliminaciones se filtran mediante la clave estable.
    public static IEnumerable<TrackedAchievement> Items(Game game, bool includeRemoved = false)
    {
        foreach (
            var (provider, items) in new[]
            {
                ("steam", game.Achievements ?? []),
                ("retro", game.RetroAchievements ?? []),
                ("manual", game.ManualAchievements),
            }
        )
        foreach (var item in items)
        {
            string key = provider + ":" + item.Id;
            if (!includeRemoved && game.RemovedAchievements.Contains(key))
                continue;
            bool manual = game.AchievementOverrides.TryGetValue(key, out bool completed);
            yield return new(key, provider, item, manual ? completed : item.Unlocked, manual);
        }
    }

    // Los logros del proveedor se ocultan para que una sincronización posterior no los vuelva a mostrar.
    public static void Remove(Game game, TrackedAchievement item)
    {
        if (item.Provider == "manual")
            game.ManualAchievements.RemoveAll(a => a.Id == item.Data.Id);
        else if (!game.RemovedAchievements.Contains(item.Key))
            game.RemovedAchievements.Add(item.Key);
        game.AchievementOverrides.Remove(item.Key);
    }

    public static void ClearManual(Game game)
    {
        game.ManualAchievements.Clear();
        game.RemovedAchievements.RemoveAll(key =>
            key.StartsWith("manual:", StringComparison.Ordinal)
        );
        foreach (
            var key in game
                .AchievementOverrides.Keys.Where(key =>
                    key.StartsWith("manual:", StringComparison.Ordinal)
                )
                .ToArray()
        )
            game.AchievementOverrides.Remove(key);
    }

    public static Achievement Add(Game game, string name, string description)
    {
        if (
            string.IsNullOrWhiteSpace(name)
            || name.Trim().Length > 250
            || description.Length > 2000
            || game.ManualAchievements.Count >= 200
        )
            throw new ArgumentException(
                I18n.T(
                    "Escribe un nombre de logro de 1 a 250 caracteres. Puedes añadir hasta 200 logros manuales."
                )
            );
        var result = new Achievement
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name.Trim(),
            Description = description,
        };
        game.ManualAchievements.Add(result);
        return result;
    }
}

public sealed record RunningGameProcess(int Id, string Name, string Title, string? Path);

public sealed record InstalledSteamGame(int AppId, string Title, string Directory);

public static class GameDetection
{
    // Steam se reconoce por la carpeta instalada; la detección personalizada usa nombre de proceso y, opcionalmente, título.
    public static bool Matches(
        Game game,
        RunningGameProcess process,
        IEnumerable<InstalledSteamGame> installed
    )
    {
        bool custom =
            !string.IsNullOrWhiteSpace(game.DetectionProcess)
            && System
                .IO.Path.GetFileNameWithoutExtension(game.DetectionProcess)
                .Equals(process.Name, StringComparison.OrdinalIgnoreCase);
        bool steam =
            game.SteamAppId is int id
            && process.Path is string path
            && installed.Any(g =>
                g.AppId == id
                && path.StartsWith(
                    g.Directory.TrimEnd('\\', '/') + System.IO.Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase
                )
            );
        return (custom || steam)
            && (
                string.IsNullOrWhiteSpace(game.DetectionWindowTitle)
                || process.Title.Contains(
                    game.DetectionWindowTitle,
                    StringComparison.OrdinalIgnoreCase
                )
            );
    }
}
