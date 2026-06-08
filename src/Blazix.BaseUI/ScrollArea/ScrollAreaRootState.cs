namespace Blazix.BaseUI.ScrollArea;

/// <summary>
/// Represents the state of the <see cref="ScrollAreaRoot"/> component.
/// </summary>
/// <param name="Scrolling">Whether the scroll area is being scrolled.</param>
/// <param name="HasOverflowX">Whether horizontal overflow is present.</param>
/// <param name="HasOverflowY">Whether vertical overflow is present.</param>
/// <param name="OverflowXStart">Whether there is overflow on the horizontal start side.</param>
/// <param name="OverflowXEnd">Whether there is overflow on the horizontal end side.</param>
/// <param name="OverflowYStart">Whether there is overflow on the vertical start side.</param>
/// <param name="OverflowYEnd">Whether there is overflow on the vertical end side.</param>
/// <param name="CornerHidden">Whether the scrollbar corner is hidden.</param>
public readonly record struct ScrollAreaRootState(
    bool Scrolling,
    bool HasOverflowX,
    bool HasOverflowY,
    bool OverflowXStart,
    bool OverflowXEnd,
    bool OverflowYStart,
    bool OverflowYEnd,
    bool CornerHidden)
{
    internal static ScrollAreaRootState Default { get; } = new(
        Scrolling: false,
        HasOverflowX: false,
        HasOverflowY: false,
        OverflowXStart: false,
        OverflowXEnd: false,
        OverflowYStart: false,
        OverflowYEnd: false,
        CornerHidden: true);
}
