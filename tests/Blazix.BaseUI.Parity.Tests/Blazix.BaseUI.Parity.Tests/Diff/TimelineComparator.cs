using System.Globalization;
using Blazix.BaseUI.Parity.Tests.Capture;

namespace Blazix.BaseUI.Parity.Tests.Diff;

/// <summary>
/// Reports how the two legs animated: the order of the recorded events, the phase
/// obligations that order has to satisfy, and how long each run actually took.
/// </summary>
/// <remarks>
/// <para>
/// This is the one comparison that reasons about a sequence over time rather than a value at
/// one instant, so it is layered. <b>L1</b> diffs the timestamp-erased signatures, which
/// catches a phase that is missing, extra, or in the wrong place. <b>L2</b> derives four
/// obligations per animating path, so that a difference L1 reports as a moved line is also
/// named for what it broke. <b>L3</b> measures each leg's run against the duration that
/// leg's own CSS declares, never against the other leg's clock: a Blazor Server circuit
/// shifts when an animation starts without changing how long it runs, and a harness that
/// failed on that would fail every animated fixture on the slow leg.
/// </para>
/// <para>
/// Only steps whose manifest entry settles on <c>animation</c> record a timeline, so on
/// every other step both legs are empty and nothing here has anything to compare.
/// </para>
/// <para>
/// L2 and L3 read the paths that animated on <em>both</em> legs. A node that animates on one
/// leg only is already the loudest possible L1 result, and holding a leg that ran no
/// animation at all to an animation's obligations states a conclusion about it that the
/// capture does not support.
/// </para>
/// <para>
/// What the capture cannot say, this comparator does not guess. A removal event carries no
/// path — <c>capture.js</c> cannot compute one for a node that has already left the tree —
/// only the departing tag, so a removal is attributed to an animating node only when that
/// node is absent from the step's final snapshot, exactly one removal carries its tag, and
/// no other animating node shares that tag. When the attribution cannot be made, the
/// invariants that depend on it are reported as undecided rather than as satisfied, and an
/// undecided invariant is <see cref="Severity.Info"/> so that it never fails a run on
/// something nobody measured.
/// </para>
/// <para>
/// Two of the four invariants overlap by construction: a node removed while its transition
/// is still running is both absent at the end of the run and removed before one, and a
/// detached node's <c>transitioncancel</c> never reaches the document listener, so that one
/// defect is the only way either can be violated in a real capture. They are still reported
/// separately, because they name different obligations and a reader fixing one wants to see
/// the other.
/// </para>
/// </remarks>
public sealed class TimelineComparator : IComparator
{
    /// <summary>
    /// The smallest difference between a run and its declaration that is worth reporting.
    /// Below this everything is scheduling: a transition is driven by animation frames, and
    /// the frame a run starts and ends on moves with whatever else the main thread is doing.
    /// </summary>
    private const double ToleranceFloorMs = 50;

    /// <summary>
    /// How far a run may be from its declaration once the declaration is long enough for the
    /// floor to stop mattering. Half is loose on purpose: this layer exists to catch a
    /// duration that is wrong by a factor, not one that is late by a frame.
    /// </summary>
    private const double RelativeTolerance = 0.5;

    /// <summary>
    /// How many diff lines a message carries before the rest are counted instead of printed.
    /// </summary>
    private const int MaxDiffLines = 40;

    /// <summary>The style property L3 measures a transition against.</summary>
    private const string DurationProperty = "transition-duration";

    private const string AttributeKind = "attribute";
    private const string AddedKind = "added";
    private const string RemovedKind = "removed";

    /// <summary>The events that open a run.</summary>
    private static readonly string[] StartKinds = ["transitionstart", "animationstart"];

    /// <summary>
    /// The events that close one. A cancellation counts: it is dispatched to the element, so
    /// observing one is as much proof that the node was still in the tree as an end is.
    /// </summary>
    private static readonly string[] TerminalKinds =
        ["transitionend", "animationend", "transitioncancel", "animationcancel"];

