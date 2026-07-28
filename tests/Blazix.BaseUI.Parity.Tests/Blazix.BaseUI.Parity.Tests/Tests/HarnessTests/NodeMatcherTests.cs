using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Diff;
using Shouldly;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

/// <summary>
/// Pins the tree-matching primitive every comparator pairs nodes with.
/// </summary>
public sealed class NodeMatcherTests
{
    [Fact]
    public void PairsIdenticalTrees()
    {
        var tree = Node("div", Node("button"));

        var result = NodeMatcher.Match(tree, tree);

        result.Pairs.Count.ShouldBe(2);
        result.ReferenceOnly.ShouldBeEmpty();
        result.CandidateOnly.ShouldBeEmpty();
    }

    [Fact]
    public void ReportsAnExtraWrapperAsCandidateOnly()
    {
        var reference = Node("div", Node("button"));
        var candidate = Node("div", Node("span", Node("button")));

        var result = NodeMatcher.Match(reference, candidate);

        result.CandidateOnly.ShouldContain(n => n.Tag == "span");
    }

    [Fact]
    public void KeepsMatchingBeneathAnExtraWrapper()
    {
        // The wrapper is reported once and the button underneath still pairs, so a
        // single extra element does not blank out every comparison below it.
        var reference = Node("div", Node("button"));
        var candidate = Node("div", Node("span", Node("button")));

        var result = NodeMatcher.Match(reference, candidate);

        result.CandidateOnly.Count.ShouldBe(1);
        result.ReferenceOnly.ShouldBeEmpty();
        result.Pairs.ShouldContain(p => p.Reference.Tag == "button" && p.Candidate.Tag == "button");
    }

    [Fact]
    public void ReportsAMissingNodeAndItsSubtreeAsReferenceOnly()
    {
        var reference = Node("div", Node("button"), Node("ul", Node("li")));
        var candidate = Node("div", Node("button"));

        var result = NodeMatcher.Match(reference, candidate);

        result.ReferenceOnly.Select(n => n.Tag).ShouldBe(["ul", "li"], ignoreOrder: true);
        result.CandidateOnly.ShouldBeEmpty();
    }

    [Fact]
    public void PairsRepeatedSameKeySiblingsByOrdinal()
    {
        var reference = Node("div", Node("span"), Node("span"), Node("span"));
        var candidate = Node("div", Node("span"), Node("span"));

        var result = NodeMatcher.Match(reference, candidate);

        // Root plus the two spans the candidate has; the third has nothing to pair with.
        result.Pairs.Count.ShouldBe(3);
        result.ReferenceOnly.Select(n => n.Tag).ShouldBe(["span"]);
    }

    [Fact]
    public void ReportsSiblingsThatPairedOutOfOrder()
    {
        var reference = Node("div", Node("button"), Node("ul"));
        var candidate = Node("div", Node("ul"), Node("button"));

        var result = NodeMatcher.Match(reference, candidate);

        // Both siblings pair — the difference is only where they sit.
        result.ReferenceOnly.ShouldBeEmpty();
        result.CandidateOnly.ShouldBeEmpty();

        var reorder = result.Reorders.ShouldHaveSingleItem();
        reorder.ParentPath.ShouldBe("div");
        reorder.ReferenceOrder.ShouldBe(["button", "ul"]);
        reorder.CandidateOrder.ShouldBe(["ul", "button"]);
    }

    [Fact]
    public void ReportsNoReorderWhenPairedSiblingsKeepTheirOrder()
    {
        var reference = Node("div", Node("button"), Node("span"), Node("ul"));
        var candidate = Node("div", Node("button"), Node("ul"));

        var result = NodeMatcher.Match(reference, candidate);

        result.Reorders.ShouldBeEmpty();
    }

    [Fact]
    public void ReportsAnExtraCaptureRootAsReferenceOnly()
    {
        // capture.js emits the root element itself when a fixture has one root and a
        // synthetic '#roots' wrapper when content is portalled, so the two legs do not
        // even agree on what the top of the tree is. React opening a portal while Blazor
        // renders inline is a first-order parity defect; pairing '#roots' with the real
        // root would compare the wrapper's empty attribute set against a live element and
        // push every real root's children one level out of step.
        var reference = Node("#roots", Node("div", Node("button")), Node("div", Node("dialog")));
        var candidate = Node("div", Node("button"));

        var result = NodeMatcher.Match(reference, candidate);

        result.Pairs.ShouldNotContain(p => p.Reference.Tag == "#roots" || p.Candidate.Tag == "#roots");
        result.ReferenceOnly.Select(n => n.Tag).ShouldBe(["div", "dialog"], ignoreOrder: true);
        result.CandidateOnly.ShouldBeEmpty();

        // The root both legs do share still pairs, and still matches beneath itself.
        result.Pairs.ShouldContain(p => p.Reference.Tag == "div" && p.Candidate.Tag == "div");
        result.Pairs.ShouldContain(p => p.Reference.Tag == "button" && p.Candidate.Tag == "button");
    }

    [Fact]
    public void DoesNotPairSiblingsWithDifferentAccessibleNames()
    {
        var reference = Node("div", Text("button", "Save"));
        var candidate = Node("div", Text("button", "Cancel"));

        var result = NodeMatcher.Match(reference, candidate);

        result.ReferenceOnly.ShouldContain(n => n.Text == "Save");
        result.CandidateOnly.ShouldContain(n => n.Text == "Cancel");
    }

    [Fact]
    public void DoesNotPairAcrossDifferentRoles()
    {
        var reference = Node("div", Role("li", "menuitem"));
        var candidate = Node("div", Role("li", "option"));

        var result = NodeMatcher.Match(reference, candidate);

        result.ReferenceOnly.ShouldContain(n => n.Attributes["role"] == "menuitem");
        result.CandidateOnly.ShouldContain(n => n.Attributes["role"] == "option");
    }

    private static DomNode Node(string tag, params DomNode[] children) => new()
    {
        Tag = tag,
        Path = tag,
        Attributes = new Dictionary<string, string>(),
        Classes = [],
        Text = string.Empty,
        Children = children
    };

    private static DomNode Text(string tag, string text) => new()
    {
        Tag = tag,
        Path = tag,
        Attributes = new Dictionary<string, string>(),
        Classes = [],
        Text = text,
        Children = []
    };

    private static DomNode Role(string tag, string role) => new()
    {
        Tag = tag,
        Path = tag,
        Attributes = new Dictionary<string, string> { ["role"] = role },
        Classes = [],
        Text = string.Empty,
        Children = []
    };
}
