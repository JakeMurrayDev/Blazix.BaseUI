using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.PreviewCard;

/// <summary>
/// Provides data for the preview card open state change event.
/// </summary>
public sealed class PreviewCardOpenChangeEventArgs : OpenChangeEventArgs<PreviewCardOpenChangeReason>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PreviewCardOpenChangeEventArgs"/> class.
    /// </summary>
    /// <param name="open">The requested open state of the preview card.</param>
    /// <param name="reason">The reason for the open state change.</param>
    /// <param name="triggerId">The ID of the trigger that requested the change, if applicable.</param>
    /// <param name="triggerElement">The trigger element that requested the change, if available.</param>
    public PreviewCardOpenChangeEventArgs(
        bool open,
        PreviewCardOpenChangeReason reason,
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
