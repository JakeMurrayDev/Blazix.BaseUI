using Microsoft.JSInterop;

namespace Blazix.BaseUI.FloatingTree;

/// <summary>
/// Provides tree hierarchy state for nested floating elements.
/// Cascaded by <see cref="FloatingTree"/> to all descendant components.
/// </summary>
public sealed class FloatingTreeContext
{
    private readonly List<FloatingTreeNode> nodes = [];
    private readonly Dictionary<string, List<Func<object?, Task>>> eventHandlers = [];

    /// <summary>
    /// Gets the unique identifier of this tree.
    /// </summary>
    public string TreeId { get; }

    /// <summary>
    /// Gets the current list of registered nodes.
    /// </summary>
    public IReadOnlyList<FloatingTreeNode> Nodes => nodes;

    /// <summary>
    /// Gets or sets the shared JS module reference for all nodes in this tree.
    /// Set by <see cref="FloatingTree"/> during initialization.
    /// </summary>
    internal Lazy<Task<IJSObjectReference>>? ModuleTask { get; set; }

    public FloatingTreeContext(string treeId)
    {
        TreeId = treeId;
    }

    /// <summary>
    /// Registers a node in the tree.
    /// </summary>
    public void RegisterNode(FloatingTreeNode node)
    {
        nodes.Add(node);
    }

    /// <summary>
    /// Unregisters a node from the tree.
    /// </summary>
    public void UnregisterNode(FloatingTreeNode node)
    {
        nodes.Remove(node);
    }

    /// <summary>
    /// Gets all descendants of the specified node recursively.
    /// When <paramref name="onlyOpenChildren"/> is <c>true</c> (default),
    /// only includes children whose context reports open state as <c>true</c>.
    /// </summary>
    public IReadOnlyList<FloatingTreeNode> GetNodeChildren(string nodeId, bool onlyOpenChildren = true)
    {
        var result = new List<FloatingTreeNode>();
        CollectChildren(nodeId, onlyOpenChildren, result);
        return result;
    }

    /// <summary>
    /// Gets the deepest open descendant node from the specified node.
    /// Returns the node itself if it has no open children.
    /// </summary>
    public FloatingTreeNode? GetDeepestNode(string nodeId)
    {
        var deepestNodeId = nodeId;
        var maxDepth = -1;

        FindDeepest(nodeId, 0);

        return nodes.FirstOrDefault(n => n.Id == deepestNodeId);

        void FindDeepest(string currentNodeId, int depth)
        {
            if (depth > maxDepth)
            {
                maxDepth = depth;
                deepestNodeId = currentNodeId;
            }

            var children = nodes
                .Where(n => n.ParentId == currentNodeId && (n.Context?.GetOpen() ?? false))
                .ToList();

            foreach (var child in children)
            {
                FindDeepest(child.Id, depth + 1);
            }
        }
    }

    private void CollectChildren(string nodeId, bool onlyOpenChildren, List<FloatingTreeNode> result)
    {
        foreach (var node in nodes)
        {
            if (node.ParentId != nodeId) continue;
            if (onlyOpenChildren && !(node.Context?.GetOpen() ?? false)) continue;

            result.Add(node);
            CollectChildren(node.Id, onlyOpenChildren, result);
        }
    }

    /// <summary>
    /// Gets all ancestors of the specified node, from parent to root.
    /// </summary>
    public IEnumerable<FloatingTreeNode> GetNodeAncestors(string nodeId)
    {
        var current = nodes.FirstOrDefault(n => n.Id == nodeId);
        while (current?.ParentId is not null)
        {
            var parent = nodes.FirstOrDefault(n => n.Id == current.ParentId);
            if (parent is null) break;
            yield return parent;
            current = parent;
        }
    }

    /// <summary>
    /// Registers an event handler for the specified event name.
    /// </summary>
    public void On(string eventName, Func<object?, Task> handler)
    {
        if (!eventHandlers.TryGetValue(eventName, out var handlers))
        {
            handlers = [];
            eventHandlers[eventName] = handlers;
        }

        if (!handlers.Contains(handler))
            handlers.Add(handler);
    }

    /// <summary>
    /// Unregisters an event handler for the specified event name.
    /// </summary>
    public void Off(string eventName, Func<object?, Task> handler)
    {
        if (eventHandlers.TryGetValue(eventName, out var handlers))
        {
            handlers.Remove(handler);
        }
    }

    /// <summary>
    /// Emits an event to all registered handlers for the specified event name.
    /// </summary>
    public async Task EmitAsync(string eventName, object? payload)
    {
        if (eventHandlers.TryGetValue(eventName, out var handlers))
        {
            foreach (var handler in handlers.ToList())
            {
                await handler(payload);
            }
        }
    }
}
