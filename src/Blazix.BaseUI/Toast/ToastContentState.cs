namespace Blazix.BaseUI.Toast;

/// <summary>
/// Represents the public state of a <see cref="ToastContent"/>.
/// </summary>
/// <param name="Expanded">Whether the toast viewport is expanded.</param>
/// <param name="Behind">Whether the toast is behind the frontmost toast.</param>
public sealed record ToastContentState(bool Expanded, bool Behind);
