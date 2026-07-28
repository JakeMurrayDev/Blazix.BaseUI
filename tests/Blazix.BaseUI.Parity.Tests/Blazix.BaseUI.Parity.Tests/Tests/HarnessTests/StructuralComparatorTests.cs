using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Diff;
using Shouldly;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

/// <summary>
/// Pins the three comparators that read the DOM snapshot alone: structure,
/// attributes, and Blazix marker classification.
/// </summary>
public sealed class StructuralComparatorTests
{
    [Fact]
    public void StructureReportsANodePresentOnOneLegOnly()
    {
        var context = Context(
            Node("div", Node("button"), Node("ul")),
            Node("div", Node("button")));

        var findings = new StructureComparator().Compare(context).ToList();

        findings.Count.ShouldBe(1);
        findings[0].Kind.ShouldBe(FindingKind.Structure);
        findings[0].Severity.ShouldBe(Severity.Error);
        findings[0].NodePath.ShouldBe("ul");
    }

    [Fact]
    public void StructureReportsNothingForIdenticalTrees()
    {
        var tree = Node("div", Node("button"), Node("ul", Node("li")));

        new StructureComparator().Compare(Context(tree, tree)).ShouldBeEmpty();
    }

    [Fact]
    public void StructureReportsReorderedSiblings()
    {
        // DOM sibling order is tab order and reading order, so a reversed footer or a
        // reordered item list is a real parity break. Key-based pairing matches these two
        // across their positions, which is what makes the reorder invisible without an
        // explicit check.
        var context = Context(
            Node("div", Node("button"), Node("ul")),
            Node("div", Node("ul"), Node("button")));

        var findings = new StructureComparator().Compare(context).ToList();

        // One finding for the sibling list, not one per node that moved.
        findings.Count.ShouldBe(1);
        findings[0].Kind.ShouldBe(FindingKind.Structure);
        findings[0].Severity.ShouldBe(Severity.Error);
        findings[0].NodePath.ShouldBe("div");
        findings[0].ReferenceValue.ShouldBe("button, ul");
        findings[0].CandidateValue.ShouldBe("ul, button");
    }

    [Fact]
    public void StructureReportsNoReorderWhenPairedSiblingsKeepTheirOrder()
    {
        // The span is missing on the candidate, which is a presence difference and
        // nothing more. Only the siblings that paired are ordered against each other, so
        // a dropped node must not also read as a move.
        var context = Context(
            Node("div", Node("button"), Node("span"), Node("ul")),
            Node("div", Node("button"), Node("ul")));

        var findings = new StructureComparator().Compare(context).ToList();

        findings.Count.ShouldBe(1);
        findings[0].NodePath.ShouldBe("span");
    }

    [Fact]
    public void StructureReportsALevelThatOnlyPairedByTag()
    {
        // Nothing under the <div> matches on tag, role, and name together, and the <ul> has
        // two children so there is no wrapper to step through. Dumping both subtrees would
        // emit a finding per node on both legs; pairing by tag reports the one difference
        // that exists — but it must still report it, or the degrade hides a real break.
        var context = Context(
            Node("div", Role("ul", "menu", Node("li"), Node("li"))),
            Node("div", Role("ul", "listbox", Node("li"), Node("li"))));

        var findings = new StructureComparator().Compare(context).ToList();

        findings.Count.ShouldBe(1);
        findings[0].Kind.ShouldBe(FindingKind.Structure);
        findings[0].Severity.ShouldBe(Severity.Error);
        findings[0].NodePath.ShouldBe("ul");
        findings[0].Message.ShouldContain("menu");
        findings[0].Message.ShouldContain("listbox");
    }

    [Fact]
    public void AttributeStillComparesBeneathALevelThatOnlyPairedByTag()
    {
        var context = Context(
            Node("div", Role("ul", "menu", Attributed("li", ("aria-selected", "true")), Node("li"))),
            Node("div", Role("ul", "listbox", Attributed("li", ("aria-selected", "false")), Node("li"))));

        var findings = new AttributeComparator().Compare(context).ToList();

        // The role on the container reads as the attribute difference it is, and the child
        // beneath it is still compared — both of which the subtree dump threw away.
        findings.Select(f => f.Property).ShouldBe(["role", "aria-selected"], ignoreOrder: true);
    }

