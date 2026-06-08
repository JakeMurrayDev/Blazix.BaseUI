namespace Blazix.BaseUI.Field;

/// <summary>
/// Provides imperative actions for a <see cref="FieldRoot"/> component.
/// </summary>
public sealed class FieldRootActions
{
    private readonly Func<Task> validateAsync;

    internal FieldRootActions(Func<Task> validateAsync)
    {
        this.validateAsync = validateAsync;
    }

    /// <summary>
    /// Validates the field.
    /// </summary>
    public Task ValidateAsync() => validateAsync();
}
