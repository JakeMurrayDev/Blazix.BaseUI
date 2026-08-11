using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Diff;
using Blazix.BaseUI.Parity.Tests.Waivers;

namespace Blazix.BaseUI.Parity.Tests.Infrastructure;

/// <summary>The capture/execution state of one parity attempt.</summary>
public enum RetryAttemptState
{
    /// <summary>Every declared scope was captured completely.</summary>
    Complete,

    /// <summary>At least one declared scope lacks complete capture evidence.</summary>
    IncompleteCapture,

    /// <summary>The browser, host, fixture, or retry execution failed.</summary>
    ExecutionFailure
}

/// <summary>How a finding behaved across two attempts.</summary>
public enum RetryFindingClassification
{
    /// <summary>The same Error identity occurred in both complete attempts.</summary>
    Stable,

    /// <summary>One attempt was clean in the same complete scope.</summary>
    Flaky,

    /// <summary>Different Error identities occurred in the same scope.</summary>
    IdentityChanged,

    /// <summary>The evidence is Info or was already classified Flaky.</summary>
    Informational,

    /// <summary>The finding kind is never eligible for retry demotion.</summary>
    NonWaivable,

    /// <summary>Correlation was unsafe because an attempt or identity was incomplete.</summary>
    ExecutionFailure
}

/// <summary>A fixture, candidate leg, and step whose capture can prove a clean retry.</summary>
/// <param name="Fixture">The fixture id.</param>
/// <param name="Leg">The candidate leg.</param>
/// <param name="Step">The manifest step.</param>
public sealed record RetryScope(string Fixture, ParityLeg Leg, string Step)
{
    /// <summary>Creates the corresponding scope for a finding.</summary>
    /// <param name="finding">The finding.</param>
    /// <returns>The finding's scope.</returns>
    public static RetryScope From(Finding finding)
    {
        ArgumentNullException.ThrowIfNull(finding);
        return new RetryScope(finding.Fixture, finding.Leg, finding.Step);
    }
}

/// <summary>One complete or failed attempt supplied to retry correlation.</summary>
public sealed record RetryAttempt
{
    /// <summary>Gets the scopes this attempt was expected to capture.</summary>
    public required IReadOnlyList<RetryScope> Scopes { get; init; }

    /// <summary>Gets findings in production order.</summary>
    public IReadOnlyList<Finding> Findings { get; init; } = [];

    /// <summary>Gets the attempt state.</summary>
    public RetryAttemptState State { get; init; } = RetryAttemptState.Complete;

    /// <summary>Gets execution or capture failure detail for a non-complete attempt.</summary>
    public string? Failure { get; init; }
}

/// <summary>One identity's evidence across both attempts.</summary>
public sealed record RetryFindingEvidence
{
    /// <summary>Gets the six-field machine identity.</summary>
    public required FindingIdentity Identity { get; init; }

    /// <summary>Gets first-attempt evidence, when present.</summary>
    public Finding? FirstAttempt { get; init; }

    /// <summary>Gets retry-attempt evidence, when present.</summary>
    public Finding? RetryAttempt { get; init; }

    /// <summary>Gets the effective finding used by policy/reporting.</summary>
    public required Finding Effective { get; init; }

    /// <summary>Gets the conservative retry classification.</summary>
    public required RetryFindingClassification Classification { get; init; }
}

/// <summary>One nonwaivable retry/capture execution failure.</summary>
/// <param name="Attempt">The one-based attempt number.</param>
/// <param name="State">The failed attempt state.</param>
/// <param name="Message">The failure detail.</param>
public sealed record RetryFailure(int Attempt, RetryAttemptState State, string Message);

/// <summary>The effective findings and nonwaivable execution evidence for two attempts.</summary>
public sealed record RetryVerdict
{
    /// <summary>Gets identity evidence in deterministic first-seen order.</summary>
    public required IReadOnlyList<RetryFindingEvidence> Evidence { get; init; }

    /// <summary>Gets effective findings in the same order as <see cref="Evidence"/>.</summary>
    public required IReadOnlyList<Finding> Findings { get; init; }

    /// <summary>Gets nonwaivable failures in attempt order.</summary>
    public required IReadOnlyList<RetryFailure> Failures { get; init; }

    /// <summary>Gets whether an Error or execution failure still blocks.</summary>
    public bool HasBlockingEvidence =>
        Failures.Count > 0 || Findings.Any(finding => finding.Severity == Severity.Error);
}

/// <summary>Correlates two attempts using only exact finding identities.</summary>
public static class RetryClassifier
{
    /// <summary>Classifies two attempts without using messages or values as identity.</summary>
    /// <param name="first">The first attempt.</param>
    /// <param name="retry">The retry attempt.</param>
    /// <returns>The conservative retry verdict.</returns>
    public static RetryVerdict Classify(RetryAttempt first, RetryAttempt retry)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(retry);

        Validate(first, 1);
        Validate(retry, 2);

        var firstByIdentity = Unique(first.Findings);
        var retryByIdentity = Unique(retry.Findings);
        var duplicateIdentities = Duplicates(first.Findings)
            .Concat(Duplicates(retry.Findings))
            .ToHashSet();
        var identities = first.Findings.Select(FindingIdentity.From)
            .Concat(retry.Findings.Select(FindingIdentity.From))
            .Distinct()
            .ToArray();
        var evidence = new List<RetryFindingEvidence>(identities.Length);
        var attemptsComplete = first.State == RetryAttemptState.Complete &&
                               retry.State == RetryAttemptState.Complete;
        var sharedScopes = first.Scopes.Intersect(retry.Scopes).ToHashSet();

