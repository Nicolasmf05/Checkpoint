using System.Text.Json;
using System.Text.Json.Serialization;

namespace Checkpoint.Core;

public enum GameStatus { Pending, Playing, Paused, Finished, Abandoned }
public enum GameGoal { Story, Achievements, Custom }

public sealed class ChecklistItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public bool Done { get; set; }
}

public sealed class Achievement
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Hidden { get; set; }
    public bool Unlocked { get; set; }
    public DateTimeOffset? UnlockedAt { get; set; }
}

public sealed class Game
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public int? SteamAppId { get; set; }
    public string Platform { get; set; } = "PC";
    public GameStatus Status { get; set; }
    public GameGoal Goal { get; set; }
    public string CustomGoal { get; set; } = "";
    public int? StoryPercent { get; set; }
    public string Notes { get; set; } = "";
    public string? CustomCover { get; set; }
    public bool Tracked { get; set; } = true;
    public bool Favorite { get; set; }
    public int SortOrder { get; set; }
    public int PlaytimeMinutes { get; set; }
    public DateTimeOffset AddedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FinishedAt { get; set; }
    public DateTimeOffset? SyncedAt { get; set; }
    public List<ChecklistItem> Tasks { get; set; } = [];
    // null means never available; an empty list means this game has no achievements.
    public List<Achievement>? Achievements { get; set; }

    [JsonIgnore] public int UnlockedCount => Achievements?.Count(a => a.Unlocked) ?? 0;
    [JsonIgnore] public int? AchievementPercent => Achievements is { Count: > 0 }
        ? (int)Math.Round(UnlockedCount * 100d / Achievements.Count) : null;
    [JsonIgnore] public bool AllAchievements => Achievements is { Count: > 0 } && Achievements.All(a => a.Unlocked);
    [JsonIgnore] public string StatusText => Labels.Status(Status);
    [JsonIgnore] public string GoalText => Goal switch
    {
        GameGoal.Story => I18n.T("Terminar la historia"),
        GameGoal.Achievements => I18n.T("Conseguir todos los logros"),
        _ => string.IsNullOrWhiteSpace(CustomGoal) ? I18n.T("Objetivo personal") : CustomGoal
    };
    [JsonIgnore] public string NextTask => Tasks.FirstOrDefault(t => !t.Done)?.Title ?? GoalText;
}

public static class Labels
{
    public static string Status(GameStatus status) => status switch
    {
        GameStatus.Pending => I18n.T("Pendiente"), GameStatus.Playing => I18n.T("Jugando"),
        GameStatus.Paused => I18n.T("Pausado"), GameStatus.Finished => I18n.T("Historia terminada"), _ => I18n.T("Abandonado")
    };
    public static string Goal(GameGoal goal) => goal switch
    {
        GameGoal.Story => I18n.T("Historia"), GameGoal.Achievements => I18n.T("Todos los logros"), _ => I18n.T("Personalizado")
    };
}

public sealed class Settings
{
    public string Language { get; set; } = "es";
    public double Width { get; set; } = 510;
    public double Height { get; set; } = 740;
    public double? Left { get; set; }
    public double? Top { get; set; }
    public double BackgroundOpacity { get; set; } = .88;
    public bool AlwaysOnTop { get; set; }
    public bool PositionLocked { get; set; }
    public bool Compact { get; set; }
    public bool GridView { get; set; }
    public bool FullWindow { get; set; }
    public bool MiniatureView { get; set; }
    public double MiniatureWidth { get; set; } = 300;
    public double MiniatureHeight { get; set; } = 220;
    public int MiniatureTextSize { get; set; } = 12;
    public bool LightTheme { get; set; }
    public string Theme { get; set; } = "";
    public bool LightweightMode { get; set; }
    public bool CloseToTray { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public int SyncMinutes { get; set; } = 30;
    public string ServiceUrl { get; set; } = "";
    public string? SteamId { get; set; }
}

public sealed class Backup
{
    public int Version { get; set; } = 1;
    public DateTimeOffset ExportedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<Game> Games { get; set; } = [];
    public Dictionary<Guid, string> Covers { get; set; } = [];
}

public sealed record DeletedGame(Guid RecoveryId, Game Game, DateTimeOffset DeletedAt);

public static class DataJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true, Converters = { new JsonStringEnumConverter() },
        MaxDepth = 32
    };
}

