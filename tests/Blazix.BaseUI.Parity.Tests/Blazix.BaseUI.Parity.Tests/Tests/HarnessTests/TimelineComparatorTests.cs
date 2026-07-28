using System.Globalization;
using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Diff;
using Shouldly;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

/// <summary>
/// Pins the animation timeline comparator: the timestamp-erased signature, the sequence
/// diff, the four phase invariants, and the per-side duration check.
/// </summary>
public sealed class TimelineComparatorTests
{
    /// <summary>The path a portalled popup is captured under.</summary>
    private const string Popup = "portal(1) > div[role=dialog]";

    /// <summary>A second animating node, so tag-based removal attribution can be exercised.</summary>
    private const string Backdrop = "portal(1) > div";

    /// <summary>The label a portalled root is captured under, which carries no tag.</summary>
    private const string Portal = "portal(1)";

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> NoText =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>> NoNumbers =
        new Dictionary<string, IReadOnlyDictionary<string, double>>(StringComparer.Ordinal);

    [Fact]
    public void IgnoresTimestampDifferences()
    {
        // Collection expressions cannot be assigned to `var` — the element type
        // must be stated explicitly.
        TimelineEvent[] fast = [Event(0, "attribute", "data-starting-style"), Event(16, "transitionend", null)];
        TimelineEvent[] slow = [Event(0, "attribute", "data-starting-style"), Event(340, "transitionend", null)];

        TimelineSequence.Normalize(fast).ShouldBe(TimelineSequence.Normalize(slow));
    }

    [Fact]
    public void DetectsAMissingEndingStylePhase()
    {
        var animated = new[]
        {
            Event(0, "attribute", "data-ending-style"),
            Event(120, "transitionend", null),
            Event(121, "removed", null)
        };
        var instant = new[] { Event(0, "removed", null) };

        TimelineSequence.Normalize(animated).ShouldNotBe(TimelineSequence.Normalize(instant));
    }

    [Fact]
    public void NormalizesAnEmptyTimelineToNoSignatures()
    {
        TimelineSequence.Normalize([]).ShouldBeEmpty();
    }

    [Fact]
    public void KeepsEventsThatCarryNoAttributeName()
    {
        // The attribute allowlist is scoped to attribute mutations. Applied to every kind
        // it would drop the lifecycle and transition events — whose `attr` is null or a
        // CSS property name — and leave the phase sequence of an animation empty.
        TimelineEvent[] timeline =
        [
            Run(0, "transitionstart", Popup),
            Run(120, "transitionend", Popup),
            Removed(121)
        ];

        TimelineSequence.Normalize(timeline).Count.ShouldBe(3);
    }

    [Fact]
    public void DropsAnAttributeOutsideTheAllowlist()
    {
        TimelineEvent[] timeline =
        [
            Attribute(0, Popup, "aria-hidden", null, "true"),
            Attribute(1, Popup, "data-open", null, string.Empty)
        ];

        TimelineSequence.Normalize(timeline).ShouldBe([$"attribute:{Popup}:data-open:"]);
    }

    [Fact]
    public void DistinguishesSettingAnAttributeFromRemovingIt()
    {
        // A marker set to the empty string and the same marker removed are the two halves
        // of an open/close pair. Rendering an absent value as an empty one would fold them
        // together and report parity between a popup that opened and one that closed.
        TimelineEvent[] opened = [Attribute(0, Popup, "data-open", null, string.Empty)];
        TimelineEvent[] closed = [Attribute(0, Popup, "data-open", string.Empty, null)];

        TimelineSequence.Normalize(opened).ShouldNotBe(TimelineSequence.Normalize(closed));
    }

    [Fact]
    public void KeepsRemovedNodesApartByTheirTag()
    {
        // A removal carries no path — capture.js cannot compute one for a node that has
        // already left the tree — so the tag it does carry is the only identity there is.
        TimelineEvent[] popup = [Removed(0, "div")];
        TimelineEvent[] label = [Removed(0, "span")];

        TimelineSequence.Normalize(popup).ShouldNotBe(TimelineSequence.Normalize(label));
    }

    [Fact]
    public void CountsConsecutiveRemovalsRatherThanCollapsingThem()
    {
        // Two removals of the same tag are two nodes, and every removal in a step
        // normalizes to the same signature. Collapsing them would report parity between a
        // dialog that unmounted its backdrop and its popup and one that unmounted only the
        // popup.
        TimelineEvent[] both = [Removed(0), Removed(1)];
        TimelineEvent[] one = [Removed(0)];

        TimelineSequence.Normalize(both).ShouldNotBe(TimelineSequence.Normalize(one));
    }

    [Fact]
    public void CollapsesAConsecutiveDuplicateAttributeWrite()
    {
        // The same value written twice in a row is a redundant write, not a phase.
        TimelineEvent[] timeline =
        [
            Attribute(0, Popup, "data-open", null, string.Empty),
            Attribute(1, Popup, "data-open", string.Empty, string.Empty)
        ];

        TimelineSequence.Normalize(timeline).Count.ShouldBe(1);
    }

    [Fact]
    public void KeepsARepeatedAttributeWriteThatIsNotConsecutive()
    {
        TimelineEvent[] timeline =
        [
            Attribute(0, Popup, "data-open", null, string.Empty),
            Attribute(1, Popup, "data-side", null, "top"),
            Attribute(2, Popup, "data-open", string.Empty, string.Empty)
        ];

        TimelineSequence.Normalize(timeline).Count.ShouldBe(3);
    }

    [Fact]
    public void ReportsNothingWhenNeitherLegRecordedATimeline()
    {
        // Every step whose manifest entry does not settle on "animation" looks like this.
        Compare(Context(Capture([]), Capture([]))).ShouldBeEmpty();
    }

    [Fact]
    public void ReportsNothingWhenTheSequencesMatchDespiteTheirTimestamps()
    {
        TimelineEvent[] fast =
        [
            Attribute(0, Popup, "data-open", null, string.Empty),
            Attribute(16, Popup, "data-starting-style", string.Empty, null)
        ];
        TimelineEvent[] slow =
        [
            Attribute(140, Popup, "data-open", null, string.Empty),
            Attribute(480, Popup, "data-starting-style", string.Empty, null)
        ];

        Compare(Context(Capture(fast), Capture(slow))).ShouldBeEmpty();
    }

