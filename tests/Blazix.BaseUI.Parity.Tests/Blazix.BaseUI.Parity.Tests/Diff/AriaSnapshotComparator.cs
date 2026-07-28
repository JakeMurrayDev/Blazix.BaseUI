namespace Blazix.BaseUI.Parity.Tests.Diff;

/// <summary>
/// Reports the two legs' ARIA snapshots differing, as one unified diff per step.
/// </summary>
/// <remarks>
/// <para>
/// One finding per step rather than one per differing line: the snapshot is a tree, so a
/// single wrong role moves every line beneath it, and a finding per line would report one
/// defect dozens of times and drown the step it belongs to.
/// </para>
/// <para>
/// A snapshot one leg captured and the other did not is reported rather than skipped. The
/// capturer takes both from the same Playwright call, so an empty one is a capture that
/// failed on that leg, which is a result worth failing on and not a no-op.
/// </para>
/// </remarks>
public sealed class AriaSnapshotComparator : IComparator
{
    /// <summary>
    /// The lines of unchanged snapshot printed either side of a change, as a unified diff
    /// conventionally prints.
    /// </summary>
    private const int ContextLines = 3;

    /// <summary>
    /// How many diff lines a message carries before the rest are counted instead of
    /// printed. Two snapshots with nothing in common produce a diff as long as both of
    /// them put together, which is unreadable in a report and says no more than its first
    /// few lines and its totals do.
    /// </summary>
    private const int MaxDiffLines = 40;

    /// <inheritdoc />
    public FindingKind Kind => FindingKind.AriaSnapshot;

    /// <inheritdoc />
    public IEnumerable<Finding> Compare(ComparisonContext context)
    {
        // Line endings and a trailing newline are not accessibility differences. Left in,
        // they would put a diff on every step of every fixture.
        var referenceText = Normalize(context.Reference.Aria);
        var candidateText = Normalize(context.Candidate.Aria);

        // The common case in a passing run, and the one case that need not be aligned.
        if (string.Equals(referenceText, candidateText, StringComparison.Ordinal))
        {
            yield break;
        }

        var reference = Lines(referenceText);
        var candidate = Lines(candidateText);
        var ops = Align(reference, candidate);
        var added = ops.Count(op => op.Marker == '+');
        var removed = ops.Count(op => op.Marker == '-');

        List<string> message =
        [
            Headline(reference.Count, candidate.Count, added, removed),
            .. Render(ops)
        ];

        yield return new Finding
        {
            Fixture = context.Fixture,
            Leg = context.Leg,
            Step = context.Step,
            Kind = FindingKind.AriaSnapshot,
            Severity = Severity.Error,
            // Both snapshots whole, because the diff in the message is the readable form
            // and this is the evidence behind it.
            ReferenceValue = context.Reference.Aria,
            CandidateValue = context.Candidate.Aria,
            Message = string.Join('\n', message)
        };
    }

    /// <summary>Settles how a snapshot's lines are separated and where it ends.</summary>
    /// <param name="aria">The captured snapshot.</param>
    /// <returns>The comparable text.</returns>
    private static string Normalize(string aria)
        => aria.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd('\n');

    /// <summary>Splits a normalized snapshot into comparable lines.</summary>
    /// <param name="aria">The normalized snapshot.</param>
    /// <returns>The lines, or none at all when the leg captured nothing.</returns>
    private static IReadOnlyList<string> Lines(string aria)
        // Splitting an empty snapshot would otherwise yield one empty line, which would
        // diff against a real first line and read as a difference in content.
        => aria.Length == 0 ? [] : aria.Split('\n');

    /// <summary>States what the diff below it amounts to.</summary>
    /// <param name="reference">How many lines React captured.</param>
    /// <param name="candidate">How many lines Blazor captured.</param>
    /// <param name="added">How many lines only Blazor has.</param>
    /// <param name="removed">How many lines only React has.</param>
    /// <returns>The first line of the message.</returns>
    private static string Headline(int reference, int candidate, int added, int removed)
    {
        if (reference == 0)
        {
            return $"React captured no ARIA snapshot; Blazor captured {Count(candidate)}.";
        }

        if (candidate == 0)
        {
            return $"Blazor captured no ARIA snapshot; React captured {Count(reference)}.";
        }

        // Counted before the diff is capped, so truncation cannot hide the scale.
        return $"ARIA snapshot differs: {Count(added)} added, {Count(removed)} removed.";
    }

    /// <summary>Counts lines without writing "1 lines".</summary>
    /// <param name="lines">The number of lines.</param>
    /// <returns>The counted phrase.</returns>
    private static string Count(int lines) => lines == 1 ? "1 line" : $"{lines} lines";