    /// <summary>
    /// The four obligations L2 derives, in the order they are reported. The names are what a
    /// report and a waiver rule read, so they are literals rather than prose.
    /// </summary>
    private static readonly string[] InvariantNames =
    [
        "mounted-before-transition-start",
        "present-at-transitionend",
        "data-open-flipped-before-starting-style-cleared",
        "removed-after-transitionend"
    ];

    /// <summary>
    /// Why each invariant may be undecidable, appended to the message when it was. Two of
    /// the four are always decidable, and carry nothing.
    /// </summary>
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

    /// <summary>Whether an obligation held, was broken, or could not be decided.</summary>
    private enum PhaseState
    {
        /// <summary>Nothing in the recording breaks it.</summary>
        Satisfied,

        /// <summary>The recording breaks it.</summary>
        Violated,

        /// <summary>The recording does not say.</summary>
        Unknown
    }

    /// <summary>Whether a removal event could be tied to an animating node.</summary>
    private enum RemovalKind
    {
        /// <summary>Nothing that could be this node was removed.</summary>
        None,

        /// <summary>Something was, and which removal it is cannot be decided.</summary>
        Ambiguous,

        /// <summary>Exactly one removal can be this node's.</summary>
        Attributed
    }

    /// <inheritdoc />
    public FindingKind Kind => FindingKind.Timeline;

    /// <inheritdoc />
    public IEnumerable<Finding> Compare(ComparisonContext context)
    {
        foreach (var finding in CompareSequences(context))
        {
            yield return finding;
        }

        var reference = Read(context.Reference);
        var candidate = Read(context.Candidate);

        foreach (var path in reference.Animating.Intersect(candidate.Animating, StringComparer.Ordinal))
        {
            foreach (var finding in ComparePhases(context, reference, candidate, path))
            {
                yield return finding;
            }

            foreach (var finding in CompareSpans(context, path))
            {
                yield return finding;
            }
        }
    }

