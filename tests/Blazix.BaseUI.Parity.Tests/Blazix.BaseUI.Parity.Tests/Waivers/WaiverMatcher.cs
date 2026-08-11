using Blazix.BaseUI.Parity.Tests.Diff;

namespace Blazix.BaseUI.Parity.Tests.Waivers;

/// <summary>Applies exact, one-to-one waiver policy to parity findings.</summary>
public static class WaiverMatcher
{
    /// <summary>Matches reviewed waivers against eligible Error findings.</summary>
    /// <param name="findings">Findings in production order.</param>
    /// <param name="waivers">Waivers in file order.</param>
    /// <param name="reviewDate">The date on which expiry is evaluated.</param>
    /// <returns>The complete waiver verdict.</returns>
    public static WaiverVerdict Match(
        IReadOnlyList<Finding> findings,
        IReadOnlyList<Waiver> waivers,
        DateOnly reviewDate)
        => MatchIndexed(
            findings,
            waivers.Select((waiver, index) => new IndexedWaiver(index, waiver)).ToArray(),
            reviewDate);

    /// <summary>Matches a selected registry view without renumbering source indexes.</summary>
    internal static WaiverVerdict MatchIndexed(
        IReadOnlyList<Finding> findings,
        IReadOnlyList<IndexedWaiver> waivers,
        DateOnly reviewDate)
    {
        ArgumentNullException.ThrowIfNull(findings);
        ArgumentNullException.ThrowIfNull(waivers);

        var candidates = new Dictionary<int, IReadOnlyList<int>>();
        var diagnostics = new List<WaiverDiagnostic>();

        for (var position = 0; position < waivers.Count; position++)
        {
            var waiverIndex = waivers[position].Index;
            var waiver = waivers[position].Waiver;

            if (waiver.Expires <= reviewDate)
            {
                diagnostics.Add(new WaiverDiagnostic(
                    waiverIndex,
                    waiver,
                    WaiverDiagnosticKind.Expired,
                    $"Waiver expired on {waiver.Expires:yyyy-MM-dd}."));
                continue;
            }

            if (ComparatorRegistry.NonWaivableKinds.Contains(waiver.Kind))
            {
                diagnostics.Add(new WaiverDiagnostic(
                    waiverIndex,
                    waiver,
                    WaiverDiagnosticKind.NonWaivable,
                    $"Finding kind '{waiver.Kind}' is nonwaivable."));
                continue;
            }

            try
            {
                WaiverFile.ValidatePolicy(waiver, waiverIndex);
            }
            catch (FormatException exception)
            {
                diagnostics.Add(new WaiverDiagnostic(
                    waiverIndex,
                    waiver,
                    WaiverDiagnosticKind.Invalid,
                    exception.Message));
                continue;
            }

            if (waiver.Disposition == WaiverDisposition.DeferredDefect &&
                waiver.IssuePolicyStatus != WaiverIssuePolicyStatus.Verified)
            {
                diagnostics.Add(new WaiverDiagnostic(
                    waiverIndex,
                    waiver,
                    WaiverDiagnosticKind.IssuePolicyUnverified,
                    "Deferred issue policy was not externally verified."));
                continue;
            }

            var matches = Enumerable.Range(0, findings.Count)
                .Where(index => findings[index].Severity == Severity.Error)
                .Where(index => !ComparatorRegistry.NonWaivableKinds.Contains(findings[index].Kind))
                .Where(index => Matches(waiver, findings[index]))
                .ToArray();

            candidates[waiverIndex] = matches;
        }

        var singleMatches = candidates
            .Where(item => item.Value.Count == 1)
            .GroupBy(item => item.Value[0])
            .ToDictionary(group => group.Key, group => group.Select(item => item.Key).ToArray());
        var applied = new List<AppliedWaiver>();
        var consumed = new HashSet<int>();

        for (var position = 0; position < waivers.Count; position++)
        {
            var waiverIndex = waivers[position].Index;
            if (!candidates.TryGetValue(waiverIndex, out var matches))
            {
                continue;
            }

            var waiver = waivers[position].Waiver;
            if (matches.Count == 0)
            {
                diagnostics.Add(new WaiverDiagnostic(
                    waiverIndex,
                    waiver,
                    WaiverDiagnosticKind.Unused,
                    "Waiver matched no eligible Error finding."));
                continue;
            }

            if (matches.Count > 1)
            {
                diagnostics.Add(new WaiverDiagnostic(
                    waiverIndex,
                    waiver,
                    WaiverDiagnosticKind.Ambiguous,
                    $"Waiver matched {matches.Count} Error findings."));
                continue;
            }

            var findingIndex = matches[0];
            if (singleMatches[findingIndex].Length > 1)
            {
                diagnostics.Add(new WaiverDiagnostic(
                    waiverIndex,
                    waiver,
                    WaiverDiagnosticKind.Ambiguous,
                    "Waiver overlaps another waiver on the same Error finding."));
                continue;
            }

            consumed.Add(findingIndex);
            applied.Add(new AppliedWaiver(
                waiverIndex,
                waiver,
                findingIndex,
                findings[findingIndex]));
        }

        var blocking = findings
            .Select((finding, index) => new { Finding = finding, Index = index })
            .Where(item => item.Finding.Severity == Severity.Error && !consumed.Contains(item.Index))
            .Select(item => item.Finding)
            .ToArray();

        return new WaiverVerdict
        {
            Findings = [.. findings],
            Applied = applied,
            BlockingFindings = blocking,
            NonWaivableFindings =
            [
                .. blocking.Where(finding =>
                    ComparatorRegistry.NonWaivableKinds.Contains(finding.Kind))
            ],
            Diagnostics = diagnostics.OrderBy(item => item.WaiverIndex).ToArray()
        };
    }

    internal sealed record IndexedWaiver(int Index, Waiver Waiver);

    private static bool Matches(Waiver waiver, Finding finding)
        => waiver.Leg == finding.Leg &&
           waiver.Kind == finding.Kind &&
           string.Equals(waiver.Fixture, finding.Fixture, StringComparison.Ordinal) &&
           string.Equals(waiver.Step, finding.Step, StringComparison.Ordinal) &&
           string.Equals(waiver.NodePath, finding.NodePath, StringComparison.Ordinal) &&
           (waiver.PropertyMatch == WaiverPropertyMatch.Exact
               ? string.Equals(waiver.Property, finding.Property, StringComparison.Ordinal)
               : finding.Property.StartsWith(waiver.Property, StringComparison.Ordinal));
}
