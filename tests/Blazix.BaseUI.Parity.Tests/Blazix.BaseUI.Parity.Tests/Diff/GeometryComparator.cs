using System.Globalization;

namespace Blazix.BaseUI.Parity.Tests.Diff;

/// <summary>
/// Reports bounding rectangles that differ between nodes that matched.
/// </summary>
/// <remarks>
/// <para>
/// This is the one comparison that catches a popup landing in the wrong place while every
/// style on it agrees: the computed <c>top</c> and <c>left</c> of an absolutely positioned
/// element say what its own rules asked for, and the rectangle says where the browser
/// actually put it.
/// </para>
/// <para>
/// The values are numbers rather than CSS strings, so they are compared directly and the
/// value tokenizer is not involved. Each node is looked up under its own path, because an
/// extra wrapper on one leg puts the two halves of a pair at different depths. A node the
/// DOM snapshot holds but the geometry map does not is read as a node with no rectangle,
/// which reports the present leg's values one-sided rather than dropping the node.
/// </para>
/// </remarks>
public sealed class GeometryComparator : IComparator
{
    /// <summary>
    /// One pixel, twice the style tolerance. A rectangle is the accumulated result of every
    /// length above it, so its rounding error compounds where a single computed length's
    /// does not; below a pixel nothing is visible and nothing is a parity break.
    /// </summary>
    private const double Epsilon = 1.0;

    private static readonly IReadOnlyDictionary<string, double> NoBox =
        new Dictionary<string, double>(StringComparer.Ordinal);

    /// <inheritdoc />
    public FindingKind Kind => FindingKind.Geometry;

    /// <inheritdoc />
    public IEnumerable<Finding> Compare(ComparisonContext context)
    {
        var match = NodeMatcher.Match(context.Reference.Dom, context.Candidate.Dom);

        foreach (var pair in match.Pairs)
        {
            var reference = context.Reference.Geometry.TryGetValue(pair.Reference.Path, out var left)
                ? left
                : NoBox;
            var candidate = context.Candidate.Geometry.TryGetValue(pair.Candidate.Path, out var right)
                ? right
                : NoBox;

            var names = reference.Keys
                .Concat(candidate.Keys)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal);

            foreach (var name in names)
            {
                var hasReference = reference.TryGetValue(name, out var referenceValue);
                var hasCandidate = candidate.TryGetValue(name, out var candidateValue);

                if (hasReference
                    && hasCandidate
                    && Math.Abs(referenceValue - candidateValue) <= Epsilon)
                {
                    continue;
                }

                yield return new Finding
                {
                    Fixture = context.Fixture,
                    Leg = context.Leg,
                    Step = context.Step,
                    Kind = FindingKind.Geometry,
                    Severity = Severity.Error,
                    NodePath = pair.Reference.Path,
                    Property = name,
                    ReferenceValue = hasReference ? Format(referenceValue) : null,
                    CandidateValue = hasCandidate ? Format(candidateValue) : null,
                    Message =
                        $"Geometry '{name}' differs at '{pair.Reference.Path}': " +
                        $"React {Describe(hasReference, referenceValue)}, " +
                        $"Blazor {Describe(hasCandidate, candidateValue)}." +
                        RelaxedNote(pair)
                };
            }
        }
    }

    /// <summary>
    /// Renders a rectangle component invariantly, so a baseline written on one machine reads
    /// the same on another whose decimal separator differs.
    /// </summary>
    private static string Format(double value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Describe(bool present, double value)
        => present ? $"'{Format(value)}'" : "absent";

    /// <summary>
    /// Says so when the two nodes only matched on their tag. A rectangle read across such a
    /// pair is still worth reporting — it is the only thing that would name a layout
    /// difference beneath the identity difference — but the operands may not correspond.
    /// </summary>
    private static string RelaxedNote(NodePair pair)
        => pair.Relaxed
            ? " The two nodes were paired on tag alone, so they may not be the same element."
            : string.Empty;
}
