using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.PreviewCard;

/// <summary>
/// Provides shared state and operations for child components of a <see cref="PreviewCardRoot"/>.
/// </summary>
internal sealed class PreviewCardRootContext
{
    /// <summary>
    /// Occurs when the root state changes and children should re-render.
    /// </summary>
    public event Action? StateChanged;

    /// <summary>
    /// Gets or sets the unique identifier of the preview card root.
    /// </summary>
    public string RootId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier of the popup element.
    /// </summary>
    public string PopupId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the preview card is currently open.
    /// </summary>
    public bool Open { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the preview card is mounted in the DOM.
    /// </summary>
    public bool Mounted { get; set; }

    /// <summary>
    /// Gets or sets the reason for the most recent open state change.
    /// </summary>
    public PreviewCardOpenChangeReason OpenChangeReason { get; set; }

    /// <summary>
    /// Gets or sets the current transition status.
    /// </summary>
    public TransitionStatus TransitionStatus { get; set; }

    /// <summary>
    /// Gets or sets the current instant transition type.
    /// </summary>
    public PreviewCardInstantType InstantType { get; set; }

    /// <summary>
    /// Gets or sets the ID of the currently active trigger.
    /// </summary>
    public string? ActiveTriggerId { get; set; }

    /// <summary>
    /// Gets or sets the current payload value from the active trigger.
    /// </summary>
    public object? Payload { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a viewport component is present.
    /// </summary>
    public bool HasViewport { get; set; }

    /// <summary>
    /// Gets or sets the delegate that returns the current open state.
    /// </summary>
    public Func<bool> GetOpen { get; set; } = null!;

    /// <summary>
    /// Gets or sets the delegate that returns the current mounted state.
    /// </summary>
    public Func<bool> GetMounted { get; set; } = null!;

    /// <summary>
    /// Gets or sets the delegate that returns the current payload.
    /// </summary>
    public Func<object?> GetPayload { get; set; } = null!;

    /// <summary>
    /// Gets or sets the delegate that returns the active trigger element reference.
    /// </summary>
    public Func<ElementReference?> GetTriggerElement { get; set; } = null!;

    /// <summary>
    /// Gets or sets the delegate that returns a trigger element by ID.
    /// </summary>
    public Func<string?, ElementReference?> GetTriggerElementById { get; set; } = null!;

    /// <summary>
    /// Gets or sets the delegate that registers a trigger element.
    /// </summary>
    public Action<string, ElementReference?> RegisterTriggerElement { get; set; } = null!;

    /// <summary>
    /// Gets or sets the delegate that unregisters a trigger element.
    /// </summary>
    public Action<string> UnregisterTriggerElement { get; set; } = null!;

    /// <summary>
    /// Gets or sets the delegate that stores the positioner element reference.
    /// </summary>
    public Action<ElementReference?> SetPositionerElement { get; set; } = null!;

    /// <summary>
    /// Gets or sets the delegate that stores the popup element reference.
    /// </summary>
    public Action<ElementReference?> SetPopupElement { get; set; } = null!;

    /// <summary>
    /// Gets or sets the delegate that returns the current popup element reference.
    /// </summary>
    public Func<ElementReference?> GetPopupElement { get; set; } = null!;

    /// <summary>
    /// Gets or sets the delegate that returns the current positioner element reference.
    /// </summary>
    public Func<ElementReference?> GetPositionerElement { get; set; } = null!;

    /// <summary>
    /// Gets or sets the delegate that sets the preview card's open state.
    /// </summary>
    public Func<bool, PreviewCardOpenChangeReason, string?, Task> SetOpenAsync { get; set; } = null!;

    /// <summary>
    /// Gets or sets the delegate that sets a trigger's payload.
    /// </summary>
    public Action<string, object?> SetTriggerPayload { get; set; } = null!;

    /// <summary>
    /// Gets or sets the delegate that forces the preview card to unmount immediately.
    /// </summary>
    public Action ForceUnmount { get; set; } = null!;

    /// <summary>
    /// Gets or sets the delegate that sets whether a viewport component is present.
    /// </summary>
    public Action<bool> SetHasViewport { get; set; } = null!;

    /// <summary>
    /// Notifies children that the root state changed.
    /// </summary>
    public void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }
}
