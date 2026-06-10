namespace Blazix.BaseUI.Select;

/// <summary>
/// Represents the state of a <see cref="SelectPositioner"/> component.
/// </summary>
/// <param name="Open">Whether the select popup is open.</param>
/// <param name="Side">Which side the popup is positioned relative to the trigger.</param>
/// <param name="Align">How the popup is aligned relative to the specified side.</param>
/// <param name="AnchorHidden">Whether the anchor is hidden.</param>
public readonly record struct SelectPositionerState(
    bool Open,
    Side Side,
    Align Align,
    bool AnchorHidden);
