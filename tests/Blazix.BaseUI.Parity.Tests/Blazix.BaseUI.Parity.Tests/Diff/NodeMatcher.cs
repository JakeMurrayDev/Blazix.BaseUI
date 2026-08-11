using Blazix.BaseUI.Parity.Tests.Capture;

namespace Blazix.BaseUI.Parity.Tests.Diff;

/// <summary>
/// A reference node and the candidate node it was matched with.
/// </summary>
/// <param name="Reference">The node from the React snapshot.</param>
/// <param name="Candidate">The node from the Blazor snapshot.</param>
/// <param name="Relaxed">
/// Whether the matcher could not prove correspondence, either because it relaxed the key
/// to the tag alone or because unequal duplicate-key leaves were positionally ambiguous.
/// Carried here, and not only on
/// <see cref="NodeMatchResult.Relaxed"/>, because <see cref="NodeMatchResult.Pairs"/> is
/// what a comparator iterates: a style or geometry difference read across a relaxed pair is
/// a difference between two elements that may not correspond, and nothing else on the list
/// says so.
/// </param>
public sealed record NodePair(DomNode Reference, DomNode Candidate, bool Relaxed = false);

/// <summary>
/// One sibling list whose matched nodes sit in a different relative order on the two legs.
/// </summary>
/// <param name="ParentPath">
/// The reference path of the node whose children were reordered, or
/// <see cref="CaptureNames.RootsWrapper"/> for the capture roots themselves.
/// </param>
/// <param name="ReferenceOrder">
/// The reference paths of the siblings that matched, in React's document order.
/// </param>
/// <param name="CandidateOrder">The same paths, in Blazor's document order.</param>
public sealed record SiblingReorder(
    string ParentPath,
    IReadOnlyList<string> ReferenceOrder,
    IReadOnlyList<string> CandidateOrder);

/// <summary>
/// A pair whose fallback match could not prove correspondence.
/// </summary>
/// <remarks>
/// The pair is in <see cref="NodeMatchResult.Pairs"/> like any other, so everything beneath
/// it is still compared, and it carries <see cref="NodePair.Relaxed"/> there. It is listed
/// here as well because the two nodes are not the same element by the matcher's own
/// definition, and a degrade that let that difference vanish would be worse than reporting
/// the two subtrees as unrelated.
/// </remarks>
/// <param name="Pair">The matched nodes.</param>
/// <param name="ReferenceIdentity">The reference node's tag, role, and accessible name.</param>
/// <param name="CandidateIdentity">The same for the candidate node.</param>
public sealed record RelaxedPair(NodePair Pair, string ReferenceIdentity, string CandidateIdentity);

/// <summary>
/// The outcome of matching two DOM snapshots.
/// </summary>
/// <remarks>
/// Init-only rather than positional: every comparator built on this primitive names the
/// member it reads, so a member added later is an additive change instead of a break on
/// each of them.
/// </remarks>
public sealed record NodeMatchResult
{
    /// <summary>Gets the nodes that matched, including any roots that matched.</summary>
    public required IReadOnlyList<NodePair> Pairs { get; init; }

    /// <summary>Gets the nodes React rendered that Blazor did not.</summary>
    public required IReadOnlyList<DomNode> ReferenceOnly { get; init; }

    /// <summary>Gets the nodes Blazor rendered that React did not.</summary>
    public required IReadOnlyList<DomNode> CandidateOnly { get; init; }

    /// <summary>Gets the sibling lists the two legs ordered differently.</summary>
    public required IReadOnlyList<SiblingReorder> Reorders { get; init; }

    /// <summary>
    /// Gets the pairs that only matched on tag. They are in <see cref="Pairs"/> as well,
    /// flagged by <see cref="NodePair.Relaxed"/>.
    /// </summary>
    public required IReadOnlyList<RelaxedPair> Relaxed { get; init; }
}

/// <summary>
/// Pairs the nodes of two DOM snapshots so the other comparators have something to
/// compare attribute by attribute.
/// </summary>
public static class NodeMatcher
{
    // Separates the key parts. A control character cannot occur in a tag name, a role,
    // or an accessible name, so two different keys can never collide once joined.
    private const char KeySeparator = '\u001f';

