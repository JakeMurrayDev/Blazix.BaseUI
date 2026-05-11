namespace BlazorBaseUI.ContextMenu;

using BlazorBaseUI.Menu;

/// <summary>
/// Positions the context menu popup relative to the cursor location.
/// Renders a <c>&lt;div&gt;</c> element.
/// </summary>
// lint-ignore:RULE-11 Public type intentionally left unsealed; consumers may subclass for customization.
public class ContextMenuPositioner : MenuPositioner;
