using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.NumberField;

/// <summary>
/// Provides shared state and callbacks for child components of the <see cref="NumberFieldScrubArea"/>.
/// Cascaded as a fixed value from the scrub area to its descendants.
/// </summary>
internal sealed class NumberFieldScrubAreaContext
{
    /// <summary>
    /// Gets or sets whether the scrub area is actively being scrubbed.
    /// </summary>
    public bool IsScrubbing { get; set; }

    /// <summary>
    /// Gets or sets whether the current scrub interaction is a touch input.
    /// </summary>
    public bool IsTouchInput { get; set; }

    /// <summary>
    /// Gets or sets whether pointer lock was denied by the browser.
    /// </summary>
    public bool IsPointerLockDenied { get; set; }

    /// <summary>
    /// Gets or sets the cursor movement direction in the scrub area.
    /// </summary>
    public ScrubDirection Direction { get; set; } = ScrubDirection.Horizontal;

    /// <summary>
    /// Gets or sets how many pixels the cursor must move before the value changes.
    /// </summary>
    public int PixelSensitivity { get; set; } = 2;

    /// <summary>
    /// Gets or sets the distance the cursor may move from the center before looping back.
    /// </summary>
    public int? TeleportDistance { get; set; }

    /// <summary>
    /// Sets the <see cref="ElementReference"/> for the custom cursor element.
    /// </summary>
    public Action<ElementReference?> SetCursorElement { get; set; } = null!;

    /// <summary>
    /// Returns the <see cref="ElementReference"/> for the custom cursor element.
    /// </summary>
    public Func<ElementReference?> GetCursorElement { get; set; } = null!;

    /// <summary>
    /// Sets the <see cref="ElementReference"/> for the scrub area element.
    /// </summary>
    public Action<ElementReference?> SetScrubAreaElement { get; set; } = null!;

    /// <summary>
    /// Returns the <see cref="ElementReference"/> for the scrub area element.
    /// </summary>
    public Func<ElementReference?> GetScrubAreaElement { get; set; } = null!;
}
