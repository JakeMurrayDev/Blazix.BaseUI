using System.Security.Cryptography;
using System.Diagnostics;
using Blazix.BaseUI.Parity.Tests.Baselines;
using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Diff;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Parity.Tests.Infrastructure;

/// <summary>The integrated evidence produced for one React-to-Blazor candidate comparison.</summary>
public sealed record ParityRunResult
{
    /// <summary>Gets the fixture id.</summary>
    public required string Fixture { get; init; }

    /// <summary>Gets the exact emulated theme.</summary>
    public required string Theme { get; init; }

    /// <summary>Gets the fixture-theme finding and waiver identity.</summary>
    public required string ExecutionId { get; init; }

    /// <summary>Gets the candidate leg this result evaluates.</summary>
    public required ParityLeg Leg { get; init; }

    /// <summary>Gets the live React capture, or <see langword="null"/> when it failed.</summary>
    public CaptureBundle? Reference { get; init; }

    /// <summary>Gets the live candidate capture, or <see langword="null"/> when it failed.</summary>
    public CaptureBundle? Candidate { get; init; }

    /// <summary>Gets the manifest-ordered paired step contexts passed to the registry.</summary>
    public IReadOnlyList<ComparisonContext> Contexts { get; init; } = [];

    /// <summary>Gets runner-owned blocking evidence followed by comparator findings.</summary>
    public IReadOnlyList<Finding> Findings { get; init; } = [];

    /// <summary>Gets positive evidence that write-baseline mode replaced this reference.</summary>
    public BaselineWriteReceipt? BaselineWrite { get; init; }

    /// <summary>Gets whether the result contains evidence that blocks a parity pass.</summary>
    public bool HasBlockingEvidence => Findings.Any(finding => finding.Severity == Severity.Error);
}

/// <summary>One complete whole-fixture attempt and its scoped evidence authority.</summary>
public sealed record ParityRunBatch
{
    public required int Attempt { get; init; }

    public required FixtureEntry Fixture { get; init; }

    public required ParityOptions Options { get; init; }

    public required string ArtifactRoot { get; init; }

    public required BaselinePlatform Platform { get; init; }

    public required TimeSpan Duration { get; init; }

    public required IReadOnlyList<ParityRunResult> Results { get; init; }

    public BaselineSnapshot? Baseline { get; init; }

    public BaselineAuthoritySnapshot? Authority { get; init; }

    public IReadOnlyList<LiveFixtureProvenanceSnapshot> LiveProvenance { get; init; } = [];

    public string? LiveDistFingerprint { get; init; }
}

/// <summary>Validates that the live React source bundle is the exact copy the host will serve.</summary>
internal static class LiveBundlePrecondition
{
    private const string BuildCommand = "pnpm parity:build";

    internal static void Validate(
        string sourceDirectory,
        string servedDirectory,
        FixtureEntry fixture,
        LiveBaselineProvenance provenance)
    {
        var source = Fingerprint(sourceDirectory, "source");
        var served = Fingerprint(servedDirectory, "served");

        if (!source.SequenceEqual(served, StringComparer.Ordinal))
        {
            throw Failure(
                $"The served React bundle at '{servedDirectory}' is stale relative to " +
                $"'{sourceDirectory}'. Rebuild the test project after building the bundle.");
        }

        try
        {
            ReactBundleProvenance.Validate(sourceDirectory, fixture, provenance);
        }
        catch (InvalidOperationException exception)
        {
            throw Failure(exception.Message);
        }
    }

    private static InvalidOperationException Failure(string reason)
        => new(
            $"Live React parity cannot start. {reason} Run `{BuildCommand}` from " +
            $"'{Path.Combine(ParityPaths.HarnessRoot, "react-fixtures")}', then rebuild the " +
            "parity test project so its served react-dist copy is refreshed.");

    private static IReadOnlyList<string> Fingerprint(string directory, string role)
    {
        if (!Directory.Exists(directory))
        {
            throw Failure($"The {role} React bundle directory '{directory}' is missing.");
        }

        try
        {
            var files = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            if (files.Length == 0 || !File.Exists(Path.Combine(directory, "index.html")))
            {
                throw Failure(
                    $"The {role} React bundle directory '{directory}' is unreadable or incomplete.");
            }

            return
            [
                .. files.Select(path =>
                    Path.GetRelativePath(directory, path).Replace(Path.DirectorySeparatorChar, '/') +
                    ":" + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))))
            ];
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw Failure(
                $"The {role} React bundle directory '{directory}' is not readable: " +
                exception.Message);
        }
    }
}

/// <summary>
/// Captures and compares the live React reference against Blazor Server and WebAssembly.
/// </summary>
public sealed class ParityRunner
{
    // Keep aligned with the bounded completion snapshot produced by shared/capture.js.
    private const int MaximumObservedLength = 500;

    private readonly ComparatorRegistry registry;
    private readonly string screenshotDirectory;
    private readonly string sourceReactDist;
    private readonly string servedReactDist;
    private readonly Func<IBrowser, Task<IBrowserContext>> createContext;
    private readonly Func<IBrowserContext, Task<IPage>> createPage;
    private readonly Func<IPage, Task> closePage;
    private readonly BaselineStore baselineStore;
    private readonly Func<FixtureEntry, LiveBaselineProvenance> readLiveProvenance;
    private readonly Action<string, string, FixtureEntry, LiveBaselineProvenance> validateLiveBundle;

