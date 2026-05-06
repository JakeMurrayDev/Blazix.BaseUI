namespace BlazorBaseUI;

/// <summary>
/// Provides helper methods for applying accessibility attributes to elements.
/// Covers button semantics, label wiring, and focusable-when-disabled patterns.
/// </summary>
internal static class AccessibilityUtilities
{
    /// <summary>
    /// Applies button semantics to a non-native button element's attribute dictionary.
    /// Adds role="button", tabindex, and aria-disabled when needed.
    /// </summary>
    public static void ApplyButtonAttributes(
        IDictionary<string, object> attributes,
        bool disabled = false,
        bool focusableWhenDisabled = false)
    {
        attributes["role"] = "button";

        if (disabled && !focusableWhenDisabled)
        {
            attributes["aria-disabled"] = "true";
        }
        else if (disabled && focusableWhenDisabled)
        {
            attributes["aria-disabled"] = "true";
            attributes["tabindex"] = 0;
        }
        else
        {
            attributes["tabindex"] = 0;
        }
    }

    /// <summary>
    /// Returns the label ID for a control, using the convention "{controlId}-label".
    /// </summary>
    public static string GetDefaultLabelId(string controlId)
        => $"{controlId}-label";

    /// <summary>
    /// Resolves the aria-labelledby value from field-level and local label IDs.
    /// Field-level takes precedence (e.g., FieldLabel overrides local label).
    /// </summary>
    public static string? ResolveAriaLabelledBy(
        string? fieldLabelId,
        string? localLabelId)
        => fieldLabelId ?? localLabelId;

    /// <summary>
    /// Applies attributes for a disabled element that should remain focusable.
    /// Uses aria-disabled instead of disabled, preserves tabindex.
    /// </summary>
    public static void ApplyFocusableWhenDisabled(
        IDictionary<string, object> attributes,
        bool disabled,
        bool focusableWhenDisabled,
        bool isNativeButton = false)
    {
        if (!disabled)
            return;

        if (focusableWhenDisabled)
        {
            attributes.Remove("disabled");
            attributes["aria-disabled"] = "true";
        }
        else if (isNativeButton)
        {
            attributes["disabled"] = true;
        }
        else
        {
            attributes["aria-disabled"] = "true";
        }
    }
}
