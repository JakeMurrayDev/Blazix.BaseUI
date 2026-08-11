using System.Text.Json;
using System.Text.Json.Nodes;
using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Diff;
using Blazix.BaseUI.Parity.Tests.Infrastructure;
using Shouldly;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

/// <summary>Pins the current capture schema and its canonical per-action trace grammar.</summary>
public sealed class ActionExecutionTests
{
    [Fact]
    public void CaptureSchemaSerializesCanonicalTraceAndRejectsAdversarialJson()
    {
        var capture = Bundle(ParityLeg.React, Step(
            [Action(0, "key", null, ActionExecutionStatus.Completed)]));
        var json = CaptureSchema.Serialize(capture);

        json.ShouldContain($"\"captureSchemaVersion\":{CaptureSchema.CurrentVersion}");
        json.ShouldContain(
            "\"actionIndex\":0,\"verb\":\"key\",\"expandedSelector\":null," +
            "\"status\":\"Completed\"");
        var roundTrip = CaptureSchema.Deserialize(json);
        roundTrip.CaptureSchemaVersion.ShouldBe(CaptureSchema.CurrentVersion);
        roundTrip.Fixture.ShouldBe(capture.Fixture);
        roundTrip.Leg.ShouldBe(capture.Leg);
        roundTrip.Steps.ShouldHaveSingleItem().Actions.ShouldBe(capture.Steps[0].Actions);

        var missingSchema = JsonNode.Parse(json)!.AsObject();
        missingSchema.Remove("captureSchemaVersion");
        var missingActions = JsonNode.Parse(json)!.AsObject();
        missingActions["steps"]![0]!.AsObject().Remove("actions");
        var missingScreenshotObservations = JsonNode.Parse(json)!.AsObject();
        missingScreenshotObservations["steps"]![0]!.AsObject()
            .Remove("screenshotObservations");
        var legacyScreenshots = JsonNode.Parse(json)!.AsObject();
        legacyScreenshots["steps"]![0]!["screenshots"] = new JsonArray();

        var malformed = new[]
        {
            json.Replace("\"status\":\"Completed\"", "\"status\":0", StringComparison.Ordinal),
            json.Replace(
                "\"status\":\"Completed\"",
                "\"status\":\"completed\"",
                StringComparison.Ordinal),
            json.Replace(
                "\"status\":\"Completed\"",
                "\"status\":\"Completed\",\"status\":\"Completed\"",
                StringComparison.Ordinal),
            json.Replace(
                "\"actionIndex\":0",
                "\"actionIndex\":0,\"mystery\":true",
                StringComparison.Ordinal),
            missingSchema.ToJsonString(),
            missingActions.ToJsonString(),
            missingScreenshotObservations.ToJsonString(),
            legacyScreenshots.ToJsonString()
        };

        foreach (var candidate in malformed)
        {
            Should.Throw<JsonException>(() => CaptureSchema.Deserialize(candidate));
        }
    }

    [Fact]
    public void AnimationReplayFailureIsTypedNonwaivableEvidenceWithoutDiscardingContexts()
    {
        var action = Action(0, "key", null, ActionExecutionStatus.Completed);
        var referenceStep = Step([action]) with
        {
            AnimationFrameCaptureFailures =
            [
                new AnimationFrameCaptureFailure
                {
                    Stage = "navigate",
                    ActionIndex = 0,
                    Detail = "replay navigation failed"
                }
            ]
        };

        var result = Runner().Compare(
            Fixture(new StepEntry
            {
                Name = "initial",
                Do = [new StepAction { Key = "Enter", ActionOnly = new ActionOnlyEntry { Reason = "probe" } }]
            }),
            ParityLeg.BlazorServer,
            Bundle(ParityLeg.React, referenceStep),
            Bundle(ParityLeg.BlazorServer, Step([action])));

        result.Contexts.ShouldHaveSingleItem();
        var finding = result.Findings.Single(item =>
            item.Kind == FindingKind.FixtureError &&
            item.Property.StartsWith("animation-frame:navigate", StringComparison.Ordinal));
        finding.Step.ShouldBe("initial");
        finding.Message.ShouldContain("action 0");
    }

