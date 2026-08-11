using System.Text.Json.Serialization;
using Blazix.BaseUI.Parity.Tests.Baselines;
using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Diff;
using Blazix.BaseUI.Parity.Tests.Infrastructure;
using Blazix.BaseUI.Parity.Tests.Waivers;

namespace Blazix.BaseUI.Parity.Tests.Report;

/// <summary>The top-level report outcome.</summary>
public enum ReportVerdictKind
{
    /// <summary>All milestone evidence is complete and nonblocking.</summary>
    Passed,

    /// <summary>All required evidence executed, but parity or policy still blocks.</summary>
    Failed,

    /// <summary>Required fixture, theme, leg, step, or action evidence is missing.</summary>
    Incomplete,

    /// <summary>The run was filtered and cannot make a milestone claim.</summary>
    Diagnostic
}

/// <summary>Presentation precedence for one finding.</summary>
public enum ReportEvidenceTier
{
    /// <summary>Controlling truth or execution evidence.</summary>
    Primary,

    /// <summary>Detail subordinate to controlling evidence.</summary>
    Subordinate,

    /// <summary>Nonblocking context, retry, or policy detail.</summary>
    Diagnostic
}

/// <summary>How one effective finding contributes to policy.</summary>
public enum ReportDisposition
{
    /// <summary>An unwaived parity error.</summary>
    Blocking,

    /// <summary>Evidence that policy never permits waiving.</summary>
    NonWaivable,

    /// <summary>A reviewed accepted limitation.</summary>
    AcceptedLimitation,

    /// <summary>A reviewed deferred defect.</summary>
    DeferredDefect,

    /// <summary>A proven retry-only difference.</summary>
    Flaky,

    /// <summary>Nonblocking contextual evidence.</summary>
    Informational
}

/// <summary>The authority that produced a report diagnostic.</summary>
public enum ReportDiagnosticSource
{
    Policy,
    WaiverLoader,
    WaiverMatcher,
    Retry,
    Execution,
    Provenance,
    Artifact
}

/// <summary>One typed report diagnostic.</summary>
public sealed record ReportDiagnostic
{
    /// <summary>Gets the diagnostic authority.</summary>
    public ReportDiagnosticSource Source { get; init; } = ReportDiagnosticSource.Policy;

    /// <summary>Gets the stable diagnostic category.</summary>
    public required string Kind { get; init; }

    /// <summary>Gets the bounded diagnostic message.</summary>
    public required string Message { get; init; }

    /// <summary>Gets whether the diagnostic blocks a milestone claim.</summary>
    public bool Blocking { get; init; } = true;

    /// <summary>Gets the waiver registry index, when applicable.</summary>
    public int? WaiverIndex { get; init; }
}

/// <summary>One report-relative artifact and its integrity metadata.</summary>
public sealed record ReportArtifact
{
    /// <summary>Gets the report-relative POSIX path.</summary>
    public required string RelativePath { get; init; }

    /// <summary>Gets the uppercase SHA-256.</summary>
    public required string Sha256 { get; init; }

    /// <summary>Gets the media type.</summary>
    public required string MediaType { get; init; }

    /// <summary>Gets the fixture id.</summary>
    public required string Fixture { get; init; }

    /// <summary>Gets the captured theme.</summary>
    public required string Theme { get; init; }

    /// <summary>Gets the fixture-theme execution id.</summary>
    public required string ExecutionId { get; init; }

    /// <summary>Gets the candidate or reference leg.</summary>
    public required ParityLeg Leg { get; init; }

    /// <summary>Gets the manifest step.</summary>
    public required string Step { get; init; }

    /// <summary>Gets the stable screenshot shot id.</summary>
    public required string Shot { get; init; }

    /// <summary>Gets the candidate leg whose three-up group owns the artifact.</summary>
    public required ParityLeg CandidateLeg { get; init; }

    /// <summary>Gets the stable artifact role.</summary>
    public required string Role { get; init; }
}

/// <summary>One explicit React/candidate/diff evidence group.</summary>
public sealed record ReportArtifactGroup
{
    public required string Fixture { get; init; }

    public required string Theme { get; init; }

    public required string ExecutionId { get; init; }

    public required ParityLeg CandidateLeg { get; init; }

    public required string Step { get; init; }

    public required string Shot { get; init; }

    public ReportArtifact? React { get; init; }

    public ReportArtifact? Candidate { get; init; }

    public ReportArtifact? Diff { get; init; }
}

/// <summary>One local artifact source supplied to the atomic package writer.</summary>
public sealed record ReportArtifactSource
{
    /// <summary>Gets the local source path, which is never serialized.</summary>
    public required string SourcePath { get; init; }

    /// <summary>Gets the public report artifact metadata.</summary>
    public required ReportArtifact Artifact { get; init; }
}

/// <summary>One declared fixture in fixed milestone order.</summary>
public sealed record ReportFixtureDeclaration
{
    public required string Fixture { get; init; }

    public required string Component { get; init; }

    public required int CatalogOrdinal { get; init; }

    public required bool Authored { get; init; }

    public required bool Selected { get; init; }

    public required IReadOnlyList<string> Themes { get; init; }

    public required IReadOnlyList<string> Steps { get; init; }

    public required double PixelThreshold { get; init; }

    public IReadOnlyList<string> Exclusions { get; init; } = [];
}

/// <summary>One fixture-theme-candidate execution.</summary>
public sealed record ReportExecution
{
    public required string Fixture { get; init; }

    public required string Theme { get; init; }

    public required string ExecutionId { get; init; }

    public required ParityLeg Leg { get; init; }

