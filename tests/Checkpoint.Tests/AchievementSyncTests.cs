using Checkpoint.Core;

internal static class AchievementSyncTests
{
    internal static async Task Run(Action<bool, string> check)
    {
        var requests = new AchievementRequests<int>();
        var release = new TaskCompletionSource<int>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        int calls = 0;
        using var first = new CancellationTokenSource();
        Task<int> Fetch(CancellationToken token)
        {
            Interlocked.Increment(ref calls);
            return release.Task.WaitAsync(token);
        }
        var a = requests.Run("same", Fetch, first.Token);
        var b = requests.Run("same", Fetch, CancellationToken.None);
        first.Cancel();
        try
        {
            await a;
            throw new Exception("Missing cancellation");
        }
        catch (OperationCanceledException) { }
        release.SetResult(7);
        check(
            await b == 7 && calls == 1,
            "duplicate achievement reads share I/O; cancelling one reader preserves the other"
        );
        check(
            await requests.Run("same", _ => Task.FromResult(8), CancellationToken.None) == 8,
            "completed achievement reads are refreshed, not cached indefinitely"
        );

        int active = 0,
            peak = 0;
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var many = Enumerable
            .Range(0, 10)
            .Select(id =>
                requests.Run(
                    id.ToString(),
                    async token =>
                    {
                        int count = Interlocked.Increment(ref active);
                        int previous;
                        do
                        {
                            previous = peak;
                            if (previous >= count)
                                break;
                        } while (
                            Interlocked.CompareExchange(ref peak, count, previous) != previous
                        );
                        try
                        {
                            await gate.Task.WaitAsync(token);
                            return id;
                        }
                        finally
                        {
                            Interlocked.Decrement(ref active);
                        }
                    },
                    CancellationToken.None
                )
            )
            .ToArray();
        check(peak == 3, "provider requests bound concurrency across manual and batch callers");
        gate.SetResult();
        await Task.WhenAll(many);
        check(active == 0, "provider slots are released after the complete batch");

        using var cancelAll = new CancellationTokenSource();
        var cancelled = requests.Run(
            "cancelled",
            token => Task.Delay(5000, token).ContinueWith(_ => 1, token),
            cancelAll.Token
        );
        cancelAll.Cancel();
        try
        {
            await cancelled;
        }
        catch (OperationCanceledException) { }
        check(
            await requests.Run("cancelled", _ => Task.FromResult(2), CancellationToken.None) == 2,
            "cancelled achievement requests do not poison later retries"
        );

        int started = 0,
            drained = 0;
        var fatal = new InvalidOperationException("session-expired");
        try
        {
            await AchievementReviewQueue.Run(
                Enumerable.Range(0, 50).ToArray(),
                async (id, token) =>
                {
                    Interlocked.Increment(ref started);
                    try
                    {
                        if (id == 0)
                        {
                            await Task.Delay(30, token);
                            throw fatal;
                        }
                        await Task.Delay(5000, token);
                    }
                    finally
                    {
                        Interlocked.Increment(ref drained);
                    }
                },
                CancellationToken.None,
                spacingMilliseconds: 0
            );
            throw new Exception("Missing fatal review error");
        }
        catch (InvalidOperationException error)
        {
            check(ReferenceEquals(error, fatal), "review returns the original fatal failure");
        }
        check(
            started == 3 && drained == 3,
            "fatal review failures cancel and drain active requests without flooding remaining games"
        );

        AchievementData.Validate([]);
        foreach (
            var invalid in new IReadOnlyList<Achievement>?[]
            {
                null,
                [new() { Id = "one", Name = "" }],
                [new() { Id = "same", Name = "one" }, new() { Id = "same", Name = "two" }],
            }
        )
        {
            bool rejected = false;
            try
            {
                AchievementData.Validate(invalid);
            }
            catch (InvalidDataException)
            {
                rejected = true;
            }
            check(
                rejected,
                "malformed provider results are rejected before replacing saved achievements"
            );
        }
    }
}
