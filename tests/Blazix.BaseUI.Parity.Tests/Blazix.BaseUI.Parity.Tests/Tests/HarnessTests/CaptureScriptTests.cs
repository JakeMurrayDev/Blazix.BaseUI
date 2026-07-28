using System.Text.Json;
using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Fixtures;
using Blazix.BaseUI.Parity.Tests.Infrastructure;
using Microsoft.Playwright;
using Shouldly;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

/// <summary>
/// Pins the normalization rules of the shared capture script against a probe fixture
/// whose markup is known, so the assertions can be exact.
/// </summary>
/// <param name="playwright">The browser fixture.</param>
public sealed class CaptureScriptTests(PlaywrightFixture playwright)
    : IClassFixture<PlaywrightFixture>
{
    [Fact]
    public async Task SymbolizesGeneratedIdsAndPreservesReferences()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await OpenProbeAsync(context);

        var capture = await CaptureScript.CaptureAsync(page, "initial");

        var button = capture.Dom.Descendants().Single(n => n.Tag == "button");
        var panel = capture.Dom.Descendants().Single(n => n.Tag == "section");

        // The panel is portalled to <body>, so the trigger and its target land in
        // different capture roots. One id table per root cannot see across that
        // boundary and leaves the reference as a raw framework-generated id.
        panel.Attributes["id"].ShouldStartWith("#id");

        // The raw ids differ between React and Blazor; the RELATIONSHIP must survive.
        button.Attributes["aria-controls"].ShouldBe(panel.Attributes["id"]);
    }

    [Fact]
    public async Task RenamesBlazixMarkersToUpstreamForm()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await OpenProbeAsync(context);

        var capture = await CaptureScript.CaptureAsync(page, "initial");
        var marked = capture.Dom.Descendants().Single(n => n.Tag == "aside");

        marked.Attributes.ShouldContainKey("data-base-ui-portal");
        marked.Attributes.ShouldNotContainKey("data-blazix-base-ui-portal");

        // Idempotent: an already-unprefixed marker passes through untouched.
        marked.Attributes.ShouldContainKey("data-base-ui-focusable");
    }

    [Fact]
    public async Task ExcludesClassAndStyleFromAttributesButRecordsClassSeparately()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await OpenProbeAsync(context);

        var capture = await CaptureScript.CaptureAsync(page, "initial");
        var button = capture.Dom.Descendants().Single(n => n.Tag == "button");

        button.Attributes.ShouldNotContainKey("class");
        button.Attributes.ShouldNotContainKey("style");
        button.Classes.ShouldContain("px-3");
    }

    [Fact]
    public async Task ExcludesTailwindInternalsButKeepsOtherCustomProperties()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await OpenProbeAsync(context);

        var capture = await CaptureScript.CaptureAsync(page, "initial");
        var button = capture.Dom.Descendants().Single(n => n.Tag == "button");
        var props = capture.CustomProps[button.Path];

        // Asserted first: without a property that must survive, a readCustomProps
        // returning nothing at all would satisfy the exclusion assertion below.
        props["--parity-probe"].ShouldBe("7px");

        props.Keys.ShouldNotContain(name => name.StartsWith("--tw-", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExcludesBlazorElementReferenceAttributes()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await OpenProbeAsync(context);

        // Asserted first so the exclusion below cannot pass vacuously: `_bl_` is applied
        // by Blazor's browser renderer, never by the server, so a probe without a
        // RenderElement-backed component would carry none of them to begin with.
        var live = await page.EvaluateAsync<int>(
            """
            () => [...document.querySelectorAll('*')]
              .filter((e) => e.getAttributeNames().some((n) => n.startsWith('_bl_')))
              .length
            """);
        live.ShouldBeGreaterThan(0);

        var capture = await CaptureScript.CaptureAsync(page, "initial");
        var leaked = capture.Dom.Descendants()
            .SelectMany(n => n.Attributes.Keys)
            .Where(name => name.StartsWith("_bl_", StringComparison.Ordinal))
            .ToList();

        // The name embeds a per-run reference-capture id, so leaking even one makes the
        // element diff against React and differ from itself on the next run.
        leaked.ShouldBeEmpty();
    }

    [Fact]
    public async Task NamespacesNodePathsByRoot()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await OpenProbeAsync(context);

        var capture = await CaptureScript.CaptureAsync(page, "initial");

        // #roots is a synthetic wrapper, not a DOM node, and holds no keyed data.
        var nodes = capture.Dom.Descendants()
            .Where(n => n.Tag != CaptureNames.RootsWrapper)
            .ToList();

        var inRoot = nodes.Single(n => n.Attributes.GetValueOrDefault("data-probe") == "in-root");
        var inPortal = nodes.Single(n => n.Attributes.GetValueOrDefault("data-probe") == "in-portal");

        inRoot.Path.ShouldBe("root > p");
        inPortal.Path.ShouldBe("portal(1) > p");

        // Both root elements are captured too, and un-namespaced they share the path "".
        nodes.Select(n => n.Path).ShouldBeUnique();

        // The keyed side-tables are what the comparators read. A collision does not throw,
        // it overwrites — so assert the counts line up rather than just spot-checking keys.
        capture.Geometry.Keys.ShouldContain("root");
        capture.Geometry.Keys.ShouldContain("portal(1)");
        capture.Geometry.Count.ShouldBe(nodes.Count);
        capture.Styles.Count.ShouldBe(nodes.Count);
        capture.CustomProps.Count.ShouldBe(nodes.Count);
    }

    [Fact]
    public async Task StartTimelineDiscardsThePreviousRegistration()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await OpenProbeAsync(context);

        // A per-step runner naturally does start -> capture -> start -> capture, because
        // capture() returns the timeline without stopping it.
        var recorded = await page.EvaluateAsync<int>(
            """
            async () => {
              const api = window[Symbol.for('Blazix.Parity.Capture')];
              api.startTimeline();
              api.startTimeline();
              document.querySelector('[data-parity-root]').setAttribute('data-probe-tick', '1');
              await new Promise((resolve) => setTimeout(resolve, 100));
              return api.stopTimeline().filter((e) => e.attr === 'data-probe-tick').length;
            }
            """);

        recorded.ShouldBe(1);
    }

    [Fact]
    public async Task SeekingPausesEveryAnimationAndResumingPutsItBackWhereItWas()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await OpenProbeAsync(context);

        var result = await page.EvaluateAsync<JsonElement>(
            """
            async () => {
              const api = window[Symbol.for('Blazix.Parity.Capture')];
              const root = document.querySelector('[data-parity-root]');
              const animation = root.animate(
                [{ opacity: 1 }, { opacity: 0 }], { duration: 4000, easing: 'linear' });
              await new Promise((resolve) => setTimeout(resolve, 120));

              const before = animation.currentTime;
              const seekedCount = api.seekAnimations(0.75);
              const seekedTime = animation.currentTime;
              const seekedState = animation.playState;

              api.resumeAnimations();
              const resumedTime = animation.currentTime;
              const resumedState = animation.playState;

              await new Promise((resolve) => setTimeout(resolve, 200));
              const laterTime = animation.currentTime;
              animation.cancel();

              return {
                before, seekedCount, seekedTime, seekedState,
                resumedTime, resumedState, laterTime,
              };
            }
            """);

        // Paused and placed, which is what makes a mid-animation screenshot a function of
        // the animation rather than of how fast the machine got there.
        result.GetProperty("seekedCount").GetInt32().ShouldBeGreaterThan(0);
        result.GetProperty("seekedState").GetString().ShouldBe("paused");
        result.GetProperty("seekedTime").GetDouble().ShouldBe(3000);

        // Put back where it was, not finished and not replayed. One runner drives every
        // step of a fixture on one page: an animation left at its end would hold the popup
        // in its final pose for every later step, and one rewound to zero would run the
        // whole transition again in the middle of the next one.
        result.GetProperty("resumedState").GetString().ShouldBe("running");
        result.GetProperty("resumedTime").GetDouble()
            .ShouldBe(result.GetProperty("before").GetDouble(), tolerance: 50);

        // And running: a resume that restored the clock without releasing it would leave
        // the page frozen at a value that never changes again.
        result.GetProperty("laterTime").GetDouble()
            .ShouldBeGreaterThan(result.GetProperty("resumedTime").GetDouble() + 100);
    }

    [Fact]
    public async Task ResumingDoesNotResolveUntilThePhaseCrossingItCausesHasBeenDispatched()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await OpenProbeAsync(context);

        var result = await page.EvaluateAsync<JsonElement>(
            """
            async () => {
              const api = window[Symbol.for('Blazix.Parity.Capture')];
              const root = document.querySelector('[data-parity-root]');
              const style = document.createElement('style');
              style.textContent =
                '@keyframes parity-resume { from { opacity: 1 } to { opacity: 0 } }' +
                '#parity-resume { animation: parity-resume 20s linear; }';
              document.head.appendChild(style);
              const el = document.createElement('div');
              el.id = 'parity-resume';
              root.appendChild(el);

              // Counted outside the harness's own recording, which the seek below detaches:
              // this has to observe what the browser dispatches, not what the timeline kept.
              let starts = 0;
              document.addEventListener('animationstart', () => { starts += 1; }, true);
              await new Promise((resolve) => setTimeout(resolve, 120));
              const own = starts;

              // A frame between the seeks, because the capturer takes a screenshot between
              // them and the browser decides an animation's phase once per frame update. Run
              // back to back in one task the end is never observed, no event is queued for
              // it, and the resume crosses back from a phase the page was never in.
              const frame = () =>
                new Promise((resolve) => requestAnimationFrame(() => requestAnimationFrame(resolve)));

              api.seekAnimations(0);
              await frame();
              api.seekAnimations(1);
              await frame();

              const pending = api.resumeAnimations();
              const atReturn = starts;
              await pending;
              const atResolve = starts;

              el.remove();
              style.remove();
              return { own, atReturn, atResolve };
            }
            """);

        // The animation's own start, and nothing else on the probe page animating.
        result.GetProperty("own").GetInt32().ShouldBe(1);

        // Restoring the clock crosses back into the active phase from the end the seek put
        // the animation at, and the browser reports that a frame later — not synchronously.
        // This is why a flag the resume could clear cannot silence it, and why a resume that
        // returned as soon as the clocks were set filed its own animationstart in the next
        // step's timeline: the capturer re-arms the recording after one round trip, which is
        // shorter than a frame.
        result.GetProperty("atReturn").GetInt32().ShouldBe(1);

        // Awaited, the crossing is dispatched before the resume resolves, so it lands while
        // the recording seekAnimations() detached is still detached and is recorded nowhere.
        result.GetProperty("atResolve").GetInt32().ShouldBe(2);
    }

    [Fact]
    public async Task ResumingWithoutHavingSeekedTouchesNothing()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await OpenProbeAsync(context);

        var state = await page.EvaluateAsync<string>(
            """
            async () => {
              const api = window[Symbol.for('Blazix.Parity.Capture')];
              const root = document.querySelector('[data-parity-root]');
              const animation = root.animate(
                [{ opacity: 1 }, { opacity: 0 }], { duration: 4000, easing: 'linear' });
              await new Promise((resolve) => setTimeout(resolve, 60));

              // A step with no animation to seek still reaches the resume, because the
              // resume is what guarantees the seek is always undone.
              api.resumeAnimations();
              const playState = animation.playState;
              animation.cancel();
              return playState;
            }
            """);

        state.ShouldBe("running");
    }

    private static async Task<IPage> OpenProbeAsync(IBrowserContext context)
    {
        var page = await context.NewPageAsync();
        await CaptureScript.InjectAsync(page);
        await page.GotoAsync($"{ParityServerAssemblyFixture.ServerAddress}/fixture/harness/capture-probe/server");

        // The Portal container is appended to <body> from OnAfterRenderAsync, after a
        // module import and an interop round trip, which can land later than the settle
        // protocol's two quiet frames. SettleProtocol now gates on the container's own
        // mid-mount flag, so no local wait is needed here.
        await SettleProtocol.WaitAsync(page);

        return page;
    }
}
