namespace Blazix.BaseUI.Drawer;

internal sealed class DrawerProviderContext
{
    private readonly Dictionary<string, bool> openById = new();

    public bool Active { get; private set; }

    public double SwipeProgress { get; private set; }

    public int FrontmostHeight { get; private set; }

    public event Action? StateChanged;

    public void SetDrawerOpen(string drawerId, bool open)
    {
        if (openById.TryGetValue(drawerId, out var previous) && previous == open)
        {
            return;
        }

        openById[drawerId] = open;
        UpdateActive();
    }

    public void RemoveDrawer(string drawerId)
    {
        if (!openById.Remove(drawerId))
        {
            return;
        }

        UpdateActive();
    }

    public void SetVisualState(double? swipeProgress = null, int? frontmostHeight = null)
    {
        var nextSwipeProgress = swipeProgress.HasValue && double.IsFinite(swipeProgress.Value)
            ? swipeProgress.Value
            : SwipeProgress;
        var nextFrontmostHeight = frontmostHeight.HasValue && frontmostHeight.Value > 0
            ? frontmostHeight.Value
            : frontmostHeight.HasValue ? 0 : FrontmostHeight;

        if (Math.Abs(nextSwipeProgress - SwipeProgress) < double.Epsilon && nextFrontmostHeight == FrontmostHeight)
        {
            return;
        }

        SwipeProgress = nextSwipeProgress;
        FrontmostHeight = nextFrontmostHeight;
        StateChanged?.Invoke();
    }

    private void UpdateActive()
    {
        var nextActive = openById.Values.Any(open => open);
        if (nextActive == Active)
        {
            return;
        }

        Active = nextActive;
        StateChanged?.Invoke();
    }
}
