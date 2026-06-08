using Blazix.BaseUI.Field;

namespace Blazix.BaseUI.NumberField;

/// <summary>
/// Represents the state of the <see cref="NumberFieldRoot"/> component,
/// exposed to style and render callbacks.
/// </summary>
/// <param name="Value">Gets the raw numeric value of the field.</param>
/// <param name="InputValue">Gets the formatted text value displayed in the input.</param>
/// <param name="Required">Gets whether the field is required.</param>
/// <param name="Disabled">Gets whether the field is disabled.</param>
/// <param name="ReadOnly">Gets whether the field is read-only.</param>
/// <param name="Scrubbing">Gets whether the field is currently being scrubbed.</param>
/// <param name="Valid">Gets whether the field is in a valid state. <see langword="null"/> when validity is unknown.</param>
/// <param name="Touched">Gets whether the field has been touched.</param>
/// <param name="Dirty">Gets whether the field value has changed from its initial value.</param>
/// <param name="Filled">Gets whether the field has a value.</param>
/// <param name="Focused">Gets whether the field is focused.</param>
public sealed record NumberFieldRootState(
    double? Value,
    string InputValue,
    bool Required,
    bool Disabled,
    bool ReadOnly,
    bool Scrubbing,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Filled,
    bool Focused)
{
    internal static NumberFieldRootState Default { get; } = new(
        Value: null,
        InputValue: string.Empty,
        Required: false,
        Disabled: false,
        ReadOnly: false,
        Scrubbing: false,
        Valid: null,
        Touched: false,
        Dirty: false,
        Filled: false,
        Focused: false);

    internal static NumberFieldRootState FromFieldState(
        FieldRootState fieldState,
        double? value,
        string inputValue,
        bool required,
        bool disabled,
        bool readOnly,
        bool scrubbing) => new(
            Value: value,
            InputValue: inputValue,
            Required: required,
            Disabled: disabled,
            ReadOnly: readOnly,
            Scrubbing: scrubbing,
            Valid: fieldState.Valid,
            Touched: fieldState.Touched,
            Dirty: fieldState.Dirty,
            Filled: fieldState.Filled,
            Focused: fieldState.Focused);
}
