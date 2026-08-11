using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Fixtures;
using Blazix.BaseUI.Parity.Tests.Infrastructure;
using Blazix.BaseUI.Utilities;
using Microsoft.Playwright;
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

    [Fact]
    public async Task FailsTheDeadlineWhenAnimationFramesStopBeingServiced()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await CaptureScript.InjectAsync(page);

        await page.GotoAsync(
            $"{ParityServerAssemblyFixture.ServerAddress}/fixture/harness/capture-probe/server");

        // Settled first for two reasons: the quiesce phase is the part under test, so the
        // page has to reach it, and Playwright's own waiting binds the page's native
        // requestAnimationFrame when it first injects into this frame — which this call
        // does, before the replacement below can be captured in its place.
        await SettleProtocol.WaitAsync(page);

        // A throttled or backgrounded tab stops servicing animation frames while its
        // timers keep running. Playwright deliberately keeps its pages foregrounded and
        // unthrottled, so the state is reached by replacing requestAnimationFrame with
        // one that never calls back: fault injection, like the routed delay above, not a
        // stand-in for the protocol's own behaviour.
        await page.EvaluateAsync("() => { window.requestAnimationFrame = () => 0; }");

        // Budgeted rather than awaited outright. When the deadline lived only inside the
        // frame callback this call never completed at all — it neither resolved nor
        // rejected — so an unbudgeted await would hang the suite instead of failing it,
        // and hanging is the defect. Ten seconds is ten times the deadline being proven.
        var settle = SettleProtocol.WaitAsync(page, 1_000);

        var failure = await Should.ThrowAsync<PlaywrightException>(
            () => settle.WaitAsync(TimeSpan.FromSeconds(10)));

        // Matched on the quiesce script's own wording, not merely on the exception type:
        // Playwright's TimeoutException derives from PlaywrightException, so a settle that
        // failed one phase earlier would otherwise satisfy this test.
        failure.Message.ShouldContain("Timed out after 1000ms waiting for the page to settle");
    }

    [Fact]
    [SlopwatchSuppress("SW004", "The post-deadline observation is fault injection that proves the rejected quiescence loop stopped scheduling work.")]
    public async Task SettleDeadlineStopsTheQuiescenceFrameLoop()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await CaptureScript.InjectAsync(page);
        await page.GotoAsync(
            $"{ParityServerAssemblyFixture.ServerAddress}/fixture/harness/capture-probe/server");
        await SettleProtocol.WaitAsync(page);

        await page.EvaluateAsync(
            """
            () => {
              const pending = document.createElement('div');
              pending.setAttribute('data-blazix-base-ui-portal', '');
              pending.style.display = 'none';
              document.body.appendChild(pending);
              window.__parityFrameTicks = 0;
              const native = window.requestAnimationFrame.bind(window);
              window.requestAnimationFrame = (callback) => native((time) => {
                window.__parityFrameTicks += 1;
                callback(time);
              });
            }
            """);

        await Should.ThrowAsync<PlaywrightException>(() => SettleProtocol.WaitAsync(page, 100));
        var atDeadline = await page.EvaluateAsync<int>("() => window.__parityFrameTicks");
        await Task.Delay(100);
        var afterDeadline = await page.EvaluateAsync<int>("() => window.__parityFrameTicks");

        afterDeadline.ShouldBe(atDeadline);
    }

    [Theory]
    [InlineData(ParityLeg.BlazorServer)]
    [InlineData(ParityLeg.BlazorWasm)]
    public async Task AnimationFenceIgnoresInfiniteMotionAndWaitsFiniteCompletion(
        ParityLeg leg)
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await OpenProbeAsync(context, leg);

        await page.EvaluateAsync(
            """
            () => {
              const root = document.querySelector('[data-parity-root]');
              const infinite = document.createElement('div');
              const finite = document.createElement('div');
              infinite.textContent = 'infinite';
              finite.textContent = 'finite';
              root.append(infinite, finite);
              infinite.animate(
                [{ opacity: 0 }, { opacity: 1 }],
                { duration: 1000, iterations: Infinity });
              finite.animate(
                [{ opacity: 0 }, { opacity: 1 }],
                { duration: 120 }).finished.then(() => {
                  finite.remove();
                  root.dataset.finiteComplete = 'true';
                });
            }
            """);

        await AnimationSettleProtocol.WaitAsync(page, 1_000);

        (await page.Locator("[data-parity-root]")
                .GetAttributeAsync("data-finite-complete"))
            .ShouldBe("true");
        (await page.GetByText("infinite", new PageGetByTextOptions { Exact = true })
                .CountAsync())
            .ShouldBe(1);
        (await page.GetByText("finite", new PageGetByTextOptions { Exact = true })
                .CountAsync())
            .ShouldBe(0);
    }

    [Theory]
    [InlineData(ParityLeg.BlazorServer)]
    [InlineData(ParityLeg.BlazorWasm)]
    public async Task AnimationFenceFailsItsDeadlineForUnfinishedFiniteMotion(ParityLeg leg)
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await OpenProbeAsync(context, leg);

        await page.EvaluateAsync(
            """
            () => {
              const root = document.querySelector('[data-parity-root]');
              const finite = document.createElement('div');
              finite.textContent = 'long finite';
              root.appendChild(finite);
              finite.animate(
                [{ opacity: 0 }, { opacity: 1 }],
                { duration: 20000 });
            }
            """);

        var failure = await Should.ThrowAsync<PlaywrightException>(() =>
            AnimationSettleProtocol.WaitAsync(page, 250));

        failure.Message.ShouldContain(
            "Timed out after 250ms waiting for finite animations");
        failure.Message.ShouldContain("pending: 1");
    }

    [Theory]
    [InlineData(ParityLeg.BlazorServer)]
    [InlineData(ParityLeg.BlazorWasm)]
    public async Task AnimationFenceFailsItsDeadlineForPausedFiniteMotion(ParityLeg leg)
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await OpenProbeAsync(context, leg);

        await page.EvaluateAsync(
            """
            () => {
              const root = document.querySelector('[data-parity-root]');
              const finite = root.animate(
                [{ opacity: 0 }, { opacity: 1 }],
                { duration: 20000 });
              finite.pause();
            }
            """);

        var failure = await Should.ThrowAsync<PlaywrightException>(() =>
            AnimationSettleProtocol.WaitAsync(page, 250));

        failure.Message.ShouldContain(
            "Timed out after 250ms waiting for finite animations");
        failure.Message.ShouldContain("pending: 1");
    }

    private static async Task<IPage> OpenProbeAsync(
        IBrowserContext context,
        ParityLeg leg)
    {
        var page = await context.NewPageAsync();
        await CaptureScript.InjectAsync(page);
        var mode = leg == ParityLeg.BlazorServer ? "server" : "wasm";
        await page.GotoAsync(
            $"{ParityServerAssemblyFixture.ServerAddress}/fixture/harness/capture-probe/{mode}");
        await SettleProtocol.WaitAsync(page);
        return page;
    }
}
