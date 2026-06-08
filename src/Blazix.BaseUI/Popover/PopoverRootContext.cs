using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Popover;

/// <summary>
/// Provides cascading state and callbacks from the <see cref="PopoverRoot"/> to child popover components.
/// </summary>
internal sealed class PopoverRootContext
{
    /// <summary>
    /// Gets or sets the unique identifier for the popover root instance.
    /// </summary>
    public string RootId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the popover is currently open.
    /// </summary>
    public bool Open { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the popover is currently mounted in the DOM.
    /// </summary>
    public bool Mounted { get; set; }

    /// <summary>
    /// Gets or sets the modal behavior of the popover.
    /// </summary>
    public PopoverModalMode Modal { get; set; }

    /// <summary>
    /// Gets or sets the reason for the most recent open state change.
    /// </summary>
    public PopoverOpenChangeReason PopoverOpenChangeReason { get; set; }

    /// <summary>
    /// Gets or sets the interaction type that opened the popover (e.g. "mouse", "touch", "pen", "keyboard").
    /// </summary>
    public string? InteractionType { get; set; }

    /// <summary>
    /// Gets or sets the current transition status of the popover.
    /// </summary>
    public TransitionStatus TransitionStatus { get; set; }

    /// <summary>
    /// Gets or sets the type of instant transition currently in effect.
    /// </summary>
    public PopoverInstantType PopoverInstantType { get; set; }

    /// <summary>
    /// Gets or sets the unique element ID of the popup, used for <c>aria-controls</c> on the trigger.
    /// </summary>
    public string? PopupId { get; set; }

    /// <summary>
    /// Gets or sets the element ID of the popover title, used for <c>aria-labelledby</c>.
    /// </summary>
    public string? TitleId { get; set; }

    /// <summary>
    /// Gets or sets the element ID of the popover description, used for <c>aria-describedby</c>.
    /// </summary>
    public string? DescriptionId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the currently active trigger.
    /// </summary>
    public string? ActiveTriggerId { get; set; }

    /// <summary>
    /// Gets or sets the payload associated with the current popover interaction.
    /// </summary>
    public object? Payload { get; set; }

    /// <summary>
    /// Gets or sets a delegate that returns the current open state.
    /// </summary>
    public Func<bool> GetOpen { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that returns the current mounted state.
    /// </summary>
    public Func<bool> GetMounted { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that returns the trigger element reference.
    /// </summary>
    public Func<ElementReference?> GetTriggerElement { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that returns the trigger's post-focus-guard element,
    /// used as the <c>NextFocusableElement</c> for tab cycling out of the popup.
    /// </summary>
    public Func<ElementReference?> GetTriggerFocusTarget { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that returns the positioner element reference.
    /// </summary>
    public Func<ElementReference?> GetPositionerElement { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that returns the popup element reference.
    /// </summary>
    public Func<ElementReference?> GetPopupElement { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that sets the title element ID.
    /// </summary>
    public Action<string?> SetTitleId { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that sets the description element ID.
    /// </summary>
    public Action<string?> SetDescriptionId { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that sets the trigger element reference.
    /// </summary>
    public Action<ElementReference?> SetTriggerElement { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that sets the trigger's post-focus-guard element reference.
    /// </summary>
    public Action<ElementReference?> SetTriggerFocusTarget { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that registers a trigger by ID with its element, focus target, and payload.
    /// </summary>
    public Action<string, ElementReference?, ElementReference?, object?> RegisterTrigger { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that unregisters a trigger by ID.
    /// </summary>
    public Action<string> UnregisterTrigger { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that updates a registered trigger element by ID.
    /// </summary>
    public Action<string, ElementReference?> SetTriggerElementById { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that updates a registered trigger focus target by ID.
    /// </summary>
    public Action<string, ElementReference?> SetTriggerFocusTargetById { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that updates a registered trigger payload by ID.
    /// </summary>
    public Action<string, object?> SetTriggerPayload { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that sets the positioner element reference.
    /// </summary>
    public Action<ElementReference?> SetPositionerElement { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that sets the popup element reference.
    /// </summary>
    public Action<ElementReference?> SetPopupElement { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that returns the backdrop element reference.
    /// </summary>
    public Func<ElementReference?> GetBackdropElement { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that sets the backdrop element reference.
    /// </summary>
    public Action<ElementReference?> SetBackdropElement { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that asynchronously sets the open state with a reason, optional payload, and optional trigger ID.
    /// </summary>
    public Func<bool, PopoverOpenChangeReason, object?, string?, Task> SetOpenAsync { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that closes the popover.
    /// </summary>
    public Action Close { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether a viewport component is present.
    /// </summary>
    public bool HasViewport { get; set; }

    /// <summary>
    /// Gets or sets a delegate that forces the popover to unmount immediately.
    /// </summary>
    public Action ForceUnmount { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that sets the interaction type.
    /// </summary>
    public Action<string?> SetInteractionType { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that sets whether a viewport is present.
    /// </summary>
    public Action<bool> SetHasViewport { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that sets the instant type for transitions.
    /// </summary>
    public Action<PopoverInstantType> SetPopoverInstantType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the ID of the previously active trigger, used for viewport transitions.
    /// </summary>
    public string? PreviousActiveTriggerId { get; set; }

    /// <summary>
    /// Gets or sets the current count of registered close parts.
    /// </summary>
    public int ClosePartCount { get; set; }

    /// <summary>
    /// Gets a value indicating whether at least one close part is present.
    /// </summary>
    public bool HasClosePart => ClosePartCount > 0;

    /// <summary>
    /// Gets or sets a delegate that registers a close part and returns an unregister action.
    /// </summary>
    public Func<Action> RegisterClosePart { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether the focus manager is in modal mode,
    /// computed from <see cref="Modal"/> and <see cref="HasClosePart"/>.
    /// </summary>
    public bool FocusManagerModal { get; set; }

    /// <summary>
    /// Gets or sets the text direction for RTL support (<c>"ltr"</c> or <c>"rtl"</c>).
    /// </summary>
    public string Direction { get; set; } = "ltr";

    /// <summary>
    /// Gets or sets the floating root context adapter for use with
    /// <see cref="FloatingFocusManager.FloatingFocusManager"/> and <see cref="FloatingTree.FloatingTree"/>.
    /// </summary>
    public IFloatingRootContext? FloatingRootContext { get; set; }
}
