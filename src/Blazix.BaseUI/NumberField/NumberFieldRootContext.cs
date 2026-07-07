using Microsoft.AspNetCore.Components;
using Blazix.BaseUI.Slider;

namespace Blazix.BaseUI.NumberField;

/// <summary>
/// Provides shared state and callbacks for child components of the <see cref="NumberFieldRoot"/>.
/// Cascaded from the root to all descendants.
/// </summary>
internal sealed class NumberFieldRootContext
{
    /// <summary>
    /// Gets or sets the formatted text value displayed in the input.
    /// </summary>
    public string InputValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the raw numeric value of the field.
    /// </summary>
    public double? Value { get; set; }

    /// <summary>
    /// Gets or sets the resolved minimum value, falling back to the JavaScript safe integer minimum when unset.
    /// </summary>
    public double MinWithDefault { get; set; } = NumberFieldUtilities.DefaultMin;

    /// <summary>
    /// Gets or sets the resolved maximum value, falling back to the JavaScript safe integer maximum when unset.
    /// </summary>
    public double MaxWithDefault { get; set; } = NumberFieldUtilities.DefaultMax;

    /// <summary>
    /// Gets or sets the minimum value of the field.
    /// </summary>
    public double? Min { get; set; }

    /// <summary>
    /// Gets or sets the maximum value of the field.
    /// </summary>
    public double? Max { get; set; }

    /// <summary>
    /// Gets or sets whether the field is disabled.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets whether the field is read-only.
    /// </summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Gets or sets the id of the input element.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the name that identifies the field when a form is submitted.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets whether the field is required.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets whether the field is in an invalid state.
    /// </summary>
    public bool? Invalid { get; set; }

    /// <summary>
    /// Gets or sets the input mode for the input element.
    /// </summary>
    public string InputMode { get; set; } = "numeric";

    /// <summary>
    /// Gets or sets whether the field is currently being scrubbed.
    /// </summary>
    public bool IsScrubbing { get; set; }

    /// <summary>
    /// Gets or sets the locale used for number formatting.
    /// </summary>
    public string? Locale { get; set; }

    /// <summary>
    /// Gets or sets the number format options for the input value.
    /// </summary>
    public NumberFormatOptions? FormatOptions { get; set; }

    /// <summary>
    /// Gets or sets whether direct input may exceed the configured range.
    /// </summary>
    public bool AllowOutOfRange { get; set; }

    /// <summary>
    /// Gets or sets whether a programmatic value change is waiting to be committed.
    /// </summary>
    public bool HasPendingCommit { get; set; }

    /// <summary>
    /// Gets or sets whether programmatic value changes may synchronize the displayed input text.
    /// </summary>
    public bool AllowInputSync { get; set; } = true;

    /// <summary>
    /// Gets or sets the current component state exposed to style and render callbacks.
    /// </summary>
    public NumberFieldRootState State { get; set; } = NumberFieldRootState.Default;

    /// <summary>
    /// Gets or sets the <see cref="ElementReference"/> to the input element.
    /// </summary>
    public ElementReference? InputElement { get; set; }

    /// <summary>
    /// Sets the numeric value with a reason and optional direction.
    /// </summary>
    public Func<double?, NumberFieldChangeReason, int?, Task<bool>> SetValue { get; set; } = null!;

    /// <summary>
    /// Returns the validated numeric value for the specified reason and optional direction.
    /// </summary>
    public Func<double?, NumberFieldChangeReason, int?, double?> GetValidatedValue { get; set; } = null!;

    /// <summary>
    /// Increments the value by the specified amount in the given direction.
    /// </summary>
    public Func<double, int, NumberFieldChangeReason, double?, Task<bool>> IncrementValue { get; set; } = null!;

    /// <summary>
    /// Returns the last accepted value for a step-based commit.
    /// </summary>
    public Func<double?> GetCommitValue { get; set; } = null!;

    /// <summary>
    /// Returns the step amount based on the current modifier keys.
    /// </summary>
    public Func<bool, bool, double> GetStepAmount { get; set; } = null!;

    /// <summary>
    /// Starts automatic value change (press-and-hold) in the specified direction.
    /// </summary>
    public Action<bool> StartAutoChange { get; set; } = null!;

    /// <summary>
    /// Stops automatic value change (press-and-hold).
    /// </summary>
    public Action StopAutoChange { get; set; } = null!;

    /// <summary>
    /// Sets the displayed input text directly without parsing.
    /// </summary>
    public Action<string> SetInputValue { get; set; } = null!;

    /// <summary>
    /// Sets whether programmatic value changes should synchronize the displayed input text.
    /// </summary>
    public Action<bool> SetAllowInputSync { get; set; } = null!;

    /// <summary>
    /// Sets the scrubbing state of the number field.
    /// </summary>
    public Action<bool> SetIsScrubbing { get; set; } = null!;

    /// <summary>
    /// Notifies that a value has been committed.
    /// </summary>
    public Action<double?, NumberFieldChangeReason> OnValueCommitted { get; set; } = null!;

    /// <summary>
    /// Sets the <see cref="ElementReference"/> for the input element.
    /// </summary>
    public Action<ElementReference> SetInputElement { get; set; } = null!;

    /// <summary>
    /// Programmatically focuses the input element.
    /// </summary>
    public Action FocusInput { get; set; } = null!;

    /// <summary>
    /// Sets the selection range of the input element.
    /// </summary>
    public Action<int, int> SetSelectionRange { get; set; } = null!;
}