    [Fact]
    public void ReportsASequenceDifferenceAsOneUnifiedDiff()
    {
        TimelineEvent[] animated =
        [
            Attribute(0, Popup, "data-ending-style", null, string.Empty),
            Run(120, "transitionend", Popup),
            Removed(121)
        ];
        TimelineEvent[] instant = [Removed(0)];

        var finding = Sequence(Compare(Context(Capture(animated), Capture(instant)))).ShouldHaveSingleItem();

        finding.Kind.ShouldBe(FindingKind.Timeline);
        finding.Severity.ShouldBe(Severity.Error);
        // The whole message, so the headline counts and the marker column are pinned
        // rather than merely present.
        finding.Message.ShouldBe(string.Join(
            '\n',
            "Animation timeline differs: 0 events added, 2 events removed.",
            $"-attribute:{Popup}:data-ending-style:",
            $"-transitionend:{Popup}:opacity:<absent>",
            " removed:::div"));
    }

    [Fact]
    public void ReportsOneSequenceFindingHoweverManyEventsDiffer()
    {
        TimelineEvent[] reference =
        [
            Attribute(0, Popup, "data-open", null, string.Empty),
            Attribute(1, Popup, "data-side", null, "top"),
            Run(2, "transitionstart", Popup),
            Run(120, "transitionend", Popup)
        ];

        Sequence(Compare(Context(Capture(reference), Capture([])))).ShouldHaveSingleItem();
    }

    [Fact]
    public void ReportsAMissingRemovalThatCollapsingWouldHide()
    {
        // React unmounts the backdrop and the popup; Blazor unmounts one node. Both
        // removals carry the same tag and no path, so the difference survives only
        // because equal signatures are not folded together.
        TimelineEvent[] both = [Run(0, "transitionend", Popup), Removed(1), Removed(1)];
        TimelineEvent[] one = [Run(0, "transitionend", Popup), Removed(1)];

        Sequence(Compare(Context(Capture(both), Capture(one)))).ShouldHaveSingleItem();
    }

    [Fact]
    public void SaysWhenALegRecordedNoAnimationEventsAtAll()
    {
        TimelineEvent[] reference = [Run(0, "transitionstart", Popup), Run(120, "transitionend", Popup)];

        var finding = Sequence(Compare(Context(Capture(reference), Capture([])))).ShouldHaveSingleItem();

        finding.Message.Split('\n')[0]
            .ShouldBe("Blazor recorded no animation events; React recorded 2 events.");
    }

    [Fact]
    public void SaysWhenTheReactLegRecordedNoAnimationEventsAtAll()
    {
        TimelineEvent[] candidate = [Run(0, "transitionstart", Popup)];

        var finding = Sequence(Compare(Context(Capture([]), Capture(candidate)))).ShouldHaveSingleItem();

        finding.Message.Split('\n')[0]
            .ShouldBe("React recorded no animation events; Blazor recorded 1 event.");
    }

    [Theory]
    // Forty-one diff lines, so exactly one is dropped, and fifty, so ten are.
    [InlineData(21, 20, "... 1 line of the diff omitted.")]
    [InlineData(25, 25, "... 10 lines of the diff omitted.")]
    public void CapsATimelineWithNothingInCommon(int reference, int candidate, string tail)
    {
        var findings = Compare(Context(Capture(Noise("r", reference)), Capture(Noise("c", candidate))));
        var lines = findings.ShouldHaveSingleItem().Message.Split('\n');

        // The headline, the capped body, and the count of what the cap dropped.
        lines.Length.ShouldBe(42);
        // Counted before the cap, so truncation cannot hide the scale of the difference.
        lines[0].ShouldBe(
            $"Animation timeline differs: {candidate} events added, {reference} events removed.");
        lines[^1].ShouldBe(tail);
    }

    [Fact]
    public void CarriesBothNormalizedSequencesAsTheComparedValues()
    {
        TimelineEvent[] reference = [Attribute(0, Popup, "data-open", null, string.Empty)];
        TimelineEvent[] candidate = [Attribute(0, Popup, "data-closed", null, string.Empty)];

        var finding = Sequence(Compare(Context(Capture(reference), Capture(candidate)))).ShouldHaveSingleItem();

        finding.ReferenceValue.ShouldBe($"attribute:{Popup}:data-open:");
        finding.CandidateValue.ShouldBe($"attribute:{Popup}:data-closed:");
    }

    [Fact]
    public void ReportsAMountThatFollowedTheTransitionStart()
    {
        // base-ui mounts the popup, then lets the starting styles transition off it. A leg
        // that inserts the node after the run has begun animates from the wrong state.
        TimelineEvent[] reference =
        [
            Added(0, Popup),
            Run(1, "transitionstart", Popup),
            Run(120, "transitionend", Popup)
        ];
        TimelineEvent[] candidate =
        [
            Run(1, "transitionstart", Popup),
            Added(2, Popup),
            Run(120, "transitionend", Popup)
        ];

        var findings = Compare(Context(Capture(reference, present: [Popup]), Capture(candidate, present: [Popup])));
        var finding = Invariant(findings, "mounted-before-transition-start").ShouldHaveSingleItem();

        finding.Severity.ShouldBe(Severity.Error);
        finding.NodePath.ShouldBe(Popup);
        finding.ReferenceValue.ShouldBe("satisfied");
        finding.CandidateValue.ShouldBe("violated");
        finding.Message.ShouldBe(
            $"Animation invariant 'mounted-before-transition-start' differs at '{Popup}': " +
            "React satisfied it; Blazor violated it.");

        // The other three invariants hold on both legs, so this one names itself alone.
        Invariant(findings, "present-at-transitionend").ShouldBeEmpty();
        Invariant(findings, "data-open-flipped-before-starting-style-cleared").ShouldBeEmpty();
        Invariant(findings, "removed-after-transitionend").ShouldBeEmpty();
    }

