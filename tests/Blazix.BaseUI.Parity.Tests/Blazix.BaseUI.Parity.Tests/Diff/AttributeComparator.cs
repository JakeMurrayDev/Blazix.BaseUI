using Blazix.BaseUI.Parity.Tests.Capture;

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
    private readonly IReadOnlyDictionary<string, string> markers = MarkerCatalog.Load();

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
                // unexplained error. Capture normalization strips the Blazix prefix from
                // every marker that carries it, so the prefix alone no longer identifies
                // the set: a listed name is skipped whatever it is spelled. An unlisted
                // upstream-spelled name is nobody's marker and is reported here.
                .Where(name => !markers.ContainsKey(name)
                    && !name.StartsWith(CaptureNames.MarkerPrefix, StringComparison.Ordinal))
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
                        $"React {FindingText.Describe(hasReference, referenceValue)}, " +
                        $"Blazor {FindingText.Describe(hasCandidate, candidateValue)}."
                };
            }
        }
    }
}
