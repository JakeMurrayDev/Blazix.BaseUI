using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Blazix.BaseUI.Parity.Tests.Baselines;
using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Diff;
using Blazix.BaseUI.Parity.Tests.Infrastructure;
using Blazix.BaseUI.Parity.Tests.Report;
using Blazix.BaseUI.Parity.Tests.Waivers;
using Shouldly;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

/// <summary>Pins deterministic, scope-complete JSON and offline HTML report packages.</summary>
public sealed class ReportWriterTests
{
    [Fact]
    public void ModelKeepsFixedDenominatorAndSeparatesThemeFromCandidateLegCounts()
    {
        var model = Model(executions:
        [
            Result("light", ParityLeg.BlazorServer),
            Result("light", ParityLeg.BlazorWasm),
            Result("dark", ParityLeg.BlazorServer),
            Result("dark", ParityLeg.BlazorWasm)
        ]);

        model.Scope.MilestoneFixtureDenominator.ShouldBe(29);
        model.Scope.MilestoneComponentDenominator.ShouldBe(26);
        model.Counts.AuthoredFixtureCount.ShouldBe(1);
        model.Counts.AuthoredThemeExecutionCount.ShouldBe(2);
        model.Counts.SelectedThemeExecutionCount.ShouldBe(2);
        model.Counts.MissingCatalogFixtureCount.ShouldBe(28);
        model.Counts.RequiredCandidateLegCount.ShouldBe(4);
        model.Counts.CompletedCandidateLegCount.ShouldBe(4);
        model.Counts.ExecutedFixtureCount.ShouldBe(1);
        model.Verdict.Kind.ShouldBe(ReportVerdictKind.Incomplete);
        model.Verdict.MilestoneClaim.ShouldBeFalse();
        model.Fixtures.Count.ShouldBe(29);
        model.Fixtures.Count(item => !item.Authored).ShouldBe(28);
        model.Fixtures.Where(item => !item.Authored)
            .ShouldAllBe(item => item.Themes.Count == 0);
    }

    [Theory]
    [InlineData("react")]
    [InlineData("execution")]
    [InlineData("theme")]
    public void ModelRejectsCandidateExecutionsThatCannotProveExactServerAndWasmCoverage(
        string defect)
    {
        var result = Result("light", ParityLeg.BlazorServer);
        result = defect switch
        {
            "react" => result with { Leg = ParityLeg.React },
            "execution" => result with { ExecutionId = "switch/hero@dark" },
            "theme" => result with { Theme = "dark" },
            _ => throw new ArgumentOutOfRangeException(nameof(defect))
        };

        Should.Throw<InvalidOperationException>(() => Model(executions: [result]));
    }

    [Theory]
    [InlineData(ParityLeg.BlazorServer)]
    [InlineData(ParityLeg.BlazorWasm)]
    public void ModelRejectsDuplicateCandidateLegsThatCannotReplaceTheOtherRequiredMode(
        ParityLeg duplicateLeg)
    {
        var result = Result("light", duplicateLeg);

        Should.Throw<InvalidOperationException>(() => Model(executions: [result, result]));
    }

    [Fact]
    public void OrdersControllingEvidenceBeforePairDependentAndTimelineSubordinateEvidence()
    {
        var timelineDetail = Finding(FindingKind.Timeline, "duration:opacity", "root > button");
        var timelineLevelOne = Finding(FindingKind.Timeline, string.Empty, string.Empty);
        var attribute = Finding(FindingKind.Attribute, "aria-checked", "root > button");
        var structure = Finding(FindingKind.Structure, "missing:root > button", "root > button");
        var model = Model(findings: [timelineDetail, timelineLevelOne, attribute, structure]);

        model.Findings.Select(item => item.Effective).ShouldBe(
            [structure, attribute, timelineLevelOne, timelineDetail]);
        model.Findings.Select(item => item.Tier).ShouldBe(
            [
                ReportEvidenceTier.Primary,
                ReportEvidenceTier.Subordinate,
                ReportEvidenceTier.Primary,
                ReportEvidenceTier.Subordinate
            ]);
        model.Findings.ShouldAllBe(item => item.Blocking);
    }

    [Fact]
    public void WaivedPrimaryEvidenceRemainsVisibleWithExactReviewMetadata()
    {
        var finding = Finding(FindingKind.Structure, "missing:root > button", "root > button");
        var waiver = new Waiver
        {
            Fixture = finding.Fixture,
            Leg = finding.Leg,
            Step = finding.Step,
            NodePath = finding.NodePath,
            Kind = finding.Kind,
            Property = finding.Property,
            Reason = "Documented structural limitation.",
            Disposition = WaiverDisposition.AcceptedLimitation,
            DocLink = "docs/audits/switch-functional-audit.md",
            Expires = new DateOnly(2026, 9, 1)
        };
        var applied = new AppliedWaiver(0, waiver, 0, finding);
        var model = Model(findings: [finding], applied: [applied]);

        var evidence = model.Findings.ShouldHaveSingleItem();
        evidence.Tier.ShouldBe(ReportEvidenceTier.Primary);
        evidence.Disposition.ShouldBe(ReportDisposition.AcceptedLimitation);
        evidence.Blocking.ShouldBeFalse();
        var reportWaiver = model.AppliedWaivers.ShouldHaveSingleItem();
        reportWaiver.Finding.ShouldBe(finding);
        reportWaiver.Reason.ShouldBe(waiver.Reason);
        reportWaiver.DocLink.ShouldBe(waiver.DocLink);
        reportWaiver.Expires.ShouldBe(waiver.Expires);
        reportWaiver.IssuePolicyStatus.ShouldBe(WaiverIssuePolicyStatus.NotRequired);
    }

    [Fact]
    public void PreservesCompletionFailureAndSkippedDependentActionFromCanonicalTrace()
    {
        ActionExecution[] actions =
        [
            Action(0, ActionExecutionStatus.CompletionUnmet),
            Action(1, ActionExecutionStatus.Skipped)
        ];
        var completion = new ActionCompletionFailure
        {
            Fixture = "switch/hero",
            Leg = ParityLeg.BlazorServer,
            Step = "initial",
            ActionIndex = 0,
            Verb = "click",
            Selector = "button",
            Predicate = "state",
            ExpectedValue = "visible",
            Observed = "{\"matches\":1,\"actual\":false}"
        };
        var execution = Result("light", ParityLeg.BlazorServer, actions, completion);

        var step = Model(executions: [execution]).Executions.ShouldHaveSingleItem()
            .Steps.ShouldHaveSingleItem();

        step.Actions.ShouldBe(actions);
        step.ActionCompletionFailures.ShouldBe([completion]);
    }

