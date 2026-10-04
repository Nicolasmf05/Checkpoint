// Ejecuta las pruebas del núcleo sobre modelos, persistencia, temas y reglas de colección.

using System.Text.Json;
using Checkpoint.Core;
using Microsoft.Data.Sqlite;

var root = Path.Combine(
    Directory.GetCurrentDirectory(),
    ".qa",
    "tests-" + Guid.NewGuid().ToString("N")
);
Directory.CreateDirectory(root);
int passed = 0;
void Check(bool condition, string name)
{
    if (!condition)
        throw new Exception("FAILED: " + name);
    passed++;
    Console.WriteLine("PASS " + name);
}
void Reject(Action action, string name)
{
    try
    {
        action();
    }
    catch
    {
        passed++;
        Console.WriteLine("PASS " + name);
        return;
    }
    throw new Exception("FAILED: " + name);
}
var reviewRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
int reviewActive = 0,
    reviewPeak = 0,
    reviewCompleted = 0;
var reviewTask = AchievementReviewQueue.Run(
    Enumerable.Range(0, 9).ToArray(),
    async (_, token) =>
    {
        int active = Interlocked.Increment(ref reviewActive);
        int old;
        do
        {
            old = reviewPeak;
            if (old >= active)
                break;
        } while (Interlocked.CompareExchange(ref reviewPeak, active, old) != old);
        await reviewRelease.Task.WaitAsync(token);
        Interlocked.Decrement(ref reviewActive);
        Interlocked.Increment(ref reviewCompleted);
    },
    CancellationToken.None,
    spacingMilliseconds: 0
);
await Task.Delay(30);
Check(
    reviewPeak == 3 && reviewCompleted == 0,
    "achievement review uses three bounded concurrent operations without blocking its caller"
);
reviewRelease.SetResult();
await reviewTask;
Check(
    reviewCompleted == 9 && reviewActive == 0,
    "achievement review finishes the entire queue before returning"
);
using (var stopReview = new CancellationTokenSource())
{
    int started = 0;
    var stopped = AchievementReviewQueue.Run(
        Enumerable.Range(0, 20).ToArray(),
        async (_, token) =>
        {
            Interlocked.Increment(ref started);
            await Task.Delay(5000, token);
        },
        stopReview.Token,
        spacingMilliseconds: 0
    );
    await Task.Delay(30);
    stopReview.Cancel();
    try
    {
        await stopped;
        throw new Exception("Review should cancel");
    }
    catch (OperationCanceledException) { }
    Check(
        started == 3,
        "stopping achievement review cancels active requests and prevents queued requests"
    );
}
using (var reviewStore = new SqliteStore(Path.Combine(root, "review-incremental")))
{
    var first = new Game
    {
        Title = "Review first",
        FriendsPrivate = true,
        Notes = "Keep local notes",
        AchievementOverrides = new() { ["steam:one"] = false },
    };
    var second = new Game { Title = "Review second" };
    var settings = new Settings { Language = "en" };
    reviewStore.Save([first, second], settings);
    first.Achievements =
    [
        new()
        {
            Id = "one",
            Name = "Remote achievement",
            Unlocked = true,
        },
    ];
    Check(
        reviewStore.SaveExistingGame(first)
            && reviewStore.LoadGames().Count == 2
            && reviewStore.LoadSettings().Language == "en",
        "incremental achievement save preserves other games and settings"
    );
    var restored = reviewStore.LoadGames().Single(g => g.Id == first.Id);
    Check(
        restored.Notes == first.Notes
            && restored.FriendsPrivate == true
            && restored.AchievementOverrides["steam:one"] == false,
        "incremental achievement save preserves private notes and manual completion"
    );
    reviewStore.Save([second], settings);
    Check(
        !reviewStore.SaveExistingGame(first) && reviewStore.LoadGames().Count == 1,
        "late achievement results cannot recreate deleted games"
    );
}
var batchOne = new Game
{
    Title = "Batch one",
    Tracked = false,
    FriendsPrivate = true,
    Lists = ["Source", "Other"],
    Notes = "Keep notes",
    StoryPercent = 37,
};
var batchTwo = new Game
{
    Title = "Batch two",
    Lists = ["Source"],
    Tasks = [new ChecklistItem { Title = "Keep task" }],
};
var batchGames = new List<Game> { batchOne, batchTwo };
string[] batchCatalog = ["Source", "Other", "Target"];
GameLists.Apply(
    batchGames,
    [batchOne.Id, batchTwo.Id],
    batchCatalog,
    "move",
    "custom:Source",
    "Target"
);
Check(
    batchOne.Lists.SequenceEqual(new[] { "Other", "Target" })
        && batchTwo.Lists.SequenceEqual(new[] { "Target" })
        && batchOne.Tracked
        && batchOne.FriendsPrivate == true,
    "batch move removes only its source membership and preserves privacy"
);
Check(
    batchOne.Notes == "Keep notes"
        && batchOne.StoryPercent == 37
        && batchTwo.Tasks.Single().Title == "Keep task",
    "batch movement preserves notes tasks and manual progress"
);
Check(
    GameLists.Members(batchGames, "custom:Target").Count() == 2
        && !GameLists.Visible(batchOne, "custom:Target"),
    "owner list sheets include private members while public list views exclude them"
);
GameLists.Apply(batchGames, [batchOne.Id], batchCatalog, "add", "library", "Source");
Check(batchOne.Lists.Count == 3, "adding a second membership preserves existing lists");
GameLists.Apply(batchGames, [batchOne.Id], batchCatalog, "remove", "custom:Source");
Check(
    !batchOne.Lists.Contains("Source") && batchGames.Count == 2 && batchOne.Lists.Count == 2,
    "removing a list membership retains the library game and other memberships"
);
GameLists.Apply(batchGames, [batchOne.Id, batchTwo.Id], batchCatalog, "public", "library");
Check(
    batchGames.All(g => g.Tracked && GameLists.ShouldShare(g)),
    "batch public actions track library games and make them shareable"
);
GameLists.Apply(batchGames, [batchOne.Id], batchCatalog, "move", "private", "Source");
Check(
    batchOne.Lists.Contains("Other")
        && batchOne.Lists.Contains("Target")
        && batchOne.Lists.Contains("Source"),
    "moving from the privacy view preserves existing list memberships"
);
var batchBefore = JsonSerializer.Serialize(batchGames, DataJson.Options);
Reject(
    () =>
        GameLists.Apply(batchGames, [batchOne.Id, Guid.NewGuid()], batchCatalog, "private", "all"),
    "a stale batch selection is rejected before any game changes"
);
Reject(
    () => GameLists.Apply(batchGames, [batchOne.Id], batchCatalog, "move", "all", "Missing"),
    "a stale destination list is rejected before mutation"
);
Check(
    batchBefore == JsonSerializer.Serialize(batchGames, DataJson.Options),
    "invalid batches are atomic"
);
GameLists.Apply(batchGames, [batchOne.Id], batchCatalog, "move", "library", "Source");
Check(
    batchOne.Lists.SequenceEqual(new[] { "Source" }),
    "moving from the library replaces memberships explicitly"
);
GameLists.Apply(batchGames, [batchOne.Id], batchCatalog, "remove", "all");
Check(
    !batchOne.Tracked && batchGames.Count == 2 && !GameLists.ShouldShare(batchOne),
    "removing from My list retains library data and withdraws sharing"
);
var coverGame = new Game { Title = "Cover fixture" };
Check(
    CoverSuggestions.ShouldSuggest(coverGame),
    "missing custom covers allow an initial automatic lookup"
);
coverGame.IgdbCoverSearchTitle = coverGame.Title;
CoverSuggestions.Reject(coverGame, "rejected_cover");
var restoredCoverGame = JsonSerializer.Deserialize<Game>(
    JsonSerializer.Serialize(coverGame, DataJson.Options),
    DataJson.Options
)!;
Check(
    !CoverSuggestions.ShouldSuggest(restoredCoverGame)
        && restoredCoverGame.RejectedIgdbCovers.Contains("rejected_cover"),
    "cover rejections and automatic-search dismissal survive restart serialization"
);
restoredCoverGame.Title = "Different title";
Check(
    CoverSuggestions.ShouldSuggest(restoredCoverGame)
        && restoredCoverGame.RejectedIgdbCovers.Contains("rejected_cover"),
    "renaming allows a new search while retaining rejected images"
);
restoredCoverGame.CustomCover = "custom-fixture.png";
Check(
    !CoverSuggestions.ShouldSuggest(restoredCoverGame),
    "a chosen custom cover prevents automatic replacement"
);
Reject(
    () => CoverSuggestions.Reject(coverGame, "../escape"),
    "rejected cover IDs never accept paths or URLs"
);
Check(Shortcuts.Canonical("shift+control+n") == "Ctrl+Shift+N", "shortcut canonical order");
Check(Shortcuts.Effective(null)["add"] == "Ctrl+N", "existing settings keep default shortcuts");
Reject(() => Shortcuts.Canonical("N"), "bare letters do not steal typing");
Reject(() => Shortcuts.Canonical("Ctrl+Ctrl+N"), "repeated modifiers rejected");
Reject(
    () => Shortcuts.Validate(new Dictionary<string, string> { { "add", "Ctrl+F" } }),
    "duplicate shortcuts rejected"
);
Reject(
    () => Shortcuts.Validate(new Dictionary<string, string> { { "edit", "Escape" } }),
    "Escape reserved for closing"
);
Reject(
    () => Shortcuts.Validate(new Dictionary<string, string> { { "global", "F12" } }),
    "global shortcut requires a modifier"
);
var shortcutSettings = new Settings { Shortcuts = new() { { "add", "Ctrl+Shift+N" } } };
var shortcutRoundTrip = JsonSerializer.Deserialize<Settings>(
    JsonSerializer.Serialize(shortcutSettings, DataJson.Options),
    DataJson.Options
)!;
Check(
    Shortcuts.Effective(shortcutRoundTrip.Shortcuts)["add"] == "Ctrl+Shift+N",
    "custom shortcut settings survive serialization"
);
var listed = new Game
{
    Title = "List fixture",
    FriendsPrivate = false,
    Lists = ["Backlog", "Favorites"],
};
Check(
    GameLists.ShouldShare(listed)
        && GameLists.Visible(listed, "all")
        && GameLists.Visible(listed, "custom:Backlog"),
    "tracked games are visible to friends by default and can belong to multiple lists"
);
listed.FriendsPrivate = true;
Check(
    !GameLists.ShouldShare(listed)
        && !GameLists.Visible(listed, "all")
        && !GameLists.Visible(listed, "custom:Backlog")
        && GameLists.Visible(listed, "private"),
    "private games stay only in the private view and are never selected for sharing"
);
listed.FriendsPrivate = false;
listed.Tracked = false;
Check(
    !GameLists.ShouldShare(listed) && !GameLists.Visible(listed, "all"),
    "untracked Steam library games are not shared by default"
);
GameLists.Rename([listed], "Backlog", "Next adventures");
Check(
    listed.Lists.SequenceEqual(new[] { "Next adventures", "Favorites" })
        && listed.FriendsPrivate == false,
    "renaming membership preserves game identity and privacy"
);
Reject(
    () => GameLists.ValidateName("favorites", ["Favorites"]),
    "list names reject case-insensitive duplicates"
);
Reject(
    () => GameLists.ValidateName("Privados", []),
    "reserved privacy view cannot be confused with a custom list"
);
Reject(() => GameLists.ValidateName("", []), "empty list names rejected");
var listsBackup = Path.Combine(root, "lists.json");
listed.FriendsPrivate = true;
BackupFiles.WriteJson(listsBackup, [listed], ["Next adventures", "Favorites", "Empty list"]);
var restoredLists = BackupFiles.Read(listsBackup);
Check(
    restoredLists.Games[0].FriendsPrivate == true
        && restoredLists.Games[0].Lists.Count == 2
        && restoredLists.GameLists!.Contains("Empty list"),
    "JSON backup restores privacy, multiple memberships and empty lists"
);
var game = new Game
{
    Title = "  Test game  ",
    SteamAppId = 620,
    Status = GameStatus.Playing,
    Notes = "Keep my notes",
    Favorite = true,
    Tasks = [new() { Title = "Last chapter" }],
};
GameRules.Validate(game);
Check(game.Title == "Test game" && game.Platform == "Steam", "normalization");
Check(
    game.AchievementPercent is null && !game.AllAchievements,
    "unknown achievements are not complete"
);
game.Achievements = [];
Check(
    game.AchievementPercent is null && !game.AllAchievements,
    "games without achievements are not complete"
);
game.Achievements =
[
    new()
    {
        Id = "one",
        Name = "One",
        Unlocked = true,
    },
    new() { Id = "two", Name = "Two" },
];
Check(game.AchievementPercent == 50 && game.UnlockedCount == 1, "achievement counts");
game.Achievements[1].Unlocked = true;
Check(
    game.AllAchievements && game.Status == GameStatus.Playing,
    "all achievements never finish the story"
);
GameRules.SetStatus(game, GameStatus.Finished);
Check(game.FinishedAt.HasValue, "story records completion date");
var finishedAt = game.FinishedAt;
GameRules.SetStatus(game, GameStatus.Finished);
Check(game.FinishedAt == finishedAt, "completion date is stable");
GameRules.SetStatus(game, GameStatus.Paused);
Check(game.FinishedAt is null, "reopening story clears completion date");
var library = new List<Game> { game };
int added = GameRules.MergeSteamLibrary(
    library,
    [
        new(620, "Different remote title", 900),
        new(367520, "Hollow Knight", 50),
        new(367520, "Hollow Knight", 55),
    ]
);
Check(added == 1 && library.Count == 2, "library merge deduplicates Steam IDs");
Check(
    game.Title == "Test game"
        && game.Status == GameStatus.Paused
        && game.Notes == "Keep my notes"
        && game.Favorite,
    "Steam sync preserves manual state"
);
Check(
    game.PlaytimeMinutes == 900 && library[1].PlaytimeMinutes == 55 && !library[1].Tracked,
    "imports only update Steam fields and do not flood widget"
);
Check(game.NextTask == "Last chapter", "next pending task");
game.Tasks[0].Done = true;
Check(game.NextTask == game.GoalText, "completed tasks fall back to goal");
var noGoal = new Game
{
    Title = "No goal",
    Goal = GameGoal.None,
    CustomGoal = "Kept",
    Status = GameStatus.Playing,
};
Check(
    noGoal.GoalText == ""
        && noGoal.NextTask == ""
        && SharedGamePayload.From(noGoal).GoalKind is null,
    "no goal hides the objective and publishes a nullable kind"
);
GameRules.Validate(noGoal);
Check(
    JsonSerializer.Deserialize<Game>(JsonSerializer.Serialize(noGoal))!.Goal == GameGoal.None,
    "no goal survives backup serialization without shifting legacy enum values"
);
var objective = new Game
{
    Title = "Goal",
    Goal = GameGoal.Story,
    Status = GameStatus.Finished,
};
Check(
    !objective.GoalVisible && objective.GoalText == "" && objective.Goal == GameGoal.Story,
    "completed story hides the goal without deleting its selection"
);
objective.Status = GameStatus.Playing;
Check(objective.GoalVisible, "reopening the story restores its goal");
objective.Goal = GameGoal.Achievements;
Check(objective.GoalVisible, "unknown or empty achievements never imply a completed goal");
objective.Achievements = [new() { Id = "first", Unlocked = true }, new() { Id = "second" }];
Check(objective.GoalVisible, "pending achievement keeps the goal visible");
objective.AchievementOverrides["steam:second"] = true;
Check(
    !objective.GoalVisible,
    "personal achievement completion hides the fulfilled achievement goal"
);
objective.AchievementOverrides["steam:first"] = false;
Check(objective.GoalVisible, "reopening an achievement restores its goal");
objective.Tasks = [new() { Title = "Pending task" }];
objective.Goal = GameGoal.None;
Check(objective.NextTask == "Pending task", "no goal preserves independently pending tasks");
Reject(() => GameRules.Validate(new Game { Title = "" }), "empty title rejected");
Reject(
    () => GameRules.Validate(new Game { Title = "x", SteamAppId = -1 }),
    "invalid Steam ID rejected"
);
Reject(
    () => GameRules.Validate(new Game { Title = "x", Goal = (GameGoal)999 }),
    "unknown enum rejected"
);
var a = new Game { Title = "A", SortOrder = 3 };
var hidden = new Game
{
    Title = "Hidden",
    SortOrder = 4,
    Tracked = false,
};
var b = new Game { Title = "B", SortOrder = 5 };
var c = new Game { Title = "C", SortOrder = 6 };
var favorite = new Game
{
    Title = "Favorite",
    Favorite = true,
    SortOrder = 99,
};
var order = new List<Game> { c, favorite, b, hidden, a };
Check(
    GameRules.Move(order, c.Id, a.Id, false)
        && GameRules.InDisplayOrder(order).SequenceEqual(new[] { favorite, c, a, hidden, b }),
    "move up retains hidden games and pinned favorites"
);
Check(
    GameRules.Move(order, c.Id, b.Id, true)
        && GameRules.InDisplayOrder(order).SequenceEqual(new[] { favorite, a, hidden, b, c }),
    "move down inserts after target"
);
var savedOrder = order.Select(g => g.SortOrder).ToArray();
Check(
    !GameRules.Move(order, a.Id, favorite.Id, false)
        && savedOrder.SequenceEqual(order.Select(g => g.SortOrder)),
    "cross-favorite move is rejected without mutation"
);
Check(
    !GameRules.Move(order, b.Id, b.Id, true) && !GameRules.Move(order, Guid.NewGuid(), b.Id, false),
    "self and missing moves are harmless"
);
Check(!GameRules.Move(order, c.Id, b.Id, true), "already adjacent move is a no-op");
Check(
    GameRules
        .InDisplayOrder(order)
        .Select(g => g.SortOrder)
        .SequenceEqual(Enumerable.Range(0, order.Count)) && !hidden.Tracked,
    "reordering normalizes ranks and preserves metadata"
);
using (var orderStore = new SqliteStore(Path.Combine(root, "order")))
{
    orderStore.Save(order, new Settings { GridView = true });
    Check(
        GameRules
            .InDisplayOrder(orderStore.LoadGames())
            .Select(g => g.Id)
            .SequenceEqual(GameRules.InDisplayOrder(order).Select(g => g.Id)),
        "drag order survives SQLite reload"
    );
    Check(orderStore.LoadSettings().GridView, "grid preference survives SQLite reload");
}
Check(
    !JsonSerializer.Deserialize<Settings>("{\"compact\":true}", DataJson.Options)!.GridView,
    "old compact settings retain their view"
);
using (var store = new SqliteStore(root))
{
    store.Save(library, new Settings { BackgroundOpacity = .65, PositionLocked = true });
    var loaded = store.LoadGames();
    Check(
        loaded.Count == 2 && loaded.First(g => g.Id == game.Id).AllAchievements,
        "SQLite roundtrip retains achievements"
    );
    Check(
        store.LoadSettings().BackgroundOpacity == .65 && store.LoadSettings().PositionLocked,
        "SQLite settings roundtrip"
    );
    store.SaveSettings(new Settings { Compact = true });
    Check(
        store.LoadSettings().Compact && store.LoadGames().Count == 2,
        "saving widget settings preserves games"
    );
    var injection = new Game { Title = "'); DROP TABLE games; --" };
    library.Add(injection);
    store.Save(library, new());
    Check(store.LoadGames().Count == 3, "game titles are parameterized SQL");
    Reject(() => store.Save([game, game], new()), "duplicate primary key rejects transaction");
    Check(store.LoadGames().Count == 3, "failed save rolls back all deletes");
    var export = Path.Combine(root, "backup.json");
    game.CustomCover = "\\\\remote-server\\secret.png";
    store.Export(export, library);
    var imported = store.ReadBackup(export);
    Check(
        imported.Count == 3 && imported.First(g => g.Id == game.Id).CustomCover is null,
        "backup import strips untrusted cover paths"
    );
    Check(
        !File.ReadAllText(export).Contains("steam-session")
            && !File.ReadAllText(export).Contains("token"),
        "backups contain no authentication token"
    );
    File.WriteAllText(
        export,
        JsonSerializer.Serialize(new Backup { Version = 99, Games = library }, DataJson.Options)
    );
    Reject(() => store.ReadBackup(export), "future backup version rejected");
    File.WriteAllText(
        export,
        JsonSerializer.Serialize(new Backup { Games = [game, game] }, DataJson.Options)
    );
    Reject(() => store.ReadBackup(export), "duplicate backup IDs rejected");
}
using (var database = new SqliteConnection("Data Source=" + Path.Combine(root, "checkpoint.db")))
{
    database.Open();
    using var cmd = database.CreateCommand();
    cmd.CommandText = "SELECT sqlite_version()";
    var sqliteVersion = Version.Parse((string)cmd.ExecuteScalar()!);
    Check(sqliteVersion >= new Version(3, 50, 2), "SQLite includes CVE-2025-6965 fix");
    cmd.CommandText = "PRAGMA user_version=99";
    cmd.ExecuteNonQuery();
}
BackupChecks.Run(root, Check, Reject);
await SocialChecks.Run(root, Check, Reject);
Reject(
    () =>
    {
        using var unsupported = new SqliteStore(root);
    },
    "future database version preserved"
);
foreach (var language in new[] { "es", "en" })
{
    I18n.SetLanguage(language);
    Check(
        I18n.Error(new IOException("An English operating system error"))
            == I18n.T(
                "No se pudo leer o guardar el archivo. Comprueba que esté disponible y vuelve a intentarlo."
            ),
        "system errors follow " + language
    );
    Check(
        I18n.Error(new InvalidOperationException("Un error externo desconocido"))
            == I18n.T("No se pudo completar la operación. Vuelve a intentarlo."),
        "unknown errors follow " + language
    );
    Check(
        I18n.TryTranslateKnown("El usuario o la contraseña no son correctos.", out var known)
            && known == I18n.T("El usuario o la contraseña no son correctos."),
        "known validation follows " + language
    );
    Check(
        System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == language,
        "thread culture follows " + language
    );
}
I18n.SetLanguage("en");
var sources = Directory
    .GetFiles("src/Checkpoint.App", "*.cs")
    .Concat(Directory.GetFiles("src/Checkpoint.Core", "*.cs"))
    .Where(p => !p.Contains("Tests"));
