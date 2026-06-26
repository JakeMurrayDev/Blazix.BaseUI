using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

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
    /// <param name="triggerEvent">The mouse event that triggered the change, when the change was caused by a trigger press.</param>
    /// <param name="triggerElement">The trigger element that caused the change, when the change was caused by a trigger press.</param>
    public CollapsibleOpenChangeEventArgs(
        bool open,
        CollapsibleOpenChangeReason reason = CollapsibleOpenChangeReason.None,
        MouseEventArgs? triggerEvent = null,
        ElementReference? triggerElement = null)
        : base(open, reason)
    {
        TriggerEvent = triggerEvent;
        TriggerElement = triggerElement;
    }

    /// <summary>
    /// Gets the mouse event that triggered the change, when the change was caused by a trigger press.
    /// </summary>
    public MouseEventArgs? TriggerEvent { get; }

    /// <summary>
    /// Gets the trigger element that caused the change, when the change was caused by a trigger press.
    /// </summary>
    public ElementReference? TriggerElement { get; }
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
