using System.Diagnostics;
using System.Text.Json;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Parity.Tests.Infrastructure;

/// <summary>
/// Waits for an animation step to reach its canonical terminal state.
/// </summary>
public static class AnimationSettleProtocol
{
    private const string Api = "window[Symbol.for('Blazix.Parity.Capture')]";

    /// <summary>
    /// Lets motion register, waits every finite capture-root animation to finish, then
    /// waits for completion callbacks and their DOM work to quiesce.
    /// </summary>
    /// <param name="page">The authoritative page whose natural lifecycle is recorded.</param>
    /// <param name="timeoutMs">The hard deadline for finite animation completion.</param>
    /// <returns>A task that completes at the canonical terminal state.</returns>
    public static async Task WaitAsync(IPage page, int timeoutMs = 30_000)
    {
        ArgumentNullException.ThrowIfNull(page);

        var stopwatch = Stopwatch.StartNew();
        await SettleProtocol.WaitAsync(page, GetRemainingTimeout(stopwatch, timeoutMs));

        var emptyScans = 0;
        while (emptyScans < 2)
        {
            var result = await page.EvaluateAsync<JsonElement>(
                $"input => {Api}.awaitFiniteAnimations(input.remaining, input.deadline)",
                new
                {
                    remaining = GetRemainingTimeout(stopwatch, timeoutMs),
                    deadline = timeoutMs
                });

            await SettleProtocol.WaitAsync(page, GetRemainingTimeout(stopwatch, timeoutMs));
            emptyScans = result.GetProperty("waited").GetInt32() == 0
                ? emptyScans + 1
                : 0;
        }
    }

    private static int GetRemainingTimeout(Stopwatch stopwatch, int timeoutMs)
    {
        var remaining = timeoutMs - (int)stopwatch.ElapsedMilliseconds;
        if (remaining <= 0)
        {
            throw new TimeoutException(
                $"Timed out after {timeoutMs}ms waiting for the animation terminal state.");
        }

        return remaining;
    }
}
