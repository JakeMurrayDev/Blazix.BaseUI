namespace BlazorBaseUI.Field;

/// <summary>
/// Represents the state of the <see cref="FieldError"/> component.
/// </summary>
/// <param name="Disabled">Whether the field is disabled.</param>
/// <param name="Valid">Whether the field is valid. <see langword="null"/> when not yet validated.</param>
/// <param name="Touched">Whether the field has been touched.</param>
/// <param name="Dirty">Whether the field value has changed from its initial value.</param>
/// <param name="Filled">Whether the field has a value.</param>
/// <param name="Focused">Whether the field control is focused.</param>
/// <param name="TransitionStatus">The current transition status.</param>
public readonly record struct FieldErrorState(
    bool Disabled,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Filled,
    bool Focused,
    FieldTransitionStatus? TransitionStatus)
{
    internal static FieldErrorState FromFieldState(
        FieldRootState state,
        FieldTransitionStatus? transitionStatus) => new(
            Disabled: state.Disabled,
            Valid: state.Valid,
            Touched: state.Touched,
            Dirty: state.Dirty,
            Filled: state.Filled,
            Focused: state.Focused,
            TransitionStatus: transitionStatus);
}
