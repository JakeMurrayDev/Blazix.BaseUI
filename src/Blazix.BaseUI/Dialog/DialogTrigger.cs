namespace Blazix.BaseUI.Dialog;

/// <summary>
/// A dialog trigger component that can work either nested inside a DialogRoot
/// or detached with a handle for typed payloads.
/// Renders a <c>&lt;button&gt;</c> element.
/// </summary>
/// <typeparam name="TPayload">The type of payload to pass to the dialog. Use object for untyped payloads.</typeparam>
public partial class DialogTypedTrigger<TPayload>;

/// <summary>
/// Non-generic version of DialogTypedTrigger for scenarios where payload type is not needed.
/// Renders a <c>&lt;button&gt;</c> element.
/// </summary>
public sealed class DialogTrigger : DialogTypedTrigger<object?>
{
}