int translatedLiterals = 0;
foreach (var source in sources)
foreach (
    System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(
        File.ReadAllText(source),
        "I18n\\.T\\(\"((?:[^\"\\\\]|\\\\.)*)\"\\)"
    )
)
{
    var key = JsonSerializer.Deserialize<string>("\"" + match.Groups[1].Value + "\"")!;
    translatedLiterals++;
    if (!I18n.TryTranslateKnown(key, out _))
        throw new Exception("Missing English translation: " + key);
}
var resourceKeys = JsonSerializer.Deserialize<Dictionary<string, string>>(
    File.ReadAllText("src/Checkpoint.App/LocalizationKeys.json")
)!;
Check(
    resourceKeys.Values.All(key => I18n.TryTranslateKnown(key, out _)),
    "all XAML strings have English translations"
);
Check(translatedLiterals > 250, "all literal app and core messages have English translations");
I18n.SetLanguage("es");
Check(
    !JsonSerializer.Deserialize<Settings>("{\"Width\":480,\"MiniatureView\":false}")!.FullWindow,
    "previous settings retain the small window default"
);
using (var modes = new SqliteStore(Path.Combine(root, "window-modes")))
{
    modes.SaveSettings(
        new Settings
        {
            FullWindow = true,
            Width = 480,
            Height = 650,
            BackgroundOpacity = .57,
        }
    );
    var restored = modes.LoadSettings();
    Check(
        restored.FullWindow
            && restored.Width == 480
            && restored.Height == 650
            && restored.BackgroundOpacity == .57,
        "full window preference retains small dimensions and opacity through SQLite reload"
    );
}
Check(Themes.Id(new Settings()) == "dark", "new settings retain the default dark theme");
Check(
    Themes.Id(JsonSerializer.Deserialize<Settings>("{\"LightTheme\":true}")!) == "light",
    "legacy light preferences retain their appearance"
);
Check(
    Themes.Id(new Settings { Theme = "unknown", LightTheme = true }) == "light"
        && Themes.Id(new Settings { Theme = null! }) == "dark",
    "invalid theme identifiers safely fall back to legacy appearance"
);
Check(
    Themes.Id(new Settings { Theme = "forest", LightTheme = true }) == "forest",
    "an explicit theme takes priority over legacy appearance"
);
using (var themes = new SqliteStore(Path.Combine(root, "themes")))
{
    foreach (var id in Themes.Ids)
    {
        themes.SaveSettings(
            new Settings
            {
                Theme = id,
                LightTheme = id == "light",
                BackgroundOpacity = .63,
                MiniatureView = true,
            }
        );
        var restored = themes.LoadSettings();
        Check(
            Themes.Id(restored) == id
                && restored.BackgroundOpacity == .63
                && restored.MiniatureView,
            "theme survives SQLite reload without changing opacity or window mode: " + id
        );
    }
}
I18n.SetLanguage("en");
Check(
    Themes.Ids.Select(Themes.Name).Distinct().Count() == 22
        && Themes.Name("ocean") == "Ocean"
        && Themes.Name("contrast") == "High contrast",
    "theme labels are distinct and localized in English"
);
I18n.SetLanguage("es");
Check(
    Themes.Name("ocean") == "Océano" && Themes.Name("forest") == "Bosque",
    "theme labels are localized in Spanish"
);
using (var listsStore = new SqliteStore(Path.Combine(root, "recovered-lists")))
{
    var recoverable = new Game
    {
        Title = "Recovered list fixture",
        FriendsPrivate = true,
        Lists = ["Weekend", "Other"],
    };
    var preferences = new Settings { GameLists = ["Weekend", "Other"] };
    listsStore.DeleteGame(recoverable, [], preferences);
    preferences.GameLists = ["Renamed", "Other"];
    listsStore.SaveListChange([], preferences, "Weekend", "Renamed");
    Check(
        listsStore.LoadDeletedGames()[0].Game.Lists.SequenceEqual(new[] { "Renamed", "Other" }),
        "list rename updates deleted recovery membership atomically"
    );
    preferences.GameLists = ["Other"];
    listsStore.SaveListChange([], preferences, "Renamed", null);
    var restored = listsStore.RestoreDeletedGame(
        listsStore.LoadDeletedGames()[0].RecoveryId,
        [],
        preferences
    );
    Check(
        restored.FriendsPrivate == true && restored.Lists.SequenceEqual(new[] { "Other" }),
        "restoring a deleted game preserves privacy without resurrecting a removed list"
    );
}
var trackedAchievements = new Game
{
    Title = "Achievement fixture",
    SteamAppId = 620,
    Achievements =
    [
        new()
        {
            Id = "first",
            Name = "First",
            Unlocked = false,
        },
    ],
    RetroGameId = 1,
    RetroAchievements =
    [
        new()
        {
            Id = "first",
            Name = "Retro first",
            Unlocked = true,
        },
    ],
};
trackedAchievements.AchievementOverrides["steam:first"] = true;
Check(
    AchievementTracking.Items(trackedAchievements).Count(a => a.Completed) == 2
        && !trackedAchievements.Achievements[0].Unlocked,
    "local completion is isolated from provider data and provider IDs do not collide"
);
trackedAchievements.Achievements =
[
    new()
    {
        Id = "first",
        Name = "Updated first",
        Unlocked = false,
    },
];
Check(
    AchievementTracking.Items(trackedAchievements).First().Completed,
    "provider refresh preserves manual completion"
);
AchievementTracking.Remove(
    trackedAchievements,
    AchievementTracking.Items(trackedAchievements).First()
);
Check(
    AchievementTracking.Items(trackedAchievements).Count() == 1
        && trackedAchievements.Achievements.Count == 1,
    "removing an official achievement hides it locally without deleting provider data"
);
trackedAchievements.RemovedAchievements.Clear();
Check(
    !AchievementTracking.Items(trackedAchievements).First().Completed,
    "restoring a removed achievement uses its original provider state"
);
var manualAchievement = AchievementTracking.Add(
    trackedAchievements,
    "Personal goal",
    "Description"
);
trackedAchievements.AchievementOverrides["manual:" + manualAchievement.Id] = true;
using (var achievementsStore = new SqliteStore(Path.Combine(root, "achievement-fixture")))
{
    achievementsStore.Save([trackedAchievements], new Settings());
    var restored = achievementsStore.LoadGames().Single();
    Check(
        restored.RetroGameId == 1 && AchievementTracking.Items(restored).Last().Completed,
        "RetroAchievements mapping and manual completion survive SQLite reload"
    );
}
var achievementBackup = JsonSerializer.Deserialize<Game>(
    JsonSerializer.Serialize(trackedAchievements, DataJson.Options),
    DataJson.Options
)!;
Check(
    achievementBackup.ManualAchievements.Count == 1
        && achievementBackup.RetroAchievements!.Count == 1
        && achievementBackup.AchievementOverrides.Count == 1,
    "portable JSON retains personal achievement state"
);
AchievementTracking.Remove(
    trackedAchievements,
    AchievementTracking.Items(trackedAchievements).Last()
);
Check(
    trackedAchievements.ManualAchievements.Count == 0
        && trackedAchievements.AchievementOverrides.Count == 0,
    "deleting a manual achievement also removes its completion override"
);
Reject(
    () => AchievementTracking.Add(trackedAchievements, " ", ""),
    "manual achievements require a name"
);
Reject(
    () => GameRules.Validate(new Game { Title = "Invalid", RetroGameId = -1 }),
    "negative RetroAchievements mapping is rejected"
);
var detected = new Game { Title = "Detection", SteamAppId = 620 };
var installed = new[] { new InstalledSteamGame(620, "Portal", Path.Combine(root, "portal")) };
Check(
    GameDetection.Matches(
        detected,
        new(123, "portal", "", Path.Combine(root, "portal", "bin", "game.exe")),
        installed
    ),
    "installed Steam game is detected by executable folder"
);
Check(
    !GameDetection.Matches(
        detected,
        new(123, "portal", "", Path.Combine(root, "portal-other", "game.exe")),
        installed
    ),
    "Steam detection requires a directory boundary"
);
detected.DetectionProcess = "retroarch.exe";
detected.DetectionWindowTitle = "Super Mario";
Check(
    GameDetection.Matches(detected, new(1, "RetroArch", "RetroArch - Super Mario World", null), []),
    "emulator detection works without process path using a game-specific window title"
);
Check(
    !GameDetection.Matches(detected, new(1, "retroarch", "RetroArch - Zelda", null), []),
    "emulator detection does not confuse games using the same executable"
);
var updateDigest = new string('a', 64);
object Release(
    string version,
    bool draft = false,
    bool bad = false,
    string runtime = "win-x64",
    bool checksum = true
) =>
    new
    {
        tag_name = "v" + version,
        draft,
        prerelease = true,
        assets = new[]
        {
            new
            {
                name = $"Checkpoint-{version}-{runtime}.msi",
                browser_download_url = $"https://github.com/{(bad ? "other/Checkpoint" : "Nicolasmf05/Checkpoint")}/releases/download/v{version}/Checkpoint-{version}-{runtime}.msi",
                size = 100,
                state = "uploaded",
                digest = "sha256:" + updateDigest,
            },
            new
            {
                name = $"Checkpoint-{version}-{runtime}.msi.sha256",
                browser_download_url = $"https://github.com/Nicolasmf05/Checkpoint/releases/download/v{version}/Checkpoint-{version}-{runtime}.msi.sha256",
                size = checksum ? 100 : 0,
                state = "uploaded",
                digest = "",
            },
        },
    };
