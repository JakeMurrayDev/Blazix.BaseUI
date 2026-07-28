namespace Blazix.BaseUI.Parity.Tests.Capture;

/// <summary>
/// Names that describe the shape of a snapshot rather than any one comparison.
/// </summary>
/// <remarks>
/// Not every name here is written by <c>shared/capture.js</c> verbatim — see
/// <see cref="MarkerPrefix"/>. They belong to the capture format all the same, so they sit
/// beside <see cref="DomNode"/> rather than in <c>Diff/</c>: the capturer and its own tests
/// need them as much as the comparators do, and a capture-layer file reaching into a
/// comparator for a capture-format constant would have the dependency backwards.
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
