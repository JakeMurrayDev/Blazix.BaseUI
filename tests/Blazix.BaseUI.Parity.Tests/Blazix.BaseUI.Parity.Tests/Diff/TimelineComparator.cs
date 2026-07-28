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
/// <para>
/// <b>L2's four invariants are derived over the whole step, never per run, and are
/// unreliable on any step that holds more than one run on a path.</b> The properties that
/// reached a terminal event, the first insertion, the last terminal event and the removal
/// index are each read from the step's entire timeline, so a step holding two runs is
/// reasoned about as though it held one: a terminal event belonging to the first run
/// satisfies a start belonging to the second, and a removal is ordered against the last
/// terminal event anywhere in the step rather than against the run it interrupted. The
/// consequence is not only that a difference can go unnamed. A leg can be reported as having
/// <em>satisfied</em> obligations it demonstrably broke — a node that completed one run and
/// then left part way through a second reads as present at its <c>transitionend</c>, because
/// the first run's end carried the same property name, and as removed after one, because that
/// same earlier end is the last terminal the removal is ordered against — which points a
/// reader at the wrong leg when the other leg broke them too. This is a mislabelled positive
/// and never a silent pass. What <see cref="TimelineSequence.Normalize"/> never drops or
/// collapses is a run, <c>added</c> or <c>removed</c> event — not the timeline, which it also
/// strips of untracked attribute mutations, of a consecutive duplicate attribute signature and
/// of every <c>from</c> but a removal's — so a run, an insertion or a removal that one leg
/// recorded and the other did not always reaches L1. Equal signatures leave two ways for the
/// derived states still to differ: the snapshot the step ended on, which
/// <see cref="AttributeRemoval"/> reads and <c>Normalize</c> never sees; and the <c>from</c> of
/// an attribute mutation, which <c>data-open-flipped-before-starting-style-cleared</c> reads to
/// tell a write that changed something from one that did not, and which <c>Normalize</c> drops.
/// The second is closed to the two invariants this paragraph is about:
/// <c>present-at-transitionend</c> and <c>removed-after-transitionend</c> read no <c>from</c>
/// but a removal's, which a signature does carry, so for them the snapshot is the whole of the
/// enumeration — and a difference in that snapshot is what the structure comparator reports.
/// The difference is always reported, by L1 or by the structure comparator; only the name put
/// on it can be wrong. Read an L2 result as reliable only on a step with a single run per
/// path, and prefer L1's diff to L2's naming where both fire.
/// </para>
/// <para>
/// L3 pairs the two legs' runs by index, so the cross-leg record and the compared values
/// carried alongside an overrun are meaningful only when both legs ran the same number of
/// runs. Where they did not, one leg's open is compared against the other's close, and a real
/// finding about one leg's run is printed next to a number lifted from a different run of the
/// other's. The verdict is never wrong for this reason — a leg is failed only against its own
/// declaration, measured on its own run — but the numbers beside it can be.
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

    private const string AttributeKind = "attribute";
    private const string AddedKind = "added";
    private const string RemovedKind = "removed";

    /// <summary>
    /// The two kinds of run, each carrying the events that open one, the events that close
    /// one, and the style property L3 measures a run of that kind against.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A transition and a keyframe animation are separate runs, and stay separate even where
    /// they overlap on one node. Measuring both in one bucket does two wrong things at once:
    /// it reads an overlapping pair as a single span from the first start to the last terminal
    /// event, and it measures a keyframe run against a <c>transition-duration</c> that says
    /// nothing about it.
    /// </para>
    /// <para>
    /// An element that declares a transition it is not currently running is the ordinary case
    /// rather than a corner one — a Tailwind <c>transition duration-150</c> utility puts a
    /// non-zero <c>transition-duration</c> on the element whatever else animates it — so a
    /// keyframe run measured against that declaration fails both legs of two byte-identical
    /// timelines while naming no difference between them.
    /// </para>
    /// </remarks>
    private static readonly RunFamily[] Families =
    [
        new(["transitionstart"], ["transitionend", "transitioncancel"], "transition-duration"),
        new(["animationstart"], ["animationend", "animationcancel"], "animation-duration")
    ];

    /// <summary>The events that open a run, of either kind.</summary>
    private static readonly string[] StartKinds = [.. Families.SelectMany(family => family.Starts)];

    /// <summary>
    /// The events that close one, of either kind. A cancellation counts: it is dispatched to
    /// the element, so observing one is as much proof that the node was still in the tree as
    /// an end is.
    /// </summary>
    private static readonly string[] TerminalKinds = [.. Families.SelectMany(family => family.Terminals)];

    /// <summary>
    /// The terminal events that cut a run short instead of completing it. L2 wants these —
    /// they are proof the node was still in the tree — and L3 cannot use them, because a run
    /// that was cancelled is shorter than its declaration for the reason it was cancelled.
    /// </summary>
    private static readonly string[] CancelKinds = ["transitioncancel", "animationcancel"];

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
        /// <summary>
        /// The node was not removed: either the step recorded no removal at all, or the
        /// node is in the snapshot the step ended on. Both are positive evidence, which is
        /// what lets this state read as the obligation being satisfied.
        /// </summary>
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
    /// L3: measures each of a leg's runs against its own declared duration, and records the
    /// delta between the two legs run for run.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each kind of run is measured against its own declaration and never against the other
    /// kind's: a transition against <c>transition-duration</c>, a keyframe animation against
    /// <c>animation-duration</c>. The runs are separated the same way, so a node that
    /// transitions and animates over one window is two runs here rather than one span across
    /// both. See <see cref="Families"/>.
    /// </para>
    /// <para>
    /// A declaration that is missing or zero is not checked. The step is captured once the
    /// animation is over, by which time the markers that switched the transition on are gone
    /// and the element may compute a duration that says nothing about the run that happened;
    /// failing on that would report a defect where there is only a stylesheet whose
    /// transition is conditional. A comma-separated list is read at its longest, because one
    /// run spans every property that ran within it.
    /// </para>
    /// <para>
    /// A run a cancellation closed is not checked against a declaration at all. Cancelling a
    /// transition is what makes it stop early, so such a run is shorter than its declaration
    /// for the reason it was cancelled and never for the reason this layer exists to find —
    /// and checking it would fail both legs of two identical timelines while naming no
    /// difference between them. It is still compared across the legs, where the two lengths
    /// are a fact about the cancellations and need no declaration to state.
    /// </para>
    /// <para>
    /// A leg that overran is reported even when the other leg overran identically, which is
    /// where this layer parts company with L2. An L2 obligation is a statement about what the
    /// component did, and one React breaks too is not a Blazix defect. An overrun is a
    /// statement about the measurement itself: a leg that contradicts the stylesheet it was
    /// measured against has no usable duration for that step, and the other leg's number came
    /// off the same clock. Silence there would leave the cross-leg record below as the only
    /// output, reporting numbers from a basis known to be broken with nothing saying so. That
    /// holds only because every run this reaches did complete against the declaration it is
    /// measured against: a cancelled run and a run whose end was never observed are excluded
    /// above, and a run is measured against its own kind's declaration rather than against
    /// whatever else the element happens to declare. The one gap left in it is written up on
    /// <see cref="Duration"/> — a keyframe animation that repeats runs to a multiple of what
    /// it declares, and the iteration count is not captured.
    /// </para>
    /// </remarks>
    /// <param name="context">The paired step.</param>
    /// <param name="path">The animating node.</param>
    /// <returns>One finding per overrunning run per leg, plus the cross-leg records.</returns>
    private static IEnumerable<Finding> CompareSpans(ComparisonContext context, string path)
    {
        foreach (var family in Families)
        {
            foreach (var finding in CompareSpans(context, path, family))
            {
                yield return finding;
            }
        }
    }

    /// <summary>Runs <see cref="CompareSpans(ComparisonContext, string)"/> for one kind of run.</summary>
    /// <param name="context">The paired step.</param>
    /// <param name="path">The animating node.</param>
    /// <param name="family">Which kind of run to measure, and what to measure it against.</param>
    /// <returns>One finding per overrunning run per leg, plus the cross-leg records.</returns>
    private static IEnumerable<Finding> CompareSpans(
        ComparisonContext context, string path, RunFamily family)
    {
        var reference = Spans(context.Reference.Timeline, path, family);
        var candidate = Spans(context.Candidate.Timeline, path, family);
        var referenceDeclared = Declared(context.Reference.Styles, path, family.Duration);
        var candidateDeclared = Declared(context.Candidate.Styles, path, family.Duration);

        // Every run, not merely the first or the last: a step that opens cleanly and closes
        // in triple the declared time is a defect in the close, and one measurement covering
        // both would report the average of a run that was right and a run that was wrong.
        for (var i = 0; i < reference.Count; i++)
        {
            if (!reference[i].Cancelled
                && referenceDeclared is { } declared
                && Overruns(reference[i], declared))
            {
                yield return Overrun(
                    context, path, "React", reference[i], declared, family.Duration,
                    At(reference, i), At(candidate, i));
            }
        }

        for (var i = 0; i < candidate.Count; i++)
        {
            if (!candidate[i].Cancelled
                && candidateDeclared is { } declared
                && Overruns(candidate[i], declared))
            {
                yield return Overrun(
                    context, path, "Blazor", candidate[i], declared, family.Duration,
                    At(reference, i), At(candidate, i));
            }
        }

        for (var i = 0; i < Math.Min(reference.Count, candidate.Count); i++)
        {
            // Two runs that started at the same millisecond and lasted the same time have no
            // delta to carry, so nothing is recorded for them. Compared on the two numbers
            // the message prints and not on the whole run: one leg cancelling where the other
            // ended, at the same millisecond, is a difference L1 reports in full, and stating
            // it here as spans that "differ by 0 ms" would say nothing twice.
            if (reference[i].Start == candidate[i].Start && reference[i].Length == candidate[i].Length)
            {
                continue;
            }

            var left = reference[i];
            var right = candidate[i];

            yield return new Finding
            {
                Fixture = context.Fixture,
                Leg = context.Leg,
                Step = context.Step,
                Kind = FindingKind.Timeline,
                // Never an error: this is the number a reader wants when a fixture feels
                // slow, and the cross-leg comparison it comes from is exactly the one that
                // must not decide a verdict.
                Severity = Severity.Info,
                NodePath = path,
                Property = family.Duration,
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
    }

    /// <summary>Builds the finding for a run that does not match its own declaration.</summary>
    /// <param name="context">The paired step.</param>
    /// <param name="path">The animating node.</param>
    /// <param name="leg">Which leg overran, named as the messages name it.</param>
    /// <param name="run">That leg's measured run.</param>
    /// <param name="declared">That leg's declared duration, as the capture spelled it.</param>
    /// <param name="property">The style property that declaration was read from.</param>
    /// <param name="reference">React's run at the same index, for the compared values.</param>
    /// <param name="candidate">Blazor's run at the same index, for the compared values.</param>
    /// <returns>The finding.</returns>
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
            Fixture = context.Fixture,
            Leg = context.Leg,
            Step = context.Step,
            Kind = FindingKind.Timeline,
            Severity = Severity.Error,
            NodePath = path,
            Property = property,
            ReferenceValue = reference is { } left ? Milliseconds(left.Length) : null,
            CandidateValue = candidate is { } right ? Milliseconds(right.Length) : null,
            // The start is named because one step can hold several runs on one node, and two
            // of them breaking the same declaration by the same amount would otherwise
            // produce two findings a reader cannot tell apart.
            Message =
                $"Animation duration differs from its own declaration at '{path}': "
                + $"{leg} ran for {Milliseconds(run.Length)} ms starting at {Milliseconds(run.Start)} ms "
                + $"against a declared '{declared}'."
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

        // Past the snapshot check the node is provably gone, so something removed it and
        // "nothing did" is the one answer left that the recording rules out. A removal
        // reports the root of the departing subtree and nothing under it, so an animating
        // node carried away by its portal container is recorded under the container's tag —
        // which is the ordinary shape of a close, not a corner case.
        if (matching.Count == 0)
        {
            return new Removal(RemovalKind.Ambiguous, 0);
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

    /// <summary>Measures each of one leg's runs of one kind on one node.</summary>
    /// <remarks>
    /// <para>
    /// One step can hold several runs on one node — a popup that opens and closes, a tooltip
    /// hovered in and out, a reposition that cancels and restarts a transform mid-open — and
    /// the idle time between two runs belongs to neither of them. A start reopens the
    /// measurement once the run in progress has closed, and also once that run has already
    /// seen the same property start, which is the recording's only sign that a run ended
    /// unobserved. A start of a property the open run has not seen is a second property
    /// joining it, which the run already spans.
    /// </para>
    /// <para>
    /// Only one kind of run at a time, because a transition and a keyframe animation on one
    /// node are two runs however much they overlap: walked in one pass they would open on
    /// whichever started first and close on whichever ended last, reporting one span where
    /// there were two. See <see cref="Families"/>.
    /// </para>
    /// </remarks>
    /// <param name="timeline">The leg's events.</param>
    /// <param name="path">The animating node.</param>
    /// <param name="family">Which kind of run to measure.</param>
    /// <returns>
    /// The runs in the order they happened, each carrying whether a cancellation closed it. A
    /// start with no terminal event after it contributes nothing — a step captured mid-run is
    /// not a measurement, and neither is a run the next one interrupted.
    /// </returns>
    private static List<Run> Spans(IReadOnlyList<TimelineEvent> timeline, string path, RunFamily family)
    {
        var runs = new List<Run>();
        var started = new HashSet<string>(StringComparer.Ordinal);
        int? start = null;

        // Whether a run was cancelled is a fact about the terminal event that closed it, so
        // the two are carried as one value: held apart, the flag outlives the terminal it
        // came from and has to be cleared in step with it everywhere the run is reset.
        (int T, bool Cancelled)? terminal = null;

        foreach (var recorded in timeline)
        {
            if (!On(recorded, path))
            {
                continue;
            }

            if (family.Starts.Contains(recorded.Kind, StringComparer.Ordinal))
            {
                // Two things close the run in progress. One is its terminal event, which
                // makes it a measurement. The other is this same property starting again
                // while the run is still open: a property cannot start twice inside one run,
                // so the run it first started in ended without the recording seeing it end.
                // That one is dropped rather than measured — carrying its start forward
                // would run it on to the *next* run's terminal and report the two as one
                // span, which is what the returns note above rules out. A start of a
                // different property is the second property joining the run, which the run
                // already spans.
                if (start is { } reopened && (terminal is not null || started.Contains(Property(recorded))))
                {
                    if (terminal is { } reclosed)
                    {
                        runs.Add(new Run(reopened, reclosed.T - reopened, reclosed.Cancelled));
                    }

                    start = null;
                    terminal = null;
                    started.Clear();
                }

                started.Add(Property(recorded));
                start ??= recorded.T;
            }
            else if (start is not null && family.Terminals.Contains(recorded.Kind, StringComparer.Ordinal))
            {
                // The last one, so a node transitioning several properties is measured over
                // all of them rather than to whichever finished first.
                terminal = (recorded.T, CancelKinds.Contains(recorded.Kind, StringComparer.Ordinal));
            }
        }

        if (start is { } opened && terminal is { } closed)
        {
            runs.Add(new Run(opened, closed.T - opened, closed.Cancelled));
        }

        return runs;
    }

    /// <summary>Reads one leg's run at an index, when it recorded one there.</summary>
    /// <param name="runs">That leg's runs.</param>
    /// <param name="index">Which run.</param>
    /// <returns>The run, or <see langword="null"/> when that leg ran fewer.</returns>
    private static Run? At(IReadOnlyList<Run> runs, int index)
        => index < runs.Count ? runs[index] : null;

    /// <summary>Reads a leg's declared duration for one node and one kind of run.</summary>
    /// <param name="styles">That leg's captured styles.</param>
    /// <param name="path">The animating node.</param>
    /// <param name="property">The style property that kind of run is measured against.</param>
    /// <returns>The declaration as captured, or <see langword="null"/> when there is none.</returns>
    private static string? Declared(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> styles,
        string path,
        string property)
        => styles.TryGetValue(path, out var node) && node.TryGetValue(property, out var declared)
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
    /// Reads a duration declaration as milliseconds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A list is read at its longest. Anything that does not parse whole — a value with no
    /// unit, an empty declaration, a list with one part this does not understand — reads as
    /// no declaration at all, so a value this cannot measure is left unchecked rather than
    /// checked against a number it guessed.
    /// </para>
    /// <para>
    /// <c>animation-duration</c> declares one iteration and not the whole run, so a keyframe
    /// animation set to repeat a fixed number of times runs to a multiple of what this
    /// returns and is reported as breaking its own declaration on both legs alike.
    /// <c>animation-iteration-count</c> is not in <c>capture.js</c>'s style allowlist, so the
    /// count cannot be read and one iteration is assumed — which is what a component's enter
    /// and exit animation is. The endlessly repeating kind, a spinner, never reaches
    /// <c>animationend</c>, so it closes no run and is never measured here at all.
    /// </para>
    /// <para>
    /// A second, rarer route to that same signature, with the same mitigation:
    /// <c>animation-delay</c> is not in the allowlist either. A <em>negative</em> delay starts a
    /// keyframe animation already part way through, so <c>animationstart</c> fires at once and
    /// the run spans the declared duration less the delay — which <see cref="Overruns"/>,
    /// comparing an absolute distance, reports as a symmetric <em>under</em>run on two
    /// byte-identical legs. No recent change introduced this and there is no regression to hunt
    /// for: <c>transition-delay</c> <em>is</em> captured but is never read here, so a negative
    /// one has always done the same to a transition run.
    /// </para>
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
    /// <param name="Cancelled">
    /// Whether the terminal event that closed it was a cancellation, which is what makes its
    /// length a fact about the interruption rather than about the declaration.
    /// </param>
    private readonly record struct Run(int Start, int Length, bool Cancelled);

    /// <summary>Which removal event an animating node's is.</summary>
    /// <param name="Kind">Whether it could be decided.</param>
    /// <param name="Index">Where it sits in the timeline, when it could.</param>
    private readonly record struct Removal(RemovalKind Kind, int Index);

    /// <summary>One kind of run: how it opens, how it closes, what it is measured against.</summary>
    /// <param name="Starts">The events that open a run of this kind.</param>
    /// <param name="Terminals">The events that close one, cancellations included.</param>
    /// <param name="Duration">The style property a run of this kind is measured against.</param>
    private sealed record RunFamily(
        IReadOnlyList<string> Starts, IReadOnlyList<string> Terminals, string Duration);

    /// <summary>What one leg's capture contributes to the phase and duration layers.</summary>
    /// <param name="Timeline">The recorded events.</param>
    /// <param name="Present">The paths the step ended with.</param>
    /// <param name="Animating">The paths that ran a transition or animation.</param>
    private sealed record Leg(
        IReadOnlyList<TimelineEvent> Timeline,
        IReadOnlySet<string> Present,
        IReadOnlyList<string> Animating);
}
