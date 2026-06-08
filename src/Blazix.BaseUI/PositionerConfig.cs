namespace Blazix.BaseUI;

/// <summary>
/// Encapsulates the common positioning parameters shared by all Positioner components.
/// Maps to the shared parameter interface of React's <c>useAnchorPositioning</c> hook.
/// <para>
/// Used by <see cref="PositionerInterop"/> to build JS interop argument lists
/// without each Positioner manually assembling 13+ positional arguments.
/// </para>
/// </summary>
internal sealed class PositionerConfig
{
    /// <summary>
    /// Gets or sets the side of the anchor element to position against.
    /// </summary>
    public Side Side { get; set; } = Side.Bottom;

    /// <summary>
    /// Gets or sets the alignment relative to the specified side.
    /// </summary>
    public Align Align { get; set; } = Align.Center;

    /// <summary>
    /// Gets or sets the offset in pixels from the side of the anchor element.
    /// </summary>
    public double SideOffset { get; set; }

    /// <summary>
    /// Gets or sets the offset in pixels from the alignment edge.
    /// </summary>
    public double AlignOffset { get; set; }

    /// <summary>
    /// Gets or sets the padding in pixels between the popup and the collision boundary edge.
    /// </summary>
    public double CollisionPadding { get; set; } = 5;

    /// <summary>
    /// Gets or sets the boundary used to detect collisions for repositioning.
    /// </summary>
    public CollisionBoundary CollisionBoundary { get; set; } = CollisionBoundary.ClippingAncestors;

    /// <summary>
    /// Gets or sets the minimum padding in pixels between the arrow and popup edges.
    /// </summary>
    public double ArrowPadding { get; set; } = 5;

    /// <summary>
    /// Gets or sets whether the popup stays attached to its anchor element when it overflows.
    /// </summary>
    public bool Sticky { get; set; }

    /// <summary>
    /// Gets or sets the CSS positioning method (absolute or fixed).
    /// </summary>
    public PositionMethod PositionMethod { get; set; } = PositionMethod.Absolute;

    /// <summary>
    /// Gets or sets whether anchor tracking updates are disabled.
    /// </summary>
    public bool DisableAnchorTracking { get; set; }

    /// <summary>
    /// Gets or sets the collision avoidance configuration.
    /// </summary>
    public CollisionAvoidance? CollisionAvoidance { get; set; }

    /// <summary>
    /// Gets or sets whether to pass collision avoidance as a single object (NavigationMenu/Select)
    /// or as three separate string arguments (Tooltip/PreviewCard/Popover/Menu).
    /// Defaults to <see langword="false"/> (separate strings).
    /// </summary>
    public bool UseCollisionAvoidanceObject { get; set; }

    /// <summary>
    /// Gets the position method string for JS interop.
    /// </summary>
    internal string PositionMethodString => PositionMethod == PositionMethod.Fixed ? "fixed" : "absolute";

    /// <summary>
    /// Appends the collision avoidance arguments to the given list in the format
    /// expected by the component's JS module.
    /// </summary>
    internal void AppendCollisionAvoidanceArgs(List<object?> args)
    {
        if (UseCollisionAvoidanceObject)
        {
            args.Add(new
            {
                side = (CollisionAvoidance?.Side ?? CollisionAvoidanceSideMode.Flip).ToJsString(),
                align = (CollisionAvoidance?.Align ?? CollisionAvoidanceAlignMode.Flip).ToJsString(),
                fallbackAxisSide = (CollisionAvoidance?.FallbackAxisSide ?? CollisionAvoidanceFallbackAxisSide.None).ToJsString()
            });
        }
        else
        {
            args.Add((CollisionAvoidance?.Side ?? CollisionAvoidanceSideMode.Flip).ToJsString());
            args.Add((CollisionAvoidance?.Align ?? CollisionAvoidanceAlignMode.Flip).ToJsString());
            args.Add((CollisionAvoidance?.FallbackAxisSide ?? CollisionAvoidanceFallbackAxisSide.End).ToJsString());
        }
    }
}
