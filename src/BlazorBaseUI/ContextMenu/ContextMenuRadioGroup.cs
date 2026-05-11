namespace BlazorBaseUI.ContextMenu;

using BlazorBaseUI.Menu;

/// <summary>
/// Groups radio items together within a context menu, managing single-selection state.
/// Renders a <c>&lt;div&gt;</c> element.
/// </summary>
// lint-ignore:RULE-11 Public type intentionally left unsealed; consumers may subclass for customization.
public class ContextMenuRadioGroup : MenuRadioGroup;
