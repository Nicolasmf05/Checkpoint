// Contratos de la API social y cola persistente de publicaciones por cuenta.
// Las revisiones e identificadores de operación permiten reintentar y detectar conflictos.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Checkpoint.Core;

public sealed record SocialProject(string Url, string PublishableKey)
{
    public Uri Validate()
    {
        if (
            !Uri.TryCreate(Url.TrimEnd('/') + "/", UriKind.Absolute, out var uri)
            || uri.Scheme != "https"
            || !uri.Host.EndsWith(".supabase.co", StringComparison.OrdinalIgnoreCase)
            || uri.AbsolutePath != "/"
            || uri.UserInfo.Length > 0
            || uri.Query.Length > 0
            || uri.Fragment.Length > 0
            || !PublishableKey.StartsWith("sb_publishable_", StringComparison.Ordinal)
            || PublishableKey.Length > 200
        )
            throw new ArgumentException(
                I18n.T("La configuración pública de Checkpoint no es válida.")
            );
        return uri;
    }
}

public sealed record SocialSession(
    string ProjectUrl,
    Guid UserId,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt
);

public sealed record SocialProfile(
    [property: JsonPropertyName("user_id")] Guid UserId,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("friend_code")] string FriendCode
);

public sealed record SocialRequest(
    Guid Id,
    [property: JsonPropertyName("sender_id")] Guid SenderId,
    [property: JsonPropertyName("recipient_id")] Guid RecipientId,
    string Status
);

public sealed record SocialFriendship(
    [property: JsonPropertyName("user_low")] Guid UserLow,
    [property: JsonPropertyName("user_high")] Guid UserHigh
);

public sealed record SocialGroup(
    Guid Id,
    string Name,
    [property: JsonPropertyName("owner_id")] Guid OwnerId,
    [property: JsonPropertyName("game_title")] string GameTitle,
    [property: JsonPropertyName("steam_app_id")] int? SteamAppId,
    JsonElement Goals
);

public sealed record SocialGroupMember(
    [property: JsonPropertyName("group_id")] Guid GroupId,
    [property: JsonPropertyName("user_id")] Guid UserId
);

public sealed record SocialGroupInvite(
    Guid Id,
    [property: JsonPropertyName("group_id")] Guid GroupId,
    [property: JsonPropertyName("inviter_id")] Guid InviterId,
    [property: JsonPropertyName("invitee_id")] Guid InviteeId,
    string Status
);

public sealed record SharedGroupGoal(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("completed")] bool Completed
);

public sealed record SocialPublication(
    [property: JsonPropertyName("owner_id")] Guid OwnerId,
    [property: JsonPropertyName("game_id")] Guid GameId,
    long Revision,
    [property: JsonPropertyName("operation_id")] Guid? OperationId,
    [property: JsonPropertyName("is_shared")] bool IsShared,
    [property: JsonPropertyName("operation_payload")] SharedGamePayload? Payload,
    [property: JsonPropertyName("updated_at")] DateTimeOffset UpdatedAt
);

public sealed record SharedManualAchievement(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("completed")] bool Completed
);

// Explicit allowlist. Never serialize Game itself for a publication.
public sealed record SharedGamePayload(
    string Title,
    string Platform,
    string Status,
    string? GoalKind,
    string? GoalText,
    int? StoryPercent,
    int? TasksDone,
    int? TasksTotal,
    int? AchievementsUnlocked,
    int? AchievementsTotal,
    int? SteamAppId,
    string? CoverPath,
    DateTimeOffset? FinishedAt,
    string? ManualAchievementsJson = null
)
{
    public static SharedGamePayload From(Game game) =>
        new(
            game.Title,
            game.Platform,
            game.Status.ToString().ToLowerInvariant(),
            game.GoalVisible ? game.Goal.ToString().ToLowerInvariant() : null,
            game.Goal == GameGoal.Custom ? game.CustomGoal : null,
            game.StoryPercent,
            game.Tasks.Count > 0 ? game.Tasks.Count(t => t.Done) : null,
            game.Tasks.Count > 0 ? game.Tasks.Count : null,
            game.Achievements is { Count: > 0 } ? game.UnlockedCount : null,
            game.Achievements is { Count: > 0 } ? game.Achievements.Count : null,
            game.SteamAppId,
            null,
            game.FinishedAt,
            JsonSerializer.Serialize(
                AchievementTracking
                    .Items(game)
                    .Where(a => a.Provider == "manual")
                    .Select(a => new SharedManualAchievement(
                        a.Data.Name,
                        a.Data.Description,
                        a.Completed
                    ))
            )
        );

    [JsonIgnore]
    public SharedManualAchievement[] ManualAchievements
    {
        get
        {
            if (
                string.IsNullOrEmpty(ManualAchievementsJson)
                || ManualAchievementsJson.Length > 600000
            )
                return [];
            try
            {
                return (
                    JsonSerializer.Deserialize<SharedManualAchievement[]>(ManualAchievementsJson)
                    ?? []
                )
                    .Take(200)
                    .ToArray();
            }
            catch (JsonException)
            {
                return [];
            }
        }
    }

    [JsonIgnore]
    public string StatusText =>
        Status switch
        {
            "pending" => I18n.T("Pendiente"),
            "playing" => I18n.T("Jugando"),
            "paused" => I18n.T("Pausado"),
            "finished" => I18n.T("Terminado"),
            _ => I18n.T("Abandonado"),
        };

    [JsonIgnore]
    public string ProgressText =>
        string.Join(
            " · ",
            new[]
            {
                StoryPercent is int story
                    ? (I18n.IsEnglish ? $"Story: {story}%" : $"Historia: {story}%")
                    : null,
                TasksTotal is int tasks
                    ? (
                        I18n.IsEnglish
                            ? $"Tasks: {TasksDone}/{tasks}"
                            : $"Tareas: {TasksDone}/{tasks}"
                    )
                    : null,
                AchievementsTotal is int achievements
                    ? (
                        I18n.IsEnglish
                            ? $"Saved achievements: {AchievementsUnlocked}/{achievements}"
                            : $"Logros guardados: {AchievementsUnlocked}/{achievements}"
                    )
                    : null,
            }.Where(s => s is not null)
        );
}

