using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using Blazix.BaseUI.Parity.Tests.Baselines;
using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Diff;
using Blazix.BaseUI.Parity.Tests.Report;
using Blazix.BaseUI.Parity.Tests.Waivers;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Parity.Tests.Infrastructure;

/// <summary>The immediate verdict owned by one public theory row.</summary>
public sealed record ParityCaseVerdict
{
    public required string Fixture { get; init; }

    public required string Theme { get; init; }

    public required ParityLeg Leg { get; init; }

    public required IReadOnlyList<Finding> Findings { get; init; }

    public required bool Blocking { get; init; }

    public required string Message { get; init; }
}

/// <summary>The globally classified evidence delivered exactly once at assembly completion.</summary>
public sealed record ParityRunFinalization
{
    public required DateTimeOffset GeneratedAtUtc { get; init; }

    public required ParityOptions Options { get; init; }

    public required IReadOnlyList<FixtureEntry> Fixtures { get; init; }

    public required IReadOnlyList<ParityRunBatch> FirstBatches { get; init; }

    public required IReadOnlyList<ParityRunBatch> RetryBatches { get; init; }

    public required IReadOnlyList<ParityRunResult> FirstResults { get; init; }

    public required RetryVerdict RetryVerdict { get; init; }

    public required WaiverVerdict WaiverVerdict { get; init; }

    public IReadOnlyList<ReportDiagnostic> PolicyDiagnostics { get; init; } = [];
}

/// <summary>
/// Thread-safe assembly fixture that memoizes two whole-fixture attempts and emits one report.
/// </summary>
public sealed class ParityRunAccumulator : IAsyncLifetime
{
    private readonly IReadOnlyList<FixtureEntry> manifest;
    private readonly IReadOnlyDictionary<string, FixtureEntry> fixtures;
    private readonly ParityOptions options;
    private readonly IReadOnlyList<WaiverMatcher.IndexedWaiver> waiverRegistry;
    private readonly Func<IBrowser, FixtureEntry, ParityOptions, int, string, Task<ParityRunBatch>> run;
    private readonly Func<ParityRunFinalization, ValueTask> finalize;
    private readonly string workRoot;
    private readonly TimeProvider timeProvider;
    private readonly Func<IBrowser, BaselinePlatform> platform;
    private readonly IReadOnlyList<ReportDiagnostic> initialDiagnostics;
    private readonly ConcurrentDictionary<string, Lazy<Task<FixtureEvaluation>>> evaluations =
        new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<CaseKey, Finding> missing = new();
    private int finalized;

    /// <summary>Creates the dormant production assembly fixture.</summary>
    public ParityRunAccumulator()
        : this(LoadProductionConfiguration())
    {
    }

    private ParityRunAccumulator(ProductionConfiguration configuration)
        : this(
            configuration.Manifest,
            configuration.Options,
            configuration.Waivers,
            RunProductionAsync,
            FinalizeProductionAsync,
            AttemptWorkRoot(),
            TimeProvider.System,
            BaselinePlatform.Current,
            configuration.Diagnostics)
    {
    }

    internal ParityRunAccumulator(
        IReadOnlyList<FixtureEntry> manifest,
        ParityOptions options,
        IReadOnlyList<Waiver> waivers,
        Func<IBrowser, FixtureEntry, ParityOptions, int, string, Task<ParityRunBatch>> run,
        Func<ParityRunFinalization, ValueTask> finalize,
        string workRoot,
        TimeProvider? timeProvider = null,
        Func<IBrowser, BaselinePlatform>? platform = null,
        IReadOnlyList<ReportDiagnostic>? policyDiagnostics = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(waivers);
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(finalize);
        ArgumentException.ThrowIfNullOrWhiteSpace(workRoot);

        this.manifest = manifest;
        fixtures = manifest.ToDictionary(item => item.Id, StringComparer.Ordinal);
        this.options = options;
        waiverRegistry = waivers.Select((waiver, index) =>
            new WaiverMatcher.IndexedWaiver(index, waiver)).ToArray();
        this.run = run;
        this.finalize = finalize;
        this.workRoot = workRoot;
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.platform = platform ?? BaselinePlatform.Current;
        initialDiagnostics = policyDiagnostics ?? [];
    }

