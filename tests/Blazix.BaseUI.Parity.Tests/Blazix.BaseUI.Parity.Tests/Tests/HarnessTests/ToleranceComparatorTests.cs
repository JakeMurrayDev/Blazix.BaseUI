using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Diff;
using Shouldly;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

/// <summary>
/// Pins the numeric tolerance and the three comparators that read the value maps
/// captured beside the DOM: computed styles, CSS custom properties, and geometry.
/// </summary>
public sealed class ToleranceComparatorTests
{
    private const string Button = "root > button";

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> NoText =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>> NoNumbers =
        new Dictionary<string, IReadOnlyDictionary<string, double>>(StringComparer.Ordinal);

    [Theory]
    [InlineData("10px", "10.4px", 0.5, true)]
    [InlineData("10px", "10.6px", 0.5, false)]
    [InlineData("rgb(1, 2, 3)", "rgb(1, 2, 3)", 0.5, true)]
    [InlineData("rgb(1, 2, 3)", "rgb(1, 2, 4)", 0.5, false)]
    [InlineData("0.5", "0.5", 0.5, true)]
    public void AppliesToleranceToNumericComponentsOnly(
        string reference, string candidate, double epsilon, bool expected)
    {
        ValueTolerance.Equivalent(reference, candidate, epsilon).ShouldBe(expected);
    }

    [Fact]
    public void TreatsMatrixTransformsComponentwise()
    {
        ValueTolerance
            .Equivalent("matrix(1, 0, 0, 1, 16, 0)", "matrix(1, 0, 0, 1, 16.3, 0)", 0.5)
            .ShouldBeTrue();
    }

    [Theory]
    // Keywords carry no numbers at all, so tolerance must never reach them.
    [InlineData("none", "none", true)]
    [InlineData("none", "block", false)]
    [InlineData("", "", true)]
    // A property one leg computes and the other leaves blank. Nothing but the run count
    // separates these two: an empty value shares every run the other value has, vacuously.
    [InlineData("", "0px", false)]
    [InlineData("0px", "", false)]
    // A different number of runs is a different value however close the numbers are.
    [InlineData("10px", "10px 10px", false)]
    [InlineData("0px", "0px 0px 0px rgb(0, 0, 0)", false)]
    // Same token count, different alternation: a comparison that only walked the numbers
    // and ignored where they sat would call these equivalent.
    [InlineData("a1", "1a", false)]
    // A sign only opens a number when a digit follows, so a hyphenated keyword stays whole
    // and cannot be read as a negative number that happens to be within tolerance.
    [InlineData("cubic-bezier(0.4, 0, 0.2, 1)", "cubic-bezier(0.4, 0, 0.2, 1)", true)]
    [InlineData("cubic-bezier(0.4, 0, 0.2, 1)", "cubic-bezier(0.4, 0, 0.2, 2)", false)]
    [InlineData("-apple-system", "-apple-system", true)]
    [InlineData("-apple-system", "-apple-systen", false)]
    // A genuine negative is still a number, so it is compared as one on both legs.
    [InlineData("-8px", "-8.3px", true)]
    [InlineData("-8px", "8px", false)]
    // The sign belongs to the number rather than to the text run before it, which is only
    // observable when the two values straddle zero: half a pixel below zero is half a pixel
    // from a tenth above it, and the tolerance has to absorb that as it would anywhere else.
    // Reading the sign as text instead splits this into a different number of runs, and the
    // sub-pixel noise the tolerance exists for is reported as a difference.
    [InlineData("-0.4px", "0.1px", true)]
    public void ComparesTokenSequencesAndNotJustTheNumbersInThem(
        string reference, string candidate, bool expected)
    {
        ValueTolerance.Equivalent(reference, candidate, 0.5).ShouldBe(expected);
    }

    [Fact]
    public void ComputedStyleReportsADifferingProperty()
    {
        var context = StyleContext(
            At(Button, ("color", "rgb(0, 0, 0)")),
            At(Button, ("color", "rgb(255, 0, 0)")));

        var finding = new ComputedStyleComparator().Compare(context).ShouldHaveSingleItem();

        finding.Kind.ShouldBe(FindingKind.ComputedStyle);
        finding.Severity.ShouldBe(Severity.Error);
        finding.NodePath.ShouldBe(Button);
        finding.Property.ShouldBe("color");
        finding.ReferenceValue.ShouldBe("rgb(0, 0, 0)");
        finding.CandidateValue.ShouldBe("rgb(255, 0, 0)");
    }