public sealed class SocialApiException(string message, string? code = null)
    : InvalidOperationException(message)
{
    public string? Code { get; } = code;
    public bool IsConflict => Code == "40001";
}

public sealed class ShareOperation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long ExpectedRevision { get; set; }
    public SharedGamePayload? Payload { get; set; }
    public string? LocalCover { get; set; }
}

public sealed class ShareEntry
{
    public bool Selected { get; set; }
    public long Revision { get; set; }
    public SharedGamePayload? Desired { get; set; }
    public SharedGamePayload? Published { get; set; }
    public string? LocalCover { get; set; }
    public ShareOperation? Pending { get; set; }
    public bool Conflict { get; set; }

    [JsonIgnore]
    public bool HasWork => Pending is not null || Desired != Published;
}

public sealed class SocialOutbox
{
    public string ProjectUrl { get; set; } = "";
    public Guid UserId { get; set; }
    public Dictionary<Guid, ShareEntry> Games { get; set; } = [];

    public ShareEntry Entry(Guid id)
    {
        if (!Games.TryGetValue(id, out var entry))
            Games[id] = entry = new();
        return entry;
    }

    public void SetDesired(Guid id, SharedGamePayload? payload, string? localCover = null)
    {
        var entry = Entry(id);
        entry.Selected = payload is not null;
        entry.Desired = payload;
        entry.LocalCover = localCover;
        // Keep an in-flight operation intact until its result is reconciled.
        if (payload is null)
            entry.Conflict = false;
    }

    // Reutiliza la operación pendiente para que los reintentos tengan el mismo identificador.
    public ShareOperation? Prepare(Guid id)
    {
        var entry = Entry(id);
        if (entry.Conflict)
            return null;
        return entry.Pending ??=
            entry.Desired == entry.Published
                ? null
                : new()
                {
                    ExpectedRevision = entry.Revision,
                    Payload = entry.Desired,
                    LocalCover = entry.LocalCover,
                };
    }

    public void Acknowledge(Guid id, long revision)
    {
        var entry = Entry(id);
        if (entry.Pending is null)
            throw new InvalidOperationException(I18n.T("No hay publicación pendiente."));
        entry.Revision = revision;
        entry.Published = entry.Pending.Payload;
        entry.Pending = null;
        entry.Conflict = false;
    }

    // Un resultado de nuestra operación confirma el envío; una revisión ajena puede requerir resolver un conflicto.
    public void Reconcile(Guid id, SocialPublication remote)
    {
        var entry = Entry(id);
        if (remote.Payload is { } payload)
            remote = remote with { Payload = payload with { CoverPath = null } };
        if (entry.Pending?.Id == remote.OperationId)
        {
            Acknowledge(id, remote.Revision);
            return;
        }
        entry.Revision = remote.Revision;
        entry.Published = remote.Payload;
        entry.Pending = null;
        // Withdrawals take priority. Other changes require explicit conflict resolution.
        entry.Conflict = entry.Desired is not null && entry.Desired != remote.Payload;
    }

    public void ResolveWithLocal(Guid id)
    {
        var entry = Entry(id);
        entry.Conflict = false;
        entry.Pending = null;
    }

    // Comprueba cuenta y proyecto antes de recuperar operaciones; las colas antiguas dejan de compartir imágenes.
    public static SocialOutbox Load(string path, string project, Guid user)
    {
        var result = File.Exists(path)
            ? JsonSerializer.Deserialize<SocialOutbox>(File.ReadAllText(path), DataJson.Options)
            : null;
        if (result is null)
            return new() { ProjectUrl = project, UserId = user };
        if (
            result.UserId != user
            || result.ProjectUrl != project
            || result.Games is null
            || result.Games.Count > 10000
        )
            throw new InvalidDataException(
                I18n.T("La cola de publicación pertenece a otra cuenta o no es válida.")
            );
        // Old queues retain operation IDs and revisions, but never publish local images.
        foreach (var entry in result.Games.Values)
        {
            if (entry.Desired is { } desired)
                entry.Desired = desired with { CoverPath = null };
            if (entry.Published is { } published)
                entry.Published = published with { CoverPath = null };
            entry.LocalCover = null;
            if (entry.Pending is { } pending)
            {
                if (pending.Payload is { } payload)
                    pending.Payload = payload with { CoverPath = null };
                pending.LocalCover = null;
            }
        }
        return result;
    }

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(this, DataJson.Options));
        File.Move(temp, path, true);
    }
}
