namespace BlazorBaseUI.Tabs;

/// <summary>
/// Indicates why the active tabs value changed.
/// </summary>
public enum TabsValueChangeReason
{
    /// <summary>
    /// No specific reason.
    /// </summary>
    None,

    /// <summary>
    /// The initial implicit selection was resolved.
    /// </summary>
    Initial,

    /// <summary>
    /// The selected tab became disabled and the root selected a fallback.
    /// </summary>
    Disabled,

    /// <summary>
    /// The selected tab was removed or was never registered.
    /// </summary>
    Missing
}