    [Fact]
    public void ComputedStyleReportsNothingWhenEveryPropertyAgrees()
    {
        var styles = At(Button, ("color", "rgb(0, 0, 0)"), ("display", "flex"));

        new ComputedStyleComparator().Compare(StyleContext(styles, styles)).ShouldBeEmpty();
    }

    [Fact]
    public void ComputedStyleReportsAPropertyCapturedOnTheReferenceLegOnly()
    {
        var context = StyleContext(
            At(Button, ("display", "flex")),
            At(Button, ("color", "rgb(0, 0, 0)")));

        var findings = new ComputedStyleComparator().Compare(context).ToList();

        // Ordinal name order, so 'color' precedes 'display'.
        findings.Select(f => f.Property).ShouldBe(["color", "display"]);
        findings[0].ReferenceValue.ShouldBeNull();
        findings[0].CandidateValue.ShouldBe("rgb(0, 0, 0)");
        findings[1].ReferenceValue.ShouldBe("flex");
        findings[1].CandidateValue.ShouldBeNull();
    }

    [Fact]
    public void ComputedStyleAbsorbsASubPixelDifference()
    {
        var context = StyleContext(
            At(Button, ("width", "120px")),
            At(Button, ("width", "120.4px")));

        new ComputedStyleComparator().Compare(context).ShouldBeEmpty();
    }

    [Fact]
    public void ComputedStyleReportsADifferenceOutsideItsTolerance()
    {
        // 0.8px is inside the geometry tolerance and outside this one, so this pins which
        // of the two epsilons the style comparator uses rather than merely that it has one.
        var context = StyleContext(
            At(Button, ("width", "120px")),
            At(Button, ("width", "120.8px")));

        var finding = new ComputedStyleComparator().Compare(context).ShouldHaveSingleItem();

        finding.Property.ShouldBe("width");
        finding.CandidateValue.ShouldBe("120.8px");
    }

    [Fact]
    public void ComputedStyleReportsNothingWhenNeitherLegCapturedTheNode()
    {
        new ComputedStyleComparator().Compare(StyleContext(NoText, NoText)).ShouldBeEmpty();
    }

    [Fact]
    public void ComputedStyleReportsEveryPropertyWhenOnlyOneLegCapturedTheNode()
    {
        // A node in the snapshot with no entry in the style map is a capture that
        // disagrees with itself. Reading the absence as an empty map keeps that loud
        // instead of dropping the node's styles from the run without a word.
        var context = StyleContext(At(Button, ("color", "rgb(0, 0, 0)"), ("display", "flex")), NoText);

        var findings = new ComputedStyleComparator().Compare(context).ToList();

        findings.Select(f => f.Property).ShouldBe(["color", "display"]);
        findings.ShouldAllBe(f => f.CandidateValue == null);
    }

    [Fact]
    public void ComputedStyleReadsEachLegAtItsOwnPath()
    {
        // The Blazor leg wraps the body in one extra <div>, so the matched pair sits at
        // 'dlg>body' on one leg and 'dlg>wrap>body' on the other. Looking the candidate up
        // under the reference's path would miss it and report a value React alone renders.
        var context = Context(
            Capture(WrapperReference(), styles: At("dlg>body", ("color", "rgb(1, 2, 3)"))),
            Capture(WrapperCandidate(), styles: At("dlg>wrap>body", ("color", "rgb(4, 5, 6)"))));

        var finding = new ComputedStyleComparator().Compare(context).ShouldHaveSingleItem();

        finding.NodePath.ShouldBe("dlg>body");
        finding.ReferenceValue.ShouldBe("rgb(1, 2, 3)");
        finding.CandidateValue.ShouldBe("rgb(4, 5, 6)");
    }

    [Fact]
    public void ComputedStyleSaysWhenThePairingWasRelaxed()
    {
        // Nothing under the <div> pairs on tag, role, and name together, so the matcher
        // falls back to the tag alone. The two <ul> nodes are still compared — suppressing
        // them would lose a real layout difference — but a reader has to be told that the
        // operands may not be the same element.
        var context = Context(
            Capture(RelaxedTree("menu"), styles: At("root > ul", ("opacity", "1"))),
            Capture(RelaxedTree("listbox"), styles: At("root > ul", ("opacity", "0"))));

        var finding = new ComputedStyleComparator().Compare(context).ShouldHaveSingleItem();

        finding.NodePath.ShouldBe("root > ul");
        finding.ReferenceValue.ShouldBe("1");
        finding.CandidateValue.ShouldBe("0");
        finding.Message.ShouldContain("tag alone");
    }