    [Fact]
    public void ReportsANodeThatLeftBeforeItsTransitionEnded()
    {
        // The realistic shape of an early unmount: the run starts, the node is removed,
        // and no terminal event is ever observed because a detached node's transitioncancel
        // never reaches the document listener. Two obligations are broken at once — the
        // node was not there at the end, and it was removed before one — so both are named.
        TimelineEvent[] reference =
        [
            Run(0, "transitionstart", Popup),
            Run(120, "transitionend", Popup),
            Removed(121)
        ];
        TimelineEvent[] candidate = [Run(0, "transitionstart", Popup), Removed(10)];

        var findings = Compare(Context(Capture(reference), Capture(candidate)));

        var present = Invariant(findings, "present-at-transitionend").ShouldHaveSingleItem();
        present.Severity.ShouldBe(Severity.Error);
        present.Message.ShouldBe(
            $"Animation invariant 'present-at-transitionend' differs at '{Popup}': " +
            "React satisfied it; Blazor violated it.");

        Invariant(findings, "removed-after-transitionend").ShouldHaveSingleItem()
            .Severity.ShouldBe(Severity.Error);
        Invariant(findings, "mounted-before-transition-start").ShouldBeEmpty();
        Invariant(findings, "data-open-flipped-before-starting-style-cleared").ShouldBeEmpty();
    }

    [Fact]
    public void ReportsAStartingStyleClearedBeforeDataOpenWasFlipped()
    {
        TimelineEvent[] reference =
        [
            Attribute(0, Popup, "data-open", null, string.Empty),
            Attribute(1, Popup, "data-starting-style", string.Empty, null),
            Run(2, "transitionstart", Popup),
            Run(120, "transitionend", Popup)
        ];
        TimelineEvent[] candidate =
        [
            Attribute(0, Popup, "data-starting-style", string.Empty, null),
            Attribute(1, Popup, "data-open", null, string.Empty),
            Run(2, "transitionstart", Popup),
            Run(120, "transitionend", Popup)
        ];

        var findings = Compare(Context(Capture(reference, present: [Popup]), Capture(candidate, present: [Popup])));
        var finding = Invariant(findings, "data-open-flipped-before-starting-style-cleared")
            .ShouldHaveSingleItem();

        finding.Severity.ShouldBe(Severity.Error);
        finding.Message.ShouldBe(
            "Animation invariant 'data-open-flipped-before-starting-style-cleared' differs at " +
            $"'{Popup}': React satisfied it; Blazor violated it.");

        Invariant(findings, "mounted-before-transition-start").ShouldBeEmpty();
        Invariant(findings, "present-at-transitionend").ShouldBeEmpty();
        Invariant(findings, "removed-after-transitionend").ShouldBeEmpty();
    }

    [Fact]
    public void ReportsARemovalOrderedBeforeTheTransitionEnd()
    {
        // Synthetic: a real capture cannot hold a terminal event after the removal of the
        // node it fired on, so this shape exists only to pin the ordering comparison that
        // the reachable case — removed with no terminal event at all — reports through the
        // same invariant.
        TimelineEvent[] reference =
        [
            Run(0, "transitionstart", Popup),
            Run(120, "transitionend", Popup),
            Removed(121)
        ];
        TimelineEvent[] candidate =
        [
            Run(0, "transitionstart", Popup),
            Removed(60),
            Run(120, "transitionend", Popup)
        ];

        var findings = Compare(Context(Capture(reference), Capture(candidate)));

        Invariant(findings, "removed-after-transitionend").ShouldHaveSingleItem()
            .Severity.ShouldBe(Severity.Error);
        // The run still reached a terminal event on both legs, so presence is untouched.
        Invariant(findings, "present-at-transitionend").ShouldBeEmpty();
    }

    [Fact]
    public void ReportsNothingWhenBothLegsBreakTheSameInvariant()
    {
        // A parity harness reports differences from React, not absolute correctness: an
        // obligation React breaks too is not a Blazix defect.
        TimelineEvent[] timeline =
        [
            Run(0, "transitionstart", Popup),
            Added(1, Popup),
            Run(120, "transitionend", Popup)
        ];

        Compare(Context(Capture(timeline, present: [Popup]), Capture(timeline, present: [Popup])))
            .ShouldBeEmpty();
    }

    [Fact]
    public void SaysSoRatherThanPassingWhenARemovalCannotBeAttributed()
    {
        // Two nodes of the same tag left the tree and a removal carries no path, so which
        // one this popup's removal is cannot be decided. Reporting the invariant as
        // satisfied would be a silent pass; it is reported as undecided instead.
        TimelineEvent[] reference =
        [
            Run(0, "transitionstart", Popup),
            Run(120, "transitionend", Popup),
            Removed(121),
            Removed(121)
        ];
        TimelineEvent[] candidate =
        [
            Run(0, "transitionstart", Popup),
            Run(120, "transitionend", Popup),
            Removed(121)
        ];

        var findings = Compare(Context(Capture(reference), Capture(candidate)));
        var finding = Invariant(findings, "removed-after-transitionend").ShouldHaveSingleItem();

        finding.Severity.ShouldBe(Severity.Info);
        finding.ReferenceValue.ShouldBe("not evaluated");
        finding.CandidateValue.ShouldBe("satisfied");
        finding.Message.ShouldStartWith(
            $"Animation invariant 'removed-after-transitionend' was not decided at '{Popup}': " +
            "React could not be evaluated; Blazor satisfied it.");
    }

    [Fact]
    public void SaysSoRatherThanPassingWhenARunNeverFinished()
    {
        // Blazor's opacity transition has no terminal event and nothing left the tree, so
        // the step may have been captured while the run was still going. Reading that as
        // the node having been present at the end would pass a leg nobody measured.
        TimelineEvent[] reference = [Run(0, "transitionstart", Popup), Run(120, "transitionend", Popup)];
        TimelineEvent[] candidate = [Run(0, "transitionstart", Popup)];

        var findings = Compare(Context(
            Capture(reference, present: [Popup]), Capture(candidate, present: [Popup])));
        var finding = Invariant(findings, "present-at-transitionend").ShouldHaveSingleItem();

        finding.Severity.ShouldBe(Severity.Info);
        finding.ReferenceValue.ShouldBe("satisfied");
        finding.CandidateValue.ShouldBe("not evaluated");
    }

