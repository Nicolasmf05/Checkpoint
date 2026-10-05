// Comparte consultas activas sin compartir la cancelación de sus consumidores.
namespace Checkpoint.Core;

public sealed class AchievementRequests<T>
{
    private sealed class Entry
    {
        internal readonly CancellationTokenSource Cancellation = new();
        internal Task<T> Task = null!;
        internal int Readers;
    }

    private readonly Dictionary<string, Entry> active = [];
    private readonly SemaphoreSlim slots = new(3, 3);

    public async Task<T> Run(
        string key,
        Func<CancellationToken, Task<T>> request,
        CancellationToken cancellation
    )
    {
        cancellation.ThrowIfCancellationRequested();
        Entry entry;
        lock (active)
        {
            if (!active.TryGetValue(key, out entry!))
            {
                entry = new Entry();
                active.Add(key, entry);
                entry.Task = Execute(request, entry.Cancellation.Token);
            }
            entry.Readers++;
        }
        try
        {
            return await entry.Task.WaitAsync(cancellation);
        }
        finally
        {
            lock (active)
            {
                if (--entry.Readers == 0)
                {
                    if (active.TryGetValue(key, out var current) && current == entry)
                        active.Remove(key);
                    entry.Cancellation.Cancel();
                    // Observe failures even if every consumer cancelled before completion.
                    _ = entry.Task.ContinueWith(
                        task =>
                        {
                            _ = task.Exception;
                            entry.Cancellation.Dispose();
                        },
                        TaskScheduler.Default
                    );
                }
            }
        }
    }

    private async Task<T> Execute(
        Func<CancellationToken, Task<T>> request,
        CancellationToken cancellation
    )
    {
        await slots.WaitAsync(cancellation);
        try
        {
            return await request(cancellation);
        }
        finally
        {
            slots.Release();
        }
    }
}

public static class AchievementData
{
    public static void Validate(IReadOnlyList<Achievement>? items)
    {
        if (items is null || items.Count > 10000)
            throw new InvalidDataException(I18n.T("Los logros guardados no son válidos."));
        HashSet<string> ids = new(StringComparer.Ordinal);
        foreach (var item in items)
            if (
                item is null
                || string.IsNullOrWhiteSpace(item.Id)
                || item.Id.Length > 250
                || string.IsNullOrWhiteSpace(item.Name)
                || item.Name.Length > 250
                || item.Description is null
                || item.Description.Length > 2000
                || !ids.Add(item.Id)
            )
                throw new InvalidDataException(I18n.T("Los logros guardados no son válidos."));
    }
}