    /// <summary>
    /// L1: reports the two normalized sequences differing, as one unified diff.
    /// </summary>
    /// <remarks>
    /// One finding per step rather than one per differing event: a phase that arrives late
    /// moves every event after it, and a finding per event would report one defect a dozen
    /// times over.
    /// </remarks>
    /// <param name="context">The paired step.</param>
    /// <returns>The sequence finding, or none.</returns>
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
            Fixture = context.Fixture,
            Leg = context.Leg,
            Step = context.Step,
            Kind = FindingKind.Timeline,
            Severity = Severity.Error,
            // Both sequences whole, because the diff in the message is the readable form and
            // this is the evidence behind it.
            ReferenceValue = string.Join('\n', reference),
            CandidateValue = string.Join('\n', candidate),
            Message = string.Join('\n', message)
        };
    }

    /// <summary>
    /// L2: reports each phase obligation the two legs disagree on.
    /// </summary>
    /// <param name="context">The paired step.</param>
    /// <param name="reference">What React recorded.</param>
    /// <param name="candidate">What Blazor recorded.</param>
    /// <param name="path">The animating node.</param>
    /// <returns>One finding per differing invariant.</returns>
    private static IEnumerable<Finding> ComparePhases(
        ComparisonContext context, Leg reference, Leg candidate, string path)
    {
        var left = Evaluate(reference, path);
        var right = Evaluate(candidate, path);

        for (var i = 0; i < InvariantNames.Length; i++)
        {
            // Two legs in the same state are not a difference — including two that break the
            // same obligation, which is not a Blazix defect, and two that cannot be decided,
            // which is not a result at all.
            if (left[i] == right[i])
            {
                continue;
            }

            var undecided = left[i] == PhaseState.Unknown || right[i] == PhaseState.Unknown;

            yield return new Finding
            {
                Fixture = context.Fixture,
                Leg = context.Leg,
                Step = context.Step,
                Kind = FindingKind.Timeline,
                // An obligation one leg could not be measured against never fails a run:
                // there is no evidence of a difference, only an absence of evidence.
                Severity = undecided ? Severity.Info : Severity.Error,
                NodePath = path,
                Property = InvariantNames[i],
                ReferenceValue = Value(left[i]),
                CandidateValue = Value(right[i]),
                Message = undecided
                    ? $"Animation invariant '{InvariantNames[i]}' was not decided at '{path}': "
                      + $"React {Describe(left[i])}; Blazor {Describe(right[i])}."
                      + InvariantNotes[i]
                    : $"Animation invariant '{InvariantNames[i]}' differs at '{path}': "
                      + $"React {Describe(left[i])}; Blazor {Describe(right[i])}."
            };
        }
    }

    /// <summary>
    /// L3: measures each leg's run against its own declared duration, and records the delta
    /// between the two.
    /// </summary>
    /// <remarks>
    /// A declaration that is missing or zero is not checked. The step is captured once the
    /// animation is over, by which time the markers that switched the transition on are gone
    /// and the element may compute a <c>transition-duration</c> that says nothing about the
    /// run that happened; failing on that would report a defect where there is only a
    /// stylesheet whose transition is conditional. A comma-separated list is read at its
    /// longest, because the span the timeline measures runs from the first start to the last
    /// terminal event across every property that ran.
    /// </remarks>
    /// <param name="context">The paired step.</param>
    /// <param name="path">The animating node.</param>
    /// <returns>Up to one finding per leg, plus the cross-leg record.</returns>
    private static IEnumerable<Finding> CompareSpans(ComparisonContext context, string path)
    {
        var reference = Span(context.Reference.Timeline, path);
        var candidate = Span(context.Candidate.Timeline, path);
        var referenceDeclared = Declared(context.Reference.Styles, path);
        var candidateDeclared = Declared(context.Candidate.Styles, path);

        if (reference is { } reactRun && referenceDeclared is { } reactDeclared
            && Overruns(reactRun, reactDeclared))
        {
            yield return Overrun(context, path, "React", reactRun, reactDeclared, reference, candidate);
        }

        if (candidate is { } blazorRun && candidateDeclared is { } blazorDeclared
            && Overruns(blazorRun, blazorDeclared))
        {
            yield return Overrun(context, path, "Blazor", blazorRun, blazorDeclared, reference, candidate);
        }

        // Two runs that started at the same millisecond and lasted the same time have no
        // delta to carry, so nothing is recorded for them.
        if (reference is not { } left || candidate is not { } right || left == right)
        {
            yield break;
        }

        yield return new Finding
        {
            Fixture = context.Fixture,
            Leg = context.Leg,
            Step = context.Step,
            Kind = FindingKind.Timeline,
            // Never an error: this is the number a reader wants when a fixture feels slow,
            // and the cross-leg comparison it comes from is exactly the one that must not
            // decide a verdict.
            Severity = Severity.Info,
            NodePath = path,
            Property = DurationProperty,
            ReferenceValue = Milliseconds(left.Length),
            CandidateValue = Milliseconds(right.Length),
            Message =
                $"Animation span at '{path}': "
                + $"React started at {Milliseconds(left.Start)} ms and ran {Milliseconds(left.Length)} ms "
                + $"(declared {FindingText.Describe(referenceDeclared is not null, referenceDeclared)}); "
                + $"Blazor started at {Milliseconds(right.Start)} ms and ran {Milliseconds(right.Length)} ms "
                + $"(declared {FindingText.Describe(candidateDeclared is not null, candidateDeclared)}); "
                + $"the spans differ by {Milliseconds(Math.Abs(left.Length - right.Length))} ms."
        };
    }

    /// <summary>Builds the finding for a run that does not match its own declaration.</summary>
    /// <param name="context">The paired step.</param>
    /// <param name="path">The animating node.</param>
    /// <param name="leg">Which leg overran, named as the messages name it.</param>
    /// <param name="run">That leg's measured run.</param>
    /// <param name="declared">That leg's declared duration, as the capture spelled it.</param>
    /// <param name="reference">React's run, for the compared values.</param>
    /// <param name="candidate">Blazor's run, for the compared values.</param>
    /// <returns>The finding.</returns>
    private static Finding Overrun(
        ComparisonContext context,
        string path,
        string leg,
        Run run,
        string declared,
        Run? reference,
        Run? candidate) => new()
        {
            Fixture = context.Fixture,
            Leg = context.Leg,
            Step = context.Step,
            Kind = FindingKind.Timeline,
            Severity = Severity.Error,
            NodePath = path,
            Property = DurationProperty,
            ReferenceValue = reference is { } left ? Milliseconds(left.Length) : null,
            CandidateValue = candidate is { } right ? Milliseconds(right.Length) : null,
            Message =
                $"Animation duration differs from its own declaration at '{path}': "
                + $"{leg} ran for {Milliseconds(run.Length)} ms against a declared '{declared}'."
        };

    /// <summary>Reads the parts of a capture the phase and duration layers work from.</summary>
    /// <param name="capture">One leg's step.</param>
    /// <returns>The leg.</returns>
    private static Leg Read(StepCapture capture) => new(
        capture.Timeline,
        capture.Dom.Descendants().Select(node => node.Path).ToHashSet(StringComparer.Ordinal),
        [
            .. capture.Timeline
                .Where(recorded => IsRun(recorded.Kind))
                .Select(recorded => recorded.Path)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal)
        ]);

    /// <summary>Derives the four obligations for one node on one leg.</summary>
    /// <param name="leg">The leg.</param>
    /// <param name="path">The animating node.</param>
    /// <returns>The four states, in <see cref="InvariantNames"/> order.</returns>
    private static PhaseState[] Evaluate(Leg leg, string path)
    {
        var timeline = leg.Timeline;
        var firstStart = IndexOf(timeline, e => On(e, path) && StartKinds.Contains(e.Kind, StringComparer.Ordinal));
        var firstAdded = IndexOf(timeline, e => On(e, path) && e.Kind == AddedKind);
        var lastTerminal = LastIndexOf(
            timeline, e => On(e, path) && TerminalKinds.Contains(e.Kind, StringComparer.Ordinal));

        // A node the recording never saw inserted was already mounted when it began, which
        // is the obligation held rather than a gap in the evidence.
        var mounted = firstStart >= 0 && firstAdded > firstStart
            ? PhaseState.Violated
            : PhaseState.Satisfied;

        // Per property, not per node: a popup that transitions opacity and transform and
        // leaves mid-transform still recorded an end for opacity, and asking only whether
        // anything ended would call that a clean finish.
        var finished = timeline
            .Where(e => On(e, path) && TerminalKinds.Contains(e.Kind, StringComparer.Ordinal))
            .Select(Property)
            .ToHashSet(StringComparer.Ordinal);
        var unfinished = timeline
            .Where(e => On(e, path) && StartKinds.Contains(e.Kind, StringComparer.Ordinal))
            .Any(e => !finished.Contains(Property(e)));

        var removal = AttributeRemoval(leg, path);

        var present = !unfinished
            ? PhaseState.Satisfied
            : removal.Kind == RemovalKind.Attributed
                ? PhaseState.Violated
                : PhaseState.Unknown;

        var flipped = IndexOf(
            timeline,
            e => On(e, path)
                && e.Kind == AttributeKind
                && e.Attr == "data-open"
                && !string.Equals(e.From, e.To, StringComparison.Ordinal));
        var cleared = IndexOf(
            timeline,
            e => On(e, path)
                && e.Kind == AttributeKind
                && e.Attr == "data-starting-style"
                && e.To is null);

        // Either half missing is the ordering never arising, not the ordering being wrong.
        var ordering = flipped >= 0 && cleared >= 0 && cleared < flipped
            ? PhaseState.Violated
            : PhaseState.Satisfied;

        var unmounted = removal.Kind switch
        {
            RemovalKind.None => PhaseState.Satisfied,
            RemovalKind.Ambiguous => PhaseState.Unknown,
            // Removed with nothing ever having ended is as much a break as removed too
            // early: the unmount did not wait for the run either way.
            _ => lastTerminal >= 0 && removal.Index > lastTerminal
                ? PhaseState.Satisfied
                : PhaseState.Violated
        };

        return [mounted, present, ordering, unmounted];
    }

    /// <summary>
    /// Decides which removal event, if any, is this node's.
    /// </summary>
    /// <remarks>
    /// The final snapshot is what makes this sound in the common direction: a node the step
    /// ended with is a node no removal event can be about, whatever tags the removals
    /// carried. In the other direction the tag is all there is, so anything that could name
    /// two nodes is left undecided rather than guessed at.
    /// </remarks>
    /// <param name="leg">The leg.</param>
    /// <param name="path">The animating node.</param>
    /// <returns>The attribution.</returns>
    private static Removal AttributeRemoval(Leg leg, string path)
    {
        var removals = new List<int>();

        for (var i = 0; i < leg.Timeline.Count; i++)
        {
            if (leg.Timeline[i].Kind == RemovedKind)
            {
                removals.Add(i);
            }
        }

        if (removals.Count == 0 || leg.Present.Contains(path))
        {
            return new Removal(RemovalKind.None, 0);
        }

        // A path of one segment is a capture root's label rather than an element, so there
        // is no tag to match a removal's against — and a portalled popup is captured exactly
        // that way.
        if (TagOf(path) is not { } tag)
        {
            return new Removal(RemovalKind.Ambiguous, 0);
        }

        var matching = removals.Where(i => leg.Timeline[i].From == tag).ToList();

        if (matching.Count == 0)
        {
            return new Removal(RemovalKind.None, 0);
        }

        if (matching.Count > 1 || leg.Animating.Count(other => TagOf(other) == tag) > 1)
        {
            return new Removal(RemovalKind.Ambiguous, 0);
        }

        return new Removal(RemovalKind.Attributed, matching[0]);
    }

    /// <summary>Reads the tag out of a node path.</summary>
    /// <param name="path">The path, for example <c>portal(1) &gt; div[role=dialog]</c>.</param>
    /// <returns>The tag, or <see langword="null"/> when the path names a capture root.</returns>
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

    /// <summary>Measures one leg's run on one node.</summary>
    /// <param name="timeline">The leg's events.</param>
    /// <param name="path">The animating node.</param>
    /// <returns>
    /// The run, or <see langword="null"/> when the recording holds no start followed by a
    /// terminal event — a step captured mid-run, which is not a measurement.
    /// </returns>
    private static Run? Span(IReadOnlyList<TimelineEvent> timeline, string path)
    {
        int? start = null;
        int? terminal = null;

        foreach (var recorded in timeline)
        {
            if (!On(recorded, path))
            {
                continue;
            }

            if (start is null && StartKinds.Contains(recorded.Kind, StringComparer.Ordinal))
            {
                start = recorded.T;
            }
            else if (start is not null && TerminalKinds.Contains(recorded.Kind, StringComparer.Ordinal))
            {
                // The last one, so a node transitioning several properties is measured over
                // all of them rather than to whichever finished first.
                terminal = recorded.T;
            }
        }

        return start is { } opened && terminal is { } closed ? new Run(opened, closed - opened) : null;
    }

    /// <summary>Reads a leg's declared transition duration for one node.</summary>
    /// <param name="styles">That leg's captured styles.</param>
    /// <param name="path">The animating node.</param>
    /// <returns>The declaration as captured, or <see langword="null"/> when there is none.</returns>
    private static string? Declared(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> styles, string path)
        => styles.TryGetValue(path, out var node) && node.TryGetValue(DurationProperty, out var declared)
            ? declared
            : null;

    /// <summary>
    /// Reports whether a run is too far from what its own leg declared.
    /// </summary>
    /// <param name="run">The measured run.</param>
    /// <param name="declared">The declaration as that leg's styles spelled it.</param>
    /// <returns><see langword="true"/> when the run breaks its own declaration.</returns>
    private static bool Overruns(Run run, string declared)
        => Duration(declared) is { } expected
            && expected > 0
            && Math.Abs(run.Length - expected) > Math.Max(ToleranceFloorMs, RelativeTolerance * expected);

    /// <summary>
    /// Reads a <c>transition-duration</c> declaration as milliseconds.
    /// </summary>
    /// <remarks>
    /// A list is read at its longest. Anything that does not parse whole — a value with no
    /// unit, an empty declaration, a list with one part this does not understand — reads as
    /// no declaration at all, so a value this cannot measure is left unchecked rather than
    /// checked against a number it guessed.
    /// </remarks>
    /// <param name="declared">The declaration.</param>
    /// <returns>The longest duration in milliseconds, or <see langword="null"/>.</returns>
    private static double? Duration(string declared)
    {
        double? longest = null;

        foreach (var part in declared.Split(','))
        {
            var text = part.Trim();
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

            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                return null;
            }

            longest = longest is { } best && best >= value * scale ? best : value * scale;
        }

        return longest;
    }

    /// <summary>Reports whether an event happened on one node.</summary>
    /// <param name="recorded">The event.</param>
    /// <param name="path">The node path.</param>
    /// <returns><see langword="true"/> when the event names that node.</returns>
    private static bool On(TimelineEvent recorded, string path)
        => string.Equals(recorded.Path, path, StringComparison.Ordinal);

    /// <summary>Reads the CSS property or animation name a run event carries.</summary>
    /// <param name="recorded">The event.</param>
    /// <returns>The name, or an empty string when the browser reported none.</returns>
    private static string Property(TimelineEvent recorded) => recorded.Attr ?? string.Empty;

    /// <summary>Finds the first event matching <paramref name="match"/>.</summary>
    /// <param name="timeline">The events.</param>
    /// <param name="match">The test.</param>
    /// <returns>Its index, or -1.</returns>
    private static int IndexOf(IReadOnlyList<TimelineEvent> timeline, Func<TimelineEvent, bool> match)
    {
        for (var i = 0; i < timeline.Count; i++)
        {
            if (match(timeline[i]))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Finds the last event matching <paramref name="match"/>.</summary>
    /// <param name="timeline">The events.</param>
    /// <param name="match">The test.</param>
    /// <returns>Its index, or -1.</returns>
    private static int LastIndexOf(IReadOnlyList<TimelineEvent> timeline, Func<TimelineEvent, bool> match)
    {
        for (var i = timeline.Count - 1; i >= 0; i--)
        {
            if (match(timeline[i]))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Reports whether a kind is a transition or animation event.</summary>
    /// <param name="kind">The event kind.</param>
    /// <returns><see langword="true"/> when the event belongs to a run.</returns>
    private static bool IsRun(string kind)
        => StartKinds.Contains(kind, StringComparer.Ordinal)
            || TerminalKinds.Contains(kind, StringComparer.Ordinal);

    /// <summary>Writes a state as a message reads it.</summary>
    /// <param name="state">The state.</param>
    /// <returns>The phrase.</returns>
    private static string Describe(PhaseState state) => state switch
    {
        PhaseState.Satisfied => "satisfied it",
        PhaseState.Violated => "violated it",
        _ => "could not be evaluated"
    };

    /// <summary>Writes a state as a compared value.</summary>
    /// <param name="state">The state.</param>
    /// <returns>The value.</returns>
    private static string Value(PhaseState state) => state switch
    {
        PhaseState.Satisfied => "satisfied",
        PhaseState.Violated => "violated",
        _ => "not evaluated"
    };

    /// <summary>
    /// Writes a millisecond count invariantly, so a message written on one machine reads the
    /// same on another whose number formatting differs.
    /// </summary>
    /// <param name="value">The count.</param>
    /// <returns>The rendered number.</returns>
    private static string Milliseconds(int value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>States what the diff below it amounts to.</summary>
    /// <param name="reference">How many events React recorded.</param>
    /// <param name="candidate">How many events Blazor recorded.</param>
    /// <param name="added">How many signatures only Blazor has.</param>
    /// <param name="removed">How many signatures only React has.</param>
    /// <returns>The first line of the message.</returns>
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

        // Counted before the diff is capped, so truncation cannot hide the scale.
        return $"Animation timeline differs: {Events(added)} added, {Events(removed)} removed.";
    }

    /// <summary>Counts events without writing "1 events".</summary>
    /// <param name="events">The number of events.</param>
    /// <returns>The counted phrase.</returns>
    private static string Events(int events) => events == 1 ? "1 event" : $"{events} events";

    /// <summary>Counts lines without writing "1 lines".</summary>
    /// <param name="lines">The number of lines.</param>
    /// <returns>The counted phrase.</returns>
    private static string Lines(int lines) => lines == 1 ? "1 line" : $"{lines} lines";

    /// <summary>
    /// Aligns the two sequences on their longest common subsequence.
    /// </summary>
    /// <remarks>
    /// A removal is preferred to an addition wherever the two cost the same, which is what
    /// puts the old event above the new one in a changed block, as a unified diff prints it.
    /// </remarks>
    /// <param name="reference">React's signatures.</param>
    /// <param name="candidate">Blazor's signatures.</param>
    /// <returns>The ordered edits.</returns>
    private static List<(char Marker, string Text)> Align(
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

        var ops = new List<(char, string)>();
        var x = 0;
        var y = 0;

        while (x < reference.Count && y < candidate.Count)
        {
            if (string.Equals(reference[x], candidate[y], StringComparison.Ordinal))
            {
                ops.Add((' ', reference[x]));
                x++;
                y++;
            }
            else if (common[x + 1, y] >= common[x, y + 1])
            {
                ops.Add(('-', reference[x]));
                x++;
            }
            else
            {
                ops.Add(('+', candidate[y]));
                y++;
            }
        }

        for (; x < reference.Count; x++)
        {
            ops.Add(('-', reference[x]));
        }

        for (; y < candidate.Count; y++)
        {
            ops.Add(('+', candidate[y]));
        }

        return ops;
    }

    /// <summary>
    /// Renders the edits, unchanged events included.
    /// </summary>
    /// <remarks>
    /// A timeline is tens of events at most, so the whole of it is printed rather than hunks
    /// around each change: the events between two changes are the phases that did survive,
    /// which is most of what a reader needs to place the ones that did not.
    /// </remarks>
    /// <param name="ops">The aligned edits.</param>
    /// <returns>The diff body, capped.</returns>
    private static List<string> Render(IReadOnlyList<(char Marker, string Text)> ops)
    {
        List<string> lines = [.. ops.Select(op => $"{op.Marker}{op.Text}")];

        return lines.Count <= MaxDiffLines
            ? lines
            : [.. lines.Take(MaxDiffLines), $"... {Lines(lines.Count - MaxDiffLines)} of the diff omitted."];
    }

    /// <summary>One measured run.</summary>
    /// <param name="Start">Milliseconds from the trigger action to the first start event.</param>
    /// <param name="Length">Milliseconds from that start to the last terminal event.</param>
    private readonly record struct Run(int Start, int Length);

    /// <summary>Which removal event an animating node's is.</summary>
    /// <param name="Kind">Whether it could be decided.</param>
    /// <param name="Index">Where it sits in the timeline, when it could.</param>
    private readonly record struct Removal(RemovalKind Kind, int Index);

    /// <summary>What one leg's capture contributes to the phase and duration layers.</summary>
    /// <param name="Timeline">The recorded events.</param>
    /// <param name="Present">The paths the step ended with.</param>
    /// <param name="Animating">The paths that ran a transition or animation.</param>
    private sealed record Leg(
        IReadOnlyList<TimelineEvent> Timeline,
        IReadOnlySet<string> Present,
        IReadOnlyList<string> Animating);
}
