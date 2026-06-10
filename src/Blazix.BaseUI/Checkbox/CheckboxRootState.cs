using Blazix.BaseUI.Field;

namespace Blazix.BaseUI.Checkbox;

/// <summary>
/// Represents the state of the <see cref="CheckboxRoot"/> component.
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
public readonly record struct CheckboxRootState(
    bool Checked,
    bool Disabled,
    bool ReadOnly,
    bool Required,
    bool Indeterminate,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Filled,
    bool Focused)
{
    internal static CheckboxRootState Default { get; } = new(
        Checked: false,
        Disabled: false,
        ReadOnly: false,
        Required: false,
        Indeterminate: false,
        Valid: null,
        Touched: false,
        Dirty: false,
        Filled: false,
        Focused: false);

    internal static CheckboxRootState FromFieldState(
        FieldRootState fieldState,
        bool isChecked,
        bool isDisabled,
        bool isReadOnly,
        bool isRequired,
        bool isIndeterminate) => new(
            Checked: isChecked,
            Disabled: isDisabled,
            ReadOnly: isReadOnly,
            Required: isRequired,
            Indeterminate: isIndeterminate,
            Valid: fieldState.Valid,
            Touched: fieldState.Touched,
            Dirty: fieldState.Dirty,
            Filled: fieldState.Filled,
            Focused: fieldState.Focused);
}
