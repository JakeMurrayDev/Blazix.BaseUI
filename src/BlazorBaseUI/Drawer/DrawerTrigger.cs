namespace BlazorBaseUI.Drawer;

/// <summary>
/// A drawer trigger component that can work either nested inside a DrawerRoot
/// or detached with a handle for typed payloads. Renders a <c>button</c> element.
/// </summary>
/// <typeparam name="TPayload">The type of payload to pass to the drawer. Use object for untyped payloads.</typeparam>
public partial class DrawerTypedTrigger<TPayload>;

/// <summary>
/// Non-generic version of DrawerTypedTrigger for scenarios where payload type is not needed.
/// Renders a <c>button</c> element.
/// </summary>
public sealed class DrawerTrigger : DrawerTypedTrigger<object?>
{
}
