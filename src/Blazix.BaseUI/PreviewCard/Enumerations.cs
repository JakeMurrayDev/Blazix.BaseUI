namespace Blazix.BaseUI.PreviewCard;

/// <summary>
/// Specifies the type of instant (non-animated) transition for the preview card.
/// </summary>
public enum PreviewCardInstantType
{
    /// <summary>
    /// No instant transition is in effect.
    /// </summary>
    None,

    /// <summary>
    /// The preview card was opened instantly via a focus interaction.
    /// </summary>
    Focus,

    /// <summary>
    /// The preview card was dismissed instantly.
    /// </summary>
    Dismiss
}

/// <summary>
/// Specifies the reason for a preview card open state change.
/// </summary>
public enum PreviewCardOpenChangeReason
{
    /// <summary>
    /// No specific reason was provided.
    /// </summary>
    None,

    /// <summary>
    /// The preview card was opened or closed by hovering over the trigger.
    /// </summary>
    TriggerHover,

    /// <summary>
    /// The preview card was opened or closed by focusing the trigger.
    /// </summary>
    TriggerFocus,

    /// <summary>
    /// The preview card was opened or closed by pressing the trigger.
    /// </summary>
    TriggerPress,

    /// <summary>
    /// The preview card was closed by pressing outside of it.
    /// </summary>
    OutsidePress,

    /// <summary>
    /// The preview card was closed by pressing the Escape key.
    /// </summary>
    EscapeKey,

    /// <summary>
    /// The preview card was opened or closed via an imperative API call.
    /// </summary>
    ImperativeAction
}
