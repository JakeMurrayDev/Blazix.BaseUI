namespace BlazorBaseUI.Toast;

/// <summary>
/// Represents the public state of a <see cref="ToastViewport"/>.
/// </summary>
/// <param name="Expanded">Whether toasts are expanded in the viewport.</param>
public sealed record ToastViewportState(bool Expanded);
