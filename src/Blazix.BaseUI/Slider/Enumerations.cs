namespace Blazix.BaseUI.Slider;

/// <summary>
/// Controls how thumbs behave when they collide during pointer interactions.
/// </summary>
public enum ThumbCollisionBehavior
{
    /// <summary>
    /// Thumbs push each other without restoring their previous positions when dragged back.
    /// </summary>
    Push,

    /// <summary>
    /// Thumbs swap places when dragged past each other.
    /// </summary>
    Swap,

    /// <summary>
    /// Thumbs cannot move past each other; excess movement is ignored.
    /// </summary>
    None
}

/// <summary>
/// Specifies how the thumb(s) are aligned relative to the control when the value is at the minimum or maximum.
/// </summary>
public enum ThumbAlignment
{
    /// <summary>
    /// The center of the thumb is aligned with the control edge.
    /// </summary>
    Center,

    /// <summary>
    /// The thumb is inset within the control such that its edge is aligned with the control edge.
    /// </summary>
    Edge,

    /// <summary>
    /// The thumb is inset within the control, but without pre-hydration positioning attributes.
    /// </summary>
    EdgeClientOnly
}

/// <summary>
/// Indicates what triggered a slider value change.
/// </summary>
public enum SliderChangeReason
{
    /// <summary>
    /// The change was triggered without a specific interaction.
    /// </summary>
    None,

    /// <summary>
    /// The hidden range input emitted a change event (e.g., via form integration).
    /// </summary>
    InputChange,

    /// <summary>
    /// The control track was pressed.
    /// </summary>
    TrackPress,

    /// <summary>
    /// A thumb was dragged.
    /// </summary>
    Drag,

    /// <summary>
    /// A keyboard key was pressed.
    /// </summary>
    Keyboard
}
