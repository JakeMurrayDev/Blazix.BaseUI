namespace BlazorBaseUI.Field;

internal sealed record FieldControlRegistration(
    string? Id,
    string? Name,
    Func<object?> GetValue,
    Func<Task> ValidateAsync,
    Func<ValueTask> FocusAsync);
