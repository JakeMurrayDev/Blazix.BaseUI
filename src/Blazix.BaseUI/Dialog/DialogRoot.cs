namespace Blazix.BaseUI.Dialog;

/// <summary>
/// Provides a root context for dialog components, managing open/close state,
/// transitions, accessibility, and nested dialog coordination.
/// Does not render its own element.
/// </summary>
public partial class DialogRoot;

/// <summary>
/// Provides the payload context for dialog content rendering via <see cref="DialogRoot.ChildContent"/>.
/// </summary>
public readonly record struct DialogRootPayloadContext(object? Payload);
