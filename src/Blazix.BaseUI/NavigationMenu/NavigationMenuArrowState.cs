namespace Blazix.BaseUI.NavigationMenu;

/// <summary>
/// Represents the state of a <see cref="NavigationMenuArrow"/> component.
/// </summary>
/// <param name="Open">Whether the navigation menu popup is open.</param>
/// <param name="Side">The side on which the popup is positioned.</param>
/// <param name="Align">The alignment of the popup relative to the side.</param>
/// <param name="Uncentered">Whether the arrow is not centered relative to the anchor.</param>
public readonly record struct NavigationMenuArrowState(bool Open, Side Side, Align Align, bool Uncentered);
