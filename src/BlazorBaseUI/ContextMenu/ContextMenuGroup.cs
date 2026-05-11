namespace BlazorBaseUI.ContextMenu;

using BlazorBaseUI.Menu;

/// <summary>
/// Groups related context menu items together with an optional label.
/// Renders a <c>&lt;div&gt;</c> element.
/// </summary>
// lint-ignore:RULE-11 Public type intentionally left unsealed; consumers may subclass for customization.
public class ContextMenuGroup : MenuGroup;
