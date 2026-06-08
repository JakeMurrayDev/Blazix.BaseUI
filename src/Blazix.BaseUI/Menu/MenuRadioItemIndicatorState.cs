namespace Blazix.BaseUI.Menu;

/// <summary>
/// Represents the state of a <see cref="MenuRadioItemIndicator"/> component.
/// </summary>
/// <param name="Checked">Whether the radio item is selected.</param>
/// <param name="Disabled">Whether the radio item is disabled.</param>
/// <param name="Highlighted">Whether the radio item is highlighted.</param>
/// <param name="TransitionStatus">The current transition animation status.</param>
public readonly record struct MenuRadioItemIndicatorState(
    bool Checked,
    bool Disabled,
    bool Highlighted,
    TransitionStatus TransitionStatus);
