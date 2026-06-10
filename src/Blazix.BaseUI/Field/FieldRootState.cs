namespace Blazix.BaseUI.Field;

/// <summary>
/// Represents the state of the <see cref="FieldRoot"/> component.
/// </summary>
/// <param name="Disabled">Whether the field is disabled.</param>
/// <param name="Valid">Whether the field is valid. <see langword="null"/> when not yet validated.</param>
/// <param name="Touched">Whether the field has been touched.</param>
/// <param name="Dirty">Whether the field value has changed from its initial value.</param>
/// <param name="Filled">Whether the field has a value.</param>
/// <param name="Focused">Whether the field control is focused.</param>
public readonly record struct FieldRootState(
    bool Disabled,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Filled,
    bool Focused)
{
    internal static FieldRootState Default { get; } = new(
        Disabled: false,
        Valid: null,
        Touched: false,
        Dirty: false,
        Filled: false,
        Focused: false);
}
