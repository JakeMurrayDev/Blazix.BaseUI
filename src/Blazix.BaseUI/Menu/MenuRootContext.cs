using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Menu;

/// <summary>
/// Provides shared state and callbacks for the <see cref="MenuRoot"/> and its descendant components.
/// </summary>
internal sealed class MenuRootContext
{
    /// <summary>
    /// Gets the unique identifier for this menu root instance.
    /// </summary>
    public string RootId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the menu is open.
    /// </summary>
    public bool Open { get; set; }

    /// <summary>
    /// Gets or sets whether the menu is mounted in the DOM.
    /// </summary>
    public bool Mounted { get; set; }

    /// <summary>
    /// Gets or sets whether the menu is disabled.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets the type of the menu's parent container.
    /// </summary>
    public MenuParentType ParentType { get; set; }

    /// <summary>
    /// Gets or sets the visual orientation of the menu.
    /// </summary>
    public MenuOrientation Orientation { get; set; }

    /// <summary>
    /// Gets or sets whether moving the pointer over items highlights them.
    /// </summary>
    public bool HighlightItemOnHover { get; set; }

    /// <summary>
    /// Gets or sets the reason the menu's open state last changed.
    /// </summary>
    public MenuOpenChangeReason MenuOpenChangeReason { get; set; }

    /// <summary>
    /// Gets or sets the current transition animation status.
    /// </summary>
    public TransitionStatus TransitionStatus { get; set; }

    /// <summary>
    /// Gets or sets the type of instant transition to apply.
    /// </summary>
    public MenuInstantType MenuInstantType { get; set; }

    /// <summary>
    /// Gets or sets the index of the currently active (focused) item.
    /// </summary>
    public int? ActiveIndex { get; set; }

    /// <summary>
    /// Gets the delegate that returns the current open state.
    /// </summary>
    public Func<bool> GetOpen { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that returns whether the menu is mounted.
    /// </summary>
    public Func<bool> GetMounted { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that returns the trigger element reference.
    /// </summary>
    public Func<ElementReference?> GetTriggerElement { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that sets the trigger element reference.
    /// </summary>
    public Action<ElementReference?> SetTriggerElement { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that sets the positioner element reference.
    /// </summary>
    public Action<ElementReference?> SetPositionerElement { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that sets the popup element reference.
    /// </summary>
    public Action<ElementReference?> SetPopupElement { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that sets the active item index.
    /// </summary>
    public Action<int?> SetActiveIndex { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that sets the open state asynchronously with a reason and optional payload.
    /// </summary>
    public Func<bool, MenuOpenChangeReason, object?, Task> SetOpenAsync { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that sets the open state from a specific trigger.
    /// </summary>
    public Func<bool, MenuOpenChangeReason, object?, string?, string?, bool, Task> SetOpenFromTriggerAsync { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that registers a trigger with this root.
    /// </summary>
    public Action<string, ElementReference?, object?> RegisterTrigger { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that unregisters a trigger from this root.
    /// </summary>
    public Action<string> UnregisterTrigger { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that updates a trigger element registered with this root.
    /// </summary>
    public Action<string, ElementReference?> UpdateTriggerElement { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that updates a trigger payload registered with this root.
    /// </summary>
    public Action<string, object?> UpdateTriggerPayload { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that emits a close event with a reason and optional payload.
    /// </summary>
    public Action<MenuOpenChangeReason, object?> EmitClose { get; init; } = null!;

    /// <summary>
    /// Gets the text direction of the menu.
    /// </summary>
    public string? Direction { get; set; }

    /// <summary>
    /// Gets or sets the ID of the active trigger element.
    /// </summary>
    public string? TriggerId { get; set; }

    /// <summary>
    /// Gets the delegate that returns the popup element reference.
    /// </summary>
    public Func<ElementReference?>? GetPopupElement { get; init; }

    /// <summary>
    /// Gets the unique identifier for the popup element.
    /// </summary>
    public string? PopupId { get; set; }

    /// <summary>
    /// Gets or sets the element to return focus to when the menu closes.
    /// </summary>
    public FocusTarget? FinalFocus { get; set; }

    /// <summary>
    /// Gets or sets the resolved final focus mode for JS interop.
    /// </summary>
    public string? FinalFocusMode { get; set; }

    /// <summary>
    /// Gets or sets the resolved final focus element for JS interop.
    /// </summary>
    public ElementReference? FinalFocusElement { get; set; }

    /// <summary>
    /// Gets or sets whether a viewport component is present.
    /// </summary>
    public bool HasViewport { get; set; }

    /// <summary>
    /// Gets the delegate that sets whether a viewport component is present.
    /// </summary>
    public Action<bool>? SetHasViewport { get; init; }

    /// <summary>
    /// Gets the delegate that sets the instant type for transitions.
    /// </summary>
    public Action<MenuInstantType>? SetMenuInstantType { get; init; }

    /// <summary>
    /// Gets or sets the interaction type that opened the menu (e.g., "mouse", "touch", "keyboard").
    /// </summary>
    public string? InteractionType { get; set; }

    /// <summary>
    /// Gets the delegate that sets the interaction type.
    /// </summary>
    public Action<string?>? SetInteractionType { get; init; }

    /// <summary>
    /// Gets or sets whether the menu popup is inside a toolbar.
    /// When true, composite keys (arrows, Home, End) are prevented from propagating to the toolbar.
    /// </summary>
    public bool InsideToolbar { get; set; }

    /// <summary>
    /// Gets or sets the floating root context adapter for <see cref="FloatingFocusManager.FloatingFocusManager"/>.
    /// </summary>
    public IFloatingRootContext? FloatingRootContext { get; set; }
}
