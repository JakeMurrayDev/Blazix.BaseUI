using Blazix.BaseUI.Popover;

namespace Blazix.BaseUI.PreviewCard;

/// <summary>
/// Represents the state of the <see cref="PreviewCardPopup"/> component.
/// </summary>
/// <param name="Open">Indicates whether the preview card is currently open.</param>
/// <param name="Side">The side of the anchor the preview card is positioned on.</param>
/// <param name="Align">The alignment of the preview card relative to the anchor.</param>
/// <param name="Instant">The current instant transition type.</param>
/// <param name="TransitionStatus">The current transition status of the popup.</param>
public readonly record struct PreviewCardPopupState(
    bool Open,
    Side Side,
    Align Align,
    PreviewCardInstantType Instant,
    TransitionStatus TransitionStatus);
