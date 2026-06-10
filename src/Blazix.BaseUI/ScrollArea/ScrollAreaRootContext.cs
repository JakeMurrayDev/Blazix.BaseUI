namespace Blazix.BaseUI.ScrollArea;

internal sealed class ScrollAreaRootContext
{
    public string RootId { get; set; } = string.Empty;

    public Direction Direction { get; set; } = Direction.Ltr;

    public ScrollAreaRootState State { get; set; } = ScrollAreaRootState.Default;

    public ScrollAreaHiddenState HiddenState { get; set; } = ScrollAreaHiddenState.Default;

    public ScrollAreaSize CornerSize { get; set; } = ScrollAreaSize.Zero;

    public ScrollAreaSize ThumbSize { get; set; } = ScrollAreaSize.Zero;

    public bool HasMeasuredScrollbar { get; set; }

    public bool Hovering { get; set; }

    public bool ScrollingX { get; set; }

    public bool ScrollingY { get; set; }

    public ScrollAreaOverflowEdgeThreshold OverflowEdgeThreshold { get; set; } = ScrollAreaOverflowEdgeThreshold.Zero;
}