    /// <summary>Creates the production runner over the harness's standard paths.</summary>
    public ParityRunner()
        : this(
            new ComparatorRegistry(),
            ParityPaths.Screenshots,
            ParityPaths.ReactDist,
            Path.Combine(AppContext.BaseDirectory, "react-dist"),
            browser => browser.NewContextAsync(),
            context => context.NewPageAsync(),
            page => page.CloseAsync(),
            new BaselineStore(),
            LiveBaselineSource.Read,
            LiveBundlePrecondition.Validate)
    {
    }

    /// <summary>Creates a production runner whose screenshot artifacts use a scoped directory.</summary>
    /// <param name="screenshotDirectory">The capture and pixel-diff directory.</param>
    public ParityRunner(string screenshotDirectory)
        : this(
            new ComparatorRegistry(screenshotDirectory),
            screenshotDirectory,
            ParityPaths.ReactDist,
            Path.Combine(AppContext.BaseDirectory, "react-dist"),
            browser => browser.NewContextAsync(),
            context => context.NewPageAsync(),
            page => page.CloseAsync(),
            new BaselineStore(screenshotDirectory),
            LiveBaselineSource.Read,
            LiveBundlePrecondition.Validate)
    {
    }

    internal ParityRunner(BaselineStore baselineStore)
        : this(
            new ComparatorRegistry(),
            ParityPaths.Screenshots,
            ParityPaths.ReactDist,
            Path.Combine(AppContext.BaseDirectory, "react-dist"),
            browser => browser.NewContextAsync(),
            context => context.NewPageAsync(),
            page => page.CloseAsync(),
            baselineStore,
            LiveBaselineSource.Read,
            LiveBundlePrecondition.Validate)
    {
    }

    internal ParityRunner(
        ComparatorRegistry registry,
        string screenshotDirectory,
        string sourceReactDist,
        string servedReactDist,
        Func<IBrowser, Task<IBrowserContext>>? createContext = null,
        Func<IBrowserContext, Task<IPage>>? createPage = null,
        Func<IPage, Task>? closePage = null,
        BaselineStore? baselineStore = null,
        Func<FixtureEntry, LiveBaselineProvenance>? readLiveProvenance = null,
        Action<string, string, FixtureEntry, LiveBaselineProvenance>? validateLiveBundle = null)
    {
        this.registry = registry;
        this.screenshotDirectory = screenshotDirectory;
        this.sourceReactDist = sourceReactDist;
        this.servedReactDist = servedReactDist;
        this.createContext = createContext ?? (browser => browser.NewContextAsync());
        this.createPage = createPage ?? (context => context.NewPageAsync());
        this.closePage = closePage ?? (page => page.CloseAsync());
        this.baselineStore = baselineStore ?? new BaselineStore();
        this.readLiveProvenance = readLiveProvenance ?? LiveBaselineSource.Read;
        this.validateLiveBundle = validateLiveBundle ?? LiveBundlePrecondition.Validate;
    }

    /// <summary>
    /// Captures the reference once and both required candidate legs, then runs the production
    /// comparison composition for each candidate.
    /// </summary>
    /// <param name="browser">The browser used for all three isolated pages.</param>
    /// <param name="fixture">The ordinary or reserved fixture to execute.</param>
    /// <returns>Server and WASM results, in that order.</returns>
    public async Task<IReadOnlyList<ParityRunResult>> RunLiveAsync(
        IBrowser browser,
        FixtureEntry fixture)
        => await RunAsync(
            browser,
            fixture,
            new ParityOptions { Mode = ParityReferenceMode.Live });

    /// <summary>Runs the configured baseline, live, or write-baseline orchestration.</summary>
    /// <param name="browser">The browser used for candidate capture and platform selection.</param>
    /// <param name="fixture">The fixture to execute.</param>
    /// <param name="options">The explicit invocation options.</param>
    /// <returns>Server and WASM results, in that order.</returns>
    public async Task<IReadOnlyList<ParityRunResult>> RunAsync(
        IBrowser browser,
        FixtureEntry fixture,
        ParityOptions options)
        => (await RunBatchAsync(browser, fixture, options, attempt: 1)).Results;

    /// <summary>Runs one whole-fixture attempt and preserves its scoped provenance.</summary>
    public async Task<ParityRunBatch> RunBatchAsync(
        IBrowser browser,
        FixtureEntry fixture,
        ParityOptions options,
        int attempt)
    {
        if (attempt < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(attempt));
        }

        var started = Stopwatch.GetTimestamp();
        var platform = BaselinePlatform.Current(browser);
        LiveBaselineProvenance? live = null;
        if (options.Mode != ParityReferenceMode.Baseline)
        {
            live = readLiveProvenance(fixture);
        }

