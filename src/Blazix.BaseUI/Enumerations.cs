namespace Blazix.BaseUI;

/// <summary>
/// Specifies the visual orientation of a component.
/// </summary>
public enum Orientation
{
    /// <summary>The orientation has not been set.</summary>
    Undefined,

    /// <summary>Horizontal orientation.</summary>
    Horizontal,

    /// <summary>Vertical orientation.</summary>
    Vertical
}

/// <summary>
/// Specifies the text reading direction.
/// </summary>
public enum Direction
{
    /// <summary>The direction has not been set.</summary>
    Undefined,

    /// <summary>Left-to-right.</summary>
    Ltr,

    /// <summary>Right-to-left.</summary>
    Rtl
}

/// <summary>
/// Describes the current phase of a CSS transition animation.
/// </summary>
public enum TransitionStatus
{
    /// <summary>No transition is active.</summary>
    Undefined,

    /// <summary>The element is animating in (opening).</summary>
    Starting,

    /// <summary>The element is animating out (closing).</summary>
    Ending,

    /// <summary>The transition has completed.</summary>
    Idle
}

/// <summary>
/// Specifies which side of the anchor element to align the popup against.
/// </summary>
public enum Side
{
    /// <summary>The top side of the anchor element.</summary>
    Top,

    /// <summary>The bottom side of the anchor element.</summary>
    Bottom,

    /// <summary>The left side of the anchor element.</summary>
    Left,

    /// <summary>The right side of the anchor element.</summary>
    Right,

    /// <summary>The inline-end side of the anchor element.</summary>
    InlineEnd,

    /// <summary>The inline-start side of the anchor element.</summary>
    InlineStart,

    /// <summary>No logical side — used when the popup overlaps the anchor (e.g. Select align-item-with-trigger).</summary>
    None
}

/// <summary>
/// Specifies how to align the popup relative to the specified side.
/// </summary>
public enum Align
{
    /// <summary>Aligns the popup to the start of the anchor.</summary>
    Start,

    /// <summary>Aligns the popup to the center of the anchor.</summary>
    Center,

    /// <summary>Aligns the popup to the end of the anchor.</summary>
    End
}

/// <summary>
/// Specifies which CSS <c>position</c> property to use for the positioner.
/// </summary>
public enum PositionMethod
{
    /// <summary>Uses CSS <c>position: absolute</c>.</summary>
    Absolute,

    /// <summary>Uses CSS <c>position: fixed</c>.</summary>
    Fixed
}

/// <summary>
/// Specifies the collision boundary used to determine repositioning behavior.
/// </summary>
public enum CollisionBoundary
{
    /// <summary>Uses the viewport as the collision boundary.</summary>
    Viewport,

    /// <summary>Uses the clipping ancestors as the collision boundary.</summary>
    ClippingAncestors
}

/// <summary>
/// Specifies how side collisions are handled when positioning the popup.
/// </summary>
public enum CollisionAvoidanceSideMode
{
    /// <summary>No collision avoidance on the side axis.</summary>
    None,

    /// <summary>Flips to the opposite side when a collision is detected.</summary>
    Flip,

    /// <summary>Shifts along the side axis to avoid the collision.</summary>
    Shift
}

/// <summary>
/// Specifies how alignment collisions are handled when positioning the popup.
/// </summary>
public enum CollisionAvoidanceAlignMode
{
    /// <summary>No collision avoidance on the alignment axis.</summary>
    None,

    /// <summary>Flips the alignment when a collision is detected.</summary>
    Flip,

    /// <summary>Shifts along the alignment axis to avoid the collision.</summary>
    Shift
}

/// <summary>
/// Specifies the fallback axis side when the popup cannot avoid a collision on both axes.
/// </summary>
public enum CollisionAvoidanceFallbackAxisSide
{
    /// <summary>No fallback axis side.</summary>
    None,

    /// <summary>Falls back to the start of the axis.</summary>
    Start,

    /// <summary>Falls back to the end of the axis.</summary>
    End
}

/// <summary>
/// Determines how to handle collisions when positioning the popup.
/// </summary>
public sealed class CollisionAvoidance
{
    /// <summary>
    /// Gets or sets how side collisions are handled.
    /// When <see cref="CollisionAvoidanceSideMode.Flip"/>, the popup flips to the opposite side.
    /// When <see cref="CollisionAvoidanceSideMode.Shift"/>, the popup shifts along the side axis.
    /// </summary>
    public CollisionAvoidanceSideMode Side { get; set; } = CollisionAvoidanceSideMode.Flip;

    /// <summary>
    /// Gets or sets how alignment collisions are handled.
    /// When <see cref="CollisionAvoidanceAlignMode.Flip"/>, the alignment flips.
    /// When <see cref="CollisionAvoidanceAlignMode.Shift"/>, the popup shifts along the alignment axis.
    /// </summary>
    public CollisionAvoidanceAlignMode Align { get; set; } = CollisionAvoidanceAlignMode.Flip;

    /// <summary>
    /// Gets or sets the fallback axis side used when collisions cannot be fully avoided.
    /// </summary>
    public CollisionAvoidanceFallbackAxisSide FallbackAxisSide { get; set; } = CollisionAvoidanceFallbackAxisSide.End;
}

/// <summary>
/// Specifies how the user interacted to open or close a component.
/// </summary>
public enum InteractionType
{
    /// <summary>No interaction type.</summary>
    None,

    /// <summary>Mouse click.</summary>
    Click,

    /// <summary>Mouse pointer.</summary>
    Mouse,

    /// <summary>Keyboard.</summary>
    Keyboard,

    /// <summary>Touch.</summary>
    Touch,

    /// <summary>Pen / stylus.</summary>
    Pen
}

/// <summary>
/// Specifies the direction from which an item was activated,
/// used to drive enter/exit animations.
/// </summary>
public enum ActivationDirection
{
    /// <summary>No activation direction.</summary>
    None,

    /// <summary>Activated from the left.</summary>
    Left,

    /// <summary>Activated from the right.</summary>
    Right,

    /// <summary>Activated from above.</summary>
    Up,

    /// <summary>Activated from below.</summary>
    Down
}
