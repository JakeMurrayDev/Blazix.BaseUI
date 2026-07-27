using Blazix.BaseUI.Parity.Tests.Fixtures;
using Blazix.BaseUI.Parity.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Parity.Tests.Capture;

/// <summary>
/// Drives one fixture through its manifest steps on one leg and assembles the captures.
/// </summary>
public sealed class ParityCapturer
{
    // ParityOptions arrives in Task 12; until then the per-action budget is fixed here.
    // It bounds how long an unresolved selector costs, so it is generous enough for a
    // popup that has to portal out and short enough that a genuinely absent element does
    // not dominate the run.
    private const float ActionTimeoutMs = 5_000;

    private readonly AliasTable aliases = AliasTable.Load();

    /// <summary>
    /// Captures every manifest step for one fixture on one leg.
    /// </summary>
    /// <param name="page">A page belonging to the shared browser context.</param>
    /// <param name="fixture">The manifest entry to capture.</param>
    /// <param name="leg">Which side is being captured.</param>
    /// <returns>The assembled capture bundle.</returns>
    public async Task<CaptureBundle> CaptureAsync(IPage page, FixtureEntry fixture, ParityLeg leg)
    {
        var console = new List<string>();

        void OnConsole(object? sender, IConsoleMessage message)
        {
            if (message.Type is not ("error" or "warning"))
            {
                return;
            }

            // Playwright raises console events on its own dispatch loop, so the list is
            // written from a different thread than the one draining it between steps.
            lock (console)
            {
                console.Add($"{message.Type}: {message.Text}");
            }
        }

        await CaptureScript.InjectAsync(page);
        page.Console += OnConsole;

        try
        {
            await page.GotoAsync(BuildUrl(fixture, leg));
            await SettleProtocol.WaitAsync(page);

            var steps = new List<StepCapture>(fixture.Steps.Count);

            foreach (var step in fixture.Steps)
            {
                var unresolved = new List<string>();

                if (step.Settle == "animation")
                {
                    // Started before the actions so the events the actions provoke are
                    // inside the recording rather than ahead of it.
                    await page.EvaluateAsync(
                        "() => window[Symbol.for('Blazix.Parity.Capture')].startTimeline()");
                }

                foreach (var action in step.Do)
                {
                    await PerformAsync(page, fixture.Component, action, unresolved);
                }

                await SettleProtocol.WaitAsync(page);

                List<string> observed;
                lock (console)
                {
                    observed = [.. console];
                    console.Clear();
                }

                var capture = await CaptureScript.CaptureAsync(page, step.Name);

                steps.Add(capture with
                {
                    // Snapshotted from <body>, not from the fixture root: a portalled popup
                    // is a sibling of the root, and excluding it would hide exactly the
                    // part of the accessibility tree the floating fixtures exist to check.
                    Aria = await page.Locator("body").AriaSnapshotAsync(),
                    Console = observed,
                    UnresolvedSelectors = unresolved
                });
            }

            return new CaptureBundle
            {
                Fixture = fixture.Id,
                Leg = leg,
                Steps = steps
            };
        }
        finally
        {
            page.Console -= OnConsole;
        }
    }

    private static string BuildUrl(FixtureEntry fixture, ParityLeg leg)
    {
        var server = ParityServerAssemblyFixture.ServerAddress;

        return leg switch
        {
            ParityLeg.React => $"{server}/react/#/fixture/{fixture.Id}",
            ParityLeg.BlazorServer => $"{server}/fixture/{fixture.Component}/{fixture.Demo}/server",
            ParityLeg.BlazorWasm => $"{server}/fixture/{fixture.Component}/{fixture.Demo}/wasm",
            _ => throw new ArgumentOutOfRangeException(nameof(leg), leg, "Unknown parity leg.")
        };
    }

    private async Task PerformAsync(
        IPage page,
        string component,
        StepAction action,
        List<string> unresolved)
    {
        if (action.Key is { } key)
        {
            // Keyboard steps are dispatched to whatever currently holds focus, which is
            // the point: the composite components are driven through the focus they set.
            await page.Keyboard.PressAsync(key);
            return;
        }

        var (verb, selector) = Describe(action);
        var expanded = aliases.Expand(component, selector);
        var locator = page.Locator(expanded).First;

        try
        {
            // Attached, not visible: "the selector resolves" is a question about the
            // element existing, and probing for visibility would report a zero-size or
            // off-screen element as absent.
            await locator.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Attached,
                Timeout = ActionTimeoutMs
            });

            switch (verb)
            {
                case "click":
                    await locator.ClickAsync(new LocatorClickOptions { Timeout = ActionTimeoutMs });
                    break;
                case "hover":
                    await locator.HoverAsync(new LocatorHoverOptions { Timeout = ActionTimeoutMs });
                    break;
                case "type":
                    await locator.PressSequentiallyAsync(
                        action.Type!, new LocatorPressSequentiallyOptions { Timeout = ActionTimeoutMs });
                    break;
                case "focus":
                    await locator.FocusAsync(new LocatorFocusOptions { Timeout = ActionTimeoutMs });
                    break;
                case "blur":
                    await locator.BlurAsync(new LocatorBlurOptions { Timeout = ActionTimeoutMs });
                    break;
                case "scroll":
                    await locator.ScrollIntoViewIfNeededAsync(
                        new LocatorScrollIntoViewIfNeededOptions { Timeout = ActionTimeoutMs });
                    break;
                default:
                    // `wait` has no follow-up: the resolution probe above is the action.
                    break;
            }
        }
        catch (TimeoutException)
        {
            // Not a harness error. The selector is a role contract both implementations
            // are obliged to honour, so failing to reach the element — or reaching one
            // that cannot be driven, which is the same addressing failure seen a step
            // later — is the result, recorded for the comparators rather than thrown.
            // Anything else, a malformed selector in particular, still propagates.
            unresolved.Add(expanded);
        }
    }

    private static (string Verb, string Selector) Describe(StepAction action) => action switch
    {
        { Click: { } selector } => ("click", selector),
        { Hover: { } selector } => ("hover", selector),
        { Type: not null, Into: { } selector } => ("type", selector),
        { Focus: { } selector } => ("focus", selector),
        { Blur: { } selector } => ("blur", selector),
        { Scroll: { } selector } => ("scroll", selector),
        { Wait: { } selector } => ("wait", selector),
        _ => throw new FormatException(
            "A manifest step action must carry exactly one of click, hover, key, focus, " +
            "blur, scroll, wait, or type with into.")
    };
}
