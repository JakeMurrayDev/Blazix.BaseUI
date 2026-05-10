namespace BlazorBaseUI.NavigationMenu;

/// <summary>
/// Specifies the visual orientation of the navigation menu.
/// </summary>
public enum NavigationMenuOrientation
{
    /// <summary>Horizontal orientation; items are laid out left to right.</summary>
    Horizontal,

    /// <summary>Vertical orientation; items are laid out top to bottom.</summary>
    Vertical
}

/// <summary>
/// Specifies the reason a navigation menu value changed.
/// </summary>
public enum NavigationMenuCloseReason
{
    /// <summary>No specific interaction reason was provided.</summary>
    None,

    /// <summary>The trigger was pressed.</summary>
    TriggerPress,

    /// <summary>The trigger was hovered.</summary>
    TriggerHover,

    /// <summary>The active item changed through list keyboard navigation.</summary>
    ListNavigation,

    /// <summary>Focus moved outside the menu.</summary>
    FocusOut,

    /// <summary>The Escape key closed the menu.</summary>
    EscapeKey,

    /// <summary>A press outside the menu closed it.</summary>
    OutsidePress,

    /// <summary>A link press closed the menu.</summary>
    LinkPress
}
