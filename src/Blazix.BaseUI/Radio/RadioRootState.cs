using Blazix.BaseUI.Field;

namespace Blazix.BaseUI.Radio;

/// <summary>
/// Represents the current state of a <see cref="RadioRoot{TValue}"/> component.
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
public sealed record RadioRootState(
    bool Checked,
    bool Disabled,
    bool ReadOnly,
    bool Required,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Filled,
    bool Focused)
{
    internal static RadioRootState Default { get; } = new(
        Checked: false,
        Disabled: false,
        ReadOnly: false,
        Required: false,
        Valid: null,
        Touched: false,
        Dirty: false,
        Filled: false,
        Focused: false);

    internal static RadioRootState FromFieldState(
        FieldRootState fieldState,
        bool isChecked,
        bool isDisabled,
        bool isReadOnly,
        bool isRequired) => new(
            Checked: isChecked,
            Disabled: isDisabled,
            ReadOnly: isReadOnly,
            Required: isRequired,
            Valid: fieldState.Valid,
            Touched: fieldState.Touched,
            Dirty: fieldState.Dirty,
            Filled: fieldState.Filled,
            Focused: fieldState.Focused);
}
