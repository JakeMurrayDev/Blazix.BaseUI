using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Diff;
using Shouldly;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

/// <summary>
/// Pins the three comparators that read a step-level value rather than a per-node map:
/// the ARIA snapshot, the focus path, and the console output.
/// </summary>
public sealed class SemanticComparatorTests
{
    private const string Button = "root > button";

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> NoText =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>> NoNumbers =
        new Dictionary<string, IReadOnlyDictionary<string, double>>(StringComparer.Ordinal);

    [Fact]
    public void AriaReportsNothingWhenTheSnapshotsAgree()
    {
        var aria = Lines("- button \"Toggle\":", "  - text: Off");

        new AriaSnapshotComparator().Compare(AriaContext(aria, aria)).ShouldBeEmpty();
    }

    [Fact]
    public void AriaReportsNothingWhenNeitherLegCapturedASnapshot()
    {
        new AriaSnapshotComparator().Compare(AriaContext(string.Empty, string.Empty)).ShouldBeEmpty();
    }

    [Fact]
    public void AriaIgnoresLineEndingsAndATrailingNewline()
    {
        // Neither is an accessibility difference. Reporting them would put a diff on every
        // step of every fixture and bury the ones that mean something.
        var context = AriaContext("- button \"Toggle\":\r\n  - text: Off\r\n", "- button \"Toggle\":\n  - text: Off");

        new AriaSnapshotComparator().Compare(context).ShouldBeEmpty();
    }

    [Fact]
    public void AriaFormatsAUnifiedDiffWithThreeLinesOfContext()
    {
        var context = AriaContext(Numbered("l", 1, 10), Numbered("l", 1, 10).Replace("l5", "L5", StringComparison.Ordinal));

        var finding = new AriaSnapshotComparator().Compare(context).ShouldHaveSingleItem();

        finding.Kind.ShouldBe(FindingKind.AriaSnapshot);
        finding.Severity.ShouldBe(Severity.Error);
        // The whole message, so the hunk header, the marker column, and the count of
        // context lines are all pinned rather than merely present. Three lines above and
        // three below the change, and nothing further out: l1, l9 and l10 are absent.
        finding.Message.ShouldBe(Lines(
            "ARIA snapshot differs: 1 line added, 1 line removed.",
            "@@ -2,7 +2,7 @@",
            " l2",
            " l3",
            " l4",
            "-l5",
            "+L5",
            " l6",
            " l7",
            " l8"));
    }

    [Fact]
    public void AriaReportsOneFindingPerStepAndNotOnePerLine()
    {
        var candidate = Numbered("l", 1, 20)
            .Replace("l2\n", "L2\n", StringComparison.Ordinal)
            .Replace("l11", "L11", StringComparison.Ordinal)
            .Replace("l19", "L19", StringComparison.Ordinal);

        new AriaSnapshotComparator()
            .Compare(AriaContext(Numbered("l", 1, 20), candidate))
            .ShouldHaveSingleItem();
    }

    [Fact]
    public void AriaMergesChangesLessThanTwiceTheContextApart()
    {
        // Three unchanged lines between the two changes: their context blocks would
        // overlap, so a unified diff renders them as one hunk rather than repeating l4-l6.
        var candidate = Numbered("l", 1, 10)
            .Replace("l3", "L3", StringComparison.Ordinal)
            .Replace("l7", "L7", StringComparison.Ordinal);

        var finding = new AriaSnapshotComparator()
            .Compare(AriaContext(Numbered("l", 1, 10), candidate))
            .ShouldHaveSingleItem();

        Hunks(finding.Message).ShouldBe(["@@ -1,10 +1,10 @@"]);
    }

    [Theory]
    // The gap that decides whether two changes share a hunk is twice the context. With six
    // unchanged lines between them the two context blocks meet and the hunks merge; with
    // seven there is a line neither block would print, so they separate. Pinning both sides
    // of the boundary is what stops the rule drifting into "always merge" or "never merge",
    // which a test sitting well inside one side of it would not notice.
    [InlineData(12, 1)]
    [InlineData(13, 2)]
    public void AriaMergesTwoChangesOnlyWhileTheirContextBlocksMeet(int second, int hunks)
    {
        var candidate = Numbered("l", 1, 20)
            .Replace("l5", "L5", StringComparison.Ordinal)
            .Replace($"l{second}", $"L{second}", StringComparison.Ordinal);

        var finding = new AriaSnapshotComparator()
            .Compare(AriaContext(Numbered("l", 1, 20), candidate))
            .ShouldHaveSingleItem();

        Hunks(finding.Message).Count.ShouldBe(hunks);
    }