    [Fact]
    public void StructureCarriesTheFixtureLegAndStep()
    {
        var context = Context(Node("div", Node("button")), Node("div"));

        var finding = new StructureComparator().Compare(context).Single();

        finding.Fixture.ShouldBe("switch/hero");
        finding.Leg.ShouldBe(ParityLeg.BlazorServer);
        finding.Step.ShouldBe("initial");
    }

    [Fact]
    public void AttributeReportsAnAttributeMissingFromTheCandidate()
    {
        var context = Context(
            Node("div", Attributed("button", ("aria-expanded", "false"))),
            Node("div", Node("button")));

        var findings = new AttributeComparator().Compare(context).ToList();

        findings.Count.ShouldBe(1);
        findings[0].Kind.ShouldBe(FindingKind.Attribute);
        findings[0].Severity.ShouldBe(Severity.Error);
        findings[0].Property.ShouldBe("aria-expanded");
        findings[0].ReferenceValue.ShouldBe("false");
        findings[0].CandidateValue.ShouldBeNull();
    }

    [Fact]
    public void AttributeReportsADifferingValue()
    {
        var context = Context(
            Node("div", Attributed("button", ("aria-expanded", "false"))),
            Node("div", Attributed("button", ("aria-expanded", "true"))));

        var finding = new AttributeComparator().Compare(context).Single();

        finding.ReferenceValue.ShouldBe("false");
        finding.CandidateValue.ShouldBe("true");
    }

    [Fact]
    public void AttributeReportsAnExtraCandidateAttribute()
    {
        var context = Context(
            Node("div", Node("button")),
            Node("div", Attributed("button", ("data-pressed", ""))));

        var finding = new AttributeComparator().Compare(context).Single();

        finding.Property.ShouldBe("data-pressed");
        finding.ReferenceValue.ShouldBeNull();
        finding.CandidateValue.ShouldBe(string.Empty);
    }

    [Fact]
    public void AttributeReportsNothingForIdenticalAttributes()
    {
        var tree = Node("div", Attributed("button", ("aria-expanded", "false"), ("type", "button")));

        new AttributeComparator().Compare(Context(tree, tree)).ShouldBeEmpty();
    }

    [Fact]
    public void AttributeLeavesBlazixMarkersToTheMarkerComparator()
    {
        // Otherwise every marker is reported twice, and a marker classified Info in
        // markers.json would still fail the run as an unexplained attribute.
        var context = Context(
            Node("div", Node("input")),
            Node("div", Attributed("input", ("data-blazix-otp-input", ""))));

        new AttributeComparator().Compare(context).ShouldBeEmpty();
    }

    [Fact]
    public void AttributeDoesNotDiffTheSyntheticRootsWrapperAgainstARealRoot()
    {
        // A portal on one leg only makes capture.js emit the '#roots' wrapper there and
        // the root element itself on the other. Diffing the wrapper's empty attribute set
        // against a live root reports every attribute on it as missing — findings about a
        // node that exists in neither page.
        var reference = Node("#roots", Attributed("div", ("data-parity-root", "")));
        var candidate = Attributed("div", ("data-parity-root", ""));

        new AttributeComparator().Compare(Context(reference, candidate)).ShouldBeEmpty();
    }

    [Fact]
    public void AttributeDoesNotDiffARealRootAgainstTheSyntheticRootsWrapper()
    {
        // The mirror of the case above — Blazor portals, React renders inline — which is
        // arguably the likelier Blazix defect of the two.
        var reference = Attributed("div", ("data-parity-root", ""));
        var candidate = Node("#roots", Attributed("div", ("data-parity-root", "")));

        new AttributeComparator().Compare(Context(reference, candidate)).ShouldBeEmpty();
    }