var updateJson = JsonSerializer.Serialize(
    new[]
    {
        Release("2.0.0"),
        Release("3.0.0", draft: true),
        Release("4.0.0", bad: true),
        Release("5.0.0", runtime: "win-arm64"),
        Release("6.0.0", checksum: false),
    }
);
var offeredUpdate = AppUpdates.Select(updateJson, new Version(1, 0, 0), "win-x64")!;
Check(
    offeredUpdate.Version == new Version(2, 0, 0) && offeredUpdate.Preview,
    "updater selects the newest complete compatible published release and rejects drafts or unrelated URLs"
);
Check(
    AppUpdates.Select(
        JsonSerializer.Serialize(new[] { Release("2.0.0") }),
        new Version(2, 0, 0),
        "win-x64"
    )
        is null,
    "updater never offers the same or an older version"
);
Check(
    AppUpdates.Select(updateJson, new Version(1, 0, 0), "win-arm64")?.Version
        == new Version(5, 0, 0),
    "updater matches the running architecture"
);
Check(
    AppUpdates.Checksum(updateDigest + "  " + offeredUpdate.Name, offeredUpdate) == updateDigest,
    "updater binds checksum to the exact installer name and GitHub digest"
);
Reject(
    () => AppUpdates.Checksum(updateDigest + "  other.msi", offeredUpdate),
    "updater rejects a checksum for a different filename"
);
Reject(
    () => AppUpdates.Checksum(new string('b', 64) + "  " + offeredUpdate.Name, offeredUpdate),
    "updater rejects conflicting GitHub and checksum-file digests"
);
Reject(
    () =>
        AppUpdates.Validate(
            offeredUpdate with
            {
                Installer = new Uri("https://evil.example/update.msi"),
            }
        ),
    "updater validates the installer origin again before downloading"
);
using (var updateStore = new SqliteStore(Path.Combine(root, "update-settings")))
{
    var setting = new Settings
    {
        AutomaticUpdates = false,
        LastUpdateCheck = DateTimeOffset.UtcNow,
    };
    updateStore.SaveSettings(setting);
    var loaded = updateStore.LoadSettings();
    Check(
        !loaded.AutomaticUpdates && loaded.LastUpdateCheck == setting.LastUpdateCheck,
        "update checking preferences and daily timestamp survive restart"
    );
}