    [Fact]
    public void JudgesPhasesOnlyWhereBothLegsAnimated()
    {
        // Blazor ran no animation at all on this node. That is the sequence diff and
        // nothing more: holding a leg that never animated to an animation's obligations
        // states a conclusion about it the capture does not support.
        TimelineEvent[] reference =
        [
            Run(0, "transitionstart", Popup),
            Run(120, "transitionend", Popup),
            Removed(121)
        ];
        TimelineEvent[] candidate = [Removed(0)];

        var finding = Compare(Context(Capture(reference), Capture(candidate))).ShouldHaveSingleItem();

        finding.Severity.ShouldBe(Severity.Error);
        finding.Property.ShouldBeEmpty();
    }

    [Fact]
    public void DoesNotAttributeARemovalToANodeStillPresentAtCapture()
    {
        // The removal belongs to some other node: this one is in the snapshot the step
        // ended on, so it cannot be what left.
        TimelineEvent[] reference = [Run(0, "transitionstart", Popup), Run(120, "transitionend", Popup)];
        TimelineEvent[] candidate = [Run(0, "transitionstart", Popup), Removed(60), Run(120, "transitionend", Popup)];

        var stillThere = Compare(Context(
            Capture(reference, present: [Popup]), Capture(candidate, present: [Popup])));
        var gone = Compare(Context(Capture(reference, present: [Popup]), Capture(candidate)));

        Invariant(stillThere, "removed-after-transitionend").ShouldBeEmpty();

        // React's node is in the snapshot the step ended on and its recording holds no
        // removal at all, which is evidence that it was not removed rather than an absence
        // of evidence — so this is a decided difference and fails the run.
        var finding = Invariant(gone, "removed-after-transitionend").ShouldHaveSingleItem();
        finding.Severity.ShouldBe(Severity.Error);
        finding.ReferenceValue.ShouldBe("satisfied");
        finding.CandidateValue.ShouldBe("violated");
    }

    [Fact]
    public void SaysSoRatherThanPassingWhenTheRemovalCarriesAnotherNodesTag()
    {
        // MutationObserver reports the root of a removed subtree and nothing under it
        // (capture.js:284-287), so a popup unmounted with its portal container records the
        // *ancestor's* tag and never the popup's. The node is provably gone — it is absent
        // from the snapshot the step ended on — so something removed it, and "nothing that
        // could be this node was removed" is the one answer the recording rules out.
        TimelineEvent[] reference =
        [
            Run(0, "transitionstart", Popup),
            Run(120, "transitionend", Popup),
            Removed(121)
        ];
        TimelineEvent[] candidate =
        [
            Run(0, "transitionstart", Popup),
            Run(10, "transitionend", Popup),
            Removed(11, "section")
        ];

        var findings = Compare(Context(Capture(reference), Capture(candidate)));
        var finding = Invariant(findings, "removed-after-transitionend").ShouldHaveSingleItem();

        finding.Severity.ShouldBe(Severity.Info);
        finding.ReferenceValue.ShouldBe("satisfied");
        finding.CandidateValue.ShouldBe("not evaluated");
    }

    [Fact]
    public void SaysSoRatherThanPassingWhenTheAnimatingNodeIsACaptureRoot()
    {
        // A portalled popup is captured as a root of its own, and a root's path is a label
        // rather than an element — there is no tag in it to match a removal's against. The
        // node is gone and something was removed, and which node that was cannot be said.
        TimelineEvent[] reference = [Run(0, "transitionstart", Portal), Run(120, "transitionend", Portal)];
        TimelineEvent[] candidate =
        [
            Run(0, "transitionstart", Portal),
            Run(120, "transitionend", Portal),
            Removed(121)
        ];

        var findings = Compare(Context(Capture(reference, present: [Portal]), Capture(candidate)));
        var finding = Invariant(findings, "removed-after-transitionend").ShouldHaveSingleItem();

        finding.Severity.ShouldBe(Severity.Info);
        finding.ReferenceValue.ShouldBe("satisfied");
        finding.CandidateValue.ShouldBe("not evaluated");
    }

    [Fact]
    public void FlagsALegThatOverranItsOwnDeclaredDuration()
    {
        TimelineEvent[] reference = [Run(0, "transitionstart", Popup), Run(300, "transitionend", Popup)];
        TimelineEvent[] candidate = [Run(0, "transitionstart", Popup), Run(900, "transitionend", Popup)];

        var findings = Compare(Context(
            Capture(reference, declared: "0.3s", present: [Popup]),
            Capture(candidate, declared: "0.3s", present: [Popup])));
        var finding = Errors(findings).ShouldHaveSingleItem();

        finding.Property.ShouldBe("transition-duration");
        finding.NodePath.ShouldBe(Popup);
        // The run's start is named as well as its length, because one step can hold several
        // runs on one node and two of them breaking the same declaration by the same amount
        // would otherwise produce two findings a reader cannot tell apart.
        finding.Message.ShouldBe(
            $"Animation duration differs from its own declaration at '{Popup}': " +
            "Blazor ran for 900 ms starting at 0 ms against a declared '0.3s'.");
    }

    [Fact]
    public void DoesNotFlagAStartTimeOffsetBetweenTheLegs()
    {
        // Blazor Server round trips shift when an animation starts without changing how
        // long it runs. Failing on that would fail every animated fixture on the slow leg.
        TimelineEvent[] reference = [Run(0, "transitionstart", Popup), Run(200, "transitionend", Popup)];
        TimelineEvent[] candidate = [Run(500, "transitionstart", Popup), Run(700, "transitionend", Popup)];

        var findings = Compare(Context(
            Capture(reference, declared: "0.2s", present: [Popup]),
            Capture(candidate, declared: "0.2s", present: [Popup])));

        Errors(findings).ShouldBeEmpty();
        findings.ShouldHaveSingleItem().Message.ShouldBe(
            $"Animation span at '{Popup}': React started at 0 ms and ran 200 ms (declared '0.2s'); " +
            "Blazor started at 500 ms and ran 200 ms (declared '0.2s'); the spans differ by 0 ms.");
    }

