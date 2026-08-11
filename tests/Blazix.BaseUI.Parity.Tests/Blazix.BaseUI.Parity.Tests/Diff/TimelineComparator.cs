using System.Globalization;
using Blazix.BaseUI.Parity.Tests.Capture;

namespace Blazix.BaseUI.Parity.Tests.Diff;

/// <summary>
/// Compares normalized timeline order, run-owned phase obligations, and measured run duration.
/// </summary>
public sealed class TimelineComparator : IComparator
{
    private const string AttributeKind = "attribute";
    private const string AddedKind = "added";
    private const string RemovedKind = "removed";

    private static readonly RunFamily[] Families =
    [
        new(
            "transition",
            ["transitionstart"],
            ["transitionend", "transitioncancel"],
            "transition-duration",
            "transition-delay",
            "transition-property",
            IterationProperty: null),
        new(
            "animation",
            ["animationstart"],
            ["animationend", "animationcancel"],
            "animation-duration",
            "animation-delay",
            "animation-name",
            "animation-iteration-count")
    ];

    private static readonly string[] CancelKinds = ["transitioncancel", "animationcancel"];

    private static readonly int MaxDiffLines = checked((int)ComparatorContract.Value(
        FindingKind.Timeline,
        ComparatorContract.MaximumDiffLines));

    private static readonly double ToleranceFloorMs = ComparatorContract.Value(
        FindingKind.Timeline,
        ComparatorContract.DurationToleranceFloor);

    private static readonly double RelativeTolerance = ComparatorContract.Value(
        FindingKind.Timeline,
        ComparatorContract.DurationRelativeTolerance);

    private static readonly string[] InvariantNames =
    [
        "mounted-before-transition-start",
        "present-at-transitionend",
        "data-open-flipped-before-starting-style-cleared",
        "removed-after-transitionend"
    ];

    private static readonly string[] InvariantNotes =
    [
        string.Empty,
        " A run that started never reached a terminal event and no removal could be " +
        "attributed to the node, so the step may simply have been captured while it was " +
        "still going.",
        string.Empty,
        " A removal event carries no node path — only the tag of the node that left — so " +
        "the removal could not be attributed to this node."
    ];

    private enum PhaseState
    {
        Satisfied,
        Violated,
        Unknown
    }

    private enum RemovalKind
    {
        None,
        Ambiguous,
        Attributed
    }

    /// <inheritdoc />
    public FindingKind Kind => FindingKind.Timeline;

    /// <inheritdoc />
    public IEnumerable<Finding> Compare(ComparisonContext context)
    {
        // Presentation order is contractual: the sequence is the primary evidence and must
        // precede the run-owned interpretation whenever both report the same defect.
        foreach (var finding in CompareSequences(context))
        {
            yield return finding;
        }

        var reference = Read(context.Reference);
        var candidate = Read(context.Candidate);

        foreach (var path in reference.Animating.Intersect(candidate.Animating, StringComparer.Ordinal))
        {
            var referenceRuns = Runs(reference, path);
            var candidateRuns = Runs(candidate, path);

            foreach (var finding in ComparePhases(
                         context, reference, candidate, referenceRuns, candidateRuns, path))
            {
                yield return finding;
            }

            foreach (var finding in CompareSpans(
                         context, referenceRuns, candidateRuns, path))
            {
                yield return finding;
            }
        }
    }

