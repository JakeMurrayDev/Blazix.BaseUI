namespace Blazix.BaseUI.Select;

/// <summary>
/// Specifies the modal behavior of the select when open.
/// </summary>
public enum SelectModalMode
{
    /// <summary>The select is not modal; user interaction with the rest of the document is allowed.</summary>
    False,

    /// <summary>The select is modal; page scroll is locked and pointer interactions on outside elements are disabled.</summary>
    True
}

/// <summary>
/// Specifies the reason the select's open state changed.
/// </summary>
public enum SelectOpenChangeReason
{
    /// <summary>The trigger was pressed.</summary>
    TriggerPress,

    /// <summary>A click occurred outside the select.</summary>
    OutsidePress,

    /// <summary>The Escape key was pressed.</summary>
    EscapeKey,

    /// <summary>An item was pressed.</summary>
    ItemPress,

    /// <summary>Focus moved outside the select.</summary>
    FocusOut,

    /// <summary>Keyboard list navigation triggered the change.</summary>
    ListNavigation,

    /// <summary>An open action was canceled mid-flight.</summary>
    CancelOpen,

    /// <summary>A window resize triggered the change.</summary>
    WindowResize,

    /// <summary>No specific reason.</summary>
    None
}

