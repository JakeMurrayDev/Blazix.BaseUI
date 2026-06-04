namespace BlazorBaseUI.Toast;

/// <summary>
/// Represents the public state of a <see cref="ToastArrow"/>.
/// </summary>
/// <param name="Side">The side of the anchor the toast is placed on.</param>
/// <param name="Align">The alignment of the toast relative to the anchor.</param>
/// <param name="Uncentered">Whether the arrow cannot be centered on the anchor.</param>
public sealed record ToastArrowState(Side Side, Align Align, bool Uncentered);
