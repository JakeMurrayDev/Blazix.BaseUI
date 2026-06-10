using Blazix.BaseUI.Field;

namespace Blazix.BaseUI.RadioGroup;

/// <summary>
/// Represents the current state of a <see cref="RadioGroup{TValue}"/> component.
/// </summary>
/// <param name="Disabled">Gets whether the radio group is disabled.</param>
/// <param name="ReadOnly">Gets whether the radio group is read-only.</param>
/// <param name="Required">Gets whether the radio group is required for form submission.</param>
/// <param name="Valid">Gets whether the radio group is in a valid state, or <see langword="null"/> if validation is not applicable.</param>
/// <param name="Touched">Gets whether the radio group has been interacted with.</param>
/// <param name="Dirty">Gets whether the radio group value has changed from its initial value.</param>
/// <param name="Filled">Gets whether a radio button is selected.</param>
/// <param name="Focused">Gets whether the radio group currently has focus.</param>
public sealed record RadioGroupState(
    bool Disabled,
    bool ReadOnly,
    bool Required,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Filled,
    bool Focused)
{
    internal static RadioGroupState Default { get; } = new(
        Disabled: false,
        ReadOnly: false,
        Required: false,
        Valid: null,
        Touched: false,
        Dirty: false,
        Filled: false,
        Focused: false);

    internal static RadioGroupState FromFieldState(
        FieldRootState fieldState,
        bool isDisabled,
        bool isReadOnly,
        bool isRequired) => new(
            Disabled: isDisabled,
            ReadOnly: isReadOnly,
            Required: isRequired,
            Valid: fieldState.Valid,
            Touched: fieldState.Touched,
            Dirty: fieldState.Dirty,
            Filled: fieldState.Filled,
            Focused: fieldState.Focused);
}
