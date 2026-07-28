namespace Blazix.BaseUI.Parity.Tests.Diff;

/// <summary>
/// Reports attribute differences between nodes that matched.
/// </summary>
/// <remarks>
/// Only matched pairs are compared. An unmatched node's attributes say nothing a reader
/// can act on beyond the structural finding that already covers the node itself.
/// </remarks>
public sealed class AttributeComparator : IComparator
{
    /// <inheritdoc />
    public FindingKind Kind => FindingKind.Attribute;

    /// <inheritdoc />
    public IEnumerable<Finding> Compare(ComparisonContext context)
    {
        var match = NodeMatcher.Match(context.Reference.Dom, context.Candidate.Dom);

        foreach (var pair in match.Pairs)
        {
            var names = pair.Reference.Attributes.Keys
                .Concat(pair.Candidate.Attributes.Keys)
                // Blazix markers belong to MarkerComparator, which classifies them
                // against manifest/markers.json. Reporting them here as well would both
                // duplicate every marker and override an Info classification with an
                // unexplained error.
                .Where(name => !name.StartsWith(MarkerComparator.MarkerPrefix, StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal);

            foreach (var name in names)
            {
                var hasReference = pair.Reference.Attributes.TryGetValue(name, out var referenceValue);
                var hasCandidate = pair.Candidate.Attributes.TryGetValue(name, out var candidateValue);

                if (hasReference && hasCandidate && referenceValue == candidateValue)
                {
                    continue;
                }

                yield return new Finding
                {
                    Fixture = context.Fixture,
                    Leg = context.Leg,
                    Step = context.Step,
                    Kind = FindingKind.Attribute,
                    Severity = Severity.Error,
                    NodePath = pair.Reference.Path,
                    Property = name,
                    ReferenceValue = hasReference ? referenceValue : null,
                    CandidateValue = hasCandidate ? candidateValue : null,
                    Message =
                        $"Attribute '{name}' differs at '{pair.Reference.Path}': " +
                        $"React {Describe(hasReference, referenceValue)}, " +
                        $"Blazor {Describe(hasCandidate, candidateValue)}."
                };
            }
        }
    }

    private static string Describe(bool present, string? value)
        => present ? $"'{value}'" : "absent";
}
