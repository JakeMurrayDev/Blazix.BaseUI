using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Dialog;

/// <summary>
/// Provides the cascading context for dialog and alert dialog root components.
/// </summary>
internal sealed class DialogRootContext
{
    public string RootId { get; set; } = string.Empty;

    public bool Nested { get; set; }

    public bool Open { get; set; }

    public bool Mounted { get; set; }

    public DialogModalMode Modal { get; set; }

    public DialogRole Role { get; set; }

    public bool DisablePointerDismissal { get; set; }

    public int NestedDialogCount { get; set; }

    public DialogOpenChangeReason DialogOpenChangeReason { get; set; }

    public string? InteractionType { get; set; }

    public TransitionStatus TransitionStatus { get; set; }

    public string? TitleId { get; set; }

    public string? DescriptionId { get; set; }

    public string? PopupId { get; set; }

    public Action<string?> SetPopupId { get; set; } = null!;

    public string? ActiveTriggerId { get; set; }

    public bool PreventUnmountingOnClose { get; set; }

    public object? Payload { get; set; }

    public Func<bool> GetOpen { get; set; } = null!;

    public Func<bool> GetMounted { get; set; } = null!;

    public Func<object?> GetPayload { get; set; } = null!;

    public Func<ElementReference?> GetTriggerElement { get; set; } = null!;

    public Func<ElementReference?> GetPopupElement { get; set; } = null!;

    public Action<string?> SetTitleId { get; set; } = null!;

    public Action<string?> SetDescriptionId { get; set; } = null!;

    public Action<string, ElementReference?> RegisterTriggerElement { get; set; } = null!;

    public Action<string> UnregisterTriggerElement { get; set; } = null!;

    public ElementReference? BackdropElement { get; set; }
    public Action<ElementReference?> SetBackdropElement { get; set; } = null!;

    public ElementReference? ViewportElement { get; set; }
    public Action<ElementReference?> SetViewportElement { get; set; } = null!;

    public Action<ElementReference?> SetPopupElement { get; set; } = null!;

    public Action<string?> SetInteractionType { get; set; } = null!;

    /// <summary>
    /// Re-resolves the popup's <c>finalFocus</c> target at close time using the close interaction type.
    /// Registered by <see cref="DialogPopup"/> only when <c>FinalFocus</c> is a callback, since the
    /// callback result can depend on how the dialog was closed (mouse/keyboard/touch/pen). Mirrors
    /// React, which evaluates the <c>finalFocus</c> function at close with the close interaction type
    /// rather than eagerly at open. <see langword="null"/> when no close-time re-resolution is needed.
    /// </summary>
    public Func<string?, (string? Mode, ElementReference? Element)>? ResolveFinalFocusForClose { get; set; }

    /// <summary>
    /// Resolves the popup's open-time <c>finalFocus</c> target. Registered by <see cref="DialogPopup"/>
    /// only when <c>FinalFocus</c> is a callback, so the root can re-send the open-time target on every
    /// open. This refreshes the shared JS state for kept-mounted popups (which do not re-initialize on
    /// reopen) after a close has overwritten it with the close-resolved target. <see langword="null"/>
    /// when no re-send is needed.
    /// </summary>
    public Func<(string? Mode, ElementReference? Element)>? ResolveFinalFocusForOpen { get; set; }

    public Func<bool, DialogOpenChangeReason, Task> SetOpenAsync { get; set; } = null!;

    public Func<object?, DialogOpenChangeReason, Task> SetOpenWithPayloadAsync { get; set; } = null!;

    public Func<string?, object?, DialogOpenChangeReason, Task> SetOpenWithTriggerIdAsync { get; set; } = null!;

    public Action<string, object?> SetTriggerPayload { get; set; } = null!;

    public Action Close { get; set; } = null!;

    public Action ForceUnmount { get; set; } = null!;

    public IFloatingRootContext? FloatingRootContext { get; set; }

    public Action? OnNestedDialogOpen { get; set; }

    public Action? OnNestedDialogClose { get; set; }
}
