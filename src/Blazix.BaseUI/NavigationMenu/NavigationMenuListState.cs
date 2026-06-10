namespace Blazix.BaseUI.NavigationMenu;

/// <summary>
/// Represents the state of a <see cref="NavigationMenuList"/> component.
/// </summary>
/// <param name="Open">Whether the navigation menu is currently open.</param>
public readonly record struct NavigationMenuListState(bool Open);
