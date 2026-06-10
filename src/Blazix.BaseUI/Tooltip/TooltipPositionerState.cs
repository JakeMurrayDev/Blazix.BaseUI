using Blazix.BaseUI.Popover;

namespace Blazix.BaseUI.Tooltip;

/// <summary>
/// Represents the state of the <see cref="TooltipPositioner"/> component.
/// </summary>
/// <param name="Open">Indicates whether the tooltip is currently open.</param>
/// <param name="Side">The side of the anchor the tooltip is positioned on.</param>
/// <param name="Align">The alignment of the tooltip relative to the anchor.</param>
/// <param name="AnchorHidden">Indicates whether the anchor element is hidden from view.</param>
/// <param name="Instant">The current instant transition type.</param>
public readonly record struct TooltipPositionerState(
    bool Open,
    Side Side,
    Align Align,
    bool AnchorHidden,
    TooltipInstantType Instant);
