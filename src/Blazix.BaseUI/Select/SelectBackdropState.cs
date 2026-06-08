namespace Blazix.BaseUI.Select;

/// <summary>
/// Represents the state of a <see cref="SelectBackdrop"/> component.
/// </summary>
/// <param name="Open">Whether the select popup is open.</param>
/// <param name="TransitionStatus">The current transition animation status.</param>
public readonly record struct SelectBackdropState(
    bool Open,
    TransitionStatus TransitionStatus);
