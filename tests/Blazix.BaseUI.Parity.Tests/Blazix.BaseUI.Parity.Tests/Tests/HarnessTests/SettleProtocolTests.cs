using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Fixtures;
using Blazix.BaseUI.Parity.Tests.Infrastructure;
using Blazix.BaseUI.Utilities;
using Shouldly;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

/// <summary>
/// Pins the settle protocol's handling of Blazix portals, which are placed from
/// <c>OnAfterRenderAsync</c> and can therefore land after the DOM has gone quiet.
/// </summary>
/// <param name="playwright">The browser fixture.</param>
public sealed class SettleProtocolTests(PlaywrightFixture playwright)
    : IClassFixture<PlaywrightFixture>
{
    [Fact]
    [SlopwatchSuppress("SW004", "Task.Delay throttles a routed response to widen the mid-mount window under test; it is fault injection, not a settle heuristic.")]
    public async Task WaitsForAPortalThatMountsAfterTheDomGoesQuiet()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await CaptureScript.InjectAsync(page);

        // Portal.razor renders its container inline, then moves it to <body> from
        // OnAfterRenderAsync via a lazily imported JS module. On a warm localhost that
        // import usually resolves inside two animation frames, so the race is real but
        // intermittent. Throttling the module response makes the window reliably longer
        // than the quiet period, so this test fails deterministically when the settle
        // protocol does not wait for the placement.
        await page.RouteAsync("**/blazix-baseui-portal*.js", async route =>
        {
            await Task.Delay(500);
            await route.ContinueAsync();
        });

        await page.GotoAsync(
            $"{ParityServerAssemblyFixture.ServerAddress}/fixture/harness/capture-probe/server");

        await SettleProtocol.WaitAsync(page);

        // Asserted first, and on the portal's content rather than on the container's
        // style: it is the placement itself that a premature capture misses, so a gate
        // that merely watched the style attribute disappear would satisfy the second
        // assertion while still capturing a popup-less DOM.
        var placed = await page.EvaluateAsync<bool>(
            "() => document.querySelector('body > [data-blazix-base-ui-portal] section') !== null");
        placed.ShouldBeTrue();

        // The probe also carries a decoy <aside data-blazix-base-ui-portal> that is never
        // moved anywhere, so a gate keyed on "not a child of body" would hang on it. Only
        // the inline display:none that Portal.razor writes and createPortal removes marks
        // a container as genuinely mid-mount.
        var pending = await page.EvaluateAsync<string[]>(
            """
            () => [...document.querySelectorAll('[data-blazix-base-ui-portal]')]
              .filter((el) => el.style.display === 'none')
              .map((el) => el.outerHTML.slice(0, 120))
            """);

        pending.ShouldBeEmpty();
    }
}
