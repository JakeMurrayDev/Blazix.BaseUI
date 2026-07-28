namespace Blazix.BaseUI.Parity.Tests.Diff;

/// <summary>
/// Reports nodes one leg rendered and the other did not, and sibling lists the two legs
/// ordered differently.
/// </summary>
/// <remarks>
/// Ordering is reported because DOM sibling order is tab order and reading order: a
/// reversed dialog footer or a reordered item list is a parity break even when every node
/// is present on both legs and every attribute on them agrees.
/// </remarks>
public sealed class StructureComparator : IComparator
{
    /// <inheritdoc />
    public FindingKind Kind => FindingKind.Structure;

    /// <inheritdoc />
    public IEnumerable<Finding> Compare(ComparisonContext context)
    {
        var match = NodeMatcher.Match(context.Reference.Dom, context.Candidate.Dom);

        foreach (var node in match.ReferenceOnly)
        {
            yield return new Finding
            {
                Fixture = context.Fixture,
                Leg = context.Leg,
                Step = context.Step,
                Kind = FindingKind.Structure,
                Severity = Severity.Error,
                NodePath = node.Path,
                ReferenceValue = node.Tag,
                Message = $"React renders <{node.Tag}> at '{node.Path}'; Blazor does not."
            };
        }

        foreach (var node in match.CandidateOnly)
        {
            yield return new Finding
            {
                Fixture = context.Fixture,
                Leg = context.Leg,
                Step = context.Step,
                Kind = FindingKind.Structure,
                Severity = Severity.Error,
                NodePath = node.Path,
                CandidateValue = node.Tag,
                Message = $"Blazor renders <{node.Tag}> at '{node.Path}'; React does not."
            };
        }

        foreach (var reorder in match.Reorders)
        {
            var referenceOrder = string.Join(", ", reorder.ReferenceOrder);
            var candidateOrder = string.Join(", ", reorder.CandidateOrder);

            yield return new Finding
            {
                Fixture = context.Fixture,
                Leg = context.Leg,
                Step = context.Step,
                Kind = FindingKind.Structure,
                Severity = Severity.Error,
                NodePath = reorder.ParentPath,
                ReferenceValue = referenceOrder,
                CandidateValue = candidateOrder,
                Message =
                    $"Sibling order differs under '{reorder.ParentPath}': " +
                    $"React renders {referenceOrder}; Blazor renders {candidateOrder}."
            };
        }
    }
}
