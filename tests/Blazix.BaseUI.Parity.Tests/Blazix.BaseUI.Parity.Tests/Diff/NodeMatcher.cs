using Blazix.BaseUI.Parity.Tests.Capture;

namespace Blazix.BaseUI.Parity.Tests.Diff;

/// <summary>
/// A reference node and the candidate node it was matched with.
/// </summary>
/// <param name="Reference">The node from the React snapshot.</param>
/// <param name="Candidate">The node from the Blazor snapshot.</param>
public sealed record NodePair(DomNode Reference, DomNode Candidate);

/// <summary>
/// The outcome of matching two DOM snapshots.
/// </summary>
/// <param name="Pairs">The nodes that matched, including the two roots.</param>
/// <param name="ReferenceOnly">The nodes React rendered that Blazor did not.</param>
/// <param name="CandidateOnly">The nodes Blazor rendered that React did not.</param>
public sealed record NodeMatchResult(
    IReadOnlyList<NodePair> Pairs,
    IReadOnlyList<DomNode> ReferenceOnly,
    IReadOnlyList<DomNode> CandidateOnly);

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
    /// <returns>The pairs and the nodes left over on either side.</returns>
    public static NodeMatchResult Match(DomNode reference, DomNode candidate)
    {
        var pairs = new List<NodePair>();
        var referenceOnly = new List<DomNode>();
        var candidateOnly = new List<DomNode>();

        // The roots are the two trees being compared, so they pair by definition.
        pairs.Add(new NodePair(reference, candidate));
        MatchChildren(reference.Children, candidate.Children, pairs, referenceOnly, candidateOnly);

        return new NodeMatchResult(pairs, referenceOnly, candidateOnly);
    }

    private static void MatchChildren(
        IReadOnlyList<DomNode> referenceChildren,
        IReadOnlyList<DomNode> candidateChildren,
        List<NodePair> pairs,
        List<DomNode> referenceOnly,
        List<DomNode> candidateOnly)
    {
        var references = referenceChildren.ToList();
        var candidates = candidateChildren.ToList();

        while (references.Count > 0 && candidates.Count > 0)
        {
            PairByKey(references, candidates, out var matched);

            foreach (var pair in matched)
            {
                pairs.Add(pair);
                MatchChildren(
                    pair.Reference.Children, pair.Candidate.Children,
                    pairs, referenceOnly, candidateOnly);
            }

            if (references.Count == 0 || candidates.Count == 0)
            {
                break;
            }

            // Nothing left at this level pairs, so try again one level in. An extra
            // wrapper element is the common cause, and reporting it while abandoning
            // everything beneath it would turn one difference into a whole subtree of
            // them. The wrapper is still reported; only the search continues.
            var unwrappedReferences = Unwrap(references, referenceOnly);
            var unwrappedCandidates = Unwrap(candidates, candidateOnly);

            if (!unwrappedReferences && !unwrappedCandidates)
            {
                break;
            }
        }

        foreach (var node in references)
        {
            referenceOnly.AddRange(node.Descendants());
        }

        foreach (var node in candidates)
        {
            candidateOnly.AddRange(node.Descendants());
        }
    }

    /// <summary>
    /// Pairs as many nodes as possible by key, removing every paired node from
    /// <paramref name="references"/> and <paramref name="candidates"/>.
    /// </summary>
    private static void PairByKey(
        List<DomNode> references,
        List<DomNode> candidates,
        out List<NodePair> matched)
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
                matched.Add(new NodePair(reference, candidates[index]));
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
}