    [Fact]
    public void ScreenshotObservationStateRequiresExactNamedStrings()
    {
        var capture = Bundle(ParityLeg.React, Step() with
        {
            ScreenshotObservations = [ScreenshotObservation.NotVisible("portal(1)", "01")]
        });
        var json = CaptureSchema.Serialize(capture);

        json.ShouldContain("\"state\":\"NotVisible\"");
        Should.Throw<JsonException>(() => CaptureSchema.Deserialize(
            json.Replace("\"state\":\"NotVisible\"", "\"state\":1", StringComparison.Ordinal)));
        Should.Throw<JsonException>(() => CaptureSchema.Deserialize(
            json.Replace("\"state\":\"NotVisible\"", "\"state\":\"notvisible\"", StringComparison.Ordinal)));
        Should.Throw<JsonException>(() => CaptureSchema.Serialize(capture with
        {
            Steps = [capture.Steps[0] with
            {
                ScreenshotObservations =
                [
                    ScreenshotObservation.NotVisible("portal(1)", "01") with
                    {
                        State = (ScreenshotObservationState)99
                    }
                ]
            }]
        }));
    }

    [Fact]
    public void RunnerAcceptsCanonicalZeroActionCompletedAndFailureSkippedTraces()
    {
        var runner = Runner();
        var zero = Fixture(new StepEntry { Name = "initial" });
        var zeroResult = runner.Compare(
            zero,
            ParityLeg.BlazorServer,
            Bundle(ParityLeg.React, Step()),
            Bundle(ParityLeg.BlazorServer, Step()));

        zeroResult.Contexts.Count.ShouldBe(1);
        zeroResult.Findings.ShouldNotContain(finding => finding.Kind == FindingKind.FixtureError);

        var actionFixture = Fixture(new StepEntry
        {
            Name = "initial",
            Do =
            [
                new StepAction
                {
                    Click = "button",
                    ActionOnly = new ActionOnlyEntry { Reason = "trace probe" }
                },
                new StepAction
                {
                    Key = "Escape",
                    ActionOnly = new ActionOnlyEntry { Reason = "trace probe" }
                }
            ]
        });
        ActionExecution[] failed =
        [
            Action(0, "click", "button", ActionExecutionStatus.Unresolved),
            Action(1, "key", null, ActionExecutionStatus.Skipped)
        ];
        var actionResult = runner.Compare(
            actionFixture,
            ParityLeg.BlazorServer,
            Bundle(ParityLeg.React, Step(failed)),
            Bundle(ParityLeg.BlazorServer, Step(failed)));

        actionResult.Contexts.Count.ShouldBe(1);
        actionResult.Findings.ShouldNotContain(finding => finding.Kind == FindingKind.FixtureError);
    }

