namespace BlazorBaseUI.Popover;

/// <summary>
/// Represents the state of the <see cref="PopoverPositioner"/> component.
/// </summary>
/// <param name="Open">Indicates whether the popover is currently open.</param>
/// <param name="Side">The side of the anchor element the popover is positioned against.</param>
/// <param name="Align">The alignment of the popover relative to the specified side.</param>
/// <param name="AnchorHidden">Indicates whether the anchor element is hidden from view.</param>
/// <param name="Instant">Indicates whether CSS transitions should be instant, and the reason why.</param>
public readonly record struct PopoverPositionerState(bool Open, Side Side, Align Align, bool AnchorHidden, PopoverInstantType Instant);
