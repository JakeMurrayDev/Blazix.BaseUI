namespace Blazix.BaseUI.Radio;

/// <summary>
/// Represents the current state of a <see cref="RadioIndicator"/> component.
/// </summary>
/// <param name="Checked">Gets whether the radio button is currently selected.</param>
/// <param name="Disabled">Gets whether the radio button is disabled.</param>
/// <param name="ReadOnly">Gets whether the radio button is read-only.</param>
/// <param name="Required">Gets whether the radio button is required for form submission.</param>
/// <param name="Valid">Gets whether the radio button is in a valid state, or <see langword="null"/> if validation is not applicable.</param>
/// <param name="Touched">Gets whether the radio button has been interacted with.</param>
/// <param name="Dirty">Gets whether the radio button value has changed from its initial value.</param>
/// <param name="Filled">Gets whether the radio button is checked.</param>
/// <param name="Focused">Gets whether the radio button currently has focus.</param>
/// <param name="TransitionStatus">Gets the current transition status of the indicator.</param>
public sealed record RadioIndicatorState(
    bool Checked,
    bool Disabled,
    bool ReadOnly,
    bool Required,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Filled,
    bool Focused,
    TransitionStatus TransitionStatus);
