namespace Blazix.BaseUI.Drawer;

/// <summary>
/// Specifies the reason the drawer's open state changed.
/// </summary>
public enum DrawerOpenChangeReason
{
    /// <summary>No specific reason.</summary>
    None,

    /// <summary>The trigger button was pressed.</summary>
    TriggerPress,

    /// <summary>A click occurred outside the drawer.</summary>
    OutsidePress,

    /// <summary>The Escape key was pressed.</summary>
    EscapeKey,

    /// <summary>The platform close watcher requested closing.</summary>
    CloseWatcher,

    /// <summary>The close button was pressed.</summary>
    ClosePress,

    /// <summary>Focus moved outside the drawer.</summary>
    FocusOut,

    /// <summary>An imperative action was invoked.</summary>
    ImperativeAction,

    /// <summary>The drawer was opened or closed by a swipe gesture.</summary>
    Swipe
}

/// <summary>
/// Specifies the modal behavior of the drawer.
/// </summary>
public enum DrawerModalMode
{
    /// <summary>Non-modal: user interaction with the rest of the document is allowed.</summary>
    False,

    /// <summary>Modal: focus is trapped, page scroll is locked, and pointer interactions on outside elements are disabled.</summary>
    True,

    /// <summary>Focus is trapped inside the drawer, but page scroll is not locked and pointer interactions outside remain enabled.</summary>
    TrapFocus
}

/// <summary>
/// Specifies the direction used to dismiss the drawer.
/// </summary>
public enum DrawerSwipeDirection
{
    /// <summary>The drawer dismisses upward.</summary>
    Up,

    /// <summary>The drawer dismisses downward.</summary>
    Down,

    /// <summary>The drawer dismisses leftward.</summary>
    Left,

    /// <summary>The drawer dismisses rightward.</summary>
    Right
}
