namespace Blazix.BaseUI;

/// <summary>
/// Provides helper methods for applying accessibility attributes to elements.
/// Covers button semantics, label wiring, and focusable-when-disabled patterns.
/// </summary>
internal static class AccessibilityUtilities
{
    /// <summary>
    /// Applies button semantics to a non-native button element's attribute dictionary.
    /// Adds role="button", tabindex, and aria-disabled when needed.
    /// Mirrors the attribute portion of React's useButton + useFocusableWhenDisabled for non-native elements.
    /// </summary>
    /// <param name="attributes">The attribute dictionary to modify.</param>
    /// <param name="disabled">Whether the element is disabled.</param>
    /// <param name="focusableWhenDisabled">Whether the element should remain focusable when disabled.</param>
    /// <param name="tabIndex">The tabindex to use when focusable. Defaults to 0.</param>
    public static void ApplyButtonAttributes(
        IDictionary<string, object> attributes,
        bool disabled = false,
        bool focusableWhenDisabled = false,
        int tabIndex = 0)
    {
        attributes["role"] = "button";

        if (disabled && !focusableWhenDisabled)
        {
            attributes["aria-disabled"] = "true";
            attributes["tabindex"] = -1;
        }
        else if (disabled && focusableWhenDisabled)
        {
            attributes["aria-disabled"] = "true";
            attributes["tabindex"] = tabIndex;
        }
        else
        {
            attributes["tabindex"] = tabIndex;
        }
    }

    /// <summary>
    /// Applies native button semantics (type="button") along with disabled/focusable-when-disabled handling.
    /// Mirrors the attribute portion of React's useButton + useFocusableWhenDisabled for native button elements.
    /// </summary>
    /// <param name="attributes">The attribute dictionary to modify.</param>
    /// <param name="disabled">Whether the element is disabled.</param>
    /// <param name="focusableWhenDisabled">Whether the element should remain focusable when disabled.</param>
    /// <param name="tabIndex">The tabindex to use when focusable. Defaults to 0.</param>
    public static void ApplyNativeButtonAttributes(
        IDictionary<string, object> attributes,
        bool disabled = false,
        bool focusableWhenDisabled = false,
        int tabIndex = 0)
    {
        attributes["type"] = "button";

        if (disabled && focusableWhenDisabled)
        {
            attributes["aria-disabled"] = "true";
            attributes["tabindex"] = tabIndex;
        }
        else if (disabled)
        {
            attributes["disabled"] = true;
        }
        else
        {
            attributes["tabindex"] = tabIndex;
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