    public required bool Complete { get; init; }

    public CaptureBundle? Reference { get; init; }

    public CaptureBundle? Candidate { get; init; }

    public BaselineWriteReceipt? BaselineWrite { get; init; }

    public required IReadOnlyList<ReportStepExecution> Steps { get; init; }
}

/// <summary>One manifest step and its canonical action trace.</summary>
public sealed record ReportStepExecution
{
    public required string Step { get; init; }

    public required IReadOnlyList<ActionExecution> Actions { get; init; }

    public required IReadOnlyList<ActionCompletionFailure> ActionCompletionFailures { get; init; }
}

/// <summary>One effective finding with complete retry and policy evidence.</summary>
public sealed record ReportFindingEvidence
{
    public required FindingIdentity Identity { get; init; }

    public Finding? FirstAttempt { get; init; }

    public Finding? RetryAttempt { get; init; }

    public required Finding Effective { get; init; }

    public required RetryFindingClassification Classification { get; init; }

    public required ReportEvidenceTier Tier { get; init; }

    public required ReportDisposition Disposition { get; init; }

    public required bool Blocking { get; init; }
}

/// <summary>A consumed waiver with the exact retained finding evidence.</summary>
public sealed record ReportAppliedWaiver
{
    public required int WaiverIndex { get; init; }

    public required int FindingIndex { get; init; }

    public required Finding Finding { get; init; }

    public required string Reason { get; init; }

    public required WaiverDisposition Disposition { get; init; }

    public required WaiverPropertyMatch PropertyMatch { get; init; }

    public required string DocLink { get; init; }

    public required DateOnly Expires { get; init; }

    public required WaiverIssuePolicyStatus IssuePolicyStatus { get; init; }
}

/// <summary>Fixed and observed scope for one report.</summary>
public sealed record ReportScope
{
    public int MilestoneFixtureDenominator { get; init; } = 29;

    public int MilestoneComponentDenominator { get; init; } = 26;

    public required bool Filtered { get; init; }

    public string? FixtureFilter { get; init; }

    public required IReadOnlyList<string> CatalogFixtures { get; init; }

    public required IReadOnlyList<string> RequiredModes { get; init; }

    public required IReadOnlyList<ComparatorDescriptor> Comparators { get; init; }
}

/// <summary>Validated baseline or live provenance.</summary>
public sealed record ReportProvenance
{
    public required bool Complete { get; init; }

    public required string Mode { get; init; }

    public string? DeclaredRepositoryPin { get; init; }

    public string? UpstreamSha { get; init; }

    public int? AuthoritySchemaVersion { get; init; }

    public int? CaptureSchemaVersion { get; init; }

    public BaselinePlatform? Platform { get; init; }

    public string? FixtureManifestHash { get; init; }

    public string? AliasManifestHash { get; init; }

    public string? StylesheetHash { get; init; }

    public string? LiveDistFingerprint { get; init; }

    public DateTimeOffset? GeneratedAtUtc { get; init; }

    public IReadOnlyList<ReportFixtureProvenance> Fixtures { get; init; } = [];

    public IReadOnlyList<BaselineWriteReceipt> BaselineWrites { get; init; } = [];
}

/// <summary>Browser platform and elapsed time for one whole-fixture attempt.</summary>
public sealed record ReportAttemptTiming
{
    public required string Fixture { get; init; }

    public required int Attempt { get; init; }

    public required BaselinePlatform Platform { get; init; }

    public required TimeSpan Duration { get; init; }
}

/// <summary>Exact source and contract provenance for one fixture-theme capture.</summary>
public sealed record ReportFixtureProvenance
{
    public required string Fixture { get; init; }

    public required string Theme { get; init; }

    public required string SourcePath { get; init; }

    public required string SourceHash { get; init; }

    public required string ContractHash { get; init; }

    public required DateTimeOffset GeneratedAtUtc { get; init; }
}

/// <summary>Invocation configuration reproduced in both output formats.</summary>
public sealed record ReportConfiguration
{
    public required ParityReferenceMode ReferenceMode { get; init; }

    public required IReadOnlyList<string> Exclusions { get; init; }
}

/// <summary>Aggregate scope and evidence counts.</summary>
public sealed record ReportCounts
{
    public required int AuthoredFixtureCount { get; init; }

    public required int AuthoredThemeExecutionCount { get; init; }

    public required int SelectedThemeExecutionCount { get; init; }

    public required int MissingCatalogFixtureCount { get; init; }

    public required int ExecutedFixtureCount { get; init; }

    public required int RequiredCandidateLegCount { get; init; }

    public required int CompletedCandidateLegCount { get; init; }

    public required int FindingCount { get; init; }

    public required int BlockingFindingCount { get; init; }

    public required IReadOnlyDictionary<string, int> ByComponent { get; init; }

    public required IReadOnlyDictionary<string, int> ByKind { get; init; }

    public required IReadOnlyDictionary<string, int> BySeverity { get; init; }

    public required IReadOnlyDictionary<string, int> ByLeg { get; init; }

    public required IReadOnlyDictionary<string, int> ByFixture { get; init; }

    public required IReadOnlyDictionary<string, int> ByDisposition { get; init; }
}

/// <summary>The policy-evaluated report verdict.</summary>
public sealed record ReportVerdict
{
    public required ReportVerdictKind Kind { get; init; }

    public required bool MilestoneClaim { get; init; }

    public required IReadOnlyList<string> BlockingReasons { get; init; }
}

/// <summary>Complete already-evaluated input to the report factory.</summary>
public sealed record ReportModelInput
{
    public required DateTimeOffset GeneratedAtUtc { get; init; }

