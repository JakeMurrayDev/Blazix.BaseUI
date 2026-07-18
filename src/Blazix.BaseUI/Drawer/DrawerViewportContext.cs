namespace Blazix.BaseUI.Drawer;

internal sealed class DrawerViewportContext
{
    public bool Swiping { get; set; }

    public double? SwipeStrength { get; set; }

    public double SnapPointOffset { get; set; }

    public Func<string> GetDragStyles { get; set; } = () => string.Empty;

    public Action<bool> SetSwipeDismissed { get; set; } = _ => { };

    public event Action? StateChanged;

    public void NotifyStateChanged() => StateChanged?.Invoke();
}
