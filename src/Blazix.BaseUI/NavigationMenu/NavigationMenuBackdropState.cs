namespace Blazix.BaseUI.NavigationMenu;

/// <summary>
/// Represents the state of a <see cref="NavigationMenuBackdrop"/> component.
/// </summary>
/// <param name="Open">Whether the navigation menu is currently open.</param>
/// <param name="TransitionStatus">The current transition animation status.</param>
public readonly record struct NavigationMenuBackdropState(bool Open, TransitionStatus TransitionStatus);
