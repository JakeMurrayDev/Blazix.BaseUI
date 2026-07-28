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
    /// Three things are folded away. A URL's port, because the parity server binds a free
    /// one per run, so a committed React baseline and a live Blazor run disagree about it
    /// while agreeing about everything that matters. The line and column a stack frame
    /// ends with, because those follow the bundler's output rather than the component.
    /// And an ISO-8601 instant, because Blazor's client logging stamps one on every line
    /// it writes.
    /// </para>
    /// <para>
    /// The instant matters more than its noisiness suggests. A stamped message is a
    /// different message on every attempt, so a Blazor-only error carrying one differs
    /// between a leg's two retry attempts by construction and is demoted to
    /// <see cref="Severity.Flaky"/>, which is reported and never fails — and the runs that
    /// retry are exactly the runs that log this kind of error. A waiver naming a stamped
    /// message could never match a second time either, and would land in the unused
    /// waivers instead.
    /// </para>
    /// <para>
    /// What is deliberately kept is everything else: a URL's scheme, its host and its path
    /// included. Replacing a whole URL would fold <c>/assets/logo.png</c> and
    /// <c>/assets/icon.png</c> into one message and report parity between two different
    /// failures; a comparator that over-strips passes everything and is worse than no
    /// comparator. Each rule is therefore anchored on the narrowest thing that identifies
    /// it: the port rule needs a scheme and <c>://</c> ahead of the digits, the instant
    /// rule needs a full date and a <c>T</c> ahead of the time, so that neither a bare
    /// time of day such as <c>12:30:45</c> nor a dotted version such as <c>1.2.3</c> is
    /// within reach of either.
    /// </para>
    /// <para>
    /// The level prefix the capturer writes survives, so an error on one leg and a warning
    /// with the same text on the other are two messages rather than one.
    /// </para>
    /// </remarks>
    /// <param name="message">The captured message.</param>
    /// <returns>The comparable form of the message.</returns>
    private static string Normalize(string message)
        => PositionRegex().Replace(
            PortRegex().Replace(InstantRegex().Replace(message, "<time>"), "$1:<port>"),
            "$1:<pos>");

    /// <summary>
    /// Matches an ISO-8601 instant: a calendar date, a <c>T</c>, a time, and optionally a
    /// fractional second and a <c>Z</c>.
    /// </summary>
    [GeneratedRegex(
        @"[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9]+)?Z?",
        RegexOptions.CultureInvariant)]
    private static partial Regex InstantRegex();

    /// <summary>
    /// Matches the port of a URL's authority, keeping the scheme and host that precede it.
    /// </summary>
    [GeneratedRegex(@"([a-zA-Z][a-zA-Z0-9+.\-]*://[^/?#\s]*?):[0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex PortRegex();

    /// <summary>
    /// Matches a line and column pair, but only where a dot and one to eight alphanumerics
    /// precede it. In a stack frame that is the file extension; nothing checks that it
    /// names a real one, so <c>Module.render:10:20</c> is folded as well.
    /// </summary>
    [GeneratedRegex(@"(\.[a-zA-Z][a-zA-Z0-9]{0,7}):[0-9]+:[0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex PositionRegex();
}
