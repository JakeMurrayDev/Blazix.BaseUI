namespace Blazix.BaseUI.Collapsible;

/// <summary>
/// Provides data for the <see cref="CollapsibleRoot.OnOpenChange"/> event.
/// </summary>
public sealed class CollapsibleOpenChangeEventArgs : OpenChangeEventArgs<CollapsibleOpenChangeReason>
{
    /// <summary>
    /// Initializes a new instance of <see cref="CollapsibleOpenChangeEventArgs"/>.
    /// </summary>
    /// <param name="open">The new open state of the collapsible.</param>
    /// <param name="reason">The reason the open state changed.</param>
    public CollapsibleOpenChangeEventArgs(bool open, CollapsibleOpenChangeReason reason = CollapsibleOpenChangeReason.None)
        : base(open, reason) { }
}

/// <summary>
/// Describes the reason a collapsible open state change was triggered.
/// </summary>
public enum CollapsibleOpenChangeReason
{
    /// <summary>
    /// No specific reason.
    /// </summary>
    None,

    /// <summary>
    /// The trigger button was pressed.
    /// </summary>
    TriggerPress
}
