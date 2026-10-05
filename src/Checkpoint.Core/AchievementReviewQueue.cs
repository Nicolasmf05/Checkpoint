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
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        var token = stop.Token;
        Exception? failure = null;
        using var gate = new SemaphoreSlim(1, 1);
        var clock = System.Diagnostics.Stopwatch.StartNew();
        long nextStart = 0;
        int index = -1;
        async Task Worker()
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                int selected = Interlocked.Increment(ref index);
                if (selected >= items.Count)
                    return;
                await gate.WaitAsync(token);
                try
                {
                    int delay = (int)Math.Max(0, nextStart - clock.ElapsedMilliseconds);
                    if (delay > 0)
                        await Task.Delay(delay, token);
                    nextStart = clock.ElapsedMilliseconds + spacingMilliseconds;
                }
                finally
                {
                    gate.Release();
                }
                token.ThrowIfCancellationRequested();
                try
                {
                    await process(items[selected], token);
                }
                catch (Exception error)
                {
                    Interlocked.CompareExchange(ref failure, error, null);
                    stop.Cancel();
                    throw;
                }
            }
        }
        try
        {
            await Task.WhenAll(
                Enumerable.Range(0, Math.Min(concurrency, items.Count)).Select(_ => Worker())
            );
        }
        catch
        {
            if (failure is not null)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            throw;
        }
    }
}
