namespace Blazix.BaseUI.Tooltip;

/// <summary>
/// Specifies which axis the tooltip should track the cursor on.
/// </summary>
public enum TrackCursorAxis
{
    /// <summary>
    /// The tooltip does not track the cursor.
    /// </summary>
    None,

    /// <summary>
    /// The tooltip tracks the cursor along the horizontal axis.
    /// </summary>
    X,

    /// <summary>
    /// The tooltip tracks the cursor along the vertical axis.
    /// </summary>
    Y,

    /// <summary>
    /// The tooltip tracks the cursor along both axes.
    /// </summary>
    Both
}

/// <summary>
/// Specifies the type of instant (non-animated) transition for the tooltip.
/// </summary>
public enum TooltipInstantType
{
    /// <summary>
    /// No instant transition is in effect.
    /// </summary>
    None,

    /// <summary>
    /// The tooltip was opened instantly due to the provider's instant phase.
    /// </summary>
    Delay,

    /// <summary>
    /// The tooltip was opened instantly via a focus interaction.
    /// </summary>
    Focus,

    /// <summary>
    /// The tooltip was dismissed instantly.
    /// </summary>
    Dismiss,

    /// <summary>
    /// The tooltip is tracking the cursor position.
    /// </summary>
    TrackingCursor
}

/// <summary>
/// Specifies the reason for a tooltip open state change.
/// </summary>
public enum TooltipOpenChangeReason
{
    /// <summary>
    /// No specific reason was provided.
    /// </summary>
    None,

    /// <summary>
    /// The tooltip was opened or closed by hovering over the trigger.
    /// </summary>
    TriggerHover,

    /// <summary>
    /// The tooltip was opened or closed by focusing the trigger.
    /// </summary>
    TriggerFocus,

    /// <summary>
    /// The tooltip was opened or closed by pressing the trigger.
    /// </summary>
    TriggerPress,

    /// <summary>
    /// The tooltip was closed by pressing outside of it.
    /// </summary>
    OutsidePress,

    /// <summary>
    /// The tooltip was closed by pressing the Escape key.
    /// </summary>
    EscapeKey,

    /// <summary>
    /// The tooltip was closed because it was disabled.
    /// </summary>
    Disabled,

    /// <summary>
    /// The tooltip was opened or closed via an imperative API call.
    /// </summary>
    ImperativeAction
}
