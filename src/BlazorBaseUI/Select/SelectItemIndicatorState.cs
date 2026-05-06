namespace BlazorBaseUI.Select;

/// <summary>
/// Represents the state of a <see cref="SelectItemIndicator"/> component.
/// </summary>
/// <param name="Selected">Whether the parent item is selected.</param>
/// <param name="TransitionStatus">The current transition animation status.</param>
public readonly record struct SelectItemIndicatorState(bool Selected, TransitionStatus TransitionStatus);
