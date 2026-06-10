namespace Blazix.BaseUI.Form;

/// <summary>
/// Determines when form field validation should be triggered.
/// </summary>
public enum ValidationMode
{
    /// <summary>Validates the field when the form is submitted; re-validates on change after submission.</summary>
    OnSubmit,

    /// <summary>Validates the field when it loses focus.</summary>
    OnBlur,

    /// <summary>Validates the field on every change to its value.</summary>
    OnChange
}