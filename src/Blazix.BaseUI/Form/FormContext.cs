using Blazix.BaseUI.Field;
using Microsoft.AspNetCore.Components.Forms;

namespace Blazix.BaseUI.Form;

/// <summary>
/// Defines the contract for a field registered with a form.
/// </summary>
internal interface IFieldRegistration
{
    /// <summary>Gets the name that identifies the field.</summary>
    string? Name { get; }

    /// <summary>Gets a function that returns the current value of the field.</summary>
    Func<object?> GetValue { get; }

    /// <summary>Gets a function that validates the field.</summary>
    Func<Task> ValidateAsync { get; }

    /// <summary>Gets the current validity data for the field.</summary>
    FieldValidityData ValidityData { get; }

    /// <summary>Gets a function that focuses the field control.</summary>
    Func<ValueTask> FocusAsync { get; }
}

/// <summary>
/// Manages field registrations for a <see cref="FormRoot"/> component.
/// </summary>
internal sealed class FieldRegistry
{
    private readonly Dictionary<string, IFieldRegistration> fields = new(8);

    /// <summary>Gets the registered fields.</summary>
    public IReadOnlyDictionary<string, IFieldRegistration> Fields => fields;

    /// <summary>Registers a field with the specified identifier.</summary>
    public void Register(string id, IFieldRegistration registration) =>
        fields[id] = registration;

    /// <summary>Unregisters the field with the specified identifier.</summary>
    public void Unregister(string id) =>
        fields.Remove(id);

    /// <summary>Validates all registered fields.</summary>
    public async Task ValidateAllAsync()
    {
        foreach (var field in fields.Values)
        {
            await field.ValidateAsync();
        }
    }

    /// <summary>Returns the first invalid field registration, or <see langword="null"/> if all fields are valid.</summary>
    public IFieldRegistration? GetFirstInvalid() =>
        fields.Values.FirstOrDefault(f => f.ValidityData.State.Valid == false);
}

/// <summary>
/// Defines the context contract for the <see cref="FormRoot"/> component.
/// </summary>
internal interface IFormContext
{
    /// <summary>Gets the associated <see cref="Microsoft.AspNetCore.Components.Forms.EditContext"/>.</summary>
    EditContext? EditContext { get; }

    /// <summary>Gets when form fields should be validated.</summary>
    ValidationMode ValidationMode { get; }

    /// <summary>Gets the registry of fields within this form.</summary>
    FieldRegistry FieldRegistry { get; }

    /// <summary>Returns whether the specified field has validation errors.</summary>
    bool HasError(string? name);

    /// <summary>Returns the validation error messages for the specified field.</summary>
    string[] GetErrors(string? name);

    /// <summary>Clears validation errors for the specified field.</summary>
    void ClearErrors(string? name);

    /// <summary>Returns whether a form submission has been attempted.</summary>
    bool GetSubmitAttempted();
}

/// <summary>
/// Provides the cascading context for the <see cref="FormRoot"/> component.
/// </summary>
internal sealed class FormContext : IFormContext
{
    private Dictionary<string, string[]> errors = new(4);
    private Action<string?>? clearErrorsCallback;
    private Func<bool>? getSubmitAttemptedCallback;

    public EditContext? EditContext { get; private set; }
    public ValidationMode ValidationMode { get; private set; } = ValidationMode.OnSubmit;
    public FieldRegistry FieldRegistry { get; private set; }

    public FormContext(
        EditContext? editContext,
        FieldRegistry fieldRegistry,
        Action<string?> clearErrors,
        Func<bool> getSubmitAttempted)
    {
        EditContext = editContext;
        FieldRegistry = fieldRegistry;
        clearErrorsCallback = clearErrors;
        getSubmitAttemptedCallback = getSubmitAttempted;
    }

    internal void Update(
        EditContext? editContext,
        Dictionary<string, string[]> errors,
        ValidationMode validationMode)
    {
        EditContext = editContext;
        this.errors = errors;
        ValidationMode = validationMode;
    }

    public bool HasError(string? name) =>
        name is not null && errors.TryGetValue(name, out var errorList) && errorList.Length > 0;

    public string[] GetErrors(string? name) =>
        name is not null && errors.TryGetValue(name, out var errorList) ? errorList : [];

    public void ClearErrors(string? name) => clearErrorsCallback?.Invoke(name);

    public bool GetSubmitAttempted() => getSubmitAttemptedCallback?.Invoke() ?? false;
}
