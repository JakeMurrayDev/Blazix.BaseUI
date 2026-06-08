namespace Blazix.BaseUI.Menu;

/// <summary>
/// Represents the state of a <see cref="MenuItem"/> component.
/// </summary>
/// <param name="Disabled">Whether the menu item is disabled.</param>
/// <param name="Highlighted">Whether the menu item is highlighted.</param>
public readonly record struct MenuItemState(
    bool Disabled,
    bool Highlighted);
