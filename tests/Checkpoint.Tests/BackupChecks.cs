// Regresión de copias de seguridad: integridad, límites, duplicados e importación de carátulas.

using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Checkpoint.Core;
using Microsoft.Data.Sqlite;

internal static class BackupChecks
{
    internal static void Run(
        string testRoot,
        Action<bool, string> check,
        Action<Action, string> reject
    )
    {
        string root = Path.Combine(testRoot, "backup-checks"),
            covers = Path.Combine(root, "covers");
        Directory.CreateDirectory(covers);
        byte[] png = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQIHWP4z8DwHwAFgAI/ScLbtAAAAABJRU5ErkJggg=="
        );
        string coverName = "custom-" + Guid.NewGuid().ToString("N") + ".png";
        File.WriteAllBytes(Path.Combine(covers, coverName), png);
        File.WriteAllText(Path.Combine(covers, "steam-session.dat"), "secret fixture");
        var game = new Game
        {
            Title = "Historia ñ",
            SteamAppId = 620,
            Notes = "Capítulo 日本語",
            CustomCover = coverName,
            Tasks = [new() { Title = "Final", Done = true }],
            Achievements =
            [
                new()
                {
                    Id = "one",
                    Name = "One",
                    Unlocked = true,
                },
            ],
        };
        var second = new Game { Title = "Segundo juego", CustomCover = coverName };
        string path = Path.Combine(root, "collection.checkpoint");
        check(
            BackupFiles.WriteComplete(path, [game, second], covers) == 2,
            "complete backup includes two cover associations"
        );
        var read = BackupFiles.Read(path);
        check(
            read.Games.Count == 2
                && read.Games[0].Notes == game.Notes
                && read.Games[0].Tasks[0].Done
                && read.Games[0].AllAchievements,
            "complete backup retains notes tasks and achievements"
        );
        check(
            read.CustomCovers[game.Id].SequenceEqual(png)
                && ReferenceEquals(read.CustomCovers[game.Id], read.CustomCovers[second.Id]),
            "shared covers are restored without duplicate buffers"
        );
        using (var zip = ZipFile.OpenRead(path))
        {
            check(
                zip.Entries.Count == 2 && zip.GetEntry("steam-session.dat") is null,
                "backup packages only referenced images and the library"
            );
            using var reader = new StreamReader(zip.GetEntry("library.json")!.Open());
            check(
                !reader.ReadToEnd().Contains(root),
                "portable backup contains no local filesystem paths"
            );
        }
        check(
            game.CustomCover == coverName && read.Games.All(g => g.CustomCover is null),
            "export preserves originals and import strips supplied cover paths"
        );
        string legacy = Path.Combine(root, "legacy.json");
        BackupFiles.WriteJson(legacy, [game]);
        check(
            BackupFiles.Read(legacy).Games.Single().Notes == game.Notes
                && BackupFiles.Read(legacy).CustomCovers.Count == 0,
            "legacy JSON remains readable without images"
        );
        File.WriteAllText(path, "previous backup");
        game.CustomCover = "custom-" + Guid.NewGuid().ToString("N") + ".png";
        reject(
            () => BackupFiles.WriteComplete(path, [game], covers),
            "missing cover aborts export"
        );
        check(
            File.ReadAllText(path) == "previous backup",
            "failed export preserves the existing destination"
        );
        game.CustomCover = coverName;
        File.WriteAllText(Path.Combine(covers, coverName), "invalid image");
        reject(
            () => BackupFiles.WriteComplete(path, [game], covers),
            "invalid image aborts archive writing"
        );
        check(
            File.ReadAllText(path) == "previous backup" && !Directory.GetFiles(root, "*.tmp").Any(),
            "archive write failure cleans temporary files"
        );
        File.WriteAllBytes(Path.Combine(covers, coverName), png);

