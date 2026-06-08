namespace Blazix.BaseUI.Popover;

/// <summary>
/// Specifies the modal behavior of the popover.
/// </summary>
public enum PopoverModalMode
{
    /// <summary>
    /// The popover is not modal.
    /// </summary>
    False,

    /// <summary>
    /// The popover is fully modal with a backdrop and focus trap.
    /// </summary>
    True,

    /// <summary>
    /// The popover traps focus without rendering a backdrop.
    /// </summary>
    TrapFocus
}

/// <summary>
/// Specifies the type of instant (non-animated) transition.
/// </summary>
public enum PopoverInstantType
{
    /// <summary>
    /// No instant transition is in effect.
    /// </summary>
    None,

    /// <summary>
    /// The popover was opened via a click interaction.
    /// </summary>
    Click,

    /// <summary>
    /// The popover was dismissed.
    /// </summary>
    Dismiss,

    /// <summary>
    /// The popover was closed because focus moved outside of it.
    /// </summary>
    Focus,

    /// <summary>
    /// The trigger element changed (viewport swap), disabling further transitions.
    /// </summary>
    TriggerChange
}

/// <summary>
/// Specifies the reason for a popover open state change.
/// </summary>
public enum PopoverOpenChangeReason
{
    /// <summary>
    /// The popover was opened or closed by hovering over the trigger.
    /// </summary>
    TriggerHover,

    /// <summary>
    /// The popover was opened or closed by focusing the trigger.
    /// </summary>
    TriggerFocus,

    /// <summary>
    /// The popover was opened or closed by pressing the trigger.
    /// </summary>
    TriggerPress,

    /// <summary>
    /// The popover was closed by pressing outside of it.
    /// </summary>
    OutsidePress,

    /// <summary>
    /// The popover was closed by pressing the Escape key.
    /// </summary>
    EscapeKey,

    /// <summary>
    /// The popover was closed by pressing the close button.
    /// </summary>
    ClosePress,

    /// <summary>
    /// The popover was closed because focus moved outside of it.
    /// </summary>
    FocusOut,

    /// <summary>
    /// The popover was opened or closed via an imperative API call.
    /// </summary>
    ImperativeAction,

    /// <summary>
    /// No specific reason was provided.
    /// </summary>
    None
}
