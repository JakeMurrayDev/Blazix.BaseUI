namespace BlazorBaseUI.Toast;

/// <summary>
/// Represents the public state of a <see cref="ToastRoot"/>.
/// </summary>
public sealed record ToastRootState(
    TransitionStatus TransitionStatus,
    bool Expanded,
    bool Limited,
    string? Type,
    bool Swiping,
    ToastSwipeDirection? SwipeDirection);
