namespace BlazorBaseUI.Toast;

/// <summary>
/// Specifies how assertively a toast is announced.
/// </summary>
public enum ToastPriority
{
    /// <summary>The toast is announced politely.</summary>
    Low,

    /// <summary>The toast is announced urgently.</summary>
    High
}

/// <summary>
/// Specifies a pointer swipe direction that can dismiss a toast.
/// </summary>
public enum ToastSwipeDirection
{
    /// <summary>Swipe upward.</summary>
    Up,

    /// <summary>Swipe downward.</summary>
    Down,

    /// <summary>Swipe left.</summary>
    Left,

    /// <summary>Swipe right.</summary>
    Right
}
