using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Dialog;

/// <summary>
/// Provides data for the dialog open change event.
/// </summary>
public sealed class DialogOpenChangeEventArgs : OpenChangeEventArgs<DialogOpenChangeReason>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DialogOpenChangeEventArgs"/> class.
    /// </summary>
    /// <param name="open">The new open state.</param>
    /// <param name="reason">The reason for the state change.</param>
    /// <param name="nativeEvent">The native Blazor event that initiated the change, when available.</param>
    /// <param name="trigger">The trigger element that initiated or owns the change, when available.</param>
    /// <param name="triggerId">The trigger id that initiated or owns the change, when available.</param>
    /// <param name="interactionType">The interaction type that initiated the change, when available.</param>
    public DialogOpenChangeEventArgs(
        bool open,
        DialogOpenChangeReason reason,
        EventArgs? nativeEvent = null,
        ElementReference? trigger = null,
        string? triggerId = null,
        string? interactionType = null) : base(open, reason)
    {
        Event = nativeEvent;
        Trigger = trigger;
        TriggerId = triggerId;
        InteractionType = interactionType;
    }

    /// <summary>
    /// Gets the native Blazor event that initiated the change, when available.
    /// </summary>
    public EventArgs? Event { get; }

    /// <summary>
    /// Gets the trigger element that initiated or owns the change, when available.
    /// Mirrors the <c>trigger</c> field of React Base UI's <c>Dialog.Root.ChangeEventDetails</c>.
    /// </summary>
    public ElementReference? Trigger { get; }

    /// <summary>
    /// Gets the trigger id that initiated or owns the change, when available.
    /// </summary>
    public string? TriggerId { get; }

    /// <summary>
    /// Gets the interaction type that initiated the change, when available.
    /// </summary>
    public string? InteractionType { get; }
}
