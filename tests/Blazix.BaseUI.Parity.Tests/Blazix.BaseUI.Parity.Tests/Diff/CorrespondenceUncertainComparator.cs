namespace Blazix.BaseUI.Parity.Tests.Diff;

/// <summary>
/// Reports fallback pairs retained to preserve descendant coverage when correspondence is
/// not strong enough for pair-dependent findings to be waiverable.
/// </summary>
public sealed class CorrespondenceUncertainComparator : IComparator
{
    /// <inheritdoc />
    public FindingKind Kind => FindingKind.CorrespondenceUncertain;

    /// <inheritdoc />
    public IEnumerable<Finding> Compare(ComparisonContext context)
    {
        var match = NodeMatcher.Match(context.Reference.Dom, context.Candidate.Dom);

        foreach (var relaxed in match.Relaxed)
        {
            yield return new Finding
            {
                Fixture = context.ExecutionId,
                Leg = context.Leg,
                Step = context.Step,
                Kind = FindingKind.CorrespondenceUncertain,
                Severity = Severity.Error,
                NodePath = relaxed.Pair.Reference.Path,
                Property = "identity",
                ReferenceValue = relaxed.ReferenceIdentity,
                CandidateValue = relaxed.CandidateIdentity,
                Message =
                    $"Node correspondence is uncertain at '{relaxed.Pair.Reference.Path}': " +
                    $"React renders {relaxed.ReferenceIdentity}; " +
                    $"Blazor renders {relaxed.CandidateIdentity}. Nothing else at that " +
                    "level matched with enough evidence, so fallback pairing was retained " +
                    "only to preserve comparison coverage beneath them."
            };
        }
    }
}