    [Fact]
    public void RunnerRejectsEveryMalformedTraceDimensionBeforeComparators()
    {
        var fixture = Fixture(new StepEntry
        {
            Name = "initial",
            Do =
            [
                new StepAction
                {
                    Click = "button",
                    ActionOnly = new ActionOnlyEntry { Reason = "trace probe" }
                },
                new StepAction
                {
                    Key = "Escape",
                    ActionOnly = new ActionOnlyEntry { Reason = "trace probe" }
                }
            ]
        });
        ActionExecution[] canonical =
        [
            Action(0, "click", "button", ActionExecutionStatus.Completed),
            Action(1, "key", null, ActionExecutionStatus.Completed)
        ];
        var valid = Bundle(ParityLeg.BlazorServer, Step(canonical));
        var completionDetail = Detail(ParityLeg.React, 0, "click");
        var cases = new Dictionary<string, CaptureBundle>
        {
            ["schema"] = Bundle(ParityLeg.React, Step(canonical)) with
            {
                CaptureSchemaVersion = 1
            },
            ["count"] = Bundle(ParityLeg.React, Step(canonical[..1])),
            ["index"] = Bundle(ParityLeg.React, Step(
                [canonical[0] with { ActionIndex = 1 }, canonical[1]])),
            ["verb"] = Bundle(ParityLeg.React, Step(
                [canonical[0] with { Verb = "hover" }, canonical[1]])),
            ["selector"] = Bundle(ParityLeg.React, Step(
                [canonical[0] with { ExpandedSelector = "a" }, canonical[1]])),
            ["null-click-selector"] = Bundle(ParityLeg.React, Step(
                [canonical[0] with { ExpandedSelector = null }, canonical[1]])),
            ["non-null-key-selector"] = Bundle(ParityLeg.React, Step(
                [canonical[0], canonical[1] with { ExpandedSelector = "button" }])),
            ["enum"] = Bundle(ParityLeg.React, Step(
                [canonical[0] with { Status = (ActionExecutionStatus)99 }, canonical[1]])),
            ["leading-skip"] = Bundle(ParityLeg.React, Step(
                [canonical[0] with { Status = ActionExecutionStatus.Skipped }, canonical[1]])),
            ["completed-after-failure"] = Bundle(ParityLeg.React, Step(
                [canonical[0] with { Status = ActionExecutionStatus.Unresolved }, canonical[1]])),
            ["second-failure"] = Bundle(ParityLeg.React, Step(
                [
                    canonical[0] with { Status = ActionExecutionStatus.Unresolved },
                    canonical[1] with { Status = ActionExecutionStatus.NonActionable }
                ])),
            ["missing-completion-detail"] = Bundle(ParityLeg.React, Step(
                [
                    canonical[0] with { Status = ActionExecutionStatus.CompletionUnmet },
                    canonical[1] with { Status = ActionExecutionStatus.Skipped }
                ])),
            ["detail-with-completed"] = Bundle(
                ParityLeg.React,
                Step([.. canonical], completionDetail)),
            ["action-only-completion-unmet"] = Bundle(
                ParityLeg.React,
                Step(
                    [
                        canonical[0] with { Status = ActionExecutionStatus.CompletionUnmet },
                        canonical[1] with { Status = ActionExecutionStatus.Skipped }
                    ],
                    completionDetail)),
            ["selectorless-key-unresolved"] = Bundle(ParityLeg.React, Step(
                [
                    canonical[0],
                    canonical[1] with { Status = ActionExecutionStatus.Unresolved }
                ])),
            ["selectorless-key-non-actionable"] = Bundle(ParityLeg.React, Step(
                [
                    canonical[0],
                    canonical[1] with { Status = ActionExecutionStatus.NonActionable }
                ]))
        };

        foreach (var (label, malformed) in cases)
        {
            var result = Runner().Compare(
                fixture, ParityLeg.BlazorServer, malformed, valid);

            result.Contexts.ShouldBeEmpty(label);
            result.Findings.ShouldContain(
                finding => finding.Kind == FindingKind.FixtureError,
                label);
        }
    }

