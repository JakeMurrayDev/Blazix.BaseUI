namespace Blazix.BaseUI.Menu;

/// <summary>
/// Represents the state of a <see cref="MenuPositioner"/> component.
/// </summary>
/// <param name="Open">Whether the menu popup is open.</param>
/// <param name="Side">Which side the popup is positioned relative to the trigger.</param>
/// <param name="Align">How the popup is aligned relative to the specified side.</param>
/// <param name="AnchorHidden">Whether the anchor is hidden.</param>
/// <param name="Nested">Whether the menu is nested inside another menu.</param>
public readonly record struct MenuPositionerState(
    bool Open,
    Side Side,
    Align Align,
    bool AnchorHidden,
    bool Nested,
    MenuInstantType Instant);
