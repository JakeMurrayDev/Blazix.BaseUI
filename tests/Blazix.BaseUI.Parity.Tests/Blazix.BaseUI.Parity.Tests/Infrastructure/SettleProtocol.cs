using Microsoft.Playwright;

namespace Blazix.BaseUI.Parity.Tests.Infrastructure;

/// <summary>
/// Waits for a fixture page to reach a stable, capture-ready state.
/// </summary>
public static class SettleProtocol
{
    /// <summary>
    /// Waits for interactivity, font loading, and two consecutive mutation-free frames.
    /// </summary>
    /// <param name="page">The page to wait on.</param>
    /// <param name="timeoutMs">The overall timeout in milliseconds.</param>
    /// <returns>A task that completes once the page is settled.</returns>
    public static async Task WaitAsync(IPage page, int timeoutMs = 30_000)
    {
        // data-interactive is only ever "true": both fixture routes use prerender: false,
        // so the component's first render is already the interactive one. Wait for the
        // root's presence, never for a false -> true transition, which cannot occur.
        await page.WaitForFunctionAsync(
            "() => window[Symbol.for('Blazix.Parity.Capture')]?.settled() === true",
            null,
            new PageWaitForFunctionOptions { Timeout = timeoutMs });

        await page.EvaluateAsync("() => document.fonts.ready");

        await page.EvaluateAsync("""
            () => new Promise(resolve => {
              let quiet = 0;
              const observer = new MutationObserver(() => { quiet = 0; });
              observer.observe(document.body, { attributes: true, subtree: true, childList: true });
              const tick = () => {
                if (++quiet >= 2) { observer.disconnect(); resolve(); return; }
                requestAnimationFrame(tick);
              };
              requestAnimationFrame(tick);
            })
            """);
    }
}