    [Fact]
    public void AriaSplitsDistantChangesAndClampsContextAtBothEnds()
    {
        // First and last line: three lines of context run off each end of the file, so
        // both hunks are short, and the eighteen untouched lines between them keep the
        // two apart instead of being printed.
        var candidate = Numbered("l", 1, 20)
            .Replace("l1\n", "L1\n", StringComparison.Ordinal)
            .Replace("l20", "L20", StringComparison.Ordinal);

        var finding = new AriaSnapshotComparator()
            .Compare(AriaContext(Numbered("l", 1, 20), candidate))
            .ShouldHaveSingleItem();

        Hunks(finding.Message).ShouldBe(["@@ -1,4 +1,4 @@", "@@ -17,4 +17,4 @@"]);
        finding.Message.ShouldNotContain("l10");
    }

    [Fact]
    public void AriaHeadsAHunkWithTheLengthEachSideReallyHas()
    {
        // A line inserted mid-snapshot: the hunk spans six reference lines and seven
        // candidate lines, and the header has to count each side for itself rather than
        // assume they match. Pinned against `diff -U3` on the same two inputs.
        var candidate = Numbered("l", 1, 10).Replace("l6", "X\nl6", StringComparison.Ordinal);

        var finding = new AriaSnapshotComparator()
            .Compare(AriaContext(Numbered("l", 1, 10), candidate))
            .ShouldHaveSingleItem();

        Hunks(finding.Message).ShouldBe(["@@ -3,6 +3,7 @@"]);
    }

    [Fact]
    public void AriaSaysWhenTheReactLegCapturedNothing()
    {
        // A capture that failed on one leg is a real signal. The diff alone would say it
        // in markers only, which reads as "React renders nothing" rather than "React
        // captured nothing".
        var context = AriaContext(string.Empty, Numbered("l", 1, 2));

        var finding = new AriaSnapshotComparator().Compare(context).ShouldHaveSingleItem();

        finding.Message.ShouldBe(Lines(
            "React captured no ARIA snapshot; Blazor captured 2 lines.",
            "@@ -0,0 +1,2 @@",
            "+l1",
            "+l2"));
    }

    [Fact]
    public void AriaSaysWhenTheBlazorLegCapturedNothing()
    {
        var context = AriaContext(Numbered("l", 1, 1), string.Empty);

        var finding = new AriaSnapshotComparator().Compare(context).ShouldHaveSingleItem();

        finding.Message.ShouldBe(Lines(
            "Blazor captured no ARIA snapshot; React captured 1 line.",
            "@@ -1,1 +0,0 @@",
            "-l1"));
    }

    [Fact]
    public void AriaTruncatesAWildlyDifferentSnapshot()
    {
        // Two snapshots with no line in common. A full diff would be four hundred lines
        // of noise in the report, so the body is capped — but the counts in the header
        // are the real totals, so the cap cannot hide the scale of the difference.
        var context = AriaContext(Numbered("react", 1, 200), Numbered("blazor", 1, 200));

        var finding = new AriaSnapshotComparator().Compare(context).ShouldHaveSingleItem();
        var lines = finding.Message.Split('\n');

        lines[0].ShouldBe("ARIA snapshot differs: 200 lines added, 200 lines removed.");
        lines.Length.ShouldBe(42);
        lines[^1].ShouldBe("... 361 lines of the diff omitted.");
    }

    [Fact]
    public void AriaCountsASingleOmittedDiffLineInTheSingular()
    {
        // Twenty lines against twenty with nothing in common renders one header and forty
        // body lines: forty-one, one over the cap, so exactly one line is omitted.
        var context = AriaContext(Numbered("react", 1, 20), Numbered("blazor", 1, 20));

        var finding = new AriaSnapshotComparator().Compare(context).ShouldHaveSingleItem();

        finding.Message.Split('\n')[^1].ShouldBe("... 1 line of the diff omitted.");
    }

