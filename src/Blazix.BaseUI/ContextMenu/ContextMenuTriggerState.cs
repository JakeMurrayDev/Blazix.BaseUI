namespace Blazix.BaseUI.ContextMenu;

/// <summary>
/// Represents the state of a <see cref="ContextMenuTrigger"/> component.
/// </summary>
/// <param name="Open">Whether the context menu is currently open.</param>
public readonly record struct ContextMenuTriggerState(bool Open);