    [Fact]
    public void ReadsTheLongestOfSeveralDeclaredDurations()
    {
        // `transition: opacity 0.15s, transform 0.9s` computes as a list, and the span the
        // timeline measures runs from the first start to the last end.
        TimelineEvent[] timeline = [Run(0, "transitionstart", Popup), Run(900, "transitionend", Popup)];

        var findings = Compare(Context(
            Capture(timeline, declared: "0.15s, 0.9s", present: [Popup]),
            Capture(timeline, declared: "0.15s, 0.9s", present: [Popup])));

        Errors(findings).ShouldBeEmpty();
    }

    [Theory]
    // 900ms is 0.9s spelled the other way, and a bare number is not a duration at all.
    [InlineData("900ms", 0)]
    [InlineData("", 0)]
    [InlineData("900", 0)]
    // A declaration of no duration says nothing about a run that plainly happened: the
    // markers that switched the transition on are gone by the time the step is captured.
    [InlineData("0s", 0)]
    // And a declaration the styles never carried cannot be checked either.
    [InlineData(null, 0)]
    [InlineData("0.2s", 1)]
    public void ChecksASpanOnlyAgainstADeclarationThatCanCarryIt(string? declared, int errors)
    {
        TimelineEvent[] timeline = [Run(0, "transitionstart", Popup), Run(900, "transitionend", Popup)];

        var findings = Compare(Context(
            Capture(timeline, declared: declared, present: [Popup]),
            Capture(timeline, declared: declared, present: [Popup])));

        // Both legs declare and run the same thing, so an error on one is an error on both.
        Errors(findings).Count.ShouldBe(errors * 2);
    }

    [Theory]
    // A 20ms declaration with 50% of it as tolerance would fail on 11ms of jitter, so the
    // floor is what applies: 70ms is 50 over and inside it, 71ms is not.
    [InlineData(70, 0)]
    [InlineData(71, 1)]
    public void AppliesTheFloorToAShortDeclaration(int observed, int errors)
    {
        Overrun("0.02s", observed).Count.ShouldBe(errors);
    }

    [Theory]
    // Half of a one-second declaration is 500ms, well past the floor, so the relative
    // tolerance is what applies at this end.
    [InlineData(1500, 0)]
    [InlineData(1501, 1)]
    // The tolerance is two-sided: a run that finishes far too early is as much a
    // difference from the declaration as one that overruns it.
    [InlineData(500, 0)]
    [InlineData(499, 1)]
    public void AppliesTheRelativeToleranceToALongDeclaration(int observed, int errors)
    {
        Overrun("1s", observed).Count.ShouldBe(errors);
    }

    [Fact]
    public void MeasuresNoSpanWithoutBothEndsOfTheRun()
    {
        // A step captured while the run was still going has a start and no terminal event.
        // There is nothing to compare against the declaration, and the presence invariant
        // is undecided on both legs alike — which is a state they share rather than a
        // difference between them, so it is not reported either.
        TimelineEvent[] timeline = [Run(0, "transitionstart", Popup)];

        var findings = Compare(Context(
            Capture(timeline, declared: "0.2s", present: [Popup]),
            Capture(timeline, declared: "0.2s", present: [Popup])));

        findings.ShouldBeEmpty();
    }

    [Fact]
    public void MeasuresTheSpanToTheLastTerminalEventOnThePath()
    {
        // Two properties transition together and end at different times. The span the CSS
        // declares is the longer of them, so the last end is the one that closes the run.
        // Reading the first end instead would measure 150ms against a declared 300ms and
        // report both legs as breaking their own declaration.
        TimelineEvent[] reference =
        [
            Run(0, "transitionstart", Popup, "opacity"),
            Run(0, "transitionstart", Popup, "transform"),
            Run(150, "transitionend", Popup, "opacity"),
            Run(300, "transitionend", Popup, "transform")
        ];
        TimelineEvent[] candidate =
        [
            Run(0, "transitionstart", Popup, "opacity"),
            Run(0, "transitionstart", Popup, "transform"),
            Run(150, "transitionend", Popup, "opacity"),
            Run(305, "transitionend", Popup, "transform")
        ];

        var findings = Compare(Context(
            Capture(reference, declared: "0.3s", present: [Popup]),
            Capture(candidate, declared: "0.3s", present: [Popup])));

        Errors(findings).ShouldBeEmpty();
        findings.ShouldHaveSingleItem().Message.ShouldContain("React started at 0 ms and ran 300 ms");
    }

    [Fact]
    public void MeasuresEachRunSeparatelyRatherThanOneSpanAcrossThemAll()
    {
        // One step, two runs on one node: a popup that opens and closes, a tooltip hovered
        // in and out, or a reposition that cancels and restarts a transform mid-open. A
        // span taken from the first start to the last terminal event swallows the idle gap
        // between the runs, so two byte-identical timelines both read as breaking their own
        // declaration and the run fails on no difference at all.
        TimelineEvent[] timeline =
        [
            Run(0, "transitionstart", Popup),
            Run(200, "transitionend", Popup),
            Run(1000, "transitionstart", Popup),
            Run(1200, "transitionend", Popup)
        ];

        var findings = Compare(Context(
            Capture(timeline, declared: "0.2s", present: [Popup]),
            Capture(timeline, declared: "0.2s", present: [Popup])));

        findings.ShouldBeEmpty();
    }

