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
    /// Half a pixel. Custom properties published by a positioner carry the same sub-pixel
    /// layout noise as the computed lengths they are derived from, and the ones that are not
    /// lengths cannot differ by less than one.
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
                        $"React {Describe(hasReference, referenceValue)}, " +
                        $"Blazor {Describe(hasCandidate, candidateValue)}." +
                        RelaxedNote(pair)
                };
            }
        }
    }

    private static string Describe(bool present, string? value)
        => present ? $"'{value}'" : "absent";

    /// <summary>
    /// Says so when the two nodes only matched on their tag, so a reader knows the two
    /// operands may not correspond. They are compared all the same: a published positioner
    /// value that differs beneath an identity difference is a finding worth having.
    /// </summary>
    private static string RelaxedNote(NodePair pair)
        => pair.Relaxed
            ? " The two nodes were paired on tag alone, so they may not be the same element."
            : string.Empty;
}
