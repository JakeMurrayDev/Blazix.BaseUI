using System.Globalization;

namespace Blazix.BaseUI.Parity.Tests.Diff;

/// <summary>
/// Reports expanded step selectors whose absent-element occurrence count differs between legs.
/// </summary>
/// <remarks>
/// The lists are ordinal multisets rather than sets. Repeating the same failed selector for two
/// actions is different evidence from failing it once, and a selector whose count falls from one
/// to zero must be reported just as one whose count rises from zero to one.
/// </remarks>
public sealed class SelectorUnresolvedComparator : IComparator
{
    /// <inheritdoc />
    public FindingKind Kind => FindingKind.SelectorUnresolved;

    /// <inheritdoc />
    public IEnumerable<Finding> Compare(ComparisonContext context)
    {
        var reference = Tally(context.Reference.UnresolvedSelectors);
        var candidate = Tally(context.Candidate.UnresolvedSelectors);

        var selectors = reference.Keys
            .Concat(candidate.Keys)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal);

        foreach (var selector in selectors)
        {
            var referenceCount = reference.GetValueOrDefault(selector);
            var candidateCount = candidate.GetValueOrDefault(selector);

            if (referenceCount == candidateCount)
            {
                continue;
            }

            yield return new Finding
            {
                Fixture = context.ExecutionId,
                Leg = context.Leg,
                Step = context.Step,
                Kind = FindingKind.SelectorUnresolved,
                Severity = Severity.Error,
                Property = selector,
                ReferenceValue = referenceCount.ToString(CultureInfo.InvariantCulture),
                CandidateValue = candidateCount.ToString(CultureInfo.InvariantCulture),
                Message =
                    $"Unresolved selector count differs for '{selector}': " +
                    $"React {referenceCount}, Blazor {candidateCount}."
            };
        }
    }

    private static Dictionary<string, int> Tally(IReadOnlyList<string> selectors)
        => selectors
            .GroupBy(selector => selector, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
}
