namespace Blazix.BaseUI.Popover;

/// <summary>
/// A popover trigger component that can work either nested inside a PopoverRoot
/// or detached with a handle for typed payloads.
/// Renders a <c>&lt;button&gt;</c> element.
/// </summary>
/// <typeparam name="TPayload">The type of payload to pass to the popover. Use object for untyped payloads.</typeparam>
public partial class PopoverTypedTrigger<TPayload>;

/// <summary>
/// Non-generic version of PopoverTypedTrigger for scenarios where payload type is not needed.
/// Renders a <c>&lt;button&gt;</c> element.
/// </summary>
public sealed class PopoverTrigger : PopoverTypedTrigger<object?>
{
}