    /// <inheritdoc />
    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    /// <summary>Returns the already globally ordered policy verdict for one public case.</summary>
    public async Task<ParityCaseVerdict> EvaluateAsync(
        IBrowser browser,
        string fixtureId,
        string theme,
        ParityLeg leg)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fixtureId);
        ArgumentException.ThrowIfNullOrWhiteSpace(theme);
        if (leg is not (ParityLeg.BlazorServer or ParityLeg.BlazorWasm))
        {
            throw new ArgumentOutOfRangeException(nameof(leg));
        }

        if (!MatchesFilter(fixtureId, options.FixtureFilter))
        {
            return Verdict(fixtureId, theme, leg, [], blocking: false, "Diagnostic filter excluded this case.");
        }

        if (initialDiagnostics.Any(item => item.Blocking))
        {
            return Verdict(
                fixtureId,
                theme,
                leg,
                [],
                blocking: true,
                string.Join(Environment.NewLine, initialDiagnostics.Where(item => item.Blocking)
                    .Select(item => $"{item.Kind}: {item.Message}")));
        }

        if (!MilestoneFixtureCatalog.Ids.Contains(fixtureId, StringComparer.Ordinal))
        {
            throw new ArgumentException($"Fixture '{fixtureId}' is outside the milestone catalog.", nameof(fixtureId));
        }

        if (!fixtures.TryGetValue(fixtureId, out var fixture))
        {
            var finding = missing.GetOrAdd(
                new CaseKey(fixtureId, theme, leg),
                key => FixtureError(key, "missing-fixture", $"Milestone fixture '{key.Fixture}' is not authored."));
            return Verdict(fixtureId, theme, leg, [finding], blocking: true, finding.Message);
        }

        if (!fixture.Themes.Contains(theme, StringComparer.Ordinal))
        {
            var finding = missing.GetOrAdd(
                new CaseKey(fixtureId, theme, leg),
                key => FixtureError(key, "missing-theme", $"Theme '{key.Theme}' is not authored for '{key.Fixture}'."));
            return Verdict(fixtureId, theme, leg, [finding], blocking: true, finding.Message);
        }

        var lazy = evaluations.GetOrAdd(fixtureId, _ => new Lazy<Task<FixtureEvaluation>>(
            () => EvaluateFixtureAsync(browser, fixture),
            LazyThreadSafetyMode.ExecutionAndPublication));
        var evaluation = await lazy.Value;
        var executionId = $"{fixtureId}@{theme}";
        var findings = evaluation.Retry.Evidence
            .Where(item =>
                string.Equals(item.Identity.Fixture, executionId, StringComparison.Ordinal) &&
                (item.Identity.Leg is ParityLeg.React || item.Identity.Leg == leg))
            .Select(item => item.Effective)
            .ToArray();
        var blocking = evaluation.Retry.Failures.Count > 0 ||
                       evaluation.Waivers.BlockingFindings.Any(item =>
                           string.Equals(item.Fixture, executionId, StringComparison.Ordinal) &&
                           (item.Leg is ParityLeg.React || item.Leg == leg)) ||
                       initialDiagnostics.Any(item => item.Blocking);
        return Verdict(
            fixtureId,
            theme,
            leg,
            findings,
            blocking,
            FailureMessage(executionId, leg, findings, evaluation, initialDiagnostics));
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref finalized, 1) != 0 ||
            evaluations.IsEmpty && missing.IsEmpty && initialDiagnostics.Count == 0)
        {
            return;
        }

        var orderedEvaluations = new List<FixtureEvaluation>();
        foreach (var fixtureId in MilestoneFixtureCatalog.Ids)
        {
            if (evaluations.TryGetValue(fixtureId, out var lazy))
            {
                orderedEvaluations.Add(await lazy.Value);
            }
        }

        var missingFindings = missing.Values
            .OrderBy(item => CatalogOrdinal(RawFixture(item.Fixture)))
            .ThenBy(item => item.Fixture, StringComparer.Ordinal)
            .ThenBy(item => item.Leg == ParityLeg.BlazorServer ? 0 : 1)
            .ToArray();
        var missingScopes = missingFindings.Select(RetryScope.From).ToArray();
        var first = Attempt(
            orderedEvaluations.SelectMany(item => item.FirstResults), missingFindings,
            orderedEvaluations.SelectMany(item => item.FirstScopes).Concat(missingScopes));
        var retry = Attempt(
            orderedEvaluations.SelectMany(item => item.RetryResults), missingFindings,
            orderedEvaluations.SelectMany(item => item.RetryScopes).Concat(missingScopes));
        var retryVerdict = RetryClassifier.Classify(first, retry);
        var activeWaivers = options.FixtureFilter is null
            ? waiverRegistry
            : waiverRegistry.Where(item =>
                MatchesFilter(RawFixture(item.Waiver.Fixture), options.FixtureFilter)).ToArray();
        var waiverVerdict = WaiverMatcher.MatchIndexed(
            retryVerdict.Findings,
            activeWaivers,
            DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime));

        await finalize(new ParityRunFinalization
        {
            GeneratedAtUtc = timeProvider.GetUtcNow(),
            Options = options,
            Fixtures = manifest,
            FirstBatches = orderedEvaluations.Select(item => item.First).ToArray(),
            RetryBatches = orderedEvaluations.Select(item => item.RetryBatch).ToArray(),
            FirstResults = orderedEvaluations.SelectMany(item => item.FirstResults).ToArray(),
            RetryVerdict = retryVerdict,
            WaiverVerdict = waiverVerdict,
            PolicyDiagnostics = initialDiagnostics
        });
    }

    private async Task<FixtureEvaluation> EvaluateFixtureAsync(IBrowser browser, FixtureEntry fixture)
    {
        var first = await RunAttemptAsync(browser, fixture, 1);
        var retryBatch = await RunAttemptAsync(browser, fixture, 2);
        if (retryBatch.Platform != first.Platform)
        {
            retryBatch = InvalidBatchContract(
                fixture,
                retryBatch,
                first.Platform,
                "cross-attempt-platform",
                "Attempts 1 and 2 returned different browser platform identities.");
        }

        var firstResults = Normalize(fixture, first.Results, 1);
        var retryResults = Normalize(fixture, retryBatch.Results, 2);
        var firstScopes = Scopes(fixture, firstResults);
        var retryScopes = Scopes(fixture, retryResults);
        var retry = RetryClassifier.Classify(
            Attempt(firstResults, [], firstScopes),
            Attempt(retryResults, [], retryScopes));
        var fixtureWaivers = waiverRegistry.Where(item =>
            string.Equals(RawFixture(item.Waiver.Fixture), fixture.Id, StringComparison.Ordinal)).ToArray();
        var waiverVerdict = WaiverMatcher.MatchIndexed(
            retry.Findings,
            fixtureWaivers,
            DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime));
        return new FixtureEvaluation(
            first,
            retryBatch,
            firstResults,
            retryResults,
            firstScopes,
            retryScopes,
            retry,
            waiverVerdict);
    }

    private async Task<ParityRunBatch> RunAttemptAsync(
        IBrowser browser,
        FixtureEntry fixture,
        int attempt)
    {
        var root = AttemptRoot(fixture.Id, attempt);
        var started = timeProvider.GetTimestamp();
        try
        {
            var batch = await run(browser, fixture, options, attempt, root);
            if (batch.Attempt != attempt ||
                batch.Fixture != fixture ||
                batch.Options != options ||
                batch.Platform is null ||
                batch.Results is null ||
                batch.Results.Any(item => item is null) ||
                batch.Duration < TimeSpan.Zero ||
                !string.Equals(
                    Path.GetFullPath(batch.ArtifactRoot),
                    Path.GetFullPath(root),
                    StringComparison.Ordinal))
            {
                return FailedAttemptBatch(
                    browser,
                    fixture,
                    attempt,
                    root,
                    started,
                    "invalid-batch-envelope",
                    "The runner returned an attempt, fixture, options, artifact root, or duration outside its request envelope.");
            }

            return batch;
        }
        catch (Exception exception)
        {
            var message = $"{exception.GetType().Name}: {SafeDetail(exception.Message)}";
            return FailedAttemptBatch(
                browser,
                fixture,
                attempt,
                root,
                started,
                "attempt-exception",
                $"Attempt {attempt} failed: {message}");
        }
    }

    private ParityRunBatch FailedAttemptBatch(
        IBrowser browser,
        FixtureEntry fixture,
        int attempt,
        string root,
        long started,
        string property,
        string message)
    {
        var results = FixtureExecution.Expand(fixture)
                .SelectMany(execution => CandidateLegs().Select(leg =>
                {
                    var key = new CaseKey(fixture.Id, execution.Theme, leg);
                    return ResultWithError(
                        key,
                        FixtureError(key, property, message));
                }))
                .ToArray();
        return new ParityRunBatch
        {
            Attempt = attempt,
            Fixture = fixture,
            Options = options,
            ArtifactRoot = root,
            Platform = platform(browser),
            Duration = timeProvider.GetElapsedTime(started),
            Results = results
        };
    }

    private static ParityRunBatch InvalidBatchContract(
        FixtureEntry fixture,
        ParityRunBatch batch,
        BaselinePlatform platform,
        string property,
        string message)
        => batch with
        {
            Platform = platform,
            Results = FixtureExecution.Expand(fixture)
                .SelectMany(execution => CandidateLegs().Select(leg =>
                {
                    var key = new CaseKey(fixture.Id, execution.Theme, leg);
                    return ResultWithError(key, FixtureError(key, property, message));
                }))
                .ToArray(),
            Baseline = null,
            LiveProvenance = [],
            LiveDistFingerprint = null
        };

    private static string SafeDetail(string detail)
    {
        var oneLine = detail.Replace('\r', ' ').Replace('\n', ' ')
            .Replace('\\', '∖').Replace('/', '∕')
            .Replace("file:", "file∶", StringComparison.OrdinalIgnoreCase);
        return oneLine.Length <= 500 ? oneLine : oneLine[..500];
    }

    private static IReadOnlyList<ParityRunResult> Normalize(
        FixtureEntry fixture,
        IReadOnlyList<ParityRunResult> results,
        int attempt)
    {
        var normalized = new List<ParityRunResult>();
        var expected = FixtureExecution.Expand(fixture)
            .SelectMany(execution => CandidateLegs().Select(leg =>
                new CaseKey(fixture.Id, execution.Theme, leg)))
            .ToArray();

        foreach (var key in expected)
        {
            var matches = results.Where(item =>
                string.Equals(item.Fixture, key.Fixture, StringComparison.Ordinal) &&
                string.Equals(item.Theme, key.Theme, StringComparison.Ordinal) &&
                item.Leg == key.Leg).ToArray();
            if (matches.Length == 0)
            {
                normalized.Add(ResultWithError(
                    key,
                    FixtureError(key, "missing-result", $"Attempt {attempt} is missing {key.Leg} evidence.")));
                continue;
            }

            var result = matches[0];
            if (!ValidResultIdentity(result, key))
            {
                normalized.Add(ResultWithError(
                    key,
                    FixtureError(
                        key,
                        "invalid-result-identity",
                        $"Attempt {attempt} returned result or capture ownership outside '{key.Fixture}@{key.Theme}/{key.Leg}'.")));
                continue;
            }

            if (matches.Length > 1)
            {
                result = result with
                {
                    Findings = result.Findings.Concat(
                    [
                        FixtureError(key, "duplicate-result", $"Attempt {attempt} duplicated {key.Leg} evidence.")
                    ]).ToArray()
                };
            }

            normalized.Add(result);
        }

        var unexpected = results.Where(result => !expected.Contains(
                new CaseKey(result.Fixture, result.Theme, result.Leg)))
            .ToArray();
        if (unexpected.Length > 0)
        {
            var owner = expected[0];
            var index = normalized.FindIndex(item =>
                string.Equals(item.Fixture, owner.Fixture, StringComparison.Ordinal) &&
                string.Equals(item.Theme, owner.Theme, StringComparison.Ordinal) &&
                item.Leg == owner.Leg);
            var description = string.Join(", ", unexpected.Select(item =>
                $"{item.ExecutionId}/{item.Leg}"));
            normalized[index] = normalized[index] with
            {
                Findings = normalized[index].Findings.Concat(
                [
                    FixtureError(
                        owner,
                        "unexpected-result",
                        $"Attempt {attempt} returned unexpected evidence: {SafeDetail(description)}.")
                ]).ToArray()
            };
        }

        return normalized;
    }

    private static bool ValidResultIdentity(ParityRunResult result, CaseKey key)
        => string.Equals(result.ExecutionId, $"{key.Fixture}@{key.Theme}", StringComparison.Ordinal) &&
           (result.Reference is null ||
            string.Equals(result.Reference.Fixture, key.Fixture, StringComparison.Ordinal) &&
            string.Equals(result.Reference.Theme, key.Theme, StringComparison.Ordinal) &&
            result.Reference.Leg == ParityLeg.React) &&
           (result.Candidate is null ||
            string.Equals(result.Candidate.Fixture, key.Fixture, StringComparison.Ordinal) &&
            string.Equals(result.Candidate.Theme, key.Theme, StringComparison.Ordinal) &&
            result.Candidate.Leg == key.Leg);

    private static RetryAttempt Attempt(
        IEnumerable<ParityRunResult> results,
        IEnumerable<Finding> additional,
        IEnumerable<RetryScope> scopes)
    {
        var rows = results.ToArray();
        var findings = CanonicalFindings(rows, additional);
        var executionFailures = findings.Where(item => item.Kind == FindingKind.FixtureError).ToArray();
        return new RetryAttempt
        {
            Scopes = scopes.Distinct().ToArray(),
            Findings = findings,
            State = executionFailures.Length > 0
                ? RetryAttemptState.ExecutionFailure
                : RetryAttemptState.Complete,
            Failure = executionFailures.Length > 0
                ? string.Join(" | ", executionFailures.Select(item => item.Message))
                : null
        };
    }

    private static IReadOnlyList<RetryScope> Scopes(
        FixtureEntry fixture,
        IReadOnlyList<ParityRunResult> results)
        => FixtureExecution.Expand(fixture)
            .SelectMany(execution => ReferenceAndCandidateLegs().SelectMany(leg =>
                fixture.Steps.Select(step => new RetryScope(execution.ExecutionId, leg, step.Name))))
            .Concat(results.SelectMany(item => item.Findings)
                .Where(item => item.Kind == FindingKind.FixtureError)
                .Select(RetryScope.From))
            .Distinct()
            .ToArray();

    private static IReadOnlyList<Finding> CanonicalFindings(
        IReadOnlyList<ParityRunResult> rows,
        IEnumerable<Finding> additional)
    {
        var findings = new List<Finding>();
        var react = new Dictionary<FindingIdentity, Finding>();
        foreach (var finding in rows.SelectMany(item => item.Findings))
        {
            if (finding.Leg != ParityLeg.React)
            {
                findings.Add(finding);
                continue;
            }

            var identity = FindingIdentity.From(finding);
            if (!react.TryGetValue(identity, out var existing))
            {
                react.Add(identity, finding);
                findings.Add(finding);
                continue;
            }

            if (existing == finding)
            {
                continue;
            }

            findings.Add(new Finding
            {
                Fixture = finding.Fixture,
                Leg = ParityLeg.React,
                Step = finding.Step,
                Kind = FindingKind.FixtureError,
                Severity = Severity.Error,
                NodePath = finding.NodePath,
                Property = "divergent-react-evidence",
                ReferenceValue = existing.Message,
                CandidateValue = finding.Message,
                Message = "Candidate results disagree about their shared React evidence."
            });
        }

        findings.AddRange(additional);
        return findings;
    }

    private static IReadOnlyList<ParityLeg> CandidateLegs()
        => [ParityLeg.BlazorServer, ParityLeg.BlazorWasm];

    private static IReadOnlyList<ParityLeg> ReferenceAndCandidateLegs()
        => [ParityLeg.React, ParityLeg.BlazorServer, ParityLeg.BlazorWasm];

    private static Finding FixtureError(CaseKey key, string property, string message) => new()
    {
        Fixture = $"{key.Fixture}@{key.Theme}",
        Leg = key.Leg,
        Step = "initial",
        Kind = FindingKind.FixtureError,
        Severity = Severity.Error,
        Property = property,
        Message = message
    };

    private static ParityRunResult ResultWithError(CaseKey key, Finding finding) => new()
    {
        Fixture = key.Fixture,
        Theme = key.Theme,
        ExecutionId = $"{key.Fixture}@{key.Theme}",
        Leg = key.Leg,
        Findings = [finding]
    };

    private static ParityCaseVerdict Verdict(
        string fixture,
        string theme,
        ParityLeg leg,
        IReadOnlyList<Finding> findings,
        bool blocking,
        string message) => new()
        {
            Fixture = fixture,
            Theme = theme,
            Leg = leg,
            Findings = findings,
            Blocking = blocking,
            Message = $"{fixture}@{theme} {leg}: {message}"
        };

    private static string FailureMessage(
        string executionId,
        ParityLeg leg,
        IReadOnlyList<Finding> findings,
        FixtureEvaluation evaluation,
        IReadOnlyList<ReportDiagnostic> policyDiagnostics)
    {
        var lines = findings.Select(item =>
            $"{item.Kind} {item.Step} {item.NodePath} {item.Property}: " +
            $"{item.ReferenceValue} -> {item.CandidateValue} ({item.Message})").ToList();
        lines.AddRange(evaluation.Retry.Failures.Select(item =>
            $"Attempt {item.Attempt} {item.State}: {item.Message}"));
        lines.AddRange(policyDiagnostics.Where(item => item.Blocking)
            .Select(item => $"{item.Source} {item.Kind}: {item.Message}"));
        return lines.Count == 0
            ? $"{executionId} {leg} has no blocking evidence."
            : string.Join(Environment.NewLine, lines);
    }

    private string AttemptRoot(string fixtureId, int attempt)
        => Path.Combine(workRoot, ScreenshotSet.Slug(fixtureId), $"attempt-{attempt}");

    private static bool MatchesFilter(string fixture, string? filter)
        => ParityOptions.MatchesFixture(fixture, filter);

    private static string RawFixture(string executionId)
    {
        var separator = executionId.LastIndexOf('@');
        return separator < 0 ? executionId : executionId[..separator];
    }

    private static int CatalogOrdinal(string fixture)
    {
        for (var index = 0; index < MilestoneFixtureCatalog.Ids.Count; index++)
        {
            if (string.Equals(MilestoneFixtureCatalog.Ids[index], fixture, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return int.MaxValue;
    }

    private static async Task<ParityRunBatch> RunProductionAsync(
        IBrowser browser,
        FixtureEntry fixture,
        ParityOptions options,
        int attempt,
        string root)
    {
        return await new ParityRunner(root).RunBatchAsync(browser, fixture, options, attempt);
    }

    private static ValueTask FinalizeProductionAsync(ParityRunFinalization finalization)
        => FinalizeProductionAsync(
            finalization,
            model =>
            {
                var canonical = ParityPaths.ReportDir;
                var diagnosticsRoot = canonical + "-diagnostics";
                var diagnosticId = finalization.Options.FixtureFilter is null
                    ? null
                    : DiagnosticId(finalization.Options.FixtureFilter);
                _ = new ReportPackageWriter().Write(model, canonical, diagnosticsRoot, diagnosticId);
                return ValueTask.CompletedTask;
            });

    internal static async ValueTask FinalizeProductionAsync(
        ParityRunFinalization finalization,
        Func<ReportModel, ValueTask> write)
    {
        ArgumentNullException.ThrowIfNull(write);
        var model = BuildReportModel(finalization);
        await write(model);
        if (model.Findings.Any(item => item.Blocking) ||
            model.Diagnostics.Any(item => item.Blocking) ||
            model.Counts.CompletedCandidateLegCount < model.Counts.RequiredCandidateLegCount)
        {
            throw new InvalidOperationException(
                "Parity report was published with selected-scope blocking or incomplete evidence.");
        }
    }

    internal static ReportModel BuildReportModel(ParityRunFinalization finalization)
        => BuildReportModel(finalization, () => new BaselineStore().DescribeAuthority());

    internal static ReportModel BuildReportModel(
        ParityRunFinalization finalization,
        Func<BaselineAuthoritySnapshot> authorityProvider)
    {
        var diagnostics = new List<ReportDiagnostic>(finalization.PolicyDiagnostics);
        var selected = finalization.Fixtures
            .Where(item => MatchesFilter(item.Id, finalization.Options.FixtureFilter))
            .ToArray();
        var selectedIds = selected.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        var firstBatches = finalization.FirstBatches
            .Where(item => selectedIds.Contains(item.Fixture.Id)).ToArray();
        var retryBatches = finalization.RetryBatches
            .Where(item => selectedIds.Contains(item.Fixture.Id)).ToArray();
        var allBatches = firstBatches.Concat(retryBatches).ToArray();
        var coverageComplete = ExactBatchCoverage(firstBatches, selectedIds, attempt: 1) &&
                               ExactBatchCoverage(retryBatches, selectedIds, attempt: 2);
        if (!coverageComplete && selected.Length > 0)
        {
            diagnostics.Add(ProvenanceDiagnostic(
                "AttemptProvenanceUnavailable",
                "Selected fixtures do not have exactly one provenance batch for both attempts."));
        }

        var authorityKeys = allBatches.Where(item => item.Authority is not null)
            .Select(item => AuthorityKey(item.Authority!))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var authorityComplete = selected.Length == 0 ||
                                coverageComplete &&
                                allBatches.All(item => item.Authority is not null) &&
                                authorityKeys.Length == 1;
        if (!authorityComplete)
        {
            diagnostics.Add(ProvenanceDiagnostic(
                authorityKeys.Length > 1 ? "AuthorityDrift" : "AuthorityUnavailable",
                "Every selected first and retry batch must carry one equal validated authority."));
        }

        var authority = authorityComplete
            ? allBatches.Select(item => item.Authority).FirstOrDefault(item => item is not null)
            : null;
        if (authority is null)
        {
            try
            {
                authority = authorityProvider();
            }
            catch (Exception exception)
            {
                diagnostics.Add(new ReportDiagnostic
                {
                    Source = ReportDiagnosticSource.Provenance,
                    Kind = "AuthorityUnavailable",
                    Message = $"Validated baseline authority is unavailable: {SafeDetail(exception.Message)}",
                    Blocking = true
                });
            }
        }

        BaselineSnapshot? baseline = null;
        IReadOnlyList<LiveFixtureProvenanceSnapshot> live = [];
        string? fingerprint = null;
        if (finalization.Options.Mode == ParityReferenceMode.Baseline)
        {
            var baselineKeys = allBatches.Where(item => item.Baseline is not null)
                .Select(item => BaselineKey(item.Baseline!))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var baselineComplete = selected.Length > 0 &&
                                   coverageComplete &&
                                   authorityComplete &&
                                   allBatches.All(item => item.Baseline is not null) &&
                                   baselineKeys.Length == 1;
            if (baselineComplete)
            {
                baseline = firstBatches[0].Baseline;
            }
            else
            {
                diagnostics.Add(ProvenanceDiagnostic(
                    baselineKeys.Length > 1 ? "BaselineSnapshotDrift" : "BaselineSnapshotUnavailable",
                    "Every selected first and retry batch must carry one equal validated baseline snapshot."));
                if (baselineKeys.Length > 1)
                {
                    diagnostics.Add(ProvenanceDiagnostic(
                        "BaselineSnapshotUnavailable",
                        "Conflicting baseline snapshots cannot establish observed provenance."));
                }
            }
        }
        else
        {
            var liveComplete = selected.Length == 0 ||
                               coverageComplete &&
                               authorityComplete &&
                               CompleteLiveProvenance(allBatches, selected);
            if (liveComplete && selected.Length > 0)
            {
                live = firstBatches.SelectMany(item => item.LiveProvenance).ToArray();
                fingerprint = firstBatches[0].LiveDistFingerprint;
            }
            else if (!liveComplete)
            {
                diagnostics.Add(ProvenanceDiagnostic(
                    "LiveProvenanceUnavailable",
                    "Every selected first and retry batch must carry equal live source, contract, and bundle provenance."));
            }
        }

        var artifacts = BuildArtifactMapping(firstBatches);
        diagnostics.AddRange(artifacts.Diagnostics);
        var receiptsMatch = RetryReceiptsMatch(finalization, firstBatches, retryBatches);
        if (!receiptsMatch)
        {
            diagnostics.Add(ProvenanceDiagnostic(
                "BaselineWriteReceiptDrift",
                "Write-baseline receipts must be identical across both attempts for every candidate result."));
        }
        var reportExecutions = receiptsMatch
            ? finalization.FirstResults
            : finalization.FirstResults.Select(item => item with { BaselineWrite = null }).ToArray();
        return ReportModel.Create(new ReportModelInput
        {
            GeneratedAtUtc = finalization.GeneratedAtUtc,
            Options = finalization.Options,
            Fixtures = finalization.Fixtures,
            Executions = reportExecutions,
            RetryVerdict = finalization.RetryVerdict,
            WaiverVerdict = finalization.WaiverVerdict,
            Baseline = baseline,
            Authority = authority,
            LiveProvenance = live,
            LiveDistFingerprint = fingerprint,
            PolicyDiagnostics = diagnostics,
            ArtifactSources = artifacts.Sources,
            Attempts = AttemptTimings(finalization)
        });
    }

    private static bool ExactBatchCoverage(
        IReadOnlyList<ParityRunBatch> batches,
        IReadOnlySet<string> selectedIds,
        int attempt)
        => batches.Count == selectedIds.Count &&
           batches.All(item => item.Attempt == attempt && selectedIds.Contains(item.Fixture.Id)) &&
           batches.Select(item => item.Fixture.Id).Distinct(StringComparer.Ordinal).Count() == selectedIds.Count;

    private static bool CompleteLiveProvenance(
        IReadOnlyList<ParityRunBatch> batches,
        IReadOnlyList<FixtureEntry> selected)
    {
        if (batches.Any(item => item.LiveDistFingerprint is null) ||
            batches.Select(item => item.LiveDistFingerprint).Distinct(StringComparer.Ordinal).Count() != 1)
        {
            return false;
        }

        foreach (var fixture in selected)
        {
            var fixtureBatches = batches.Where(item =>
                string.Equals(item.Fixture.Id, fixture.Id, StringComparison.Ordinal)).ToArray();
            if (fixtureBatches.Length != 2)
            {
                return false;
            }

            var expected = fixture.Themes.Select(theme => (fixture.Id, Theme: theme)).ToHashSet();
            if (fixtureBatches.Any(batch =>
                {
                    var supplied = batch.LiveProvenance
                        .Select(item => (item.Fixture, item.Theme)).ToHashSet();
                    return batch.LiveProvenance.Count != expected.Count ||
                           supplied.Count != expected.Count ||
                           !supplied.SetEquals(expected);
                }) ||
                fixtureBatches.Select(LiveKey).Distinct(StringComparer.Ordinal).Count() != 1)
            {
                return false;
            }
        }

        return true;
    }

    private static string AuthorityKey(BaselineAuthoritySnapshot authority)
        => JsonSerializer.Serialize(authority.Authority);

    private static string BaselineKey(BaselineSnapshot baseline)
        => AuthorityKey(new BaselineAuthoritySnapshot(baseline.Authority)) + "|" +
           JsonSerializer.Serialize(baseline.Set);

    private static string LiveKey(ParityRunBatch batch)
        => batch.LiveDistFingerprint + "|" + JsonSerializer.Serialize(
            batch.LiveProvenance.OrderBy(item => item.Fixture, StringComparer.Ordinal)
                .ThenBy(item => item.Theme, StringComparer.Ordinal));

    private static ReportDiagnostic ProvenanceDiagnostic(string kind, string message) => new()
    {
        Source = ReportDiagnosticSource.Provenance,
        Kind = kind,
        Message = message,
        Blocking = true
    };

    private static bool RetryReceiptsMatch(
        ParityRunFinalization finalization,
        IReadOnlyList<ParityRunBatch> firstBatches,
        IReadOnlyList<ParityRunBatch> retryBatches)
    {
        var firstRows = firstBatches.SelectMany(item => item.Results).ToArray();
        var retryRows = retryBatches.SelectMany(item => item.Results).ToArray();
        if (firstRows.GroupBy(item => (item.ExecutionId, item.Leg)).Any(group => group.Count() != 1) ||
            retryRows.GroupBy(item => (item.ExecutionId, item.Leg)).Any(group => group.Count() != 1))
        {
            return false;
        }

        var first = firstRows.ToDictionary(item => (item.ExecutionId, item.Leg));
        var retry = retryRows.ToDictionary(item => (item.ExecutionId, item.Leg));
        if (finalization.Options.Mode != ParityReferenceMode.WriteBaseline)
        {
            return first.Values.Concat(retry.Values).All(item => item.BaselineWrite is null);
        }

        var selectedFixtures = finalization.Fixtures.Where(item =>
            MatchesFilter(item.Id, finalization.Options.FixtureFilter)).ToArray();
        var expected = selectedFixtures.SelectMany(fixture =>
                FixtureExecution.Expand(fixture).SelectMany(execution => CandidateLegs().Select(leg =>
                    (execution.ExecutionId, leg))))
            .ToHashSet();
        return first.Count == expected.Count &&
               retry.Count == expected.Count &&
               first.Keys.ToHashSet().SetEquals(expected) &&
               retry.Keys.ToHashSet().SetEquals(expected) &&
               first.Values.All(item => item.BaselineWrite is not null) &&
               retry.Values.All(item => item.BaselineWrite is not null) &&
               first.All(item =>
            retry.TryGetValue(item.Key, out var retryResult) &&
            item.Value.BaselineWrite == retryResult.BaselineWrite);
    }

    internal static IReadOnlyList<ReportArtifactSource> BuildArtifactSources(
        IReadOnlyList<ParityRunBatch> batches)
        => BuildArtifactMapping(batches).Sources;

    internal static ReportArtifactMapping BuildArtifactMapping(
        IReadOnlyList<ParityRunBatch> batches,
        Func<string, byte[]>? readFile = null)
    {
        readFile ??= File.ReadAllBytes;
        var sources = new List<ReportArtifactSource>();
        var diagnostics = new List<ReportDiagnostic>();
        foreach (var batch in batches)
        {
            if (batch.Results is null)
            {
                diagnostics.Add(MalformedArtifactDiagnostic("The attempt result collection is null."));
                continue;
            }

            foreach (var result in batch.Results)
            {
                if (result is null)
                {
                    diagnostics.Add(MalformedArtifactDiagnostic("The attempt contains a null result row."));
                    continue;
                }

                if (result.Reference is null || result.Candidate is null)
                {
                    continue;
                }

                if (!TryValidateArtifactSteps(result, diagnostics, out var referenceSteps, out var candidateSteps))
                {
                    continue;
                }

                foreach (var reference in referenceSteps)
                {
                    if (!candidateSteps.TryGetValue(reference.Step, out var candidate))
                    {
                        continue;
                    }

                    AddArtifacts(
                        sources,
                        diagnostics,
                        batch.ArtifactRoot,
                        result,
                        reference,
                        candidate,
                        readFile);
                }
            }
        }

        return new ReportArtifactMapping(sources, diagnostics);
    }

    private static bool TryValidateArtifactSteps(
        ParityRunResult result,
        ICollection<ReportDiagnostic> diagnostics,
        out IReadOnlyList<StepCapture> referenceSteps,
        out IReadOnlyDictionary<string, StepCapture> candidateSteps)
    {
        referenceSteps = [];
        candidateSteps = new Dictionary<string, StepCapture>(StringComparer.Ordinal);
        try
        {
            var references = result.Reference!.Steps;
            var candidates = result.Candidate!.Steps;
            if (references is null || candidates is null)
            {
                throw new InvalidOperationException("Capture step collections cannot be null.");
            }

            if (references.Any(item => item is null) || candidates.Any(item => item is null) ||
                references.GroupBy(item => item.Step, StringComparer.Ordinal).Any(group => group.Count() > 1) ||
                candidates.GroupBy(item => item.Step, StringComparer.Ordinal).Any(group => group.Count() > 1) ||
                references.Any(item => string.IsNullOrWhiteSpace(item.Step)) ||
                candidates.Any(item => string.IsNullOrWhiteSpace(item.Step)))
            {
                throw new InvalidOperationException("Capture steps must be non-null and unique by nonblank step name.");
            }

            if (references.Any(item => item.ScreenshotObservations is null) ||
                candidates.Any(item => item.ScreenshotObservations is null))
            {
                throw new InvalidOperationException(
                    "Capture screenshot-observation collections cannot be null.");
            }

            referenceSteps = references;
            candidateSteps = candidates.ToDictionary(item => item.Step, StringComparer.Ordinal);
            if (!references.Select(item => item.Step).ToHashSet(StringComparer.Ordinal)
                    .SetEquals(candidateSteps.Keys))
            {
                throw new InvalidOperationException(
                    "React and candidate captures must expose the same unique step identities.");
            }

            return true;
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            diagnostics.Add(ArtifactDiagnostic(
                "ArtifactInvalid",
                result,
                "capture",
                "Capture",
                "steps",
                $"The capture step graph could not be traversed: {SafeDetail(exception.Message)}"));
            return false;
        }
    }

    private static IReadOnlyList<ReportAttemptTiming> AttemptTimings(
        ParityRunFinalization finalization)
        => finalization.FirstBatches.Select(item => new ReportAttemptTiming
            {
                Fixture = item.Fixture.Id,
                Attempt = item.Attempt,
                Platform = item.Platform,
                Duration = item.Duration
            })
            .Concat(finalization.RetryBatches.Select(item => new ReportAttemptTiming
            {
                Fixture = item.Fixture.Id,
                Attempt = item.Attempt,
                Platform = item.Platform,
                Duration = item.Duration
            }))
            .ToArray();

    private static void AddArtifacts(
        ICollection<ReportArtifactSource> sources,
        ICollection<ReportDiagnostic> diagnostics,
        string directory,
        ParityRunResult result,
        StepCapture reference,
        StepCapture candidate,
        Func<string, byte[]> readFile)
    {
        foreach (var name in reference.Screenshots)
        {
            AddArtifact(
                sources,
                diagnostics,
                directory,
                result,
                reference.Step,
                name,
                ParityLeg.React,
                "React",
                required: true,
                readFile);
        }

        foreach (var name in candidate.Screenshots)
        {
            AddArtifact(
                sources,
                diagnostics,
                directory,
                result,
                candidate.Step,
                name,
                result.Leg,
                "Candidate",
                required: true,
                readFile);
            var diff = ScreenshotSet.DiffName(name);
            if (File.Exists(Path.Combine(directory, diff)))
            {
                AddArtifact(
                    sources,
                    diagnostics,
                    directory,
                    result,
                    candidate.Step,
                    diff,
                    result.Leg,
                    "Diff",
                    required: false,
                    readFile);
            }
        }
    }

    private static void AddArtifact(
        ICollection<ReportArtifactSource> sources,
        ICollection<ReportDiagnostic> diagnostics,
        string directory,
        ParityRunResult result,
        string step,
        string name,
        ParityLeg leg,
        string role,
        bool required,
        Func<string, byte[]> readFile)
    {
        var path = Path.Combine(directory, name);
        if (!File.Exists(path))
        {
            if (required)
            {
                diagnostics.Add(ArtifactDiagnostic(
                    "ArtifactMissing",
                    result,
                    step,
                    role,
                    name,
                    "The capture declared a screenshot that is absent from attempt 1."));
            }

            return;
        }

        try
        {
            var shotName = role == "Diff"
                ? name.Replace(".diff.png", ".png", StringComparison.Ordinal)
                : name;
            var shot = ScreenshotSet.Shot(
                shotName,
                result.Fixture,
                result.Theme,
                role == "React" ? ParityLeg.React : result.Leg,
                step);
            if (string.Equals(shot, shotName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Screenshot name does not match its capture identity.");
            }

            var relativePath = $"assets/screenshots/{result.Leg}/{name}";
            ReportOutputSafety.ValidateRelativeArtifactPath(relativePath);
            sources.Add(new ReportArtifactSource
            {
                SourcePath = path,
                Artifact = new ReportArtifact
                {
                    RelativePath = relativePath,
                    Sha256 = Convert.ToHexString(SHA256.HashData(readFile(path))),
                    MediaType = "image/png",
                    Fixture = result.Fixture,
                    Theme = result.Theme,
                    ExecutionId = result.ExecutionId,
                    Leg = leg,
                    Step = step,
                    Shot = shot,
                    CandidateLeg = result.Leg,
                    Role = role
                }
            });
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            diagnostics.Add(ArtifactDiagnostic(
                "ArtifactInvalid",
                result,
                step,
                role,
                name,
                $"The declared screenshot could not be validated: {SafeDetail(exception.Message)}"));
        }
    }

    private static ReportDiagnostic ArtifactDiagnostic(
        string kind,
        ParityRunResult result,
        string step,
        string role,
        string name,
        string detail) => new()
        {
            Source = ReportDiagnosticSource.Artifact,
            Kind = kind,
            Message = $"{result.ExecutionId} {result.Leg} {step} {role} " +
                      $"'{SafeDetail(name)}': {detail}",
            Blocking = true
        };

    private static ReportDiagnostic MalformedArtifactDiagnostic(string message) => new()
    {
        Source = ReportDiagnosticSource.Artifact,
        Kind = "ArtifactInvalid",
        Message = message,
        Blocking = true
    };

    internal static string DiagnosticId(string filter)
    {
        var value = new string(filter.ToLowerInvariant().Select(character =>
            char.IsAsciiLetterOrDigit(character) ? character : '-').ToArray()).Trim('-');
        while (value.Contains("--", StringComparison.Ordinal))
        {
            value = value.Replace("--", "-", StringComparison.Ordinal);
        }

        var slug = string.IsNullOrEmpty(value) ? "filtered" : value;
        if (slug.Length > 40)
        {
            slug = slug[..40].TrimEnd('-');
        }

        var hash = Convert.ToHexString(SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(filter)))[..12].ToLowerInvariant();
        return $"{slug}-{hash}";
    }

    internal static WaiverLoadResult LoadWaivers(Func<IReadOnlyList<Waiver>> load)
    {
        ArgumentNullException.ThrowIfNull(load);
        try
        {
            return new WaiverLoadResult(load(), []);
        }
        catch (Exception exception) when (
            exception is FormatException or IOException or UnauthorizedAccessException)
        {
            return new WaiverLoadResult(
                [],
                [
                    new ReportDiagnostic
                    {
                        Source = ReportDiagnosticSource.WaiverLoader,
                        Kind = "WaiverFileInvalid",
                        Message = SafeDetail(exception.Message),
                        Blocking = true
                    }
                ]);
        }
    }

    private static ProductionConfiguration LoadProductionConfiguration()
    {
        var manifest = LoadManifest(FixtureManifest.Load);
        var options = LoadOptions(ParityOptions.FromEnvironment);
        var waiverLoad = LoadWaivers(WaiverFile.Load);
        return new ProductionConfiguration(
            manifest.Manifest,
            options.Options,
            waiverLoad.Waivers,
            manifest.Diagnostics.Concat(options.Diagnostics).Concat(waiverLoad.Diagnostics).ToArray());
    }

    internal static ManifestLoadResult LoadManifest(Func<IReadOnlyList<FixtureEntry>> load)
    {
        ArgumentNullException.ThrowIfNull(load);
        try
        {
            return new ManifestLoadResult(load(), []);
        }
        catch (Exception exception) when (
            exception is FormatException or IOException or UnauthorizedAccessException)
        {
            return new ManifestLoadResult([], [ConfigurationDiagnostic(
                "ManifestUnavailable", exception)]);
        }
    }

    internal static OptionsLoadResult LoadOptions(Func<ParityOptions> load)
    {
        ArgumentNullException.ThrowIfNull(load);
        try
        {
            return new OptionsLoadResult(load(), []);
        }
        catch (Exception exception) when (
            exception is FormatException or IOException or UnauthorizedAccessException)
        {
            return new OptionsLoadResult(
                new ParityOptions { Mode = ParityReferenceMode.Baseline },
                [ConfigurationDiagnostic("OptionsInvalid", exception)]);
        }
    }

    private static ReportDiagnostic ConfigurationDiagnostic(string kind, Exception exception) => new()
    {
        Source = ReportDiagnosticSource.Policy,
        Kind = kind,
        Message = SafeDetail(exception.Message),
        Blocking = true
    };

    private static string AttemptWorkRoot()
    {
        var report = Path.GetFullPath(ParityPaths.ReportDir);
        var parent = Path.GetDirectoryName(report);
        if (string.IsNullOrEmpty(parent))
        {
            throw new InvalidOperationException("Parity report root has no parent directory.");
        }

        return Path.Combine(parent, $".{Path.GetFileName(report)}-attempts");
    }

    private sealed record FixtureEvaluation(
        ParityRunBatch First,
        ParityRunBatch RetryBatch,
        IReadOnlyList<ParityRunResult> FirstResults,
        IReadOnlyList<ParityRunResult> RetryResults,
        IReadOnlyList<RetryScope> FirstScopes,
        IReadOnlyList<RetryScope> RetryScopes,
        RetryVerdict Retry,
        WaiverVerdict Waivers);

    private sealed record CaseKey(string Fixture, string Theme, ParityLeg Leg);

    internal sealed record WaiverLoadResult(
        IReadOnlyList<Waiver> Waivers,
        IReadOnlyList<ReportDiagnostic> Diagnostics);

    internal sealed record ManifestLoadResult(
        IReadOnlyList<FixtureEntry> Manifest,
        IReadOnlyList<ReportDiagnostic> Diagnostics);

    internal sealed record OptionsLoadResult(
        ParityOptions Options,
        IReadOnlyList<ReportDiagnostic> Diagnostics);

    internal sealed record ReportArtifactMapping(
        IReadOnlyList<ReportArtifactSource> Sources,
        IReadOnlyList<ReportDiagnostic> Diagnostics);

    private sealed record ProductionConfiguration(
        IReadOnlyList<FixtureEntry> Manifest,
        ParityOptions Options,
        IReadOnlyList<Waiver> Waivers,
        IReadOnlyList<ReportDiagnostic> Diagnostics);
}