    [Fact]
    public void AriaCarriesBothSnapshotsAsTheComparedValues()
    {
        var context = AriaContext("- button", "- link");

        var finding = new AriaSnapshotComparator().Compare(context).ShouldHaveSingleItem();

        finding.ReferenceValue.ShouldBe("- button");
        finding.CandidateValue.ShouldBe("- link");
    }

    [Fact]
    public void FocusReportsNothingWhenBothLegsFocusTheSameNode()
    {
        new FocusComparator().Compare(FocusContext(Button, Button)).ShouldBeEmpty();
    }

    [Fact]
    public void FocusReportsNothingWhenNeitherLegFocusedAnything()
    {
        new FocusComparator().Compare(FocusContext(null, null)).ShouldBeEmpty();
    }

    [Fact]
    public void FocusReportsADifferingPath()
    {
        var finding = new FocusComparator()
            .Compare(FocusContext(Button, "portal(1) > div"))
            .ShouldHaveSingleItem();

        finding.Kind.ShouldBe(FindingKind.Focus);
        finding.Severity.ShouldBe(Severity.Error);
        finding.NodePath.ShouldBe(Button);
        finding.ReferenceValue.ShouldBe(Button);
        finding.CandidateValue.ShouldBe("portal(1) > div");
        finding.Message.ShouldBe(
            "Focus differs: React focused 'root > button'; Blazor focused 'portal(1) > div'.");
    }

    [Fact]
    public void FocusReportsWhenOnlyReactFocusedSomething()
    {
        var finding = new FocusComparator().Compare(FocusContext(Button, null)).ShouldHaveSingleItem();

        finding.NodePath.ShouldBe(Button);
        finding.ReferenceValue.ShouldBe(Button);
        finding.CandidateValue.ShouldBeNull();
        finding.Message.ShouldBe(
            "Focus differs: React focused 'root > button'; Blazor focused nothing inside " +
            "the captured roots.");
    }

    [Fact]
    public void FocusReportsWhenOnlyBlazorFocusedSomething()
    {
        var finding = new FocusComparator().Compare(FocusContext(null, Button)).ShouldHaveSingleItem();

        // The reference has no path to name the finding by, so it is named by the only
        // path either leg produced.
        finding.NodePath.ShouldBe(Button);
        finding.ReferenceValue.ShouldBeNull();
        finding.CandidateValue.ShouldBe(Button);
        finding.Message.ShouldBe(
            "Focus differs: React focused nothing inside the captured roots; " +
            "Blazor focused 'root > button'.");
    }

    [Theory]
    // Ordinal: a path differing only in case is a different path, which a comparison
    // that ignored case would wave through.
    [InlineData("root > button", "root > Button")]
    // And two canonically equivalent spellings of one character are two different paths.
    // A culture-sensitive comparison calls these equal, which would silently pass a
    // difference in a node label taken from the page's own text.
    [InlineData("root > \u00C5ngstrom", "root > A\u030Angstrom")]
    public void FocusComparesPathsOrdinally(string reference, string candidate)
    {
        new FocusComparator().Compare(FocusContext(reference, candidate)).ShouldHaveSingleItem();
    }

    [Fact]
    public void ConsoleReportsNothingWhenBothLegsAreSilent()
    {
        new ConsoleComparator().Compare(ConsoleContext([], [])).ShouldBeEmpty();
    }

    [Fact]
    public void ConsoleReportsNothingWhenTheSameMessagesAppear()
    {
        string[] messages = ["error: boom", "warning: slow", "error: boom"];

        new ConsoleComparator().Compare(ConsoleContext(messages, messages)).ShouldBeEmpty();
    }

    [Fact]
    public void ConsoleReportsABlazorOnlyMessageAsAnError()
    {
        var finding = new ConsoleComparator()
            .Compare(ConsoleContext([], ["error: boom"]))
            .ShouldHaveSingleItem();

        finding.Kind.ShouldBe(FindingKind.Console);
        finding.Severity.ShouldBe(Severity.Error);
        finding.Property.ShouldBe("error: boom");
        finding.ReferenceValue.ShouldBeNull();
        finding.CandidateValue.ShouldBe("error: boom");
        finding.Message.ShouldBe("Console message count differs: React 0, Blazor 1: 'error: boom'.");
    }

