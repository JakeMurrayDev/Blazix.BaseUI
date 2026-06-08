namespace Blazix.BaseUI.Form;

/// <summary>
/// Provides imperative actions for a <see cref="Form"/> component.
/// </summary>
public sealed class FormActions
{
    private readonly Func<string?, Task> validateAsync;

    internal FormActions(Func<string?, Task> validateAsync)
    {
        this.validateAsync = validateAsync;
    }

    /// <summary>
    /// Validates all fields in the form.
    /// </summary>
    public Task ValidateAsync() => validateAsync(null);

    /// <summary>
    /// Validates a single field in the form identified by name.
    /// </summary>
    /// <param name="fieldName">The name of the field to validate.</param>
    public Task ValidateAsync(string fieldName) => validateAsync(fieldName);
}