    [Fact]
    public void CompletionUnmetRequiresOneIdentityAlignedDetail()
    {
        var fixture = Fixture(new StepEntry
        {
            Name = "initial",
            Do =
            [
                new StepAction
                {
                    Click = "button",
                    Complete =
                    [
                        new CompletionPredicate
                        {
                            Selector = "button",
                            State = "visible"
                        }
                    ]
                }
            ]
        });
        var action = Action(0, "click", "button", ActionExecutionStatus.CompletionUnmet);
        var reference = Bundle(ParityLeg.React, Step([action], Detail(ParityLeg.React, 0, "click")));
        var candidate = Bundle(
            ParityLeg.BlazorServer,
            Step([action], Detail(ParityLeg.BlazorServer, 0, "click")));

        var accepted = Runner().Compare(
            fixture, ParityLeg.BlazorServer, reference, candidate);

        accepted.Contexts.Count.ShouldBe(1);
        accepted.Findings.ShouldNotContain(finding => finding.Kind == FindingKind.FixtureError);
        accepted.Findings.Count(finding =>
            finding.Kind == FindingKind.ActionCompletionUnmet).ShouldBe(2);

        var mismatched = reference with
        {
            Steps = [Step([action], Detail(ParityLeg.React, 0, "hover"))]
        };
        var rejected = Runner().Compare(
            fixture, ParityLeg.BlazorServer, mismatched, candidate);

        rejected.Contexts.ShouldBeEmpty();
        rejected.Findings.ShouldContain(finding => finding.Kind == FindingKind.FixtureError);
        rejected.Findings.ShouldNotContain(
            finding => finding.Kind == FindingKind.ActionCompletionUnmet);
    }

    [Fact]
    public void CompletionDetailObservedMustBeNonBlankAndAtMostFiveHundredCharacters()
    {
        var fixture = Fixture(new StepEntry
        {
            Name = "initial",
            Do =
            [
                new StepAction
                {
                    Click = "button",
                    Complete =
                    [
                        new CompletionPredicate { Selector = "button", State = "visible" }
                    ]
                }
            ]
        });
        var action = Action(0, "click", "button", ActionExecutionStatus.CompletionUnmet);
        var candidate = Bundle(
            ParityLeg.BlazorServer,
            Step([action], Detail(ParityLeg.BlazorServer, 0, "click")));
        var invalidObservedValues = new Dictionary<string, string>
        {
            ["empty"] = string.Empty,
            ["whitespace"] = " ",
            ["too-long"] = new('x', 501)
        };

        foreach (var (label, observed) in invalidObservedValues)
        {
            var reference = Bundle(
                ParityLeg.React,
                Step(
                    [action],
                    Detail(ParityLeg.React, 0, "click") with { Observed = observed }));

            var result = Runner().Compare(
                fixture, ParityLeg.BlazorServer, reference, candidate);

            result.Contexts.ShouldBeEmpty(label);
            result.Findings.ShouldContain(
                finding => finding.Kind == FindingKind.FixtureError &&
                           finding.Property == "action-trace",
                label);
            result.Findings.ShouldNotContain(
                finding => finding.Kind == FindingKind.ActionCompletionUnmet,
                label);
        }
    }

    [Fact]
    public void CompletionDetailObservedAcceptsFiveHundredCharacterProducerBoundary()
    {
        var fixture = Fixture(new StepEntry
        {
            Name = "initial",
            Do =
            [
                new StepAction
                {
                    Click = "button",
                    Complete =
                    [
                        new CompletionPredicate { Selector = "button", State = "visible" }
                    ]
                }
            ]
        });
        var action = Action(0, "click", "button", ActionExecutionStatus.CompletionUnmet);
        ActionCompletionFailure Boundary(ParityLeg leg) =>
            Detail(leg, 0, "click") with { Observed = new string('x', 500) };

        var result = Runner().Compare(
            fixture,
            ParityLeg.BlazorServer,
            Bundle(ParityLeg.React, Step([action], Boundary(ParityLeg.React))),
            Bundle(
                ParityLeg.BlazorServer,
                Step([action], Boundary(ParityLeg.BlazorServer))));

        result.Contexts.ShouldHaveSingleItem();
        result.Findings.ShouldNotContain(finding => finding.Kind == FindingKind.FixtureError);
        result.Findings.Count(finding => finding.Kind == FindingKind.ActionCompletionUnmet)
            .ShouldBe(2);
    }