    [Fact]
    public void ConsoleReportsAReactOnlyMessageAsInfo()
    {
        // React's own development warnings are not Blazix's problem, so they are reported
        // for context and never fail a run.
        var finding = new ConsoleComparator()
            .Compare(ConsoleContext(["warning: validateDOMNesting"], []))
            .ShouldHaveSingleItem();

        finding.Severity.ShouldBe(Severity.Info);
        finding.ReferenceValue.ShouldBe("warning: validateDOMNesting");
        finding.CandidateValue.ShouldBeNull();
        finding.Message.ShouldBe(
            "Console message count differs: React 1, Blazor 0: 'warning: validateDOMNesting'.");
    }

    [Fact]
    public void ConsoleReportsAMessageBlazorLoggedTwiceAndReactOnce()
    {
        // Multisets, not sets: an error logged once per render pass on one leg and once
        // in total on the other is a difference a set comparison would erase.
        var finding = new ConsoleComparator()
            .Compare(ConsoleContext(["error: boom"], ["error: boom", "error: boom"]))
            .ShouldHaveSingleItem();

        finding.Severity.ShouldBe(Severity.Error);
        finding.Message.ShouldBe("Console message count differs: React 1, Blazor 2: 'error: boom'.");
    }

    [Fact]
    public void ConsoleReportsAMessageReactLoggedTwiceAndBlazorOnce()
    {
        var finding = new ConsoleComparator()
            .Compare(ConsoleContext(["error: boom", "error: boom"], ["error: boom"]))
            .ShouldHaveSingleItem();

        finding.Severity.ShouldBe(Severity.Info);
        finding.Message.ShouldBe("Console message count differs: React 2, Blazor 1: 'error: boom'.");
    }

    [Fact]
    public void ConsoleIgnoresThePortAndPositionAMessageWasLoggedFrom()
    {
        // The parity server binds a free port per run, so the port in a stack frame
        // differs between the committed React baseline and a live Blazor run. The line
        // and column differ with the bundle. Neither is a parity result.
        var context = ConsoleContext(
            ["error: boom\n    at http://127.0.0.1:5157/app/main.js:12:34"],
            ["error: boom\n    at http://127.0.0.1:61022/app/main.js:9:8"]);

        new ConsoleComparator().Compare(context).ShouldBeEmpty();
    }

    [Fact]
    public void ConsoleKeepsMessagesApartWhenTheUrlPathDiffers()
    {
        // Two different resources failing to load are two different results. Replacing
        // the whole URL rather than its volatile parts would collapse them into one and
        // report parity where there is none.
        var context = ConsoleContext(
            ["error: failed to load http://127.0.0.1:5157/assets/logo.png"],
            ["error: failed to load http://127.0.0.1:5157/assets/icon.png"]);

        var findings = new ConsoleComparator().Compare(context).ToList();

        findings.Count.ShouldBe(2);
        findings.Select(f => f.Severity).ShouldBe([Severity.Error, Severity.Info], ignoreOrder: true);

        var blazorOnly = findings.Single(f => f.Severity == Severity.Error);
        // The comparison is made on the normalized text, and a reader still needs the
        // port the message really carried, so the finding holds both.
        blazorOnly.Property.ShouldBe("error: failed to load http://127.0.0.1:<port>/assets/icon.png");
        blazorOnly.CandidateValue.ShouldBe("error: failed to load http://127.0.0.1:5157/assets/icon.png");
        blazorOnly.ReferenceValue.ShouldBeNull();
    }

    [Fact]
    public void ConsoleKeepsMessagesApartWhenOnlyTheirTextDiffers()
    {
        var context = ConsoleContext(
            ["error: first failure at http://127.0.0.1:5157/app/main.js:1:1"],
            ["error: second failure at http://127.0.0.1:5157/app/main.js:1:1"]);

        new ConsoleComparator().Compare(context).ToList().Count.ShouldBe(2);
    }

    [Fact]
    public void ConsoleKeepsATimeOfDayInTheMessageText()
    {
        // '12:30:45' has the shape of a line and column suffix and is not one. A rule
        // that stripped every colon-number-colon-number would fold these two messages
        // together and silently pass a difference.
        var context = ConsoleContext(
            ["error: connection lost at 12:30:45"],
            ["error: connection lost at 12:30:46"]);

        new ConsoleComparator().Compare(context).ToList().Count.ShouldBe(2);
    }

