using System.Diagnostics;
using System.Text.Json;
using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Fixtures;
using Blazix.BaseUI.Parity.Tests.Infrastructure;
using Microsoft.Playwright;
using Shouldly;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

/// <summary>
/// Pins the manifest action contract and its observable cross-mode completion fence.
/// </summary>
/// <param name="playwright">The browser fixture.</param>
public sealed class ActionCompletionTests(PlaywrightFixture playwright)
    : IClassFixture<PlaywrightFixture>, IDisposable
{
    private const int Samples = 20;

    private readonly string screenshots = Path.Combine(
        Path.GetTempPath(), "blazix-parity-completion", Guid.NewGuid().ToString("N"));

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(screenshots))
        {
            Directory.Delete(screenshots, recursive: true);
        }
    }

    [Fact]
    public void LoadsTheStrictPredicateVocabularyAndReasonedActionOnly()
    {
        var entries = FixtureManifest.Parse(Manifest(
            """
            {
              "click": "@trigger",
              "complete": [
                { "selector": "@popup", "state": "visible" },
                { "selector": "@trigger", "attribute": "aria-expanded", "equals": "true" },
                { "selector": "input", "property": "checked", "equals": "true" },
                { "selector": "@input", "inputValue": "typed" },
                { "selector": "@input", "focus": "not-equals" }
              ]
            },
            {
              "click": "[data-no-op]",
              "actionOnly": { "reason": "Purpose-built probe intentionally changes no observable state." }
            }
            """));

        var actions = entries.Single().Steps[1].Do;
        actions.Count.ShouldBe(2);
        actions[0].Complete!.Count.ShouldBe(5);
        actions[0].Complete![1].Expected.ShouldBe("true");
        actions[1].ActionOnly!.Reason!.ShouldContain("intentionally");
    }

    [Fact]
    public void RejectsMissingBothEmptyAndBlankCompletionContractsWithCoordinates()
    {
        var cases = new Dictionary<string, string>
        {
            ["{ \"click\": \"button\" }"] = "exactly one completion contract",
            ["{ \"click\": \"button\", \"complete\": [], \"actionOnly\": { \"reason\": \"no-op\" } }"] =
                "exactly one completion contract",
            ["{ \"click\": \"button\", \"complete\": [] }"] =
                "complete must contain at least one predicate",
            ["{ \"click\": \"button\", \"actionOnly\": { \"reason\": \"  \" } }"] =
                "actionOnly.reason must not be blank"
        };

        foreach (var (action, expected) in cases)
        {
            var failure = Should.Throw<FormatException>(() => FixtureManifest.Parse(Manifest(action)));

            failure.Message.ShouldContain("Manifest entry 0 ('harness/completion-contract-probe')");
            failure.Message.ShouldContain("step 1 ('action'), action 0");
            failure.Message.ShouldContain(expected);
        }
    }

    [Fact]
    public void RejectsMalformedPredicatesWithCoordinates()
    {
        var predicates = new Dictionary<string, string>
        {
            ["{ \"state\": \"visible\" }"] = "selector must not be blank",
            ["{ \"selector\": \"button\" }"] = "exactly one predicate discriminator",
            ["null"] = "predicate must be an object",
            ["{ \"selector\": \"button\", \"state\": \"visible\", \"focus\": \"equals\" }"] =
                "exactly one predicate discriminator",
            ["{ \"selector\": \"button\", \"state\": \"open\" }"] =
                "state must be attached, detached, visible, or hidden",
            ["{ \"selector\": \"button\", \"attribute\": \"aria-expanded\" }"] =
                "require a non-blank name and equals value",
            ["{ \"selector\": \"button\", \"property\": \"checked\" }"] =
                "require a non-blank name and equals value",
            ["{ \"selector\": \"button\", \"inputValue\": \"x\", \"equals\": \"x\" }"] =
                "equals is legal only with attribute or property",
            ["{ \"selector\": \"button\", \"focus\": \"near\" }"] =
                "focus must be equals or not-equals"
        };

        foreach (var (predicate, expected) in predicates)
        {
            var action = $"{{ \"click\": \"button\", \"complete\": [{predicate}] }}";
            var failure = Should.Throw<FormatException>(() => FixtureManifest.Parse(Manifest(action)));

            failure.Message.ShouldContain("step 1 ('action'), action 0, completion predicate 0");
            failure.Message.ShouldContain(expected);
        }
    }

    [Fact]
    public void RejectsUnknownFieldsAtTheExactActionAndPredicatePath()
    {
        var actionFailure = Should.Throw<JsonException>(() => FixtureManifest.Parse(Manifest(
            "{ \"click\": \"button\", \"mystery\": true, " +
            "\"actionOnly\": { \"reason\": \"no-op\" } }")));
        actionFailure.Path.ShouldBe("$[0].steps[1].do[0].mystery");

        var predicateFailure = Should.Throw<JsonException>(() => FixtureManifest.Parse(Manifest(
            "{ \"click\": \"button\", \"complete\": [{ " +
            "\"selector\": \"button\", \"state\": \"visible\", \"mystery\": true }] }")));
        predicateFailure.Path.ShouldBe("$[0].steps[1].do[0].complete[0].mystery");
    }

    [Fact]
    public void RejectsInvalidActionVerbShapesBeforeCapture()
    {
        var cases = new[]
        {
            "{ \"click\": \"button\", \"hover\": \"button\", \"actionOnly\": { \"reason\": \"probe\" } }",
            "{ \"type\": \"a\", \"actionOnly\": { \"reason\": \"probe\" } }",
            "{ \"into\": \"input\", \"actionOnly\": { \"reason\": \"probe\" } }",
            "{ \"click\": \"  \", \"actionOnly\": { \"reason\": \"probe\" } }"
        };

        foreach (var action in cases)
        {
            Should.Throw<FormatException>(() => FixtureManifest.Parse(Manifest(action)))
                .Message.ShouldContain("step 1 ('action'), action 0");
        }
    }

    [Fact]
    public void RejectsNullStepsDoListsAndActionsWithCoordinates()
    {
        var stepsFailure = Should.Throw<FormatException>(() => FixtureManifest.Parse(
            "[{\"id\":\"harness/probe\",\"component\":\"harness\"," +
            "\"react\":\"harness/demos/probe/tailwind/index.tsx\"," +
            "\"blazor\":\"Harness/Probe\",\"steps\":[null]}]"));
        stepsFailure.Message.ShouldBe(
            "Manifest entry 0 ('harness/probe'), step 0: step must be an object.");

        var doFailure = Should.Throw<FormatException>(() => FixtureManifest.Parse(
            "[{\"id\":\"harness/probe\",\"component\":\"harness\"," +
            "\"react\":\"harness/demos/probe/tailwind/index.tsx\"," +
            "\"blazor\":\"Harness/Probe\"," +
            "\"steps\":[{\"name\":\"broken\",\"do\":null}]}]"));
        doFailure.Message.ShouldBe(
            "Manifest entry 0 ('harness/probe'), step 0 ('broken'): do must be an array.");

        var actionFailure = Should.Throw<FormatException>(() => FixtureManifest.Parse(
            "[{\"id\":\"harness/probe\",\"component\":\"harness\"," +
            "\"react\":\"harness/demos/probe/tailwind/index.tsx\"," +
            "\"blazor\":\"Harness/Probe\"," +
            "\"steps\":[{\"name\":\"broken\",\"do\":[null]}]}]"));
        actionFailure.Message.ShouldBe(
            "Manifest entry 0 ('harness/probe'), step 0 ('broken'), action 0: " +
            "action must be an object.");
    }

    [Fact]
    public void SwitchHeroDeclaresItsObservableUncheckedState()
    {
        var action = FixtureManifest.Load()
            .Single(entry => entry.Id == "switch/hero")
            .Steps.Single(step => step.Name == "toggle-off")
            .Do.ShouldHaveSingleItem();

        var predicate = action.Complete.ShouldNotBeNull().ShouldHaveSingleItem();
        predicate.Selector.ShouldBe("[role='switch']");
        predicate.Attribute.ShouldBe("aria-checked");
        predicate.Expected.ShouldBe("false");
    }

    [Theory]
    [InlineData(ParityLeg.BlazorServer)]
    [InlineData(ParityLeg.BlazorWasm)]
    public async Task CompletesEveryPredicateShapeAndKeepsTimelinesPerStep(ParityLeg leg)
    {
        var fixture = ProbeFixture(
            Step("sync", Click("[data-action=sync]",
                Attribute("[data-completion-probe]", "data-sync-value", "1"),
                Attribute("@item(1)", "data-option", "1"))),
            Step("no-op", ActionOnlyClick("[data-action=noop]", "Intentional no-render probe.")),
            Step("focus", Focus("[data-action=input]", FocusPredicate("[data-action=input]", "equals"))),
            Step("type", Type("abc", "[data-action=input]", InputValue("[data-action=input]", "abc"))),
            Step("blur", Blur("[data-action=input]", FocusPredicate("[data-action=input]", "not-equals"))),
            Step("property", Click("[data-action=property]", Property(
                "[data-property-target]", "checked", "true"), Property(
                "[data-property-target]", "tabIndex", "0.0"))),
            Step("open", Click("[data-action=portal]",
                State("[data-probe-portal]", "attached"),
                State("[data-probe-portal]", "visible")), settle: "animation"),
            Step("close", Click("[data-action=portal]",
                State("[data-probe-portal]", "detached")), settle: "animation"),
            Step("hidden", Click("[data-action=hide]",
                State("[data-hide-target]", "hidden"))));

        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        var bundle = await new ParityCapturer(screenshots).CaptureAsync(page, fixture, leg, "light");

        bundle.Steps.SelectMany(step => step.ActionCompletionFailures).ShouldBeEmpty();
        (await page.Locator("[data-action=input]").InputValueAsync()).ShouldBe("abc");
        (await page.Locator("[data-property-target]").IsCheckedAsync()).ShouldBeTrue();
        (await page.Locator("[data-hide-target]").IsHiddenAsync()).ShouldBeTrue();

        bundle.Steps.Single(step => step.Step == "open").Timeline
            .ShouldContain(item => item.Kind == "added");
        bundle.Steps.Single(step => step.Step == "close").Timeline
            .ShouldContain(item => item.Kind == "removed");
        bundle.Steps.Single(step => step.Step == "no-op").Timeline.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(ParityLeg.BlazorServer)]
    [InlineData(ParityLeg.BlazorWasm)]
    public async Task DelayedAndDependentActionsCompleteTwentyTimesWithoutFalseEarlyReturns(
        ParityLeg leg)
    {
        var steps = new List<StepEntry>(Samples * 2);

        for (var sample = 1; sample <= Samples; sample++)
        {
            steps.Add(Step(
                $"delayed-{sample}",
                Click("[data-action=async]", Attribute(
                    "[data-completion-probe]", "data-async-value", sample.ToString()))));
            steps.Add(Step(
                $"dependent-{sample}",
                Click("[data-action=prepare]", State("[data-action=dependent]", "attached")),
                Click("[data-action=dependent]", Attribute(
                    "[data-completion-probe]", "data-dependent-value", sample.ToString()))));
        }

        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        var started = Stopwatch.GetTimestamp();
        var bundle = await new ParityCapturer(screenshots)
            .CaptureAsync(page, ProbeFixture([.. steps]), leg, "light");
        var elapsed = Stopwatch.GetElapsedTime(started);

        bundle.Steps.Count.ShouldBe(Samples * 2);
        bundle.Steps.SelectMany(step => step.ActionCompletionFailures).ShouldBeEmpty();

        for (var sample = 1; sample <= Samples; sample++)
        {
            Value(bundle, $"delayed-{sample}", "data-async-value").ShouldBe(sample.ToString());
            Value(bundle, $"dependent-{sample}", "data-dependent-value").ShouldBe(sample.ToString());
        }

        // Forty deliberately delayed descendants contribute at least ten seconds. This is
        // not the completion heuristic; it proves the run did not return inside either
        // 250 ms quiet gap while the exact final-state assertions above prove correctness.
        elapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromSeconds(9));
    }

    [Theory]
    [InlineData(ParityLeg.BlazorServer)]
    [InlineData(ParityLeg.BlazorWasm)]
    public async Task MissingConsequenceStopsTheDependentActionAndStillCapturesDiagnostics(
        ParityLeg leg)
    {
        var fixture = ProbeFixture(Step(
            "missing",
            Click("[data-action=missing]", Attribute(
                "[data-completion-probe]", "data-never", "1")),
            Click("[data-action=sync]", Attribute(
                "[data-completion-probe]", "data-sync-value", "1"))));

        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        var bundle = await new ParityCapturer(screenshots, completionTimeoutMs: 300)
            .CaptureAsync(page, fixture, leg, "light");
        var step = bundle.Steps.ShouldHaveSingleItem();
        var failure = step.ActionCompletionFailures.ShouldHaveSingleItem();

        step.Actions.ShouldBe(
        [
            new ActionExecution
            {
                ActionIndex = 0,
                Verb = "click",
                ExpandedSelector = "[data-action=missing]",
                Status = ActionExecutionStatus.CompletionUnmet
            },
            new ActionExecution
            {
                ActionIndex = 1,
                Verb = "click",
                ExpandedSelector = "[data-action=sync]",
                Status = ActionExecutionStatus.Skipped
            }
        ]);

        failure.Fixture.ShouldBe("harness/completion-contract-probe");
        failure.Leg.ShouldBe(leg);
        failure.Step.ShouldBe("missing");
        failure.ActionIndex.ShouldBe(0);
        failure.Verb.ShouldBe("click");
        failure.Selector.ShouldBe("[data-completion-probe]");
        failure.Predicate.ShouldBe("attribute:data-never");
        failure.ExpectedValue.ShouldBe("1");
        failure.Observed.Length.ShouldBeLessThanOrEqualTo(500);
        failure.Observed.ShouldContain("data-missing-value");

        Value(bundle, "missing", "data-missing-value").ShouldBe("1");
        Value(bundle, "missing", "data-sync-value").ShouldBe("0");
        step.Styles.ShouldNotBeEmpty();
        step.Screenshots.ShouldNotBeEmpty();
        step.Timeline.ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData(ParityLeg.BlazorServer)]
    [InlineData(ParityLeg.BlazorWasm)]
    public async Task AllOfPredicatesMustHoldAtTheSameTime(ParityLeg leg)
    {
        var fixture = ProbeFixture(Step(
            "transient",
            Click("[data-action=transient]",
                Attribute("[data-completion-probe]", "data-phase", "first"),
                Attribute("[data-completion-probe]", "data-phase", "second"))));

        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        var bundle = await new ParityCapturer(screenshots, completionTimeoutMs: 300)
            .CaptureAsync(page, fixture, leg, "light");

        var failure = bundle.Steps.ShouldHaveSingleItem()
            .ActionCompletionFailures.ShouldHaveSingleItem();
        failure.Predicate.ShouldBe("attribute:data-phase");
        failure.ExpectedValue.ShouldBe("first");
        Value(bundle, "transient", "data-phase").ShouldBe("second");
    }

    [Theory]
    [InlineData(ParityLeg.BlazorServer)]
    [InlineData(ParityLeg.BlazorWasm)]
    public async Task FocusNotEqualsRequiresTheDeclaredTargetToResolve(ParityLeg leg)
    {
        var fixture = ProbeFixture(Step(
            "missing-focus-target",
            Click("[data-action=sync]",
                FocusPredicate("[data-does-not-exist]", "not-equals"))));

        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        var bundle = await new ParityCapturer(screenshots, completionTimeoutMs: 300)
            .CaptureAsync(page, fixture, leg, "light");

        var failure = bundle.Steps.ShouldHaveSingleItem()
            .ActionCompletionFailures.ShouldHaveSingleItem();
        failure.Leg.ShouldBe(leg);
        failure.Predicate.ShouldBe("focus");
        failure.ExpectedValue.ShouldBe("not-equals");
        failure.Selector.ShouldBe("[data-does-not-exist]");
        failure.Observed.ShouldContain("\"matches\":0");
    }

    [Theory]
    [InlineData(ParityLeg.BlazorServer)]
    [InlineData(ParityLeg.BlazorWasm)]
    public async Task RapidAlternationIsObservedOnlyThroughAtomicPredicateSnapshots(ParityLeg leg)
    {
        var fixture = ProbeFixture(Step(
            "rapid-alternation",
            Click("[data-action=alternate]",
                Attribute("[data-completion-probe]", "data-atomic-a", "true"),
                Attribute("[data-completion-probe]", "data-atomic-b", "true"))));

        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.AddInitScriptAsync(
            """
            (() => {
              const nativeGetAttribute = Element.prototype.getAttribute;
              window.__atomicEpoch = 0;
              window.__atomicReads = [];
              Element.prototype.getAttribute = function(name) {
                const value = nativeGetAttribute.call(this, name);
                const stack = new Error().stack ?? '';
                if ((name === 'data-atomic-a' || name === 'data-atomic-b') &&
                    stack.includes('completionSnapshot')) {
                  window.__atomicReads.push({ name, epoch: window.__atomicEpoch });
                  if (name === 'data-atomic-a') {
                    queueMicrotask(() => { window.__atomicEpoch++; });
                  }
                }
                return value;
              };
            })();
            """);

        var bundle = await new ParityCapturer(screenshots, completionTimeoutMs: 350)
            .CaptureAsync(page, fixture, leg, "light");
        bundle.Steps.ShouldHaveSingleItem().ActionCompletionFailures.ShouldHaveSingleItem();

        var reads = await page.EvaluateAsync<AtomicRead[]>("() => window.__atomicReads");
        reads.Length.ShouldBeGreaterThan(2);
        (reads.Length % 2).ShouldBe(0);

        for (var index = 0; index < reads.Length; index += 2)
        {
            reads[index].Name.ShouldBe("data-atomic-a");
            reads[index + 1].Name.ShouldBe("data-atomic-b");
            reads[index].Epoch.ShouldBe(reads[index + 1].Epoch);
        }

        var probe = bundle.Steps.Single().Dom.Descendants()
            .Single(node => node.Attributes.ContainsKey("data-completion-probe"));
        (probe.Attributes["data-atomic-a"] == "true" &&
            probe.Attributes["data-atomic-b"] == "true").ShouldBeFalse();
    }

    [Theory]
    [InlineData(ParityLeg.BlazorServer)]
    [InlineData(ParityLeg.BlazorWasm)]
    public async Task TimelineCleanupPreservesThePrimaryFailureAndLeavesNoListeners(
        ParityLeg leg)
    {
        var fixture = ProbeFixture(Step(
            "fault",
            Click("[", State("[data-completion-probe]", "attached"))));

        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await CaptureScript.InjectAsync(page);
        await page.AddInitScriptAsync(
            """
            (() => {
              const api = window[Symbol.for('Blazix.Parity.Capture')];
              api.__originalStopTimeline = api.stopTimeline;
              api.stopTimeline = () => {
                api.__originalStopTimeline();
                throw new Error('injected timeline stop failure');
              };
            })();
            """);

        var primary = await Should.ThrowAsync<PlaywrightException>(() =>
            new ParityCapturer(screenshots).CaptureAsync(page, fixture, leg, "light"));

        primary.Message.ShouldNotContain("injected timeline stop failure");
        var cleanup = primary.Data["ParityTimelineStopFailure"]
            .ShouldBeOfType<PlaywrightException>();
        cleanup.Message.ShouldContain("injected timeline stop failure");
        (await page.EvaluateAsync<bool>(
            "() => window[Symbol.for('Blazix.Parity.Capture')].timelineActive()"))
            .ShouldBeFalse();

        await page.EvaluateAsync(
            """
            () => {
              const api = window[Symbol.for('Blazix.Parity.Capture')];
              api.stopTimeline = api.__originalStopTimeline;
              api.startTimeline();
            }
            """);
        await page.Locator("[data-action=sync]").ClickAsync();
        await SettleProtocol.WaitAsync(page);
        await page.EvaluateAsync(
            "() => window[Symbol.for('Blazix.Parity.Capture')].stopTimeline()");

        var after = await CaptureScript.CaptureAsync(page, "after-fault");
        after.Timeline.Count(item =>
            item.Kind == "attribute" && item.Attr == "data-sync-value").ShouldBe(1);
        (await page.EvaluateAsync<bool>(
            "() => window[Symbol.for('Blazix.Parity.Capture')].timelineActive()"))
            .ShouldBeFalse();
    }

    private static StepAction ActionOnlyClick(string selector, string reason) => new()
    {
        Click = selector,
        ActionOnly = new ActionOnlyEntry { Reason = reason }
    };

    private static CompletionPredicate Attribute(string selector, string name, string expected) => new()
    {
        Selector = selector,
        Attribute = name,
        Expected = expected
    };

    private static StepAction Blur(string selector, CompletionPredicate predicate) => new()
    {
        Blur = selector,
        Complete = [predicate]
    };

    private static StepAction Click(string selector, params CompletionPredicate[] predicates) => new()
    {
        Click = selector,
        Complete = predicates
    };

    private static CompletionPredicate FocusPredicate(string selector, string comparison) => new()
    {
        Selector = selector,
        Focus = comparison
    };

    private static StepAction Focus(string selector, CompletionPredicate predicate) => new()
    {
        Focus = selector,
        Complete = [predicate]
    };

    private static CompletionPredicate InputValue(string selector, string expected) => new()
    {
        Selector = selector,
        InputValue = expected
    };

    private static string Manifest(params string[] actions)
        =>
            "[{" +
            "\"id\":\"harness/completion-contract-probe\"," +
            "\"component\":\"harness\"," +
            "\"react\":\"harness/demos/completion-contract-probe/tailwind/index.tsx\"," +
            "\"blazor\":\"Harness/CompletionContractProbe\"," +
            "\"steps\":[{\"name\":\"initial\"},{\"name\":\"action\",\"do\":[" +
            string.Join(',', actions) +
            "]}]}]";

    private static FixtureEntry ProbeFixture(params StepEntry[] steps) => new()
    {
        Id = "harness/completion-contract-probe",
        Component = "harness",
        React = "internal:none",
        Blazor = "Harness/CompletionContractProbe",
        Steps = steps
    };

    private static CompletionPredicate Property(string selector, string name, string expected) => new()
    {
        Selector = selector,
        Property = name,
        Expected = expected
    };

    private static CompletionPredicate State(string selector, string state) => new()
    {
        Selector = selector,
        State = state
    };

    private static StepEntry Step(
        string name,
        StepAction action,
        string settle = "render") => new()
        {
            Name = name,
            Do = [action],
            Settle = settle
        };

    private static StepEntry Step(
        string name,
        StepAction first,
        StepAction second) => new()
        {
            Name = name,
            Do = [first, second]
        };

    private static StepAction Type(
        string text,
        string selector,
        CompletionPredicate predicate) => new()
        {
            Type = text,
            Into = selector,
            Complete = [predicate]
        };

    private static string Value(CaptureBundle bundle, string step, string attribute)
        => bundle.Steps.Single(capture => capture.Step == step).Dom.Descendants()
            .Single(node => node.Attributes.ContainsKey("data-completion-probe"))
            .Attributes[attribute];

    private sealed record AtomicRead
    {
        public required string Name { get; init; }

        public required int Epoch { get; init; }
    }
}
