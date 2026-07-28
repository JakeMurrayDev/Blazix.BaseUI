namespace Blazix.BaseUI.Parity.Tests.Diff;

/// <summary>
/// Reports CSS custom properties that differ between nodes that matched.
/// </summary>
/// <remarks>
/// <para>
/// These are the values base-ui publishes for a consumer's stylesheet to read —
/// <c>--anchor-width</c>, <c>--available-width</c>, <c>--transform-origin</c>, and the
/// <c>--positioner-*</c> family — so a difference here is a difference in the contract a
/// demo's own CSS is written against, even when nothing in the computed styles moved.
/// </para>
/// <para>
/// Each node is looked up under its own path, because an extra wrapper on one leg puts the
/// two halves of a pair at different depths. A node the DOM snapshot holds but the property
/// map does not is read as a node with no custom properties: the present leg's properties
/// are then reported one-sided, which keeps a capture that disagrees with itself loud
/// instead of dropping the node silently.
/// </para>
/// </remarks>
public sealed class CustomPropertyComparator : IComparator
{
    /// <summary>
    /// Half a pixel. The lengths a positioner publishes — <c>--anchor-width</c>,
    /// <c>--available-height</c>, the <c>--positioner-*</c> family — carry the same
    /// sub-pixel layout noise as the computed lengths they are derived from, and so does
    /// the percentage in <c>--transform-origin</c>. It reaches those and nothing else:
    /// <see cref="ValueTolerance"/> compares every run that does not carry a length
    /// exactly, including the <c>rem</c> values a demo's own stylesheet declares, where
    /// half a unit would be eight pixels.
    /// </summary>
    private const double Epsilon = 0.5;

    private static readonly IReadOnlyDictionary<string, string> NoProperties =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <inheritdoc />
    public FindingKind Kind => FindingKind.CustomProperty;

    /// <inheritdoc />
    public IEnumerable<Finding> Compare(ComparisonContext context)
    {
        var match = NodeMatcher.Match(context.Reference.Dom, context.Candidate.Dom);

        foreach (var pair in match.Pairs)
        {
            var reference =
                context.Reference.CustomProps.TryGetValue(pair.Reference.Path, out var left)
                    ? left
                    : NoProperties;
            var candidate =
                context.Candidate.CustomProps.TryGetValue(pair.Candidate.Path, out var right)
                    ? right
                    : NoProperties;

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
                    Kind = FindingKind.CustomProperty,
                    Severity = Severity.Error,
                    NodePath = pair.Reference.Path,
                    Property = name,
                    ReferenceValue = hasReference ? referenceValue : null,
                    CandidateValue = hasCandidate ? candidateValue : null,
                    Message =
                        $"Custom property '{name}' differs at '{pair.Reference.Path}': " +
                        $"React {FindingText.Describe(hasReference, referenceValue)}, " +
                        $"Blazor {FindingText.Describe(hasCandidate, candidateValue)}." +
                        FindingText.RelaxedNote(pair)
                };
            }
        }
    }
}
