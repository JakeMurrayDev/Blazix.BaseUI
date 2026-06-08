using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Tooltip;

/// <summary>
/// Provides data for the tooltip open state change event.
/// </summary>
public sealed class TooltipOpenChangeEventArgs : OpenChangeEventArgs<TooltipOpenChangeReason>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TooltipOpenChangeEventArgs"/> class.
    /// </summary>
    /// <param name="open">The requested open state of the tooltip.</param>
    /// <param name="reason">The reason for the open state change.</param>
    /// <param name="triggerId">The ID of the trigger that requested the change, if applicable.</param>
    /// <param name="triggerElement">The trigger element that requested the change, if available.</param>
    public TooltipOpenChangeEventArgs(
        bool open,
        TooltipOpenChangeReason reason,
        string? triggerId = null,
        ElementReference? triggerElement = null) : base(open, reason)
    {
        TriggerId = triggerId;
        TriggerElement = triggerElement;
    }

    /// <summary>
    /// Gets the ID of the trigger that requested the change, if applicable.
    /// </summary>
    public string? TriggerId { get; }

    /// <summary>
    /// Gets the trigger element that requested the change, if available.
    /// </summary>
    public ElementReference? TriggerElement { get; }
}