    [Fact]
    public void ExecutionRetainsFullReferenceAndCandidateCaptureEvidence()
    {
        var execution = Model(executions:
            [Result("light", ParityLeg.BlazorServer)]).Executions.ShouldHaveSingleItem();

        var reference = execution.Reference.ShouldNotBeNull().Steps.ShouldHaveSingleItem();
        var candidate = execution.Candidate.ShouldNotBeNull().Steps.ShouldHaveSingleItem();
        reference.Dom.Text.ShouldBe("Switch");
        candidate.Dom.Attributes["role"].ShouldBe("switch");
        candidate.Styles["root > button"]["opacity"].ShouldBe("1");
        candidate.Timeline.ShouldHaveSingleItem().Kind.ShouldBe("attribute");
        candidate.Actions.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("placeholder", "Required")]
    [InlineData("url", "https://playwright.dev")]
    [InlineData("children", "equal")]
    public void ReportProjectionRoundTripsPlaywrightAriaPropertiesWithoutTreatingThemAsMachinePaths(
        string property,
        string value)
    {
        var captured = $"- text: Name\n- textbox \"Name\":\n  - /{property}: {value}";
        var result = Result("light", ParityLeg.BlazorServer);
        result = result with
        {
            Reference = result.Reference! with
            {
                Steps =
                [
                    result.Reference.Steps[0] with { Aria = captured }
                ]
            }
        };

        var model = Model(executions: [result]);

        model.Executions.ShouldHaveSingleItem().Reference.ShouldNotBeNull()
            .Steps.ShouldHaveSingleItem().Aria.ShouldBe(captured);
        var json = JsonReportWriter.Render(model);
        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("executions")[0]
            .GetProperty("reference").GetProperty("steps")[0]
            .GetProperty("aria").GetString().ShouldBe(captured);
        System.Net.WebUtility.HtmlDecode(HtmlReportWriter.Render(model))
            .ShouldContain(captured);
    }

    [Theory]
    [InlineData("- textbox \"Name\":\n  - /Users/private/field-report")]
    [InlineData("- textbox \"Name\":\n  - /url: /Users/private/field-report")]
    [InlineData("- textbox \"Name\":\n  - /placeholder: file:///Users/private/field-report")]
    [InlineData("- list:\n  - /children: \\\\server\\share\\field-report")]
    public void ReportProjectionDoesNotHideAnAbsolutePathInAriaText(string captured)
    {
        var result = Result("light", ParityLeg.BlazorServer);
        result = result with
        {
            Reference = result.Reference! with
            {
                Steps =
                [
                    result.Reference.Steps[0] with { Aria = captured }
                ]
            }
        };

        var model = Model(executions: [result]);

        model.Executions.ShouldHaveSingleItem().Reference.ShouldNotBeNull()
            .Steps.ShouldHaveSingleItem().Aria.ShouldBe(captured);
        var exception = Should.Throw<InvalidOperationException>(() =>
            JsonReportWriter.Render(model));
        exception.Message.ShouldContain("$.executions[0].reference.steps[0].aria");
    }

    [Fact]
    public void PreservesGlobalRetryClassificationsAndBlockingExecutionFailures()
    {
        var stable = Finding(FindingKind.Attribute, "aria-checked");
        var flaky = Finding(FindingKind.ComputedStyle, "opacity") with
        {
            Severity = Severity.Flaky
        };
        var nonWaivable = Finding(
            FindingKind.CorrespondenceUncertain,
            "correspondence:root > button");
        var identityChanged = Finding(FindingKind.ComputedStyle, "color");
        var retry = new RetryVerdict
        {
            Evidence =
            [
                RetryEvidence(stable, RetryFindingClassification.Stable),
                RetryEvidence(flaky, RetryFindingClassification.Flaky),
                RetryEvidence(nonWaivable, RetryFindingClassification.NonWaivable),
                RetryEvidence(identityChanged, RetryFindingClassification.IdentityChanged)
            ],
            Findings = [stable, flaky, nonWaivable, identityChanged],
            Failures =
            [
                new RetryFailure(2, RetryAttemptState.ExecutionFailure, "browser closed")
            ]
        };

        var model = Model(
            findings: retry.Findings,
            retryVerdict: retry);

        model.Findings.Select(item => item.Classification).ShouldBe(
            [
                RetryFindingClassification.NonWaivable,
                RetryFindingClassification.Stable,
                RetryFindingClassification.Flaky,
                RetryFindingClassification.IdentityChanged
            ]);
        model.Findings.Select(item => item.Disposition).ShouldBe(
            [
                ReportDisposition.NonWaivable,
                ReportDisposition.Blocking,
                ReportDisposition.Flaky,
                ReportDisposition.Blocking
            ]);
        model.Diagnostics.ShouldContain(item =>
            item.Source == ReportDiagnosticSource.Retry &&
            item.Blocking &&
            item.Message == "browser closed");
        model.Verdict.MilestoneClaim.ShouldBeFalse();
    }

    [Fact]
    public void RejectsEffectiveFindingsWithoutExactRetryEvidence()
    {
        var finding = Finding(FindingKind.Attribute, "aria-checked");
        var retry = new RetryVerdict
        {
            Evidence = [],
            Findings = [],
            Failures = []
        };
        var waivers = new WaiverVerdict
        {
            Findings = [finding],
            Applied = [],
            BlockingFindings = [finding],
            NonWaivableFindings = [],
            Diagnostics = []
        };

        Should.Throw<InvalidOperationException>(() => Model(
            retryVerdict: retry,
            waiverVerdict: waivers));
    }

    [Fact]
    public void PreservesMalformedExpiredUnusedAmbiguousAndUnverifiedWaiverDiagnostics()
    {
        var finding = Finding(FindingKind.Attribute, "aria-checked");
        var waiver = WaiverFor(finding);
        WaiverDiagnostic Matcher(
            int index,
            WaiverDiagnosticKind kind,
            string message) => new(index, waiver, kind, message);
        var waiverVerdict = new WaiverVerdict
        {
            Findings = [finding],
            Applied = [],
            BlockingFindings = [finding],
            NonWaivableFindings = [],
            Diagnostics =
            [
                Matcher(3, WaiverDiagnosticKind.Unused, "waiver was unused"),
                Matcher(4, WaiverDiagnosticKind.Ambiguous, "waiver overlaps another entry"),
                Matcher(
                    5,
                    WaiverDiagnosticKind.IssuePolicyUnverified,
                    "issue policy is unverified")
            ]
        };
        ReportDiagnostic Loader(int index, string kind, string message) => new()
            {
                Source = ReportDiagnosticSource.WaiverLoader,
                Kind = kind,
                Message = message,
                Blocking = true,
                WaiverIndex = index
            };

        var model = Model(
            findings: [finding],
            waiverVerdict: waiverVerdict,
            policyDiagnostics:
            [
                Loader(1, "Expired", "waiver expired before review"),
                Loader(2, "Malformed", "waiver has a duplicate property")
            ]);

        model.Diagnostics.Select(item => (item.Source, item.Kind, item.WaiverIndex)).ShouldBe(
            [
                (ReportDiagnosticSource.WaiverLoader, "Expired", 1),
                (ReportDiagnosticSource.WaiverLoader, "Malformed", 2),
                (ReportDiagnosticSource.WaiverMatcher, "Unused", 3),
                (ReportDiagnosticSource.WaiverMatcher, "Ambiguous", 4),
                (ReportDiagnosticSource.WaiverMatcher, "IssuePolicyUnverified", 5)
            ]);
        model.Diagnostics.ShouldAllBe(item => item.Blocking);
    }

    [Fact]
    public void BaselineProvenancePreservesSchemaThreeHashesPlatformAndFixtureContracts()
    {
        var fixtureHash = new string('B', 64);
        var contractHash = new string('C', 64);
        var snapshot = BaselineSnapshot(fixtureHash, contractHash);

        var model = Model(baseline: snapshot);

        model.Provenance.AuthoritySchemaVersion.ShouldBe(3);
        model.Provenance.CaptureSchemaVersion.ShouldBe(CaptureSchema.CurrentVersion);
        model.Provenance.DeclaredRepositoryPin.ShouldBe(new string('a', 40));
        model.Provenance.UpstreamSha.ShouldBe(new string('a', 40));
        model.Provenance.Platform.ShouldBe(snapshot.Set.Platform);
        model.Provenance.GeneratedAtUtc.ShouldBe(snapshot.Set.GeneratedAtUtc);
        model.Provenance.FixtureManifestHash.ShouldBe(fixtureHash);
        model.Provenance.AliasManifestHash.ShouldBe(new string('D', 64));
        model.Provenance.StylesheetHash.ShouldBe(new string('E', 64));
        var provenance = model.Provenance.Fixtures.ShouldHaveSingleItem();
        provenance.Fixture.ShouldBe("switch/hero");
        provenance.Theme.ShouldBe("light");
        provenance.SourcePath.ShouldBe("switch/demos/hero/tailwind/index.tsx");
        provenance.SourceHash.ShouldBe(new string('F', 64));
        provenance.ContractHash.ShouldBe(contractHash);
    }