    [Fact]
    public void CustomPropertyReportsADifferingProperty()
    {
        var context = PropContext(
            At(Button, ("--anchor-width", "100px")),
            At(Button, ("--anchor-width", "140px")));

        var finding = new CustomPropertyComparator().Compare(context).ShouldHaveSingleItem();

        finding.Kind.ShouldBe(FindingKind.CustomProperty);
        finding.Severity.ShouldBe(Severity.Error);
        finding.NodePath.ShouldBe(Button);
        finding.Property.ShouldBe("--anchor-width");
        finding.ReferenceValue.ShouldBe("100px");
        finding.CandidateValue.ShouldBe("140px");
    }

    [Fact]
    public void CustomPropertyReportsNothingWhenEveryPropertyAgrees()
    {
        var props = At(Button, ("--anchor-width", "100px"), ("--transform-origin", "0px 0px"));

        new CustomPropertyComparator().Compare(PropContext(props, props)).ShouldBeEmpty();
    }

    [Fact]
    public void CustomPropertyReportsAPropertyCapturedOnTheCandidateLegOnly()
    {
        var context = PropContext(NoText, At(Button, ("--available-height", "420px")));

        var finding = new CustomPropertyComparator().Compare(context).ShouldHaveSingleItem();

        finding.Property.ShouldBe("--available-height");
        finding.ReferenceValue.ShouldBeNull();
        finding.CandidateValue.ShouldBe("420px");
    }

    [Fact]
    public void CustomPropertyAbsorbsASubPixelDifference()
    {
        var context = PropContext(
            At(Button, ("--anchor-width", "100px")),
            At(Button, ("--anchor-width", "100.4px")));

        new CustomPropertyComparator().Compare(context).ShouldBeEmpty();
    }

    [Fact]
    public void CustomPropertyReportsADifferenceOutsideItsTolerance()
    {
        var context = PropContext(
            At(Button, ("--anchor-width", "100px")),
            At(Button, ("--anchor-width", "100.8px")));

        new CustomPropertyComparator().Compare(context).ShouldHaveSingleItem();
    }

    [Fact]
    public void CustomPropertyReadsEachLegAtItsOwnPath()
    {
        var context = Context(
            Capture(WrapperReference(), customProps: At("dlg>body", ("--anchor-width", "100px"))),
            Capture(WrapperCandidate(), customProps: At("dlg>wrap>body", ("--anchor-width", "140px"))));

        var finding = new CustomPropertyComparator().Compare(context).ShouldHaveSingleItem();

        finding.NodePath.ShouldBe("dlg>body");
        finding.ReferenceValue.ShouldBe("100px");
        finding.CandidateValue.ShouldBe("140px");
    }

    [Fact]
    public void CustomPropertyReportsNothingWhenNeitherLegCapturedTheNode()
    {
        new CustomPropertyComparator().Compare(PropContext(NoText, NoText)).ShouldBeEmpty();
    }

    [Fact]
    public void CustomPropertyReportsEveryPropertyWhenOnlyOneLegCapturedTheNode()
    {
        var context = PropContext(NoText, At(Button, ("--anchor-width", "1px"), ("--available-width", "2px")));

        var findings = new CustomPropertyComparator().Compare(context).ToList();

        findings.Select(f => f.Property).ShouldBe(["--anchor-width", "--available-width"]);
        findings.ShouldAllBe(f => f.ReferenceValue == null);
    }

    [Fact]
    public void GeometryReportsEveryValueWhenOnlyOneLegCapturedTheNode()
    {
        var context = GeometryContext(Box(Button, ("width", 120), ("height", 32)), NoNumbers);

        var findings = new GeometryComparator().Compare(context).ToList();

        findings.Select(f => f.Property).ShouldBe(["height", "width"]);
        findings.ShouldAllBe(f => f.CandidateValue == null);
    }

    [Fact]
    public void GeometryReportsADifferingBox()
    {
        var context = GeometryContext(
            Box(Button, ("width", 120)),
            Box(Button, ("width", 140)));

        var finding = new GeometryComparator().Compare(context).ShouldHaveSingleItem();

        finding.Kind.ShouldBe(FindingKind.Geometry);
        finding.Severity.ShouldBe(Severity.Error);
        finding.NodePath.ShouldBe(Button);
        finding.Property.ShouldBe("width");
        finding.ReferenceValue.ShouldBe("120");
        finding.CandidateValue.ShouldBe("140");
    }

