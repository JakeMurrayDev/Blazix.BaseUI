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
/// The outcome of matching two DOM snapshots.
/// </summary>
/// <param name="Pairs">The nodes that matched, including any roots that matched.</param>
/// <param name="ReferenceOnly">The nodes React rendered that Blazor did not.</param>
/// <param name="CandidateOnly">The nodes Blazor rendered that React did not.</param>
/// <param name="Reorders">The sibling lists the two legs ordered differently.</param>
public sealed record NodeMatchResult(
    IReadOnlyList<NodePair> Pairs,
    IReadOnlyList<DomNode> ReferenceOnly,
    IReadOnlyList<DomNode> CandidateOnly,
    IReadOnlyList<SiblingReorder> Reorders);

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
            state.Pairs, state.ReferenceOnly, state.CandidateOnly, state.Reorders);
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

        while (references.Count > 0 && candidates.Count > 0)
        {
            PairByKey(references, candidates, out var matched);

            if (firstPass)
            {
                // Only the first pass sees the two sibling lists as the two pages ordered
                // them. After an unwrap the candidate indices count positions in a list
                // that exists in neither DOM, so ordering read off it means nothing.
                RecordReorder(parentPath, matched, state.Reorders);
                firstPass = false;
            }

            foreach (var (pair, _) in matched)
            {
                state.Pairs.Add(pair);
                MatchChildren(
                    pair.Reference.Children, pair.Candidate.Children, pair.Reference.Path, state);
            }

            if (references.Count == 0 || candidates.Count == 0)
            {
                break;
            }

            // Nothing left at this level pairs, so try again one level in. An extra
            // wrapper element is the common cause, and reporting it while abandoning
            // everything beneath it would turn one difference into a whole subtree of
            // them. The wrapper is still reported; only the search continues.
            var unwrappedReferences = Unwrap(references, state.ReferenceOnly);
            var unwrappedCandidates = Unwrap(candidates, state.CandidateOnly);

            if (!unwrappedReferences && !unwrappedCandidates)
            {
                break;
            }
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
    /// Pairs as many nodes as possible by key, removing every paired node from
    /// <paramref name="references"/> and <paramref name="candidates"/>.
    /// </summary>
    /// <remarks>
    /// Each pair carries the candidate's index within <paramref name="candidates"/> so the
    /// caller can tell how the two legs ordered the nodes that paired.
    /// </remarks>
    private static void PairByKey(
        List<DomNode> references,
        List<DomNode> candidates,
        out List<MatchedPair> matched)
    {
        var available = new Dictionary<string, Queue<int>>(StringComparer.Ordinal);
        for (var i = 0; i < candidates.Count; i++)
        {
            var key = Key(candidates[i]);
            if (!available.TryGetValue(key, out var queue))
            {
                queue = new Queue<int>();
                available[key] = queue;
            }

            queue.Enqueue(i);
        }

        matched = [];
        var taken = new bool[candidates.Count];
        var referenceLeftovers = new List<DomNode>();

        foreach (var reference in references)
        {
            if (available.TryGetValue(Key(reference), out var queue) && queue.Count > 0)
            {
                var index = queue.Dequeue();
                taken[index] = true;
                matched.Add(new MatchedPair(new NodePair(reference, candidates[index]), index));
            }
            else
            {
                referenceLeftovers.Add(reference);
            }
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
    /// Reports every single-child node in <paramref name="nodes"/> and replaces it with
    /// that child.
    /// </summary>
    /// <returns><see langword="true"/> if any node was replaced.</returns>
    private static bool Unwrap(List<DomNode> nodes, List<DomNode> reported)
    {
        var unwrapped = false;

        for (var i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].Children.Count == 1)
            {
                reported.Add(nodes[i]);
                nodes[i] = nodes[i].Children[0];
                unwrapped = true;
            }
        }

        return unwrapped;
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

    /// <summary>A pair together with the candidate's position among its siblings.</summary>
    /// <param name="Pair">The matched nodes.</param>
    /// <param name="CandidateIndex">The candidate's index within its sibling list.</param>
    private readonly record struct MatchedPair(NodePair Pair, int CandidateIndex);

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
    }
}
