using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.NavigationMenu;

/// <summary>
/// Provides shared state and callbacks for the <see cref="NavigationMenuRoot"/> and its descendant components.
/// </summary>
internal sealed class NavigationMenuRootContext
{
    /// <summary>
    /// Gets the unique identifier for this navigation menu root instance.
    /// </summary>
    public string RootId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the value of the currently active item, or <see langword="null"/> if no item is active.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets whether the navigation menu popup is mounted in the DOM.
    /// </summary>
    public bool Mounted { get; set; }

    /// <summary>
    /// Gets whether this is a nested navigation menu.
    /// </summary>
    public bool IsNested { get; init; }

    /// <summary>
    /// Gets the visual orientation of the navigation menu.
    /// </summary>
    public NavigationMenuOrientation Orientation { get; init; }

    /// <summary>
    /// Gets or sets the direction from which the current item was activated.
    /// </summary>
    public ActivationDirection ActivationDirection { get; set; }

    /// <summary>
    /// Gets or sets the current transition animation status.
    /// </summary>
    public TransitionStatus TransitionStatus { get; set; }

    /// <summary>
    /// Gets the auto-generated ID of the popup element.
    /// Used by the trigger for <c>aria-controls</c> and by the popup for its <c>id</c>.
    /// </summary>
    public string PopupId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the auto-generated ID of the viewport element.
    /// Used by the trigger's ownership shim when the viewport is active.
    /// </summary>
    public string ViewportId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the auto-generated ID of the viewport target element.
    /// Used by content to render into the viewport target.
    /// </summary>
    public string ViewportTargetId { get; init; } = string.Empty;

    /// <summary>
    /// Gets whether the viewport should be inert to avoid focus loops.
    /// </summary>
    public bool ViewportInert { get; set; }

    /// <summary>
    /// Gets or sets the text direction used for physical side calculations.
    /// </summary>
    public string Direction { get; init; } = "ltr";

    /// <summary>
    /// Gets the delegate that returns the current active value.
    /// </summary>
    public Func<string?> GetValue { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that returns whether the menu is mounted.
    /// </summary>
    public Func<bool> GetMounted { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that sets the active value asynchronously.
    /// </summary>
    public Func<string?, NavigationMenuCloseReason, Task> SetValueAsync { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that sets a trigger element reference for a specific item value.
    /// </summary>
    public Action<string, ElementReference?> SetTriggerElement { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that gets a trigger element reference for a specific item value.
    /// </summary>
    public Func<string, ElementReference?> GetTriggerElement { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that sets the popup element reference.
    /// </summary>
    public Action<ElementReference?> SetPopupElement { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that sets the positioner element reference.
    /// </summary>
    public Action<ElementReference?> SetPositionerElement { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that sets the viewport element reference.
    /// </summary>
    public Action<ElementReference?> SetViewportElement { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that sets the viewport target element reference.
    /// </summary>
    public Action<ElementReference?> SetViewportTargetElement { get; init; } = _ => { };

    /// <summary>
    /// Gets the delegate that registers an item value in the registration order.
    /// </summary>
    public Action<string> RegisterItem { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that unregisters an item value.
    /// </summary>
    public Action<string> UnregisterItem { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that sets a content element reference for a specific item value.
    /// </summary>
    public Action<string, ElementReference?> SetContentElement { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that unregisters a content element for a specific item value.
    /// </summary>
    public Action<string> DisposeContentElement { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that emits a close event with a reason.
    /// </summary>
    public Action<NavigationMenuCloseReason> EmitClose { get; init; } = _ => { };

    /// <summary>
    /// Gets the delegate that marks whether the viewport should be inert.
    /// </summary>
    public Action<bool> SetViewportInert { get; init; } = _ => { };

    /// <summary>
    /// Gets the delegate that stores the previous trigger element.
    /// </summary>
    public Action<ElementReference?> SetPrevTriggerElement { get; init; } = _ => { };

    /// <summary>
    /// Gets the delegate that returns the previous trigger element.
    /// </summary>
    public Func<ElementReference?> GetPrevTriggerElement { get; init; } = () => null;

    /// <summary>
    /// Gets the delegate that stores the list element.
    /// </summary>
    public Action<ElementReference?> SetListElement { get; init; } = _ => { };

    /// <summary>
    /// Gets the delegate that unmounts the popup immediately.
    /// </summary>
    public Func<Task> UnmountAsync { get; init; } = () => Task.CompletedTask;

    /// <summary>
    /// Gets the delegate that registers content callbacks.
    /// </summary>
    public Action<string, Func<Task>> RegisterContentCallback { get; init; } = (_, _) => { };

    /// <summary>
    /// Gets the delegate that unregisters content callbacks.
    /// </summary>
    public Action<string> UnregisterContentCallback { get; init; } = _ => { };

    /// <summary>
    /// Gets the delegate that emits a link-press close event.
    /// </summary>
    public Func<Task> EmitLinkCloseAsync { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that asks JavaScript to focus the previous tabbable element.
    /// </summary>
    public Action<ElementReference?> RequestFocusPrevious { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that asks JavaScript to focus inside the active navigation menu content.
    /// </summary>
    public Action<ElementReference?, ElementReference?> RequestFocusInside { get; init; } = null!;
}
