using Blazix.BaseUI.Parity.Tests.Capture;

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

        // A leftover on each side at the same stable path is one position both legs render,
        // left unpaired because their identities disagree or because an ancestor did not
        // pair. Reporting it as "React renders <li> here; Blazor does not" *and* as its
        // mirror is two statements that are each false and that contradict each other. What
        // each leg renders at that path is knowable from these two lists alone, so it is
        // said instead. The pairing is untouched: role is in the key on purpose, so a role
        // difference is meant to surface structurally — only the wording was ever wrong.
        var counterparts = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        for (var i = 0; i < match.CandidateOnly.Count; i++)
        {
            var node = match.CandidateOnly[i];
            if (!counterparts.TryGetValue(node.Path, out var indices))
            {
                indices = [];
                counterparts[node.Path] = indices;
            }

            indices.Add(i);
        }

        var reconciled = new bool[match.CandidateOnly.Count];

        foreach (var node in match.ReferenceOnly)
        {
            if (counterparts.TryGetValue(node.Path, out var indices) && indices.Count > 0)
            {
                var counterpart = match.CandidateOnly[indices[0]];
                reconciled[indices[0]] = true;
                indices.RemoveAt(0);

                var referenceIdentity = NodeMatcher.Identity(node);
                var candidateIdentity = NodeMatcher.Identity(counterpart);

                yield return new Finding
                {
                    Fixture = context.ExecutionId,
                    Leg = context.Leg,
                    Step = context.Step,
                    Kind = FindingKind.Structure,
                    Severity = Severity.Error,
                    NodePath = node.Path,
                    Property = "identity",
                    ReferenceValue = referenceIdentity,
                    CandidateValue = candidateIdentity,
                    Message =
                        $"React renders {referenceIdentity} at '{node.Path}'; " +
                        $"Blazor renders {candidateIdentity} there. The two did not pair, " +
                        "so nothing beneath them was compared."
                };

                continue;
            }

            yield return new Finding
            {
                Fixture = context.ExecutionId,
                Leg = context.Leg,
                Step = context.Step,
                Kind = FindingKind.Structure,
                Severity = Severity.Error,
                NodePath = node.Path,
                Property = "presence",
                ReferenceValue = node.Tag,
                Message = $"React renders <{node.Tag}> at '{node.Path}'; Blazor does not."
            };
        }

        for (var i = 0; i < match.CandidateOnly.Count; i++)
        {
            if (reconciled[i])
            {
                continue;
            }

            var node = match.CandidateOnly[i];

            yield return new Finding
            {
                Fixture = context.ExecutionId,
                Leg = context.Leg,
                Step = context.Step,
                Kind = FindingKind.Structure,
                Severity = Severity.Error,
                NodePath = node.Path,
                Property = "presence",
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
                Fixture = context.ExecutionId,
                Leg = context.Leg,
                Step = context.Step,
                Kind = FindingKind.Structure,
                Severity = Severity.Error,
                NodePath = reorder.ParentPath,
                Property = "sibling-order",
                ReferenceValue = referenceOrder,
                CandidateValue = candidateOrder,
                Message =
                    $"Sibling order differs under '{reorder.ParentPath}': " +
                    $"React renders {referenceOrder}; Blazor renders {candidateOrder}."
            };
        }
    }
}
