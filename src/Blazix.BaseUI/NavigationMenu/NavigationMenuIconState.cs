namespace Blazix.BaseUI.NavigationMenu;

/// <summary>
/// Represents the state of a <see cref="NavigationMenuIcon"/> component.
/// </summary>
/// <param name="Open">Whether the parent item is currently active.</param>
public readonly record struct NavigationMenuIconState(bool Open);
