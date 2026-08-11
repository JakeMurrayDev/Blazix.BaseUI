using Blazix.BaseUI.Parity.Tests.Fixtures;
using Blazix.BaseUI.Parity.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Parity.Tests.Capture;

/// <summary>
/// Drives one fixture through its manifest steps on one leg and assembles the captures.
/// </summary>
/// <param name="screenshotDirectory">Where this run's screenshots are written.</param>
/// <param name="completionTimeoutMs">The deadline for each action's all-of consequence.</param>
public sealed class ParityCapturer(string screenshotDirectory, float completionTimeoutMs = 5_000)
{
    // ParityOptions arrives in Task 12; until then the per-action budget is fixed here.
    // It bounds how long an unresolved selector costs, so it is generous enough for a
    // popup that has to portal out and short enough that a genuinely absent element does
    // not dominate the run.
    private const float ActionTimeoutMs = 5_000;

    private readonly AliasTable aliases = AliasTable.Load();

    /// <summary>
    /// Writes screenshots under the harness's own screenshot directory.
    /// </summary>
    public ParityCapturer()
        : this(ParityPaths.Screenshots)
    {
    }

    /// <summary>
    /// Captures every manifest step for one fixture on one leg.
    /// </summary>
    /// <param name="page">A page belonging to the shared browser context.</param>
    /// <param name="fixture">The manifest entry to capture.</param>
    /// <param name="leg">Which side is being captured.</param>
    /// <returns>The assembled capture bundle.</returns>
    public async Task<CaptureBundle> CaptureAsync(
        IPage page,
        FixtureEntry fixture,
        ParityLeg leg,
        string theme)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentException.ThrowIfNullOrWhiteSpace(theme);
        if (theme is not ("light" or "dark") ||
            !fixture.Themes.Contains(theme, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                $"Theme '{theme}' is not declared and supported for fixture '{fixture.Id}'.",
                nameof(theme));
        }

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
        await page.EmulateMediaAsync(new PageEmulateMediaOptions
        {
            ColorScheme = theme == "dark" ? ColorScheme.Dark : ColorScheme.Light
        });
        page.Console += OnConsole;