    [Fact]
    public void WaitActionRejectsNonActionableStatusAsProducerImpossible()
    {
        var fixture = Fixture(new StepEntry
        {
            Name = "initial",
            Do =
            [
                new StepAction
                {
                    Wait = "button",
                    ActionOnly = new ActionOnlyEntry { Reason = "trace probe" }
                }
            ]
        });
        var impossible = Action(0, "wait", "button", ActionExecutionStatus.NonActionable);

        var result = Runner().Compare(
            fixture,
            ParityLeg.BlazorServer,
            Bundle(ParityLeg.React, Step([impossible])),
            Bundle(ParityLeg.BlazorServer, Step(
                [Action(0, "wait", "button", ActionExecutionStatus.Completed)])));

        result.Contexts.ShouldBeEmpty();
        result.Findings.ShouldContain(finding =>
            finding.Kind == FindingKind.FixtureError &&
            finding.Property == "action-trace");
    }

    [Theory]
    [InlineData(ActionExecutionStatus.Completed)]
    [InlineData(ActionExecutionStatus.Unresolved)]
    public void StandaloneWaitActionAcceptsProducerFeasibleStatuses(
        ActionExecutionStatus status)
    {
        var fixture = Fixture(new StepEntry
        {
            Name = "initial",
            Do =
            [
                new StepAction
                {
                    Wait = "button",
                    ActionOnly = new ActionOnlyEntry { Reason = "trace probe" }
                }
            ]
        });
        var action = Action(0, "wait", "button", status);

        var result = Runner().Compare(
            fixture,
            ParityLeg.BlazorServer,
            Bundle(ParityLeg.React, Step([action])),
            Bundle(ParityLeg.BlazorServer, Step([action])));

        result.Contexts.ShouldHaveSingleItem();
        result.Findings.ShouldNotContain(finding => finding.Kind == FindingKind.FixtureError);
    }

    [Theory]
    [InlineData("selector")]
    [InlineData("predicate")]
    [InlineData("expected")]
    public void CompletionUnmetDetailMustMatchOneExactManifestPredicate(string field)
    {
        var fixture = Fixture(new StepEntry
        {
            Name = "initial",
            Do =
            [
                new StepAction
                {
                    Click = "button",
                    Complete =
                    [
                        new CompletionPredicate
                        {
                            Selector = "button",
                            Attribute = "aria-expanded",
                            Expected = "true"
                        }
                    ]
                }
            ]
        });
        var action = Action(0, "click", "button", ActionExecutionStatus.CompletionUnmet);
        var detail = Detail(ParityLeg.React, 0, "click") with
        {
            Predicate = "attribute:aria-expanded",
            ExpectedValue = "true"
        };
        detail = field switch
        {
            "selector" => detail with { Selector = "input" },
            "predicate" => detail with { Predicate = "attribute:aria-pressed" },
            "expected" => detail with { ExpectedValue = "false" },
            _ => throw new ArgumentOutOfRangeException(nameof(field))
        };
        var reference = Bundle(ParityLeg.React, Step([action], detail));
        var candidate = Bundle(
            ParityLeg.BlazorServer,
            Step(
                [action],
                Detail(ParityLeg.BlazorServer, 0, "click") with
                {
                    Predicate = "attribute:aria-expanded",
                    ExpectedValue = "true"
                }));

        var result = Runner().Compare(
            fixture, ParityLeg.BlazorServer, reference, candidate);

        result.Contexts.ShouldBeEmpty();
        result.Findings.ShouldContain(finding =>
            finding.Kind == FindingKind.FixtureError &&
            finding.Property == "action-trace");
        result.Findings.ShouldNotContain(
            finding => finding.Kind == FindingKind.ActionCompletionUnmet);
    }

