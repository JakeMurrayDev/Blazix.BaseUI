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
    public void PairsBeneathAnExtraWrapperWhenTheReferenceLeftoverAlsoHasOneChild()
    {
        // The canonical positioner/popup shape: React <div positioner><div popup><p/></div></div>
        // against Blazor's extra <span wrapper> around the popup. Both leftovers have exactly
        // one child, so stepping the two sides in lockstep reports the popup React *does*
        // render as absent from Blazor and leaves the two popups unpaired for good — and the
        // attribute comparator only walks pairs, so role, data-open, data-side, and every
        // aria-* on the one element this harness exists to check would go uncompared.
        var reference = Node("div", Node("div", Node("p")));
        var candidate = Node("div", Node("span", Node("div", Node("p"))));

        var result = NodeMatcher.Match(reference, candidate);

        result.CandidateOnly.Select(n => n.Tag).ShouldBe(["span"]);
        result.ReferenceOnly.ShouldBeEmpty();

        // The positioner and the popup, not just the positioner.
        result.Pairs.Count(p => p.Reference.Tag == "div").ShouldBe(2);
        result.Pairs.ShouldContain(p => p.Reference.Tag == "p" && p.Candidate.Tag == "p");
    }

    [Fact]
    public void PairsAContainerWhoseRoleDiffersAndKeepsComparingBeneathIt()
    {
        // A role-only difference on a container. Dumping both subtrees would cost every
        // attribute, style, and geometry comparison beneath them to report what is one
        // attribute on one element.
        var reference = Node("div", Role("ul", "menu", Node("li"), Node("li")));
        var candidate = Node("div", Role("ul", "listbox", Node("li"), Node("li")));

        var result = NodeMatcher.Match(reference, candidate);

        result.ReferenceOnly.ShouldBeEmpty();
        result.CandidateOnly.ShouldBeEmpty();
        result.Pairs.ShouldContain(p => p.Reference.Tag == "ul" && p.Candidate.Tag == "ul");
        result.Pairs.Count(p => p.Reference.Tag == "li").ShouldBe(2);

        // Force-pairing must not make the difference vanish.
        var relaxed = result.Relaxed.ShouldHaveSingleItem();
        relaxed.Pair.Reference.Tag.ShouldBe("ul");
        relaxed.ReferenceIdentity.ShouldContain("menu");
        relaxed.CandidateIdentity.ShouldContain("listbox");
    }

    [Fact]
    public void PairsAContainerWhoseRoleDiffersWhenEachSideHasExactlyOneChild()
    {
        // The dominant real-world shape and the one the degrade above could not reach: a
        // popup carrying a single child on both legs. Every leftover at the level has
        // exactly one child, so a matcher that unwraps before it degrades steps straight
        // past the two popups and reports each of them as one-sided — two Structure
        // findings that contradict each other, and *no* attribute finding for the missing
        // role, because only pairs are ever diffed attribute by attribute.
        var reference = Node("div", Popup("dialog", Text("p", "hi")));
        var candidate = Node("div", Popup(role: null, Text("p", "hi")));

        var result = NodeMatcher.Match(reference, candidate);

        result.ReferenceOnly.ShouldBeEmpty();
        result.CandidateOnly.ShouldBeEmpty();

        // The outer div and the popup, and the paragraph beneath the popup.
        result.Pairs.Count(p => p.Reference.Tag == "div").ShouldBe(2);
        result.Pairs.ShouldContain(p => p.Reference.Tag == "p" && p.Candidate.Tag == "p");

        var relaxed = result.Relaxed.ShouldHaveSingleItem();
        relaxed.ReferenceIdentity.ShouldContain("dialog");
        relaxed.CandidateIdentity.ShouldBe("<div>");
    }

    [Fact]
    public void PairsAContainerWhoseRoleDiffersWhenTheTwoSidesHoldDifferentChildCounts()
    {
        // Asymmetric child counts, which is what makes only one side steppable. A matcher
        // that unwraps before it degrades commits that one-sided step even though it
        // unblocks nothing, and then compares the <li> against the <ul> — five one-sided
        // nodes and no attribute finding, for one role difference plus one extra <li>.
        var reference = Node("div", Role("ul", "menu", Node("li")));
        var candidate = Node("div", Role("ul", "listbox", Node("li"), Node("li")));

        var result = NodeMatcher.Match(reference, candidate);

        result.ReferenceOnly.ShouldBeEmpty();
        result.CandidateOnly.Select(n => n.Tag).ShouldBe(["li"]);
        result.Pairs.ShouldContain(p => p.Reference.Tag == "ul" && p.Candidate.Tag == "ul");
        result.Pairs.Count(p => p.Reference.Tag == "li").ShouldBe(1);

        var relaxed = result.Relaxed.ShouldHaveSingleItem();
        relaxed.ReferenceIdentity.ShouldContain("menu");
        relaxed.CandidateIdentity.ShouldContain("listbox");
    }

    [Fact]
    public void FlagsARelaxedPairOnThePairListItself()
    {
        // Later comparators iterate Pairs and diff styles and geometry across it. A pair
        // the matcher does not itself hold to be the same element has to say so there, not
        // only on a side list a consumer has to remember to cross-reference.
        var reference = Node("div", Role("li", "menuitem"));
        var candidate = Node("div", Role("li", "option"));

        var result = NodeMatcher.Match(reference, candidate);

        result.Pairs.ShouldContain(p => p.Reference.Tag == "li" && p.Relaxed);
        result.Pairs.ShouldContain(p => p.Reference.Tag == "div" && !p.Relaxed);
        result.Relaxed.ShouldHaveSingleItem().Pair.Relaxed.ShouldBeTrue();
    }

    [Fact]
    public void DoesNotForcePairAGenuinelyAbsentSubtree()
    {
        // Same trigger as above — nothing pairs and nothing can be stepped — but the tags
        // differ too, so the last resort must decline and report both sides as one-sided.
        var reference = Node("div", Node("ul", Node("li"), Node("li")));
        var candidate = Node("div", Node("section", Node("p"), Node("p")));

        var result = NodeMatcher.Match(reference, candidate);

        result.Relaxed.ShouldBeEmpty();
        result.Pairs.ShouldNotContain(p => p.Reference.Tag == "ul");
        result.ReferenceOnly.Select(n => n.Tag).ShouldBe(["ul", "li", "li"], ignoreOrder: true);
        result.CandidateOnly.Select(n => n.Tag).ShouldBe(["section", "p", "p"], ignoreOrder: true);
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
    public void ReportsNoReorderWhenAnExtraSameKeySiblingIsInsertedAhead()
    {
        // Blazor renders one extra leading item. Taking the earliest candidate of a key
        // regardless of position pairs the <b> React renders second with the inserted one
        // rendered first, so the indices step backwards and a move is reported. Nothing
        // moved — a node was inserted, and the extra node is already reported on its own.
        var reference = Node("div", Node("a"), Node("b"));
        var candidate = Node("div", Node("b"), Node("a"), Node("b"));

        var result = NodeMatcher.Match(reference, candidate);

        result.Reorders.ShouldBeEmpty();
        result.CandidateOnly.Select(n => n.Tag).ShouldBe(["b"]);
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
    public void ReportsAnExtraCaptureRootAsCandidateOnly()
    {
        // The mirror of the case above, and the likelier Blazix defect: Blazor portals
        // where React renders inline. Root normalization is symmetric, so swapping the two
        // legs must produce the same result with the sides exchanged.
        var reference = Node("div", Node("button"));
        var candidate = Node("#roots", Node("div", Node("button")), Node("div", Node("dialog")));

        var result = NodeMatcher.Match(reference, candidate);

        result.Pairs.ShouldNotContain(p => p.Reference.Tag == "#roots" || p.Candidate.Tag == "#roots");
        result.CandidateOnly.Select(n => n.Tag).ShouldBe(["div", "dialog"], ignoreOrder: true);
        result.ReferenceOnly.ShouldBeEmpty();

        result.Pairs.ShouldContain(p => p.Reference.Tag == "div" && p.Candidate.Tag == "div");
        result.Pairs.ShouldContain(p => p.Reference.Tag == "button" && p.Candidate.Tag == "button");
    }

    [Fact]
    public void DoesNotPairSiblingsWithDifferentAccessibleNames()
    {
        // The <hr> pairs, which keeps the level off the last-resort tag pairing. This test
        // is about the key itself, so it has to exercise the ordinary path.
        var reference = Node("div", Node("hr"), Text("button", "Save"));
        var candidate = Node("div", Node("hr"), Text("button", "Cancel"));

        var result = NodeMatcher.Match(reference, candidate);

        result.ReferenceOnly.Select(n => n.Text).ShouldBe(["Save"], ignoreOrder: true);
        result.CandidateOnly.Select(n => n.Text).ShouldBe(["Cancel"], ignoreOrder: true);
    }

    [Fact]
    public void DoesNotPairAcrossDifferentRoles()
    {
        var reference = Node("div", Node("hr"), Role("li", "menuitem"));
        var candidate = Node("div", Node("hr"), Role("li", "option"));

        var result = NodeMatcher.Match(reference, candidate);

        result.ReferenceOnly.Select(n => n.Attributes["role"]).ShouldBe(["menuitem"], ignoreOrder: true);
        result.CandidateOnly.Select(n => n.Attributes["role"]).ShouldBe(["option"], ignoreOrder: true);
    }

    [Fact]
    public void PairsAMismatchedLeafByTagOnceNothingElseAtTheLevelMatches()
    {
        // Without the sibling above, the same two nodes are all the level has. Reporting
        // them as two unrelated elements loses the one fact worth reporting: they are the
        // same element with a different role.
        var reference = Node("div", Role("li", "menuitem"));
        var candidate = Node("div", Role("li", "option"));

        var result = NodeMatcher.Match(reference, candidate);

        result.Pairs.ShouldContain(p => p.Reference.Tag == "li" && p.Candidate.Tag == "li");
        result.Relaxed.ShouldHaveSingleItem().ReferenceIdentity.ShouldContain("menuitem");
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
}