        var manifest = new Backup
        {
            Version = 2,
            Games = [game],
            Covers = new() { [game.Id] = "covers/" + coverName },
        };
        void Archive(string name, Backup data, params (string Name, byte[] Bytes)[] extra)
        {
            string file = Path.Combine(root, name);
            using var output = File.Create(file);
            using var zip = new ZipArchive(output, ZipArchiveMode.Create);
            using (var entry = zip.CreateEntry("library.json").Open())
                entry.Write(JsonSerializer.SerializeToUtf8Bytes(data, DataJson.Options));
            foreach (var item in extra)
            {
                using var entry = zip.CreateEntry(item.Name).Open();
                entry.Write(item.Bytes);
            }
        }
        void RejectArchive(string name, string assertion) =>
            reject(() => BackupFiles.Read(Path.Combine(root, name)), assertion);
        Archive("traversal.zip", manifest, ("../../escape.png", png));
        RejectArchive("traversal.zip", "archive traversal paths are rejected");
        Archive("absolute.zip", manifest, ("C:\\secret.png", png));
        RejectArchive("absolute.zip", "absolute Windows archive paths are rejected");
        Archive("duplicates.zip", manifest, ("library.json", png));
        RejectArchive("duplicates.zip", "duplicate archive names are rejected");
        Archive("missing.zip", manifest);
        RejectArchive("missing.zip", "missing referenced cover is rejected");
        Archive(
            "extra.zip",
            new Backup { Version = 2, Games = [game] },
            ("covers/" + coverName, png)
        );
        RejectArchive("extra.zip", "unreferenced cover is rejected");
        Archive("future.zip", new Backup { Version = 99, Games = [game] });
        RejectArchive("future.zip", "future archive version is rejected");
        Archive("null-games.zip", new Backup { Version = 2, Games = null! });
        RejectArchive("null-games.zip", "null archive collection is rejected");
        Archive(
            "wrong-owner.zip",
            new Backup
            {
                Version = 2,
                Games = [game],
                Covers = new() { [Guid.NewGuid()] = "covers/" + coverName },
            },
            ("covers/" + coverName, png)
        );
        RejectArchive("wrong-owner.zip", "cover cannot reference a game outside the archive");
        Archive(
            "oversized.zip",
            manifest,
            ("covers/" + coverName, new byte[BackupFiles.MaxImageBytes + 1])
        );
        RejectArchive("oversized.zip", "oversized expanded image is rejected before reading");
        byte[] hugePixels = png.ToArray();
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(
            hugePixels.AsSpan(16, 4),
            100000
        );
        Archive("huge-pixels.zip", manifest, ("covers/" + coverName, hugePixels));
        RejectArchive("huge-pixels.zip", "oversized PNG dimensions are rejected before decoding");
        byte[] tallPixels = png.ToArray();
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(tallPixels.AsSpan(20, 4), 4096);
        Archive("tall-pixels.zip", manifest, ("covers/" + coverName, tallPixels));
        RejectArchive("tall-pixels.zip", "extreme PNG proportions cannot inflate thumbnail memory");
        Archive("wrong-size.zip", manifest, ("covers/" + coverName, png));
        PatchLengths(Path.Combine(root, "wrong-size.zip"), _ => true, 1);
        RejectArchive(
            "wrong-size.zip",
            "forged archive size cannot bypass decompression accounting"
        );
        var many = Enumerable.Range(0, 26).Select(i => new Game { Title = "G" + i }).ToList();
        var manyCovers = many.ToDictionary(
            g => g.Id,
            g => "covers/custom-" + g.Id.ToString("N") + ".png"
        );
        Archive(
            "expanded-budget.zip",
            new Backup
            {
                Version = 2,
                Games = many,
                Covers = manyCovers,
            },
            manyCovers.Values.Select(n => (n, png)).ToArray()
        );
        PatchLengths(
            Path.Combine(root, "expanded-budget.zip"),
            n => n.StartsWith("covers/"),
            BackupFiles.MaxImageBytes
        );
        RejectArchive("expanded-budget.zip", "total expanded archive budget is enforced");
        check(
            !File.Exists(Path.Combine(testRoot, "escape.png")),
            "reading archives never extracts attacker paths"
        );