    public required ParityOptions Options { get; init; }

    public required IReadOnlyList<FixtureEntry> Fixtures { get; init; }

    public required IReadOnlyList<ParityRunResult> Executions { get; init; }

    public required RetryVerdict RetryVerdict { get; init; }

    public required WaiverVerdict WaiverVerdict { get; init; }

    public BaselineSnapshot? Baseline { get; init; }

    public BaselineAuthoritySnapshot? Authority { get; init; }

    public IReadOnlyList<LiveFixtureProvenanceSnapshot> LiveProvenance { get; init; } = [];

    public string? LiveDistFingerprint { get; init; }

    public IReadOnlyList<ReportDiagnostic> PolicyDiagnostics { get; init; } = [];

    public IReadOnlyList<ReportArtifactSource> ArtifactSources { get; init; } = [];

    public IReadOnlyList<ReportAttemptTiming> Attempts { get; init; } = [];

    public IReadOnlyList<string> Exclusions { get; init; } = [];
}

/// <summary>Immutable shared source for the JSON and HTML reports.</summary>
public sealed record ReportModel
{
    public int SchemaVersion { get; init; } = 1;

    public required DateTimeOffset GeneratedAtUtc { get; init; }

    public required ReportScope Scope { get; init; }

    public required ReportProvenance Provenance { get; init; }

    public required ReportConfiguration Configuration { get; init; }

    public required ReportVerdict Verdict { get; init; }

    public required ReportCounts Counts { get; init; }

    public required IReadOnlyList<ReportAttemptTiming> Attempts { get; init; }

    public required IReadOnlyList<ReportFixtureDeclaration> Fixtures { get; init; }

    public required IReadOnlyList<ReportExecution> Executions { get; init; }

    public required IReadOnlyList<ReportFindingEvidence> Findings { get; init; }

    public required IReadOnlyList<ReportAppliedWaiver> AppliedWaivers { get; init; }

    public required IReadOnlyList<ReportDiagnostic> Diagnostics { get; init; }

    public required IReadOnlyList<ReportArtifactGroup> Artifacts { get; init; }

    [JsonIgnore]
    public IReadOnlyList<ReportArtifactSource> ArtifactSources { get; init; } = [];

    /// <summary>Builds the one shared deterministic report model.</summary>
    public static ReportModel Create(ReportModelInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.GeneratedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Report generation time must be UTC.", nameof(input));
        }

        var fixtureById = input.Fixtures.ToDictionary(item => item.Id, StringComparer.Ordinal);
        var declarations = MilestoneFixtureCatalog.Ids.Select((id, index) =>
        {
            fixtureById.TryGetValue(id, out var fixture);
            return new ReportFixtureDeclaration
            {
                Fixture = id,
                Component = id[..id.IndexOf('/')],
                CatalogOrdinal = index,
                Authored = fixture is not null,
                Selected = fixture is not null && MatchesFilter(id, input.Options.FixtureFilter),
                Themes = fixture?.Themes ?? [],
                Steps = fixture?.Steps.Select(step => step.Name).ToArray() ?? [],
                PixelThreshold = fixture?.PixelThreshold ?? 0,
                Exclusions = []
            };
        }).ToArray();

