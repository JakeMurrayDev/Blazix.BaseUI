using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Blazix.BaseUI.Base;

/// <summary>
/// Base class for form control components that support data binding, validation, and
/// integration with <see cref="EditContext"/>.
/// </summary>
/// <typeparam name="TValue">The type of the value bound to the control.</typeparam>
public abstract class ControlBase<TValue> : ComponentBase, IDisposable
{
    private readonly EventHandler<ValidationStateChangedEventArgs> validationStateChangedHandler;
    private bool hasInitializedParameters;
    private bool parsingFailed;
    private string? incomingValueBeforeParsing;
    private bool previousParsingAttemptFailed;
    private ValidationMessageStore? parsingValidationMessages;
    private Type? nullableUnderlyingType;
    private TValue? currentValue;
    private bool hasFieldIdentifier;
    private string? formattedValueExpression;

    [CascadingParameter]
    private EditContext? CascadedEditContext { get; set; }

    /// <summary>
    /// Gets or sets a collection of additional attributes that will be applied to the created element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    /// Gets or sets the current value of the control.
    /// To render an uncontrolled control, use <see cref="DefaultValue"/> instead.
    /// </summary>
    [Parameter]
    public TValue? Value { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the value changes via two-way binding.
    /// </summary>
    [Parameter]
    public EventCallback<TValue> ValueChanged { get; set; }

    /// <summary>
    /// Gets or sets an expression that identifies the bound value,
    /// used to integrate with <see cref="EditContext"/> validation.
    /// </summary>
    [Parameter]
    public Expression<Func<TValue>>? ValueExpression { get; set; }

    /// <summary>
    /// Gets or sets the initial value of the control.
    /// To render a controlled control, use <see cref="Value"/> instead.
    /// </summary>
    [Parameter]
    public TValue? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets the human-readable display name for the field,
    /// used in validation error messages.
    /// </summary>
    [Parameter]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the associated <see cref="Microsoft.AspNetCore.Components.Forms.EditContext"/>.
    /// </summary>
    protected EditContext? EditContext { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="Microsoft.AspNetCore.Components.Forms.FieldIdentifier"/>
    /// for this control, derived from <see cref="ValueExpression"/>.
    /// </summary>
    protected FieldIdentifier FieldIdentifier { get; set; }

    /// <summary>
    /// Gets whether the control is in controlled mode (i.e., <see cref="ValueChanged"/> has a delegate).
    /// </summary>
    protected bool IsControlled => ValueChanged.HasDelegate;

    /// <summary>
    /// Gets the <c>name</c> attribute value for the control element,
    /// resolved from additional attributes or the <see cref="ValueExpression"/>.
    /// </summary>
    protected string NameAttributeValue
    {
        get
        {
            if (AdditionalAttributes?.TryGetValue("name", out var nameAttributeValue) ?? false)
            {
                return Convert.ToString(nameAttributeValue, CultureInfo.InvariantCulture) ?? string.Empty;
            }

            if (hasFieldIdentifier && ValueExpression is not null)
            {
                formattedValueExpression ??= ExpressionFormatter.FormatLambda(ValueExpression);
                return formattedValueExpression;
            }

            return string.Empty;
        }
    }

    /// <summary>
    /// Gets or sets the current value of the control.
    /// In controlled mode, reads from <see cref="Value"/>; in uncontrolled mode, reads from internal state.
    /// Setting this property triggers change notifications and validation as appropriate.
    /// </summary>
    protected TValue? CurrentValue
    {
        get => IsControlled ? Value : currentValue;
        set
        {
            var previousValue = IsControlled ? Value : currentValue;
            var hasChanged = !EqualityComparer<TValue>.Default.Equals(value, previousValue);
            if (hasChanged)
            {
                parsingFailed = false;

                if (IsControlled)
                {
                    _ = ValueChanged.InvokeAsync(value);
                    if (hasFieldIdentifier)
                    {
                        EditContext?.NotifyFieldChanged(FieldIdentifier);
                    }
                }
                else
                {
                    currentValue = value;
                    StateHasChanged();
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the current value as a string representation.
    /// Setting this property attempts to parse the string into <typeparamref name="TValue"/>
    /// via <see cref="TryParseValueFromString"/> and updates <see cref="CurrentValue"/> on success.
    /// </summary>
    protected string? CurrentValueAsString
    {
        get => parsingFailed ? incomingValueBeforeParsing : FormatValueAsString(CurrentValue);
        set
        {
            incomingValueBeforeParsing = value;
            parsingValidationMessages?.Clear();

            if (nullableUnderlyingType is not null && string.IsNullOrEmpty(value))
            {
                parsingFailed = false;
                CurrentValue = default!;
            }
            else if (TryParseValueFromString(value, out var parsedValue, out var validationErrorMessage))
            {
                parsingFailed = false;
                CurrentValue = parsedValue!;
            }
            else
            {
                parsingFailed = true;

                if (EditContext is not null && hasFieldIdentifier)
                {
                    parsingValidationMessages ??= new ValidationMessageStore(EditContext);
                    parsingValidationMessages.Add(FieldIdentifier, validationErrorMessage);
                    EditContext.NotifyFieldChanged(FieldIdentifier);
                }
            }

            if (parsingFailed || previousParsingAttemptFailed)
            {
                if (hasFieldIdentifier)
                {
                    EditContext?.NotifyValidationStateChanged();
                }
                previousParsingAttemptFailed = parsingFailed;
            }
        }
    }

    protected ControlBase()
    {
        validationStateChangedHandler = OnValidateStateChanged;
    }

    /// <summary>
    /// Formats the value as a string. Derived classes can override this to control
    /// how the value is displayed.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>A string representation of the value.</returns>
    protected virtual string? FormatValueAsString(TValue? value) => value?.ToString();

    /// <summary>
    /// Parses a string into a value of type <typeparamref name="TValue"/>.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <param name="result">The parsed value if successful.</param>
    /// <param name="validationErrorMessage">A validation error message if parsing fails.</param>
    /// <returns><see langword="true"/> if parsing succeeded; otherwise, <see langword="false"/>.</returns>
    protected abstract bool TryParseValueFromString(
        string? value,
        [MaybeNullWhen(false)] out TValue result,
        [NotNullWhen(false)] out string? validationErrorMessage);

    /// <summary>
    /// Gets the CSS class string for the control, combining user-provided classes
    /// with any validation-related classes from the <see cref="EditContext"/>.
    /// </summary>
    protected string CssClass
    {
        get
        {
            if (!hasFieldIdentifier)
                return AttributeUtilities.CombineClassNames(AdditionalAttributes, null) ?? string.Empty;

            var fieldClass = EditContext?.FieldCssClass(FieldIdentifier);
            return AttributeUtilities.CombineClassNames(AdditionalAttributes, fieldClass) ?? string.Empty;
        }
    }

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        if (!hasInitializedParameters)
        {
            if (CascadedEditContext is not null)
            {
                EditContext = CascadedEditContext;
                EditContext.OnValidationStateChanged += validationStateChangedHandler;
            }

            if (ValueExpression is not null)
            {
                FieldIdentifier = FieldIdentifier.Create(ValueExpression);
                hasFieldIdentifier = true;
            }

            nullableUnderlyingType = Nullable.GetUnderlyingType(typeof(TValue));

            if (!IsControlled && DefaultValue is not null)
            {
                currentValue = DefaultValue;
            }

            hasInitializedParameters = true;
        }
        else if (CascadedEditContext != EditContext)
        {
            throw new InvalidOperationException(
                $"{GetType()} does not support changing the EditContext dynamically.");
        }

        UpdateAdditionalValidationAttributes();

        return base.SetParametersAsync(ParameterView.Empty);
    }

    private void OnValidateStateChanged(object? sender, ValidationStateChangedEventArgs eventArgs)
    {
        UpdateAdditionalValidationAttributes();
        StateHasChanged();
    }

    private void UpdateAdditionalValidationAttributes()
    {
        if (EditContext is null || !hasFieldIdentifier)
            return;

        var hasAriaInvalidAttribute = AdditionalAttributes?.ContainsKey("aria-invalid") ?? false;

        if (EditContext.GetValidationMessages(FieldIdentifier).Any())
        {
            if (hasAriaInvalidAttribute)
                return;

            if (ConvertToDictionary(AdditionalAttributes, out var additionalAttributes))
                AdditionalAttributes = additionalAttributes;

            additionalAttributes["aria-invalid"] = "true";
        }
        else if (hasAriaInvalidAttribute)
        {
            if (AdditionalAttributes!.Count == 1)
            {
                AdditionalAttributes = null;
            }
            else
            {
                if (ConvertToDictionary(AdditionalAttributes, out var additionalAttributes))
                    AdditionalAttributes = additionalAttributes;

                additionalAttributes.Remove("aria-invalid");
            }
        }
    }

    private static bool ConvertToDictionary(
        IReadOnlyDictionary<string, object>? source,
        out Dictionary<string, object> result)
    {
        var newDictionaryCreated = true;
        if (source is null)
        {
            result = new Dictionary<string, object>();
        }
        else if (source is Dictionary<string, object> currentDictionary)
        {
            result = currentDictionary;
            newDictionaryCreated = false;
        }
        else
        {
            result = new Dictionary<string, object>(source);
        }

        return newDictionaryCreated;
    }

    /// <summary>
    /// Releases resources used by the control.
    /// </summary>
    /// <param name="disposing"><see langword="true"/> if called from <see cref="IDisposable.Dispose"/>; otherwise, <see langword="false"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
    }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        if (EditContext is not null)
        {
            EditContext.OnValidationStateChanged -= validationStateChangedHandler;
        }

        if (parsingValidationMessages is not null && hasFieldIdentifier)
        {
            parsingValidationMessages.Clear();
            EditContext?.NotifyValidationStateChanged();
        }

        Dispose(disposing: true);
    }
}