        foreach (var identity in identities)
        {
            firstByIdentity.TryGetValue(identity, out var left);
            retryByIdentity.TryGetValue(identity, out var right);

            var classification = Classify(
                identity,
                left,
                right,
                first,
                retry,
                attemptsComplete,
                sharedScopes.Contains(new RetryScope(identity.Fixture, identity.Leg, identity.Step)),
                duplicateIdentities.Contains(identity));
            var source = right?.Severity == Severity.Error
                ? right
                : left?.Severity == Severity.Error
                    ? left
                    : right ?? left!;
            var effective = classification == RetryFindingClassification.Flaky
                ? source with { Severity = Severity.Flaky }
                : source;

            evidence.Add(new RetryFindingEvidence
            {
                Identity = identity,
                FirstAttempt = left,
                RetryAttempt = right,
                Effective = effective,
                Classification = classification
            });
        }

        var failures = Failures(first, retry).ToArray();

        return new RetryVerdict
        {
            Evidence = evidence,
            Findings = [.. evidence.Select(item => item.Effective)],
            Failures = failures
        };
    }

    private static RetryFindingClassification Classify(
        FindingIdentity identity,
        Finding? firstFinding,
        Finding? retryFinding,
        RetryAttempt first,
        RetryAttempt retry,
        bool attemptsComplete,
        bool scopeComplete,
        bool duplicateIdentity)
    {
        var firstError = firstFinding?.Severity == Severity.Error;
        var retryError = retryFinding?.Severity == Severity.Error;

        if (!firstError && !retryError)
        {
            return RetryFindingClassification.Informational;
        }

        if (ComparatorRegistry.NonWaivableKinds.Contains(identity.Kind))
        {
            return RetryFindingClassification.NonWaivable;
        }

        if (duplicateIdentity || !attemptsComplete || !scopeComplete)
        {
            return RetryFindingClassification.ExecutionFailure;
        }

        if (firstError && retryError)
        {
            return RetryFindingClassification.Stable;
        }

        var scope = new RetryScope(identity.Fixture, identity.Leg, identity.Step);
        var otherAttempt = firstError ? retry : first;
        var hasDifferentError = otherAttempt.Findings.Any(finding =>
            finding.Severity == Severity.Error &&
            RetryScope.From(finding) == scope &&
            FindingIdentity.From(finding) != identity);

        return hasDifferentError
            ? RetryFindingClassification.IdentityChanged
            : RetryFindingClassification.Flaky;
    }

    private static void Validate(RetryAttempt attempt, int number)
    {
        ArgumentNullException.ThrowIfNull(attempt.Scopes);
        ArgumentNullException.ThrowIfNull(attempt.Findings);

        if (attempt.Scopes.Count != attempt.Scopes.Distinct().Count())
        {
            throw new ArgumentException($"Retry attempt {number} contains duplicate scopes.");
        }

        if (attempt.Scopes.Any(scope => !FixtureExecution.IsExecutionId(scope.Fixture)) ||
            attempt.Findings.Any(finding => !FixtureExecution.IsExecutionId(finding.Fixture)))
        {
            throw new ArgumentException(
                $"Retry attempt {number} contains an invalid fixture-theme identity; " +
                "expected exactly one '@light' or '@dark' suffix.");
        }

        if (attempt.State == RetryAttemptState.Complete && attempt.Failure is not null)
        {
            throw new ArgumentException(
                $"Retry attempt {number} is complete but contains failure detail.");
        }

        if (attempt.State != RetryAttemptState.Complete &&
            string.IsNullOrWhiteSpace(attempt.Failure))
        {
            throw new ArgumentException(
                $"Retry attempt {number} is incomplete but has no failure detail.");
        }

        var scopes = attempt.Scopes.ToHashSet();
        if (attempt.Findings.Any(finding => !scopes.Contains(RetryScope.From(finding))))
        {
            throw new ArgumentException(
                $"Retry attempt {number} contains a finding outside its declared scopes.");
        }
    }

    private static Dictionary<FindingIdentity, Finding> Unique(IReadOnlyList<Finding> findings)
        => findings
            .GroupBy(FindingIdentity.From)
            .ToDictionary(group => group.Key, group => group.First());

    private static IEnumerable<FindingIdentity> Duplicates(IReadOnlyList<Finding> findings)
        => findings
            .GroupBy(FindingIdentity.From)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

    private static IEnumerable<RetryFailure> Failures(RetryAttempt first, RetryAttempt retry)
    {
        if (first.State != RetryAttemptState.Complete)
        {
            yield return new RetryFailure(1, first.State, first.Failure!);
        }

        if (retry.State != RetryAttemptState.Complete)
        {
            yield return new RetryFailure(2, retry.State, retry.Failure!);
        }

        var retryScopes = retry.Scopes.ToHashSet();
        foreach (var scope in first.Scopes.Where(scope => !retryScopes.Contains(scope)))
        {
            yield return new RetryFailure(
                2,
                RetryAttemptState.IncompleteCapture,
                $"Retry attempt 2 is missing declared scope {scope}.");
        }

        var firstScopes = first.Scopes.ToHashSet();
        foreach (var scope in retry.Scopes.Where(scope => !firstScopes.Contains(scope)))
        {
            yield return new RetryFailure(
                1,
                RetryAttemptState.IncompleteCapture,
                $"Retry attempt 1 is missing declared scope {scope}.");
        }

        foreach (var identity in Duplicates(first.Findings))
        {
            yield return new RetryFailure(
                1,
                RetryAttemptState.ExecutionFailure,
                $"Finding identity occurs more than once: {identity}.");
        }

        foreach (var identity in Duplicates(retry.Findings))
        {
            yield return new RetryFailure(
                2,
                RetryAttemptState.ExecutionFailure,
                $"Finding identity occurs more than once: {identity}.");
        }
    }
}