    /// <summary>
    /// Matches two snapshots, walking both trees in parallel.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Before the Task 10c correspondence repair, the shapes below defeated this matcher.
    /// They remain documented as the failure modes the adversarial suite prevents because
    /// Tasks 8-10 read <see cref="NodeMatchResult.Pairs"/> to diff computed styles and
    /// geometry. The list deliberately carries no count: it grew at every review,
    /// and a number that stops matching invites the next person to leave the text stale.
    /// Across 27 probe trees no real difference produced zero findings — every shape here
    /// still fails the run, just not always for the right reason.
    /// </para>
    /// <para>
    /// Before that repair, <see cref="NodeMatchResult.Pairs"/> could
    /// hold a real element paired with a layout wrapper, or with the wrong same-key sibling.
    /// In both cases the pair carries <see cref="NodePair.Relaxed"/> as
    /// <see langword="false"/> and appears nowhere in <see cref="NodeMatchResult.Relaxed"/>:
    /// those two members flagged the deliberate tag degrade and nothing else, so nothing in
    /// the result flagged a mispairing.
    /// </para>
    /// <list type="number">
    /// <item>
    /// <b>A childless same-key sibling swallows a wrapper.</b> Loud but mislabelled. An
    /// element carrying neither a role nor its own text keys as <c>tag||</c>, which is also
    /// every layout wrapper's key. <see cref="StepUnblocks"/> is what normally unpicks that
    /// collision: it asks <see cref="WrappedCounterpartScore"/> whether the reference is
    /// corroborated by something inside the wrapper, and the old implementation required
    /// <see cref="Corroboration"/> above zero — which a reference with no children can never
    /// reach. So the childless sibling takes the wrapper. React
    /// <c>&lt;div&gt;(&lt;div data-popup&gt;&lt;p/&gt;, &lt;div data-arrow/&gt;)</c> against
    /// the same tree with one extra wrapper around the popup and nothing else changed pairs
    /// the arrow with the wrapper, and emits five Structure findings — two of them false
    /// about both legs — plus a fabricated <c>data-arrow</c> Attribute finding. Give the
    /// arrow a single child of its own and the same tree collapses to one correct finding,
    /// so the trigger is precisely "the colliding sibling is childless" and not the wrapper.
    /// <c>PairsBeneathAWrapperAroundAChildlessSameKeySibling</c> prevents its return.
    /// </item>
    /// <item>
    /// <b>The corroboration tiebreak cross-pairs same-key siblings.</b> Loud but
    /// mislabelled, and it needs no childless node anywhere. <see cref="Choose"/> maximises
    /// <see cref="Corroboration"/> over the flat list of same-key candidates, and the wrapper
    /// lookahead in <see cref="PairBy"/> is consulted only when the chosen candidate scores
    /// exactly zero. A wrapper holds one child — the real element — so it corroborates the
    /// reference at zero, and any sibling sharing even one child key with the reference
    /// outscores it; the guard short-circuits and the lookahead never runs, even when the
    /// node inside the wrapper is a perfect match. On a probe of one extra wrapper plus a
    /// genuine sibling reorder this pairs <c>dlg&gt;body</c> with <c>dlg&gt;foot</c> and then
    /// <c>dlg&gt;foot</c> with <c>dlg&gt;wrap&gt;body</c>, emitting four fabricated Attribute
    /// findings and never reporting the reorder.
    /// </item>
    /// <item>
    /// <b>What 1 and 2 cost: the wrapped subtree is reported one-sided on both legs.</b> The
    /// real element and its descendants land in <see cref="NodeMatchResult.ReferenceOnly"/>
    /// and the wrapped copy and its descendants in
    /// <see cref="NodeMatchResult.CandidateOnly"/>, or the mirror. Nothing in
    /// <see cref="NodeMatchResult.Pairs"/> covers them, so no Attribute, ComputedStyle, or
    /// Geometry finding will ever name them: an ARIA attribute React renders and Blazor drops
    /// inside that subtree produces no finding at all. Add <c>aria-labelledby</c> to the
    /// popup of the probe in 1 and nothing is emitted for the dropped attribute, while the
    /// one Attribute finding that is emitted points at an element that is fine. The run still
    /// fails, on the Structure findings for the one-sided nodes, so this is a mislabelled
    /// positive rather than a silent pass — which is the whole reason it is shippable. Never
    /// read "no Attribute finding here" as "these attributes agree".
    /// </item>
    /// <item>
    /// <b>An identity difference on a node whose sibling paired.</b> Accepted; loud but
    /// coarse. React <c>&lt;div&gt;(&lt;button/&gt;, &lt;ul role=menu&gt;)</c> against Blazor
    /// <c>&lt;div&gt;(&lt;button/&gt;, &lt;ul role=listbox&gt;)</c> pairs the buttons, which
    /// makes <c>pairedHere</c> non-zero and so skips the tag degrade that would otherwise
    /// pair the two lists. Both <c>&lt;ul&gt;</c> nodes are reported one-sided and no
    /// Attribute finding names <c>role</c> — whether the subtree beneath them still pairs
    /// depends on whether the lockstep step happens to reach it. Removing the guard would
    /// turn the degrade into a general relaxation of the key, so this is a trade rather than
    /// an oversight; it is nonetheless the commonest shape here, because a popup usually has
    /// a sibling arrow or backdrop that pairs. <see cref="StructureComparator"/> reconciles
    /// the two leftovers into one truthful finding naming both identities, but there is still
    /// no pair, so nothing on the pair list covers those elements.
    /// </item>
    /// <item>
    /// <b>An extra wrapper on one leg <em>and</em> an identity difference beneath it at the
    /// same level.</b> Accepted; loud but mislabelled. React
    /// <c>&lt;div&gt;&lt;div role=dialog&gt;&lt;p/&gt;</c> against Blazor
    /// <c>&lt;div&gt;&lt;span&gt;&lt;div&gt;&lt;p/&gt;</c>: the one-sided step requires the
    /// key to match <em>after</em> stepping, so it declines, and the tag degrade compares
    /// <c>&lt;div&gt;</c> against <c>&lt;span&gt;</c> and correctly declines too. The lockstep
    /// step then separates the two popups for good, so they and everything beneath them fall
    /// under 3. Closing it needs a search over step-then-degrade combinations rather than one
    /// try of each, which is an algorithmic extension and its own task.
    /// </item>
    /// <item>
    /// <b><see cref="NodeMatchResult.Reorders"/> is recorded for the first pairing pass at a
    /// level only.</b> The quiet one — this loses information rather than mislabelling it.
    /// <see cref="RecordReorder"/> runs once per level, before any wrapper there has been
    /// stepped through, because after a step the candidate indices count positions in a list
    /// that exists in neither DOM and ordering read off them means nothing. That local
    /// reasoning is sound; the consequence is that a genuine sibling reorder below any
    /// stepped level is never reported. Such a level does carry a Structure finding for the
    /// wrapper it stepped, so the run fails, but nothing in the output says the order
    /// differs. Shape 2 above is a worked example.
    /// </item>
    /// </list>
    /// <para>
    /// The repaired matcher searches wrapper chains before accepting a weaker same-key
    /// sibling, permits step-then-tag-degrade for identity changes, and retains original
    /// sibling-branch ordinals so a reorder remains observable after wrapper projection.
    /// Every accepted pair therefore comes from the same original sibling branches used by
    /// Structure evidence; deliberate tag-only uncertainty remains explicit in
    /// <see cref="NodeMatchResult.Relaxed"/>.
    /// </para>
    /// </remarks>
    /// <param name="reference">The React snapshot root.</param>
    /// <param name="candidate">The Blazor snapshot root.</param>
    /// <returns>The pairs, the nodes left over on either side, and the reordered levels.</returns>
    public static NodeMatchResult Match(DomNode reference, DomNode candidate)
    {
        var state = new MatchState();

        // The roots are a sibling list like any other, so they go through the same
        // pairing. Pairing them positionally instead would mispair the case this harness
        // exists to catch: when React portals content and Blazor renders it inline, one
        // leg's snapshot is the synthetic '#roots' wrapper and the other's is the root
        // element, and pairing those puts every root's children one level out of step.
        MatchChildren(Roots(reference), Roots(candidate), CaptureNames.RootsWrapper, state);

        return new NodeMatchResult
        {
            Pairs = state.Pairs,
            ReferenceOnly = state.ReferenceOnly,
            CandidateOnly = state.CandidateOnly,
            Reorders = state.Reorders,
            Relaxed = state.Relaxed
        };
    }