        var catalogOrder = MilestoneFixtureCatalog.Ids
            .Select((id, index) => (id, index))
            .ToDictionary(item => item.id, item => item.index, StringComparer.Ordinal);
        var selectedFixtures = declarations.Where(item => item.Selected)
            .Select(item => item.Fixture)
            .ToHashSet(StringComparer.Ordinal);
        var executions = input.Executions
            .Where(item => selectedFixtures.Contains(item.Fixture))
            .OrderBy(item => catalogOrder.GetValueOrDefault(item.Fixture, int.MaxValue))
            .ThenBy(item => ThemeOrdinal(fixtureById.GetValueOrDefault(item.Fixture), item.Theme))
            .ThenBy(item => item.Leg == ParityLeg.BlazorServer ? 0 : 1)
            .Select(ToExecution)
            .ToArray();
        var duplicateExecution = executions.GroupBy(item => new
            {
                item.ExecutionId,
                item.Leg
            })
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateExecution is not null)
        {
            throw new InvalidOperationException(
                $"Report execution '{duplicateExecution.Key.ExecutionId}' has duplicate " +
                $"'{duplicateExecution.Key.Leg}' candidate legs.");
        }

        var appliedByIdentity = input.WaiverVerdict.Applied
            .ToDictionary(item => FindingIdentity.From(item.Finding));
        var retryIdentities = input.RetryVerdict.Evidence.Select(item => item.Identity).ToArray();
        var retryFindingIdentities = input.RetryVerdict.Findings
            .Select(FindingIdentity.From).ToArray();
        var waiverFindingIdentities = input.WaiverVerdict.Findings
            .Select(FindingIdentity.From).ToArray();
        if (!ExactIdentitySet(retryIdentities, retryFindingIdentities) ||
            !ExactIdentitySet(retryIdentities, waiverFindingIdentities) ||
            input.RetryVerdict.Evidence.Any(item =>
                item.Identity != FindingIdentity.From(item.Effective) ||
                item.FirstAttempt is not null &&
                item.Identity != FindingIdentity.From(item.FirstAttempt) ||
                item.RetryAttempt is not null &&
                item.Identity != FindingIdentity.From(item.RetryAttempt)))
        {
            throw new InvalidOperationException(
                "Report findings require exact identity-correlated RetryVerdict evidence.");
        }

        var effective = input.RetryVerdict.Evidence;
        var findings = effective.Select((item, index) => new
            {
                Evidence = item,
                Index = index,
                Fixture = RawFixture(item.Identity.Fixture),
                Theme = Theme(item.Identity.Fixture)
            })
            .OrderBy(item => catalogOrder.GetValueOrDefault(item.Fixture, int.MaxValue))
            .ThenBy(item => ThemeOrdinal(
                fixtureById.GetValueOrDefault(item.Fixture), item.Theme))
            .ThenBy(item => item.Evidence.Identity.Leg == ParityLeg.BlazorServer ? 0 : 1)
            .ThenBy(item => StepOrdinal(
                fixtureById.GetValueOrDefault(item.Fixture), item.Evidence.Identity.Step))
            .ThenBy(item => KindOrdinal(item.Evidence.Effective.Kind))
            .ThenBy(item => TimelineOrdinal(item.Evidence.Effective))
            .ThenBy(item => item.Index)
            .Select(item => ToFinding(
                item.Evidence,
                appliedByIdentity.GetValueOrDefault(item.Evidence.Identity)))
            .ToArray();

        var applied = input.WaiverVerdict.Applied.Select(item => new ReportAppliedWaiver
        {
            WaiverIndex = item.WaiverIndex,
            FindingIndex = item.FindingIndex,
            Finding = item.Finding,
            Reason = item.Waiver.Reason,
            Disposition = item.Waiver.Disposition,
            PropertyMatch = item.Waiver.PropertyMatch,
            DocLink = item.Waiver.DocLink,
            Expires = item.Waiver.Expires,
            IssuePolicyStatus = item.Waiver.IssuePolicyStatus
        }).ToArray();
        var diagnostics = input.PolicyDiagnostics
            .Concat(input.WaiverVerdict.Diagnostics.Select(item => new ReportDiagnostic
            {
                Source = ReportDiagnosticSource.WaiverMatcher,
                Kind = item.Kind.ToString(),
                Message = item.Message,
                Blocking = true,
                WaiverIndex = item.WaiverIndex
            }))
            .Concat(input.RetryVerdict.Failures.Select(item => new ReportDiagnostic
            {
                Source = ReportDiagnosticSource.Retry,
                Kind = item.State.ToString(),
                Message = item.Message,
                Blocking = true
            }))
            .ToArray();

        var executedFixtures = declarations.Count(declaration =>
            declaration.Authored && declaration.Selected &&
            declaration.Themes.All(theme =>
                executions.Any(execution =>
                    execution.Fixture == declaration.Fixture &&
                    execution.Theme == theme &&
                    execution.Leg == ParityLeg.BlazorServer &&
                    execution.Complete) &&
                executions.Any(execution =>
                    execution.Fixture == declaration.Fixture &&
                    execution.Theme == theme &&
                    execution.Leg == ParityLeg.BlazorWasm &&
                    execution.Complete)));
        var requiredLegs = declarations.Where(item => item.Authored && item.Selected)
            .Sum(item => item.Themes.Count * 2);
        var completedLegs = executions.Count(item => item.Complete);
        var filtered = input.Options.FixtureFilter is not null;
        var baselineWrites = ValidateBaselineWrites(input, declarations);
        var attempts = ValidateAttempts(input);
        var blocking = findings.Count(item => item.Blocking);
        var reasons = new List<string>();
        if (executedFixtures != 29)
        {
            reasons.Add($"Only {executedFixtures}/29 milestone fixtures are complete.");
        }

        if (blocking > 0)
        {
            reasons.Add($"{blocking} effective finding(s) remain blocking.");
        }

        if (diagnostics.Any(item => item.Blocking))
        {
            reasons.Add("Policy, retry, provenance, or artifact diagnostics remain blocking.");
        }

        var verdictKind = filtered
            ? ReportVerdictKind.Diagnostic
            : executedFixtures != 29 || completedLegs != requiredLegs
                ? ReportVerdictKind.Incomplete
                : blocking > 0 || diagnostics.Any(item => item.Blocking)
                    ? ReportVerdictKind.Failed
                    : ReportVerdictKind.Passed;

        return new ReportModel
        {
            GeneratedAtUtc = input.GeneratedAtUtc,
            Scope = new ReportScope
            {
                Filtered = filtered,
                FixtureFilter = input.Options.FixtureFilter,
                CatalogFixtures = MilestoneFixtureCatalog.Ids,
                RequiredModes = [nameof(ParityLeg.BlazorServer), nameof(ParityLeg.BlazorWasm)],
                Comparators = ComparatorContract.Descriptors
            },
            Provenance = ToProvenance(input, declarations, baselineWrites),
            Configuration = new ReportConfiguration
            {
                ReferenceMode = input.Options.Mode,
                Exclusions = input.Exclusions
            },
            Verdict = new ReportVerdict
            {
                Kind = verdictKind,
                MilestoneClaim = verdictKind == ReportVerdictKind.Passed,
                BlockingReasons = reasons
            },
            Counts = BuildCounts(declarations, findings, executedFixtures, requiredLegs, completedLegs),
            Attempts = attempts,
            Fixtures = declarations,
            Executions = executions,
            Findings = findings,
            AppliedWaivers = applied,
            Diagnostics = diagnostics,
            Artifacts = GroupArtifacts(input.ArtifactSources, fixtureById, catalogOrder),
            ArtifactSources = input.ArtifactSources
        };
    }

    private static IReadOnlyList<ReportAttemptTiming> ValidateAttempts(ReportModelInput input)
    {
        if (input.Attempts.Count == 0)
        {
            return [];
        }

        var expected = input.Executions.Select(item => item.Fixture)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var supplied = input.Attempts.GroupBy(item => item.Fixture, StringComparer.Ordinal).ToArray();
        if (supplied.Length != expected.Length ||
            expected.Any(fixture => supplied.All(group =>
                !string.Equals(group.Key, fixture, StringComparison.Ordinal))))
        {
            throw new InvalidOperationException(
                "Report attempt timings must cover every executed raw fixture exactly.");
        }

        foreach (var group in supplied)
        {
            var rows = group.OrderBy(item => item.Attempt).ToArray();
            if (rows.Length != 2 || rows[0].Attempt != 1 || rows[1].Attempt != 2 ||
                rows.Any(item => item.Duration < TimeSpan.Zero) ||
                rows[0].Platform != rows[1].Platform)
            {
                throw new InvalidOperationException(
                    $"Report attempt timings for '{group.Key}' require attempts 1 and 2 " +
                    "with nonnegative durations and one exact platform.");
            }
        }

        var catalogOrder = MilestoneFixtureCatalog.Ids
            .Select((fixture, index) => (fixture, index))
            .ToDictionary(item => item.fixture, item => item.index, StringComparer.Ordinal);
        return input.Attempts
            .OrderBy(item => catalogOrder.GetValueOrDefault(item.Fixture, int.MaxValue))
            .ThenBy(item => item.Attempt)
            .ToArray();
    }

    private static ReportExecution ToExecution(ParityRunResult result)
    {
        if (result.Leg is not (ParityLeg.BlazorServer or ParityLeg.BlazorWasm) ||
            !FixtureExecution.IsExecutionId(result.ExecutionId) ||
            !string.Equals(result.ExecutionId, $"{result.Fixture}@{result.Theme}",
                StringComparison.Ordinal) ||
            result.Reference is { } reference &&
            (!string.Equals(reference.Fixture, result.Fixture, StringComparison.Ordinal) ||
             !string.Equals(reference.Theme, result.Theme, StringComparison.Ordinal) ||
             reference.Leg != ParityLeg.React) ||
            result.Candidate is { } candidate &&
            (!string.Equals(candidate.Fixture, result.Fixture, StringComparison.Ordinal) ||
             !string.Equals(candidate.Theme, result.Theme, StringComparison.Ordinal) ||
             candidate.Leg != result.Leg))
        {
            throw new InvalidOperationException(
                $"Report execution '{result.ExecutionId}' has inconsistent fixture, theme, or leg evidence.");
        }

        var steps = result.Candidate?.Steps.Select(step => new ReportStepExecution
        {
            Step = step.Step,
            Actions = step.Actions,
            ActionCompletionFailures = step.ActionCompletionFailures
        }).ToArray() ?? [];
        var complete = result.Reference is not null && result.Candidate is not null &&
                       result.Findings.All(finding => finding.Kind is not
                           (FindingKind.FixtureError or FindingKind.ActionCompletionUnmet)) &&
                       steps.All(step => step.Actions.All(action =>
                           action.Status == ActionExecutionStatus.Completed));

        return new ReportExecution
        {
            Fixture = result.Fixture,
            Theme = result.Theme,
            ExecutionId = result.ExecutionId,
            Leg = result.Leg,
            Complete = complete,
            Reference = result.Reference,
            Candidate = result.Candidate,
            BaselineWrite = result.BaselineWrite,
            Steps = steps
        };
    }

    private static ReportFindingEvidence ToFinding(
        RetryFindingEvidence evidence,
        AppliedWaiver? waiver)
    {
        var finding = evidence.Effective;
        var disposition = waiver?.Waiver.Disposition switch
        {
            WaiverDisposition.AcceptedLimitation => ReportDisposition.AcceptedLimitation,
            WaiverDisposition.DeferredDefect => ReportDisposition.DeferredDefect,
            _ when finding.Severity == Severity.Flaky => ReportDisposition.Flaky,
            _ when finding.Severity == Severity.Info => ReportDisposition.Informational,
            _ when ComparatorRegistry.NonWaivableKinds.Contains(finding.Kind) =>
                ReportDisposition.NonWaivable,
            _ => ReportDisposition.Blocking
        };

        return new ReportFindingEvidence
        {
            Identity = evidence.Identity,
            FirstAttempt = evidence.FirstAttempt,
            RetryAttempt = evidence.RetryAttempt,
            Effective = finding,
            Classification = evidence.Classification,
            Tier = Tier(finding),
            Disposition = disposition,
            Blocking = finding.Severity == Severity.Error && waiver is null
        };
    }

    private static ReportEvidenceTier Tier(Finding finding)
    {
        if (finding.Severity is Severity.Info or Severity.Flaky)
        {
            return ReportEvidenceTier.Diagnostic;
        }

        if (finding.Kind is FindingKind.Structure or FindingKind.CorrespondenceUncertain or
            FindingKind.ActionCompletionUnmet or FindingKind.FixtureError ||
            finding.Kind == FindingKind.Timeline &&
            string.IsNullOrEmpty(finding.NodePath) && string.IsNullOrEmpty(finding.Property))
        {
            return ReportEvidenceTier.Primary;
        }

        return ReportEvidenceTier.Subordinate;
    }

    private static ReportProvenance ToProvenance(
        ReportModelInput input,
        IReadOnlyList<ReportFixtureDeclaration> declarations,
        IReadOnlyList<BaselineWriteReceipt> baselineWrites)
    {
        if (input.Baseline is { } baseline)
        {
            return new ReportProvenance
            {
                Complete = true,
                Mode = input.Options.Mode.ToString(),
                DeclaredRepositoryPin = baseline.Authority.DeclaredRepositoryPin,
                UpstreamSha = baseline.Set.UpstreamSha,
                AuthoritySchemaVersion = baseline.Authority.SchemaVersion,
                CaptureSchemaVersion = baseline.Authority.CaptureSchemaVersion,
                Platform = baseline.Set.Platform,
                FixtureManifestHash = baseline.Set.FixtureManifestHash,
                AliasManifestHash = baseline.Set.AliasManifestHash,
                StylesheetHash = baseline.Set.StylesheetHash,
                GeneratedAtUtc = baseline.Set.GeneratedAtUtc,
                Fixtures = baseline.Set.Fixtures.Select(item => new ReportFixtureProvenance
                {
                    Fixture = item.Fixture,
                    Theme = item.Theme,
                    SourcePath = item.SourcePath,
                    SourceHash = item.SourceHash,
                    ContractHash = item.ContractHash,
                    GeneratedAtUtc = baseline.Set.GeneratedAtUtc
                }).ToArray(),
                BaselineWrites = baselineWrites
            };
        }

        var authority = input.Authority;
        if (authority is null)
        {
            if (!HasBlockingProvenanceDiagnostic(input, "AuthorityUnavailable"))
            {
                throw new ArgumentException(
                    "Reports without validated baseline authority require a matching blocking diagnostic.",
                    nameof(input));
            }

            return new ReportProvenance
            {
                Complete = false,
                Mode = input.Options.Mode.ToString(),
                DeclaredRepositoryPin = null,
                UpstreamSha = null,
                AuthoritySchemaVersion = null,
                CaptureSchemaVersion = null,
                BaselineWrites = baselineWrites
            };
        }
        var expected = declarations
            .Where(item => item.Authored && item.Selected)
            .SelectMany(item => item.Themes.Select(theme => (item.Fixture, Theme: theme)))
            .ToArray();
        if (input.Options.Mode == ParityReferenceMode.Baseline || expected.Length == 0)
        {
            if (input.Options.Mode == ParityReferenceMode.Baseline &&
                !HasBlockingProvenanceDiagnostic(input, "BaselineSnapshotUnavailable"))
            {
                throw new InvalidOperationException(
                    "Missing baseline provenance requires a matching blocking diagnostic.");
            }

            if (input.LiveProvenance.Count > 0 || input.LiveDistFingerprint is not null)
            {
                throw new InvalidOperationException(
                    "An authority-only report cannot claim live fixture provenance.");
            }

            return new ReportProvenance
            {
                Complete = false,
                Mode = input.Options.Mode.ToString(),
                DeclaredRepositoryPin = authority.Authority.DeclaredRepositoryPin,
                UpstreamSha = null,
                AuthoritySchemaVersion = authority.Authority.SchemaVersion,
                CaptureSchemaVersion = authority.Authority.CaptureSchemaVersion,
                BaselineWrites = baselineWrites
            };
        }

        var liveIncomplete = input.LiveDistFingerprint is null ||
                             input.LiveProvenance.Count != expected.Length ||
                             expected.Any(identity => input.LiveProvenance.All(item =>
                                 !string.Equals(item.Fixture, identity.Fixture, StringComparison.Ordinal) ||
                                 !string.Equals(item.Theme, identity.Theme, StringComparison.Ordinal)));
        if (liveIncomplete)
        {
            if (!HasBlockingProvenanceDiagnostic(input, "LiveProvenanceUnavailable"))
            {
                throw new InvalidOperationException(
                    "Incomplete live provenance requires a matching blocking diagnostic.");
            }

            return new ReportProvenance
            {
                Complete = false,
                Mode = input.Options.Mode.ToString(),
                DeclaredRepositoryPin = authority.Authority.DeclaredRepositoryPin,
                UpstreamSha = null,
                AuthoritySchemaVersion = authority.Authority.SchemaVersion,
                CaptureSchemaVersion = authority.Authority.CaptureSchemaVersion,
                BaselineWrites = baselineWrites
            };
        }

        if (input.LiveDistFingerprint is null ||
            input.LiveDistFingerprint.Length != 64 ||
            input.LiveDistFingerprint.Any(character =>
                character is not (>= '0' and <= '9' or >= 'A' and <= 'F')))
        {
            throw new ArgumentException(
                "Live/write reports require the validated uppercase dist fingerprint.",
                nameof(input));
        }

        var duplicate = input.LiveProvenance
            .GroupBy(item => (item.Fixture, item.Theme))
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"Live report provenance duplicates '{duplicate.Key.Fixture}@{duplicate.Key.Theme}'.");
        }

        var byExecution = input.LiveProvenance.ToDictionary(
            item => (item.Fixture, item.Theme));
        if (byExecution.Count != expected.Length ||
            expected.Any(item => !byExecution.ContainsKey(item)) ||
            byExecution.Keys.Any(item => !expected.Contains(item)))
        {
            throw new InvalidOperationException(
                "Live report provenance must cover every selected authored fixture-theme exactly once.");
        }

        var fixtures = expected.Select(identity =>
        {
            var item = byExecution[identity];
            var fixture = input.Fixtures.Single(entry =>
                string.Equals(entry.Id, identity.Fixture, StringComparison.Ordinal));
            var live = item.Provenance;
            if (!string.Equals(live.UpstreamSha, authority.Authority.DeclaredRepositoryPin,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    live.SourcePath,
                    LiveBaselineSource.ExpectedSourcePath(fixture),
                    StringComparison.Ordinal) ||
                live.GeneratedAtUtc.Offset != TimeSpan.Zero ||
                !IsUpperHex(live.SourceHash, 64) ||
                !IsUpperHex(item.ContractHash, 64))
            {
                throw new InvalidOperationException(
                    $"Live report provenance for '{identity.Fixture}@{identity.Theme}' is invalid.");
            }

            return new ReportFixtureProvenance
            {
                Fixture = identity.Fixture,
                Theme = identity.Theme,
                SourcePath = live.SourcePath,
                SourceHash = live.SourceHash,
                ContractHash = item.ContractHash,
                GeneratedAtUtc = live.GeneratedAtUtc
            };
        }).ToArray();

        return new ReportProvenance
        {
            Complete = true,
            Mode = input.Options.Mode.ToString(),
            DeclaredRepositoryPin = authority.Authority.DeclaredRepositoryPin,
            UpstreamSha = fixtures.Select(item => byExecution[(item.Fixture, item.Theme)]
                .Provenance.UpstreamSha).Distinct(StringComparer.Ordinal).Single(),
            AuthoritySchemaVersion = authority.Authority.SchemaVersion,
            CaptureSchemaVersion = authority.Authority.CaptureSchemaVersion,
            LiveDistFingerprint = input.LiveDistFingerprint,
            Fixtures = fixtures,
            BaselineWrites = baselineWrites
        };
    }

    private static bool HasBlockingProvenanceDiagnostic(ReportModelInput input, string kind)
        => input.PolicyDiagnostics.Any(item =>
            item.Source == ReportDiagnosticSource.Provenance &&
            item.Blocking &&
            string.Equals(item.Kind, kind, StringComparison.Ordinal));

    private static IReadOnlyList<BaselineWriteReceipt> ValidateBaselineWrites(
        ReportModelInput input,
        IReadOnlyList<ReportFixtureDeclaration> declarations)
    {
        var supplied = input.Executions.Where(item => item.BaselineWrite is not null).ToArray();
        if (HasBlockingProvenanceDiagnostic(input, "BaselineWriteReceiptDrift"))
        {
            return [];
        }

        if (input.Options.Mode != ParityReferenceMode.WriteBaseline)
        {
            if (supplied.Length > 0)
            {
                throw new InvalidOperationException(
                    "Baseline/live reports cannot contain baseline-write receipts.");
            }

            return [];
        }

        var selected = declarations.Where(item => item.Authored && item.Selected).ToArray();
        var selectedIds = selected.Select(item => item.Fixture).ToHashSet(StringComparer.Ordinal);
        if (supplied.Any(item => !selectedIds.Contains(item.Fixture)))
        {
            throw new InvalidOperationException(
                "Write-baseline receipts cannot belong to an unselected fixture.");
        }

        var receipts = new List<BaselineWriteReceipt>(selected.Length);
        foreach (var fixture in selected)
        {
            var rows = input.Executions.Where(item =>
                string.Equals(item.Fixture, fixture.Fixture, StringComparison.Ordinal)).ToArray();
            if (rows.Length == 0 || rows.Any(item => item.BaselineWrite is null))
            {
                throw new InvalidOperationException(
                    $"Write-baseline report requires a receipt on every '{fixture.Fixture}' result.");
            }

            var distinct = rows.Select(item => item.BaselineWrite!).Distinct().ToArray();
            if (distinct.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Write-baseline report has inconsistent receipts for '{fixture.Fixture}'.");
            }

            var receipt = distinct[0];
            if (!string.Equals(receipt.Fixture, fixture.Fixture, StringComparison.Ordinal) ||
                receipt.GeneratedAtUtc == default ||
                receipt.GeneratedAtUtc.Offset != TimeSpan.Zero ||
                !IsUpperHex(receipt.CaptureSha256, 64))
            {
                throw new InvalidOperationException(
                    $"Write-baseline receipt for '{fixture.Fixture}' is invalid.");
            }

            receipts.Add(receipt);
        }

        return receipts;
    }

    private static bool IsUpperHex(string value, int length)
        => value.Length == length && value.All(character =>
            character is >= '0' and <= '9' or >= 'A' and <= 'F');

    private static ReportCounts BuildCounts(
        IReadOnlyList<ReportFixtureDeclaration> declarations,
        IReadOnlyList<ReportFindingEvidence> findings,
        int executedFixtures,
        int requiredLegs,
        int completedLegs)
    {
        static IReadOnlyDictionary<string, int> Group(
            IEnumerable<string> values) => values.GroupBy(value => value, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        return new ReportCounts
        {
            AuthoredFixtureCount = declarations.Count(item => item.Authored),
            AuthoredThemeExecutionCount = declarations.Where(item => item.Authored)
                .Sum(item => item.Themes.Count),
            SelectedThemeExecutionCount = declarations.Where(item => item.Selected)
                .Sum(item => item.Themes.Count),
            MissingCatalogFixtureCount = declarations.Count(item => !item.Authored),
            ExecutedFixtureCount = executedFixtures,
            RequiredCandidateLegCount = requiredLegs,
            CompletedCandidateLegCount = completedLegs,
            FindingCount = findings.Count,
            BlockingFindingCount = findings.Count(item => item.Blocking),
            ByComponent = Group(findings.Select(item =>
                item.Identity.Fixture[..item.Identity.Fixture.IndexOf('/')])),
            ByKind = Group(findings.Select(item => item.Identity.Kind.ToString())),
            BySeverity = Group(findings.Select(item => item.Effective.Severity.ToString())),
            ByLeg = Group(findings.Select(item => item.Identity.Leg.ToString())),
            ByFixture = Group(findings.Select(item => item.Identity.Fixture)),
            ByDisposition = Group(findings.Select(item => item.Disposition.ToString()))
        };
    }

    private static bool MatchesFilter(string fixture, string? filter)
        => ParityOptions.MatchesFixture(fixture, filter);

    private static bool ExactIdentitySet(
        IReadOnlyList<FindingIdentity> left,
        IReadOnlyList<FindingIdentity> right)
        => left.Count == right.Count &&
           left.Distinct().Count() == left.Count &&
           right.Distinct().Count() == right.Count &&
           left.ToHashSet().SetEquals(right);

    private static int ThemeOrdinal(FixtureEntry? fixture, string theme)
    {
        if (fixture is null)
        {
            return int.MaxValue;
        }

        for (var index = 0; index < fixture.Themes.Count; index++)
        {
            if (string.Equals(fixture.Themes[index], theme, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return int.MaxValue;
    }

    private static int StepOrdinal(FixtureEntry? fixture, string step)
    {
        if (fixture is null)
        {
            return int.MaxValue;
        }

        for (var index = 0; index < fixture.Steps.Count; index++)
        {
            if (string.Equals(fixture.Steps[index].Name, step, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return int.MaxValue;
    }

    private static int KindOrdinal(FindingKind kind)
    {
        if (kind is FindingKind.ActionCompletionUnmet or FindingKind.FixtureError)
        {
            return 0;
        }

        if (kind == FindingKind.Structure)
        {
            return 1;
        }

        if (kind == FindingKind.CorrespondenceUncertain)
        {
            return 2;
        }

        var kinds = new ComparatorRegistry().OrderedKinds;
        for (var index = 0; index < kinds.Count; index++)
        {
            if (kinds[index] == kind)
            {
                return index + 3;
            }
        }

        return int.MaxValue;
    }

    private static int TimelineOrdinal(Finding finding)
        => finding.Kind == FindingKind.Timeline &&
           string.IsNullOrEmpty(finding.NodePath) && string.IsNullOrEmpty(finding.Property)
            ? 0
            : 1;

    private static string RawFixture(string executionId)
    {
        var separator = executionId.LastIndexOf('@');
        return separator < 0 ? executionId : executionId[..separator];
    }

    private static string Theme(string executionId)
    {
        var separator = executionId.LastIndexOf('@');
        return separator < 0 ? string.Empty : executionId[(separator + 1)..];
    }

    private static IReadOnlyList<ReportArtifactGroup> GroupArtifacts(
        IReadOnlyList<ReportArtifactSource> sources,
        IReadOnlyDictionary<string, FixtureEntry> fixtureById,
        IReadOnlyDictionary<string, int> catalogOrder)
    {
        foreach (var source in sources)
        {
            var artifact = source.Artifact;
            if (!FixtureExecution.IsExecutionId(artifact.ExecutionId) ||
                !string.Equals(
                    artifact.ExecutionId,
                    $"{artifact.Fixture}@{artifact.Theme}",
                    StringComparison.Ordinal) ||
                artifact.CandidateLeg is not
                    (ParityLeg.BlazorServer or ParityLeg.BlazorWasm) ||
                artifact.Role switch
                {
                    "React" => artifact.Leg != ParityLeg.React,
                    "Candidate" or "Diff" => artifact.Leg != artifact.CandidateLeg,
                    _ => true
                })
            {
                throw new InvalidOperationException(
                    $"Report artifact '{artifact.RelativePath}' has inconsistent " +
                    "fixture, theme, execution, role, or leg metadata.");
            }
        }

        return sources.Select(item => item.Artifact)
            .GroupBy(item => new
            {
                item.Fixture,
                item.Theme,
                item.ExecutionId,
                item.CandidateLeg,
                item.Step,
                item.Shot
            })
            .OrderBy(group => catalogOrder.GetValueOrDefault(group.Key.Fixture, int.MaxValue))
            .ThenBy(group => ThemeOrdinal(
                fixtureById.GetValueOrDefault(group.Key.Fixture), group.Key.Theme))
            .ThenBy(group => group.Key.CandidateLeg == ParityLeg.BlazorServer ? 0 : 1)
            .ThenBy(group => StepOrdinal(
                fixtureById.GetValueOrDefault(group.Key.Fixture), group.Key.Step))
            .ThenBy(group => group.Key.Shot, StringComparer.Ordinal)
            .Select(group =>
            {
                ReportArtifact? One(string role)
                {
                    var matches = group.Where(item =>
                        string.Equals(item.Role, role, StringComparison.Ordinal)).ToArray();
                    if (matches.Length > 1)
                    {
                        throw new InvalidOperationException(
                            $"Report artifact group '{group.Key.ExecutionId}' has duplicate " +
                            $"'{role}' evidence for step '{group.Key.Step}', shot '{group.Key.Shot}'.");
                    }

                    return matches.SingleOrDefault();
                }

                var unsupported = group.FirstOrDefault(item => item.Role is not
                    ("React" or "Candidate" or "Diff"));
                if (unsupported is not null)
                {
                    throw new InvalidOperationException(
                        $"Report artifact role '{unsupported.Role}' is unsupported.");
                }

                return new ReportArtifactGroup
                {
                    Fixture = group.Key.Fixture,
                    Theme = group.Key.Theme,
                    ExecutionId = group.Key.ExecutionId,
                    CandidateLeg = group.Key.CandidateLeg,
                    Step = group.Key.Step,
                    Shot = group.Key.Shot,
                    React = One("React"),
                    Candidate = One("Candidate"),
                    Diff = One("Diff")
                };
            })
            .ToArray();
    }
}
