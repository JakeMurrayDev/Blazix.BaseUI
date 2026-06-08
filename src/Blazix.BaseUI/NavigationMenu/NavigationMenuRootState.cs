namespace Blazix.BaseUI.NavigationMenu;

/// <summary>
/// Represents the state of a <see cref="NavigationMenuRoot"/> component.
/// </summary>
/// <param name="Open">Whether the navigation menu is currently open.</param>
/// <param name="Nested">Whether this is a nested navigation menu.</param>
public readonly record struct NavigationMenuRootState(bool Open, bool Nested);
