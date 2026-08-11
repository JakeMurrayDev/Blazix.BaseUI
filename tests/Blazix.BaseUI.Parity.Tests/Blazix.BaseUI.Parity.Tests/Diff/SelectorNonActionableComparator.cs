using System.Globalization;

namespace Blazix.BaseUI.Parity.Tests.Diff;

/// <summary>
/// Reports expanded step selectors whose attached-but-undriveable occurrence count differs
/// between legs.
/// </summary>
/// <remarks>
/// Actionability is intentionally separate from resolution. An attached element that is covered,
/// zero-size, or otherwise cannot be driven is different evidence from an element that is absent,
/// even when the same expanded selector identifies both failures.
/// </remarks>
public sealed class SelectorNonActionableComparator : IComparator
{
    /// <inheritdoc />
    public FindingKind Kind => FindingKind.SelectorNonActionable;

    /// <inheritdoc />
    public IEnumerable<Finding> Compare(ComparisonContext context)
    {
        var reference = Tally(context.Reference.NonActionableSelectors);
        var candidate = Tally(context.Candidate.NonActionableSelectors);

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
                Kind = FindingKind.SelectorNonActionable,
                Severity = Severity.Error,
                Property = selector,
                ReferenceValue = referenceCount.ToString(CultureInfo.InvariantCulture),
                CandidateValue = candidateCount.ToString(CultureInfo.InvariantCulture),
                Message =
                    $"Non-actionable selector count differs for '{selector}': " +
                    $"React {referenceCount}, Blazor {candidateCount}."
            };
        }
    }

    private static Dictionary<string, int> Tally(IReadOnlyList<string> selectors)
        => selectors
            .GroupBy(selector => selector, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
}
