using Blazix.BaseUI.Field;

namespace Blazix.BaseUI.Switch;

/// <summary>
/// Represents the state of the <see cref="SwitchRoot"/> component.
/// </summary>
/// <param name="Checked">Whether the switch is checked.</param>
/// <param name="Disabled">Whether the switch is disabled.</param>
/// <param name="ReadOnly">Whether the switch is read-only.</param>
/// <param name="Required">Whether the switch is required.</param>
/// <param name="Valid">Whether the switch is in a valid state, or <see langword="null"/> if not validated.</param>
/// <param name="Touched">Whether the switch has been touched.</param>
/// <param name="Dirty">Whether the switch's value has changed from its initial value.</param>
/// <param name="Filled">Whether the switch has a value (is filled).</param>
/// <param name="Focused">Whether the switch is focused.</param>
public sealed record SwitchRootState(
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
    internal static SwitchRootState Default { get; } = new(
        Checked: false,
        Disabled: false,
        ReadOnly: false,
        Required: false,
        Valid: null,
        Touched: false,
        Dirty: false,
        Filled: false,
        Focused: false);

    internal static SwitchRootState FromFieldState(
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