        var results = await RunCoreAsync(browser, fixture, options, live);
        BaselineSnapshot? baseline = null;
        if (options.Mode == ParityReferenceMode.Baseline)
        {
            try
            {
                baseline = baselineStore.Describe(platform);
            }
            catch (InvalidOperationException) when (results.Any(result =>
                result.Findings.Any(finding => finding.Kind == FindingKind.FixtureError)))
            {
                // The result rows already own the typed missing/stale baseline evidence.
            }
        }
        var authority = baselineStore.DescribeAuthority();
        var liveSnapshots = live is null
            ? []
            : fixture.Themes.Select(theme =>
                baselineStore.DescribeLiveFixture(fixture, theme, live)).ToArray();

        return new ParityRunBatch
        {
            Attempt = attempt,
            Fixture = fixture,
            Options = options,
            ArtifactRoot = screenshotDirectory,
            Platform = platform,
            Duration = Stopwatch.GetElapsedTime(started),
            Results = results,
            Baseline = baseline,
            Authority = authority,
            LiveProvenance = liveSnapshots,
            LiveDistFingerprint = live is null
                ? null
                : ReactBundleProvenance.Fingerprint(sourceReactDist)
        };
    }

    private async Task<IReadOnlyList<ParityRunResult>> RunCoreAsync(
        IBrowser browser,
        FixtureEntry fixture,
        ParityOptions options,
        LiveBaselineProvenance? suppliedLiveProvenance = null)
    {
        ArgumentNullException.ThrowIfNull(browser);
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentNullException.ThrowIfNull(options);

        var platform = BaselinePlatform.Current(browser);
        var executions = FixtureExecution.Expand(fixture);

        var liveProvenance = suppliedLiveProvenance;
        if (options.Mode != ParityReferenceMode.Baseline)
        {
            liveProvenance ??= readLiveProvenance(fixture);
            _ = baselineStore.ValidateLiveProvenance(fixture, liveProvenance);
            validateLiveBundle(sourceReactDist, servedReactDist, fixture, liveProvenance);
        }

        IBrowserContext context;

        try
        {
            context = await createContext(browser);
        }
        catch (Exception exception)
        {
            return executions
                .SelectMany(execution => CandidateLegs().Select(leg => FailedCapture(
                    execution,
                    leg,
                    ParityLeg.React,
                    exception,
                    reference: null,
                    candidate: null,
                    property: "browser-context")))
                .ToArray();
        }

        var results = new List<ParityRunResult>(executions.Count * 2);
        var references = new List<CaptureBundle>(executions.Count);

        try
        {
            foreach (var execution in executions)
            {
                CaptureAttempt referenceAttempt;
                if (options.Mode == ParityReferenceMode.Baseline)
                {
                    try
                    {
                        referenceAttempt = new CaptureAttempt(
                            baselineStore.Load(fixture, execution.Theme, platform), Exception: null);
                    }
                    catch (Exception exception)
                    {
                        referenceAttempt = new CaptureAttempt(null, exception);
                    }
                }
                else
                {
                    referenceAttempt = Pin(
                        await CaptureAsync(context, execution, ParityLeg.React),
                        liveProvenance!);
                }

                if (referenceAttempt.Exception is null && referenceAttempt.Bundle is not null &&
                    (options.Mode != ParityReferenceMode.WriteBaseline ||
                     CanWriteReference(referenceAttempt.Bundle)))
                {
                    references.Add(referenceAttempt.Bundle);
                }

                foreach (var leg in CandidateLegs())
                {
                    // Candidate capture is independent evidence and remains required even if
                    // React failed. A broken reference must not hide whether Server or WASM can
                    // navigate, execute, settle, and capture the fixture.
                    var candidateAttempt = await CaptureAsync(context, execution, leg);
                    if (liveProvenance is not null)
                    {
                        candidateAttempt = Pin(candidateAttempt, liveProvenance);
                    }

                    var result = referenceAttempt.Exception is null &&
                                candidateAttempt.Exception is null &&
                                referenceAttempt.Bundle is not null &&
                                candidateAttempt.Bundle is not null
                        ? Compare(
                            execution,
                            leg,
                            referenceAttempt.Bundle,
                            candidateAttempt.Bundle)
                        : FailedCaptures(execution, leg, referenceAttempt, candidateAttempt);
                    results.Add(result);
                }
            }

            if (options.Mode == ParityReferenceMode.WriteBaseline &&
                references.Count == executions.Count)
            {
                try
                {
                    var receipt = baselineStore.Write(
                        fixture,
                        references,
                        platform,
                        liveProvenance!);
                    for (var index = 0; index < results.Count; index++)
                    {
                        results[index] = results[index] with { BaselineWrite = receipt };
                    }
                }
                catch (Exception exception)
                {
                    for (var index = 0; index < results.Count; index++)
                    {
                        results[index] = WithExecutionFailure(
                            results[index],
                            ParityLeg.React,
                            "baseline-write",
                            $"Baseline write failed: {SafeDetail(exception.Message)}");
                    }
                }
            }
        }
        finally
        {
            try
            {
                await context.DisposeAsync();
            }
            catch (Exception exception)
            {
                for (var index = 0; index < results.Count; index++)
                {
                    results[index] = WithExecutionFailure(
                        results[index],
                        results[index].Leg,
                        "browser-context-dispose",
                        $"Browser context cleanup failed: {SafeDetail(exception.Message)}");
                }
            }
        }

        return results;
    }

    private static CaptureAttempt Pin(
        CaptureAttempt attempt,
        LiveBaselineProvenance provenance)
        => attempt.Bundle is null
            ? attempt
            : attempt with
            {
                Bundle = attempt.Bundle with
                {
                    BaseUiSha = provenance.UpstreamSha,
                    SourceHash = provenance.SourceHash
                }
            };

    /// <summary>Pairs and compares already captured live bundles.</summary>
    /// <param name="fixture">The manifest contract governing the comparison.</param>
    /// <param name="candidateLeg">The required Blazor candidate leg.</param>
    /// <param name="reference">The React bundle, or <see langword="null"/> when absent.</param>
    /// <param name="candidate">The candidate bundle, or <see langword="null"/> when absent.</param>
    /// <returns>The typed integrated result.</returns>
    public ParityRunResult Compare(
        FixtureEntry fixture,
        ParityLeg candidateLeg,
        CaptureBundle? reference,
        CaptureBundle? candidate)
        => Compare(
            new FixtureExecution
            {
                Fixture = fixture,
                Theme = reference?.Theme ?? candidate?.Theme ?? fixture.Themes.First()
            },
            candidateLeg,
            reference,
            candidate);

    private ParityRunResult Compare(
        FixtureExecution execution,
        ParityLeg candidateLeg,
        CaptureBundle? reference,
        CaptureBundle? candidate)
    {
        var fixture = execution.Fixture;
        if (candidateLeg == ParityLeg.React)
        {
            throw new ArgumentOutOfRangeException(
                nameof(candidateLeg), candidateLeg, "The candidate leg must be Blazor.");
        }

        var configurationErrors = ValidateBundles(
            fixture, execution.Theme, candidateLeg, reference, candidate);

        if (configurationErrors.Count > 0)
        {
            return Result(
                execution, candidateLeg, reference, candidate, [], configurationErrors);
        }

        var findings = new List<Finding>();
        AddCompletionFindings(findings, reference);
        AddCompletionFindings(findings, candidate);
        AddCaptureEvidenceFindings(findings, reference);
        AddCaptureEvidenceFindings(findings, candidate);

        var referenceSteps = reference!.Steps.ToDictionary(step => step.Step, StringComparer.Ordinal);
        var candidateSteps = candidate!.Steps.ToDictionary(step => step.Step, StringComparer.Ordinal);
        var contexts = fixture.Steps
            .Select(step => new ComparisonContext(
                fixture.Id,
                execution.Theme,
                execution.ExecutionId,
                candidateLeg,
                step.Name,
                referenceSteps[step.Name],
                candidateSteps[step.Name],
                fixture.PixelThreshold))
            .ToArray();

        foreach (var context in contexts)
        {
            try
            {
                findings.AddRange(registry.Compare(context));
            }
            catch (Exception exception)
            {
                findings.Add(FixtureError(
                    fixture.Id,
                    candidateLeg,
                    context.Step,
                    "comparator",
                    $"Comparison failed for step '{context.Step}': {SafeDetail(exception.Message)}"));
                break;
            }
        }

        return Result(execution, candidateLeg, reference, candidate, contexts, findings);
    }

    private async Task<CaptureAttempt> CaptureAsync(
        IBrowserContext context,
        FixtureExecution execution,
        ParityLeg leg)
    {
        IPage page;

        try
        {
            page = await createPage(context);
        }
        catch (Exception exception)
        {
            return new CaptureAttempt(null, exception);
        }

        CaptureBundle? bundle = null;
        Exception? failure = null;

        try
        {
            bundle = await new ParityCapturer(screenshotDirectory)
                .CaptureAsync(page, execution.Fixture, leg, execution.Theme);
        }
        catch (Exception exception)
        {
            failure = exception;
        }

        try
        {
            await closePage(page);
        }
        catch (Exception closeFailure)
        {
            if (failure is null)
            {
                failure = closeFailure;
            }
            else
            {
                failure.Data["ParityPageCloseFailure"] = closeFailure;
            }
        }

        return new CaptureAttempt(bundle, failure);
    }

    private static void AddCompletionFindings(
        ICollection<Finding> findings,
        CaptureBundle? bundle)
    {
        if (bundle is null)
        {
            return;
        }

        foreach (var failure in bundle.Steps.SelectMany(step => step.ActionCompletionFailures))
        {
            var observed = $"expected={failure.ExpectedValue}; observed={failure.Observed}";

            findings.Add(new Finding
            {
                Fixture = failure.Fixture,
                Leg = failure.Leg,
                Step = failure.Step,
                Kind = FindingKind.ActionCompletionUnmet,
                Severity = Severity.Error,
                Property =
                    $"{failure.ActionIndex}:{failure.Verb}:{failure.Predicate}:{failure.Selector}",
                ReferenceValue = failure.Leg == ParityLeg.React ? observed : null,
                CandidateValue = failure.Leg == ParityLeg.React ? null : observed,
                Message =
                    $"Action {failure.ActionIndex} ({failure.Verb}) did not satisfy " +
                    $"'{failure.Predicate}' for '{failure.Selector}'; expected " +
                    $"'{failure.ExpectedValue}'. Observed: {failure.Observed}"
            });
        }
    }

    internal static bool CanWriteReference(CaptureBundle bundle)
        => bundle.Steps.All(step =>
            step.AnimationFrameCaptureFailures is { Count: 0 } &&
            step.ScreenshotObservations is not null &&
            step.ScreenshotObservations.All(observation =>
                observation.State != ScreenshotObservationState.CaptureFailed));

    private static void AddCaptureEvidenceFindings(
        ICollection<Finding> findings,
        CaptureBundle? bundle)
    {
        if (bundle is null)
        {
            return;
        }

        foreach (var step in bundle.Steps)
        {
            foreach (var observation in step.ScreenshotObservations.Where(
                         item => item.State == ScreenshotObservationState.CaptureFailed))
            {
                findings.Add(FixtureError(
                    bundle.Fixture,
                    bundle.Leg,
                    step.Step,
                    $"screenshot:{observation.Shot}:{observation.RootLabel}",
                    $"Visible capture root '{observation.RootLabel}' failed at shot " +
                    $"'{observation.Shot}': {SafeDetail(observation.Detail!)}"));
            }

            foreach (var failure in step.AnimationFrameCaptureFailures)
            {
                var action = failure.ActionIndex is { } index ? $", action {index}" : string.Empty;
                var cleanup = failure.CleanupDetail is null
                    ? string.Empty
                    : $" Cleanup also failed: {SafeDetail(failure.CleanupDetail)}";
                findings.Add(FixtureError(
                    bundle.Fixture,
                    bundle.Leg,
                    step.Step,
                    $"animation-frame:{failure.Stage}:{failure.ActionIndex?.ToString() ?? "none"}",
                    $"Animation frame replay failed at stage '{failure.Stage}' for step " +
                    $"'{step.Step}'{action}: {SafeDetail(failure.Detail)}.{cleanup}"));
            }
        }
    }

    private static ParityRunResult FailedCapture(
        FixtureExecution execution,
        ParityLeg resultLeg,
        ParityLeg failedLeg,
        Exception exception,
        CaptureBundle? reference,
        CaptureBundle? candidate,
        string? property = null)
        => Result(
            execution,
            resultLeg,
            reference,
            candidate,
            [],
            [FixtureError(
                execution.ExecutionId,
                failedLeg,
                string.Empty,
                property ?? failedLeg.ToString(),
                $"Capture failed on {failedLeg}: {SafeDetail(exception.Message)}")]);

    private static ParityRunResult FailedCaptures(
        FixtureExecution execution,
        ParityLeg resultLeg,
        CaptureAttempt referenceAttempt,
        CaptureAttempt candidateAttempt)
    {
        var findings = new List<Finding>();
        AddCompletionFindings(findings, referenceAttempt.Bundle);
        AddCompletionFindings(findings, candidateAttempt.Bundle);

        if (referenceAttempt.Exception is not null)
        {
                findings.Add(FixtureError(
                execution.ExecutionId,
                ParityLeg.React,
                string.Empty,
                ParityLeg.React.ToString(),
                $"Capture failed on React: {CaptureFailureMessage(referenceAttempt.Exception)}"));
        }

        if (candidateAttempt.Exception is not null)
        {
            findings.Add(FixtureError(
                execution.ExecutionId,
                resultLeg,
                string.Empty,
                resultLeg.ToString(),
                $"Capture failed on {resultLeg}: " +
                CaptureFailureMessage(candidateAttempt.Exception)));
        }

        return Result(
            execution,
            resultLeg,
            referenceAttempt.Bundle,
            candidateAttempt.Bundle,
            [],
            findings);
    }

    private static Finding FixtureError(
        string fixture,
        ParityLeg leg,
        string step,
        string property,
        string message)
        => new()
        {
            Fixture = fixture,
            Leg = leg,
            Step = step,
            Kind = FindingKind.FixtureError,
            Severity = Severity.Error,
            Property = property,
            Message = message
        };

    private static string CaptureFailureMessage(Exception exception)
    {
        if (exception.Data["ParityPageCloseFailure"] is not Exception closeFailure)
        {
            return SafeDetail(exception.Message);
        }

        return $"{SafeDetail(exception.Message)} Page cleanup also failed: " +
               SafeDetail(closeFailure.Message);
    }

    private static string SafeDetail(string detail)
    {
        var oneLine = detail.Replace('\r', ' ').Replace('\n', ' ')
            .Replace('\\', '∖').Replace('/', '∕')
            .Replace("file:", "file∶", StringComparison.OrdinalIgnoreCase);
        return oneLine.Length <= 500 ? oneLine : oneLine[..500];
    }

    private static ParityRunResult Result(
        FixtureExecution execution,
        ParityLeg leg,
        CaptureBundle? reference,
        CaptureBundle? candidate,
        IReadOnlyList<ComparisonContext> contexts,
        IReadOnlyList<Finding> findings)
        => new()
        {
            Fixture = execution.Fixture.Id,
            Theme = execution.Theme,
            ExecutionId = execution.ExecutionId,
            Leg = leg,
            Reference = reference,
            Candidate = candidate,
            Contexts = contexts,
            Findings = findings
                .Select(finding => finding with { Fixture = execution.ExecutionId })
                .ToArray()
        };

    private static IReadOnlyList<ParityLeg> CandidateLegs()
        => [ParityLeg.BlazorServer, ParityLeg.BlazorWasm];

    private static ParityRunResult WithExecutionFailure(
        ParityRunResult result,
        ParityLeg actualLeg,
        string property,
        string message)
        => result with
        {
            Findings =
            [
                .. result.Findings,
                FixtureError(result.ExecutionId, actualLeg, string.Empty, property, message)
            ]
        };

    private static IReadOnlyList<Finding> ValidateBundles(
        FixtureEntry fixture,
        string theme,
        ParityLeg candidateLeg,
        CaptureBundle? reference,
        CaptureBundle? candidate)
    {
        var errors = new List<Finding>();

        ValidateBundle(errors, fixture, theme, reference, ParityLeg.React, candidateLeg);
        ValidateBundle(errors, fixture, theme, candidate, candidateLeg, candidateLeg);

        if (reference is null || candidate is null)
        {
            return errors;
        }

        var expectedSteps = fixture.Steps.ToArray();
        var duplicateManifestStep = expectedSteps
            .GroupBy(step => step.Name, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateManifestStep is not null)
        {
            errors.Add(FixtureError(
                fixture.Id,
                candidateLeg,
                duplicateManifestStep.Key,
                "manifest-step",
                $"Manifest step '{duplicateManifestStep.Key}' is duplicated."));
            return errors;
        }

        ValidateSteps(errors, fixture, reference, expectedSteps, candidateLeg);
        ValidateSteps(errors, fixture, candidate, expectedSteps, candidateLeg);

        return errors;
    }

    private static void ValidateBundle(
        ICollection<Finding> errors,
        FixtureEntry fixture,
        string theme,
        CaptureBundle? bundle,
        ParityLeg expectedLeg,
        ParityLeg resultLeg)
    {
        if (bundle is null)
        {
            errors.Add(FixtureError(
                fixture.Id,
                expectedLeg,
                string.Empty,
                expectedLeg.ToString(),
                $"Required {expectedLeg} capture is missing for candidate result {resultLeg}."));
            return;
        }

        if (!string.Equals(bundle.Fixture, fixture.Id, StringComparison.Ordinal))
        {
            errors.Add(FixtureError(
                fixture.Id,
                bundle.Leg,
                string.Empty,
                "fixture",
                $"Expected fixture '{fixture.Id}' but received '{bundle.Fixture}'."));
        }

        if (bundle.Leg != expectedLeg)
        {
            errors.Add(FixtureError(
                fixture.Id,
                bundle.Leg,
                string.Empty,
                "leg",
                $"Expected capture leg '{expectedLeg}' but received '{bundle.Leg}'."));
        }

        if (!string.Equals(bundle.Theme, theme, StringComparison.Ordinal))
        {
            errors.Add(FixtureError(
                fixture.Id,
                bundle.Leg,
                string.Empty,
                "theme",
                $"Expected capture theme '{theme}' but received '{bundle.Theme}'."));
        }

        if (bundle.CaptureSchemaVersion != CaptureSchema.CurrentVersion)
        {
            errors.Add(FixtureError(
                fixture.Id,
                bundle.Leg,
                string.Empty,
                "capture-schema",
                $"Capture leg '{bundle.Leg}' declares schema " +
                $"'{bundle.CaptureSchemaVersion}', expected '{CaptureSchema.CurrentVersion}'."));
        }
    }

    private static void ValidateSteps(
        ICollection<Finding> errors,
        FixtureEntry fixture,
        CaptureBundle bundle,
        IReadOnlyList<StepEntry> expectedSteps,
        ParityLeg resultLeg)
    {
        if (bundle.Steps is null)
        {
            errors.Add(FixtureError(
                fixture.Id,
                bundle.Leg,
                string.Empty,
                "step",
                $"Capture leg '{bundle.Leg}' has a null steps collection."));
            return;
        }

        if (bundle.Steps.Any(step => step is null))
        {
            errors.Add(FixtureError(
                fixture.Id,
                bundle.Leg,
                string.Empty,
                "step",
                $"Capture leg '{bundle.Leg}' contains a null step."));
            return;
        }

        var duplicate = bundle.Steps
            .GroupBy(step => step.Step, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            errors.Add(FixtureError(
                fixture.Id,
                bundle.Leg,
                duplicate.Key,
                "step",
                $"Capture leg '{bundle.Leg}' contains duplicate step '{duplicate.Key}'."));
        }

        var expectedNames = expectedSteps.Select(step => step.Name).ToArray();
        var actual = bundle.Steps.Select(step => step.Step).ToHashSet(StringComparer.Ordinal);

        foreach (var missing in expectedNames.Where(step => !actual.Contains(step)))
        {
            errors.Add(FixtureError(
                fixture.Id,
                bundle.Leg,
                missing,
                "step",
                $"Capture leg '{bundle.Leg}' is missing required step '{missing}' " +
                $"for candidate result {resultLeg}."));
        }

        foreach (var unexpected in actual.Where(step => !expectedNames.Contains(
                     step, StringComparer.Ordinal)).OrderBy(step => step, StringComparer.Ordinal))
        {
            errors.Add(FixtureError(
                fixture.Id,
                bundle.Leg,
                unexpected,
                "step",
                $"Capture leg '{bundle.Leg}' contains unexpected step '{unexpected}' " +
                $"for candidate result {resultLeg}."));
        }

        var aliases = AliasTable.Load();

        foreach (var expectedStep in expectedSteps)
        {
            var matches = bundle.Steps
                .Where(step => string.Equals(
                    step.Step, expectedStep.Name, StringComparison.Ordinal))
                .ToArray();

            if (matches.Length == 1)
            {
                ValidateActions(errors, fixture, bundle.Leg, expectedStep, matches[0], aliases);
                ValidateScreenshotObservations(errors, fixture, bundle.Leg, matches[0]);
            }
        }
    }

    private static void ValidateScreenshotObservations(
        ICollection<Finding> errors,
        FixtureEntry fixture,
        ParityLeg leg,
        StepCapture capture)
    {
        if (capture.ScreenshotObservations is null)
        {
            errors.Add(FixtureError(fixture.Id, leg, capture.Step, "screenshots",
                "Screenshot observations cannot be null."));
            return;
        }

        if (capture.ScreenshotObservations.Any(item => item is null))
        {
            errors.Add(FixtureError(fixture.Id, leg, capture.Step, "screenshots",
                "Screenshot observations cannot contain null entries."));
            return;
        }

        var duplicate = capture.ScreenshotObservations
            .GroupBy(item => item.Shot, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            errors.Add(FixtureError(fixture.Id, leg, capture.Step, "screenshots",
                $"Screenshot shot '{duplicate.Key}' is duplicated."));
        }

        foreach (var observation in capture.ScreenshotObservations)
        {
            var validPayload = Enum.IsDefined(observation.State) &&
                !string.IsNullOrWhiteSpace(observation.RootLabel) &&
                !string.IsNullOrWhiteSpace(observation.Shot) && (observation.State switch
                {
                    ScreenshotObservationState.Captured =>
                        !string.IsNullOrWhiteSpace(observation.FileName) && observation.Detail is null,
                    ScreenshotObservationState.NotVisible =>
                        observation.FileName is null && observation.Detail is null,
                    ScreenshotObservationState.CaptureFailed =>
                        observation.FileName is null && !string.IsNullOrWhiteSpace(observation.Detail),
                    _ => false
                });
            if (!validPayload)
            {
                errors.Add(FixtureError(fixture.Id, leg, capture.Step,
                    $"screenshot:{observation.Shot}",
                    $"Screenshot observation '{observation.Shot}' has an invalid state payload."));
            }
        }

        if (capture.AnimationFrameCaptureFailures is null ||
            capture.AnimationFrameCaptureFailures.Any(failure => failure is null ||
                string.IsNullOrWhiteSpace(failure.Stage) ||
                string.IsNullOrWhiteSpace(failure.Detail)))
        {
            errors.Add(FixtureError(fixture.Id, leg, capture.Step, "animation-frame",
                "Animation frame failures must be non-null with nonblank stage and detail."));
        }
    }

    private static void ValidateActions(
        ICollection<Finding> errors,
        FixtureEntry fixture,
        ParityLeg leg,
        StepEntry expectedStep,
        StepCapture capture,
        AliasTable aliases)
    {
        if (capture.Actions is null)
        {
            AddActionError(errors, fixture, leg, expectedStep.Name,
                "the action trace is null");
            return;
        }

        if (capture.Actions.Count != expectedStep.Do.Count)
        {
            AddActionError(
                errors,
                fixture,
                leg,
                expectedStep.Name,
                $"expected {expectedStep.Do.Count} rows but received {capture.Actions.Count}");
            return;
        }

        if (capture.Actions.Any(action => action is null))
        {
            AddActionError(errors, fixture, leg, expectedStep.Name,
                "the action trace contains a null row");
            return;
        }

        var failureSeen = false;

        for (var index = 0; index < capture.Actions.Count; index++)
        {
            var actual = capture.Actions[index];
            var expected = ActionExecutionContract.Expected(
                aliases,
                fixture.Component,
                expectedStep.Do[index],
                index,
                actual.Status);

            if (actual.ActionIndex != index ||
                !string.Equals(actual.Verb, expected.Verb, StringComparison.Ordinal) ||
                !string.Equals(
                    actual.ExpandedSelector,
                    expected.ExpandedSelector,
                    StringComparison.Ordinal))
            {
                AddActionError(
                    errors,
                    fixture,
                    leg,
                    expectedStep.Name,
                    $"row {index} does not match its manifest index, verb, and expanded target");
            }

            if (!Enum.IsDefined(actual.Status))
            {
                AddActionError(
                    errors,
                    fixture,
                    leg,
                    expectedStep.Name,
                    $"row {index} has unknown status '{(int)actual.Status}'");
                continue;
            }

            var manifestAction = expectedStep.Do[index];
            if (actual.Status == ActionExecutionStatus.CompletionUnmet &&
                manifestAction.Complete is not { Count: > 0 })
            {
                AddActionError(
                    errors,
                    fixture,
                    leg,
                    expectedStep.Name,
                    $"row {index} reports CompletionUnmet without a manifest completion contract");
            }

            if (actual.Status is
                    ActionExecutionStatus.Unresolved or
                    ActionExecutionStatus.NonActionable &&
                expected.ExpandedSelector is null)
            {
                AddActionError(
                    errors,
                    fixture,
                    leg,
                    expectedStep.Name,
                    $"row {index} reports selector failure for a selectorless action");
            }

            if (manifestAction.Wait is not null &&
                actual.Status == ActionExecutionStatus.NonActionable)
            {
                AddActionError(
                    errors,
                    fixture,
                    leg,
                    expectedStep.Name,
                    $"row {index} reports NonActionable for wait, whose resolution probe " +
                    "has no actionability operation");
            }

            if (!failureSeen && actual.Status == ActionExecutionStatus.Completed)
            {
                continue;
            }

            if (!failureSeen && actual.Status is
                ActionExecutionStatus.Unresolved or
                ActionExecutionStatus.NonActionable or
                ActionExecutionStatus.CompletionUnmet)
            {
                failureSeen = true;
                continue;
            }

            if (failureSeen && actual.Status == ActionExecutionStatus.Skipped)
            {
                continue;
            }

            AddActionError(
                errors,
                fixture,
                leg,
                expectedStep.Name,
                $"row {index} violates Completed*, one optional failure, then Skipped* order");
        }

        ValidateCompletionDetails(errors, fixture, leg, expectedStep, capture, aliases);
    }

    private static void ValidateCompletionDetails(
        ICollection<Finding> errors,
        FixtureEntry fixture,
        ParityLeg leg,
        StepEntry expectedStep,
        StepCapture capture,
        AliasTable aliases)
    {
        if (capture.ActionCompletionFailures is null)
        {
            AddActionError(errors, fixture, leg, expectedStep.Name,
                "the completion-detail collection is null");
            return;
        }

        foreach (var action in capture.Actions)
        {
            var matches = capture.ActionCompletionFailures
                .Where(failure => failure is not null && failure.ActionIndex == action.ActionIndex)
                .ToArray();
            var expectedCount = action.Status == ActionExecutionStatus.CompletionUnmet ? 1 : 0;

            if (matches.Length != expectedCount)
            {
                AddActionError(
                    errors,
                    fixture,
                    leg,
                    expectedStep.Name,
                    $"row {action.ActionIndex} with status '{action.Status}' requires exactly " +
                    $"{expectedCount} completion-detail rows but received {matches.Length}");
                continue;
            }

            if (matches.Length == 1)
            {
                var detail = matches[0];
                if (action.ActionIndex < 0 || action.ActionIndex >= expectedStep.Do.Count)
                {
                    AddActionError(
                        errors,
                        fixture,
                        leg,
                        expectedStep.Name,
                        $"completion detail references unknown row {action.ActionIndex}");
                    continue;
                }

                var manifestAction = expectedStep.Do[action.ActionIndex];
                var matchesManifestPredicate = manifestAction.Complete is { Count: > 0 } predicates &&
                    predicates.Any(predicate =>
                        string.Equals(
                            detail.Selector,
                            aliases.Expand(fixture.Component, predicate.Selector!),
                            StringComparison.Ordinal) &&
                        string.Equals(
                            detail.Predicate,
                            ParityCapturer.PredicateName(predicate),
                            StringComparison.Ordinal) &&
                        string.Equals(
                            detail.ExpectedValue,
                            ParityCapturer.ExpectedValue(predicate),
                            StringComparison.Ordinal));

                if (string.IsNullOrWhiteSpace(detail.Observed) ||
                    detail.Observed.Length > MaximumObservedLength)
                {
                    AddActionError(
                        errors,
                        fixture,
                        leg,
                        expectedStep.Name,
                        $"completion detail for row {action.ActionIndex} has an observed snapshot " +
                        $"that is blank or exceeds {MaximumObservedLength} characters");
                }

                if (!string.Equals(detail.Fixture, fixture.Id, StringComparison.Ordinal) ||
                    detail.Leg != leg ||
                    !string.Equals(detail.Step, expectedStep.Name, StringComparison.Ordinal) ||
                    !string.Equals(detail.Verb, action.Verb, StringComparison.Ordinal) ||
                    !matchesManifestPredicate)
                {
                    AddActionError(
                        errors,
                        fixture,
                        leg,
                        expectedStep.Name,
                        $"completion detail for row {action.ActionIndex} has inconsistent identity");
                }
            }
        }

        if (capture.ActionCompletionFailures.Any(failure => failure is null) ||
            capture.ActionCompletionFailures.Any(failure =>
                failure is not null &&
                (failure.ActionIndex < 0 || failure.ActionIndex >= capture.Actions.Count)))
        {
            AddActionError(errors, fixture, leg, expectedStep.Name,
                "the completion-detail collection contains an unassociated row");
        }
    }

    private static void AddActionError(
        ICollection<Finding> errors,
        FixtureEntry fixture,
        ParityLeg leg,
        string step,
        string reason)
        => errors.Add(FixtureError(
            fixture.Id,
            leg,
            step,
            "action-trace",
            $"Capture leg '{leg}', step '{step}' has an invalid action trace: {reason}."));

    private sealed record CaptureAttempt(CaptureBundle? Bundle, Exception? Exception);
}