    private static IEnumerable<Finding> CompareSequences(ComparisonContext context)
    {
        var reference = TimelineSequence.Normalize(context.Reference.Timeline);
        var candidate = TimelineSequence.Normalize(context.Candidate.Timeline);

        if (reference.SequenceEqual(candidate, StringComparer.Ordinal))
        {
            yield break;
        }

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
            Fixture = context.ExecutionId,
            Leg = context.Leg,
            Step = context.Step,
            Kind = FindingKind.Timeline,
            Severity = Severity.Error,
            ReferenceValue = string.Join('\n', reference),
            CandidateValue = string.Join('\n', candidate),
            Message = string.Join('\n', message)
        };
    }

    private static IEnumerable<Finding> ComparePhases(
        ComparisonContext context,
        Leg reference,
        Leg candidate,
        IReadOnlyList<Run> referenceRuns,
        IReadOnlyList<Run> candidateRuns,
        string path)
    {
        var candidateByIdentity = candidateRuns.ToDictionary(run => run.Identity);

        foreach (var referenceRun in referenceRuns)
        {
            if (!SameGroupCardinality(referenceRun.Identity, referenceRuns, candidateRuns) ||
                !candidateByIdentity.TryGetValue(referenceRun.Identity, out var candidateRun))
            {
                // A missing run is already exact L1 evidence. There is no opposite run whose
                // phase obligations can truthfully be compared.
                continue;
            }

            var left = Evaluate(reference, path, referenceRun);
            var right = Evaluate(candidate, path, candidateRun);

            for (var index = 0; index < InvariantNames.Length; index++)
            {
                if (left[index] == right[index])
                {
                    continue;
                }

                var invariant = InvariantNames[index];
                var undecided = left[index] == PhaseState.Unknown || right[index] == PhaseState.Unknown;

                yield return new Finding
                {
                    Fixture = context.ExecutionId,
                    Leg = context.Leg,
                    Step = context.Step,
                    Kind = FindingKind.Timeline,
                    Severity = undecided ? Severity.Info : Severity.Error,
                    NodePath = path,
                    Property = $"{invariant}@{referenceRun.Identity}",
                    ReferenceValue = Value(left[index]),
                    CandidateValue = Value(right[index]),
                    Message = undecided
                        ? $"Animation invariant '{invariant}' was not decided at '{path}': " +
                          $"React {Describe(left[index])}; Blazor {Describe(right[index])}." +
                          InvariantNotes[index]
                        : $"Animation invariant '{invariant}' differs at '{path}': " +
                          $"React {Describe(left[index])}; Blazor {Describe(right[index])}."
                };
            }
        }
    }

    private static PhaseState[] Evaluate(Leg leg, string path, Run run)
    {
        var windowEnd = run.WindowEndIndex ?? leg.Timeline.Count;
        var firstAdded = IndexOf(
            leg.Timeline,
            run.WindowStartIndex,
            windowEnd,
            item => On(item, path) && item.Kind == AddedKind);
        var mounted = firstAdded > run.StartIndex
            ? PhaseState.Violated
            : PhaseState.Satisfied;

        var removal = RunRemoval(leg, path, run);
        var present = run.TerminalIndex is not null
            ? PhaseState.Satisfied
            : removal.Kind == RemovalKind.Attributed
                ? PhaseState.Violated
                : PhaseState.Unknown;

        var flipped = IndexOf(
            leg.Timeline,
            run.WindowStartIndex,
            windowEnd,
            item => On(item, path) &&
                    item.Kind == AttributeKind &&
                    item.Attr == "data-open" &&
                    !string.Equals(item.From, item.To, StringComparison.Ordinal));
        var cleared = IndexOf(
            leg.Timeline,
            run.WindowStartIndex,
            windowEnd,
            item => On(item, path) &&
                    item.Kind == AttributeKind &&
                    item.Attr == "data-starting-style" &&
                    item.To is null);
        var ordering = flipped >= 0 && cleared >= 0 && cleared < flipped
            ? PhaseState.Violated
            : PhaseState.Satisfied;

        var unmounted = removal.Kind switch
        {
            RemovalKind.None => PhaseState.Satisfied,
            RemovalKind.Ambiguous => PhaseState.Unknown,
            _ => run.TerminalIndex is { } terminal && removal.Index > terminal
                ? PhaseState.Satisfied
                : PhaseState.Violated
        };

        return [mounted, present, ordering, unmounted];
    }

    private static Removal RunRemoval(Leg leg, string path, Run run)
        => AttributeRemoval(
            leg,
            path,
            run.WindowStartIndex,
            run.WindowEndIndex ?? leg.Timeline.Count);

    private static IEnumerable<Finding> CompareSpans(
        ComparisonContext context,
        IReadOnlyList<Run> referenceRuns,
        IReadOnlyList<Run> candidateRuns,
        string path)
    {
        var referenceByIdentity = referenceRuns.ToDictionary(run => run.Identity);
        var candidateByIdentity = candidateRuns.ToDictionary(run => run.Identity);

        foreach (var run in referenceRuns.Where(run => run.TerminalIndex is not null))
        {
            var declared = DeclarationFor(
                context.Reference.Styles, path, run.Identity.Family, run.Identity.Property);

            if (!run.Cancelled && declared?.ExpectedMs is { } expected && Overruns(run, expected))
            {
                Run? counterpart = null;
                if (SameGroupCardinality(run.Identity, referenceRuns, candidateRuns))
                {
                    candidateByIdentity.TryGetValue(run.Identity, out counterpart);
                }
                yield return Overrun(
                    context,
                    path,
                    "React",
                    run,
                    declared.RawDuration,
                    $"{run.Identity.Family.DurationProperty}@{run.Identity}/react",
                    run,
                    counterpart?.TerminalIndex is not null ? counterpart : null);
            }
        }

        foreach (var run in candidateRuns.Where(run => run.TerminalIndex is not null))
        {
            var declared = DeclarationFor(
                context.Candidate.Styles, path, run.Identity.Family, run.Identity.Property);

            if (!run.Cancelled && declared?.ExpectedMs is { } expected && Overruns(run, expected))
            {
                Run? counterpart = null;
                if (SameGroupCardinality(run.Identity, referenceRuns, candidateRuns))
                {
                    referenceByIdentity.TryGetValue(run.Identity, out counterpart);
                }
                yield return Overrun(
                    context,
                    path,
                    "Blazor",
                    run,
                    declared.RawDuration,
                    $"{run.Identity.Family.DurationProperty}@{run.Identity}/blazor",
                    counterpart?.TerminalIndex is not null ? counterpart : null,
                    run);
            }
        }

        foreach (var left in referenceRuns.Where(run => run.TerminalIndex is not null))
        {
            if (!SameGroupCardinality(left.Identity, referenceRuns, candidateRuns) ||
                !candidateByIdentity.TryGetValue(left.Identity, out var right) ||
                right.TerminalIndex is null ||
                left.Start == right.Start && left.Length == right.Length)
            {
                continue;
            }

            var referenceDeclared = DeclarationFor(
                context.Reference.Styles, path, left.Identity.Family, left.Identity.Property);
            var candidateDeclared = DeclarationFor(
                context.Candidate.Styles, path, right.Identity.Family, right.Identity.Property);

            if (referenceDeclared is { ExpectedMs: null } ||
                candidateDeclared is { ExpectedMs: null })
            {
                // Infinite animations have no finite span to compare. Browsers may still
                // surface a synthetic terminal event when the page is torn down or a test
                // fixture forces one, but its timestamp is not L3 duration evidence.
                continue;
            }

            yield return new Finding
            {
                Fixture = context.ExecutionId,
                Leg = context.Leg,
                Step = context.Step,
                Kind = FindingKind.Timeline,
                Severity = Severity.Info,
                NodePath = path,
                Property = $"{left.Identity.Family.DurationProperty}@{left.Identity}",
                ReferenceValue = Milliseconds(left.Length),
                CandidateValue = Milliseconds(right.Length),
                Message =
                    $"Animation span at '{path}': " +
                    $"React started at {Milliseconds(left.Start)} ms and ran {Milliseconds(left.Length)} ms " +
                    $"(declared {FindingText.Describe(referenceDeclared is not null, referenceDeclared?.RawDuration)}); " +
                    $"Blazor started at {Milliseconds(right.Start)} ms and ran {Milliseconds(right.Length)} ms " +
                    $"(declared {FindingText.Describe(candidateDeclared is not null, candidateDeclared?.RawDuration)}); " +
                    $"the spans differ by {Milliseconds(Math.Abs(left.Length - right.Length))} ms."
            };
        }
    }

    private static Finding Overrun(
        ComparisonContext context,
        string path,
        string leg,
        Run run,
        string declared,
        string property,
        Run? reference,
        Run? candidate) => new()
        {
            Fixture = context.ExecutionId,
            Leg = context.Leg,
            Step = context.Step,
            Kind = FindingKind.Timeline,
            Severity = Severity.Error,
            NodePath = path,
            Property = property,
            ReferenceValue = reference is not null ? Milliseconds(reference.Length) : null,
            CandidateValue = candidate is not null ? Milliseconds(candidate.Length) : null,
            Message =
                $"Animation duration differs from its own declaration at '{path}': " +
                $"{leg} ran for {Milliseconds(run.Length)} ms starting at {Milliseconds(run.Start)} ms " +
                $"against a declared '{declared}'."
        };

    private static Leg Read(StepCapture capture) => new(
        capture.Timeline,
        capture.Dom.Descendants().Select(node => node.Path).ToHashSet(StringComparer.Ordinal),
        [
            .. capture.Timeline
                .Where(item => FamilyFor(item.Kind) is not null)
                .Select(item => item.Path)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal)
        ]);

    private static IReadOnlyList<Run> Runs(Leg leg, string path)
    {
        var runs = new List<Run>();
        var ordinals = new Dictionary<(RunFamily Family, string Property), int>();
        var open = new Dictionary<(RunFamily Family, string Property), Run>();
        var cycle = new List<Run>();
        var cycleStart = 0;
        var lastCycleTerminal = -1;

        for (var index = 0; index < leg.Timeline.Count; index++)
        {
            var recorded = leg.Timeline[index];

            if (!On(recorded, path) || FamilyFor(recorded.Kind) is not { } family)
            {
                continue;
            }

            var property = Property(recorded);
            var key = (family, property);

            if (family.Starts.Contains(recorded.Kind, StringComparer.Ordinal))
            {
                if (open.Remove(key, out var stranded) && open.Count == 0)
                {
                    CloseCycle(cycle, index);
                    cycleStart = index;
                }
                else if (open.Count == 0 && cycle.Count > 0)
                {
                    var boundary = CycleBoundary(
                        leg.Timeline, path, lastCycleTerminal + 1, index);
                    CloseCycle(cycle, boundary);
                    cycleStart = boundary;
                }

                var ordinal = ordinals.GetValueOrDefault(key);
                ordinals[key] = ordinal + 1;
                var run = new Run(
                    new RunIdentity(family, property, ordinal), index, recorded.T, cycleStart);
                runs.Add(run);
                cycle.Add(run);
                open[key] = run;
            }
            else if (open.TryGetValue(key, out var run))
            {
                run.TerminalIndex = index;
                run.Terminal = recorded.T;
                run.Cancelled = CancelKinds.Contains(recorded.Kind, StringComparer.Ordinal);
                open.Remove(key);

                if (open.Count == 0)
                {
                    lastCycleTerminal = index;
                }
            }
        }

        CloseCycle(cycle, leg.Timeline.Count);

        return runs.OrderBy(run => run.StartIndex).ToArray();
    }

    private static void CloseCycle(ICollection<Run> cycle, int windowEnd)
    {
        foreach (var run in cycle)
        {
            run.WindowEndIndex = windowEnd;
        }

        cycle.Clear();
    }

    private static int CycleBoundary(
        IReadOnlyList<TimelineEvent> timeline,
        string path,
        int start,
        int end)
    {
        var tag = TagOf(path);

        for (var index = end - 1; index >= start; index--)
        {
            if (timeline[index].Kind == RemovedKind &&
                (tag is null || timeline[index].From == tag))
            {
                return index + 1;
            }
        }

        return Math.Max(0, start);
    }

    private static Removal AttributeRemoval(
        Leg leg,
        string path,
        int windowStart,
        int windowEnd)
    {
        var removals = Enumerable.Range(windowStart, windowEnd - windowStart)
            .Where(index => leg.Timeline[index].Kind == RemovedKind)
            .ToArray();

        if (removals.Length == 0 ||
            windowEnd == leg.Timeline.Count && leg.Present.Contains(path))
        {
            return new Removal(RemovalKind.None, 0);
        }

        if (TagOf(path) is not { } tag)
        {
            return new Removal(RemovalKind.Ambiguous, 0);
        }

        var matching = removals.Where(index => leg.Timeline[index].From == tag).ToArray();

        if (matching.Length != 1 || leg.Animating.Count(other => TagOf(other) == tag) > 1)
        {
            return new Removal(RemovalKind.Ambiguous, 0);
        }

        return new Removal(RemovalKind.Attributed, matching[0]);
    }

    private static bool SameGroupCardinality(
        RunIdentity identity,
        IReadOnlyList<Run> reference,
        IReadOnlyList<Run> candidate)
        => reference.Count(run => run.Identity.SameGroup(identity)) ==
           candidate.Count(run => run.Identity.SameGroup(identity));

    private static string? TagOf(string path)
    {
        var separator = path.LastIndexOf(" > ", StringComparison.Ordinal);

        if (separator < 0)
        {
            return null;
        }

        var segment = path[(separator + 3)..];
        var qualifier = segment.IndexOfAny(['[', ':']);

        return qualifier < 0 ? segment : segment[..qualifier];
    }

    private static Declaration? DeclarationFor(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> styles,
        string path,
        RunFamily family,
        string runProperty)
    {
        if (!styles.TryGetValue(path, out var node) ||
            !node.TryGetValue(family.DurationProperty, out var durations))
        {
            return null;
        }

        var durationParts = Split(durations);
        var parsedDurations = durationParts.Select(ParseTime).ToArray();

        if (parsedDurations.Length == 0 || parsedDurations.Any(value => value is null))
        {
            return null;
        }

        int index;

        if (node.TryGetValue(family.IdentityProperty, out var identities))
        {
            var identityParts = Split(identities);
            index = Array.FindIndex(
                identityParts,
                value => string.Equals(value, runProperty, StringComparison.Ordinal));

            if (index < 0 && family.Name == "transition")
            {
                index = Array.FindIndex(
                    identityParts,
                    value => string.Equals(value, "all", StringComparison.Ordinal));
            }

            if (index < 0)
            {
                return null;
            }
        }
        else
        {
            index = LongestIndex(parsedDurations);
        }

        var duration = parsedDurations[index % parsedDurations.Length]!.Value;
        var delay = MappedTime(node, family.DelayProperty, index) ?? 0;
        var iterations = 1d;

        if (family.IterationProperty is not null &&
            node.TryGetValue(family.IterationProperty, out var iterationText))
        {
            var iterationParts = Split(iterationText);
            var selected = iterationParts[index % iterationParts.Length];

            if (string.Equals(selected, "infinite", StringComparison.Ordinal))
            {
                return new Declaration(durationParts[index % durationParts.Length], ExpectedMs: null);
            }

            if (!double.TryParse(
                    selected,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out iterations) || iterations < 0)
            {
                return null;
            }
        }

        var expected = Math.Max(0, duration * iterations + Math.Min(0, delay));
        return new Declaration(durationParts[index % durationParts.Length], expected);
    }

    private static double? MappedTime(
        IReadOnlyDictionary<string, string> node,
        string property,
        int index)
    {
        if (!node.TryGetValue(property, out var text))
        {
            return null;
        }

        var parts = Split(text);
        var parsed = parts.Select(ParseTime).ToArray();

        return parsed.Length > 0 && parsed.All(value => value is not null)
            ? parsed[index % parsed.Length]
            : null;
    }

    private static string[] Split(string value)
        => value.Split(',').Select(part => part.Trim()).ToArray();

    private static int LongestIndex(IReadOnlyList<double?> values)
    {
        var index = 0;

        for (var current = 1; current < values.Count; current++)
        {
            if (values[current] > values[index])
            {
                index = current;
            }
        }

        return index;
    }

    private static double? ParseTime(string value)
    {
        var text = value.Trim();
        double scale;

        if (text.EndsWith("ms", StringComparison.Ordinal))
        {
            scale = 1;
            text = text[..^2];
        }
        else if (text.EndsWith('s'))
        {
            scale = 1000;
            text = text[..^1];
        }
        else
        {
            return null;
        }

        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            ? number * scale
            : null;
    }

    private static bool Overruns(Run run, double expected)
        => expected > 0 &&
           Math.Abs(run.Length - expected) >
           Math.Max(ToleranceFloorMs, RelativeTolerance * expected);

    private static RunFamily? FamilyFor(string kind)
        => Families.FirstOrDefault(family =>
            family.Starts.Contains(kind, StringComparer.Ordinal) ||
            family.Terminals.Contains(kind, StringComparer.Ordinal));

    private static bool On(TimelineEvent recorded, string path)
        => string.Equals(recorded.Path, path, StringComparison.Ordinal);

    private static string Property(TimelineEvent recorded) => recorded.Attr ?? string.Empty;

    private static int IndexOf(
        IReadOnlyList<TimelineEvent> timeline,
        int start,
        int end,
        Func<TimelineEvent, bool> match)
    {
        for (var index = start; index < end; index++)
        {
            if (match(timeline[index]))
            {
                return index;
            }
        }

        return -1;
    }

    private static string Describe(PhaseState state) => state switch
    {
        PhaseState.Satisfied => "satisfied it",
        PhaseState.Violated => "violated it",
        _ => "could not be evaluated"
    };

    private static string Value(PhaseState state) => state switch
    {
        PhaseState.Satisfied => "satisfied",
        PhaseState.Violated => "violated",
        _ => "not evaluated"
    };

    private static string Milliseconds(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Headline(int reference, int candidate, int added, int removed)
    {
        if (reference == 0)
        {
            return $"React recorded no animation events; Blazor recorded {Events(candidate)}.";
        }

        if (candidate == 0)
        {
            return $"Blazor recorded no animation events; React recorded {Events(reference)}.";
        }

        return $"Animation timeline differs: {Events(added)} added, {Events(removed)} removed.";
    }

    private static string Events(int events) => events == 1 ? "1 event" : $"{events} events";

    private static string Lines(int lines) => lines == 1 ? "1 line" : $"{lines} lines";

    private static List<(char Marker, string Text)> Align(
        IReadOnlyList<string> reference,
        IReadOnlyList<string> candidate)
    {
        var common = new int[reference.Count + 1, candidate.Count + 1];

        for (var referenceIndex = reference.Count - 1; referenceIndex >= 0; referenceIndex--)
        {
            for (var candidateIndex = candidate.Count - 1; candidateIndex >= 0; candidateIndex--)
            {
                common[referenceIndex, candidateIndex] = string.Equals(
                    reference[referenceIndex], candidate[candidateIndex], StringComparison.Ordinal)
                    ? common[referenceIndex + 1, candidateIndex + 1] + 1
                    : Math.Max(
                        common[referenceIndex + 1, candidateIndex],
                        common[referenceIndex, candidateIndex + 1]);
            }
        }

        var operations = new List<(char, string)>();
        var left = 0;
        var right = 0;

        while (left < reference.Count && right < candidate.Count)
        {
            if (string.Equals(reference[left], candidate[right], StringComparison.Ordinal))
            {
                operations.Add((' ', reference[left]));
                left++;
                right++;
            }
            else if (common[left + 1, right] >= common[left, right + 1])
            {
                operations.Add(('-', reference[left++]));
            }
            else
            {
                operations.Add(('+', candidate[right++]));
            }
        }

        for (; left < reference.Count; left++)
        {
            operations.Add(('-', reference[left]));
        }

        for (; right < candidate.Count; right++)
        {
            operations.Add(('+', candidate[right]));
        }

        return operations;
    }

    private static List<string> Render(IReadOnlyList<(char Marker, string Text)> operations)
    {
        List<string> lines = [.. operations.Select(operation => $"{operation.Marker}{operation.Text}")];

        return lines.Count <= MaxDiffLines
            ? lines
            :
            [
                .. lines.Take(MaxDiffLines),
                $"... {Lines(lines.Count - MaxDiffLines)} of the diff omitted."
            ];
    }

    private sealed class Run(
        RunIdentity identity,
        int startIndex,
        int start,
        int windowStartIndex)
    {
        internal RunIdentity Identity { get; } = identity;

        internal int StartIndex { get; } = startIndex;

        internal int Start { get; } = start;

        internal int WindowStartIndex { get; } = windowStartIndex;

        internal int? TerminalIndex { get; set; }

        internal int? Terminal { get; set; }

        internal int? WindowEndIndex { get; set; }

        internal bool Cancelled { get; set; }

        internal int Length => Terminal!.Value - Start;
    }

    private readonly record struct RunIdentity(
        RunFamily Family,
        string Property,
        int Ordinal)
    {
        internal bool SameGroup(RunIdentity other)
            => Family == other.Family && string.Equals(Property, other.Property, StringComparison.Ordinal);

        public override string ToString() => $"{Family.Name}:{Property}#{Ordinal}";
    }

    private readonly record struct Removal(RemovalKind Kind, int Index);

    private sealed record RunFamily(
        string Name,
        IReadOnlyList<string> Starts,
        IReadOnlyList<string> Terminals,
        string DurationProperty,
        string DelayProperty,
        string IdentityProperty,
        string? IterationProperty);

    private sealed record Leg(
        IReadOnlyList<TimelineEvent> Timeline,
        IReadOnlySet<string> Present,
        IReadOnlyList<string> Animating);

    private sealed record Declaration(string RawDuration, double? ExpectedMs);
}
