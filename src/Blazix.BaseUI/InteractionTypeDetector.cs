using Microsoft.AspNetCore.Components.Web;

namespace Blazix.BaseUI;

/// <summary>
/// Detects interaction type (mouse, touch, pen, keyboard) from browser events.
/// Mirrors React Base UI's useEnhancedClickHandler / useOpenInteractionType.
/// </summary>
public static class InteractionTypeDetector
{
    /// <summary>
    /// Records the pointer type from a pointerdown event.
    /// Call from the component's pointerdown handler, store the result.
    /// </summary>
    public static string FromPointerEvent(PointerEventArgs e)
        => e.PointerType ?? string.Empty;

    /// <summary>
    /// Determines interaction type from a click event, using the previously
    /// stored pointer type as fallback.
    /// Detail == 0 means keyboard-triggered click (no physical click).
    /// </summary>
    public static string? FromClickEvent(MouseEventArgs e, string? lastPointerType)
        => e.Detail == 0 ? "keyboard" : lastPointerType;
}
