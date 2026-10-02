using System.IO.Compression;
using System.Buffers.Binary;
using System.Text.Json;

namespace Checkpoint.Core;

public sealed record BackupContents(List<Game> Games, IReadOnlyDictionary<Guid, byte[]> CustomCovers);

public static class BackupFiles
{
    public const int MaxGames = 10000;
    public const int MaxImageBytes = 8_000_000;
    public const int MaxJsonBytes = 25_000_000;
    public const long MaxExpandedBytes = 200_000_000;
    public const long MaxArchiveBytes = 250_000_000;
    private const string ManifestName = "library.json";

    public static bool IsCustomCoverName(string? name) => name is { Length: 43 } &&
        name.StartsWith("custom-", StringComparison.Ordinal) && name.EndsWith(".png", StringComparison.Ordinal) &&
        Guid.TryParseExact(name.AsSpan(7, 32), "N", out _);

    public static bool IsPng(ReadOnlySpan<byte> bytes) => bytes.Length >= 8 &&
        bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });

    public static void ValidatePngImage(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 33 || !IsPng(bytes) || BinaryPrimitives.ReadUInt32BigEndian(bytes[8..12]) != 13 || !bytes[12..16].SequenceEqual("IHDR"u8))
            throw new InvalidDataException("Una carátula no tiene una cabecera PNG válida.");
        uint width = BinaryPrimitives.ReadUInt32BigEndian(bytes[16..20]);
        uint height = BinaryPrimitives.ReadUInt32BigEndian(bytes[20..24]);
        if (width == 0 || height == 0 || width > 4096 || height > 4096 || (ulong)width * height > 12_000_000)
            throw new InvalidDataException("Una carátula supera los 4.096 píxeles por lado o los 12 millones de píxeles.");
        if ((ulong)height * 220 > (ulong)width * 4096)
            throw new InvalidDataException("Una carátula tiene una proporción demasiado alta.");
    }

    internal static void ValidateGames(List<Game>? games)
    {
        if (games is null || games.Count > MaxGames) throw new InvalidDataException("La copia no tiene una colección válida o supera los 10.000 juegos.");
        var ids = new HashSet<Guid>(); var appIds = new HashSet<int>();
        foreach (var game in games)
        {
            if (game is null || game.Id == Guid.Empty) throw new InvalidDataException("La copia contiene un juego sin identificador válido.");
            GameRules.Validate(game);
            if (!ids.Add(game.Id) || (game.SteamAppId is int id && !appIds.Add(id)))
                throw new InvalidDataException("La copia contiene juegos duplicados.");
        }
    }

    public static List<Game> ReadJson(string path)
    {
        using var file = File.OpenRead(path);
        var backup = JsonSerializer.Deserialize<Backup>(ReadLimited(file, MaxJsonBytes), DataJson.Options)
            ?? throw new InvalidDataException("Copia no válida.");
        if (backup.Version != 1) throw new InvalidDataException("Versión de copia JSON no compatible.");
        ValidateGames(backup.Games);
        foreach (var game in backup.Games) game.CustomCover = null;
        return backup.Games;
    }

    public static void WriteJson(string path, IEnumerable<Game> games)
    {
        var snapshot = Snapshot(games);
        foreach (var game in snapshot) game.CustomCover = null;
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(new Backup { Games = snapshot }, DataJson.Options);
        if (bytes.Length > MaxJsonBytes) throw new InvalidDataException("La copia JSON supera los 25 MB.");
        WriteAtomic(path, stream => stream.Write(bytes));
    }

    public static int WriteComplete(string path, IEnumerable<Game> games, string coversDirectory)
    {
        var snapshot = Snapshot(games);
        var manifest = new Backup { Version = 2, Games = snapshot };
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        long total = 0;
        foreach (var game in snapshot)
        {
            if (game.CustomCover is not null)
            {
                if (!IsCustomCoverName(game.CustomCover)) throw new InvalidDataException($"La carátula personalizada de «{game.Title}» no es válida.");
                string entry = "covers/" + game.CustomCover;
                string file = Path.Combine(coversDirectory, game.CustomCover);
                if (!files.ContainsKey(entry))
                {
                    var info = new FileInfo(file);
                    if (!info.Exists) throw new FileNotFoundException($"Falta la carátula personalizada de «{game.Title}». Vuelve a elegirla antes de exportar.");
                    if (info.Length > MaxImageBytes) throw new InvalidDataException("Una carátula supera los 8 MB.");
                    total += info.Length; files.Add(entry, file);
                }
                manifest.Covers[game.Id] = entry;
            }
            // Archive references are explicit; no local paths enter the manifest.
            game.CustomCover = null;
        }
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(manifest, DataJson.Options);
        if (json.Length > MaxJsonBytes || total + json.Length > MaxExpandedBytes)
            throw new InvalidDataException("La copia supera el límite de 200 MB o su colección supera los 25 MB.");
        WriteAtomic(path, output =>
        {
            using var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);
            using (var entry = archive.CreateEntry(ManifestName).Open()) entry.Write(json);
            foreach (var (name, file) in files)
            {
                using var input = File.OpenRead(file);
                var bytes = ReadLimited(input, MaxImageBytes);
                ValidatePngImage(bytes);
                using var entry = archive.CreateEntry(name, CompressionLevel.Optimal).Open(); entry.Write(bytes);
            }
        });
        return manifest.Covers.Count;
    }

    public static BackupContents Read(string path)
    {
        using var file = File.OpenRead(path);
        Span<byte> signature = stackalloc byte[4];
        int read = file.Read(signature); file.Position = 0;
        if (read < 4 || signature[0] != 'P' || signature[1] != 'K')
            return new(ReadJson(path), new Dictionary<Guid, byte[]>());
        if (file.Length > MaxArchiveBytes) throw new InvalidDataException("La copia comprimida supera los 250 MB.");
        using var archive = new ZipArchive(file, ZipArchiveMode.Read);
        if (archive.Entries.Count > MaxGames + 2) throw new InvalidDataException("La copia tiene demasiados archivos.");
        var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.Ordinal);
        long total = 0;
        foreach (var entry in archive.Entries)
        {
            if (entry.FullName == "covers/" && entry.Length == 0) continue;
            bool cover = entry.FullName.StartsWith("covers/", StringComparison.Ordinal) && IsCustomCoverName(entry.FullName[7..]);
            if ((entry.FullName != ManifestName && !cover) || !entries.TryAdd(entry.FullName, entry))
                throw new InvalidDataException("La copia contiene rutas, archivos duplicados o archivos no permitidos.");
            if (entry.Length > (cover ? MaxImageBytes : MaxJsonBytes)) throw new InvalidDataException("Un archivo de la copia supera su límite de tamaño.");
            total += entry.Length;
            if (total > MaxExpandedBytes) throw new InvalidDataException("La copia descomprimida supera los 200 MB.");
        }
        if (!entries.TryGetValue(ManifestName, out var jsonEntry)) throw new InvalidDataException("Falta la colección en la copia.");
        Backup manifest;
        manifest = JsonSerializer.Deserialize<Backup>(ReadEntry(jsonEntry, MaxJsonBytes), DataJson.Options)
            ?? throw new InvalidDataException("Copia no válida.");
        if (manifest.Version != 2 || manifest.Covers is null) throw new InvalidDataException("Versión de copia completa no compatible.");
        ValidateGames(manifest.Games);
        var gameIds = manifest.Games.Select(g => g.Id).ToHashSet();
        if (manifest.Covers.Count > gameIds.Count) throw new InvalidDataException("La copia contiene demasiadas referencias de carátulas.");
        var referenced = new HashSet<string>(StringComparer.Ordinal) { ManifestName };
        foreach (var (gameId, name) in manifest.Covers)
        {
            if (!gameIds.Contains(gameId) || name is null || !name.StartsWith("covers/", StringComparison.Ordinal) ||
                !IsCustomCoverName(name[7..]) || !entries.ContainsKey(name))
                throw new InvalidDataException("La copia contiene una referencia de carátula no válida.");
            referenced.Add(name);
        }
        if (referenced.Count != entries.Count) throw new InvalidDataException("La copia contiene carátulas que no pertenecen a la colección.");
        var images = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (string name in referenced.Where(n => n != ManifestName))
        {
            var bytes = ReadEntry(entries[name], MaxImageBytes);
            ValidatePngImage(bytes);
            images.Add(name, bytes);
        }
        foreach (var game in manifest.Games) game.CustomCover = null;
        return new(manifest.Games, manifest.Covers.ToDictionary(pair => pair.Key, pair => images[pair.Value]));
    }

    private static List<Game> Snapshot(IEnumerable<Game> games)
    {
        var snapshot = JsonSerializer.Deserialize<List<Game>>(JsonSerializer.Serialize(games, DataJson.Options), DataJson.Options)!;
        ValidateGames(snapshot); return snapshot;
    }
    private static byte[] ReadLimited(Stream input, int maximum)
    {
        if (input.CanSeek && input.Length > maximum) throw new InvalidDataException("Un archivo de la copia supera su límite de tamaño.");
        using var output = new MemoryStream(); var buffer = new byte[65536]; int count;
        while ((count = input.Read(buffer)) > 0)
        {
            if (output.Length + count > maximum) throw new InvalidDataException("Un archivo de la copia supera su límite al descomprimirlo.");
            output.Write(buffer, 0, count);
        }
        return output.ToArray();
    }
    private static byte[] ReadEntry(ZipArchiveEntry entry, int maximum)
    {
        using var input = entry.Open(); var bytes = ReadLimited(input, maximum);
        if (bytes.LongLength != entry.Length) throw new InvalidDataException("El tamaño real de un archivo no coincide con la copia.");
        return bytes;
    }
    private static void WriteAtomic(string path, Action<Stream> write)
    {
        string target = Path.GetFullPath(path);
        string temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { write(output); output.Flush(true); }
            File.Move(temporary, target, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
