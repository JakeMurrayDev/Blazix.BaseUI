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
    public void StructureReportsARoleDifferenceOnContainersThatEachHoldOneChild()
    {
        // The popup shape this harness exists to check: one child on both legs, so every
        // leftover at the level is unwrappable. Reporting the popup as React-only *and* as
        // Blazor-only is two findings that contradict each other and are both false.
        var context = Context(
            Node("div", Popup("dialog", Text("p", "hi"))),
            Node("div", Popup(role: null, Text("p", "hi"))));

        var findings = new StructureComparator().Compare(context).ToList();

        findings.Count.ShouldBe(1);
        findings[0].Kind.ShouldBe(FindingKind.Structure);
        findings[0].NodePath.ShouldBe("div");
        findings[0].Message.ShouldContain("dialog");
    }

    [Fact]
    public void AttributeReportsTheMissingRoleOnContainersThatEachHoldOneChild()
    {
        // The finding the whole degrade exists to produce. Without the pair there is no
        // attribute comparison at all, and the likeliest Blazix popup defect — a dropped
        // role — leaves no attribute finding anywhere in the run.
        var context = Context(
            Node("div", Popup("dialog", Text("p", "hi"))),
            Node("div", Popup(role: null, Text("p", "hi"))));

        var findings = new AttributeComparator().Compare(context).ToList();

        // data-open agrees on both legs, so the role is the only difference.
        findings.Select(f => f.Property).ShouldBe(["role"]);
        findings[0].ReferenceValue.ShouldBe("dialog");
        findings[0].CandidateValue.ShouldBeNull();
    }

    [Fact]
    public void StructureReportsOnlyTheIdentityAndTheExtraChildWhenChildCountsDiffer()
    {
        // Asymmetric child counts: one <li> against two. Stepping the one side that can be
        // stepped unblocks nothing and leaves the <li> compared against the <ul>, which
        // dumps both subtrees — five findings for one role difference plus one extra node.
        var context = Context(
            Node("div", Role("ul", "menu", Node("li"))),
            Node("div", Role("ul", "listbox", Node("li"), Node("li"))));

        var findings = new StructureComparator().Compare(context).ToList();

        findings.Count.ShouldBe(2);
        findings.ShouldContain(f => f.Message.Contains("menu") && f.Message.Contains("listbox"));
        findings.ShouldContain(f => f.CandidateValue == "li" && f.NodePath == "li");
    }

    [Fact]
    public void AttributeReportsTheRoleDifferenceWhenChildCountsDiffer()
    {
        var context = Context(
            Node("div", Role("ul", "menu", Node("li"))),
            Node("div", Role("ul", "listbox", Node("li"), Node("li"))));

        var findings = new AttributeComparator().Compare(context).ToList();

        findings.Select(f => f.Property).ShouldBe(["role"]);
        findings[0].ReferenceValue.ShouldBe("menu");
        findings[0].CandidateValue.ShouldBe("listbox");
    }

    [Fact]
    public void StructureReportsOnlyTheWrapperWhenItsKeyCollidesWithASibling()
    {
        var findings = new StructureComparator().Compare(WrapperContext()).ToList();

        // One extra element, one finding. Before the tiebreak the body was consumed as if it
        // were the wrapper, which produced five findings — two claiming React renders a <p>
        // Blazor does not, three claiming the reverse of nodes both legs render.
        findings.Count.ShouldBe(1);
        findings[0].NodePath.ShouldBe("dlg>wrap");
        findings[0].Message.ShouldBe("Blazor renders <div> at 'dlg>wrap'; React does not.");
    }

    [Fact]
    public void AttributeReportsNothingWhenAnExtraWrapperKeyCollidesWithASibling()
    {
        // The body's attributes were being diffed against the wrapper's empty set, so
        // 'data-body' read as missing from Blazor. Blazor renders it — on the element one
        // level in. A fabricated attribute finding is worse than a missed one: it sends a
        // reader to an element that is not the one that differs.
        new AttributeComparator().Compare(WrapperContext()).ShouldBeEmpty();
    }

    [Fact]
    public void StructureReconcilesTwoLeftoversAtOnePathIntoOneTruthfulFinding()
    {
        // React <ul>(<li role=menuitem>A, ...) against Blazor with A's role as 'option'. The
        // second <li> pairs, which keeps the level off the tag degrade, so both <li> A nodes
        // are left over. "React renders <li> at 'ul>li(1)'; Blazor does not" and its mirror
        // are each false and they contradict each other — Blazor renders an <li> at exactly
        // that path. The pairing here is the plan working as designed; only the wording was
        // wrong, and both sides are knowable at report time.
        var findings = new StructureComparator().Compare(ListContext()).ToList();

        var finding = findings.Where(f => f.NodePath == "ul>li(1)").ShouldHaveSingleItem();
        finding.ReferenceValue.ShouldBe("<li> role 'menuitem' named 'A'");
        finding.CandidateValue.ShouldBe("<li> role 'option' named 'A'");
        finding.Message.ShouldBe(
            "React renders <li> role 'menuitem' named 'A' at 'ul>li(1)'; Blazor renders " +
            "<li> role 'option' named 'A' there. The two did not pair, so nothing beneath " +
            "them was compared.");
    }

    [Fact]
    public void StructureStillReportsAGenuinelyAbsentNodeAsAbsent()
    {
        // The other half of the reconciliation. The third <li> has no counterpart at its
        // path on either leg, so it must keep reading as absent — otherwise the fix for the
        // contradictory pair costs the finding this comparator exists for.
        var findings = new StructureComparator().Compare(ListContext()).ToList();

        findings.Count.ShouldBe(2);
        var finding = findings.Where(f => f.NodePath == "ul>li(3)").ShouldHaveSingleItem();
        finding.ReferenceValue.ShouldBe("li");
        finding.CandidateValue.ShouldBeNull();
        finding.Message.ShouldBe("React renders <li> at 'ul>li(3)'; Blazor does not.");
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
    public void MarkerReportsAListedNormalizedMarkerAsInfo()
    {
        // The shape capture.js actually delivers: Blazix renders
        // data-blazix-base-ui-positioner and the capture renames it before the comparator
        // ever sees it. Keying markers.json on the Blazix spelling made every entry of this
        // family unreachable, so the classification the manifest was written for never ran.
        var context = Context(
            Node("div", Node("span")),
            Node("div", Attributed("span", ("data-base-ui-positioner", ""))));

        var finding = new MarkerComparator().Compare(context).Single();

        finding.Kind.ShouldBe(FindingKind.Marker);
        finding.Severity.ShouldBe(Severity.Info);
        finding.Property.ShouldBe("data-base-ui-positioner");
        finding.Message.ShouldContain("Positioner element lookup for the JS floating module.");
    }

    [Fact]
    public void ListedMarkerIsReportedOnceAcrossBothComparators()
    {
        // React renders no data-base-ui-positioner, so the name is one-sided and
        // AttributeComparator would otherwise report it too — an unexplained Error beside
        // the Info that explains it, for one attribute.
        var context = Context(
            Node("div", Node("span")),
            Node("div", Attributed("span", ("data-base-ui-positioner", ""))));

        var findings = new MarkerComparator().Compare(context)
            .Concat(new AttributeComparator().Compare(context))
            .ToList();

        var finding = findings.ShouldHaveSingleItem();
        finding.Kind.ShouldBe(FindingKind.Marker);
        finding.Severity.ShouldBe(Severity.Info);
    }

    [Fact]
    public void MarkerLeavesAnUnlistedUpstreamNameAlone()
    {
        // Nothing tells this comparator that an unlisted data-base-ui-* name is a marker
        // rather than an attribute Blazor renders and React does not, and the second is a
        // parity defect. Claiming it here would classify a defect as an intentional marker.
        var context = Context(
            Node("div", Node("span")),
            Node("div", Attributed("span", ("data-base-ui-inert", ""), ("aria-hidden", "true"))));

        new MarkerComparator().Compare(context).ShouldBeEmpty();
    }

    [Fact]
    public void AttributeReportsAnUnlistedUpstreamName()
    {
        // The other half: leaving it to AttributeComparator is only correct if
        // AttributeComparator actually reports it.
        var context = Context(
            Node("div", Node("span")),
            Node("div", Attributed("span", ("data-base-ui-inert", ""))));

        var finding = new AttributeComparator().Compare(context).Single();

        finding.Kind.ShouldBe(FindingKind.Attribute);
        finding.Severity.ShouldBe(Severity.Error);
        finding.Property.ShouldBe("data-base-ui-inert");
        finding.ReferenceValue.ShouldBeNull();
        finding.CandidateValue.ShouldBe(string.Empty);
    }

    [Fact]
    public void EachComparatorOwnsOneKind()
    {
        new StructureComparator().Kind.ShouldBe(FindingKind.Structure);
        new AttributeComparator().Kind.ShouldBe(FindingKind.Attribute);
        new MarkerComparator().Kind.ShouldBe(FindingKind.Marker);
    }

    /// <summary>
    /// The dialog with one extra wrapper around its body on the Blazor leg. No node in it
    /// has an identity difference, and the wrapper's key is the body's key.
    /// </summary>
    private static ComparisonContext WrapperContext() => Context(
        AtRole("div", "dlg", "dialog",
            AtMarked("div", "dlg>body", "data-body",
                At("p", "dlg>body>p"),
                At("p", "dlg>body>q")),
            AtMarked("div", "dlg>foot", "data-footer",
                At("button", "dlg>foot>ok"))),
        AtRole("div", "dlg", "dialog",
            At("div", "dlg>wrap",
                AtMarked("div", "dlg>wrap>body", "data-body",
                    At("p", "dlg>wrap>body>p"),
                    At("p", "dlg>wrap>body>q"))),
            AtMarked("div", "dlg>foot", "data-footer",
                At("button", "dlg>foot>ok"))));

    /// <summary>
    /// A list whose first item's role differs and whose second item pairs, plus a third item
    /// Blazor does not render at all.
    /// </summary>
    private static ComparisonContext ListContext() => Context(
        At("ul", "ul",
            Item("ul>li(1)", "menuitem", "A"),
            Item("ul>li(2)", "menuitem", "B"),
            Item("ul>li(3)", "menuitem", "C")),
        At("ul", "ul",
            Item("ul>li(1)", "option", "A"),
            Item("ul>li(2)", "menuitem", "B")));

    private static DomNode At(string tag, string path, params DomNode[] children)
        => At(tag, path, [], children);

    private static DomNode AtRole(string tag, string path, string role, params DomNode[] children)
        => At(tag, path, new Dictionary<string, string> { ["role"] = role }, children);

    private static DomNode AtMarked(
        string tag, string path, string attribute, params DomNode[] children)
        => At(tag, path, new Dictionary<string, string> { [attribute] = string.Empty }, children);

    private static DomNode At(
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

    private static DomNode Item(string path, string role, string text) => new()
    {
        Tag = "li",
        Path = path,
        Attributes = new Dictionary<string, string> { ["role"] = role },
        Classes = [],
        Text = text,
        Children = []
    };

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

    /// <summary>
    /// Builds the popup shape: a div carrying <c>data-open</c>, which both legs agree on,
    /// and optionally the role, which is the only thing they differ by.
    /// </summary>
    private static DomNode Popup(string? role, params DomNode[] children)
    {
        var attributes = new Dictionary<string, string> { ["data-open"] = string.Empty };
        if (role is not null)
        {
            attributes["role"] = role;
        }

        return new DomNode
        {
            Tag = "div",
            Path = "div",
            Attributes = attributes,
            Classes = [],
            Text = string.Empty,
            Children = children
        };
    }

    private static DomNode Text(string tag, string text) => new()
    {
        Tag = tag,
        Path = tag,
        Attributes = new Dictionary<string, string>(),
        Classes = [],
        Text = text,
        Children = []
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
