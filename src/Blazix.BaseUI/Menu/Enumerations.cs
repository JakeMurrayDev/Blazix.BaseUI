namespace Blazix.BaseUI.Menu;

/// <summary>
/// Specifies the modal behavior of the menu when open.
/// </summary>
public enum MenuModalMode
{
    /// <summary>The menu is not modal; user interaction with the rest of the document is allowed.</summary>
    False,

    /// <summary>The menu is modal; page scroll is locked and pointer interactions on outside elements are disabled.</summary>
    True
}

/// <summary>
/// Specifies the type of instant transition to apply.
/// </summary>
public enum MenuInstantType
{
    /// <summary>No instant transition.</summary>
    None,

    /// <summary>Instant transition triggered by a click interaction.</summary>
    Click,

    /// <summary>Instant transition triggered by a dismiss interaction.</summary>
    Dismiss,

    /// <summary>Instant transition triggered by a group interaction.</summary>
    Group,

    /// <summary>Instant transition triggered by switching between detached triggers.</summary>
    TriggerChange
}

/// <summary>
/// Specifies the reason the menu's open state changed.
/// </summary>
public enum MenuOpenChangeReason
{
    /// <summary>The trigger was hovered.</summary>
    TriggerHover,

    /// <summary>The trigger received focus.</summary>
    TriggerFocus,

    /// <summary>The trigger was pressed.</summary>
    TriggerPress,

    /// <summary>A click occurred outside the menu.</summary>
    OutsidePress,

    /// <summary>The Escape key was pressed.</summary>
    EscapeKey,

    /// <summary>A menu item was pressed.</summary>
    ItemPress,

    /// <summary>A close button was pressed.</summary>
    ClosePress,

    /// <summary>Focus moved outside the menu.</summary>
    FocusOut,

    /// <summary>Keyboard list navigation occurred.</summary>
    ListNavigation,

    /// <summary>A sibling menu was opened.</summary>
    SiblingOpen,

    /// <summary>The open action was canceled.</summary>
    CancelOpen,

    /// <summary>An imperative action triggered the change.</summary>
    ImperativeAction,

    /// <summary>No specific reason.</summary>
    None
}

/// <summary>
/// Specifies the type of the menu's parent container.
/// </summary>
public enum MenuParentType
{
    /// <summary>The menu has no parent container.</summary>
    None,

    /// <summary>The menu is nested inside another menu.</summary>
    Menu,

    /// <summary>The menu is nested inside a context menu.</summary>
    ContextMenu,

    /// <summary>The menu is nested inside a menubar.</summary>
    Menubar
}

/// <summary>
/// Specifies the visual orientation of the menu.
/// </summary>
public enum MenuOrientation
{
    /// <summary>Vertical orientation; roving focus uses up/down arrow keys.</summary>
    Vertical,

    /// <summary>Horizontal orientation; roving focus uses left/right arrow keys.</summary>
    Horizontal
}

/// <summary>
/// Specifies the reason the radio group's selected value changed.
/// </summary>
public enum MenuRadioGroupChangeReason
{
    /// <summary>A radio item was pressed.</summary>
    ItemPress
}

/// <summary>
/// Specifies the reason the checkbox item's checked state changed.
/// </summary>
public enum MenuCheckboxItemChangeReason
{
    /// <summary>The checkbox item was pressed.</summary>
    ItemPress
}
