using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Diff;

namespace Blazix.BaseUI.Parity.Tests.Waivers;

/// <summary>How a waiver's property field is compared.</summary>
public enum WaiverPropertyMatch
{
    /// <summary>The finding property must equal the waiver property ordinally.</summary>
    Exact,

    /// <summary>The finding property must start with the waiver property ordinally.</summary>
    Prefix
}

/// <summary>Why an exact parity difference remains accepted temporarily.</summary>
public enum WaiverDisposition
{
    /// <summary>The difference is a durable, documented product limitation.</summary>
    AcceptedLimitation,

    /// <summary>The difference is an owned defect awaiting a fix.</summary>
    DeferredDefect
}

/// <summary>Whether an external policy validator confirmed a deferred issue.</summary>
public enum WaiverIssuePolicyStatus
{
    /// <summary>The accepted limitation uses repository documentation instead of an issue.</summary>
    NotRequired,

    /// <summary>The offline loader validated only the issue URL shape.</summary>
    Unverified,

    /// <summary>An injected validator confirmed the live issue policy.</summary>
    Verified
}

/// <summary>The externally observed policy state of a deferred-defect issue.</summary>
/// <param name="IsOpen">Whether the issue is open.</param>
/// <param name="IsOwned">Whether the issue has an explicit owner.</param>
/// <param name="CapturedAttemptCount">How many captured attempts the issue documents.</param>
/// <param name="HasAcceptanceCriteria">Whether the issue contains acceptance criteria.</param>
/// <param name="Detail">Optional validator detail for a rejection.</param>
public sealed record WaiverIssuePolicyValidation(
    bool IsOpen,
    bool IsOwned,
    int CapturedAttemptCount,
    bool HasAcceptanceCriteria,
    string? Detail = null);

/// <summary>Validates live tracking-issue state outside the ordinary offline loader.</summary>
public interface IWaiverIssuePolicyValidator
{
    /// <summary>Reads the policy state of one repository issue URL.</summary>
    /// <param name="issueUrl">The already shape-validated issue URL.</param>
    /// <returns>The live issue policy state.</returns>
    WaiverIssuePolicyValidation Validate(string issueUrl);
}

/// <summary>The exact machine identity shared by waivers and retry correlation.</summary>
/// <param name="Fixture">The fixture id.</param>
/// <param name="Leg">The candidate leg.</param>
/// <param name="Step">The manifest step.</param>
/// <param name="NodePath">The exact normalized node path.</param>
/// <param name="Kind">The finding kind.</param>
/// <param name="Property">The exact comparator-owned discriminator.</param>
public sealed record FindingIdentity(
    string Fixture,
    ParityLeg Leg,
    string Step,
    string NodePath,
    FindingKind Kind,
    string Property)
{
    /// <summary>Creates the machine identity for a finding.</summary>
    /// <param name="finding">The finding to identify.</param>
    /// <returns>The finding's six-field identity.</returns>
    public static FindingIdentity From(Finding finding)
    {
        ArgumentNullException.ThrowIfNull(finding);

        return new FindingIdentity(
            finding.Fixture,
            finding.Leg,
            finding.Step,
            finding.NodePath,
            finding.Kind,
            finding.Property);
    }
}

/// <summary>One reviewed, expiring waiver for one exact Error identity.</summary>
public sealed record Waiver
{
    private bool IssuePolicyVerified { get; init; }

    /// <summary>Gets the fixture id.</summary>
    public required string Fixture { get; init; }

    /// <summary>Gets the candidate leg.</summary>
    public required ParityLeg Leg { get; init; }

    /// <summary>Gets the manifest step.</summary>
    public required string Step { get; init; }

    /// <summary>Gets the exact normalized node path.</summary>
    public required string NodePath { get; init; }

    /// <summary>Gets the finding kind.</summary>
    public required FindingKind Kind { get; init; }

    /// <summary>Gets the exact property or the Console-only prefix.</summary>
    public required string Property { get; init; }

    /// <summary>Gets how <see cref="Property"/> is compared.</summary>
    public WaiverPropertyMatch PropertyMatch { get; init; } = WaiverPropertyMatch.Exact;

    /// <summary>Gets the human explanation for accepting the evidence.</summary>
    public required string Reason { get; init; }

    /// <summary>Gets the reviewed disposition.</summary>
    public required WaiverDisposition Disposition { get; init; }

    /// <summary>Gets the repository documentation or tracking issue link.</summary>
    public required string DocLink { get; init; }

    /// <summary>Gets the final date on which the waiver is valid.</summary>
    public required DateOnly Expires { get; init; }

    /// <summary>Gets whether deferred-issue policy was externally verified.</summary>
    public WaiverIssuePolicyStatus IssuePolicyStatus =>
        Disposition == WaiverDisposition.AcceptedLimitation
            ? WaiverIssuePolicyStatus.NotRequired
            : IssuePolicyVerified
                ? WaiverIssuePolicyStatus.Verified
                : WaiverIssuePolicyStatus.Unverified;

    /// <summary>Gets the six machine identity fields.</summary>
    public FindingIdentity Identity => new(Fixture, Leg, Step, NodePath, Kind, Property);

    /// <summary>Invokes and enforces the only authority allowed to create verified state.</summary>
    /// <param name="validator">The injected live issue-policy validator.</param>
    /// <returns>A copy carrying opaque verified provenance.</returns>
    internal Waiver VerifyIssuePolicy(IWaiverIssuePolicyValidator validator)
    {
        ArgumentNullException.ThrowIfNull(validator);

        var validation = validator.Validate(DocLink)
            ?? throw IssueFailure("issue validator returned no result", detail: null);

        if (!validation.IsOpen)
        {
            throw IssueFailure("must reference an open issue", validation.Detail);
        }

        if (!validation.IsOwned)
        {
            throw IssueFailure("must reference an owned issue", validation.Detail);
        }

        if (validation.CapturedAttemptCount < 2)
        {
            throw IssueFailure("must document at least two captured attempts", validation.Detail);
        }

        if (!validation.HasAcceptanceCriteria)
        {
            throw IssueFailure("must document acceptance criteria", validation.Detail);
        }

        return this with { IssuePolicyVerified = true };
    }

    private static InvalidOperationException IssueFailure(string message, string? detail)
        => new(string.IsNullOrWhiteSpace(detail) ? message : $"{message}: {detail}");
}