    [Fact]
    public void MarkerSkipsTheSyntheticRootsWrapper()
    {
        // capture.js always emits the wrapper with an empty attribute set, so the marker on
        // it here is what makes visiting it observable at all. The wrapper is not a DOM
        // node — its path is the empty string — so a finding originating there would name
        // an element in neither page.
        var candidate = new DomNode
        {
            Tag = "#roots",
            Path = string.Empty,
            Attributes = new Dictionary<string, string> { ["data-blazix-unclassified-marker"] = string.Empty },
            Classes = [],
            Text = string.Empty,
            Children = [Attributed("span", ("data-blazix-unclassified-marker", ""))]
        };

        var finding = new MarkerComparator().Compare(Context(Node("div"), candidate)).Single();

        finding.NodePath.ShouldBe("span");
    }

    [Fact]
    public void MarkerReportsAnUnclassifiedMarkerAsAnError()
    {
        var context = Context(
            Node("div"),
            Node("div", Attributed("span", ("data-blazix-unclassified-marker", ""))));

        var finding = new MarkerComparator().Compare(context).Single();

        finding.Kind.ShouldBe(FindingKind.Marker);
        finding.Severity.ShouldBe(Severity.Error);
        finding.Property.ShouldBe("data-blazix-unclassified-marker");
        finding.Message.ShouldBe(
            "Unclassified Blazix marker 'data-blazix-unclassified-marker'. Add it to " +
            "manifest/markers.json with a reason, or rename it to its data-base-ui-* counterpart.");
    }

    [Fact]
    public void MarkerReportsAListedMarkerAsInfo()
    {
        var context = Context(
            Node("div"),
            Node("div", Attributed("input", ("data-blazix-otp-input", ""))));

        var finding = new MarkerComparator().Compare(context).Single();

        finding.Kind.ShouldBe(FindingKind.Marker);
        finding.Severity.ShouldBe(Severity.Info);
        finding.Property.ShouldBe("data-blazix-otp-input");
        // The reason line from markers.json travels with the finding, so a reader never
        // has to open the manifest to learn why the marker is allowed to exist.
        finding.Message.ShouldContain("Slot input lookup for the JS OTP module.");
    }

    [Fact]
    public void MarkerIgnoresNormalizedAndUnmarkedAttributes()
    {
        // capture.js already rewrites data-blazix-base-ui-* to its upstream spelling,
        // so only markers with no upstream counterpart reach this comparator.
        var context = Context(
            Node("div"),
            Node("div", Attributed("span", ("data-base-ui-inert", ""), ("aria-hidden", "true"))));

        new MarkerComparator().Compare(context).ShouldBeEmpty();
    }

    [Fact]
    public void EachComparatorOwnsOneKind()
    {
        new StructureComparator().Kind.ShouldBe(FindingKind.Structure);
        new AttributeComparator().Kind.ShouldBe(FindingKind.Attribute);
        new MarkerComparator().Kind.ShouldBe(FindingKind.Marker);
    }

    private static ComparisonContext Context(DomNode reference, DomNode candidate) => new(
        "switch/hero",
        ParityLeg.BlazorServer,
        "initial",
        Capture(reference),
        Capture(candidate),
        0.001);

    private static StepCapture Capture(DomNode dom) => new()
    {
        Step = "initial",
        Dom = dom,
        Styles = new Dictionary<string, IReadOnlyDictionary<string, string>>(),
        CustomProps = new Dictionary<string, IReadOnlyDictionary<string, string>>(),
        Geometry = new Dictionary<string, IReadOnlyDictionary<string, double>>()
    };

    private static DomNode Node(string tag, params DomNode[] children) => new()
    {
        Tag = tag,
        Path = tag,
        Attributes = new Dictionary<string, string>(),
        Classes = [],
        Text = string.Empty,
        Children = children
    };

    private static DomNode Role(string tag, string role, params DomNode[] children) => new()
    {
        Tag = tag,
        Path = tag,
        Attributes = new Dictionary<string, string> { ["role"] = role },
        Classes = [],
        Text = string.Empty,
        Children = children
    };

    private static DomNode Attributed(string tag, params (string Name, string Value)[] attributes) => new()
    {
        Tag = tag,
        Path = tag,
        Attributes = attributes.ToDictionary(a => a.Name, a => a.Value),
        Classes = [],
        Text = string.Empty,
        Children = []
    };
}
