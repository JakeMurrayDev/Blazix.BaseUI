namespace BlazorBaseUI.ContextMenu;

using BlazorBaseUI.Menu;

/// <summary>
/// An individual interactive item within the context menu.
/// Renders a <c>&lt;div&gt;</c> element.
/// </summary>
// lint-ignore:RULE-11 Public type intentionally left unsealed; consumers may subclass for customization.
public class ContextMenuItem : MenuItem;
