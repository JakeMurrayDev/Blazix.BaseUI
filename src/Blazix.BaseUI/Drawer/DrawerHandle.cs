using Blazix.BaseUI.Dialog;

namespace Blazix.BaseUI.Drawer;

/// <summary>
/// A handle to control a drawer imperatively and associate detached triggers with it.
/// </summary>
public class DrawerHandle<TPayload> : DialogHandle<TPayload>
{
}

/// <summary>
/// Non-generic drawer handle for scenarios where payload type is not needed.
/// </summary>
public sealed class DrawerHandle : DrawerHandle<object?>;

/// <summary>
/// Factory methods for creating drawer handles.
/// </summary>
public static class DrawerHandleFactory
{
    /// <summary>
    /// Creates a new handle to connect a Drawer.Root with detached Drawer.Trigger components.
    /// </summary>
    public static DrawerHandle<TPayload> CreateHandle<TPayload>() => new();

    /// <summary>
    /// Creates a new handle to connect a Drawer.Root with detached Drawer.Trigger components.
    /// </summary>
    public static DrawerHandle CreateHandle() => new();
}
