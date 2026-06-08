using Blazix.BaseUI.Popover;

namespace Blazix.BaseUI.Tooltip;

/// <summary>
/// Represents the state of the <see cref="TooltipArrow"/> component.
/// </summary>
/// <param name="Open">Indicates whether the tooltip is currently open.</param>
/// <param name="Side">The side of the anchor the tooltip is positioned on.</param>
/// <param name="Align">The alignment of the tooltip relative to the anchor.</param>
/// <param name="Uncentered">Indicates whether the arrow is uncentered relative to the popup.</param>
/// <param name="Instant">The current instant transition type.</param>
public readonly record struct TooltipArrowState(
    bool Open,
    Side Side,
    Align Align,
    bool Uncentered,
    TooltipInstantType Instant);
