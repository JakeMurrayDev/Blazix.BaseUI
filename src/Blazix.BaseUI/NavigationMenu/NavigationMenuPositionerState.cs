namespace Blazix.BaseUI.NavigationMenu;

/// <summary>
/// Represents the state of a <see cref="NavigationMenuPositioner"/> component.
/// </summary>
/// <param name="Open">Whether the positioner is currently active.</param>
/// <param name="Side">The side on which the popup is positioned.</param>
/// <param name="Align">The alignment of the popup relative to the side.</param>
/// <param name="AnchorHidden">Whether the anchor element is hidden from view.</param>
/// <param name="Instant">Whether the positioner should skip transition animations.</param>
public readonly record struct NavigationMenuPositionerState(bool Open, Side Side, Align Align, bool AnchorHidden, bool Instant);