        string databaseRoot = Path.Combine(root, "recovery");
        Directory.CreateDirectory(databaseRoot);
        using (
            var old = new SqliteConnection(
                "Data Source=" + Path.Combine(databaseRoot, "checkpoint.db")
            )
        )
        {
            old.Open();
            using var command = old.CreateCommand();
            command.CommandText =
                "CREATE TABLE games(id TEXT PRIMARY KEY,payload TEXT NOT NULL); CREATE TABLE settings(id INTEGER PRIMARY KEY,payload TEXT NOT NULL); INSERT INTO games VALUES($id,$game); INSERT INTO settings VALUES(1,$settings); PRAGMA user_version=1;";
            command.Parameters.AddWithValue("$id", game.Id.ToString());
            command.Parameters.AddWithValue(
                "$game",
                JsonSerializer.Serialize(game, DataJson.Options)
            );
            command.Parameters.AddWithValue(
                "$settings",
                JsonSerializer.Serialize(
                    new Settings { GridView = true, BackgroundOpacity = .6 },
                    DataJson.Options
                )
            );
            command.ExecuteNonQuery();
        }
        Guid recoveryId;
        using (var store = new SqliteStore(databaseRoot))
        {
            check(
                store.LoadGames().Single().Notes == game.Notes
                    && store.LoadSettings().GridView
                    && store.LoadDeletedGames().Count == 0,
                "schema upgrade retains existing library and preferences"
            );
            store.DeleteGame(game, [], new());
            check(
                store.LoadGames().Count == 0 && store.LoadDeletedGames().Count == 1,
                "deletion records a recovery entry atomically"
            );
            recoveryId = store.LoadDeletedGames()[0].RecoveryId;
            store.Save([], new());
            check(store.LoadDeletedGames().Count == 1, "ordinary saves preserve recovery history");
        }
        using (var store = new SqliteStore(databaseRoot))
        {
            var restored = store.RestoreDeletedGame(recoveryId, [], new());
            check(
                restored.Notes == game.Notes
                    && restored.Tasks[0].Done
                    && restored.CustomCover == coverName
                    && restored.AllAchievements,
                "recovery after restart retains complete game data"
            );
            check(
                store.LoadGames().Count == 1 && store.LoadDeletedGames().Count == 0,
                "restoration consumes only its recovery entry"
            );
            reject(
                () => store.RestoreDeletedGame(recoveryId, [restored], new()),
                "already restored entry cannot duplicate a game"
            );
            store.DeleteGame(restored, [], new());
            var entry = store.LoadDeletedGames()[0];
            var conflict = new Game
            {
                Title = "Existing Steam entry",
                SteamAppId = game.SteamAppId,
                Notes = "Current notes",
            };
            store.Save([conflict], new());
            reject(
                () => store.RestoreDeletedGame(entry.RecoveryId, [conflict], new()),
                "Steam duplicate blocks recovery without overwrite"
            );
            check(
                store.LoadGames().Single().Notes == "Current notes"
                    && store.LoadDeletedGames().Count == 1,
                "recovery conflict preserves current data and history"
            );
            reject(
                () => store.DeleteGame(game, [conflict, conflict], new()),
                "failed deletion rejects an invalid remaining collection"
            );
            check(
                store.LoadGames().Count == 1 && store.LoadDeletedGames().Count == 1,
                "failed deletion rolls back collection and history"
            );
            var other = new Game { Title = "Other" };
            reject(
                () => store.RestoreDeletedGame(entry.RecoveryId, [other, other], new()),
                "failed restoration rolls back its transaction"
            );
            check(
                store.LoadGames().Single().Id == conflict.Id && store.LoadDeletedGames().Count == 1,
                "failed restore leaves the recovery entry available"
            );
            store.Save([], new());
            for (int i = 0; i < 21; i++)
                store.DeleteGame(new Game { Title = "Deleted " + i }, [], new());
            var history = store.LoadDeletedGames();
            check(
                history.Count == 20
                    && history[0].Game.Title == "Deleted 20"
                    && history[^1].Game.Title == "Deleted 1",
                "recovery history retains the latest twenty entries in order"
            );
        }
        using (
            var connection = new SqliteConnection(
                "Data Source=" + Path.Combine(databaseRoot, "checkpoint.db")
            )
        )
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM deleted_games";
            check(
                Convert.ToInt32(command.ExecuteScalar()) == 20,
                "history retention limit applies to persisted rows"
            );
        }
    }

    private static void PatchLengths(string path, Func<string, bool> select, int length)
    {
        byte[] bytes = File.ReadAllBytes(path);
        for (int i = 0; i + 46 <= bytes.Length; i++)
        {
            if (bytes[i] != 0x50 || bytes[i + 1] != 0x4b || bytes[i + 2] != 1 || bytes[i + 3] != 2)
                continue;
            int nameLength = BitConverter.ToUInt16(bytes, i + 28);
            string name = Encoding.UTF8.GetString(bytes, i + 46, nameLength);
            if (select(name))
                BitConverter.GetBytes(length).CopyTo(bytes, i + 24);
            i += 45 + nameLength;
        }
        File.WriteAllBytes(path, bytes);
    }
}