    [Fact]
    public void ConsoleCountsOneStampedMessageTwiceRatherThanReportingItTwice()
    {
        // Blazor's client logging stamps every line it writes with an ISO instant. Left
        // in, one error logged on two render passes is two messages, so it is a finding
        // whose Property differs between a leg's two attempts — which the retry demotes
        // to Flaky, and a Flaky finding never fails the run. A waiver naming one of them
        // could never match twice either.
        var context = ConsoleContext(
            [],
            [
                "error: [2026-07-28T12:30:45.123Z] Error: circuit failed",
                "error: [2026-07-28T12:31:02.004Z] Error: circuit failed"
            ]);

        var finding = new ConsoleComparator().Compare(context).ShouldHaveSingleItem();

        finding.Severity.ShouldBe(Severity.Error);
        finding.Property.ShouldBe("error: [<time>] Error: circuit failed");
        finding.Message.ShouldBe(
            "Console message count differs: React 0, Blazor 2: " +
            "'error: [<time>] Error: circuit failed'.");
    }

    [Theory]
    // Sub-second precision is the logger's choice, not the format's.
    [InlineData("2026-07-28T12:30:45Z", "2026-07-28T12:30:46Z")]
    // And so is the trailing 'Z': a stamp written in local time carries none.
    [InlineData("2026-07-28T12:30:45.123", "2026-07-28T12:31:02.004")]
    // Neither part is there to key on, so the rule cannot require either.
    [InlineData("2026-07-28T12:30:45", "2026-07-28T12:31:02")]
    public void ConsoleFoldsAnInstantHoweverItsOptionalPartsAreSpelled(string first, string second)
    {
        var context = ConsoleContext(
            [], [$"error: reconnecting at {first}", $"error: reconnecting at {second}"]);

        new ConsoleComparator().Compare(context).ShouldHaveSingleItem()
            .Property.ShouldBe("error: reconnecting at <time>");
    }

    [Theory]
    // A bare time of day: no date and no 'T' in front of it, so the instant rule must not
    // reach it. This is the same text the position rule already has to leave alone.
    [InlineData("error: connection lost at 12:30:45", "error: connection lost at 12:30:46")]
    // A dotted version: the optional fractional-second tail must not be what a rule keys
    // on, or every version number in a message becomes a timestamp.
    [InlineData("error: bundle 1.2.3 failed to parse", "error: bundle 1.2.4 failed to parse")]
    // A calendar date with no time is not an instant. Nothing seen so far shows a date
    // alone being volatile between legs, so it stays in the compared text.
    [InlineData("error: certificate expired 2026-07-28", "error: certificate expired 2026-07-29")]
    public void ConsoleKeepsApartTextThatOnlyLooksLikeAnInstant(string reference, string candidate)
    {
        new ConsoleComparator().Compare(ConsoleContext([reference], [candidate])).ToList().Count.ShouldBe(2);
    }

    [Fact]
    public void ConsoleKeepsMessagesApartWhenOnlyTheHostDiffers()
    {
        // Only the port is volatile — the parity server binds a free one per run. Two
        // hosts are two origins, and folding the whole authority would report parity
        // between an asset that loaded and one that did not.
        var context = ConsoleContext(
            ["error: failed to load https://cdn-a.example.com/lib.js"],
            ["error: failed to load https://cdn-b.example.com/lib.js"]);

        new ConsoleComparator().Compare(context).ToList().Count.ShouldBe(2);
    }

    [Fact]
    public void ConsoleKeepsMessagesApartWhenOnlyTheSchemeDiffers()
    {
        // A socket that fell back to long polling and one that did not are two results.
        var context = ConsoleContext(
            ["error: closed ws://127.0.0.1:5157/_blazor"],
            ["error: closed http://127.0.0.1:5157/_blazor"]);

        new ConsoleComparator().Compare(context).ToList().Count.ShouldBe(2);
    }

    [Fact]
    public void ConsoleKeepsRelativePathsApart()
    {
        var context = ConsoleContext(
            ["error: 404 for /api/users"],
            ["error: 404 for /api/orders"]);

        new ConsoleComparator().Compare(context).ToList().Count.ShouldBe(2);
    }

    [Fact]
    public void ConsoleSeparatesAnErrorFromAWarningWithTheSameText()
    {
        // The capturer prefixes each message with its level. An error one leg logs as a
        // warning is a difference, so the prefix has to survive normalization.
        var context = ConsoleContext(["warning: boom"], ["error: boom"]);

        var findings = new ConsoleComparator().Compare(context).ToList();

        findings.Count.ShouldBe(2);
        findings.Single(f => f.Severity == Severity.Error).Property.ShouldBe("error: boom");
        findings.Single(f => f.Severity == Severity.Info).Property.ShouldBe("warning: boom");
    }

