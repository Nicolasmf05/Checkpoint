using Checkpoint.Core;
using Microsoft.Data.Sqlite;
using System.Text.Json;

var root = Path.Combine(Directory.GetCurrentDirectory(), ".qa", "tests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
int passed = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception("FAILED: " + name); passed++; Console.WriteLine("PASS " + name); }
void Reject(Action action, string name) { try { action(); } catch { passed++; Console.WriteLine("PASS " + name); return; } throw new Exception("FAILED: " + name); }
var game = new Game { Title = "  Test game  ", SteamAppId = 620, Status = GameStatus.Playing,
    Notes = "Keep my notes", Favorite = true, Tasks = [new() { Title = "Last chapter" }] };
GameRules.Validate(game);
Check(game.Title == "Test game" && game.Platform == "Steam", "normalization");
Check(game.AchievementPercent is null && !game.AllAchievements, "unknown achievements are not complete");
game.Achievements = [];
Check(game.AchievementPercent is null && !game.AllAchievements, "games without achievements are not complete");
game.Achievements = [new() { Id = "one", Name = "One", Unlocked = true }, new() { Id = "two", Name = "Two" }];
Check(game.AchievementPercent == 50 && game.UnlockedCount == 1, "achievement counts");
game.Achievements[1].Unlocked = true;
Check(game.AllAchievements && game.Status == GameStatus.Playing, "all achievements never finish the story");
GameRules.SetStatus(game, GameStatus.Finished);
Check(game.FinishedAt.HasValue, "story records completion date");
var finishedAt = game.FinishedAt; GameRules.SetStatus(game, GameStatus.Finished);
Check(game.FinishedAt == finishedAt, "completion date is stable");
GameRules.SetStatus(game, GameStatus.Paused);
Check(game.FinishedAt is null, "reopening story clears completion date");
var library = new List<Game> { game };
int added = GameRules.MergeSteamLibrary(library, [new(620, "Different remote title", 900), new(367520, "Hollow Knight", 50), new(367520, "Hollow Knight", 55)]);
Check(added == 1 && library.Count == 2, "library merge deduplicates Steam IDs");
Check(game.Title == "Test game" && game.Status == GameStatus.Paused && game.Notes == "Keep my notes" && game.Favorite, "Steam sync preserves manual state");
Check(game.PlaytimeMinutes == 900 && library[1].PlaytimeMinutes == 55 && !library[1].Tracked, "imports only update Steam fields and do not flood widget");
Check(game.NextTask == "Last chapter", "next pending task"); game.Tasks[0].Done = true;
Check(game.NextTask == game.GoalText, "completed tasks fall back to goal");
Reject(() => GameRules.Validate(new Game { Title = "" }), "empty title rejected");
Reject(() => GameRules.Validate(new Game { Title = "x", SteamAppId = -1 }), "invalid Steam ID rejected");
Reject(() => GameRules.Validate(new Game { Title = "x", Goal = (GameGoal)999 }), "unknown enum rejected");
var a = new Game { Title = "A", SortOrder = 3 };
var hidden = new Game { Title = "Hidden", SortOrder = 4, Tracked = false };
var b = new Game { Title = "B", SortOrder = 5 };
var c = new Game { Title = "C", SortOrder = 6 };
var favorite = new Game { Title = "Favorite", Favorite = true, SortOrder = 99 };
var order = new List<Game> { c, favorite, b, hidden, a };
Check(GameRules.Move(order, c.Id, a.Id, false) && GameRules.InDisplayOrder(order).SequenceEqual(new[] { favorite, c, a, hidden, b }), "move up retains hidden games and pinned favorites");
Check(GameRules.Move(order, c.Id, b.Id, true) && GameRules.InDisplayOrder(order).SequenceEqual(new[] { favorite, a, hidden, b, c }), "move down inserts after target");
var savedOrder = order.Select(g => g.SortOrder).ToArray();
Check(!GameRules.Move(order, a.Id, favorite.Id, false) && savedOrder.SequenceEqual(order.Select(g => g.SortOrder)), "cross-favorite move is rejected without mutation");
Check(!GameRules.Move(order, b.Id, b.Id, true) && !GameRules.Move(order, Guid.NewGuid(), b.Id, false), "self and missing moves are harmless");
Check(!GameRules.Move(order, c.Id, b.Id, true), "already adjacent move is a no-op");
Check(GameRules.InDisplayOrder(order).Select(g => g.SortOrder).SequenceEqual(Enumerable.Range(0, order.Count)) && !hidden.Tracked, "reordering normalizes ranks and preserves metadata");
using (var orderStore = new SqliteStore(Path.Combine(root, "order")))
{
    orderStore.Save(order, new Settings { GridView = true });
    Check(GameRules.InDisplayOrder(orderStore.LoadGames()).Select(g => g.Id).SequenceEqual(GameRules.InDisplayOrder(order).Select(g => g.Id)), "drag order survives SQLite reload");
    Check(orderStore.LoadSettings().GridView, "grid preference survives SQLite reload");
}
Check(!JsonSerializer.Deserialize<Settings>("{\"compact\":true}", DataJson.Options)!.GridView, "old compact settings retain their view");
using (var store = new SqliteStore(root))
{
    store.Save(library, new Settings { BackgroundOpacity = .65, PositionLocked = true });
    var loaded = store.LoadGames();
    Check(loaded.Count == 2 && loaded.First(g => g.Id == game.Id).AllAchievements, "SQLite roundtrip retains achievements");
    Check(store.LoadSettings().BackgroundOpacity == .65 && store.LoadSettings().PositionLocked, "SQLite settings roundtrip");
    store.SaveSettings(new Settings { Compact = true });
    Check(store.LoadSettings().Compact && store.LoadGames().Count == 2, "saving widget settings preserves games");
    var injection = new Game { Title = "'); DROP TABLE games; --" }; library.Add(injection); store.Save(library, new());
    Check(store.LoadGames().Count == 3, "game titles are parameterized SQL");
    Reject(() => store.Save([game, game], new()), "duplicate primary key rejects transaction");
    Check(store.LoadGames().Count == 3, "failed save rolls back all deletes");
    var export = Path.Combine(root, "backup.json"); game.CustomCover = "\\\\remote-server\\secret.png"; store.Export(export, library);
    var imported = store.ReadBackup(export);
    Check(imported.Count == 3 && imported.First(g => g.Id == game.Id).CustomCover is null, "backup import strips untrusted cover paths");
    Check(!File.ReadAllText(export).Contains("steam-session") && !File.ReadAllText(export).Contains("token"), "backups contain no authentication token");
    File.WriteAllText(export, JsonSerializer.Serialize(new Backup { Version = 99, Games = library }, DataJson.Options));
    Reject(() => store.ReadBackup(export), "future backup version rejected");
    File.WriteAllText(export, JsonSerializer.Serialize(new Backup { Games = [game, game] }, DataJson.Options));
    Reject(() => store.ReadBackup(export), "duplicate backup IDs rejected");
}
using (var database = new SqliteConnection("Data Source=" + Path.Combine(root, "checkpoint.db")))
{
    database.Open(); using var cmd = database.CreateCommand(); cmd.CommandText = "SELECT sqlite_version()";
    var sqliteVersion = Version.Parse((string)cmd.ExecuteScalar()!);
    Check(sqliteVersion >= new Version(3, 50, 2), "SQLite includes CVE-2025-6965 fix");
    cmd.CommandText = "PRAGMA user_version=99"; cmd.ExecuteNonQuery();
}
BackupChecks.Run(root, Check, Reject);
await SocialChecks.Run(root, Check, Reject);
Reject(() => { using var unsupported = new SqliteStore(root); }, "future database version preserved");
Console.WriteLine($"{passed} checks passed. Test files: {root}");
