using Blazix.BaseUI.Parity.Tests.Capture;

namespace Blazix.BaseUI.Parity.Tests.Diff;

/// <summary>
/// A reference node and the candidate node it was matched with.
/// </summary>
/// <param name="Reference">The node from the React snapshot.</param>
/// <param name="Candidate">The node from the Blazor snapshot.</param>
public sealed record NodePair(DomNode Reference, DomNode Candidate);

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
/// A pair that matched only after the key was relaxed to the tag name alone.
/// </summary>
/// <remarks>
/// The pair is in <see cref="NodeMatchResult.Pairs"/> like any other, so everything beneath
/// it is still compared. It is listed here as well because the two nodes are not the same
/// element by the matcher's own definition, and a degrade that let that difference vanish
/// would be worse than reporting the two subtrees as unrelated.
/// </remarks>
/// <param name="Pair">The matched nodes.</param>
/// <param name="ReferenceIdentity">The reference node's tag, role, and accessible name.</param>
/// <param name="CandidateIdentity">The same for the candidate node.</param>
public sealed record RelaxedPair(NodePair Pair, string ReferenceIdentity, string CandidateIdentity);

/// <summary>
/// The outcome of matching two DOM snapshots.
/// </summary>
/// <param name="Pairs">The nodes that matched, including any roots that matched.</param>
/// <param name="ReferenceOnly">The nodes React rendered that Blazor did not.</param>
/// <param name="CandidateOnly">The nodes Blazor rendered that React did not.</param>
/// <param name="Reorders">The sibling lists the two legs ordered differently.</param>
/// <param name="Relaxed">The pairs that only matched on tag, listed also in <paramref name="Pairs"/>.</param>
public sealed record NodeMatchResult(
    IReadOnlyList<NodePair> Pairs,
    IReadOnlyList<DomNode> ReferenceOnly,
    IReadOnlyList<DomNode> CandidateOnly,
    IReadOnlyList<SiblingReorder> Reorders,
    IReadOnlyList<RelaxedPair> Relaxed);

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

        return new NodeMatchResult(
            state.Pairs, state.ReferenceOnly, state.CandidateOnly, state.Reorders, state.Relaxed);
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
    {
        var references = referenceChildren.ToList();
        var candidates = candidateChildren.ToList();
        var firstPass = true;
        var pairedHere = 0;

        while (references.Count > 0 && candidates.Count > 0)
        {
            PairBy(references, candidates, Key, out var matched);

            if (firstPass)
            {
                // Only the first pass sees the two sibling lists as the two pages ordered
                // them. After an unwrap the candidate indices count positions in a list
                // that exists in neither DOM, so ordering read off it means nothing.
                RecordReorder(parentPath, matched, state.Reorders);
                firstPass = false;
            }

            pairedHere += matched.Count;
            Accept(matched, state);

            if (references.Count == 0 || candidates.Count == 0)
            {
                break;
            }

            if (StepIntoWrappers(references, candidates, state))
            {
                continue;
            }

            // Nothing at this level pairs and there is no wrapper left to step through.
            // If nothing here paired at all, the two lists are far more likely to be the
            // same elements differing in role or name than two unrelated subtrees, so try
            // them on tag alone before giving up: dumping both subtrees costs every
            // attribute, style, and geometry comparison beneath them in order to report
            // what is usually one attribute on one element. Guarded on `pairedHere` so
            // this stays a last resort and never becomes a general relaxation of the key.
            if (pairedHere == 0)
            {
                PairByTag(references, candidates, state);
            }

            break;
        }

        foreach (var node in references)
        {
            state.ReferenceOnly.AddRange(node.Descendants());
        }

        foreach (var node in candidates)
        {
            state.CandidateOnly.AddRange(node.Descendants());
        }
    }

    /// <summary>
    /// Records each pair and matches the two subtrees beneath it.
    /// </summary>
    private static void Accept(List<MatchedPair> matched, MatchState state)
    {
        foreach (var (pair, _) in matched)
        {
            state.Pairs.Add(pair);
            MatchChildren(
                pair.Reference.Children, pair.Candidate.Children, pair.Reference.Path, state);
        }
    }

    /// <summary>
    /// Pairs whatever shares a tag name, as the last resort before both subtrees are
    /// reported as unrelated. Nodes without a counterpart of the same tag are left in place
    /// for the caller to report one-sided.
    /// </summary>
    private static void PairByTag(List<DomNode> references, List<DomNode> candidates, MatchState state)
    {
        PairBy(references, candidates, static node => node.Tag, out var matched);

        foreach (var (pair, _) in matched)
        {
            state.Relaxed.Add(
                new RelaxedPair(pair, Identity(pair.Reference), Identity(pair.Candidate)));
        }

        Accept(matched, state);
    }

    /// <summary>
    /// Replaces single-child leftovers with their child so pairing can be retried one level
    /// in, reporting each element stepped through as one-sided.
    /// </summary>
    /// <remarks>
    /// One side is tried at a time first. Stepping both in the same pass reports a wrapper
    /// on the leg that never had one whenever only one leg carries the extra element and
    /// both leftovers happen to have a single child — React
    /// <c>&lt;div positioner&gt;&lt;div popup&gt;</c> against Blazor's extra wrapper around
    /// the popup is exactly that shape — and it leaves the two popups permanently unpaired,
    /// so every attribute on them goes uncompared.
    /// </remarks>
    /// <returns><see langword="true"/> if either list was stepped.</returns>
    private static bool StepIntoWrappers(
        List<DomNode> references,
        List<DomNode> candidates,
        MatchState state)
    {
        var steppedReferences = StepThrough(references);
        var steppedCandidates = StepThrough(candidates);

        if (steppedCandidates is { } candidateStep && SharesAKey(references, candidateStep.Nodes))
        {
            Commit(candidates, candidateStep, state.CandidateOnly);
            return true;
        }

        if (steppedReferences is { } referenceStep && SharesAKey(referenceStep.Nodes, candidates))
        {
            Commit(references, referenceStep, state.ReferenceOnly);
            return true;
        }

        if (steppedReferences is null && steppedCandidates is null)
        {
            return false;
        }

        // Neither side alone unblocks pairing, so step both and look again one level in.
        if (steppedReferences is { } bothReferences)
        {
            Commit(references, bothReferences, state.ReferenceOnly);
        }

        if (steppedCandidates is { } bothCandidates)
        {
            Commit(candidates, bothCandidates, state.CandidateOnly);
        }

        return true;
    }

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
        List<MatchedPair> matched,
        List<SiblingReorder> reorders)
    {
        // `matched` is built in reference document order, so "both legs agree" is exactly
        // "the candidate indices never step backwards".
        for (var i = 1; i < matched.Count; i++)
        {
            if (matched[i].CandidateIndex >= matched[i - 1].CandidateIndex)
            {
                continue;
            }

            reorders.Add(new SiblingReorder(
                parentPath,
                [.. matched.Select(m => m.Pair.Reference.Path)],
                [.. matched.OrderBy(m => m.CandidateIndex).Select(m => m.Pair.Reference.Path)]));

            return;
        }
    }

    /// <summary>
    /// Pairs as many nodes as possible on <paramref name="key"/>, removing every paired
    /// node from <paramref name="references"/> and <paramref name="candidates"/>.
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
        Func<DomNode, string> key,
        out List<MatchedPair> matched)
    {
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

            // Prefer the earliest candidate at or after the one that last matched. Taking
            // the globally earliest instead reports a reorder whenever an extra same-key
            // sibling was inserted ahead of the real counterpart — the indices step
            // backwards even though nothing moved, and the inserted node is already
            // reported on its own. Falling back to the earliest available keeps a genuine
            // swap detectable.
            var slot = indices.FindIndex(index => index >= lastMatched);
            if (slot < 0)
            {
                slot = 0;
            }

            var chosen = indices[slot];
            indices.RemoveAt(slot);
            taken[chosen] = true;
            lastMatched = chosen;
            matched.Add(new MatchedPair(new NodePair(reference, candidates[chosen]), chosen));
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
    /// Spells the parts of <see cref="Key"/> out for a reader, so a pair that only matched
    /// on tag can say which part of the identity the two legs disagreed on.
    /// </summary>
    private static string Identity(DomNode node)
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

    /// <summary>A pair together with the candidate's position among its siblings.</summary>
    /// <param name="Pair">The matched nodes.</param>
    /// <param name="CandidateIndex">The candidate's index within its sibling list.</param>
    private readonly record struct MatchedPair(NodePair Pair, int CandidateIndex);

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
