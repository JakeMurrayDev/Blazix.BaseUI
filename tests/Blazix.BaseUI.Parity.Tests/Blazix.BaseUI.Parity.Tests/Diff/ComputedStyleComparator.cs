namespace Blazix.BaseUI.Parity.Tests.Diff;

/// <summary>
/// Reports allowlisted computed style properties that differ between nodes that matched.
/// </summary>
/// <remarks>
/// <para>
/// Each node is looked up under its own path, not the reference's: an extra wrapper on one
/// leg puts the two halves of a pair at different depths, and reading both legs at the
/// reference's path would miss the candidate's entry and report every property on it as
/// Blazor-only.
/// </para>
/// <para>
/// A node the DOM snapshot holds but the style map does not is read as a node with no
/// styles. <c>capture.js</c> writes an entry for every element it walks, so an absence is a
/// capture disagreeing with itself rather than a parity result; reporting the present leg's
/// properties as one-sided keeps that loud, where skipping the pair would drop the node's
/// styles from the run without a word. Nothing is reported when neither leg has an entry,
/// because then there is no captured value on either side to differ.
/// </para>
/// </remarks>
public sealed class ComputedStyleComparator : IComparator
{
    /// <summary>
    /// Half a pixel: above the sub-pixel layout noise that otherwise makes nearly every
    /// element differ. It reaches only the runs <see cref="ValueTolerance"/> reads as
    /// lengths, so the numbers in this allowlist that are not lengths — an
    /// <c>opacity</c>, a <c>transition-duration</c>, a <c>line-height</c>, a
    /// <c>z-index</c>, a colour channel, the scale in a <c>transform</c> — are compared
    /// exactly and are not weakened by it.
    /// A percentage counts as a length, and the epsilon is spent as half a <em>unit</em>,
    /// so a percentage gets half a percent rather than half a pixel — about a pixel and a
    /// half of a 300px box. This allowlist carries percentages the layout never measured:
    /// the colour stops of a <c>background-image</c> gradient, and the percentage a
    /// <c>flex-basis</c> keeps into its computed value. Both are authored, read from the
    /// same stylesheet by both legs, so they have nothing to differ by and the slack
    /// absorbs nothing that was ever going to be reported.
    /// Kept private rather than shared: the custom property comparator's tolerance is the
    /// same number today for its own reasons, and a shared constant would tie the two.
    /// </summary>
    private const double Epsilon = 0.5;

    private static readonly IReadOnlyDictionary<string, string> NoStyles =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <inheritdoc />
    public FindingKind Kind => FindingKind.ComputedStyle;

    /// <inheritdoc />
    public IEnumerable<Finding> Compare(ComparisonContext context)
    {
        var match = NodeMatcher.Match(context.Reference.Dom, context.Candidate.Dom);

        foreach (var pair in match.Pairs)
        {
            var reference = context.Reference.Styles.TryGetValue(pair.Reference.Path, out var left)
                ? left
                : NoStyles;
            var candidate = context.Candidate.Styles.TryGetValue(pair.Candidate.Path, out var right)
                ? right
                : NoStyles;

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
                    && ValueTolerance.Equivalent(referenceValue!, candidateValue!, Epsilon))
                {
                    continue;
                }

                yield return new Finding
                {
                    Fixture = context.Fixture,
                    Leg = context.Leg,
                    Step = context.Step,
                    Kind = FindingKind.ComputedStyle,
                    Severity = Severity.Error,
                    NodePath = pair.Reference.Path,
                    Property = name,
                    ReferenceValue = hasReference ? referenceValue : null,
                    CandidateValue = hasCandidate ? candidateValue : null,
                    Message =
                        $"Computed style '{name}' differs at '{pair.Reference.Path}': " +
                        $"React {FindingText.Describe(hasReference, referenceValue)}, " +
                        $"Blazor {FindingText.Describe(hasCandidate, candidateValue)}." +
                        FindingText.RelaxedNote(pair)
                };
            }
        }
    }
}