        try
        {
            await page.GotoAsync(BuildUrl(fixture, leg));
            await SettleProtocol.WaitAsync(page);

            var steps = new List<StepCapture>(fixture.Steps.Count);

            foreach (var step in fixture.Steps)
            {
                var completionFailures = new List<ActionCompletionFailure>();
                var actions = step.Do
                    .Select((action, index) => ActionExecutionContract.Expected(
                        aliases,
                        fixture.Component,
                        action,
                        index,
                        ActionExecutionStatus.Skipped))
                    .ToList();

                // Every step owns a fresh recording, including non-animation steps. Timeline
                // comparison must not inherit the previous animation step's events or miss a
                // mutation merely because that step does not request seeked screenshots.
                await page.EvaluateAsync(
                    "() => window[Symbol.for('Blazix.Parity.Capture')].startTimeline()");
                Exception? stepFailure = null;

                try
                {
                    for (var actionIndex = 0; actionIndex < step.Do.Count; actionIndex++)
                    {
                        var action = step.Do[actionIndex];
                        var dispatch = await PerformAsync(
                            page, action, actions[actionIndex].ExpandedSelector);

                        // Resolution and actionability are their own typed capture facts. The action
                        // never dispatched in either case, so waiting for its consequence would add a
                        // misleading completion failure. Later actions still cannot safely run from
                        // this intermediate state.
                        if (dispatch != ActionDispatch.Completed)
                        {
                            actions[actionIndex] = actions[actionIndex] with
                            {
                                Status = dispatch == ActionDispatch.Unresolved
                                    ? ActionExecutionStatus.Unresolved
                                    : ActionExecutionStatus.NonActionable
                            };
                            break;
                        }

                        var completionFailure = await AwaitCompletionAsync(
                            page, fixture, leg, step, action, actionIndex);

                        if (completionFailure is not null)
                        {
                            completionFailures.Add(completionFailure);
                            actions[actionIndex] = actions[actionIndex] with
                            {
                                Status = ActionExecutionStatus.CompletionUnmet
                            };
                            break;
                        }

                        actions[actionIndex] = actions[actionIndex] with
                        {
                            Status = ActionExecutionStatus.Completed
                        };
                    }

                    if (step.Settle == "animation")
                    {
                        await AnimationSettleProtocol.WaitAsync(page);
                    }
                    else
                    {
                        await SettleProtocol.WaitAsync(page);
                    }
                }
                catch (Exception exception)
                {
                    stepFailure = exception;
                    throw;
                }
                finally
                {
                    try
                    {
                        await page.EvaluateAsync(
                            "() => window[Symbol.for('Blazix.Parity.Capture')].stopTimeline()");
                    }
                    catch (Exception cleanupFailure) when (stepFailure is not null)
                    {
                        // The action/completion failure is the reason the step failed. Keep it as
                        // the thrown exception while retaining timeline cleanup diagnostics for the
                        // caller instead of replacing the primary fault with a secondary one.
                        stepFailure.Data["ParityTimelineStopFailure"] = cleanupFailure;
                    }
                }

                List<string> observed;
                lock (console)
                {
                    observed = [.. console];
                    console.Clear();
                }

                var capture = await CaptureScript.CaptureAsync(page, step.Name);

                // The ordinary shot belongs to the natural terminal state. Deterministic
                // animation fractions are captured later on disposable replay pages: seeking
                // this authoritative page can resolve animation.finished callbacks and mutate
                // both the current state and every step after it.
                var screenshots = await ScreenshotSet.CaptureCanonicalAsync(
                    page,
                    screenshotDirectory,
                    fixture.Id,
                    theme,
                    leg,
                    step.Name);

                steps.Add(capture with
                {
                    // Snapshotted from <body>, not from the fixture root: a portalled popup
                    // is a sibling of the root, and excluding it would hide exactly the
                    // part of the accessibility tree the floating fixtures exist to check.
                    Aria = await page.Locator("body").AriaSnapshotAsync(),
                    Console = observed,
                    ScreenshotObservations = screenshots,
                    Actions = actions,
                    ActionCompletionFailures = completionFailures
                });
            }

            await AttachAnimationFramesAsync(page.Context, fixture, leg, theme, steps);

            return new CaptureBundle
            {
                CaptureSchemaVersion = CaptureSchema.CurrentVersion,
                Fixture = fixture.Id,
                Leg = leg,
                Theme = theme,
                Steps = steps
            };
        }
        finally
        {
            page.Console -= OnConsole;
        }
    }

    private async Task AttachAnimationFramesAsync(
        IBrowserContext context,
        FixtureEntry fixture,
        ParityLeg leg,
        string theme,
        List<StepCapture> captures)
    {
        for (var stepIndex = 0; stepIndex < fixture.Steps.Count; stepIndex++)
        {
            if (fixture.Steps[stepIndex].Settle != "animation")
            {
                continue;
            }

            var frameResult = await CaptureAnimationFramesAsync(
                context, fixture, leg, theme, stepIndex);
            captures[stepIndex] = captures[stepIndex] with
            {
                ScreenshotObservations =
                [
                    .. captures[stepIndex].ScreenshotObservations,
                    .. frameResult.Observations
                ],
                AnimationFrameCaptureFailures = frameResult.Failure is null
                    ? captures[stepIndex].AnimationFrameCaptureFailures
                    : [.. captures[stepIndex].AnimationFrameCaptureFailures, frameResult.Failure]
            };
        }
    }

    private async Task<AnimationFrameCaptureResult> CaptureAnimationFramesAsync(
        IBrowserContext context,
        FixtureEntry fixture,
        ParityLeg leg,
        string theme,
        int targetStepIndex)
    {
        IPage? replay = null;
        var stage = "select-target-action";
        int? currentActionIndex = null;
        IReadOnlyList<ScreenshotObservation> observations = [];
        AnimationFrameCaptureFailure? failure = null;

        try
        {
            var targetActions = fixture.Steps[targetStepIndex].Do;
            int? targetActionIndex = targetActions.Count switch
            {
                0 => null,
                1 => 0,
                _ => await FindLastAnimatedActionAsync(
                    context, fixture, leg, theme, targetStepIndex)
            };

            stage = "create-page";
            replay = await context.NewPageAsync();
            stage = "inject-capture-script";
            await CaptureScript.InjectAsync(replay);
            stage = "emulate-theme";
            await replay.EmulateMediaAsync(new PageEmulateMediaOptions
            {
                ColorScheme = theme == "dark" ? ColorScheme.Dark : ColorScheme.Light
            });
            stage = "navigate";
            await replay.GotoAsync(BuildUrl(fixture, leg));
            stage = "initial-settle";
            await SettleProtocol.WaitAsync(replay);

            for (var stepIndex = 0; stepIndex <= targetStepIndex; stepIndex++)
            {
                var step = fixture.Steps[stepIndex];
                var actions = step.Do
                    .Select((action, index) => ActionExecutionContract.Expected(
                        aliases,
                        fixture.Component,
                        action,
                        index,
                        ActionExecutionStatus.Skipped))
                    .ToArray();

                for (var actionIndex = 0; actionIndex < step.Do.Count; actionIndex++)
                {
                    currentActionIndex = actionIndex;
                    var action = step.Do[actionIndex];
                    var targetAction = stepIndex == targetStepIndex &&
                        actionIndex == targetActionIndex;
                    var priorAnimationAction = stepIndex < targetStepIndex &&
                        step.Settle == "animation";

                    if (targetAction)
                    {
                        stage = "begin-animation-probe";
                        await CaptureScript.BeginAnimationProbeAsync(replay);
                    }
                    else if (priorAnimationAction)
                    {
                        // A prior animation step can publish its visible/expanded consequence
                        // before a relocated popup transition registers. Observe before dispatch
                        // so a short transition cannot escape the natural-terminal replay fence.
                        // Timers remain native because this probe is only a settle fence; the
                        // target action's probe is the one allowed to freeze lifecycle timers.
                        stage = "begin-prior-animation-probe";
                        await replay.EvaluateAsync(
                            "() => window[Symbol.for('Blazix.Parity.Capture')]" +
                            ".beginAnimationProbe(false)");
                    }

                    stage = "dispatch-action";
                    var dispatch = await PerformAsync(
                        replay, action, actions[actionIndex].ExpandedSelector);

                    if (dispatch != ActionDispatch.Completed)
                    {
                        throw new InvalidOperationException(
                            $"Replay action {actionIndex} was {dispatch}.");
                    }

                    if (targetAction)
                    {
                        stage = "animation-registration";
                        if (await AwaitAnimationRegistrationAsync(
                                replay, fixture, step, action, actionIndex) == 0)
                        {
                            observations = [];
                            goto ReplayComplete;
                        }

                        stage = "capture-frames";
                        observations = await ScreenshotSet.CaptureFramesAsync(
                            replay,
                            screenshotDirectory,
                            fixture.Id,
                            theme,
                            leg,
                            step.Name);
                        goto ReplayComplete;
                    }

                    stage = "action-completion";
                    if (await AwaitCompletionAsync(
                            replay, fixture, leg, step, action, actionIndex) is not null)
                    {
                        throw new InvalidOperationException(
                            $"Replay action {actionIndex} did not reach its declared completion.");
                    }

                    if (priorAnimationAction)
                    {
                        stage = "prior-animation-registration";
                        _ = await AwaitAnimationRegistrationAsync(
                            replay, fixture, step, action, actionIndex);
                        stage = "resume-prior-animation";
                        _ = await replay.EvaluateAsync<int>(
                            "() => window[Symbol.for('Blazix.Parity.Capture')]" +
                            ".resumeAnimations()");
                    }
                }

                if (stepIndex == targetStepIndex)
                {
                    currentActionIndex = null;
                    stage = "select-current-animations";
                    await CaptureScript.SelectCurrentAnimationsAsync(replay);
                    stage = "capture-frames";
                    observations = await ScreenshotSet.CaptureFramesAsync(
                        replay,
                        screenshotDirectory,
                        fixture.Id,
                        theme,
                        leg,
                        step.Name);
                    goto ReplayComplete;
                }

                currentActionIndex = null;
                stage = "prior-step-settle";
                if (step.Settle == "animation")
                {
                    await AnimationSettleProtocol.WaitAsync(replay);
                }
                else
                {
                    await SettleProtocol.WaitAsync(replay);
                }
            }
        ReplayComplete:;
        }
        catch (Exception exception)
        {
            failure = new AnimationFrameCaptureFailure
            {
                Stage = stage,
                ActionIndex = currentActionIndex,
                Detail = Bounded(exception.Message),
                CleanupDetail = exception.Data["ParityAnimationTargetProbeCloseFailure"] is
                    Exception cleanupException
                        ? Bounded(cleanupException.Message)
                        : null
            };
        }
        finally
        {
            if (replay is not null)
            {
                try
                {
                    await replay.CloseAsync();
                }
                catch (Exception cleanupException)
                {
                    if (failure is null)
                    {
                        failure = new AnimationFrameCaptureFailure
                        {
                            Stage = "replay-close",
                            ActionIndex = currentActionIndex,
                            Detail = Bounded(cleanupException.Message)
                        };
                    }
                    else
                    {
                        failure = failure with { CleanupDetail = Bounded(cleanupException.Message) };
                    }
                }
            }
        }

        return new AnimationFrameCaptureResult(observations, failure);
    }

    private async Task<int?> FindLastAnimatedActionAsync(
        IBrowserContext context,
        FixtureEntry fixture,
        ParityLeg leg,
        string theme,
        int targetStepIndex)
    {
        var replay = await context.NewPageAsync();
        return await PreservePrimaryDuringCleanupAsync(
            async () =>
        {
            await CaptureScript.InjectAsync(replay);
            await replay.EmulateMediaAsync(new PageEmulateMediaOptions
            {
                ColorScheme = theme == "dark" ? ColorScheme.Dark : ColorScheme.Light
            });
            await replay.GotoAsync(BuildUrl(fixture, leg));
            await SettleProtocol.WaitAsync(replay);

            int? lastAnimatedAction = null;

            for (var stepIndex = 0; stepIndex <= targetStepIndex; stepIndex++)
            {
                var step = fixture.Steps[stepIndex];
                var actions = step.Do
                    .Select((action, index) => ActionExecutionContract.Expected(
                        aliases,
                        fixture.Component,
                        action,
                        index,
                        ActionExecutionStatus.Skipped))
                    .ToArray();

                for (var actionIndex = 0; actionIndex < step.Do.Count; actionIndex++)
                {
                    var action = step.Do[actionIndex];
                    if (stepIndex == targetStepIndex)
                    {
                        // This page discovers which action moved and must subsequently
                        // reach that action's terminal consequence. Only the separate
                        // frame replay freezes action-owned lifecycle fallback timers.
                        await replay.EvaluateAsync(
                            "() => window[Symbol.for('Blazix.Parity.Capture')]" +
                            ".beginAnimationProbe(false)");
                    }

                    if (await PerformAsync(
                            replay, action, actions[actionIndex].ExpandedSelector) !=
                        ActionDispatch.Completed)
                    {
                        return null;
                    }

                    if (stepIndex == targetStepIndex)
                    {
                        var registered = await AwaitAnimationRegistrationAsync(
                            replay, fixture, step, action, actionIndex);
                        if (registered > 0)
                        {
                            lastAnimatedAction = actionIndex;
                        }

                        // Target-action discovery must let this disposable probe reach the
                        // action's declared consequence before it can drive the next action.
                        // Registration now freezes component-owned clocks immediately, so
                        // release those clocks here; the separate frame replay intentionally
                        // keeps its selected set frozen until that page is discarded.
                        await replay.EvaluateAsync<int>(
                            "() => window[Symbol.for('Blazix.Parity.Capture')]" +
                            ".resumeAnimations()");
                    }

                    if (await AwaitCompletionAsync(
                            replay, fixture, leg, step, action, actionIndex) is not null)
                    {
                        return null;
                    }
                }

                if (stepIndex == targetStepIndex)
                {
                    return lastAnimatedAction;
                }

                if (step.Settle == "animation")
                {
                    await AnimationSettleProtocol.WaitAsync(replay);
                }
                else
                {
                    await SettleProtocol.WaitAsync(replay);
                }
            }

            return lastAnimatedAction;
        },
            () => replay.CloseAsync());
    }

    internal static async Task<T> PreservePrimaryDuringCleanupAsync<T>(
        Func<Task<T>> operation,
        Func<Task> cleanup)
    {
        Exception? primary = null;
        try
        {
            return await operation();
        }
        catch (Exception exception)
        {
            primary = exception;
            throw;
        }
        finally
        {
            try
            {
                await cleanup();
            }
            catch (Exception cleanupException) when (primary is not null)
            {
                primary.Data["ParityAnimationTargetProbeCloseFailure"] = cleanupException;
            }
        }
    }

    private async Task<int> AwaitAnimationRegistrationAsync(
        IPage page,
        FixtureEntry fixture,
        StepEntry step,
        StepAction action,
        int actionIndex)
    {
        if (action.ActionOnly is not null)
        {
            return await CaptureScript.WaitForAnimationRegistrationAsync(
                page, [], completionTimeoutMs);
        }

        if (action.Complete is not { Count: > 0 } predicates)
        {
            throw new FormatException(
                $"Fixture '{fixture.Id}', step '{step.Name}', action {actionIndex}: " +
                "a non-empty complete list is required for animation replay.");
        }

        var descriptors = predicates.Select(predicate =>
        {
            var selector = aliases.Expand(fixture.Component, predicate.Selector!);
            var browserSelector = BrowserSelector(predicate.Selector!, selector);
            return CompletionDescriptor(
                predicate,
                browserSelector.Css,
                browserSelector.Index);
        }).ToArray();

        return await CaptureScript.WaitForAnimationRegistrationAsync(
            page, descriptors, completionTimeoutMs);
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

    private async Task<ActionDispatch> PerformAsync(
        IPage page,
        StepAction action,
        string? expandedSelector)
    {
        if (action.Key is { } key)
        {
            // Keyboard steps are dispatched to whatever currently holds focus, which is
            // the point: the composite components are driven through the focus they set.
            await page.Keyboard.PressAsync(key);
            return ActionDispatch.Completed;
        }

        var verb = ActionExecutionContract.Verb(action);
        var locator = page.Locator(expandedSelector!).First;

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
        }
        catch (TimeoutException)
        {
            // Not a harness error. The selector is a role contract both implementations
            // are obliged to honour, so one leg failing to expose the element at all is
            // the result, recorded for the comparators rather than thrown. Anything else,
            // a malformed selector in particular, still propagates.
            return ActionDispatch.Unresolved;
        }

        // Scoped apart from the probe above on purpose. A timeout here means the element
        // resolved and could not be driven — zero-size, covered, pointer-events: none —
        // which is a layout or interaction difference, not an addressing one. Recording
        // both in the same list would mislabel it and, worse, let two legs that are
        // non-actionable for unrelated reasons produce identical lists and cancel out
        // into a false report of parity.
        try
        {
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
            // Skipped rather than thrown, exactly as an unresolved selector is: the run
            // continues and the step is captured in whatever state the failed action
            // left it.
            return ActionDispatch.NonActionable;
        }

        return ActionDispatch.Completed;
    }

    private async Task<ActionCompletionFailure?> AwaitCompletionAsync(
        IPage page,
        FixtureEntry fixture,
        ParityLeg leg,
        StepEntry step,
        StepAction action,
        int actionIndex)
    {
        if (action.ActionOnly is not null)
        {
            if (action.Complete is not null)
            {
                throw new FormatException(
                    $"Fixture '{fixture.Id}', step '{step.Name}', action {actionIndex}: " +
                    "complete and actionOnly cannot both be declared.");
            }

            if (string.IsNullOrWhiteSpace(action.ActionOnly.Reason))
            {
                throw new FormatException(
                    $"Fixture '{fixture.Id}', step '{step.Name}', action {actionIndex}: " +
                    "actionOnly.reason must not be blank.");
            }

            return null;
        }

        if (action.Complete is not { Count: > 0 } predicates)
        {
            throw new FormatException(
                $"Fixture '{fixture.Id}', step '{step.Name}', action {actionIndex}: " +
                "a non-empty complete list or actionOnly declaration is required.");
        }

        var expanded = predicates
            .Select(predicate =>
            {
                var selector = aliases.Expand(fixture.Component, predicate.Selector!);
                return (
                    Predicate: predicate,
                    Selector: selector,
                    BrowserSelector: BrowserSelector(predicate.Selector!, selector));
            })
            .ToArray();

        var descriptors = expanded.Select(item => CompletionDescriptor(
            item.Predicate,
            item.BrowserSelector.Css,
            item.BrowserSelector.Index)).ToArray();
        var result = await CaptureScript.WaitForCompletionAsync(
            page, descriptors, completionTimeoutMs);

        if (result.GetProperty("completed").GetBoolean())
        {
            return null;
        }

        var unmetIndex = result.GetProperty("unmetIndex").GetInt32();
        var unmet = expanded[unmetIndex];

        return new ActionCompletionFailure
        {
            Fixture = fixture.Id,
            Leg = leg,
            Step = step.Name,
            ActionIndex = actionIndex,
            Verb = ActionExecutionContract.Verb(action),
            Selector = unmet.Selector,
            Predicate = PredicateName(unmet.Predicate),
            ExpectedValue = ExpectedValue(unmet.Predicate),
            Observed = result.GetProperty("observed").GetString() ?? "{}"
        };
    }

    private static (string Css, int Index) BrowserSelector(string original, string expanded)
    {
        if (!original.StartsWith('@'))
        {
            return (expanded, 0);
        }

        const string prefix = ":nth-match(";

        if (!expanded.StartsWith(prefix, StringComparison.Ordinal) ||
            !expanded.EndsWith(')'))
        {
            return (expanded, 0);
        }

        var body = expanded[prefix.Length..^1];
        var separator = body.LastIndexOf(", ", StringComparison.Ordinal);

        if (separator < 0 ||
            !int.TryParse(
                body[(separator + 2)..],
                System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture,
                out var oneBasedIndex) ||
            oneBasedIndex < 1)
        {
            throw new FormatException(
                $"Alias expansion '{expanded}' is not a valid Playwright :nth-match selector.");
        }

        return (body[..separator], oneBasedIndex - 1);
    }

    private static IReadOnlyDictionary<string, object?> CompletionDescriptor(
        CompletionPredicate predicate,
        string css,
        int index) => new Dictionary<string, object?>
        {
            ["selector"] = new Dictionary<string, object?>
            {
                ["css"] = css,
                ["index"] = index
            },
            ["kind"] = predicate switch
            {
                { State: { } value } => value,
                { Attribute: not null } => "attribute",
                { Property: not null } => "property",
                { InputValue: not null } => "input-value",
                { Focus: "equals" } => "focus-equals",
                { Focus: "not-equals" } => "focus-not-equals",
                _ => throw new FormatException("Unknown completion predicate.")
            },
            ["name"] = predicate.Attribute ?? predicate.Property,
            ["expected"] = predicate.Property is null
                ? predicate.Expected ?? predicate.InputValue
                : PropertyExpected(predicate.Expected!)
        };

    internal static string ExpectedValue(CompletionPredicate predicate) => predicate switch
    {
        { State: { } value } => value,
        { Expected: { } value } => value,
        { InputValue: { } value } => value,
        { Focus: { } value } => value,
        _ => string.Empty
    };

    internal static string PredicateName(CompletionPredicate predicate) => predicate switch
    {
        { State: not null } => "state",
        { Attribute: { } value } => $"attribute:{value}",
        { Property: { } value } => $"property:{value}",
        { InputValue: not null } => "input-value",
        { Focus: not null } => "focus",
        _ => "unknown"
    };

    private static object PropertyExpected(string expected)
        => expected switch
        {
            _ when bool.TryParse(expected, out var boolValue) => boolValue,
            _ when double.TryParse(
                expected,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var numberValue) => numberValue,
            _ => expected
        };

    private static string Bounded(string detail)
        => detail.Length <= 500 ? detail : detail[..500];

    private sealed record AnimationFrameCaptureResult(
        IReadOnlyList<ScreenshotObservation> Observations,
        AnimationFrameCaptureFailure? Failure);

    private enum ActionDispatch
    {
        Completed,
        Unresolved,
        NonActionable
    }
}