    [Theory]
    // The first run overruns and the second is clean, then the other way round. Checking
    // only the first run, or only the last, lets one of these two through.
    [InlineData(1400, 2200, 1400, 0, "200")]
    [InlineData(200, 3200, 1200, 2000, "250")]
    public void ChecksEachRunAgainstTheDeclarationSeparately(
        int firstEnd, int secondEnd, int length, int start, string reacted)
    {
        // React's two runs are 200 ms and 250 ms, both inside a 0.2s declaration's 50 ms
        // floor, and different from each other — so the compared values pin that the
        // finding carries the runs at *its own* index rather than whichever came first.
        TimelineEvent[] reference =
        [
            Run(0, "transitionstart", Popup),
            Run(200, "transitionend", Popup),
            Run(2000, "transitionstart", Popup),
            Run(2250, "transitionend", Popup)
        ];
        TimelineEvent[] candidate =
        [
            Run(0, "transitionstart", Popup),
            Run(firstEnd, "transitionend", Popup),
            Run(2000, "transitionstart", Popup),
            Run(secondEnd, "transitionend", Popup)
        ];

        var findings = Compare(Context(
            Capture(reference, declared: "0.2s", present: [Popup]),
            Capture(candidate, declared: "0.2s", present: [Popup])));

        // Both of React's runs match its declaration, so only the one bad Blazor run fails,
        // and the message says which of the two it was.
        var finding = Errors(findings).ShouldHaveSingleItem();

        finding.Message.ShouldBe(
            $"Animation duration differs from its own declaration at '{Popup}': " +
            $"Blazor ran for {length} ms starting at {start} ms against a declared '0.2s'.");
        finding.ReferenceValue.ShouldBe(reacted);
        finding.CandidateValue.ShouldBe(length.ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void KeepsASecondPropertyStartingMidRunInsideTheSameRun()
    {
        // `transition: opacity 1s, transform 0.3s 0.7s` starts transform seven tenths of a
        // second into the run it belongs to, and does so again in the second run. Only a
        // start that follows a *closed* run opens a new one: reading either later start as
        // a run of its own would measure 300 ms against a declared second and fail both
        // legs of an animation that behaved.
        TimelineEvent[] timeline =
        [
            Run(0, "transitionstart", Popup, "opacity"),
            Run(700, "transitionstart", Popup, "transform"),
            Run(1000, "transitionend", Popup, "opacity"),
            Run(1000, "transitionend", Popup, "transform"),
            Run(3000, "transitionstart", Popup, "opacity"),
            Run(3700, "transitionstart", Popup, "transform"),
            Run(4000, "transitionend", Popup, "opacity"),
            Run(4000, "transitionend", Popup, "transform")
        ];

        var findings = Compare(Context(
            Capture(timeline, declared: "1s", present: [Popup]),
            Capture(timeline, declared: "1s", present: [Popup])));

        findings.ShouldBeEmpty();
    }

    [Fact]
    public void ClosesARunOnItsTerminalEvenWhereTheNextRunAnimatesAnotherProperty()
    {
        // The open transitions opacity and the close transitions transform, so the two runs
        // share no property. A terminal event closes a run on its own: waiting for a property
        // to start twice would read the transform start as one joining the open and measure a
        // single span from 0 ms across the idle gap to 1200 ms.
        TimelineEvent[] timeline =
        [
            Run(0, "transitionstart", Popup, "opacity"),
            Run(200, "transitionend", Popup, "opacity"),
            Run(1000, "transitionstart", Popup, "transform"),
            Run(1200, "transitionend", Popup, "transform")
        ];

        var findings = Compare(Context(
            Capture(timeline, declared: "0.2s", present: [Popup]),
            Capture(timeline, declared: "0.2s", present: [Popup])));

        findings.ShouldBeEmpty();
    }

    [Fact]
    public void DoesNotMeasureARunACancellationCutShort()
    {
        // A cancelled run is shorter than its declaration by definition — cutting it short is
        // what cancelling it means — so measuring it against that declaration fails both legs
        // of two byte-identical timelines while naming no difference between them. Any cancel
        // arriving before roughly half the declared duration produces that.
        TimelineEvent[] timeline = [Run(0, "transitionstart", Popup), Run(80, "transitioncancel", Popup)];

        var findings = Compare(Context(
            Capture(timeline, declared: "0.2s", present: [Popup]),
            Capture(timeline, declared: "0.2s", present: [Popup])));

        findings.ShouldBeEmpty();
    }

    [Fact]
    public void DropsACancelledRunAndKeepsTheOneThatReplacedIt()
    {
        // A reposition that cancels a transform mid-open and restarts it. The cancelled run
        // is not measurable; the run that replaced it is, and it matches its declaration.
        TimelineEvent[] timeline =
        [
            Run(0, "transitionstart", Popup),
            Run(80, "transitioncancel", Popup),
            Run(1000, "transitionstart", Popup),
            Run(1200, "transitionend", Popup)
        ];

        var findings = Compare(Context(
            Capture(timeline, declared: "0.2s", present: [Popup]),
            Capture(timeline, declared: "0.2s", present: [Popup])));

        findings.ShouldBeEmpty();
    }

    [Fact]
    public void StillChecksTheRunThatFollowedACancellation()
    {
        // The skip covers the cancelled run alone. The run that replaced it is measured like
        // any other, and the message names it by its own start rather than the cancelled
        // run's — which is also what proves the cancelled run was dropped and not merged.
        TimelineEvent[] reference =
        [
            Run(0, "transitionstart", Popup),
            Run(80, "transitioncancel", Popup),
            Run(1000, "transitionstart", Popup),
            Run(1200, "transitionend", Popup)
        ];
        TimelineEvent[] candidate =
        [
            Run(0, "transitionstart", Popup),
            Run(80, "transitioncancel", Popup),
            Run(1000, "transitionstart", Popup),
            Run(1900, "transitionend", Popup)
        ];

        var findings = Compare(Context(
            Capture(reference, declared: "0.2s", present: [Popup]),
            Capture(candidate, declared: "0.2s", present: [Popup])));

        Errors(findings).ShouldHaveSingleItem().Message.ShouldBe(
            $"Animation duration differs from its own declaration at '{Popup}': " +
            "Blazor ran for 900 ms starting at 1000 ms against a declared '0.2s'.");
    }

    [Fact]
    public void StillRecordsTheCrossLegDeltaBetweenTwoCancelledRuns()
    {
        // Not measuring a cancelled run against a declaration must not silence the cross-leg
        // record: a cancellation one leg took nearly four times as long to reach is exactly
        // the number a reader wants, and stating it needs no declaration at all.
        TimelineEvent[] reference = [Run(0, "transitionstart", Popup), Run(80, "transitioncancel", Popup)];
        TimelineEvent[] candidate = [Run(0, "transitionstart", Popup), Run(300, "transitioncancel", Popup)];

        var findings = Compare(Context(
            Capture(reference, declared: "0.2s", present: [Popup]),
            Capture(candidate, declared: "0.2s", present: [Popup])));

        Errors(findings).ShouldBeEmpty();
        findings.ShouldHaveSingleItem().Message.ShouldBe(
            $"Animation span at '{Popup}': React started at 0 ms and ran 80 ms (declared '0.2s'); " +
            "Blazor started at 0 ms and ran 300 ms (declared '0.2s'); the spans differ by 220 ms.");
    }

    [Fact]
    public void LeavesACancellationWhereTheOtherLegEndedToTheSequenceDiff()
    {
        // One leg cancels at the millisecond the other ends. The two spans are the same two
        // numbers, so L3 has no delta to state and states none — reporting spans that "differ
        // by 0 ms" would be a second, emptier telling of a difference L1 already prints in
        // full as transitionend against transitioncancel.
        TimelineEvent[] reference = [Run(0, "transitionstart", Popup), Run(200, "transitionend", Popup)];
        TimelineEvent[] candidate = [Run(0, "transitionstart", Popup), Run(200, "transitioncancel", Popup)];

        var findings = Compare(Context(
            Capture(reference, declared: "0.2s", present: [Popup]),
            Capture(candidate, declared: "0.2s", present: [Popup])));

        // The sequence diff alone, which is the finding whose Property is empty.
        findings.ShouldHaveSingleItem().Property.ShouldBeEmpty();
    }

    [Fact]
    public void DropsAStartThatNeverReachedATerminalEvent()
    {
        // The same property cannot start twice inside one run, so a second start while the
        // first is still open is proof that the first run ended without the recording seeing
        // it end. Carrying its start forward joins it to the next run's terminal — 0 ms to
        // 1200 ms as one span — and fails both legs of two byte-identical timelines.
        TimelineEvent[] timeline =
        [
            Run(0, "transitionstart", Popup),
            Run(1000, "transitionstart", Popup),
            Run(1200, "transitionend", Popup)
        ];

        var findings = Compare(Context(
            Capture(timeline, declared: "0.2s", present: [Popup]),
            Capture(timeline, declared: "0.2s", present: [Popup])));

        findings.ShouldBeEmpty();
    }

    [Fact]
    public void MeasuresAStrandedStartsSuccessorFromItsOwnStart()
    {
        // The surviving run is the one with both ends, and it is measured from the start that
        // opened it. Measuring from the stranded start instead would report 1900 ms here.
        TimelineEvent[] reference =
        [
            Run(0, "transitionstart", Popup),
            Run(1000, "transitionstart", Popup),
            Run(1200, "transitionend", Popup)
        ];
        TimelineEvent[] candidate =
        [
            Run(0, "transitionstart", Popup),
            Run(1000, "transitionstart", Popup),
            Run(1900, "transitionend", Popup)
        ];

        var findings = Compare(Context(
            Capture(reference, declared: "0.2s", present: [Popup]),
            Capture(candidate, declared: "0.2s", present: [Popup])));

        Errors(findings).ShouldHaveSingleItem().Message.ShouldBe(
            $"Animation duration differs from its own declaration at '{Popup}': " +
            "Blazor ran for 900 ms starting at 1000 ms against a declared '0.2s'.");
    }

    [Fact]
    public void RecordsTheCrossLegDeltaRunForRun()
    {
        // Both legs open and close, and only the close differs. Pairing the runs by index
        // keeps the delta on the run that carries it instead of averaging it across both.
        TimelineEvent[] reference =
        [
            Run(0, "transitionstart", Popup),
            Run(200, "transitionend", Popup),
            Run(2000, "transitionstart", Popup),
            Run(2200, "transitionend", Popup)
        ];
        TimelineEvent[] candidate =
        [
            Run(0, "transitionstart", Popup),
            Run(200, "transitionend", Popup),
            Run(2000, "transitionstart", Popup),
            Run(2280, "transitionend", Popup)
        ];

        var findings = Compare(Context(
            Capture(reference, declared: "0.2s", present: [Popup]),
            Capture(candidate, declared: "0.2s", present: [Popup])));

        Errors(findings).ShouldBeEmpty();
        findings.ShouldHaveSingleItem().Message.ShouldBe(
            $"Animation span at '{Popup}': React started at 2000 ms and ran 200 ms (declared '0.2s'); " +
            "Blazor started at 2000 ms and ran 280 ms (declared '0.2s'); the spans differ by 80 ms.");
    }

    [Fact]
    public void ReportsAPropertyThatStartedAndNeverEndedAsAPresenceBreak()
    {
        // The node left while its transform was still running: opacity reached its end and
        // transform did not, which a path-level "did anything end?" test would miss.
        TimelineEvent[] reference =
        [
            Run(0, "transitionstart", Popup, "opacity"),
            Run(0, "transitionstart", Popup, "transform"),
            Run(150, "transitionend", Popup, "opacity"),
            Run(300, "transitionend", Popup, "transform"),
            Removed(301)
        ];
        TimelineEvent[] candidate =
        [
            Run(0, "transitionstart", Popup, "opacity"),
            Run(0, "transitionstart", Popup, "transform"),
            Run(150, "transitionend", Popup, "opacity"),
            Removed(160)
        ];

        var findings = Compare(Context(Capture(reference), Capture(candidate)));

        Invariant(findings, "present-at-transitionend").ShouldHaveSingleItem()
            .Severity.ShouldBe(Severity.Error);
    }

    [Fact]
    public void KeepsAnimatingPathsApart()
    {
        // Two nodes animate; only one of them differs. The finding has to name that one.
        TimelineEvent[] reference =
        [
            Added(0, Backdrop),
            Run(1, "transitionstart", Backdrop),
            Run(120, "transitionend", Backdrop),
            Added(0, Popup),
            Run(1, "transitionstart", Popup),
            Run(120, "transitionend", Popup)
        ];
        TimelineEvent[] candidate =
        [
            Added(0, Backdrop),
            Run(1, "transitionstart", Backdrop),
            Run(120, "transitionend", Backdrop),
            Run(1, "transitionstart", Popup),
            Added(2, Popup),
            Run(120, "transitionend", Popup)
        ];

        var findings = Compare(Context(
            Capture(reference, present: [Popup, Backdrop]),
            Capture(candidate, present: [Popup, Backdrop])));

        Invariant(findings, "mounted-before-transition-start").ShouldHaveSingleItem()
            .NodePath.ShouldBe(Popup);
    }

    [Fact]
    public void OwnsOneKind()
    {
        new TimelineComparator().Kind.ShouldBe(FindingKind.Timeline);
    }

    [Fact]
    public void EveryFindingCarriesTheFixtureLegAndStep()
    {
        TimelineEvent[] reference =
        [
            Added(0, Popup),
            Run(1, "transitionstart", Popup),
            Run(300, "transitionend", Popup)
        ];
        TimelineEvent[] candidate =
        [
            Run(1, "transitionstart", Popup),
            Added(2, Popup),
            Run(900, "transitionend", Popup)
        ];

        var findings = Compare(Context(
            Capture(reference, declared: "0.3s", present: [Popup]),
            Capture(candidate, declared: "0.3s", present: [Popup])));

        // A sequence diff, a phase invariant, and a duration check, all from one step.
        findings.Count.ShouldBe(4);
        findings.ShouldAllBe(f => f.Kind == FindingKind.Timeline);
        findings.ShouldAllBe(f => f.Fixture == "dialog/hero");
        findings.ShouldAllBe(f => f.Leg == ParityLeg.BlazorServer);
        findings.ShouldAllBe(f => f.Step == "open");
    }

    private static TimelineEvent Event(int t, string kind, string? attr) => new()
    {
        T = t, Kind = kind, Path = "div[role=dialog]", Attr = attr
    };

    /// <summary>Builds an attribute mutation as the MutationObserver records one.</summary>
    private static TimelineEvent Attribute(int t, string path, string name, string? from, string? to)
        => new() { T = t, Kind = "attribute", Path = path, Attr = name, From = from, To = to };

    /// <summary>Builds a transition or animation event, which carries its property name.</summary>
    private static TimelineEvent Run(int t, string kind, string path, string property = "opacity")
        => new() { T = t, Kind = kind, Path = path, Attr = property };

    /// <summary>Builds an insertion, which carries the new node's tag in <c>to</c>.</summary>
    private static TimelineEvent Added(int t, string path, string tag = "div")
        => new() { T = t, Kind = "added", Path = path, To = tag };

    /// <summary>
    /// Builds a removal exactly as <c>capture.js</c> records one: no path at all, and the
    /// departing node's tag in <c>from</c>.
    /// </summary>
    private static TimelineEvent Removed(int t, string tag = "div")
        => new() { T = t, Kind = "removed", Path = string.Empty, From = tag };

    /// <summary>Builds a run of attribute writes no two of which share a signature.</summary>
    private static TimelineEvent[] Noise(string prefix, int count)
        => [.. Enumerable.Range(0, count).Select(i => Attribute(i, Popup, "data-side", null, $"{prefix}{i}"))];

    /// <summary>Runs one span against one declaration on the Blazor leg alone.</summary>
    private static IReadOnlyList<Finding> Overrun(string declared, int observed)
    {
        TimelineEvent[] reference = [Run(0, "transitionstart", Popup), Run(20, "transitionend", Popup)];
        TimelineEvent[] candidate = [Run(0, "transitionstart", Popup), Run(observed, "transitionend", Popup)];

        return Errors(Compare(Context(
            // The reference declares nothing, so only the candidate's own check can fire.
            Capture(reference, present: [Popup]),
            Capture(candidate, declared: declared, present: [Popup]))));
    }

    private static IReadOnlyList<Finding> Compare(ComparisonContext context)
        => [.. new TimelineComparator().Compare(context)];

    /// <summary>Picks out the sequence findings, which name no invariant and no property.</summary>
    private static IReadOnlyList<Finding> Sequence(IReadOnlyList<Finding> findings)
        => [.. findings.Where(f => f.Property.Length == 0)];

    /// <summary>Picks out the findings one named phase invariant produced.</summary>
    private static IReadOnlyList<Finding> Invariant(IReadOnlyList<Finding> findings, string invariant)
        => [.. findings.Where(f => f.Property == invariant)];

    private static IReadOnlyList<Finding> Errors(IReadOnlyList<Finding> findings)
        => [.. findings.Where(f => f.Severity == Severity.Error)];

    private static ComparisonContext Context(StepCapture reference, StepCapture candidate)
        => new("dialog/hero", ParityLeg.BlazorServer, "open", reference, candidate, 0.001);

    private static StepCapture Capture(
        IReadOnlyList<TimelineEvent> timeline,
        string? declared = null,
        IReadOnlyList<string>? present = null)
    {
        var styles = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);

        if (declared is not null)
        {
            styles[Popup] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["transition-duration"] = declared
            };
        }

        return new StepCapture
        {
            Step = "open",
            Dom = Tree(present ?? []),
            Styles = styles.Count == 0 ? NoText : styles,
            CustomProps = NoText,
            Geometry = NoNumbers,
            Timeline = timeline
        };
    }

    /// <summary>
    /// Builds a snapshot holding exactly the paths a step ended with, which is what decides
    /// whether an animating node can be the one a removal event reported.
    /// </summary>
    private static DomNode Tree(IReadOnlyList<string> paths) => new()
    {
        Tag = "div",
        Path = "root",
        Attributes = new Dictionary<string, string>(StringComparer.Ordinal),
        Classes = [],
        Text = string.Empty,
        Children = [.. paths.Select(Leaf)]
    };

    private static DomNode Leaf(string path) => new()
    {
        Tag = "div",
        Path = path,
        Attributes = new Dictionary<string, string>(StringComparer.Ordinal),
        Classes = [],
        Text = string.Empty,
        Children = []
    };
}
