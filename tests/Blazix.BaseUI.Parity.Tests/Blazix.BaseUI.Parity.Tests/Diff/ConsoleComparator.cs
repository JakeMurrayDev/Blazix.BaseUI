using System.Text.RegularExpressions;

namespace Blazix.BaseUI.Parity.Tests.Diff;

/// <summary>
/// Reports console errors and warnings one leg produced and the other did not.
/// </summary>
/// <remarks>
/// <para>
/// The two lists are compared as multisets, not as sets. An error logged once per render
/// pass on one leg and once in total on the other is a real difference, and a set
/// comparison would erase it.
/// </para>
/// <para>
/// A message present only on the Blazor leg is an <see cref="Severity.Error"/>; one
/// present only on the React leg is <see cref="Severity.Info"/>, because React's own
/// development warnings are not Blazix's problem and must not fail a run.
/// </para>
/// </remarks>
public sealed partial class ConsoleComparator : IComparator
{
    /// <inheritdoc />
    public FindingKind Kind => FindingKind.Console;

    /// <inheritdoc />
    public IEnumerable<Finding> Compare(ComparisonContext context)
    {
        var reference = Tally(context.Reference.Console);
        var candidate = Tally(context.Candidate.Console);

        var messages = reference.Keys
            .Concat(candidate.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(message => message, StringComparer.Ordinal);

        foreach (var message in messages)
        {
            var hasReference = reference.TryGetValue(message, out var left);
            var hasCandidate = candidate.TryGetValue(message, out var right);
            var referenceCount = hasReference ? left.Count : 0;
            var candidateCount = hasCandidate ? right.Count : 0;

            if (referenceCount == candidateCount)
            {
                continue;
            }

            yield return new Finding
            {
                Fixture = context.Fixture,
                Leg = context.Leg,
                Step = context.Step,
                Kind = FindingKind.Console,
                Severity = candidateCount > referenceCount ? Severity.Error : Severity.Info,
                // The normalized text, so that a waiver can name one message rather than
                // silencing every console difference in the step.
                Property = message,
                // The raw text as each leg logged it, so the reader sees the real origin
                // and position that normalization folded away.
                ReferenceValue = hasReference ? left.Text : null,
                CandidateValue = hasCandidate ? right.Text : null,
                Message =
                    $"Console message count differs: React {referenceCount}, " +
                    $"Blazor {candidateCount}: '{message}'."
            };
        }
    }

    /// <summary>
    /// Counts one leg's messages by their normalized text, keeping the first raw text
    /// behind each count.
    /// </summary>
    /// <param name="messages">The messages captured on that leg, in the order logged.</param>
    /// <returns>The count and first raw text per normalized message.</returns>
    private static Dictionary<string, (int Count, string Text)> Tally(IReadOnlyList<string> messages)
        => messages
            .GroupBy(Normalize, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => (group.Count(), group.First()),
                StringComparer.Ordinal);

    /// <summary>
    /// Removes the parts of a message that differ for reasons other than the components.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two things are folded away. The origin a stack frame names, because the parity
    /// server binds a free port per run, so a committed React baseline and a live Blazor
    /// run disagree about it while agreeing about everything that matters. And the line
    /// and column a frame ends with, because those follow the bundler's output rather than
    /// the component.
    /// </para>
    /// <para>
    /// What is deliberately kept is everything else, the URL's path included. Replacing a
    /// whole URL would fold <c>/assets/logo.png</c> and <c>/assets/icon.png</c> into one
    /// message and report parity between two different failures; a comparator that
    /// over-strips passes everything and is worse than no comparator. For the same reason
    /// the position rule is anchored to a file extension, so that a time of day such as
    /// <c>12:30:45</c> keeps its digits.
    /// </para>
    /// <para>
    /// The level prefix the capturer writes survives, so an error on one leg and a warning
    /// with the same text on the other are two messages rather than one.
    /// </para>
    /// </remarks>
    /// <param name="message">The captured message.</param>
    /// <returns>The comparable form of the message.</returns>
    private static string Normalize(string message)
        => PositionRegex().Replace(OriginRegex().Replace(message, "<origin>"), "$1:<pos>");

    /// <summary>Matches a URL's scheme and authority, stopping at the path.</summary>
    [GeneratedRegex(@"[a-zA-Z][a-zA-Z0-9+.\-]*://[^/\s]*", RegexOptions.CultureInvariant)]
    private static partial Regex OriginRegex();

    /// <summary>
    /// Matches a line and column pair, but only where a file extension precedes it.
    /// </summary>
    [GeneratedRegex(@"(\.[a-zA-Z][a-zA-Z0-9]{0,7}):[0-9]+:[0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex PositionRegex();
}
