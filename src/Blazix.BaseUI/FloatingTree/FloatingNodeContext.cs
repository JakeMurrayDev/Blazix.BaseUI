namespace Blazix.BaseUI.FloatingTree;

/// <summary>
/// Provides node-specific context for a floating element within a <see cref="FloatingTree"/>.
/// Cascaded by <see cref="FloatingNode"/> to all descendant components.
/// </summary>
public sealed class FloatingNodeContext
{
    /// <summary>
    /// Gets the unique identifier of this node.
    /// </summary>
    public string NodeId { get; }

    /// <summary>
    /// Gets the identifier of the parent node, or <c>null</c> if this is a root node.
    /// </summary>
    public string? ParentNodeId { get; }

    /// <summary>
    /// Gets the identifier of the tree this node belongs to.
    /// </summary>
    public string TreeId { get; }

    /// <summary>
    /// Gets the tree context this node belongs to.
    /// </summary>
    public FloatingTreeContext? TreeContext { get; }

    /// <summary>
    /// Gets or sets a callback to update the per-node runtime context
    /// on both the C# and JS sides.
    /// </summary>
    public Action<IFloatingRootContext?>? SetContext { get; internal set; }

    internal FloatingNodeContext(string nodeId, string? parentNodeId, string treeId, FloatingTreeContext? treeContext)
    {
        NodeId = nodeId;
        ParentNodeId = parentNodeId;
        TreeId = treeId;
        TreeContext = treeContext;
    }

    /// <summary>
    /// Registers an event handler on the tree.
    /// </summary>
    public void On(string eventName, Func<object?, Task> handler) =>
        TreeContext?.On(eventName, handler);

    /// <summary>
    /// Unregisters an event handler from the tree.
    /// </summary>
    public void Off(string eventName, Func<object?, Task> handler) =>
        TreeContext?.Off(eventName, handler);

    /// <summary>
    /// Emits an event to all registered handlers on the tree.
    /// </summary>
    public Task EmitAsync(string eventName, object? payload) =>
        TreeContext?.EmitAsync(eventName, payload) ?? Task.CompletedTask;
}
