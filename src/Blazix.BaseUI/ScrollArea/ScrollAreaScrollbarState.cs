namespace Blazix.BaseUI.ScrollArea;

/// <summary>
/// Represents the state of the <see cref="ScrollAreaScrollbar"/> component.
/// </summary>
/// <param name="Hovering">Whether the pointer is over the scroll area.</param>
/// <param name="Scrolling">Whether this scrollbar's axis is being scrolled.</param>
/// <param name="Orientation">The scrollbar orientation.</param>
/// <param name="HasOverflowX">Whether horizontal overflow is present.</param>
/// <param name="HasOverflowY">Whether vertical overflow is present.</param>
/// <param name="OverflowXStart">Whether there is overflow on the horizontal start side.</param>
/// <param name="OverflowXEnd">Whether there is overflow on the horizontal end side.</param>
/// <param name="OverflowYStart">Whether there is overflow on the vertical start side.</param>
/// <param name="OverflowYEnd">Whether there is overflow on the vertical end side.</param>
/// <param name="CornerHidden">Whether the scrollbar corner is hidden.</param>
public readonly record struct ScrollAreaScrollbarState(
    bool Hovering,
    bool Scrolling,
    Orientation Orientation,
    bool HasOverflowX,
    bool HasOverflowY,
    bool OverflowXStart,
    bool OverflowXEnd,
    bool OverflowYStart,
    bool OverflowYEnd,
    bool CornerHidden);