    [Fact]
    public void GeometryReportsNothingWhenEveryValueAgrees()
    {
        var geometry = Box(Button, ("x", 12.5), ("y", 4), ("width", 120), ("height", 32));

        new GeometryComparator().Compare(GeometryContext(geometry, geometry)).ShouldBeEmpty();
    }

    [Fact]
    public void GeometryReportsAValueCapturedOnOneLegOnly()
    {
        var context = GeometryContext(
            Box(Button, ("width", 120), ("height", 32)),
            Box(Button, ("width", 120)));

        var finding = new GeometryComparator().Compare(context).ShouldHaveSingleItem();

        finding.Property.ShouldBe("height");
        finding.ReferenceValue.ShouldBe("32");
        finding.CandidateValue.ShouldBeNull();
    }

    [Fact]
    public void GeometryAbsorbsASubPixelDifference()
    {
        // 0.8 is outside the style tolerance and inside this one, which is what pins the
        // geometry comparator to its own, looser epsilon.
        var context = GeometryContext(
            Box(Button, ("x", 12.5)),
            Box(Button, ("x", 13.3)));

        new GeometryComparator().Compare(context).ShouldBeEmpty();
    }

    [Fact]
    public void GeometryReportsADifferenceOutsideItsTolerance()
    {
        var context = GeometryContext(
            Box(Button, ("x", 12.5)),
            Box(Button, ("x", 14)));

        var finding = new GeometryComparator().Compare(context).ShouldHaveSingleItem();

        finding.Property.ShouldBe("x");
        finding.ReferenceValue.ShouldBe("12.5");
        finding.CandidateValue.ShouldBe("14");
    }

    [Fact]
    public void GeometryReportsNothingWhenNeitherLegCapturedTheNode()
    {
        new GeometryComparator().Compare(GeometryContext(NoNumbers, NoNumbers)).ShouldBeEmpty();
    }

    [Fact]
    public void GeometryReadsEachLegAtItsOwnPath()
    {
        var context = Context(
            Capture(WrapperReference(), geometry: Box("dlg>body", ("width", 120))),
            Capture(WrapperCandidate(), geometry: Box("dlg>wrap>body", ("width", 140))));

        var finding = new GeometryComparator().Compare(context).ShouldHaveSingleItem();

        finding.NodePath.ShouldBe("dlg>body");
        finding.ReferenceValue.ShouldBe("120");
        finding.CandidateValue.ShouldBe("140");
    }

    [Fact]
    public void GeometrySaysWhenThePairingWasRelaxed()
    {
        var context = Context(
            Capture(RelaxedTree("menu"), geometry: Box("root > ul", ("height", 100))),
            Capture(RelaxedTree("listbox"), geometry: Box("root > ul", ("height", 240))));

        var finding = new GeometryComparator().Compare(context).ShouldHaveSingleItem();

        finding.Message.ShouldContain("tag alone");
    }

    [Fact]
    public void CustomPropertySaysWhenThePairingWasRelaxed()
    {
        var context = Context(
            Capture(RelaxedTree("menu"), customProps: At("root > ul", ("--anchor-width", "10px"))),
            Capture(RelaxedTree("listbox"), customProps: At("root > ul", ("--anchor-width", "40px"))));

        var finding = new CustomPropertyComparator().Compare(context).ShouldHaveSingleItem();

        finding.Message.ShouldContain("tag alone");
    }

    [Fact]
    public void EveryFindingCarriesTheFixtureLegAndStep()
    {
        var findings = new ComputedStyleComparator()
            .Compare(StyleContext(At(Button, ("color", "red")), At(Button, ("color", "blue"))))
            .Concat(new CustomPropertyComparator()
                .Compare(PropContext(At(Button, ("--x", "1px")), At(Button, ("--x", "9px")))))
            .Concat(new GeometryComparator()
                .Compare(GeometryContext(Box(Button, ("width", 1)), Box(Button, ("width", 9)))))
            .ToList();

        findings.Count.ShouldBe(3);
        findings.ShouldAllBe(f => f.Fixture == "switch/hero");
        findings.ShouldAllBe(f => f.Leg == ParityLeg.BlazorServer);
        findings.ShouldAllBe(f => f.Step == "initial");
    }

