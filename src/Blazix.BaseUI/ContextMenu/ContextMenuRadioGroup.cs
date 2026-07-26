namespace Blazix.BaseUI.ContextMenu;

using Blazix.BaseUI.Menu;

/// <summary>
/// Groups radio items together within a context menu, managing single-selection state.
/// Renders a <c>&lt;div&gt;</c> element.
/// </summary>
/// <typeparam name="TValue">The type used to identify radio items.</typeparam>
public class ContextMenuRadioGroup<TValue> : MenuRadioGroup<TValue>;
