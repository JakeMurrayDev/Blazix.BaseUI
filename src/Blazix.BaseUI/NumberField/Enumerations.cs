namespace Blazix.BaseUI.NumberField;

/// <summary>
/// Specifies the cursor movement direction in the <see cref="NumberFieldScrubArea"/>.
/// </summary>
public enum ScrubDirection
{
    /// <summary>
    /// Scrubbing is detected on horizontal cursor movement.
    /// </summary>
    Horizontal,

    /// <summary>
    /// Scrubbing is detected on vertical cursor movement.
    /// </summary>
    Vertical
}

/// <summary>
/// Indicates what triggered a number field value change.
/// </summary>
public enum NumberFieldChangeReason
{
    /// <summary>
    /// No specific reason.
    /// </summary>
    None,

    /// <summary>
    /// A parseable typing or programmatic text update in the input.
    /// </summary>
    InputChange,

    /// <summary>
    /// The input field was cleared.
    /// </summary>
    InputClear,

    /// <summary>
    /// Formatting or clamping occurred on input blur.
    /// </summary>
    InputBlur,

    /// <summary>
    /// A paste interaction in the input.
    /// </summary>
    InputPaste,

    /// <summary>
    /// A keyboard interaction (arrow keys, Home, End).
    /// </summary>
    Keyboard,

    /// <summary>
    /// The increment button was pressed.
    /// </summary>
    IncrementPress,

    /// <summary>
    /// The decrement button was pressed.
    /// </summary>
    DecrementPress,

    /// <summary>
    /// Wheel-based scrubbing while focused and hovering.
    /// </summary>
    Wheel,

    /// <summary>
    /// Scrub area pointer drag.
    /// </summary>
    Scrub
}
