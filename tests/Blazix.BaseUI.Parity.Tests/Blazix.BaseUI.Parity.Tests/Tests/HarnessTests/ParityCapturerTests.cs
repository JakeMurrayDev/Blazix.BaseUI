using System.Globalization;
using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Fixtures;
using Blazix.BaseUI.Parity.Tests.Infrastructure;
using Shouldly;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

/// <summary>
/// Verifies that a manifest entry round-trips through a real browser into a capture bundle.
/// </summary>
/// <param name="playwright">The browser fixture.</param>
public sealed class ParityCapturerTests(PlaywrightFixture playwright)
    : IClassFixture<PlaywrightFixture>, IDisposable
{
    /// <summary>
    /// Installs a twenty-second animation inside the fixture root once the root exists.
    /// </summary>
    /// <remarks>
    /// Injected from the test rather than added to <c>CaptureProbe.razor</c>, so that the
    /// probe every other capture test reads keeps the markup those tests were written
    /// against and only this test sees a page with something to seek.
    /// </remarks>
    private const string DriftScript = """
        (() => {
          const install = () => {
            const root = document.querySelector('[data-parity-root]');
            if (!root) { requestAnimationFrame(install); return; }
            const style = document.createElement('style');
            style.textContent =
              '@keyframes parity-drift { from { opacity: 0 } to { opacity: 1 } }' +
              '#parity-drift { animation: parity-drift 20s linear; }';
            document.head.appendChild(style);
            const drift = document.createElement('div');
            drift.id = 'parity-drift';
            drift.textContent = 'drift';
            root.appendChild(drift);
          };
          install();
        })();
        """;

    /// <summary>
    /// Appends a body child with no size, which <c>capture.js</c> counts as a capture root
    /// and Playwright cannot photograph. Appended before Blazix moves its own portal
    /// container out, so this is <c>portal(1)</c> and the real one is <c>portal(2)</c>.
    /// </summary>
    private const string EmptyPortalScript = """
        (() => {
          const install = () => {
            if (!document.body) { requestAnimationFrame(install); return; }
            const empty = document.createElement('div');
            empty.style.width = '0';
            empty.style.height = '0';
            document.body.appendChild(empty);
          };
          install();
        })();
        """;

    private readonly string screenshots = Path.Combine(
        Path.GetTempPath(), "blazix-parity-capture", Guid.NewGuid().ToString("N"));

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(screenshots))
        {
            Directory.Delete(screenshots, recursive: true);
        }
    }

    [Fact]
    public async Task CapturesEveryStepOfSwitchHeroOnTheServerLeg()
    {
        var fixture = FixtureManifest.Load().Single(entry => entry.Id == "switch/hero");

        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        var bundle = await new ParityCapturer().CaptureAsync(page, fixture, ParityLeg.BlazorServer);

        bundle.Steps.Count.ShouldBe(2);
        bundle.Steps[0].Styles.ShouldNotBeEmpty();
        bundle.Steps[0].Aria.ShouldContain("switch");

        // The Task 1 placeholder fixture carries no Tailwind classes, so its
        // <span role="switch"> lays out 0px wide and has no area a pointer can reach.
        // The step's selector is therefore addressable but not actionable, which is a
        // parity result and not a harness failure: it is recorded, the run continues,
        // and the state is captured unchanged. Task 15 replaces the placeholder with the
        // React demo's class strings, at which point the click lands and these
        // assertions invert.
        bundle.Steps[1].NonActionableSelectors.ShouldBe(["[role=switch]"]);

        // Asserted alongside, because separating the two is the point: the element is
        // there, so a comparator reading this bundle must see a layout difference and not
        // an addressing one.
        bundle.Steps[1].UnresolvedSelectors.ShouldBeEmpty();
        Checked(bundle.Steps[1]).ShouldBe(Checked(bundle.Steps[0]));
    }

    [Fact]
    public async Task PerformsStepActionsAndCapturesTheirEffect()
    {
        // Built here rather than added to the manifest: the manifest is the fixture
        // corpus, and this exercises the capturer's action loop against markup that is
        // actually actionable, which switch/hero's placeholder is not.
        var fixture = new FixtureEntry
        {
            Id = "harness/capture-probe",
            Component = "harness",
            React = "internal:none",
            Blazor = "Harness/CaptureProbe",
            Steps =
            [
                new StepEntry { Name = "initial" },
                new StepEntry { Name = "focused", Do = [new StepAction { Focus = "button" }] }
            ]
        };

        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        var bundle = await new ParityCapturer().CaptureAsync(page, fixture, ParityLeg.BlazorServer);

        bundle.Steps[0].Focus.ShouldBeNull();
        bundle.Steps[1].Focus.ShouldBe("root > button");
        bundle.Steps[1].UnresolvedSelectors.ShouldBeEmpty();
        bundle.Steps[1].NonActionableSelectors.ShouldBeEmpty();
    }

    [Fact]
    public async Task PhotographsTheFixtureRootAndEveryPortalContainer()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        var bundle = await new ParityCapturer(screenshots)
            .CaptureAsync(page, ProbeFixture(new StepEntry { Name = "initial" }), ParityLeg.BlazorServer);

        // The probe portals one container out to <body>, so the step is photographed twice:
        // shot 0 is the fixture root and shot 1 is portal(1), the label capture.js gives
        // that same element. A run that only photographed the root would compare every
        // popup in the corpus against nothing.
        bundle.Steps[0].Screenshots.ShouldBe([
            "harness__capture-probe.BlazorServer.initial.0.png",
            "harness__capture-probe.BlazorServer.initial.1.png"
        ]);

        foreach (var name in bundle.Steps[0].Screenshots)
        {
            File.Exists(Path.Combine(screenshots, name)).ShouldBeTrue();
        }
    }

    [Fact]
    public async Task LeavesAContainerItCannotPhotographOutOfTheList()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.AddInitScriptAsync(EmptyPortalScript);

        var bundle = await new ParityCapturer(screenshots)
            .CaptureAsync(page, ProbeFixture(new StepEntry { Name = "initial" }), ParityLeg.BlazorServer);

        // The run continues past the container that could not be photographed, and the
        // shot is left out rather than listed: a name recorded for a file that was never
        // written turns a harness hiccup into a pixel finding on the next comparison, and
        // throwing would have ended the fixture over one empty div.
        bundle.Steps[0].Screenshots.ShouldBe([
            "harness__capture-probe.BlazorServer.initial.0.png",
            "harness__capture-probe.BlazorServer.initial.2.png"
        ]);

        foreach (var name in bundle.Steps[0].Screenshots)
        {
            File.Exists(Path.Combine(screenshots, name)).ShouldBeTrue();
        }
    }

    [Fact]
    public async Task PhotographsNoFramesWhenNothingOnTheLegAnimated()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        var fixture = ProbeFixture(new StepEntry { Name = "still", Settle = "animation" });
        var bundle = await new ParityCapturer(screenshots)
            .CaptureAsync(page, fixture, ParityLeg.BlazorServer);

        // Nothing on the probe animates, so there is no frame to seek to. Five copies of
        // the settled shot would compare equal to the other leg's five real frames and
        // report parity where a whole animation is missing; an absent frame does not.
        bundle.Steps[0].Screenshots.ShouldBe([
            "harness__capture-probe.BlazorServer.still.0.png",
            "harness__capture-probe.BlazorServer.still.1.png"
        ]);
    }

    [Fact]
    public async Task PhotographsFiveSeekedFramesOfEveryRootOfAnAnimationStep()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.AddInitScriptAsync(DriftScript);

        var fixture = ProbeFixture(new StepEntry { Name = "drifting", Settle = "animation" });
        var bundle = await new ParityCapturer(screenshots)
            .CaptureAsync(page, fixture, ParityLeg.BlazorServer);

        var frames = bundle.Steps[0].Screenshots
            .Where(name => name.Contains(".frame", StringComparison.Ordinal))
            .ToList();

        // Five fractions across two roots, named by percentage so the ordering a reader
        // sees in the report is the ordering of the animation.
        frames.ShouldBe([
            "harness__capture-probe.BlazorServer.drifting.frame0.0.png",
            "harness__capture-probe.BlazorServer.drifting.frame0.1.png",
            "harness__capture-probe.BlazorServer.drifting.frame25.0.png",
            "harness__capture-probe.BlazorServer.drifting.frame25.1.png",
            "harness__capture-probe.BlazorServer.drifting.frame50.0.png",
            "harness__capture-probe.BlazorServer.drifting.frame50.1.png",
            "harness__capture-probe.BlazorServer.drifting.frame75.0.png",
            "harness__capture-probe.BlazorServer.drifting.frame75.1.png",
            "harness__capture-probe.BlazorServer.drifting.frame100.0.png",
            "harness__capture-probe.BlazorServer.drifting.frame100.1.png"
        ]);

        foreach (var name in frames)
        {
            File.Exists(Path.Combine(screenshots, name)).ShouldBeTrue();
        }
    }

    [Fact]
    public async Task LeavesTheStepAfterAnAnimationStepUnaffectedByTheSeeking()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.AddInitScriptAsync(DriftScript);

        var fixture = ProbeFixture(
            new StepEntry { Name = "drifting", Settle = "animation" },
            new StepEntry { Name = "after" });

        var bundle = await new ParityCapturer(screenshots)
            .CaptureAsync(page, fixture, ParityLeg.BlazorServer);

        // Asserted first so nothing below can hold vacuously: the frames were taken, so
        // the animation really was paused and seeked to its end.
        bundle.Steps[0].Screenshots
            .ShouldContain("harness__capture-probe.BlazorServer.drifting.frame100.0.png");

        var after = Drift(bundle.Steps[1]);

        // The final seek parks the animation on its last keyframe, where opacity is 1. An
        // animation left there stays there for every remaining step of the fixture, and
        // every screenshot and computed style after it is of a frozen page. A few seconds
        // into a twenty-second run, a live one is nowhere near.
        after.ShouldBeLessThan(0.5);
        after.ShouldBeGreaterThanOrEqualTo(Drift(bundle.Steps[0]));

        var paused = await page.EvaluateAsync<int>(
            "() => document.getAnimations().filter((a) => a.playState === 'paused').length");
        paused.ShouldBe(0);

        // And nothing the frame loop did reached the record. Seeking an animation to its
        // end and back is two phase transitions, which the browser reports as an
        // animationend and an animationstart; recorded, they would attribute the harness's
        // own bookkeeping to the component, on the one comparator whose entire subject is
        // animation. (That this step's record is the previous step's at all is a separate,
        // pre-existing matter: startTimeline() runs only for animation steps.)
        bundle.Steps[1].Timeline.ShouldBe(bundle.Steps[0].Timeline);
    }

    private static double Drift(StepCapture step)
    {
        var node = step.Dom.Descendants().Single(n => n.Text == "drift");

        return double.Parse(step.Styles[node.Path]["opacity"], CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// The capture probe as a manifest entry. Built here rather than added to the manifest:
    /// the manifest is the fixture corpus, and this is a harness probe.
    /// </summary>
    private static FixtureEntry ProbeFixture(params StepEntry[] steps) => new()
    {
        Id = "harness/capture-probe",
        Component = "harness",
        React = "internal:none",
        Blazor = "Harness/CaptureProbe",
        Steps = steps
    };

    private static string Checked(StepCapture step)
        => step.Dom.Descendants().Single(node => node.Attributes.ContainsKey("aria-checked"))
            .Attributes["aria-checked"];
}
