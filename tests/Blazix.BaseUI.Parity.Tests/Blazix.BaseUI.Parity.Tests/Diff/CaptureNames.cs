namespace Blazix.BaseUI.Parity.Tests.Diff;

/// <summary>
/// The literal names <c>shared/capture.js</c> writes into a snapshot.
/// </summary>
/// <remarks>
/// These describe the capture format, not any one comparison, so they live apart from
/// the comparators. Reaching into a sibling comparator for one would couple two files
/// that are meant to be reviewed, tested, and rejected independently.
/// </remarks>
public static class CaptureNames
{
    /// <summary>
    /// The tag of the synthetic wrapper a capture with more than one root is emitted
    /// under. A capture with exactly one root is emitted as that root element itself.
    /// </summary>
    public const string RootsWrapper = "#roots";

    /// <summary>
    /// The prefix a Blazix marker attribute still carries once capture normalization has
    /// renamed every marker with an upstream counterpart.
    /// </summary>
    public const string MarkerPrefix = "data-blazix-";
}
