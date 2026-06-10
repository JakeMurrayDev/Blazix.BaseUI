namespace Blazix.BaseUI.Popover;

/// <summary>
/// Represents the state of the <see cref="PopoverPopup"/> component.
/// </summary>
/// <param name="Open">Indicates whether the popover is currently open.</param>
/// <param name="Side">The side of the anchor element the popover is positioned against.</param>
/// <param name="Align">The alignment of the popover relative to the specified side.</param>
/// <param name="Instant">The type of instant transition currently in effect.</param>
/// <param name="TransitionStatus">The current transition status of the popup.</param>
public readonly record struct PopoverPopupState(bool Open, Side Side, Align Align, PopoverInstantType Instant, TransitionStatus TransitionStatus);
