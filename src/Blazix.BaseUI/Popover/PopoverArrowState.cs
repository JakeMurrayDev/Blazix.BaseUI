namespace Blazix.BaseUI.Popover;

/// <summary>
/// Represents the state of the <see cref="PopoverArrow"/> component.
/// </summary>
/// <param name="Open">Indicates whether the popover is currently open.</param>
/// <param name="Side">The side of the anchor element the popover is positioned against.</param>
/// <param name="Align">The alignment of the popover relative to the specified side.</param>
/// <param name="Uncentered">Indicates whether the arrow is not centered relative to the popup.</param>
public readonly record struct PopoverArrowState(bool Open, Side Side, Align Align, bool Uncentered);
