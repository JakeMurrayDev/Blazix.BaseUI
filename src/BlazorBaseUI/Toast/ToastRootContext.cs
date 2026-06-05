namespace BlazorBaseUI.Toast;

internal sealed class ToastRootContext
{
    public ToastObject Toast { get; set; } = null!;

    public string? TitleId { get; set; }

    public string? DescriptionId { get; set; }

    public bool Swiping { get; set; }

    public ToastSwipeDirection? SwipeDirection { get; set; }

    public int Index { get; set; }

    public int VisibleIndex { get; set; }

    public bool Expanded { get; set; }

    public Action<string?> SetTitleId { get; set; } = _ => { };

    public Action<string?> SetDescriptionId { get; set; } = _ => { };

    public Action<bool, ToastSwipeDirection?> SetSwipeState { get; set; } = (_, _) => { };

    public Action<double, bool> RecalculateHeight { get; set; } = (_, _) => { };
}
