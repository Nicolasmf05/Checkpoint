// Carga y prepara carátulas locales o remotas con límites de memoria y tamaño.
// La caché evita decodificar repetidamente la misma imagen al actualizar tarjetas.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Checkpoint.Core;

namespace Checkpoint.App;

public sealed class CoverCache : IDisposable
{
    private readonly string folder;
    private readonly HttpClient http = new()
    {
        Timeout = TimeSpan.FromSeconds(12),
        MaxResponseContentBufferSize = 8_000_000,
    };
    private readonly SemaphoreSlim downloads = new(4);
    private readonly ConcurrentDictionary<int, Lazy<Task<BitmapImage?>>> inFlight = new();
    private readonly ConcurrentDictionary<int, DateTimeOffset> retryAfter = new();
    private readonly Dictionary<string, BitmapImage> memory = new(StringComparer.OrdinalIgnoreCase);
    private readonly Queue<string> memoryOrder = new();
    internal const long MaxMemoryBytes = 8 * 1024 * 1024;
    private long memoryBytes;
    private volatile bool enabled = true;

    internal void SetEnabled(bool value)
    {
        lock (memory)
        {
            enabled = value;
            if (!value)
            {
                memory.Clear();
                memoryOrder.Clear();
                memoryBytes = 0;
            }
        }
    }

    internal long MemoryBytes
    {
        get
        {
            lock (memory)
                return memoryBytes;
        }
    }

    private static long ImageBytes(BitmapImage bitmap) =>
        (long)bitmap.PixelWidth
        * bitmap.PixelHeight
        * Math.Max(4, (bitmap.Format.BitsPerPixel + 7) / 8);

    internal string DirectoryPath => folder;

    public CoverCache(string directory)
    {
        folder = Path.Combine(directory, "covers");
        Directory.CreateDirectory(folder);
    }

    private static BitmapImage Read(string path)
    {
        using var stream = File.OpenRead(path);
        return Read(stream);
    }

    private static BitmapImage Read(Stream stream)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.DecodePixelWidth = 220;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        if (bitmap.PixelHeight > 4096)
            throw new InvalidDataException(
                I18n.T("La carátula tiene una proporción demasiado alta.")
            );
        bitmap.Freeze();
        return bitmap;
    }

    private BitmapImage ReadCached(string path)
    {
        lock (memory)
        {
            if (memory.TryGetValue(path, out var cached))
                return cached;
            var bitmap = Read(path);
            Remember(path, bitmap);
            return bitmap;
        }
    }

    private void Remember(string path, BitmapImage bitmap)
    {
        lock (memory)
        {
            if (!enabled || memory.ContainsKey(path))
                return;
            long bytes = ImageBytes(bitmap);
            if (bytes > MaxMemoryBytes)
                return;
            memory[path] = bitmap;
            memoryOrder.Enqueue(path);
            memoryBytes += bytes;
            while (memory.Count > 32 || memoryBytes > MaxMemoryBytes)
            {
                string oldest = memoryOrder.Dequeue();
                if (memory.Remove(oldest, out var removed))
                    memoryBytes -= ImageBytes(removed);
            }
        }
    }

    public string Import(string source)
    {
        if (new FileInfo(source).Length > BackupFiles.MaxImageBytes)
            throw new ArgumentException(I18n.T("La imagen debe ocupar menos de 8 MB."));
        return SavePrepared(Encode(Read(source)));
    }

    internal static byte[] PrepareRemote(byte[] bytes)
    {
        if (bytes.Length > 2_000_000)
            throw new InvalidDataException(I18n.T("La imagen debe ocupar menos de 8 MB."));
        using var stream = new MemoryStream(bytes, writable: false);
        return Encode(Read(stream));
    }

    internal static BitmapImage ReadPrepared(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        return Read(stream);
    }

    internal static byte[] PrepareImport(byte[] bytes)
    {
        if (bytes.Length > BackupFiles.MaxImageBytes || !BackupFiles.IsPng(bytes))
            throw new InvalidDataException(
                I18n.T("La carátula de la copia no es una imagen PNG válida.")
            );
        BackupFiles.ValidatePngImage(bytes);
        using var input = new MemoryStream(bytes, writable: false);
        return Encode(Read(input));
    }

    private static byte[] Encode(BitmapImage image)
    {
        using var output = new MemoryStream();
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        encoder.Save(output);
        if (output.Length > BackupFiles.MaxImageBytes)
            throw new InvalidDataException(I18n.T("La carátula preparada supera los 8 MB."));
        return output.ToArray();
    }

    internal string SavePrepared(byte[] bytes)
    {
        var name = "custom-" + Guid.NewGuid().ToString("N") + ".png";
        string path = Path.Combine(folder, name);
        bool created = false;
        try
        {
            using var output = new FileStream(
                path,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None
            );
            created = true;
            output.Write(bytes);
            return name;
        }
        catch
        {
            if (created && File.Exists(path))
                File.Delete(path);
            throw;
        }
    }

    internal void RemoveCreated(string name)
    {
        if (!BackupFiles.IsCustomCoverName(name))
            throw new ArgumentException(I18n.T("Nombre de carátula no válido."));
        File.Delete(Path.Combine(folder, name));
    }

    public async Task<BitmapImage?> Get(Game game, bool review = false)
    {
        if (!enabled && !review)
            return null;
        var custom = game.CustomCover;
        if (BackupFiles.IsCustomCoverName(custom))
        {
            try
            {
                return ReadCached(Path.Combine(folder, custom!));
            }
            catch (Exception ex)
                when (ex
                        is InvalidDataException
                            or IOException
                            or NotSupportedException
                            or FormatException
                ) { }
        }
        if (game.SteamAppId is not int appId)
            return null;
        if (retryAfter.TryGetValue(appId, out var retry) && retry > DateTimeOffset.UtcNow)
            return null;
        var work = inFlight.GetOrAdd(
            appId,
            id => new Lazy<Task<BitmapImage?>>(() => Download(id, review))
        );
        try
        {
            return await work.Value;
        }
        finally
        {
            inFlight.TryRemove(appId, out _);
        }
    }

    private async Task<BitmapImage?> Download(int appId, bool review)
    {
        var path = Path.Combine(folder, appId + ".jpg");
        try
        {
            if (File.Exists(path))
                return ReadCached(path);
            await downloads.WaitAsync();
            try
            {
                if (File.Exists(path))
                    return ReadCached(path);
                foreach (var asset in new[] { "library_600x900.jpg", "header.jpg" })
                {
                    if (!enabled && !review)
                        return null;
                    using var response = await http.GetAsync(
                        $"https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/{appId}/{asset}"
                    );
                    if (!response.IsSuccessStatusCode)
                        continue;
                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    if (bytes.Length > 8_000_000)
                        continue;
                    var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                    try
                    {
                        await File.WriteAllBytesAsync(temporary, bytes);
                        var image = Read(temporary);
                        File.Move(temporary, path, true);
                        Remember(path, image);
                        return image;
                    }
                    finally
                    {
                        if (File.Exists(temporary))
                            File.Delete(temporary);
                    }
                }
            }
            finally
            {
                downloads.Release();
            }
        }
        catch (Exception ex)
            when (ex
                    is HttpRequestException
                        or InvalidDataException
                        or IOException
                        or TaskCanceledException
                        or NotSupportedException
                        or FormatException
                        or ObjectDisposedException
            ) { }
        retryAfter[appId] = DateTimeOffset.UtcNow.AddMinutes(10);
        return null;
    }

    public void Dispose()
    {
        http.Dispose();
    }
}
