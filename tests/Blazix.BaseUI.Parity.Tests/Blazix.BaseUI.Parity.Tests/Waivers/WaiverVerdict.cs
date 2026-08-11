using Blazix.BaseUI.Parity.Tests.Diff;

namespace Blazix.BaseUI.Parity.Tests.Waivers;

/// <summary>The reason a waiver did not become one valid consumption.</summary>
public enum WaiverDiagnosticKind
{
    /// <summary>The in-memory waiver violates the same strict policy as the JSON loader.</summary>
    Invalid,

    /// <summary>A deferred issue has not been externally confirmed open and reviewable.</summary>
    IssuePolicyUnverified,

    /// <summary>The waiver matched no eligible Error.</summary>
    Unused,

    /// <summary>The waiver was no longer valid on the review date.</summary>
    Expired,

    /// <summary>The waiver matched more than one finding or overlapped another waiver.</summary>
    Ambiguous,

    /// <summary>The waiver targeted evidence that policy never permits waiving.</summary>
    NonWaivable
}

/// <summary>One successful waiver-to-finding consumption.</summary>
/// <param name="WaiverIndex">The waiver's registry position.</param>
/// <param name="Waiver">The reviewed waiver.</param>
/// <param name="FindingIndex">The finding's production-order position.</param>
/// <param name="Finding">The consumed Error.</param>
public sealed record AppliedWaiver(
    int WaiverIndex,
    Waiver Waiver,
    int FindingIndex,
    Finding Finding);

/// <summary>One blocking waiver registry diagnostic.</summary>
/// <param name="WaiverIndex">The waiver's registry position.</param>
/// <param name="Waiver">The invalid or unmatched waiver.</param>
/// <param name="Kind">The diagnostic category.</param>
/// <param name="Message">The human explanation.</param>
public sealed record WaiverDiagnostic(
    int WaiverIndex,
    Waiver Waiver,
    WaiverDiagnosticKind Kind,
    string Message);

/// <summary>The complete result of applying one waiver registry to one finding set.</summary>
public sealed record WaiverVerdict
{
    /// <summary>Gets every finding in production order.</summary>
    public required IReadOnlyList<Finding> Findings { get; init; }

    /// <summary>Gets valid consumptions in waiver-file order.</summary>
    public required IReadOnlyList<AppliedWaiver> Applied { get; init; }

    /// <summary>Gets unwaived Error findings in production order.</summary>
    public required IReadOnlyList<Finding> BlockingFindings { get; init; }

    /// <summary>Gets the blocking Error subset whose kinds can never be waived.</summary>
    public required IReadOnlyList<Finding> NonWaivableFindings { get; init; }

    /// <summary>Gets blocking registry diagnostics in waiver-file order.</summary>
    public required IReadOnlyList<WaiverDiagnostic> Diagnostics { get; init; }

    /// <summary>Gets whether either parity evidence or waiver policy blocks the verdict.</summary>
    public bool HasBlockingEvidence => BlockingFindings.Count > 0 || Diagnostics.Count > 0;
}