    /// <summary>
    /// Unwraps a snapshot into the roots it holds. The wrapper is synthetic and has no
    /// element behind it, so it is never itself paired or reported.
    /// </summary>
    private static IReadOnlyList<DomNode> Roots(DomNode dom)
        => dom.Tag == CaptureNames.RootsWrapper ? dom.Children : [dom];

    private static void MatchChildren(
        IReadOnlyList<DomNode> referenceChildren,
        IReadOnlyList<DomNode> candidateChildren,
        string parentPath,
        MatchState state)
        => new Level(referenceChildren, candidateChildren, parentPath, state).Match();

    /// <summary>
    /// Builds the list that replacing every single-child node with its child would produce,
    /// without touching <paramref name="nodes"/>.
    /// </summary>
    /// <returns>
    /// The stepped list and the elements stepped through, or <see langword="null"/> when no
    /// node has exactly one child.
    /// </returns>
    private static Step? StepThrough(List<DomNode> nodes)
    {
        List<DomNode>? wrappers = null;
        var stepped = new List<DomNode>(nodes.Count);

        foreach (var node in nodes)
        {
            if (node.Children.Count == 1)
            {
                (wrappers ??= []).Add(node);
                stepped.Add(node.Children[0]);
            }
            else
            {
                stepped.Add(node);
            }
        }

        return wrappers is null ? null : new Step(stepped, wrappers);
    }

