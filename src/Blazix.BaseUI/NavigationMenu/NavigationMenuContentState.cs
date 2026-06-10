namespace Blazix.BaseUI.NavigationMenu;

/// <summary>
/// Represents the state of a <see cref="NavigationMenuContent"/> component.
/// </summary>
/// <param name="Open">Whether this content panel is the currently active one.</param>
/// <param name="ActivationDirection">The direction from which the content was activated.</param>
/// <param name="TransitionStatus">The current transition animation status.</param>
public readonly record struct NavigationMenuContentState(bool Open, ActivationDirection ActivationDirection, TransitionStatus TransitionStatus);