    [Fact]
    public void EachComparatorOwnsOneKind()
    {
        new ComputedStyleComparator().Kind.ShouldBe(FindingKind.ComputedStyle);
        new CustomPropertyComparator().Kind.ShouldBe(FindingKind.CustomProperty);
        new GeometryComparator().Kind.ShouldBe(FindingKind.Geometry);
    }

    [Fact]
    public void EachComparatorReadsOnlyItsOwnMap()
    {
        // One file, one kind: a style difference must not also surface as a custom
        // property or a geometry finding, or a waiver written for one silences the others.
        var context = Context(
            Capture(Tree(), styles: At(Button, ("color", "red"))),
            Capture(Tree(), styles: At(Button, ("color", "blue"))));

        new CustomPropertyComparator().Compare(context).ShouldBeEmpty();
        new GeometryComparator().Compare(context).ShouldBeEmpty();
    }

    /// <summary>A root holding one button, which is the pair every value test reads.</summary>
    private static DomNode Tree() => Node("div", "root", Node("button", Button));

    /// <summary>
    /// A list whose role differs between the legs, and whose <c>&lt;ul&gt;</c> therefore
    /// only pairs once the matcher degrades the key to the tag alone.
    /// </summary>
    private static DomNode RelaxedTree(string role) => Node(
        "div",
        "root",
        Role("ul", "root > ul", role, Node("li", "root > ul > li(1)"), Node("li", "root > ul > li(2)")));

    /// <summary>The dialog as React renders it.</summary>
    private static DomNode WrapperReference() => Role(
        "div",
        "dlg",
        "dialog",
        Marked("div", "dlg>body", "data-body", Node("p", "dlg>body>p"), Node("p", "dlg>body>q")),
        Marked("div", "dlg>foot", "data-footer", Node("button", "dlg>foot>ok")));

    /// <summary>The same dialog with one extra wrapper around the body.</summary>
    private static DomNode WrapperCandidate() => Role(
        "div",
        "dlg",
        "dialog",
        Node(
            "div",
            "dlg>wrap",
            Marked(
                "div",
                "dlg>wrap>body",
                "data-body",
                Node("p", "dlg>wrap>body>p"),
                Node("p", "dlg>wrap>body>q"))),
        Marked("div", "dlg>foot", "data-footer", Node("button", "dlg>foot>ok")));

    private static DomNode Node(string tag, string path, params DomNode[] children)
        => Build(tag, path, new Dictionary<string, string>(StringComparer.Ordinal), children);

    private static DomNode Role(string tag, string path, string role, params DomNode[] children)
        => Build(
            tag,
            path,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["role"] = role },
            children);

    private static DomNode Marked(
        string tag, string path, string attribute, params DomNode[] children)
        => Build(
            tag,
            path,
            new Dictionary<string, string>(StringComparer.Ordinal) { [attribute] = string.Empty },
            children);

    private static DomNode Build(
        string tag,
        string path,
        Dictionary<string, string> attributes,
        DomNode[] children) => new()
        {
            Tag = tag,
            Path = path,
            Attributes = attributes,
            Classes = [],
            Text = string.Empty,
            Children = children
        };

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> At(
        string path, params (string Name, string Value)[] entries)
        => new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal)
        {
            [path] = entries.ToDictionary(e => e.Name, e => e.Value, StringComparer.Ordinal)
        };

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>> Box(
        string path, params (string Name, double Value)[] entries)
        => new Dictionary<string, IReadOnlyDictionary<string, double>>(StringComparer.Ordinal)
        {
            [path] = entries.ToDictionary(e => e.Name, e => e.Value, StringComparer.Ordinal)
        };

    private static ComparisonContext StyleContext(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> reference,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> candidate)
        => Context(Capture(Tree(), styles: reference), Capture(Tree(), styles: candidate));

    private static ComparisonContext PropContext(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> reference,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> candidate)
        => Context(Capture(Tree(), customProps: reference), Capture(Tree(), customProps: candidate));

    private static ComparisonContext GeometryContext(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>> reference,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>> candidate)
        => Context(Capture(Tree(), geometry: reference), Capture(Tree(), geometry: candidate));

    private static ComparisonContext Context(StepCapture reference, StepCapture candidate)
        => new("switch/hero", ParityLeg.BlazorServer, "initial", reference, candidate, 0.001);

    private static StepCapture Capture(
        DomNode dom,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>? styles = null,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>? customProps = null,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>>? geometry = null) => new()
        {
            Step = "initial",
            Dom = dom,
            Styles = styles ?? NoText,
            CustomProps = customProps ?? NoText,
            Geometry = geometry ?? NoNumbers
        };
}
