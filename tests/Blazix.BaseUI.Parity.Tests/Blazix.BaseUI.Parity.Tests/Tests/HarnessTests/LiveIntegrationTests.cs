using System.Text.Json;
using Blazix.BaseUI.Parity.Tests.Baselines;
using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Diff;
using Blazix.BaseUI.Parity.Tests.Fixtures;
using Blazix.BaseUI.Parity.Tests.Infrastructure;
using Microsoft.Playwright;
using Shouldly;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

/// <summary>Exercises bundle validation, context construction, and the real live smoke path.</summary>
/// <param name="playwright">The production browser fixture.</param>
[Collection(ParityTimingCollection.Name)]
public sealed class LiveIntegrationTests(PlaywrightFixture playwright)
    : IClassFixture<PlaywrightFixture>, IDisposable
{
    private readonly string artifacts = Path.Combine(
        Path.GetTempPath(), "blazix-parity-live", Guid.NewGuid().ToString("N"));

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(artifacts))
        {
            Directory.Delete(artifacts, recursive: true);
        }
    }

    [Fact]
    public void PairsStepsByManifestNameAndPropagatesThePixelThreshold()
    {
        var fixture = Fixture(
            0.037,
            new StepEntry { Name = "first" },
            new StepEntry { Name = "second" });
        var reference = Bundle(
            fixture.Id,
            ParityLeg.React,
            Capture("second", "two"),
            Capture("first", "one"));
        var candidate = Bundle(
            fixture.Id,
            ParityLeg.BlazorServer,
            Capture("first", "one"),
            Capture("second", "two"));

        var result = Runner().Compare(
            fixture, ParityLeg.BlazorServer, reference, candidate);

        result.Contexts.Select(context => context.Step).ShouldBe(["first", "second"]);
        result.Contexts.Select(context => context.Reference.Step)
            .ShouldBe(["first", "second"]);
        result.Contexts.Select(context => context.Candidate.Step)
            .ShouldBe(["first", "second"]);
        result.Contexts.ShouldAllBe(context => context.Fixture == fixture.Id);
        result.Contexts.ShouldAllBe(context => context.Leg == ParityLeg.BlazorServer);
        result.Contexts.ShouldAllBe(context => context.PixelThreshold == 0.037);
        result.Findings.ShouldBeEmpty();
    }

    [Fact]
    public void MapsActualLegCompletionFailuresBeforeComparatorFindings()
    {
        var fixture = Fixture(0.001, ActionStep("action", 1));
        var referenceStep = ActionCapture(
            "action", "reference", 1, ActionExecutionStatus.CompletionUnmet) with
        {
            ActionCompletionFailures =
            [
                new ActionCompletionFailure
                {
                    Fixture = fixture.Id,
                    Leg = ParityLeg.React,
                    Step = "action",
                    ActionIndex = 1,
                    Verb = "click",
                    Selector = "[data-target]",
                    Predicate = "attribute:aria-expanded",
                    ExpectedValue = "true",
                    Observed = "{\"matches\":1,\"actual\":\"false\"}"
                }
            ]
        };
        var candidateStep = ActionCapture(
            "action", "candidate", 1, ActionExecutionStatus.Completed);

        var result = Runner().Compare(
            fixture,
            ParityLeg.BlazorWasm,
            Bundle(fixture.Id, ParityLeg.React, referenceStep),
            Bundle(fixture.Id, ParityLeg.BlazorWasm, candidateStep));

        var completion = result.Findings[0];
        completion.Kind.ShouldBe(FindingKind.ActionCompletionUnmet);
        completion.Severity.ShouldBe(Severity.Error);
        completion.Leg.ShouldBe(ParityLeg.React);
        completion.Step.ShouldBe("action");
        completion.Property.ShouldBe(
            "1:click:attribute:aria-expanded:[data-target]");
        completion.ReferenceValue.ShouldNotBeNull().ShouldContain("expected=true");
        completion.CandidateValue.ShouldBeNull();
        result.Findings.ShouldContain(finding => finding.Kind == FindingKind.Attribute);
        result.HasBlockingEvidence.ShouldBeTrue();
    }

    [Theory]
    [InlineData(ParityLeg.BlazorServer)]
    [InlineData(ParityLeg.BlazorWasm)]
    public void MapsCandidateCompletionFailuresToTheirActualLegBeforeComparatorFindings(
        ParityLeg candidateLeg)
    {
        var fixture = Fixture(0.001, ActionStep("action", 2));
        var referenceStep = ActionCapture(
            "action", "reference", 2, ActionExecutionStatus.Completed);
        var candidateStep = ActionCapture(
            "action", "candidate", 2, ActionExecutionStatus.CompletionUnmet) with
        {
            ActionCompletionFailures =
            [
                new ActionCompletionFailure
                {
                    Fixture = fixture.Id,
                    Leg = candidateLeg,
                    Step = "action",
                    ActionIndex = 2,
                    Verb = "click",
                    Selector = "[data-target]",
                    Predicate = "attribute:aria-expanded",
                    ExpectedValue = "true",
                    Observed = "{\"matches\":1,\"actual\":\"false\"}"
                }
            ]
        };

        var result = Runner().Compare(
            fixture,
            candidateLeg,
            Bundle(fixture.Id, ParityLeg.React, referenceStep),
            Bundle(fixture.Id, candidateLeg, candidateStep));

        var completion = result.Findings[0];
        completion.Kind.ShouldBe(FindingKind.ActionCompletionUnmet);
        completion.Severity.ShouldBe(Severity.Error);
        completion.Leg.ShouldBe(candidateLeg);
        completion.Step.ShouldBe("action");
        completion.Property.ShouldBe(
            "2:click:attribute:aria-expanded:[data-target]");
        completion.ReferenceValue.ShouldBeNull();
        completion.CandidateValue.ShouldNotBeNull().ShouldContain("expected=true");
        result.Findings.Skip(1).ShouldContain(finding =>
            finding.Kind == FindingKind.Attribute);
        result.HasBlockingEvidence.ShouldBeTrue();
    }

    [Fact]
    public void MissingRequiredLegAndStepBecomeBlockingRunnerEvidence()
    {
        var fixture = Fixture(0.001, new StepEntry { Name = "initial" });
        var reference = Bundle(
            fixture.Id, ParityLeg.React, Capture("unexpected", "reference"));

        var missingCandidate = Runner().Compare(
            fixture, ParityLeg.BlazorServer, reference, candidate: null);
        var missingLeg = missingCandidate.Findings.ShouldHaveSingleItem();
        missingLeg.Kind.ShouldBe(FindingKind.FixtureError);
        missingLeg.Severity.ShouldBe(Severity.Error);
        missingLeg.Leg.ShouldBe(ParityLeg.BlazorServer);
        missingLeg.Message.ShouldContain("Required BlazorServer capture is missing");
        missingCandidate.Contexts.ShouldBeEmpty();

        var missingStep = Runner().Compare(
            fixture,
            ParityLeg.BlazorWasm,
            reference,
            Bundle(fixture.Id, ParityLeg.BlazorWasm, Capture("unexpected", "candidate")));
        missingStep.Contexts.ShouldBeEmpty();
        missingStep.Findings.ShouldContain(finding =>
            finding.Kind == FindingKind.FixtureError &&
            finding.Leg == ParityLeg.React &&
            finding.Step == "initial" &&
            finding.Message.Contains("missing required step", StringComparison.Ordinal));
        missingStep.Findings.ShouldContain(finding =>
            finding.Kind == FindingKind.FixtureError &&
            finding.Leg == ParityLeg.BlazorWasm &&
            finding.Step == "unexpected" &&
            finding.Message.Contains("unexpected step", StringComparison.Ordinal));
    }

    [Fact]
    public void LiveBundlePreconditionRejectsMissingAndStaleCopiesWithBuildCommand()
    {
        var source = Path.Combine(artifacts, "source");
        var served = Path.Combine(artifacts, "served");
        var fixture = CanaryFixture();
        var provenance = Provenance();

        var missing = Should.Throw<InvalidOperationException>(() =>
            LiveBundlePrecondition.Validate(source, served, fixture, provenance));
        missing.Message.ShouldContain("source React bundle directory");
        missing.Message.ShouldContain("pnpm parity:build");

        Directory.CreateDirectory(source);
        Directory.CreateDirectory(served);

        var incomplete = Should.Throw<InvalidOperationException>(() =>
            LiveBundlePrecondition.Validate(source, served, fixture, provenance));
        incomplete.Message.ShouldContain("unreadable or incomplete");
        incomplete.Message.ShouldContain("pnpm parity:build");

        File.WriteAllText(Path.Combine(source, "index.html"), "source");

        var incompleteServed = Should.Throw<InvalidOperationException>(() =>
            LiveBundlePrecondition.Validate(source, served, fixture, provenance));
        incompleteServed.Message.ShouldContain("served React bundle directory");
        incompleteServed.Message.ShouldContain("unreadable or incomplete");
        incompleteServed.Message.ShouldContain("pnpm parity:build");

        File.WriteAllText(Path.Combine(served, "index.html"), "source");

        var missingProvenance = Should.Throw<InvalidOperationException>(() =>
            LiveBundlePrecondition.Validate(source, served, fixture, provenance));
        missingProvenance.Message.ShouldContain("provenance manifest");

        WriteBundleProvenance(source, fixture, provenance);
        File.Copy(
            Path.Combine(source, ReactBundleProvenanceManifest.FileName),
            Path.Combine(served, ReactBundleProvenanceManifest.FileName));
        Should.NotThrow(() =>
            LiveBundlePrecondition.Validate(source, served, fixture, provenance));

        File.WriteAllText(Path.Combine(served, "stale-chunk.js"), "removed source chunk");
        var extraServedFile = Should.Throw<InvalidOperationException>(() =>
            LiveBundlePrecondition.Validate(source, served, fixture, provenance));
        extraServedFile.Message.ShouldContain("served React bundle");
        extraServedFile.Message.ShouldContain("stale");
        extraServedFile.Message.ShouldContain("pnpm parity:build");

        File.Delete(Path.Combine(served, "stale-chunk.js"));
        File.WriteAllText(Path.Combine(source, "index.html"), "mutated after build");
        File.WriteAllText(Path.Combine(served, "index.html"), "mutated after build");
        var sourceMutation = Should.Throw<InvalidOperationException>(() =>
            LiveBundlePrecondition.Validate(source, served, fixture, provenance));
        sourceMutation.Message.ShouldContain("fingerprint");
        sourceMutation.Message.ShouldContain("pnpm parity:build");

        WriteBundleProvenance(source, fixture, provenance);
        File.Copy(
            Path.Combine(source, ReactBundleProvenanceManifest.FileName),
            Path.Combine(served, ReactBundleProvenanceManifest.FileName),
            overwrite: true);
        var sourceMismatch = Should.Throw<InvalidOperationException>(() =>
            LiveBundlePrecondition.Validate(
                source,
                served,
                fixture,
                provenance with { SourceHash = new string('B', 64) }));
        sourceMismatch.Message.ShouldContain("current provenance");

        var manifestPath = Path.Combine(source, ReactBundleProvenanceManifest.FileName);
        var schemaProperty =
            $"\"schemaVersion\": {ReactBundleProvenanceManifest.CurrentSchemaVersion}";
        File.WriteAllText(
            manifestPath,
            File.ReadAllText(manifestPath).Replace(
                schemaProperty,
                $"{schemaProperty}, {schemaProperty}",
                StringComparison.Ordinal));
        File.Copy(manifestPath, Path.Combine(served, ReactBundleProvenanceManifest.FileName), true);
        var duplicate = Should.Throw<InvalidOperationException>(() =>
            LiveBundlePrecondition.Validate(source, served, fixture, provenance));
        duplicate.Message.ShouldContain("duplicate", Case.Insensitive);
    }

    [Fact]
    public void BundleFingerprintExcludesOnlyTheRootProvenanceManifest()
    {
        var directory = Path.Combine(artifacts, "fingerprint-root-only");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "index.html"), "index");
        File.WriteAllText(
            Path.Combine(directory, ReactBundleProvenanceManifest.FileName),
            "root manifest");
        var rootManifestExcluded = ReactBundleProvenance.Fingerprint(directory);

        var nested = Path.Combine(directory, "assets");
        Directory.CreateDirectory(nested);
        File.WriteAllText(
            Path.Combine(nested, ReactBundleProvenanceManifest.FileName),
            "nested asset");

        ReactBundleProvenance.Fingerprint(directory).ShouldNotBe(rootManifestExcluded);
    }

    [Fact]
    public void LiveSourceUsesTheStableValidatedBundleTimestampAcrossAttempts()
    {
        var fixture = FixtureManifest.Load().Single(entry => entry.Id == "field/hero");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            ParityPaths.ReactDist,
            ReactBundleProvenanceManifest.FileName)));
        var generatedAtUtc = manifest.RootElement.GetProperty("generatedAtUtc")
            .GetDateTimeOffset();

        var first = LiveBaselineSource.Read(fixture);
        var readAt = DateTimeOffset.UtcNow;
        SpinWait.SpinUntil(
            () => DateTimeOffset.UtcNow > readAt.AddMilliseconds(20),
            TimeSpan.FromSeconds(1)).ShouldBeTrue();
        var retry = LiveBaselineSource.Read(fixture);

        first.GeneratedAtUtc.ShouldBe(generatedAtUtc);
        retry.GeneratedAtUtc.ShouldBe(generatedAtUtc);
        retry.ShouldBe(first);
    }

    [Fact]
    public void LiveBundlePreconditionRejectsTamperedAndNonUtcBuildTimestamps()
    {
        var directory = Path.Combine(artifacts, "provenance-timestamp");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "index.html"), "source");
        var fixture = CanaryFixture();
        var provenance = Provenance();
        WriteBundleProvenance(directory, fixture, provenance);

        var path = Path.Combine(directory, ReactBundleProvenanceManifest.FileName);
        var valid = File.ReadAllText(path);
        Should.NotThrow(() => ReactBundleProvenance.Validate(directory, fixture, provenance));

        File.WriteAllText(path, valid.Replace(
            "2026-08-09T00:00:00+00:00",
            "2026-08-09T08:00:00+08:00",
            StringComparison.Ordinal));
        Should.Throw<InvalidOperationException>(() =>
            ReactBundleProvenance.Validate(directory, fixture, provenance)).Message
            .ShouldContain("invalid", Case.Insensitive);

        File.WriteAllText(path, valid.Replace(
            "2026-08-09T00:00:00+00:00",
            "2026-08-10T00:00:00+00:00",
            StringComparison.Ordinal));
        Should.Throw<InvalidOperationException>(() =>
            ReactBundleProvenance.Validate(directory, fixture, provenance)).Message
            .ShouldContain("current provenance", Case.Insensitive);
    }

    [Fact]
    public async Task CaptureExecutionFailureBecomesBlockingActualLegEvidence()
    {
        var fixture = new FixtureEntry
        {
            Id = "harness/canary",
            Component = "harness",
            React = "internal:canary",
            Blazor = "Harness/Canary",
            Steps =
            [
                new StepEntry { Name = "initial" },
                new StepEntry
                {
                    Name = "broken-action",
                    Do =
                    [
                        new StepAction
                        {
                            Click = "[",
                            Complete =
                            [
                                new CompletionPredicate
                                {
                                    Selector = "p",
                                    State = "attached"
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var results = await new ParityRunner(artifacts)
            .RunLiveAsync(playwright.Browser, fixture);

        results.Count.ShouldBe(2);
        results.ShouldAllBe(result => result.Reference == null);
        results.ShouldAllBe(result => result.Candidate == null);

        foreach (var result in results)
        {
            result.Findings.Count.ShouldBe(2);
            result.Findings.ShouldContain(failure =>
                failure.Kind == FindingKind.FixtureError &&
                failure.Severity == Severity.Error &&
                failure.Leg == ParityLeg.React &&
                failure.Message.Contains("Capture failed on React", StringComparison.Ordinal));
            result.Findings.ShouldContain(failure =>
                failure.Kind == FindingKind.FixtureError &&
                failure.Severity == Severity.Error &&
                failure.Leg == result.Leg &&
                failure.Message.Contains($"Capture failed on {result.Leg}", StringComparison.Ordinal));
            result.HasBlockingEvidence.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task BrowserContextCreationFailureBecomesTwoBlockingResults()
    {
        var fixture = CanaryFixture();
        var runner = LiveRunner(
            createContext: _ => Task.FromException<IBrowserContext>(
                new InvalidOperationException("context creation probe")));

        var results = await runner.RunLiveAsync(playwright.Browser, fixture);

        results.Select(result => result.Leg)
            .ShouldBe([ParityLeg.BlazorServer, ParityLeg.BlazorWasm]);
        results.ShouldAllBe(result => result.HasBlockingEvidence);
        results.ShouldAllBe(result => result.Reference == null && result.Candidate == null);
        results.ShouldAllBe(result => result.Findings.ShouldHaveSingleItem().Leg == ParityLeg.React);
        results.ShouldAllBe(result =>
            result.Findings[0].Property == "browser-context" &&
            result.Findings[0].Message.Contains("context creation probe", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ReactPageFailureDoesNotHideSuccessfulServerAndWasmCaptures()
    {
        var pageCount = 0;
        var runner = LiveRunner(createPage: context =>
        {
            pageCount++;
            return pageCount == 1
                ? Task.FromException<IPage>(new InvalidOperationException("React page probe"))
                : context.NewPageAsync();
        });

        var results = await runner.RunLiveAsync(playwright.Browser, CanaryFixture());

        pageCount.ShouldBe(3);
        results.ShouldAllBe(result => result.Reference == null);
        results.ShouldAllBe(result => result.Candidate != null);
        results.ShouldAllBe(result => result.Candidate!.Leg == result.Leg);
        var provenance = LiveBaselineSource.Read(CanaryFixture());
        results.ShouldAllBe(result => result.Candidate!.BaseUiSha == provenance.UpstreamSha);
        results.ShouldAllBe(result => result.Candidate!.SourceHash == provenance.SourceHash);
        results.ShouldAllBe(result => result.Findings.ShouldHaveSingleItem().Leg == ParityLeg.React);
        results.ShouldAllBe(result =>
            result.Findings[0].Message.Contains("React page probe", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CaptureAndPageCleanupFailuresPreserveBothDiagnostics()
    {
        var fixture = CanaryFixture() with
        {
            Steps =
            [
                new StepEntry { Name = "initial" },
                new StepEntry
                {
                    Name = "broken-action",
                    Do = [new StepAction { Click = "[" }]
                }
            ]
        };
        var runner = LiveRunner(closePage: _ => Task.FromException(
            new InvalidOperationException("page cleanup probe")));

        var results = await runner.RunLiveAsync(playwright.Browser, fixture);

        foreach (var result in results)
        {
            result.Findings.Count.ShouldBe(2);
            result.Findings.ShouldAllBe(finding =>
                finding.Message.Contains("Unexpected token", StringComparison.Ordinal));
            result.Findings.ShouldAllBe(finding =>
                finding.Message.Contains("Page cleanup also failed: page cleanup probe", StringComparison.Ordinal));
            result.Findings.Select(finding => finding.Leg)
                .ShouldBe([ParityLeg.React, result.Leg]);
        }
    }

    [Fact]
    public async Task PageCleanupFailureKeepsCapturedBundlesButBlocksComparison()
    {
        var runner = LiveRunner(closePage: _ => Task.FromException(
            new InvalidOperationException(
                "page cleanup probe at /Users/private/task15-live-field/attempt-1")));

        var results = await runner.RunLiveAsync(playwright.Browser, CanaryFixture());

        foreach (var result in results)
        {
            result.Reference.ShouldNotBeNull().Leg.ShouldBe(ParityLeg.React);
            result.Candidate.ShouldNotBeNull().Leg.ShouldBe(result.Leg);
            result.Contexts.ShouldBeEmpty();
            result.Findings.Count.ShouldBe(2);
            result.Findings.Select(finding => finding.Leg)
                .ShouldBe([ParityLeg.React, result.Leg]);
            result.Findings.ShouldAllBe(finding =>
                finding.Kind == FindingKind.FixtureError &&
                finding.Message.Contains("page cleanup probe", StringComparison.Ordinal));
            result.Findings.ShouldAllBe(finding =>
                !finding.Message.Contains("/Users/private", StringComparison.Ordinal));
            result.HasBlockingEvidence.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task SelectGroupedPreservesSourceOwnedAlignedAndForceMountedStateInBothModes()
    {
        var fixture = FixtureManifest.Load().Single(entry => entry.Id == "select/grouped");

        var results = await new ParityRunner(artifacts)
            .RunLiveAsync(playwright.Browser, fixture);

        results.Select(result => result.Leg)
            .ShouldBe([ParityLeg.BlazorServer, ParityLeg.BlazorWasm]);

        var originEvidence = new List<string>();
        foreach (var result in results)
        {
            var reference = result.Reference.ShouldNotBeNull();
            var candidate = result.Candidate.ShouldNotBeNull();
            var referenceOpen = reference.Steps.Single(step => step.Step == "open");
            var candidateOpen = candidate.Steps.Single(step => step.Step == "open");

            FindDomPathsWithAttribute(candidateOpen.Dom, "data-base-ui-inert")
                .ShouldBe(FindDomPathsWithAttribute(referenceOpen.Dom, "data-base-ui-inert"));

            foreach (var stepName in new[] { "open", "arrow-down", "arrow-up" })
            {
                var step = candidate.Steps.Single(item => item.Step == stepName);
                var stepPositionerPath = FindDomNodeWithAttribute(
                    step.Dom,
                    "data-align").Path;
                var stepTriggerPath = FindDomNodeWithAttribute(
                    step.Dom,
                    "role",
                    "combobox").Path;
                originEvidence.Add(
                    $"{result.Leg}/{stepName}={step.CustomProps[stepPositionerPath]["--transform-origin"]}; " +
                    $"trigger={JsonSerializer.Serialize(step.Geometry[stepTriggerPath])}; " +
                    $"positioner={JsonSerializer.Serialize(step.Geometry[stepPositionerPath])}");
            }

            var terminal = candidate.Steps.Single(step => step.Step == "select-banana");
            var terminalPositioner = FindDomNodeWithAttribute(
                terminal.Dom,
                "data-align");
            var positionerPath = terminalPositioner.Path;
            terminalPositioner.Attributes["data-side"].ShouldBe("bottom");
            terminalPositioner.Attributes.ShouldContainKey("hidden");
            terminal.Styles[positionerPath]["display"].ShouldBe("none");
            terminal.Styles[positionerPath]["position"].ShouldBe("absolute");
            terminal.Styles[positionerPath]["visibility"].ShouldBe("visible");
            terminal.Styles[positionerPath]["opacity"].ShouldBe("0");
            terminal.CustomProps[positionerPath]["--available-width"].ShouldBe("100vw");
            terminal.CustomProps[positionerPath]["--available-height"].ShouldBe("100vh");
            var terminalPopupPath = FindDomNodeWithAttribute(
                terminal.Dom,
                "data-base-ui-focusable").Path;
            var referenceTerminal = reference.Steps.Single(step => step.Step == "select-banana");
            terminal.Focus.ShouldBe(
                referenceTerminal.Focus,
                $"{result.Leg}/select-banana focus");
            var referencePopupPath = FindDomNodeWithAttribute(
                referenceTerminal.Dom,
                "data-base-ui-focusable").Path;
            terminal.CustomProps[terminalPopupPath]["--transform-origin"].ShouldBe(
                referenceTerminal.CustomProps[referencePopupPath]["--transform-origin"],
                $"{result.Leg}/select-banana popup transform origin");
            originEvidence.Add(
                $"{result.Leg}/select-banana={terminal.CustomProps[positionerPath]["--transform-origin"]}; " +
                $"positioner={JsonSerializer.Serialize(terminal.Geometry[positionerPath])}");
            var selectedOption = FindDomNodeWithAttribute(
                terminal.Dom,
                "aria-selected",
                "true");
            selectedOption.Attributes.ShouldContainKey("data-selected");
            selectedOption.Children.Single(child => child.Tag == "span").Attributes
                .ShouldContainKey("data-selected");

            if (result.Leg == ParityLeg.BlazorWasm)
            {
                var referenceFrames = referenceOpen.ScreenshotObservations
                    .Where(observation => observation.Shot.StartsWith("frame", StringComparison.Ordinal))
                    .Select(observation => observation.Shot)
                    .ToArray();
                var candidateFrames = candidateOpen.ScreenshotObservations
                    .Where(observation => observation.Shot.StartsWith("frame", StringComparison.Ordinal))
                    .Select(observation => observation.Shot)
                    .ToArray();

                candidateFrames.ShouldBe(referenceFrames, $"{result.Leg}/open animation frames");
            }
        }

        originEvidence.ShouldAllBe(
            item => item.Contains("=99px -4px;", StringComparison.Ordinal),
            string.Join(Environment.NewLine, originEvidence));
    }

    [Fact]
    public async Task SwitchHeroRunsThroughLiveCapturePairingAndEveryComparatorInBothModes()
    {
        var fixture = FixtureManifest.Load().Single(entry => entry.Id == "switch/hero");

        var results = await new ParityRunner(artifacts)
            .RunLiveAsync(playwright.Browser, fixture);

        results.Select(result => result.Leg)
            .ShouldBe([ParityLeg.BlazorServer, ParityLeg.BlazorWasm]);

        foreach (var result in results)
        {
            result.Reference.ShouldNotBeNull().Leg.ShouldBe(ParityLeg.React);
            result.Candidate.ShouldNotBeNull().Leg.ShouldBe(result.Leg);
            result.Contexts.Select(context => context.Step)
                .ShouldBe(fixture.Steps.Select(step => step.Name));
            result.Contexts.ShouldAllBe(context =>
                context.PixelThreshold == fixture.PixelThreshold);
            result.Findings.ShouldNotContain(finding =>
                finding.Kind == FindingKind.FixtureError ||
                finding.Kind == FindingKind.ActionCompletionUnmet);

            foreach (var finding in result.Findings)
            {
                finding.Fixture.ShouldBe($"{fixture.Id}@light");
                finding.Step.ShouldNotBeNullOrWhiteSpace();
                Console.WriteLine(
                    $"[switch/hero:{result.Leg}] {finding.Kind} " +
                    $"{finding.Step} {finding.NodePath} {finding.Property}");
            }
        }
    }

    private ParityRunner Runner()
        => new(
            new ComparatorRegistry(artifacts),
            artifacts,
            Path.Combine(artifacts, "unused-source"),
            Path.Combine(artifacts, "unused-served"));

    private ParityRunner LiveRunner(
        Func<IBrowser, Task<IBrowserContext>>? createContext = null,
        Func<IBrowserContext, Task<IPage>>? createPage = null,
        Func<IPage, Task>? closePage = null)
        => new(
            new ComparatorRegistry(artifacts),
            artifacts,
            ParityPaths.ReactDist,
            Path.Combine(AppContext.BaseDirectory, "react-dist"),
            createContext,
            createPage,
            closePage);

    private static CaptureBundle Bundle(
        string fixture,
        ParityLeg leg,
        params StepCapture[] steps) => new()
        {
            CaptureSchemaVersion = CaptureSchema.CurrentVersion,
            Fixture = fixture,
            Theme = "light",
            Leg = leg,
            Steps = steps
        };

    private static StepEntry ActionStep(string step, int failureIndex) => new()
    {
        Name = step,
        Do = Enumerable.Range(0, failureIndex + 1)
            .Select(index => index == failureIndex
                ? new StepAction
                {
                    Click = "[data-target]",
                    Complete =
                    [
                        new CompletionPredicate
                        {
                            Selector = "[data-target]",
                            Attribute = "aria-expanded",
                            Expected = "true"
                        }
                    ]
                }
                : new StepAction
                {
                    Click = $"[data-before-{index}]",
                    ActionOnly = new ActionOnlyEntry { Reason = "manual runner trace" }
                })
            .ToArray()
    };

    private static StepCapture ActionCapture(
        string step,
        string value,
        int finalIndex,
        ActionExecutionStatus finalStatus)
        => Capture(step, value) with
        {
            Actions = Enumerable.Range(0, finalIndex + 1)
                .Select(index => new ActionExecution
                {
                    ActionIndex = index,
                    Verb = "click",
                    ExpandedSelector = index == finalIndex
                        ? "[data-target]"
                        : $"[data-before-{index}]",
                    Status = index == finalIndex
                        ? finalStatus
                        : ActionExecutionStatus.Completed
                })
                .ToArray()
        };

    private static StepCapture Capture(string step, string value)
    {
        const string path = "root>button[role=switch]";
        return new StepCapture
        {
            Step = step,
            Dom = new DomNode
            {
                Tag = "button",
                Path = path,
                Attributes = new Dictionary<string, string>
                {
                    ["role"] = "switch",
                    ["data-value"] = value
                },
                Classes = [],
                Text = "Switch",
                Children = []
            },
            Styles = new Dictionary<string, IReadOnlyDictionary<string, string>>(),
            CustomProps = new Dictionary<string, IReadOnlyDictionary<string, string>>(),
            Geometry = new Dictionary<string, IReadOnlyDictionary<string, double>>()
        };
    }

    private static DomNode FindDomNode(DomNode root, string path)
    {
        if (root.Path == path)
        {
            return root;
        }

        foreach (var child in root.Children)
        {
            var match = FindDomNodeOrDefault(child, path);
            if (match is not null)
            {
                return match;
            }
        }

        throw new InvalidOperationException($"DOM node '{path}' was not captured.");
    }

    private static DomNode FindDomNodeWithAttribute(
        DomNode root,
        string attribute,
        string? value = null)
    {
        var match = FindDomNodeWithAttributeOrDefault(root, attribute, value);
        return match ?? throw new InvalidOperationException(
            $"DOM node with attribute '{attribute}' was not captured.");
    }

    private static DomNode? FindDomNodeWithAttributeOrDefault(
        DomNode root,
        string attribute,
        string? value)
    {
        if (root.Attributes.TryGetValue(attribute, out var actual) &&
            (value is null || string.Equals(actual, value, StringComparison.Ordinal)))
        {
            return root;
        }

        foreach (var child in root.Children)
        {
            var match = FindDomNodeWithAttributeOrDefault(child, attribute, value);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private static DomNode? FindDomNodeOrDefault(DomNode root, string path)
    {
        if (root.Path == path)
        {
            return root;
        }

        foreach (var child in root.Children)
        {
            var match = FindDomNodeOrDefault(child, path);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private static IReadOnlyList<string> FindDomPathsWithAttribute(DomNode root, string attribute)
    {
        var paths = new List<string>();
        AddDomPathsWithAttribute(root, attribute, paths);
        return paths;
    }

    private static void AddDomPathsWithAttribute(
        DomNode root,
        string attribute,
        ICollection<string> paths)
    {
        if (root.Attributes.ContainsKey(attribute))
        {
            paths.Add(root.Path);
        }

        foreach (var child in root.Children)
        {
            AddDomPathsWithAttribute(child, attribute, paths);
        }
    }

    private static FixtureEntry Fixture(
        double threshold,
        params StepEntry[] steps) => new()
        {
            Id = "harness/runner-probe",
            Component = "harness",
            React = "internal:none",
            Blazor = "Harness/RunnerProbe",
            PixelThreshold = threshold,
            Steps = steps
        };

    private static FixtureEntry CanaryFixture()
        => new()
        {
            Id = "harness/canary",
            Component = "harness",
            React = "internal:canary",
            Blazor = "Harness/Canary",
            PixelThreshold = 0.001,
            Steps = [new StepEntry { Name = "initial" }]
        };

    private static LiveBaselineProvenance Provenance() => new(
        "bdcb685fadcca9d18b18f013c052795a53b6aa33",
        "react-fixtures/src/canary.tsx",
        new string('A', 64),
        new DateTimeOffset(2026, 8, 9, 0, 0, 0, TimeSpan.Zero));

    private static void WriteBundleProvenance(
        string directory,
        FixtureEntry fixture,
        LiveBaselineProvenance provenance)
    {
        File.WriteAllText(
            Path.Combine(directory, ReactBundleProvenanceManifest.FileName),
            JsonSerializer.Serialize(new ReactBundleProvenanceManifest
            {
                SchemaVersion = ReactBundleProvenanceManifest.CurrentSchemaVersion,
                UpstreamSha = provenance.UpstreamSha,
                DistFingerprint = ReactBundleProvenance.Fingerprint(directory),
                GeneratedAtUtc = provenance.GeneratedAtUtc,
                Sources =
                [
                    new ReactBundleSource
                    {
                        Fixture = fixture.Id,
                        SourcePath = provenance.SourcePath,
                        SourceHash = provenance.SourceHash
                    }
                ]
            }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
