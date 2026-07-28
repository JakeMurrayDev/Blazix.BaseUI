namespace Blazix.BaseUI.Parity.Tests.Diff;

/// <summary>
/// Reports nodes one leg rendered and the other did not.
/// </summary>
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
    }
}
