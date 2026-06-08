namespace Blazix.BaseUI.Dialog;

/// <summary>
/// Specifies the reason the dialog's open state changed.
/// </summary>
public enum DialogOpenChangeReason
{
    /// <summary>No specific reason.</summary>
    None,

    /// <summary>The trigger button was pressed.</summary>
    TriggerPress,

    /// <summary>A click occurred outside the dialog.</summary>
    OutsidePress,

    /// <summary>The Escape key was pressed.</summary>
    EscapeKey,

    /// <summary>The close button was pressed.</summary>
    ClosePress,

    /// <summary>Focus moved outside the dialog.</summary>
    FocusOut,

    /// <summary>An imperative action was invoked.</summary>
    ImperativeAction,

    /// <summary>The popup was closed by a swipe gesture.</summary>
    Swipe,

    /// <summary>The platform close watcher requested closing.</summary>
    CloseWatcher
}

/// <summary>
/// Specifies the modal behavior of the dialog.
/// </summary>
public enum DialogModalMode
{
    /// <summary>Non-modal: user interaction with the rest of the document is allowed.</summary>
    False,

    /// <summary>Modal: focus is trapped, page scroll is locked, and pointer interactions on outside elements are disabled.</summary>
    True,

    /// <summary>Focus is trapped inside the dialog, but page scroll is not locked and pointer interactions outside remain enabled.</summary>
    TrapFocus
}

/// <summary>
/// Specifies the ARIA role of the dialog.
/// </summary>
public enum DialogRole
{
    /// <summary>A standard dialog role.</summary>
    Dialog,

    /// <summary>An alert dialog role for important messages that require user acknowledgment.</summary>
    AlertDialog
}
