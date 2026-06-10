using Blazix.BaseUI.Popover;

namespace Blazix.BaseUI.Tooltip;

/// <summary>
/// Represents the state of the <see cref="TooltipPopup"/> component.
/// </summary>
/// <param name="Open">Indicates whether the tooltip is currently open.</param>
/// <param name="Side">The side of the anchor the tooltip is positioned on.</param>
/// <param name="Align">The alignment of the tooltip relative to the anchor.</param>
/// <param name="Instant">The current instant transition type.</param>
/// <param name="TransitionStatus">The current transition status of the popup.</param>
public readonly record struct TooltipPopupState(
    bool Open,
    Side Side,
    Align Align,
    TooltipInstantType Instant,
    TransitionStatus TransitionStatus);
