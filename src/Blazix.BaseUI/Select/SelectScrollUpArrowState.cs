namespace Blazix.BaseUI.Select;

/// <summary>
/// Represents the state of a <see cref="SelectScrollUpArrow"/> component.
/// </summary>
/// <param name="Visible">Whether the scroll-up arrow is currently visible.</param>
/// <param name="Side">Which side the popup is positioned relative to the trigger.</param>
/// <param name="TransitionStatus">The current transition animation status.</param>
public readonly record struct SelectScrollUpArrowState(
    bool Visible,
    Side Side,
    TransitionStatus TransitionStatus);
