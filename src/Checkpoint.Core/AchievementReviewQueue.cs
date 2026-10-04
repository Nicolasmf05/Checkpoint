// Coordina el repaso de logros con concurrencia limitada y separación entre solicitudes.
// Las operaciones reciben cancelación y conservan el contexto de sincronización del llamador.

namespace Checkpoint.Core;

// Bounded asynchronous I/O; callbacks keep the caller's UI synchronization context.
public static class AchievementReviewQueue
{
    // El semáforo serializa el inicio de solicitudes; el trabajo en curso puede solaparse hasta el límite indicado.
    public static async Task Run<T>(
        IReadOnlyList<T> items,
        Func<T, CancellationToken, Task> process,
        CancellationToken cancellation,
        int concurrency = 3,
        int spacingMilliseconds = 750
    )
    {
        if (concurrency < 1 || concurrency > 3 || spacingMilliseconds < 0)
            throw new ArgumentOutOfRangeException(nameof(concurrency));
        using var gate = new SemaphoreSlim(1, 1);
        var clock = System.Diagnostics.Stopwatch.StartNew();
        long nextStart = 0;
        int index = -1;
        async Task Worker()
        {
            while (true)
            {
                cancellation.ThrowIfCancellationRequested();
                int selected = Interlocked.Increment(ref index);
                if (selected >= items.Count)
                    return;
                await gate.WaitAsync(cancellation);
                try
                {
                    int delay = (int)Math.Max(0, nextStart - clock.ElapsedMilliseconds);
                    if (delay > 0)
                        await Task.Delay(delay, cancellation);
                    nextStart = clock.ElapsedMilliseconds + spacingMilliseconds;
                }
                finally
                {
                    gate.Release();
                }
                cancellation.ThrowIfCancellationRequested();
                await process(items[selected], cancellation);
            }
        }
        await Task.WhenAll(
            Enumerable.Range(0, Math.Min(concurrency, items.Count)).Select(_ => Worker())
        );
    }
}
