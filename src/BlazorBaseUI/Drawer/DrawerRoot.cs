namespace BlazorBaseUI.Drawer;

/// <summary>
/// Provides a root context for drawer components, managing open/close state,
/// snap points, swipe direction, accessibility, and nested drawer coordination.
/// Does not render its own element.
/// </summary>
public partial class DrawerRoot;

/// <summary>
/// Provides the payload context for drawer content rendering via <see cref="DrawerRoot.ChildContent"/>.
/// </summary>
public readonly record struct DrawerRootPayloadContext(object? Payload);