    [Fact]
    public void WriteBaselinePreservesOneConsistentReceiptPerSelectedRawFixture()
    {
        var receipt = new BaselineWriteReceipt(
            "switch/hero",
            new BaselinePlatform
            {
                Browser = "chromium",
                BrowserVersion = "140.0.7339.16",
                Os = "macos",
                Architecture = "arm64"
            },
            new DateTimeOffset(2026, 8, 10, 1, 1, 0, TimeSpan.Zero),
            new string('A', 64));
        var executions = new[]
        {
            Result("light", ParityLeg.BlazorServer) with { BaselineWrite = receipt },
            Result("light", ParityLeg.BlazorWasm) with { BaselineWrite = receipt }
        };

        var model = Model(
            executions: executions,
            mode: ParityReferenceMode.WriteBaseline);

        model.Provenance.BaselineWrites.ShouldBe([receipt]);
        model.Executions.ShouldAllBe(item => item.BaselineWrite == receipt);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("inconsistent")]
    [InlineData("live")]
    [InlineData("baseline")]
    public void RejectsMissingInconsistentOrUnexpectedBaselineWriteReceipts(string defect)
    {
        var receipt = new BaselineWriteReceipt(
            "switch/hero",
            new BaselinePlatform
            {
                Browser = "chromium",
                BrowserVersion = "140.0.7339.16",
                Os = "macos",
                Architecture = "arm64"
            },
            new DateTimeOffset(2026, 8, 10, 1, 1, 0, TimeSpan.Zero),
            new string('A', 64));
        var server = Result("light", ParityLeg.BlazorServer) with
        {
            BaselineWrite = receipt
        };
        var wasm = Result("light", ParityLeg.BlazorWasm) with
        {
            BaselineWrite = defect == "missing"
                ? null
                : defect == "inconsistent"
                    ? receipt with { CaptureSha256 = new string('B', 64) }
                    : receipt
        };
        var mode = defect switch
        {
            "live" => ParityReferenceMode.Live,
            "baseline" => ParityReferenceMode.Baseline,
            _ => ParityReferenceMode.WriteBaseline
        };

        Should.Throw<InvalidOperationException>(() => Model(
            executions: [server, wasm],
            baseline: defect == "baseline"
                ? BaselineSnapshot(new string('B', 64), new string('C', 64))
                : null,
            mode: mode));
    }

    [Fact]
    public void LiveProvenanceRequiresEveryAuthoredFixtureThemeInManifestOrder()
    {
        var authority = new BaselineAuthoritySnapshot(
            BaselineSnapshot(new string('B', 64), new string('C', 64)).Authority);
        var switchFixture = Fixture();
        var collapsibleFixture = Fixture("collapsible/hero");
        var switchLive = new LiveBaselineProvenance(
            authority.Authority.DeclaredRepositoryPin,
            "docs/src/app/(docs)/react/components/" + switchFixture.React,
            new string('F', 64),
            new DateTimeOffset(2026, 8, 10, 1, 0, 0, TimeSpan.Zero));
        var collapsibleLive = switchLive with
        {
            SourcePath = "docs/src/app/(docs)/react/components/" + collapsibleFixture.React,
            SourceHash = new string('E', 64)
        };
        LiveFixtureProvenanceSnapshot[] entries =
        [
            new("collapsible/hero", "dark", new string('C', 64), collapsibleLive),
            new("switch/hero", "dark", new string('D', 64), switchLive),
            new("collapsible/hero", "light", new string('C', 64), collapsibleLive),
            new("switch/hero", "light", new string('D', 64), switchLive)
        ];

        var model = Model(
            fixtures: [switchFixture, collapsibleFixture],
            authority: authority,
            liveProvenance: entries);

        model.Provenance.DeclaredRepositoryPin.ShouldBe(
            authority.Authority.DeclaredRepositoryPin);
        model.Provenance.AuthoritySchemaVersion.ShouldBe(
            authority.Authority.SchemaVersion);
        model.Provenance.Fixtures.Select(item => $"{item.Fixture}@{item.Theme}")
            .ShouldBe(
            [
                "switch/hero@light",
                "switch/hero@dark",
                "collapsible/hero@light",
                "collapsible/hero@dark"
            ]);
        model.Provenance.Fixtures.Select(item => item.Fixture).Distinct()
            .ShouldBe(["switch/hero", "collapsible/hero"]);
        model.Provenance.Fixtures.ShouldAllBe(item =>
            item.SourcePath.EndsWith("/demos/hero/tailwind/index.tsx", StringComparison.Ordinal) &&
            item.GeneratedAtUtc == switchLive.GeneratedAtUtc);

        Should.Throw<InvalidOperationException>(() => Model(
            fixtures: [switchFixture, collapsibleFixture],
            authority: authority,
            liveProvenance: [entries[0]]));
    }

