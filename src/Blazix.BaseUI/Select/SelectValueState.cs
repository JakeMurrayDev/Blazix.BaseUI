namespace Blazix.BaseUI.Select;

/// <summary>
/// Represents the state of a <see cref="SelectValue{TValue}"/> component.
/// </summary>
/// <param name="Placeholder">Whether no value is selected and a placeholder is shown.</param>
/// <param name="Value">The currently selected value when in single-select mode, otherwise <c>null</c>.</param>
/// <param name="Values">The currently selected values when in multi-select mode, otherwise <c>null</c>.</param>
public readonly record struct SelectValueState(
    bool Placeholder,
    object? Value,
    IReadOnlyList<object>? Values);