    [Fact]
    public void SelectorlessKeyMayReportCompletionUnmetWhenItsManifestContractMatches()
    {
        var fixture = Fixture(new StepEntry
        {
            Name = "initial",
            Do =
            [
                new StepAction
                {
                    Key = "Enter",
                    Complete =
                    [
                        new CompletionPredicate { Selector = "button", State = "visible" }
                    ]
                }
            ]
        });
        var action = Action(0, "key", null, ActionExecutionStatus.CompletionUnmet);
        var reference = Bundle(
            ParityLeg.React,
            Step([action], Detail(ParityLeg.React, 0, "key")));
        var candidate = Bundle(
            ParityLeg.BlazorServer,
            Step([action], Detail(ParityLeg.BlazorServer, 0, "key")));

        var result = Runner().Compare(
            fixture, ParityLeg.BlazorServer, reference, candidate);

        result.Contexts.ShouldHaveSingleItem();
        result.Findings.ShouldNotContain(finding => finding.Kind == FindingKind.FixtureError);
        result.Findings.Count(finding => finding.Kind == FindingKind.ActionCompletionUnmet)
            .ShouldBe(2);
    }

    [Fact]
    public void CompletionDetailMayMatchAnyDeclaredPredicateNotOnlyTheFirst()
    {
        var fixture = Fixture(new StepEntry
        {
            Name = "initial",
            Do =
            [
                new StepAction
                {
                    Click = "button",
                    Complete =
                    [
                        new CompletionPredicate { Selector = "button", State = "visible" },
                        new CompletionPredicate { Selector = "input", InputValue = "ready" }
                    ]
                }
            ]
        });
        var action = Action(0, "click", "button", ActionExecutionStatus.CompletionUnmet);
        ActionCompletionFailure Later(ParityLeg leg) => Detail(leg, 0, "click") with
        {
            Selector = "input",
            Predicate = "input-value",
            ExpectedValue = "ready"
        };

        var result = Runner().Compare(
            fixture,
            ParityLeg.BlazorServer,
            Bundle(ParityLeg.React, Step([action], Later(ParityLeg.React))),
            Bundle(
                ParityLeg.BlazorServer,
                Step([action], Later(ParityLeg.BlazorServer))));

        result.Contexts.ShouldHaveSingleItem();
        result.Findings.ShouldNotContain(finding => finding.Kind == FindingKind.FixtureError);
        result.Findings.Count(finding => finding.Kind == FindingKind.ActionCompletionUnmet)
            .ShouldBe(2);
    }

    private static ActionExecution Action(
        int index,
        string verb,
        string? selector,
        ActionExecutionStatus status) => new()
        {
            ActionIndex = index,
            Verb = verb,
            ExpandedSelector = selector,
            Status = status
        };

    private static CaptureBundle Bundle(ParityLeg leg, StepCapture step) => new()
    {
        CaptureSchemaVersion = CaptureSchema.CurrentVersion,
        Fixture = "harness/action-trace",
        Leg = leg,
        Theme = "light",
        Steps = [step]
    };

    private static ActionCompletionFailure Detail(ParityLeg leg, int index, string verb) => new()
    {
        Fixture = "harness/action-trace",
        Leg = leg,
        Step = "initial",
        ActionIndex = index,
        Verb = verb,
        Selector = "button",
        Predicate = "state",
        ExpectedValue = "visible",
        Observed = "{}"
    };

    private static FixtureEntry Fixture(StepEntry step) => new()
    {
        Id = "harness/action-trace",
        Component = "harness",
        React = "internal:none",
        Blazor = "Harness/ActionTrace",
        Steps = [step]
    };

    private static ParityRunner Runner()
        => new(new ComparatorRegistry(), "unused", "unused", "unused");

    private static StepCapture Step(
        IReadOnlyList<ActionExecution>? actions = null,
        params ActionCompletionFailure[] failures) => new()
        {
            Step = "initial",
            Dom = new DomNode
            {
                Tag = "button",
                Path = "root > button",
                Attributes = new Dictionary<string, string>(),
                Classes = [],
                Text = "Probe",
                Children = []
            },
            Styles = new Dictionary<string, IReadOnlyDictionary<string, string>>(),
            CustomProps = new Dictionary<string, IReadOnlyDictionary<string, string>>(),
            Geometry = new Dictionary<string, IReadOnlyDictionary<string, double>>(),
            Actions = actions ?? [],
            ActionCompletionFailures = failures
        };
}