    [Fact]
    public void LiveAuthorityAndContractSnapshotsRequireNoPlatformSetAndCannotBeForgedPublicly()
    {
        using var directory = TemporaryDirectory.Create();
        var root = Path.Combine(directory.Path, "baselines");
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "metadata.json"), $$"""
            {
              "schemaVersion": 3,
              "captureSchemaVersion": {{CaptureSchema.CurrentVersion}},
              "declaredRepositoryPin": "{{new string('a', 40)}}"
            }
            """);
        var aliases = Path.Combine(directory.Path, "aliases.json");
        File.WriteAllText(aliases, "{}");
        var store = new BaselineStore(
            root,
            Path.Combine(directory.Path, "screenshots"),
            Path.Combine(directory.Path, "missing-fixtures.json"),
            Path.Combine(directory.Path, "missing-parity.css"),
            aliasManifestPath: aliases);
        var fixture = Fixture();
        var live = new LiveBaselineProvenance(
            new string('a', 40),
            "docs/src/app/(docs)/react/components/" + fixture.React,
            new string('A', 64),
            new DateTimeOffset(2026, 8, 10, 1, 0, 0, TimeSpan.Zero));

        var authority = store.DescribeAuthority();
        var fixtureSnapshot = store.DescribeLiveFixture(fixture, "light", live);

        authority.Authority.DeclaredRepositoryPin.ShouldBe(new string('a', 40));
        Directory.EnumerateDirectories(root).ShouldBeEmpty();
        fixtureSnapshot.ContractHash.ShouldNotBe(new string('0', 64));
        typeof(BaselineAuthoritySnapshot).GetConstructors().ShouldBeEmpty();
        typeof(LiveFixtureProvenanceSnapshot).GetConstructors().ShouldBeEmpty();
    }

    [Fact]
    public void ReportsEveryRegisteredComparatorInProductionOrderIncludingEmptyThresholdSets()
    {
        var expected = new ComparatorRegistry().OrderedKinds;

        var comparators = Model().Scope.Comparators;

        comparators.Select(item => item.Kind).ShouldBe(expected);
        comparators.Where(item => item.Kind is FindingKind.Attribute or FindingKind.Focus)
            .ShouldAllBe(item => item.Thresholds.Count == 0);
        comparators.Single(item => item.Kind == FindingKind.Pixel)
            .Thresholds.ShouldNotBeEmpty();
    }

    [Fact]
    public void JsonIsDeterministicUsesStringEnumsAndRejectsMachinePaths()
    {
        var model = Model(findings: [Finding(FindingKind.Attribute, "aria-checked")]);

        var first = JsonReportWriter.Render(model);
        var second = JsonReportWriter.Render(model);

        first.ShouldBe(second);
        var json = Encoding.UTF8.GetString(first);
        json.ShouldContain("\"schemaVersion\": 1");
        json.ShouldContain("\"kind\": \"Incomplete\"");
        json.ShouldContain("\"kind\": \"Attribute\"");
        json.ShouldContain(
            "\"sourcePath\": \"docs/src/app/(docs)/react/components/" +
            "switch/demos/hero/tailwind/index.tsx\"");
        json.ShouldNotContain("artifactSources");

        var unsafeModel = model with
        {
            Diagnostics =
            [
                new ReportDiagnostic
                {
                    Kind = "probe",
                    Message = "failed at /Users/example/private.txt"
                }
            ]
        };
        Should.Throw<InvalidOperationException>(() => JsonReportWriter.Render(unsafeModel));
    }

    [Fact]
    public void JsonAndHtmlPreserveValidatedWholeFixtureAttemptPlatformAndTimingEvidence()
    {
        var platform = new BaselinePlatform
        {
            Browser = "chromium",
            BrowserVersion = "140.0.0.0",
            Os = "linux",
            Architecture = "x64"
        };
        var executions = new[]
        {
            Result("light", ParityLeg.BlazorServer),
            Result("light", ParityLeg.BlazorWasm),
            Result("dark", ParityLeg.BlazorServer),
            Result("dark", ParityLeg.BlazorWasm)
        };
        var model = Model(
            executions: executions,
            attempts:
            [
                new ReportAttemptTiming
                {
                    Fixture = "switch/hero",
                    Attempt = 1,
                    Platform = platform,
                    Duration = TimeSpan.FromMilliseconds(125)
                },
                new ReportAttemptTiming
                {
                    Fixture = "switch/hero",
                    Attempt = 2,
                    Platform = platform,
                    Duration = TimeSpan.FromMilliseconds(150)
                }
            ]);

        var json = System.Text.Encoding.UTF8.GetString(JsonReportWriter.Render(model));
        var html = HtmlReportWriter.Render(model);

        model.Attempts.Select(item => item.Attempt).ShouldBe([1, 2]);
        json.ShouldContain("\"duration\": \"00:00:00.1250000\"");
        json.ShouldContain("\"browserVersion\": \"140.0.0.0\"");
        html.ShouldContain("Attempts");
        html.ShouldContain("140.0.0.0");
        html.ShouldContain("00:00:00.1500000");
    }

    [Fact]
    public void RejectsIncompleteDuplicateOrCrossPlatformAttemptTimingEvidence()
    {
        var platform = new BaselinePlatform
        {
            Browser = "chromium",
            BrowserVersion = "140.0.0.0",
            Os = "linux",
            Architecture = "x64"
        };
        var executions = new[]
        {
            Result("light", ParityLeg.BlazorServer),
            Result("light", ParityLeg.BlazorWasm)
        };
        var first = new ReportAttemptTiming
        {
            Fixture = "switch/hero",
            Attempt = 1,
            Platform = platform,
            Duration = TimeSpan.FromMilliseconds(1)
        };

        Should.Throw<InvalidOperationException>(() => Model(
            executions: executions,
            attempts: [first]));
        Should.Throw<InvalidOperationException>(() => Model(
            executions: executions,
            attempts: [first, first]));
        Should.Throw<InvalidOperationException>(() => Model(
            executions: executions,
            attempts:
            [
                first,
                first with
                {
                    Attempt = 2,
                    Platform = platform with { BrowserVersion = "141.0.0.0" }
                }
            ]));
    }

    [Fact]
    public void IncompleteBaselineOrEmptyFilteredLiveRunUsesAuthorityOnlyProvenance()
    {
        var authority = new BaselineAuthoritySnapshot(
            BaselineSnapshot(new string('B', 64), new string('C', 64)).Authority);
        var baseline = Model(
            fixtures: [],
            authority: authority,
            mode: ParityReferenceMode.Baseline,
            policyDiagnostics:
            [
                new ReportDiagnostic
                {
                    Source = ReportDiagnosticSource.Provenance,
                    Kind = "BaselineSnapshotUnavailable",
                    Message = "Missing platform set.",
                    Blocking = true
                }
            ]);
        var live = Model(
            fixtures: [],
            filter: "collapsible/hero",
            authority: authority,
            liveProvenance: [],
            mode: ParityReferenceMode.Live);

        baseline.Provenance.Fixtures.ShouldBeEmpty();
        baseline.Provenance.Platform.ShouldBeNull();
        baseline.Verdict.Kind.ShouldBe(ReportVerdictKind.Incomplete);
        live.Provenance.Fixtures.ShouldBeEmpty();
        live.Provenance.LiveDistFingerprint.ShouldBeNull();
        live.Verdict.Kind.ShouldBe(ReportVerdictKind.Diagnostic);
    }

    [Fact]
    public void HtmlEncodesHostileEvidenceAndUsesOnlyOfflineLocalResources()
    {
        var hostile = Finding(FindingKind.Attribute, "aria-label") with
        {
            Message = "<img src=x onerror=alert(1)>",
            ReferenceValue = "<&\"'",
            CandidateValue = "safe"
        };

        var html = HtmlReportWriter.Render(Model(findings: [hostile]));

        html.ShouldContain("&lt;img src=x onerror=alert(1)&gt;");
        html.ShouldNotContain("<img src=x onerror");
        html.ShouldContain("href=\"report.css\"");
        html.ShouldNotContain("<script", Case.Insensitive);
        html.ShouldNotContain("http://", Case.Insensitive);
        html.ShouldNotContain("https://", Case.Insensitive);
        html.ShouldContain("Content-Security-Policy");
    }

    [Fact]
    public void VerifiedDeferredIssueUrlRendersAsEncodedTextWithoutBecomingANetworkResource()
    {
        using var directory = TemporaryDirectory.Create();
        var finding = Finding(FindingKind.Attribute, "aria-checked");
        var path = Path.Combine(directory.Path, "waivers.json");
        File.WriteAllText(path, $$"""
            [
              {
                "fixture": "{{finding.Fixture}}",
                "leg": "{{finding.Leg}}",
                "step": "{{finding.Step}}",
                "nodePath": "{{finding.NodePath}}",
                "kind": "{{finding.Kind}}",
                "property": "{{finding.Property}}",
                "reason": "Captured twice with acceptance criteria.",
                "disposition": "deferred-defect",
                "docLink": "https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/999999",
                "expires": "2026-09-01"
              }
            ]
            """);
        var waiver = WaiverFile.Load(
            path,
            new DateOnly(2026, 8, 10),
            new VerifiedIssuePolicyValidator()).ShouldHaveSingleItem();
        var model = Model(
            findings: [finding],
            applied: [new AppliedWaiver(0, waiver, 0, finding)]);

        var json = Encoding.UTF8.GetString(JsonReportWriter.Render(model));
        var html = HtmlReportWriter.Render(model);

        json.ShouldContain(waiver.DocLink);
        System.Net.WebUtility.HtmlDecode(html).ShouldContain(waiver.DocLink);
        html.ShouldNotContain($"href=\"{waiver.DocLink}");
        html.ShouldNotContain($"src=\"{waiver.DocLink}");
    }

    [Fact]
    public void FilteredPackageCannotOverwriteCanonicalAndCopiesHashedLocalArtifacts()
    {
        using var directory = TemporaryDirectory.Create();
        var canonical = Path.Combine(directory.Path, "canonical");
        var diagnostics = Path.Combine(directory.Path, "diagnostics");
        Directory.CreateDirectory(canonical);
        File.WriteAllText(Path.Combine(canonical, "sentinel.txt"), "full report");
        var screenshot = Path.Combine(directory.Path, "shot.png");
        File.WriteAllBytes(screenshot, [1, 2, 3, 4]);
        var artifact = Artifact(screenshot, "assets/screenshots/switch.light.React.initial.00.png");
        var model = Model(filter: "switch/hero", artifacts: [artifact]);

        model.Scope.MilestoneFixtureDenominator.ShouldBe(29);
        model.Counts.SelectedThemeExecutionCount.ShouldBe(2);
        model.Verdict.Kind.ShouldBe(ReportVerdictKind.Diagnostic);
        model.Verdict.MilestoneClaim.ShouldBeFalse();

        var result = new ReportPackageWriter().Write(
            model, canonical, diagnostics, "switch-hero");

        result.Directory.ShouldBe(Path.Combine(diagnostics, "switch-hero"));
        File.ReadAllText(Path.Combine(canonical, "sentinel.txt")).ShouldBe("full report");
        File.Exists(Path.Combine(result.Directory, "parity-result.json")).ShouldBeTrue();
        File.Exists(Path.Combine(result.Directory, "index.html")).ShouldBeTrue();
        File.Exists(Path.Combine(result.Directory, "report.css")).ShouldBeTrue();
        File.ReadAllBytes(Path.Combine(
            result.Directory,
            "assets",
            "screenshots",
            "switch.light.React.initial.00.png")).ShouldBe([1, 2, 3, 4]);
        var group = model.Artifacts.ShouldHaveSingleItem();
        group.React.ShouldNotBeNull();
        group.Candidate.ShouldBeNull();
        group.Diff.ShouldBeNull();
    }

    [Fact]
    public void FilteredPackageRejectsAResolvedTargetThatAliasesCanonicalOutput()
    {
        using var directory = TemporaryDirectory.Create();
        var canonical = Path.Combine(directory.Path, "canonical");
        Directory.CreateDirectory(canonical);
        File.WriteAllText(Path.Combine(canonical, "sentinel.txt"), "full report");
        var diagnostics = Path.Combine(directory.Path, ".", "diagnostics", "..");

        Should.Throw<InvalidOperationException>(() => new ReportPackageWriter().Write(
            Model(filter: "switch/hero"), canonical, diagnostics, "canonical"));
        File.ReadAllText(Path.Combine(canonical, "sentinel.txt")).ShouldBe("full report");
    }

    [Fact]
    public void ArtifactGroupsAreCanonicalRegardlessOfInputEnumerationOrder()
    {
        using var directory = TemporaryDirectory.Create();
        var first = Path.Combine(directory.Path, "first.png");
        var second = Path.Combine(directory.Path, "second.png");
        File.WriteAllBytes(first, [1]);
        File.WriteAllBytes(second, [2]);
        var laterBase = Artifact(
            second,
            "assets/screenshots/switch.dark.React.initial.01.png");
        var later = laterBase with
        {
            Artifact = laterBase.Artifact with
            {
                Theme = "dark",
                ExecutionId = "switch/hero@dark",
                CandidateLeg = ParityLeg.BlazorWasm,
                Shot = "01"
            }
        };
        var earlier = Artifact(first, "assets/screenshots/switch.light.React.initial.00.png");

        var forward = Model(artifacts: [earlier, later]);
        var reverse = Model(artifacts: [later, earlier]);

        reverse.Artifacts.ShouldBe(forward.Artifacts);
        JsonReportWriter.Render(reverse).ShouldBe(JsonReportWriter.Render(forward));
    }

    [Fact]
    public void BuildsEveryRequiredFindingBreakdownWithoutMixingRawAndThemeIdentity()
    {
        var blocking = Finding(FindingKind.Attribute, "aria-checked");
        var info = Finding(FindingKind.Console, "console:note", string.Empty) with
        {
            Severity = Severity.Info
        };
        var model = Model(findings: [blocking, info]);

        model.Counts.ByComponent.ShouldBe(new Dictionary<string, int> { ["switch"] = 2 });
        model.Counts.ByKind.ShouldBe(new Dictionary<string, int>
        {
            ["Attribute"] = 1,
            ["Console"] = 1
        });
        model.Counts.BySeverity.ShouldBe(new Dictionary<string, int>
        {
            ["Error"] = 1,
            ["Info"] = 1
        });
        model.Counts.ByLeg.ShouldBe(new Dictionary<string, int> { ["BlazorServer"] = 2 });
        model.Counts.ByFixture.ShouldBe(new Dictionary<string, int>
        {
            ["switch/hero@light"] = 2
        });
        model.Counts.ByDisposition.ShouldBe(new Dictionary<string, int>
        {
            ["Blocking"] = 1,
            ["Informational"] = 1
        });
    }

    [Fact]
    public void UncertainStructureNeverProducesAnEqualityLabel()
    {
        var finding = Finding(
            FindingKind.CorrespondenceUncertain,
            "correspondence:root > button");
        var model = Model(findings: [finding]);

        var json = Encoding.UTF8.GetString(JsonReportWriter.Render(model));
        var html = HtmlReportWriter.Render(model);

        json.ShouldContain("CorrespondenceUncertain");
        html.ShouldContain("CorrespondenceUncertain");
        json.ShouldNotContain("\"equal\": true", Case.Insensitive);
        html.ShouldNotContain(">Equal<", Case.Insensitive);
        html.ShouldNotContain("Parity achieved", Case.Insensitive);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("hash")]
    [InlineData("duplicate")]
    public void PackageRejectsMissingTamperedAndCollidingArtifactsBeforeChangingOutput(string defect)
    {
        using var directory = TemporaryDirectory.Create();
        var first = Path.Combine(directory.Path, "first.png");
        var second = Path.Combine(directory.Path, "second.png");
        File.WriteAllBytes(first, [1, 2, 3]);
        File.WriteAllBytes(second, [4, 5, 6]);
        var relative = "assets/screenshots/switch.light.React.initial.00.png";
        var artifact = Artifact(first, relative);
        IReadOnlyList<ReportArtifactSource> artifacts = defect switch
        {
            "missing" => [artifact with { SourcePath = Path.Combine(directory.Path, "missing.png") }],
            "hash" => [artifact with
            {
                Artifact = artifact.Artifact with { Sha256 = new string('0', 64) }
            }],
            "duplicate" =>
            [
                artifact,
                Artifact(second, relative) with
                {
                    Artifact = Artifact(second, relative).Artifact with
                    {
                        Leg = ParityLeg.BlazorServer,
                        Role = "Candidate"
                    }
                }
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(defect))
        };
        var model = Model(artifacts: artifacts);
        var canonical = Path.Combine(directory.Path, "canonical");

        Should.Throw<InvalidOperationException>(() => new ReportPackageWriter().Write(
            model, canonical, Path.Combine(directory.Path, "diagnostics"), null));
        Directory.Exists(canonical).ShouldBeFalse();
    }

    [Theory]
    [InlineData("execution")]
    [InlineData("role-leg")]
    [InlineData("candidate-leg")]
    [InlineData("fixture")]
    public void ModelRejectsInternallyInconsistentThreeUpArtifactMetadata(string defect)
    {
        using var directory = TemporaryDirectory.Create();
        var screenshot = Path.Combine(directory.Path, "shot.png");
        File.WriteAllBytes(screenshot, [1, 2, 3]);
        var source = Artifact(
            screenshot,
            "assets/screenshots/switch.light.React.initial.00.png");
        source = defect switch
        {
            "execution" => source with
            {
                Artifact = source.Artifact with { ExecutionId = "switch/hero@dark" }
            },
            "role-leg" => source with
            {
                Artifact = source.Artifact with { Role = "Candidate" }
            },
            "candidate-leg" => source with
            {
                Artifact = source.Artifact with { CandidateLeg = ParityLeg.React }
            },
            "fixture" => source with
            {
                Artifact = source.Artifact with { Fixture = "select/hero" }
            },
            _ => throw new ArgumentOutOfRangeException(nameof(defect))
        };

        Should.Throw<InvalidOperationException>(() => Model(artifacts: [source]));
    }

    [Theory]
    [InlineData("/absolute/shot.png")]
    [InlineData("../shot.png")]
    [InlineData("assets\\shot.png")]
    [InlineData("https://example.com/shot.png")]
    public void PackageRejectsUnsafeArtifactPathsBeforeChangingOutput(string relativePath)
    {
        using var directory = TemporaryDirectory.Create();
        var screenshot = Path.Combine(directory.Path, "shot.png");
        File.WriteAllBytes(screenshot, [1, 2, 3]);
        var model = Model(artifacts: [Artifact(screenshot, relativePath)]);
        var canonical = Path.Combine(directory.Path, "canonical");

        Should.Throw<InvalidOperationException>(() => new ReportPackageWriter().Write(
            model, canonical, Path.Combine(directory.Path, "diagnostics"), null));
        Directory.Exists(canonical).ShouldBeFalse();
    }

    [Fact]
    public void AtomicReplacementRestoresExactPriorPackageWhenBackupCleanupFails()
    {
        using var directory = TemporaryDirectory.Create();
        var canonical = Path.Combine(directory.Path, "canonical");
        var diagnostics = Path.Combine(directory.Path, "diagnostics");
        var initial = Model();
        new ReportPackageWriter().Write(initial, canonical, diagnostics, null);
        var originalJson = File.ReadAllBytes(Path.Combine(canonical, "parity-result.json"));
        var replacement = initial with { GeneratedAtUtc = initial.GeneratedAtUtc.AddMinutes(1) };
        var writer = new ReportPackageWriter((path, recursive) =>
        {
            if (path.Contains(".bak", StringComparison.Ordinal))
            {
                throw new IOException("backup cleanup probe");
            }

            Directory.Delete(path, recursive);
        });

        var exception = Should.Throw<IOException>(() =>
            writer.Write(replacement, canonical, diagnostics, null));

        exception.Message.ShouldContain("backup cleanup probe");
        File.ReadAllBytes(Path.Combine(canonical, "parity-result.json")).ShouldBe(originalJson);
    }

    [Fact]
    public void AtomicReplacementRestoresPriorPackageWhenEveryInjectedDeletionFails()
    {
        using var directory = TemporaryDirectory.Create();
        var canonical = Path.Combine(directory.Path, "canonical");
        var diagnostics = Path.Combine(directory.Path, "diagnostics");
        var initial = Model();
        new ReportPackageWriter().Write(initial, canonical, diagnostics, null);
        var originalJson = File.ReadAllBytes(Path.Combine(canonical, "parity-result.json"));
        var replacement = initial with { GeneratedAtUtc = initial.GeneratedAtUtc.AddMinutes(1) };
        var writer = new ReportPackageWriter((_, _) =>
            throw new IOException("all deletion paths fail"));

        var exception = Should.Throw<IOException>(() =>
            writer.Write(replacement, canonical, diagnostics, null));

        exception.Message.ShouldContain("all deletion paths fail");
        File.ReadAllBytes(Path.Combine(canonical, "parity-result.json")).ShouldBe(originalJson);
    }

    [Theory]
    [InlineData("diagnostic mentions /opt/build/report.json")]
    [InlineData("diagnostic mentions /private/var/folders/output.json")]
    [InlineData("diagnostic mentions \\\\server\\share\\report.json")]
    [InlineData("diagnostic mentions //server/share/report.json")]
    public void JsonRejectsEmbeddedPosixAndUncMachinePaths(string message)
    {
        var diagnostic = new ReportDiagnostic
        {
            Source = ReportDiagnosticSource.Execution,
            Kind = "FixtureError",
            Message = message
        };

        Should.Throw<InvalidOperationException>(() =>
            JsonReportWriter.Render(Model(policyDiagnostics: [diagnostic])));
        Should.Throw<InvalidOperationException>(() =>
            HtmlReportWriter.Render(Model(policyDiagnostics: [diagnostic])));
    }

    [Theory]
    // A captured `href`, which is the whole field value.
    [InlineData("/react/overview/quick-start")]
    [InlineData("/react/handbook/styling")]
    [InlineData("/parity.css")]
    [InlineData("/fixture/navigation-menu/hero/server")]
    // The same reference quoted inside a manifest selector or a finding's prose.
    [InlineData("a[href='/react/overview/quick-start']")]
    [InlineData("expected href=\"/react/handbook/styling\"")]
    // A Playwright aria snapshot whose `/url:` value is a root-relative reference.
    [InlineData("- link \"Quick Start\":\n  - /url: /react/overview/quick-start")]
    // Query and fragment forms of the same reference.
    [InlineData("/react/overview/quick-start?tab=tailwind")]
    [InlineData("/react/overview/quick-start#install")]
    // The remaining route roots the fixture sites serve: the WASM leg's host page,
    // the React site's built bundle, and the Blazor static web assets.
    [InlineData("/fixture/navigation-menu/hero/wasm")]
    [InlineData("/react/assets/index-CzQHf14l.js")]
    [InlineData("/_content/Blazix.BaseUI/blazix-baseui-navigation-menu.min.js")]
    [InlineData("/_framework/blazor.web.js")]
    public void JsonAndHtmlAcceptRootRelativeUrlReferences(string message)
    {
        var diagnostic = new ReportDiagnostic
        {
            Source = ReportDiagnosticSource.Execution,
            Kind = "FixtureError",
            Message = message
        };

        var json = JsonReportWriter.Render(Model(policyDiagnostics: [diagnostic]));

        JsonDocument.Parse(json).RootElement
            .GetProperty("diagnostics")[0]
            .GetProperty("message").GetString().ShouldBe(message);
        System.Net.WebUtility.HtmlDecode(
                HtmlReportWriter.Render(Model(policyDiagnostics: [diagnostic])))
            .ShouldContain(message);
    }

    [Theory]
    // Home directories on every platform the harness runs on.
    [InlineData("/Users/example/private.txt")]
    [InlineData("/home/runner/work/parity/report.json")]
    // Temporary directories.
    [InlineData("/tmp/parity-report/parity-result.json")]
    [InlineData("/private/var/folders/9k/T/parity-result.json")]
    // Other filesystem roots, including ones carrying a space.
    [InlineData("/opt/build/report.json")]
    [InlineData("/Volumes/Backup Disk/parity-report")]
    [InlineData("/etc/hosts")]
    [InlineData("/usr/local/share/parity")]
    // Windows drive, UNC, protocol-relative, and file-URI spellings.
    [InlineData("C:\\Users\\example\\report.json")]
    [InlineData("\\\\server\\share\\report.json")]
    [InlineData("//server/share/report.json")]
    [InlineData("file:///Users/private/field-report")]
    // Traversal and the bare filesystem root itself.
    [InlineData("/react/../../../etc/passwd")]
    [InlineData("/")]
    // Embedded in prose rather than standing alone.
    [InlineData("capture failed at /Users/example/private.txt")]
    [InlineData("- textbox \"Name\":\n  - /url: /home/runner/private")]
    // CI and sandbox roots that appear at no fixed place in any filesystem-root
    // inventory. A sibling checkout on a GitLab runner whose own CI_PROJECT_DIR is
    // /builds/group/repo is still a machine-local path.
    [InlineData("/builds/group/other-repo/artifacts/report.json")]
    [InlineData("/workspace/parity/parity-result.json")]
    [InlineData("/data/ci/artifacts/parity-result.json")]
    [InlineData("/nix/store/9v1w0rk-node-22.11.0/bin/node")]
    [InlineData("/scratch/agent-7/output/parity-result.json")]
    // Near misses on the served route roots: matching must be per-segment, never a
    // string prefix.
    [InlineData("/reactor/build/parity-result.json")]
    [InlineData("/react-fixtures/dist/index.html")]
    [InlineData("/fixtures/navigation-menu/report.json")]
    [InlineData("/parity.css.bak/report.json")]
    // The same CI roots embedded in prose and in a Playwright aria snapshot value.
    [InlineData("capture failed at /builds/group/other-repo/artifacts/report.json")]
    [InlineData("- link \"Node\":\n  - /url: /nix/store/9v1w0rk-node-22.11.0/bin/node")]
    public void JsonAndHtmlStillRejectMachineLocalPaths(string message)
    {
        var diagnostic = new ReportDiagnostic
        {
            Source = ReportDiagnosticSource.Execution,
            Kind = "FixtureError",
            Message = message
        };

        Should.Throw<InvalidOperationException>(() =>
            JsonReportWriter.Render(Model(policyDiagnostics: [diagnostic])));
        Should.Throw<InvalidOperationException>(() =>
            HtmlReportWriter.Render(Model(policyDiagnostics: [diagnostic])));
    }

    [Fact]
    public void JsonRejectsThisMachinesOwnRepositoryTemporaryAndProfileRootsWhateverTheyAre()
    {
        var roots = new[]
        {
            Path.GetFullPath(Path.Combine(ParityPaths.HarnessRoot, "..", "..")),
            Path.GetTempPath(),
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            AppContext.BaseDirectory
        };

        foreach (var root in roots)
        {
            var diagnostic = new ReportDiagnostic
            {
                Source = ReportDiagnosticSource.Execution,
                Kind = "FixtureError",
                Message = Path.Combine(root, "parity-report", "parity-result.json")
            };

            Should.Throw<InvalidOperationException>(
                () => JsonReportWriter.Render(Model(policyDiagnostics: [diagnostic])),
                $"'{root}' is a machine-local root and must never reach the report.");
        }
    }

    [Fact]
    public async Task ReportStylesheetUsesThePinnedTailwindOfflineGenerationContract()
    {
        var harness = ParityPaths.HarnessRoot;
        var packageJson = File.ReadAllText(Path.Combine(harness, "react-fixtures", "package.json"));
        var source = File.ReadAllText(Path.Combine(
            harness,
            "Blazix.BaseUI.Parity.Tests",
            "Report",
            "report.source.css"));
        var compiled = File.ReadAllText(Path.Combine(
            harness,
            "Blazix.BaseUI.Parity.Tests",
            "Report",
            "report.css"));

        packageJson.ShouldContain("\"parity:report-css\"");
        packageJson.ShouldContain("tailwindcss");
        packageJson.ShouldContain("\"4.2.4\"");
        source.ShouldStartWith("@import \"tailwindcss\" source(none);");
        compiled.ShouldNotContain("@import", Case.Insensitive);
        compiled.ShouldNotContain("url(", Case.Insensitive);

        var start = new ProcessStartInfo
        {
            FileName = OperatingSystem.IsWindows() ? "pnpm.cmd" : "pnpm",
            WorkingDirectory = Path.Combine(harness, "react-fixtures"),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.ArgumentList.Add("parity:report-css:check");
        using var process = Process.Start(start).ShouldNotBeNull();
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var output = await outputTask;
        var error = await errorTask;
        process.ExitCode.ShouldBe(0, output + error);
    }

    private static ReportModel Model(
        IReadOnlyList<ParityRunResult>? executions = null,
        IReadOnlyList<FixtureEntry>? fixtures = null,
        IReadOnlyList<Finding>? findings = null,
        IReadOnlyList<AppliedWaiver>? applied = null,
        string? filter = null,
        IReadOnlyList<ReportArtifactSource>? artifacts = null,
        RetryVerdict? retryVerdict = null,
        WaiverVerdict? waiverVerdict = null,
        IReadOnlyList<ReportDiagnostic>? policyDiagnostics = null,
        BaselineSnapshot? baseline = null,
        BaselineAuthoritySnapshot? authority = null,
        IReadOnlyList<LiveFixtureProvenanceSnapshot>? liveProvenance = null,
        ParityReferenceMode? mode = null,
        IReadOnlyList<ReportAttemptTiming>? attempts = null)
    {
        var effective = findings ?? [];
        var retryEvidence = effective.Select(finding => new RetryFindingEvidence
        {
            Identity = FindingIdentity.From(finding),
            FirstAttempt = finding,
            Effective = finding,
            Classification = finding.Severity switch
            {
                Severity.Info => RetryFindingClassification.Informational,
                Severity.Flaky => RetryFindingClassification.Flaky,
                _ when ComparatorRegistry.NonWaivableKinds.Contains(finding.Kind) =>
                    RetryFindingClassification.NonWaivable,
                _ => RetryFindingClassification.Stable
            }
        }).ToArray();
        var appliedWaivers = applied ?? [];
        var blocking = effective.Where(finding =>
            finding.Severity == Severity.Error &&
            appliedWaivers.All(item => !item.Finding.Equals(finding))).ToArray();

        var retry = retryVerdict ?? new RetryVerdict
        {
            Evidence = retryEvidence,
            Findings = effective,
            Failures = []
        };
        var waivers = waiverVerdict ?? new WaiverVerdict
        {
            Findings = effective,
            Applied = appliedWaivers,
            BlockingFindings = blocking,
            NonWaivableFindings = blocking.Where(item =>
                ComparatorRegistry.NonWaivableKinds.Contains(item.Kind)).ToArray(),
            Diagnostics = []
        };

        return ReportModel.Create(new ReportModelInput
        {
            GeneratedAtUtc = new DateTimeOffset(2026, 8, 10, 1, 2, 3, TimeSpan.Zero),
            Options = new ParityOptions
            {
                Mode = mode ?? (baseline is null
                    ? ParityReferenceMode.Live
                    : ParityReferenceMode.Baseline),
                FixtureFilter = filter
            },
            Fixtures = fixtures ?? [Fixture()],
            Executions = executions ?? [],
            RetryVerdict = retry,
            WaiverVerdict = waivers,
            Baseline = baseline,
            Authority = baseline is null
                ? authority ?? new BaselineAuthoritySnapshot(
                    BaselineSnapshot(new string('B', 64), new string('C', 64)).Authority)
                : null,
            LiveProvenance = baseline is null && mode != ParityReferenceMode.Baseline
                ? liveProvenance ?? DefaultLiveProvenance()
                : [],
            LiveDistFingerprint = baseline is null && mode != ParityReferenceMode.Baseline &&
                                  (fixtures is null || fixtures.Count > 0)
                ? new string('9', 64)
                : null,
            ArtifactSources = artifacts ?? [],
            Attempts = attempts ?? [],
            PolicyDiagnostics = policyDiagnostics ?? []
        });
    }

    private static IReadOnlyList<LiveFixtureProvenanceSnapshot> DefaultLiveProvenance()
    {
        var provenance = new LiveBaselineProvenance(
            new string('a', 40),
            "docs/src/app/(docs)/react/components/switch/demos/hero/tailwind/index.tsx",
            new string('A', 64),
            new DateTimeOffset(2026, 8, 10, 1, 0, 0, TimeSpan.Zero));
        return
        [
            new("switch/hero", "light", new string('D', 64), provenance),
            new("switch/hero", "dark", new string('D', 64), provenance)
        ];
    }

    private static RetryFindingEvidence RetryEvidence(
        Finding finding,
        RetryFindingClassification classification) => new()
        {
            Identity = FindingIdentity.From(finding),
            FirstAttempt = finding,
            Effective = finding,
            Classification = classification
        };

    private static Waiver WaiverFor(Finding finding) => new()
    {
        Fixture = finding.Fixture,
        Leg = finding.Leg,
        Step = finding.Step,
        NodePath = finding.NodePath,
        Kind = finding.Kind,
        Property = finding.Property,
        Reason = "Report diagnostic probe.",
        Disposition = WaiverDisposition.AcceptedLimitation,
        DocLink = "docs/audits/switch-functional-audit.md",
        Expires = new DateOnly(2026, 9, 1)
    };

    private static BaselineSnapshot BaselineSnapshot(
        string fixtureManifestHash,
        string contractHash)
    {
        var platform = new BaselinePlatform
        {
            Browser = "chromium",
            BrowserVersion = "140.0.7339.16",
            Os = "macos",
            Architecture = "arm64"
        };
        return new BaselineSnapshot(
            new BaselineAuthority
            {
                SchemaVersion = 3,
                CaptureSchemaVersion = CaptureSchema.CurrentVersion,
                DeclaredRepositoryPin = new string('a', 40)
            },
            new BaselineSetMetadata
            {
                SchemaVersion = 3,
                CaptureSchemaVersion = CaptureSchema.CurrentVersion,
                UpstreamSha = new string('a', 40),
                Platform = platform,
                GeneratedAtUtc = new DateTimeOffset(2026, 8, 9, 2, 0, 0, TimeSpan.Zero),
                FixtureManifestHash = fixtureManifestHash,
                AliasManifestHash = new string('D', 64),
                StylesheetHash = new string('E', 64),
                Fixtures =
                [
                    new BaselineFixtureMetadata
                    {
                        Fixture = "switch/hero",
                        SourcePath = "switch/demos/hero/tailwind/index.tsx",
                        SourceHash = new string('F', 64),
                        ContractHash = contractHash,
                        Theme = "light",
                        Steps = ["initial"],
                        Capture = "captures/switch__hero.light.json",
                        Artifacts = []
                    }
                ]
            });
    }

    private static FixtureEntry Fixture(string id = "switch/hero")
    {
        var separator = id.IndexOf('/');
        var component = id[..separator];
        var demo = id[(separator + 1)..];
        return new FixtureEntry
        {
            Id = id,
            Component = component,
            React = $"{component}/demos/{demo}/tailwind/index.tsx",
            Blazor = $"{char.ToUpperInvariant(component[0])}{component[1..]}/" +
                     $"{char.ToUpperInvariant(demo[0])}{demo[1..]}",
            Themes = ["light", "dark"],
            Steps = [new StepEntry { Name = "initial" }]
        };
    }

    private static ParityRunResult Result(
        string theme,
        ParityLeg leg,
        IReadOnlyList<ActionExecution>? actions = null,
        params ActionCompletionFailure[] failures)
    {
        var reference = Bundle(theme, ParityLeg.React, actions, failures);
        var candidate = Bundle(theme, leg, actions, failures);
        return new ParityRunResult
        {
            Fixture = "switch/hero",
            Theme = theme,
            ExecutionId = $"switch/hero@{theme}",
            Leg = leg,
            Reference = reference,
            Candidate = candidate,
            Findings = []
        };
    }

    private static CaptureBundle Bundle(
        string theme,
        ParityLeg leg,
        IReadOnlyList<ActionExecution>? actions,
        IReadOnlyList<ActionCompletionFailure> failures) => new()
        {
            CaptureSchemaVersion = CaptureSchema.CurrentVersion,
            Fixture = "switch/hero",
            Leg = leg,
            BaseUiSha = new string('a', 40),
            SourceHash = new string('A', 64),
            Theme = theme,
            Steps =
            [
                new StepCapture
                {
                    Step = "initial",
                    Dom = new DomNode
                    {
                        Tag = "button",
                        Path = "root > button",
                        Attributes = new Dictionary<string, string> { ["role"] = "switch" },
                        Classes = [],
                        Text = "Switch",
                        Children = []
                    },
                    Styles = new Dictionary<string, IReadOnlyDictionary<string, string>>
                    {
                        ["root > button"] = new Dictionary<string, string> { ["opacity"] = "1" }
                    },
                    CustomProps = new Dictionary<string, IReadOnlyDictionary<string, string>>(),
                    Geometry = new Dictionary<string, IReadOnlyDictionary<string, double>>(),
                    Timeline =
                    [
                        new TimelineEvent
                        {
                            T = 10,
                            Kind = "attribute",
                            Path = "root > button",
                            Attr = "aria-checked",
                            From = "false",
                            To = "true"
                        }
                    ],
                    Actions = actions ?? [],
                    ActionCompletionFailures = failures
                }
            ]
        };

    private static ActionExecution Action(int index, ActionExecutionStatus status) => new()
    {
        ActionIndex = index,
        Verb = "click",
        ExpandedSelector = "button",
        Status = status
    };

    private static Finding Finding(
        FindingKind kind,
        string property,
        string nodePath = "root > button") => new()
        {
            Fixture = "switch/hero@light",
            Leg = ParityLeg.BlazorServer,
            Step = "initial",
            Kind = kind,
            Severity = Severity.Error,
            NodePath = nodePath,
            Property = property,
            ReferenceValue = "reference",
            CandidateValue = "candidate",
            Message = $"{kind} evidence"
        };

    private static ReportArtifactSource Artifact(string sourcePath, string relativePath) => new()
    {
        SourcePath = sourcePath,
        Artifact = new ReportArtifact
        {
            RelativePath = relativePath,
            Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(sourcePath))),
            MediaType = "image/png",
            Fixture = "switch/hero",
            Theme = "light",
            ExecutionId = "switch/hero@light",
            Leg = ParityLeg.React,
            Step = "initial",
            Shot = "00",
            CandidateLeg = ParityLeg.BlazorServer,
            Role = "React"
        }
    };

    private sealed class TemporaryDirectory : IDisposable
    {
        private TemporaryDirectory(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TemporaryDirectory Create()
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"blazix-report-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(path);
            return new TemporaryDirectory(path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }

    private sealed class VerifiedIssuePolicyValidator : IWaiverIssuePolicyValidator
    {
        public WaiverIssuePolicyValidation Validate(string issueUrl)
            => new(
                IsOpen: true,
                IsOwned: true,
                CapturedAttemptCount: 2,
                HasAcceptanceCriteria: true);
    }
}
