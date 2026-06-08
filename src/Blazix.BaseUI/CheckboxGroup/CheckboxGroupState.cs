using Blazix.BaseUI.Field;

namespace Blazix.BaseUI.CheckboxGroup;

/// <summary>
/// Represents the state of the <see cref="CheckboxGroup"/> component.
/// </summary>
/// <param name="Disabled">Whether the checkbox group is disabled.</param>
/// <param name="Valid">Whether the checkbox group is in a valid state, or <see langword="null"/> if not validated.</param>
/// <param name="Touched">Whether the checkbox group has been touched.</param>
/// <param name="Dirty">Whether the checkbox group's value has changed from its initial value.</param>
/// <param name="Filled">Whether any checkbox in the group is checked.</param>
/// <param name="Focused">Whether the checkbox group is focused.</param>
public readonly record struct CheckboxGroupState(
    bool Disabled,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Filled,
    bool Focused)
{
    internal static CheckboxGroupState Default { get; } = new(
        Disabled: false,
        Valid: null,
        Touched: false,
        Dirty: false,
        Filled: false,
        Focused: false);

    internal static CheckboxGroupState FromFieldState(
        FieldRootState fieldState,
        bool isDisabled) => new(
            Disabled: isDisabled,
            Valid: fieldState.Valid,
            Touched: fieldState.Touched,
            Dirty: fieldState.Dirty,
            Filled: fieldState.Filled,
            Focused: fieldState.Focused);
}