    [Fact]
    public void ConsoleOrdersFindingsByMessage()
    {
        var context = ConsoleContext([], ["error: second", "error: first"]);

        new ConsoleComparator().Compare(context).Select(f => f.Property)
            .ShouldBe(["error: first", "error: second"]);
    }

    [Fact]
    public void EachComparatorOwnsOneKind()
    {
        new AriaSnapshotComparator().Kind.ShouldBe(FindingKind.AriaSnapshot);
        new FocusComparator().Kind.ShouldBe(FindingKind.Focus);
        new ConsoleComparator().Kind.ShouldBe(FindingKind.Console);
    }

    [Fact]
    public void EachComparatorReadsOnlyItsOwnPartOfTheCapture()
    {
        // One file, one kind: a console difference must not also surface as a focus or an
        // ARIA finding, or a waiver written for one silences the others. The snapshot and
        // the focus path are non-empty and equal on both legs, so the two empty results
        // are those comparators reading their own part and agreeing — not a context in
        // which they had nothing to read at all.
        var aria = Lines("- button \"Toggle\":", "  - text: Off");
        var context = Context(
            Capture(aria: aria, focus: Button),
            Capture(aria: aria, focus: Button, console: ["error: boom"]));

        new ConsoleComparator().Compare(context).ShouldHaveSingleItem();
        new AriaSnapshotComparator().Compare(context).ShouldBeEmpty();
        new FocusComparator().Compare(context).ShouldBeEmpty();
    }

    [Fact]
    public void EveryFindingCarriesTheFixtureLegAndStep()
    {
        var findings = new AriaSnapshotComparator().Compare(AriaContext("- button", "- link"))
            .Concat(new FocusComparator().Compare(FocusContext(Button, null)))
            .Concat(new ConsoleComparator().Compare(ConsoleContext([], ["error: boom"])))
            .ToList();

        findings.Count.ShouldBe(3);
        findings.ShouldAllBe(f => f.Fixture == "switch/hero@light");
        findings.ShouldAllBe(f => f.Leg == ParityLeg.BlazorServer);
        findings.ShouldAllBe(f => f.Step == "initial");
    }

    /// <summary>Joins lines the way both a snapshot and a rendered diff are joined.</summary>
    private static string Lines(params string[] lines) => string.Join("\n", lines);

    /// <summary>Builds a snapshot of numbered lines, for example <c>l1</c> through <c>l10</c>.</summary>
    private static string Numbered(string prefix, int first, int last)
        => Lines([.. Enumerable.Range(first, last - first + 1).Select(i => $"{prefix}{i}")]);

    /// <summary>Picks the hunk headers out of a rendered diff.</summary>
    private static IReadOnlyList<string> Hunks(string message)
        => [.. message.Split('\n').Where(line => line.StartsWith("@@", StringComparison.Ordinal))];

    private static ComparisonContext AriaContext(string reference, string candidate)
        => Context(Capture(aria: reference), Capture(aria: candidate));

    private static ComparisonContext FocusContext(string? reference, string? candidate)
        => Context(Capture(focus: reference), Capture(focus: candidate));

    private static ComparisonContext ConsoleContext(
        IReadOnlyList<string> reference, IReadOnlyList<string> candidate)
        => Context(Capture(console: reference), Capture(console: candidate));

    private static ComparisonContext Context(StepCapture reference, StepCapture candidate)
        => new(
            "switch/hero",
            "light",
            "switch/hero@light",
            ParityLeg.BlazorServer,
            "initial",
            reference,
            candidate,
            0.001);

    private static StepCapture Capture(
        string? aria = null,
        string? focus = null,
        IReadOnlyList<string>? console = null) => new()
        {
            Step = "initial",
            Dom = new DomNode
            {
                Tag = "div",
                Path = "root",
                Attributes = new Dictionary<string, string>(StringComparer.Ordinal),
                Classes = [],
                Text = string.Empty,
                Children = []
            },
            Styles = NoText,
            CustomProps = NoText,
            Geometry = NoNumbers,
            Aria = aria ?? string.Empty,
            Focus = focus,
            Console = console ?? []
        };
}