    /// <summary>
    /// Steps only single-child nodes whose wrapper chain exposes a more strongly
    /// corroborated same-tag counterpart on the other leg.
    /// </summary>
    private static Step? StepThroughTagWrappers(
        List<DomNode> nodes,
        List<DomNode> counterparts)
    {
        List<DomNode>? wrappers = null;
        var stepped = new List<DomNode>(nodes.Count);

        foreach (var node in nodes)
        {
            if (node.Children.Count == 1 &&
                counterparts.Exists(counterpart =>
                    ExposesStrongerTagCounterpart(counterpart, node)))
            {
                (wrappers ??= []).Add(node);
                stepped.Add(node.Children[0]);
            }
            else
            {
                stepped.Add(node);
            }
        }

        return wrappers is null ? null : new Step(stepped, wrappers);
    }

    /// <summary>
    /// Reports whether walking a wrapper chain reaches a same-tag node whose children
    /// corroborate <paramref name="counterpart"/> more strongly than the outer node does.
    /// </summary>
    private static bool ExposesStrongerTagCounterpart(DomNode counterpart, DomNode wrapper)
    {
        var outerScore = string.Equals(counterpart.Tag, wrapper.Tag, StringComparison.Ordinal)
            ? Corroboration(counterpart, wrapper)
            : -1;

        for (var inner = wrapper; inner.Children.Count == 1; inner = inner.Children[0])
        {
            var child = inner.Children[0];
            if (string.Equals(counterpart.Tag, child.Tag, StringComparison.Ordinal) &&
                Corroboration(counterpart, child) > outerScore)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Applies a step, reporting the elements it stepped through.</summary>
    private static void Commit(List<DomNode> nodes, Step step, List<DomNode> reported)
    {
        reported.AddRange(step.Wrappers);
        nodes.Clear();
        nodes.AddRange(step.Nodes);
    }

    /// <summary>
    /// Reports whether <see cref="PairBy"/> keyed on <see cref="Key"/> would pair anything.
    /// </summary>
    private static bool SharesAKey(List<DomNode> references, List<DomNode> candidates)
    {
        var keys = new HashSet<string>(candidates.Select(Key), StringComparer.Ordinal);

        return references.Exists(reference => keys.Contains(Key(reference)));
    }

    /// <summary>
    /// Reports whether a wrapper projection exposes a node that can participate in the
    /// explicit tag-degrade pass when no full-key pair exists at the level.
    /// </summary>
    private static bool SharesATag(List<DomNode> references, List<DomNode> candidates)
    {
        var tags = new HashSet<string>(candidates.Select(node => node.Tag), StringComparer.Ordinal);

        return references.Exists(reference => tags.Contains(reference.Tag));
    }

    /// <summary>
    /// Records the sibling list when the nodes that matched do not sit in the same
    /// relative order on both legs.
    /// </summary>
    /// <remarks>
    /// One record for the whole list rather than one per node: which of two swapped nodes
    /// is the one that "moved" is not a question a snapshot can answer, and guessing per
    /// node turns a single moved element into a finding for every node it passed.
    /// Siblings that did not match are left out, so a node missing from one leg reads as
    /// the presence difference it is and not also as a move.
    /// </remarks>
    private static void RecordReorder(
        string parentPath,
        List<LevelPair> matched,
        List<SiblingReorder> reorders)
    {
        var referenceOrder = matched
            .OrderBy(item => item.ReferenceIndex)
            .ToArray();

        // Each accepted pair retains the index of the original sibling branch it came
        // through. Those indices remain meaningful after a wrapper is stepped through,
        // unlike an index in the temporary projected list.
        for (var i = 1; i < referenceOrder.Length; i++)
        {
            if (referenceOrder[i].CandidateIndex >= referenceOrder[i - 1].CandidateIndex)
            {
                continue;
            }

            reorders.Add(new SiblingReorder(
                parentPath,
                [.. referenceOrder.Select(item => item.Pair.Reference.Path)],
                [.. referenceOrder
                    .OrderBy(item => item.CandidateIndex)
                    .Select(item => item.Pair.Reference.Path)]));

            return;
        }
    }

    /// <summary>Which pass is pairing, which fixes both the key and how ties are broken.</summary>
    private enum Pass
    {
        /// <summary>Tag, role, and accessible name must all agree.</summary>
        FullKey,

        /// <summary>The tag alone, as the last resort before both subtrees are dumped.</summary>
        TagDegrade
    }

    /// <summary>
    /// Pairs as many nodes as possible on the key <paramref name="pass"/> selects, removing
    /// every paired node from <paramref name="references"/> and <paramref name="candidates"/>.
    /// </summary>
    /// <remarks>
    /// Pairing is by index rather than by set membership because <see cref="DomNode"/> is a
    /// record with value equality: two sibling nodes with identical content are equal, so a
    /// set would collapse them into one. Each pair carries the candidate's index within
    /// <paramref name="candidates"/> so the caller can tell how the two legs ordered the
    /// nodes that paired.
    /// </remarks>
    private static void PairBy(
        List<DomNode> references,
        List<DomNode> candidates,
        Pass pass,
        out List<MatchedPair> matched)
    {
        Func<DomNode, string> key = pass == Pass.FullKey ? Key : static node => node.Tag;
        var relaxed = pass == Pass.TagDegrade;
        var referenceCounts = references
            .GroupBy(key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var candidateCounts = candidates
            .GroupBy(key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        var available = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        for (var i = 0; i < candidates.Count; i++)
        {
            var candidateKey = key(candidates[i]);
            if (!available.TryGetValue(candidateKey, out var indices))
            {
                indices = [];
                available[candidateKey] = indices;
            }

            indices.Add(i);
        }

        matched = [];
        var taken = new bool[candidates.Count];
        var referenceLeftovers = new List<DomNode>();
        var lastMatched = -1;

        foreach (var reference in references)
        {
            if (!available.TryGetValue(key(reference), out var indices) || indices.Count == 0)
            {
                referenceLeftovers.Add(reference);
                continue;
            }

            var slot = Choose(reference, candidates, indices, lastMatched, pass);
            var chosen = indices[slot];

            if (pass == Pass.FullKey
                && StepUnblocks(reference, candidates, taken, chosen))
            {
                // Nothing about this candidate says it is the reference, and something
                // about a wrapper still on the list says the reference's counterpart is one
                // level inside it. Leaving the reference for the step pass is the whole
                // point: a wrapper's key is `tag||`, identical to any plain sibling of the
                // same tag, so taking it here consumes the wrapper as if it were that
                // sibling and the step pass never runs.
                referenceLeftovers.Add(reference);
                continue;
            }

            indices.RemoveAt(slot);
            taken[chosen] = true;
            lastMatched = chosen;
            var pairKey = key(reference);
            var duplicateUncertain = pass == Pass.FullKey &&
                referenceCounts[pairKey] != candidateCounts[pairKey] &&
                Corroboration(reference, candidates[chosen]) == 0 &&
                indices.All(index => Corroboration(reference, candidates[index]) == 0);
            matched.Add(new MatchedPair(
                new NodePair(reference, candidates[chosen], relaxed || duplicateUncertain),
                chosen));
        }

        var candidateLeftovers = new List<DomNode>();
        for (var i = 0; i < candidates.Count; i++)
        {
            if (!taken[i])
            {
                candidateLeftovers.Add(candidates[i]);
            }
        }

        references.Clear();
        references.AddRange(referenceLeftovers);
        candidates.Clear();
        candidates.AddRange(candidateLeftovers);
    }

    /// <summary>
    /// Picks which of the same-key candidates in <paramref name="indices"/> a reference
    /// takes, and returns its slot within that list.
    /// </summary>
    /// <remarks>
    /// The full key is <c>tag|role|name</c>, so an element carrying neither a role nor its
    /// own text keys as <c>tag||</c> — indistinguishable from every plain sibling of the same
    /// tag, and from every layout wrapper. Position alone cannot separate them: in the
    /// dialog-with-an-extra-wrapper shape the wrapper sits at exactly the index the element
    /// it wraps would have. The children are the one piece of evidence left that does not
    /// come from the node itself, so <see cref="Corroboration"/> chooses and position only
    /// breaks its ties — which keeps the inserted-sibling case reading as an insertion.
    /// </remarks>
    private static int Choose(
        DomNode reference,
        List<DomNode> candidates,
        List<int> indices,
        int lastMatched,
        Pass pass)
    {
        // Prefer the earliest candidate at or after the one that last matched. Taking the
        // globally earliest instead reports a reorder whenever an extra same-key sibling was
        // inserted ahead of the real counterpart — the indices step backwards even though
        // nothing moved, and the inserted node is already reported on its own. Falling back
        // to the earliest available keeps a genuine swap detectable.
        var positional = indices.FindIndex(index => index >= lastMatched);
        if (positional < 0)
        {
            positional = 0;
        }

        if (pass != Pass.FullKey || indices.Count == 1)
        {
            return positional;
        }

        var best = positional;
        var bestScore = Corroboration(reference, candidates[indices[positional]]);

        for (var slot = 0; slot < indices.Count; slot++)
        {
            var score = Corroboration(reference, candidates[indices[slot]]);
            if (score > bestScore)
            {
                best = slot;
                bestScore = score;
            }
        }

        return best;
    }

    /// <summary>
    /// Counts how many children the two nodes have in common by key, comparing multisets so
    /// that two of a kind on one leg and one on the other scores one rather than two.
    /// </summary>
    private static int Corroboration(DomNode reference, DomNode candidate)
    {
        if (reference.Children.Count == 0 || candidate.Children.Count == 0)
        {
            return 0;
        }

        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var child in candidate.Children)
        {
            var childKey = Key(child);
            counts[childKey] = counts.TryGetValue(childKey, out var count) ? count + 1 : 1;
        }

        var shared = 0;
        foreach (var child in reference.Children)
        {
            var childKey = Key(child);
            if (counts.TryGetValue(childKey, out var count) && count > 0)
            {
                counts[childKey] = count - 1;
                shared++;
            }
        }

        return shared;
    }

    /// <summary>
    /// Reports whether stepping through a wrapper on either leg would put a better-evidenced
    /// counterpart in front of the reference than the candidate it is about to take.
    /// </summary>
    /// <remarks>
    /// Both directions are needed because nothing in this matcher is symmetric by
    /// construction: <see cref="PairBy"/> walks the references and looks candidates up, so a
    /// wrapper on the reference leg is invisible to the candidate-side test.
    /// </remarks>
    private static bool StepUnblocks(
        DomNode reference,
        List<DomNode> candidates,
        bool[] taken,
        int chosen)
    {
        var chosenScore = Corroboration(reference, candidates[chosen]);

        for (var i = 0; i < candidates.Count; i++)
        {
            // The candidate is the wrapper and the reference's counterpart is inside it, or
            // the mirror: the reference is the wrapper and this candidate's counterpart is
            // inside that.
            if (taken[i])
            {
                continue;
            }

            var forward = WrappedCounterpartScore(reference, candidates[i]);
            var reverse = WrappedCounterpartScore(candidates[i], reference);

            if ((forward is { } forwardScore
                    && (forwardScore > chosenScore
                        || (reference.Children.Count == 0 && i == chosen)))
                || (reverse is { } reverseScore
                    && (reverseScore > chosenScore
                        || (candidates[i].Children.Count == 0 && i == chosen))))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Reports whether <paramref name="wrapper"/> holds <paramref name="node"/>'s real
    /// counterpart somewhere down its chain of single-child descendants.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The whole chain rather than one step, because two nested layout wrappers are as
    /// ordinary as one and a one-step lookahead sees only the outer one's child — another
    /// wrapper, which corroborates nothing. The walk stops at the first node with no children
    /// or with several, so it is bounded by the chain's length.
    /// </para>
    /// <para>
    /// Both halves of the test are load-bearing. Without the key test, a plain container is
    /// unpicked from its counterpart whenever that counterpart holds one child — the
    /// positioner/popup shape, where the two containers must pair. Without the corroboration
    /// test, any two elements sharing a tag look like a wrapping, and the popup whose role
    /// differs from its counterpart's is stepped past instead of degraded onto.
    /// </para>
    /// </remarks>
    private static int? WrappedCounterpartScore(DomNode node, DomNode wrapper)
    {
        var key = Key(node);
        int? best = null;

        for (var inner = wrapper; inner.Children.Count == 1; inner = inner.Children[0])
        {
            var child = inner.Children[0];
            if (string.Equals(Key(child), key, StringComparison.Ordinal))
            {
                var score = Corroboration(node, child);
                best = best is null ? score : Math.Max(best.Value, score);
            }
        }

        return best;
    }

    /// <summary>
    /// Builds the key two nodes must share to pair: tag, role, and accessible name.
    /// </summary>
    /// <remarks>
    /// The snapshot carries no computed accessible name, so <c>aria-label</c> stands in
    /// for it and the node's own text is the fallback — which is what the browser
    /// computes for the labelled-by-content case that dominates these fixtures.
    /// </remarks>
    private static string Key(DomNode node)
    {
        var role = node.Attributes.TryGetValue("role", out var value) ? value : string.Empty;
        var name = node.Attributes.TryGetValue("aria-label", out var label) ? label : node.Text;

        return string.Join(KeySeparator, node.Tag, role, name);
    }

    /// <summary>
    /// Spells the parts of the pairing key out for a reader, so a report can say which part
    /// of the identity the two legs disagreed on.
    /// </summary>
    /// <param name="node">The node to describe.</param>
    /// <returns>The tag, and the role and accessible name when the node carries them.</returns>
    public static string Identity(DomNode node)
    {
        var parts = new List<string> { $"<{node.Tag}>" };

        if (node.Attributes.TryGetValue("role", out var role) && role.Length > 0)
        {
            parts.Add($"role '{role}'");
        }

        var name = node.Attributes.TryGetValue("aria-label", out var label) ? label : node.Text;
        if (name.Length > 0)
        {
            parts.Add($"named '{name}'");
        }

        return string.Join(' ', parts);
    }

    /// <summary>
    /// One sibling level: the two lists being paired against each other, and the passes
    /// over them.
    /// </summary>
    /// <param name="referenceChildren">The reference siblings.</param>
    /// <param name="candidateChildren">The candidate siblings.</param>
    /// <param name="parentPath">The reference path of the parent, for reorder reporting.</param>
    /// <param name="state">The result being collected across the whole walk.</param>
    private sealed class Level(
        IReadOnlyList<DomNode> referenceChildren,
        IReadOnlyList<DomNode> candidateChildren,
        string parentPath,
        MatchState state)
    {
        private readonly List<DomNode> references = [.. referenceChildren];
        private readonly List<DomNode> candidates = [.. candidateChildren];
        private readonly List<LevelPair> matchedBranches = [];
        private int pairedHere;
        private bool tagDegradeUnblocked;

        /// <summary>
        /// Gets a value indicating whether nothing at this level has paired on the full key.
        /// </summary>
        /// <remarks>
        /// The one guard on the tag degrade, and what keeps it a last resort rather than a
        /// general relaxation of the key: once anything at a level has paired, the two
        /// lists are taken to be the same list, so a leftover in it reads as the presence
        /// difference it usually is. The cost is real and worth stating — an identity
        /// difference on a node whose sibling paired is <em>not</em> degraded unless a
        /// wrapper projection exposes a better-corresponding same-tag node. This keeps two
        /// genuinely different siblings separate while still unwrapping layout inserted
        /// around an identity change.
        /// </remarks>
        private bool NothingPaired => pairedHere == 0;

        /// <summary>
        /// Pairs this level, recursing into every pair, and reports whatever is left over
        /// on either side as one-sided along with its subtree.
        /// </summary>
        /// <remarks>
        /// The order of the passes is the algorithm, and each earns its place ahead of the
        /// next:
        /// <list type="number">
        /// <item>
        /// Step one side alone, and only where doing so unblocks pairing. An extra wrapper
        /// on one leg is the common shape — React
        /// <c>&lt;div positioner&gt;&lt;div popup&gt;</c> against Blazor's extra wrapper
        /// around the popup — and stepping both sides there reports a wrapper on the leg
        /// that never had one and leaves the two popups unpaired for good.
        /// </item>
        /// <item>
        /// Degrade to the tag alone, when nothing at this level paired. This runs
        /// <em>before</em> the lockstep step, because a single-child node on either side
        /// would otherwise pre-empt it: a popup with one child on both legs is exactly that
        /// shape, and stepping first walks past the two elements whose differing identity
        /// is the one finding worth reporting, leaving each of them one-sided and every
        /// attribute on them uncompared.
        /// </item>
        /// <item>
        /// Step whatever is still steppable in lockstep, as the last way to reach a level
        /// where something might pair.
        /// </item>
        /// </list>
        /// </remarks>
        public void Match()
        {
            while (references.Count > 0 && candidates.Count > 0)
            {
                PairOnKey();

                if (references.Count == 0 || candidates.Count == 0)
                {
                    break;
                }

                if (TryStepCandidatesOnly() || TryStepReferencesOnly())
                {
                    continue;
                }

                if ((NothingPaired || tagDegradeUnblocked) && TryPairByTag())
                {
                    break;
                }

                if (TryStepBoth())
                {
                    continue;
                }

                break;
            }

            RecordReorder(parentPath, matchedBranches, state.Reorders);

            foreach (var node in references)
            {
                state.ReferenceOnly.AddRange(node.Descendants());
            }

            foreach (var node in candidates)
            {
                state.CandidateOnly.AddRange(node.Descendants());
            }
        }

        /// <summary>Pairs on the full key: tag, role, and accessible name.</summary>
        private void PairOnKey()
        {
            PairBy(references, candidates, Pass.FullKey, out var matched);

            foreach (var (pair, _) in matched.Where(item => item.Pair.Relaxed))
            {
                state.Relaxed.Add(
                    new RelaxedPair(pair, Identity(pair.Reference), Identity(pair.Candidate)));
            }

            pairedHere += matched.Count;
            Accept(matched);
        }

        /// <summary>
        /// Pairs whatever shares a tag name, as the last resort before both subtrees are
        /// reported as unrelated. Dumping them instead costs every attribute, style, and
        /// geometry comparison beneath them in order to report what is usually one
        /// attribute on one element. Nodes without a counterpart of the same tag are left
        /// in place to be reported one-sided.
        /// </summary>
        /// <returns><see langword="true"/> if anything paired.</returns>
        private bool TryPairByTag()
        {
            PairBy(references, candidates, Pass.TagDegrade, out var matched);

            foreach (var (pair, _) in matched)
            {
                state.Relaxed.Add(
                    new RelaxedPair(pair, Identity(pair.Reference), Identity(pair.Candidate)));
            }

            Accept(matched);

            return matched.Count > 0;
        }

        /// <summary>
        /// Replaces single-child candidates with their child, if that makes something pair.
        /// </summary>
        /// <returns><see langword="true"/> if the candidates were stepped.</returns>
        private bool TryStepCandidatesOnly()
        {
            if (StepThrough(candidates) is { } step &&
                (SharesAKey(references, step.Nodes) ||
                 NothingPaired && SharesATag(references, step.Nodes)))
            {
                Commit(candidates, step, state.CandidateOnly);
                return true;
            }

            if (NothingPaired ||
                StepThroughTagWrappers(candidates, references) is not { } tagStep)
            {
                return false;
            }

            Commit(candidates, tagStep, state.CandidateOnly);
            tagDegradeUnblocked = true;

            return true;
        }

        /// <summary>The mirror of <see cref="TryStepCandidatesOnly"/>.</summary>
        /// <returns><see langword="true"/> if the references were stepped.</returns>
        private bool TryStepReferencesOnly()
        {
            if (StepThrough(references) is { } step &&
                (SharesAKey(step.Nodes, candidates) ||
                 NothingPaired && SharesATag(step.Nodes, candidates)))
            {
                Commit(references, step, state.ReferenceOnly);
                return true;
            }

            if (NothingPaired ||
                StepThroughTagWrappers(references, candidates) is not { } tagStep)
            {
                return false;
            }

            Commit(references, tagStep, state.ReferenceOnly);
            tagDegradeUnblocked = true;

            return true;
        }

        /// <summary>
        /// Steps every side that has a single-child node, whether or not doing so unblocks
        /// pairing, so that a level neither one-sided step nor the tag degrade could reach
        /// is at least retried one level in.
        /// </summary>
        /// <returns><see langword="true"/> if either list was stepped.</returns>
        private bool TryStepBoth()
        {
            var steppedReferences = StepThrough(references);
            var steppedCandidates = StepThrough(candidates);

            if (steppedReferences is { } referenceStep)
            {
                Commit(references, referenceStep, state.ReferenceOnly);
            }

            if (steppedCandidates is { } candidateStep)
            {
                Commit(candidates, candidateStep, state.CandidateOnly);
            }

            return steppedReferences is not null || steppedCandidates is not null;
        }

        /// <summary>Records each pair and matches the two subtrees beneath it.</summary>
        private void Accept(List<MatchedPair> matched)
        {
            foreach (var (pair, _) in matched)
            {
                matchedBranches.Add(new LevelPair(
                    pair,
                    BranchIndex(referenceChildren, pair.Reference),
                    BranchIndex(candidateChildren, pair.Candidate)));
                state.Pairs.Add(pair);
                MatchChildren(
                    pair.Reference.Children, pair.Candidate.Children, pair.Reference.Path, state);
            }
        }

        private static int BranchIndex(IReadOnlyList<DomNode> branches, DomNode node)
        {
            for (var index = 0; index < branches.Count; index++)
            {
                if (branches[index].Descendants().Any(item => ReferenceEquals(item, node)))
                {
                    return index;
                }
            }

            throw new InvalidOperationException(
                $"Matched node '{node.Path}' does not belong to this sibling level.");
        }
    }

    /// <summary>A pair together with the candidate's position among its siblings.</summary>
    /// <param name="Pair">The matched nodes.</param>
    /// <param name="CandidateIndex">The candidate's index within its sibling list.</param>
    private readonly record struct MatchedPair(NodePair Pair, int CandidateIndex);

    /// <summary>
    /// A pair together with the original sibling branches it came through, retained across
    /// wrapper projection so reorder evidence remains tied to real document order.
    /// </summary>
    private readonly record struct LevelPair(
        NodePair Pair,
        int ReferenceIndex,
        int CandidateIndex);

    /// <summary>A sibling list with its single-child nodes replaced by their children.</summary>
    /// <param name="Nodes">The replacement list.</param>
    /// <param name="Wrappers">The elements that were stepped through.</param>
    private readonly record struct Step(List<DomNode> Nodes, List<DomNode> Wrappers);

    /// <summary>Collects the result while the two trees are walked.</summary>
    private sealed class MatchState
    {
        /// <summary>Gets the nodes that matched.</summary>
        public List<NodePair> Pairs { get; } = [];

        /// <summary>Gets the nodes React rendered that Blazor did not.</summary>
        public List<DomNode> ReferenceOnly { get; } = [];

        /// <summary>Gets the nodes Blazor rendered that React did not.</summary>
        public List<DomNode> CandidateOnly { get; } = [];

        /// <summary>Gets the sibling lists the two legs ordered differently.</summary>
        public List<SiblingReorder> Reorders { get; } = [];

        /// <summary>Gets the pairs that only matched once the key was relaxed to the tag.</summary>
        public List<RelaxedPair> Relaxed { get; } = [];
    }
}