var resetFixture = new Game
{
    Title = "Manual reset",
    ManualAchievements = [new() { Id = "m" }],
    Achievements = [new() { Id = "s", Unlocked = true }],
    RetroAchievements = [new() { Id = "r" }],
    RemovedAchievements = ["manual:m", "steam:s"],
    AchievementOverrides = new() { ["manual:m"] = true, ["steam:s"] = false },
};
AchievementTracking.ClearManual(resetFixture);
Check(
    resetFixture.ManualAchievements.Count == 0
        && resetFixture.Achievements!.Count == 1
        && resetFixture.RetroAchievements!.Count == 1
        && resetFixture.RemovedAchievements.SequenceEqual(new[] { "steam:s" })
        && resetFixture.AchievementOverrides.Count == 1
        && !resetFixture.AchievementOverrides["steam:s"],
    "clearing manual achievements preserves providers and their personal overrides"
);

var sharedManualGame = new Game
{
    Title = "Shared manual",
    Notes = "Private note",
    ManualAchievements =
    [
        new()
        {
            Id = "m",
            Name = "Personal challenge",
            Description = "Finish without damage",
        },
        new() { Id = "hidden", Name = "Removed" },
    ],
    RemovedAchievements = ["manual:hidden"],
    AchievementOverrides = new() { ["manual:m"] = true },
};
var sharedManual = SharedGamePayload.From(sharedManualGame);
Check(
    sharedManual.ManualAchievements.Length == 1
        && sharedManual.ManualAchievements[0].Completed
        && sharedManual.ManualAchievements[0].Description == "Finish without damage",
    "friend publication shares manual completion and descriptions, excluding removed entries"
);
Check(
    sharedManual == SharedGamePayload.From(sharedManualGame),
    "unchanged manual publications retain value equality and do not trigger repeated uploads"
);
Check(
    !System.Text.Json.JsonSerializer.Serialize(sharedManual).Contains("Private note"),
    "manual publications do not expose private game notes"
);
Check(
    (sharedManual with { ManualAchievementsJson = "invalid" }).ManualAchievements.Length == 0,
    "invalid legacy manual achievement content cannot break friends view"
);
Console.WriteLine($"{passed} checks passed. Test files: {root}");
