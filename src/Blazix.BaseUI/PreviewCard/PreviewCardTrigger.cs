namespace Blazix.BaseUI.PreviewCard;

/// <summary>
/// A preview card trigger component that can work either nested inside a PreviewCardRoot
/// or detached with a handle for typed payloads.
/// Renders an <c>&lt;a&gt;</c> element.
/// </summary>
/// <typeparam name="TPayload">The type of payload to pass to the preview card. Use object for untyped payloads.</typeparam>
public partial class PreviewCardTypedTrigger<TPayload>;

/// <summary>
/// A preview card trigger component using untyped payloads.
/// Renders an <c>&lt;a&gt;</c> element.
/// </summary>
public sealed class PreviewCardTrigger : PreviewCardTypedTrigger<object>
{
}
