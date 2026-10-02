using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace Checkpoint.Core;

public sealed class SqliteStore : IDisposable
{
    private readonly SqliteConnection connection;
    public string DirectoryPath { get; }
    public SqliteStore(string directory)
    {
        DirectoryPath = Path.GetFullPath(directory);
        Directory.CreateDirectory(DirectoryPath);
        connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(DirectoryPath, "checkpoint.db"), Pooling = false
        }.ToString());
        connection.Open();
        using var version = connection.CreateCommand(); version.CommandText = "PRAGMA user_version";
        if (Convert.ToInt32(version.ExecuteScalar()) > 2) throw new InvalidDataException("Esta biblioteca pertenece a una versión más reciente de Checkpoint.");
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL; CREATE TABLE IF NOT EXISTS games (id TEXT PRIMARY KEY, payload TEXT NOT NULL); CREATE TABLE IF NOT EXISTS settings (id INTEGER PRIMARY KEY CHECK(id=1), payload TEXT NOT NULL); CREATE TABLE IF NOT EXISTS deleted_games (id TEXT PRIMARY KEY, payload TEXT NOT NULL, deleted_at TEXT NOT NULL); PRAGMA user_version=2;";
        cmd.ExecuteNonQuery();
    }

    public List<Game> LoadGames()
    {
        using var cmd = connection.CreateCommand(); cmd.CommandText = "SELECT payload FROM games";
        using var reader = cmd.ExecuteReader(); var games = new List<Game>();
        while (reader.Read())
        {
            var game = JsonSerializer.Deserialize<Game>(reader.GetString(0), DataJson.Options)
                ?? throw new InvalidDataException("Un juego guardado no se puede leer.");
            GameRules.Validate(game); games.Add(game);
        }
        return games;
    }

    public Settings LoadSettings()
    {
        using var cmd = connection.CreateCommand(); cmd.CommandText = "SELECT payload FROM settings WHERE id=1";
        return cmd.ExecuteScalar() is string json
            ? JsonSerializer.Deserialize<Settings>(json, DataJson.Options) ?? new() : new();
    }

    public void Save(IEnumerable<Game> games, Settings settings) => SaveState(games, settings);

    private void SaveState(IEnumerable<Game> games, Settings settings, DeletedGame? deleted = null, Guid? restored = null)
    {
        var snapshot = games.ToList();
        snapshot.ForEach(GameRules.Validate);
        using var transaction = connection.BeginTransaction();
        using var clear = connection.CreateCommand(); clear.Transaction = transaction;
        clear.CommandText = "DELETE FROM games"; clear.ExecuteNonQuery();
        foreach (var game in snapshot)
        {
            using var cmd = connection.CreateCommand(); cmd.Transaction = transaction;
            cmd.CommandText = "INSERT INTO games(id,payload) VALUES($id,$payload)";
            cmd.Parameters.AddWithValue("$id", game.Id.ToString());
            cmd.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(game, DataJson.Options));
            cmd.ExecuteNonQuery();
        }
        using var preferences = connection.CreateCommand(); preferences.Transaction = transaction;
        preferences.CommandText = "INSERT INTO settings(id,payload) VALUES(1,$payload) ON CONFLICT(id) DO UPDATE SET payload=excluded.payload";
        preferences.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(settings, DataJson.Options));
        preferences.ExecuteNonQuery();
        if (deleted is not null)
        {
            using var recovery = connection.CreateCommand(); recovery.Transaction = transaction;
            recovery.CommandText = "INSERT INTO deleted_games(id,payload,deleted_at) VALUES($id,$payload,$date); DELETE FROM deleted_games WHERE id NOT IN (SELECT id FROM deleted_games ORDER BY rowid DESC LIMIT 20);";
            recovery.Parameters.AddWithValue("$id", deleted.RecoveryId.ToString());
            recovery.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(deleted.Game, DataJson.Options));
            recovery.Parameters.AddWithValue("$date", deleted.DeletedAt.ToString("O")); recovery.ExecuteNonQuery();
        }
        if (restored is Guid recoveryId)
        {
            using var remove = connection.CreateCommand(); remove.Transaction = transaction;
            remove.CommandText = "DELETE FROM deleted_games WHERE id=$id"; remove.Parameters.AddWithValue("$id", recoveryId.ToString()); remove.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    public void Export(string path, IEnumerable<Game> games) => BackupFiles.WriteJson(path, games);

    public List<DeletedGame> LoadDeletedGames()
    {
        using var cmd = connection.CreateCommand(); cmd.CommandText = "SELECT id,payload,deleted_at FROM deleted_games ORDER BY rowid DESC LIMIT 20";
        using var reader = cmd.ExecuteReader(); var deleted = new List<DeletedGame>();
        while (reader.Read())
        {
            var game = JsonSerializer.Deserialize<Game>(reader.GetString(1), DataJson.Options) ?? throw new InvalidDataException("No se puede leer un juego eliminado.");
            GameRules.Validate(game);
            deleted.Add(new(Guid.Parse(reader.GetString(0)), game, DateTimeOffset.Parse(reader.GetString(2))));
        }
        return deleted;
    }

    public void DeleteGame(Game game, IEnumerable<Game> remaining, Settings settings)
    {
        var snapshot = remaining.ToList();
        if (snapshot.Any(g => g.Id == game.Id)) throw new ArgumentException("El juego eliminado sigue en la colección.");
        GameRules.Validate(game);
        SaveState(snapshot, settings, new(Guid.NewGuid(), game, DateTimeOffset.UtcNow));
    }

    public Game RestoreDeletedGame(Guid recoveryId, IEnumerable<Game> games, Settings settings)
    {
        var deleted = LoadDeletedGames().FirstOrDefault(d => d.RecoveryId == recoveryId)
            ?? throw new InvalidOperationException("Ese juego ya se ha recuperado o no está disponible.");
        var current = games.ToList();
        if (current.Any(g => g.Id == deleted.Game.Id || (deleted.Game.SteamAppId is not null && g.SteamAppId == deleted.Game.SteamAppId)))
            throw new InvalidOperationException("Este juego ya existe en la biblioteca. Se han conservado sus datos actuales; puedes recuperar otro juego desde Ajustes.");
        current.Add(deleted.Game); SaveState(current, settings, restored: recoveryId); return deleted.Game;
    }

    public void SaveSettings(Settings settings)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT INTO settings(id,payload) VALUES(1,$payload) ON CONFLICT(id) DO UPDATE SET payload=excluded.payload";
        cmd.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(settings, DataJson.Options)); cmd.ExecuteNonQuery();
    }

    public List<Game> ReadBackup(string path) => BackupFiles.ReadJson(path);

    public void Dispose() => connection.Dispose();
}
