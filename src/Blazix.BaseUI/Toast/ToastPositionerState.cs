namespace Blazix.BaseUI.Toast;

/// <summary>
/// Represents the public state of a <see cref="ToastPositioner"/>.
/// </summary>
/// <param name="Side">The side of the anchor the toast is placed on.</param>
/// <param name="Align">The alignment of the toast relative to the anchor.</param>
/// <param name="AnchorHidden">Whether the anchor element is hidden.</param>
public sealed record ToastPositionerState(Side Side, Align Align, bool AnchorHidden);
