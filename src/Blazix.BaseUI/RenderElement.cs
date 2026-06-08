namespace Blazix.BaseUI;

/// <summary>
/// Shared rendering component that centralizes element output for all
/// Blazix.BaseUI components. Handles attribute merging, class/style
/// composition, and dispatches to either a consumer-supplied
/// <see cref="RenderProps{TState}">Render</see> function or the
/// default HTML element.
/// </summary>
/// <typeparam name="TState">The component's public state record.</typeparam>
public partial class RenderElement<TState>;