    /// <summary>
    /// Aligns the two snapshots on their longest common subsequence.
    /// </summary>
    /// <remarks>
    /// A removal is preferred to an addition wherever the two cost the same, which is what
    /// puts the old line above the new one in a changed block, as a unified diff prints it.
    /// </remarks>
    /// <param name="reference">React's lines.</param>
    /// <param name="candidate">Blazor's lines.</param>
    /// <returns>
    /// The ordered edits, each carrying its 1-based line number on the sides it belongs to
    /// and 0 on the side it does not.
    /// </returns>
    private static List<(char Marker, string Text, int Reference, int Candidate)> Align(
        IReadOnlyList<string> reference, IReadOnlyList<string> candidate)
    {
        var common = new int[reference.Count + 1, candidate.Count + 1];

        for (var i = reference.Count - 1; i >= 0; i--)
        {
            for (var j = candidate.Count - 1; j >= 0; j--)
            {
                common[i, j] = string.Equals(reference[i], candidate[j], StringComparison.Ordinal)
                    ? common[i + 1, j + 1] + 1
                    : Math.Max(common[i + 1, j], common[i, j + 1]);
            }
        }

        var ops = new List<(char, string, int, int)>();
        var x = 0;
        var y = 0;

        while (x < reference.Count && y < candidate.Count)
        {
            if (string.Equals(reference[x], candidate[y], StringComparison.Ordinal))
            {
                ops.Add((' ', reference[x], x + 1, y + 1));
                x++;
                y++;
            }
            else if (common[x + 1, y] >= common[x, y + 1])
            {
                ops.Add(('-', reference[x], x + 1, 0));
                x++;
            }
            else
            {
                ops.Add(('+', candidate[y], 0, y + 1));
                y++;
            }
        }

        for (; x < reference.Count; x++)
        {
            ops.Add(('-', reference[x], x + 1, 0));
        }

        for (; y < candidate.Count; y++)
        {
            ops.Add(('+', candidate[y], 0, y + 1));
        }

        return ops;
    }

    /// <summary>
    /// Renders the edits as unified diff hunks.
    /// </summary>
    /// <remarks>
    /// Two changes with no more than twice the context between them share one hunk, since
    /// separate hunks would print the lines between them twice.
    /// </remarks>
    /// <param name="ops">The aligned edits.</param>
    /// <returns>The hunk headers and body lines, capped.</returns>
    private static List<string> Render(
        IReadOnlyList<(char Marker, string Text, int Reference, int Candidate)> ops)
    {
        var changes = Enumerable.Range(0, ops.Count).Where(i => ops[i].Marker != ' ').ToList();
        var lines = new List<string>();

        var first = 0;
        while (first < changes.Count)
        {
            var last = first;
            while (last + 1 < changes.Count
                   && changes[last + 1] - changes[last] - 1 <= 2 * ContextLines)
            {
                last++;
            }

            // Clamped at both ends, so a change on the first or last line prints whatever
            // context exists rather than running off the snapshot.
            var from = Math.Max(0, changes[first] - ContextLines);
            var to = Math.Min(ops.Count - 1, changes[last] + ContextLines);

            lines.Add(Header(ops, from, to));

            for (var i = from; i <= to; i++)
            {
                lines.Add($"{ops[i].Marker}{ops[i].Text}");
            }

            first = last + 1;
        }

        return lines.Count <= MaxDiffLines
            ? lines
            : [.. lines.Take(MaxDiffLines), $"... {Count(lines.Count - MaxDiffLines)} of the diff omitted."];
    }

    /// <summary>
    /// Writes the <c>@@</c> header naming where the hunk sits in each snapshot.
    /// </summary>
    /// <param name="ops">The aligned edits.</param>
    /// <param name="from">The first edit in the hunk.</param>
    /// <param name="to">The last edit in the hunk.</param>
    /// <returns>The header line.</returns>
    private static string Header(
        IReadOnlyList<(char Marker, string Text, int Reference, int Candidate)> ops,
        int from,
        int to)
    {
        var referenceStart = 0;
        var referenceCount = 0;
        var candidateStart = 0;
        var candidateCount = 0;

        for (var i = from; i <= to; i++)
        {
            if (ops[i].Reference > 0)
            {
                referenceStart = referenceCount == 0 ? ops[i].Reference : referenceStart;
                referenceCount++;
            }

            if (ops[i].Candidate > 0)
            {
                candidateStart = candidateCount == 0 ? ops[i].Candidate : candidateStart;
                candidateCount++;
            }
        }

        // A side the hunk touches no line of starts at 0, which is how a unified diff
        // writes a file that has no such lines at all.
        return $"@@ -{referenceStart},{referenceCount} +{candidateStart},{candidateCount} @@";
    }
}
