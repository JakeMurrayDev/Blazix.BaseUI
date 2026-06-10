namespace Blazix.BaseUI.NavigationMenu;

/// <summary>
/// Represents the state of a <see cref="NavigationMenuTrigger"/> component.
/// </summary>
/// <param name="Open">Whether the associated item's content is currently displayed.</param>
public readonly record struct NavigationMenuTriggerState(bool Open);