public static class GameRules
{
    public static IOrderedEnumerable<Game> InDisplayOrder(IEnumerable<Game> games) => games
        .OrderByDescending(g => g.Favorite).ThenBy(g => g.SortOrder)
        .ThenBy(g => g.Title, StringComparer.CurrentCultureIgnoreCase).ThenBy(g => g.Id);

    // Move relative to a visible target while retaining the order of hidden games.
    // Favorites stay in their own pinned group; a move never changes game metadata.
    public static bool Move(List<Game> games, Guid sourceId, Guid targetId, bool after)
    {
        if (sourceId == targetId) return false;
        var source = games.FirstOrDefault(g => g.Id == sourceId);
        var target = games.FirstOrDefault(g => g.Id == targetId);
        if (source is null || target is null || source.Favorite != target.Favorite) return false;
        var ordered = InDisplayOrder(games).ToList();
        var before = ordered.Select(g => g.Id).ToArray();
        ordered.Remove(source);
        ordered.Insert(ordered.IndexOf(target) + (after ? 1 : 0), source);
        if (before.SequenceEqual(ordered.Select(g => g.Id))) return false;
        for (int i = 0; i < ordered.Count; i++) ordered[i].SortOrder = i;
        return true;
    }

    public static void SetStatus(Game game, GameStatus status)
    {
        if (!Enum.IsDefined(status)) throw new ArgumentException(I18n.T("Estado desconocido."));
        if (status == GameStatus.Finished && game.Status != GameStatus.Finished)
            game.FinishedAt = DateTimeOffset.UtcNow;
        else if (status != GameStatus.Finished) game.FinishedAt = null;
        game.Status = status;
    }

    public static void Validate(Game game)
    {
        game.Title ??= ""; game.Platform ??= "PC"; game.Notes ??= ""; game.CustomGoal ??= ""; game.Tasks ??= [];
        if (game.Tasks.Any(t => t is null || t.Title is null)) throw new ArgumentException(I18n.T("Una tarea no es válida."));
        if (game.Achievements?.Any(a => a is null || a.Id is null || a.Name is null || a.Description is null) == true)
            throw new ArgumentException(I18n.T("Los logros guardados no son válidos."));
        game.Title = game.Title.Trim();
        if (game.Title.Length is < 1 or > 140) throw new ArgumentException(I18n.T("El nombre debe tener entre 1 y 140 caracteres."));
        if (game.SteamAppId is <= 0) throw new ArgumentException(I18n.T("El identificador de Steam debe ser un número positivo."));
        if (game.StoryPercent is < 0 or > 100) throw new ArgumentException(I18n.T("El porcentaje de historia debe estar entre 0 y 100."));
        if (!Enum.IsDefined(game.Status) || !Enum.IsDefined(game.Goal)) throw new ArgumentException(I18n.T("Estado u objetivo desconocido."));
        if (game.Notes.Length > 20000 || game.Tasks.Count > 200 || game.Tasks.Any(t => t.Title.Length > 500))
            throw new ArgumentException(I18n.T("Las notas o tareas son demasiado largas."));
        game.Platform = game.SteamAppId is null ? game.Platform.Trim()[..Math.Min(game.Platform.Trim().Length, 60)] : "Steam";
        if (game.Platform.Length == 0) game.Platform = "PC";
        game.CustomGoal = game.CustomGoal[..Math.Min(game.CustomGoal.Length, 500)];
        game.PlaytimeMinutes = Math.Max(0, game.PlaytimeMinutes);
    }

    public static int MergeSteamLibrary(List<Game> games, IEnumerable<SteamGame> library)
    {
        int added = 0;
        var existing = games.Where(g => g.SteamAppId.HasValue).ToDictionary(g => g.SteamAppId!.Value, g => g);
        foreach (var remote in library)
        {
            if (remote.AppId <= 0 || string.IsNullOrWhiteSpace(remote.Name)) continue;
            if (existing.TryGetValue(remote.AppId, out var game))
                game.PlaytimeMinutes = Math.Max(0, remote.PlaytimeMinutes);
            else
            {
                game = new Game { Title = remote.Name, SteamAppId = remote.AppId, Platform = "Steam",
                    PlaytimeMinutes = Math.Max(0, remote.PlaytimeMinutes), Tracked = false, SortOrder = games.Count };
                games.Add(game); existing[remote.AppId] = game; added++;
            }
        }
        return added;
    }
}

public sealed record SteamGame(int AppId, string Name, int PlaytimeMinutes);
