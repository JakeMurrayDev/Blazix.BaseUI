using Microsoft.Playwright;

namespace Blazix.BaseUI.Parity.Tests.Infrastructure;

/// <summary>
/// Waits for a fixture page to reach a stable, capture-ready state.
/// </summary>
public static class SettleProtocol
{
    /// <summary>
    /// Races <c>document.fonts.ready</c> against a self-armed timer so a promise that
    /// never settles rejects with a diagnostic rather than hanging until an outer timeout.
    /// </summary>
    private const string FontsScript = """
        (timeoutMs) => new Promise((resolve, reject) => {
          const deadline = setTimeout(() => reject(new Error(
            `Timed out after ${timeoutMs}ms waiting for document.fonts.ready.`)), timeoutMs);
          document.fonts.ready.then(
            () => { clearTimeout(deadline); resolve(); },
            (error) => { clearTimeout(deadline); reject(error); });
        })
        """;

    /// <summary>
    /// Waits for two consecutive mutation-free frames during which no Blazix portal is
    /// mid-mount.
    /// </summary>
    /// <remarks>
    /// The portal gate is what stops a popup step from being captured against a DOM the
    /// popup has not reached yet. Blazix's <c>Portal</c> renders its container inline and
    /// only moves it to the target from <c>OnAfterRenderAsync</c>, behind a lazy JS module
    /// import and an interop round trip. Nothing mutates while that is in flight, so the
    /// quiet frames elapse and the capture records a missing popup — a harness artifact
    /// that looks exactly like a component difference.
    /// <para>
    /// The wait is on the component's own state rather than on a delay: <c>Portal.razor</c>
    /// writes <c>display: none</c> inline while the move is pending, and the same JS that
    /// performs the move removes the declaration, so the flag is set for exactly the
    /// window that must be waited out. Marker attributes with no inline
    /// <c>display: none</c> are deliberately not treated as pending: a container that is
    /// never moved would otherwise hold the settle open until the deadline.
    /// </para>
    /// <para>
    /// The deadline is armed on a timer rather than checked inside the frame callback.
    /// A throttled or backgrounded page stops servicing animation frames, and a deadline
    /// only the frame callback enforces cannot fire there: the promise would neither
    /// resolve nor reject and the awaiting evaluate would hang with no diagnostic, which
    /// is the failure the deadline exists to prevent.
    /// </para>
    /// </remarks>
    private const string QuiesceScript = """
        (timeoutMs) => new Promise((resolve, reject) => {
          let quiet = 0;
          let finished = false;
          let frame = 0;
          const observer = new MutationObserver(() => { quiet = 0; });
          observer.observe(document.body, { attributes: true, subtree: true, childList: true });

          const portalPending = () => [...document.querySelectorAll('[data-blazix-base-ui-portal]')]
            .some((el) => el.style.display === 'none');

          let deadline = 0;
          const stop = () => {
            finished = true;
            observer.disconnect();
            if (frame) cancelAnimationFrame(frame);
            clearTimeout(deadline);
          };

          deadline = setTimeout(() => {
            const pending = portalPending();
            stop();
            reject(new Error(
              `Timed out after ${timeoutMs}ms waiting for the page to settle ` +
              `(quiet frames: ${quiet}, portal mid-mount: ${pending}).`));
          }, timeoutMs);

          const tick = () => {
            if (finished) return;
            if (portalPending()) { quiet = 0; frame = requestAnimationFrame(tick); return; }
            if (++quiet >= 2) { stop(); resolve(); return; }
            frame = requestAnimationFrame(tick);
          };
          frame = requestAnimationFrame(tick);
        })
        """;

    /// <summary>
    /// Waits for interactivity, font loading, and two consecutive mutation-free frames
    /// during which no Blazix portal is still being mounted.
    /// </summary>
    /// <param name="page">The page to wait on.</param>
    /// <param name="timeoutMs">The overall timeout in milliseconds.</param>
    /// <returns>A task that completes once the page is settled.</returns>
    public static async Task WaitAsync(IPage page, int timeoutMs = 30_000)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        static int Remaining(long deadline) => (int)Math.Max(1, deadline - Environment.TickCount64);

        // data-interactive is only ever "true": both fixture routes use prerender: false,
        // so the component's first render is already the interactive one. Wait for the
        // root's presence, never for a false -> true transition, which cannot occur.
        await page.WaitForFunctionAsync(
            "() => window[Symbol.for('Blazix.Parity.Capture')]?.settled() === true",
            null,
            new PageWaitForFunctionOptions { Timeout = Remaining(deadline) });

        await page.EvaluateAsync(FontsScript, Remaining(deadline));

        await page.EvaluateAsync(QuiesceScript, Remaining(deadline));
    }
}
