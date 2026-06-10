namespace Blazix.BaseUI.Field;

/// <summary>
/// Represents the constraint validation state of a field control,
/// mirroring the HTML <c>ValidityState</c> interface.
/// </summary>
/// <param name="BadInput">Whether the browser cannot convert the user's input.</param>
/// <param name="CustomError">Whether a custom validation error has been set.</param>
/// <param name="PatternMismatch">Whether the value does not match the specified pattern.</param>
/// <param name="RangeOverflow">Whether the value exceeds the maximum.</param>
/// <param name="RangeUnderflow">Whether the value is below the minimum.</param>
/// <param name="StepMismatch">Whether the value does not fit the step constraint.</param>
/// <param name="TooLong">Whether the value exceeds the maximum length.</param>
/// <param name="TooShort">Whether the value is shorter than the minimum length.</param>
/// <param name="TypeMismatch">Whether the value does not match the expected type.</param>
/// <param name="ValueMissing">Whether a required field has no value.</param>
/// <param name="Valid">Whether the field is valid. <see langword="null"/> when not yet validated.</param>
public sealed record FieldValidityState(
    bool BadInput = false,
    bool CustomError = false,
    bool PatternMismatch = false,
    bool RangeOverflow = false,
    bool RangeUnderflow = false,
    bool StepMismatch = false,
    bool TooLong = false,
    bool TooShort = false,
    bool TypeMismatch = false,
    bool ValueMissing = false,
    bool? Valid = null)
{
    internal static FieldValidityState Default { get; } = new();
}
