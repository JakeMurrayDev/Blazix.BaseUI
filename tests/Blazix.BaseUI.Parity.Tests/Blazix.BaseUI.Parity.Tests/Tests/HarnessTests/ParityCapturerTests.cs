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
    : IClassFixture<PlaywrightFixture>
{
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
        // React demo's class strings, at which point the click lands and these two
        // assertions invert.
        bundle.Steps[1].UnresolvedSelectors.ShouldBe(["[role=switch]"]);
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
    }

    private static string Checked(StepCapture step)
        => step.Dom.Descendants().Single(node => node.Attributes.ContainsKey("aria-checked"))
            .Attributes["aria-checked"];
}
