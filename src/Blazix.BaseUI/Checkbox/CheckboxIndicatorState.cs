namespace Blazix.BaseUI.Checkbox;

/// <summary>
/// Represents the state of the <see cref="CheckboxIndicator"/> component.
/// </summary>
/// <param name="Checked">Whether the checkbox is checked.</param>
/// <param name="Disabled">Whether the checkbox is disabled.</param>
/// <param name="ReadOnly">Whether the checkbox is read-only.</param>
/// <param name="Required">Whether the checkbox is required.</param>
/// <param name="Indeterminate">Whether the checkbox is in an indeterminate state.</param>
/// <param name="Valid">Whether the checkbox is in a valid state, or <see langword="null"/> if not validated.</param>
/// <param name="Touched">Whether the checkbox has been touched.</param>
/// <param name="Dirty">Whether the checkbox's value has changed from its initial value.</param>
/// <param name="Filled">Whether the checkbox has a value (is filled).</param>
/// <param name="Focused">Whether the checkbox is focused.</param>
/// <param name="TransitionStatus">The current transition status of the indicator.</param>
public readonly record struct CheckboxIndicatorState(
    bool Checked,
    bool Disabled,
    bool ReadOnly,
    bool Required,
    bool Indeterminate,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Filled,
    bool Focused,
    TransitionStatus TransitionStatus)
{
    internal static CheckboxIndicatorState FromRootState(
        CheckboxRootState rootState,
        TransitionStatus transitionStatus) => new(
            Checked: rootState.Checked,
            Disabled: rootState.Disabled,
            ReadOnly: rootState.ReadOnly,
            Required: rootState.Required,
            Indeterminate: rootState.Indeterminate,
            Valid: rootState.Valid,
            Touched: rootState.Touched,
            Dirty: rootState.Dirty,
            Filled: rootState.Filled,
            Focused: rootState.Focused,
            TransitionStatus: transitionStatus);
}
