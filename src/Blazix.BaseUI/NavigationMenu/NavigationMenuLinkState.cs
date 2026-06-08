namespace Blazix.BaseUI.NavigationMenu;

/// <summary>
/// Represents the state of a <see cref="NavigationMenuLink"/> component.
/// </summary>
/// <param name="Active">Whether this link represents the current page.</param>
public readonly record struct NavigationMenuLinkState(bool Active);
