namespace Blazix.BaseUI.Parity.Tests.Diff;

/// <summary>
/// Reports a step that ended with focus somewhere other than where React left it.
/// </summary>
/// <remarks>
/// <para>
/// The two paths are compared ordinally. Focus is a keyboard contract, so a path that
/// differs only in case or only in how a character is composed is a different element,
/// and a culture-sensitive comparison would call those two paths equal.
/// </para>
/// <para>
/// A null path is what <c>capture.js</c> writes both when nothing is focused —
/// <c>document.activeElement</c> is then <c>&lt;body&gt;</c>, which no capture root
/// contains — and when focus rests on an element outside every captured root. Both mean
/// focus is not on anything this fixture rendered, which is what the message says rather
/// than claiming the document has no active element.
/// </para>
/// <para>
/// This produces a false positive where the two legs' DOM shapes differ: an extra wrapper
/// on one leg lengthens the path of everything beneath it, so focus can land on the same
/// button on both legs and still be reported. <see cref="StructureComparator"/> has
/// already reported the shape difference that causes it. Resolving it would mean pairing
/// the two nodes through <see cref="NodeMatcher"/>, which this comparator deliberately
/// does not do: <c>StepCapture.Focus</c> is one string per leg rather than a per-node map,
/// and the path it holds need not name a node either snapshot walked.
/// </para>
/// </remarks>
public sealed class FocusComparator : IComparator
{
    /// <inheritdoc />
    public FindingKind Kind => FindingKind.Focus;

    /// <inheritdoc />
    public IEnumerable<Finding> Compare(ComparisonContext context)
    {
        var reference = context.Reference.Focus;
        var candidate = context.Candidate.Focus;

        if (string.Equals(reference, candidate, StringComparison.Ordinal))
        {
            yield break;
        }

        yield return new Finding
        {
            Fixture = context.Fixture,
            Leg = context.Leg,
            Step = context.Step,
            Kind = FindingKind.Focus,
            Severity = Severity.Error,
            // The two cannot both be null here, so this names the finding by React's path
            // where there is one and by Blazor's where React focused nothing.
            NodePath = reference ?? candidate ?? string.Empty,
            ReferenceValue = reference,
            CandidateValue = candidate,
            Message = $"Focus differs: React focused {Where(reference)}; Blazor focused {Where(candidate)}."
        };
    }

    /// <summary>
    /// Names where a leg left focus.
    /// </summary>
    /// <remarks>
    /// Written here rather than taken from <see cref="FindingText"/>: that helper renders a
    /// missing value as <c>absent</c>, which for focus would read as "there is no such
    /// element" rather than "focus was not inside the fixture".
    /// </remarks>
    /// <param name="path">The captured focus path, or <see langword="null"/>.</param>
    /// <returns>The quoted path, or a phrase naming the absence.</returns>
    private static string Where(string? path)
        => path is null ? "nothing inside the captured roots" : $"'{path}'";
}
