namespace Blazix.BaseUI.Utilities.FloatingTree;

/// <summary>
/// Represents a node in a floating element tree hierarchy.
/// </summary>
/// <param name="Id">The unique identifier of this node.</param>
/// <param name="ParentId">The identifier of the parent node, or <c>null</c> if this is a root node.</param>
public sealed record FloatingTreeNode(string Id, string? ParentId)
{
    /// <summary>
    /// Per-node runtime state (e.g., open state). Used for tree coordination.
    /// </summary>
    public IFloatingRootContext? Context { get; set; }

}
