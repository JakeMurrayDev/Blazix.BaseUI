using Blazix.BaseUI.Field;

namespace Blazix.BaseUI.Select;

/// <summary>
/// Represents the state of a <see cref="SelectRoot{TValue}"/> component.
/// </summary>
/// <param name="Open">Whether the select popup is open.</param>
/// <param name="Valid">Whether the select is in a valid state, or <see langword="null"/> if not validated.</param>
/// <param name="Touched">Whether the select has been touched.</param>
/// <param name="Dirty">Whether the select's value has changed from its initial value.</param>
/// <param name="Filled">Whether the select has a value (is filled).</param>
/// <param name="Focused">Whether the select trigger is focused.</param>
public readonly record struct SelectRootState(
    bool Open,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Filled,
    bool Focused)
{
    internal static SelectRootState Default { get; } = new(
        Open: false,
        Valid: null,
        Touched: false,
        Dirty: false,
        Filled: false,
        Focused: false);

    internal static SelectRootState FromFieldState(
        FieldRootState fieldState,
        bool isOpen) => new(
            Open: isOpen,
            Valid: fieldState.Valid,
            Touched: fieldState.Touched,
            Dirty: fieldState.Dirty,
            Filled: fieldState.Filled,
            Focused: fieldState.Focused);
}
