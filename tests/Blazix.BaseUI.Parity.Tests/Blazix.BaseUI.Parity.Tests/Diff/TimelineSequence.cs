using Blazix.BaseUI.Parity.Tests.Capture;

namespace Blazix.BaseUI.Parity.Tests.Diff;

/// <summary>
/// Reduces a recorded timeline to the ordered signature the two legs are compared on.
/// </summary>
/// <remarks>
/// <para>
/// The timestamps are dropped outright. A Blazor Server leg runs every state change through
/// a circuit round trip, so it reaches each phase later than React does and, on a loaded
/// machine, by a different amount each run. What has to match is the order of the phases,
/// not when they happened; how long a run took is compared separately, against the duration
/// that leg's own CSS declares.
/// </para>
/// <para>
/// The attribute allowlist is applied to attribute mutations and to nothing else. Applied to
/// every event it would drop the transition and lifecycle events too — their <c>attr</c> is
/// null or a CSS property name rather than an attribute — and the signature of an animation
/// would be empty, which is the one shape that makes every timeline compare equal.
/// </para>
/// </remarks>
public static class TimelineSequence
{
    /// <summary>
    /// The attribute mutations that carry a phase. Every other attribute a component writes
    /// during an animation — an <c>aria-*</c> flip, an id, a class rewrite — is noise that
    /// would swamp the phases and differ between the two legs for reasons that are not
    /// animation.
    /// </summary>
    private static readonly string[] TrackedAttributes =
    [
        "data-open", "data-closed", "data-starting-style", "data-ending-style",
        "data-side", "data-align", "hidden", "inert", "style"
    ];

    /// <summary>The kind whose <c>attr</c> is an attribute name.</summary>
    private const string AttributeKind = "attribute";

    /// <summary>
    /// The kind whose identity lives in <c>from</c>: <c>capture.js</c> cannot compute a path
    /// for a node that has already left the tree, so it records the departing tag instead.
    /// </summary>
    private const string RemovedKind = "removed";

    /// <summary>
    /// How a value that was not there at all is written, so that it cannot be read as an
    /// empty one. A marker set to the empty string and the same marker removed are the two
    /// halves of an open/close pair, and both write nothing into a signature that renders
    /// them the same way.
    /// </summary>
    private const string Absent = "<absent>";

    /// <summary>
    /// Reduces a timeline to its comparable signature.
    /// </summary>
    /// <param name="timeline">The events recorded for one step on one leg.</param>
    /// <returns>
    /// One signature per surviving event, in the order they were recorded.
    /// </returns>
    public static IReadOnlyList<string> Normalize(IReadOnlyList<TimelineEvent> timeline)
    {
        var signatures = new List<string>(timeline.Count);

        foreach (var recorded in timeline)
        {
            var isAttribute = string.Equals(recorded.Kind, AttributeKind, StringComparison.Ordinal);

            if (isAttribute && !TrackedAttributes.Contains(recorded.Attr, StringComparer.Ordinal))
            {
                continue;
            }

            var signature = Signature(recorded);

            // Consecutive duplicates are collapsed for attribute writes only. The same value
            // written to the same attribute twice in a row is a redundant write and not a
            // phase, whereas every removal in a step normalizes to the same signature — a
            // removal carries no path — so collapsing those would report parity between a
            // dialog that unmounted two nodes and one that unmounted one.
            if (isAttribute
                && signatures.Count > 0
                && string.Equals(signatures[^1], signature, StringComparison.Ordinal))
            {
                continue;
            }

            signatures.Add(signature);
        }

        return signatures;
    }

    /// <summary>
    /// Writes one event as the text it is compared and diffed as.
    /// </summary>
    /// <param name="recorded">The event.</param>
    /// <returns>The signature.</returns>
    private static string Signature(TimelineEvent recorded)
        => $"{recorded.Kind}:{recorded.Path}:{recorded.Attr}:{Identity(recorded)}";

    /// <summary>
    /// Reads the value that says which node or state an event is about.
    /// </summary>
    /// <remarks>
    /// It is <c>to</c> for every kind but a removal, which has no <c>to</c> and no path: its
    /// only identity is the tag <c>capture.js</c> writes into <c>from</c>. Without that a
    /// dialog dropping its backdrop and one dropping its popup normalize identically.
    /// </remarks>
    /// <param name="recorded">The event.</param>
    /// <returns>The identity, or <see cref="Absent"/> when the event carries none.</returns>
    private static string Identity(TimelineEvent recorded)
        => (string.Equals(recorded.Kind, RemovedKind, StringComparison.Ordinal)
            ? recorded.From
            : recorded.To) ?? Absent;
}
