namespace Blazix.BaseUI.FloatingFocusManager;

/// <summary>
/// Specifies a focus target in the tab cycling order for a floating focus manager.
/// </summary>
public enum FocusManagerOrderItem
{
    /// <summary>The content inside the floating element.</summary>
    Content,

    /// <summary>The floating element itself.</summary>
    Floating,

    /// <summary>The reference/trigger element.</summary>
    Reference
}
