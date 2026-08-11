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
        await page.Locator("button").EvaluateAsync(
            "element => element.style.setProperty('--blazor-load-percentage', '88%')");

        var capture = await CaptureScript.CaptureAsync(page, "initial");
        var button = capture.Dom.Descendants().Single(n => n.Tag == "button");
        var props = capture.CustomProps[button.Path];

        // Asserted first: without a property that must survive, a readCustomProps
        // returning nothing at all would satisfy the exclusion assertion below.
        props["--parity-probe"].ShouldBe("7px");

        props.Keys.ShouldNotContain(name => name.StartsWith("--tw-", StringComparison.Ordinal));
        props.Keys.ShouldNotContain(name =>
            name.StartsWith("--blazor-load-", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CapturesAnimationDelayAndIterationCountForRunMeasurement()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await OpenProbeAsync(context);
        await page.Locator("button").EvaluateAsync(
            """
            element => {
              element.style.animationDelay = '-1.2s';
              element.style.animationIterationCount = '3';
            }
            """);

        var capture = await CaptureScript.CaptureAsync(page, "initial");
        var button = capture.Dom.Descendants().Single(node => node.Tag == "button");
        var styles = capture.Styles[button.Path];

        styles["animation-delay"].ShouldBe("-1.2s");
        styles["animation-iteration-count"].ShouldBe("3");
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

        // The production runner stops each step explicitly. This still guards an interrupted
        // diagnostic attempt that starts the next recording without reaching that stop.
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

    [Fact]
    public async Task AnimationRegistrationWaitsForAStaggeredActiveSet()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await OpenProbeAsync(context);

        var count = await page.EvaluateAsync<int>(
            """
            async () => {
              const api = window[Symbol.for('Blazix.Parity.Capture')];
              const root = document.querySelector('[data-parity-root]');
              const pending = api.awaitAnimationRegistration([], 1000);

              const first = document.createElement('div');
              root.appendChild(first);
              first.animate(
                [{ opacity: 0 }, { opacity: 1 }],
                { duration: 500 });

              requestAnimationFrame(() => {
                const second = document.createElement('div');
                root.appendChild(second);
                second.animate(
                  [{ transform: 'scale(0)' }, { transform: 'scale(1)' }],
                  { duration: 500 });
              });

              return pending;
            }
            """);

        count.ShouldBe(2);
    }

    [Fact]
    public async Task AnimationRegistrationWaitsForDelayedRootMotionAfterNonTerminalCompletion()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await OpenProbeAsync(context);

        var result = await page.EvaluateAsync<JsonElement>(
            """
            async () => {
              const api = window[Symbol.for('Blazix.Parity.Capture')];
              const root = document.querySelector('[data-parity-root]');
              api.beginAnimationProbe();

              const descendant = document.createElement('div');
              descendant.style.cssText = 'width:0;height:0;overflow:hidden';
              root.appendChild(descendant);
              descendant.animate(
                [{ opacity: 0 }, { opacity: 1 }],
                { duration: 1000, easing: 'linear' });

              setTimeout(() => {
                root.setAttribute('data-root-motion-ready', 'true');
                root.animate(
                  [{ backgroundColor: 'rgb(255, 255, 255)' },
                   { backgroundColor: 'rgb(0, 0, 0)' }],
                  { duration: 1000, easing: 'linear' });
              }, 100);

              const registered = await api.awaitAnimationRegistration([
                {
                  kind: 'attribute',
                  selector: { css: '[data-parity-root]', index: 0 },
                  name: 'data-root-motion-ready',
                  expected: 'true',
                },
              ], 1000);
              const seeked = api.seekAnimations(0.5);
              return { registered, seeked };
            }
            """);

        // The zero-box descendant models the thumb transition that can register before
        // Blazor Server publishes the checked state on the root. The declared consequence
        // is non-terminal, so selecting the first stable animation set would freeze only
        // the descendant and photograph the root at wall-clock rather than fraction time.
        result.GetProperty("registered").GetInt32().ShouldBe(2);
        result.GetProperty("seeked").GetInt32().ShouldBe(2);
    }

    [Fact]
    public async Task FrozenEndpointDoesNotResolveOriginalFinishedLifecycleOrDetachPortal()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await OpenProbeAsync(context);

        var result = await page.EvaluateAsync<JsonElement>(
            """
            async () => {
              const api = window[Symbol.for('Blazix.Parity.Capture')];
              const portal = document.createElement('div');
              const popup = document.createElement('div');
              popup.textContent = 'closing popup';
              popup.style.cssText = 'width:120px;height:40px;background:red';
              portal.appendChild(popup);
              document.body.appendChild(portal);

              api.beginAnimationProbe();
              const original = popup.animate(
                [{ opacity: 1, transform: 'scale(1)' },
                 { opacity: 0, transform: 'scale(0.98)' }],
                { duration: 500, easing: 'linear' });
              original.finished.then(() => portal.remove());
              setTimeout(() => portal.remove(), 100);

              const registered = await api.awaitAnimationRegistration([
                {
                  kind: 'detached',
                  selector: { css: 'body > div:last-child', index: 0 },
                },
              ], 1000);
              const seeked = api.seekAnimations(1);
              await new Promise((resolve) => setTimeout(resolve, 150));

              return {
                registered,
                seeked,
                connected: portal.isConnected,
                roots: api.screenshotRoots().map((root) => root.label),
              };
            }
            """);

        result.GetProperty("registered").GetInt32().ShouldBe(1);
        result.GetProperty("seeked").GetInt32().ShouldBe(1);
        result.GetProperty("connected").GetBoolean().ShouldBeTrue();
        result.GetProperty("roots").EnumerateArray()
            .Select(item => item.GetString())
            .ShouldContain("portal(2)");
    }

    [Fact]
    public async Task AnimationRegistrationRetainsAnObservedAnimationAfterItFinishes()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await OpenProbeAsync(context);

        var result = await page.EvaluateAsync<JsonElement>(
            """
            async () => {
              const api = window[Symbol.for('Blazix.Parity.Capture')];
              const root = document.querySelector('[data-parity-root]');
              api.beginAnimationProbe();

              const moving = document.createElement('div');
              moving.style.cssText =
                'width:40px;height:40px;background:red;opacity:1';
              root.appendChild(moving);
              const animation = moving.animate(
                [{ opacity: 0 }, { opacity: 1 }],
                { duration: 30 });

              // Let the probe observe the animation, then let its short natural run finish
              // before registration asks for a stable set. WASM render/interop can create
              // this exact scheduling gap for a 100 ms popup transition.
              await new Promise((resolve) => setTimeout(resolve, 100));
              const registered = await api.awaitAnimationRegistration([], 250);
              const seeked = api.seekAnimations(0.5);
              const currentTime = animation.currentTime;
              const state = animation.playState;
              return { registered, seeked, currentTime, state };
            }
            """);

        result.GetProperty("registered").GetInt32().ShouldBe(1);
        result.GetProperty("seeked").GetInt32().ShouldBe(1);
        result.GetProperty("state").GetString().ShouldBe("paused");
        result.GetProperty("currentTime").GetDouble().ShouldBe(15, tolerance: 2);
    }

    [Fact]
    public async Task SeekingIgnoresAnimationsOutsideTheCaptureRoots()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await OpenProbeAsync(context);

        var count = await page.EvaluateAsync<int>(
            """
            async () => {
              const api = window[Symbol.for('Blazix.Parity.Capture')];
              const ignored = document.createElement('div');
              ignored.setAttribute('data-parity-ignore', '');
              document.body.appendChild(ignored);
              const animation = ignored.animate(
                [{ opacity: 0 }, { opacity: 1 }],
                { duration: 20000, iterations: Infinity });
              await new Promise((resolve) => requestAnimationFrame(resolve));
              const count = api.seekAnimations(0.5);
              animation.cancel();
              ignored.remove();
              return count;
            }
            """);

        count.ShouldBe(0);
    }

    [Fact]
    public async Task SeekingDoesNotRewindAFinishedFillForwardAnimation()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await OpenProbeAsync(context);

        var result = await page.EvaluateAsync<JsonElement>(
            """
            async () => {
              const api = window[Symbol.for('Blazix.Parity.Capture')];
              const root = document.querySelector('[data-parity-root]');
              const finished = root.animate(
                [{ opacity: 0 }, { opacity: 1 }],
                { duration: 40, fill: 'forwards' });
              await finished.finished;
              const terminalTime = finished.currentTime;
              const active = root.animate(
                [{ transform: 'scale(0)' }, { transform: 'scale(1)' }],
                { duration: 20000 });
              await new Promise((resolve) => requestAnimationFrame(resolve));

              const count = api.seekAnimations(0);
              const result = {
                count,
                terminalTime,
                finishedTime: finished.currentTime,
                activeTime: active.currentTime,
              };
              finished.cancel();
              active.cancel();
              return result;
            }
            """);

        result.GetProperty("count").GetInt32().ShouldBe(1);
        result.GetProperty("finishedTime").GetDouble().ShouldBe(
            result.GetProperty("terminalTime").GetDouble(), tolerance: 1);
        result.GetProperty("activeTime").GetDouble().ShouldBe(0);
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
