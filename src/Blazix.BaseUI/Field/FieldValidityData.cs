namespace Blazix.BaseUI.Field;

/// <summary>
/// Contains the current validity state and error information for a field.
/// </summary>
/// <param name="State">The constraint validation state of the field.</param>
/// <param name="Errors">All current validation error messages.</param>
/// <param name="Error">The first validation error message, or an empty string if valid.</param>
/// <param name="Value">The current value of the field control.</param>
/// <param name="InitialValue">The initial value of the field control.</param>
public sealed record FieldValidityData(
    FieldValidityState State,
    string[] Errors,
    string Error,
    object? Value,
    object? InitialValue)
{
    internal static FieldValidityData Default { get; } = new(
        State: FieldValidityState.Default,
        Errors: [],
        Error: string.Empty,
        Value: null,
        InitialValue: null);
}
