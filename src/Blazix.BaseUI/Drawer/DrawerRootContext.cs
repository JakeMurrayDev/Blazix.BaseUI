namespace Blazix.BaseUI.Drawer;

internal sealed class DrawerRootContext
{
    public string RootId { get; set; } = string.Empty;

    public string? ParentRootId { get; set; }

    public DrawerSwipeDirection SwipeDirection { get; set; } = DrawerSwipeDirection.Down;

    public bool SnapToSequentialPoints { get; set; }

    public IReadOnlyList<DrawerSnapPoint>? SnapPoints { get; set; }

    public DrawerSnapPoint? ActiveSnapPoint { get; set; }

    public int FrontmostHeight { get; set; }

    public int PopupHeight { get; set; }

    public bool HasNestedDrawer { get; set; }

    public int NestedDrawerCount { get; set; }

    public bool NestedSwiping { get; set; }

    public double NestedSwipeProgress { get; set; }

    public Func<DrawerSnapPoint?, DrawerOpenChangeReason, Task> SetActiveSnapPointAsync { get; set; } = null!;

    public Action<int> OnPopupHeightChange { get; set; } = null!;

    public Action<bool> OnNestedDrawerPresenceChange { get; set; } = null!;

    public Action<int> OnNestedFrontmostHeightChange { get; set; } = null!;

    public Action<bool> OnNestedSwipingChange { get; set; } = null!;

    public Action<double> OnNestedSwipeProgressChange { get; set; } = null!;

    public Action<int>? NotifyParentFrontmostHeight { get; set; }

    public Action<bool>? NotifyParentSwipingChange { get; set; }

    public Action<double>? NotifyParentSwipeProgressChange { get; set; }

    public Action<bool>? NotifyParentHasNestedDrawer { get; set; }

    public event Action? StateChanged;

    public void NotifyStateChanged() => StateChanged?.Invoke();
}
