using Blazix.BaseUI.Popover;

namespace Blazix.BaseUI.PreviewCard;

/// <summary>
/// Represents the state of the <see cref="PreviewCardPositioner"/> component.
/// </summary>
/// <param name="Open">Indicates whether the preview card is currently open.</param>
/// <param name="Side">The side of the anchor the preview card is positioned on.</param>
/// <param name="Align">The alignment of the preview card relative to the anchor.</param>
/// <param name="AnchorHidden">Indicates whether the anchor element is hidden from view.</param>
/// <param name="Instant">The current instant transition type.</param>
public readonly record struct PreviewCardPositionerState(
    bool Open,
    Side Side,
    Align Align,
    bool AnchorHidden,
    PreviewCardInstantType Instant);
