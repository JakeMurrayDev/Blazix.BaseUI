namespace Blazix.BaseUI.Menu;

/// <summary>
/// Represents the state of a <see cref="MenuBackdrop"/> component.
/// </summary>
/// <param name="Open">Whether the menu is open.</param>
/// <param name="TransitionStatus">The current transition animation status.</param>
public readonly record struct MenuBackdropState(
    bool Open,
    TransitionStatus TransitionStatus);
