namespace Blazix.BaseUI.Popover;

/// <summary>
/// Represents the state of the <see cref="PopoverBackdrop"/> component.
/// </summary>
/// <param name="Open">Indicates whether the popover is currently open.</param>
/// <param name="TransitionStatus">The current transition status of the backdrop.</param>
public readonly record struct PopoverBackdropState(bool Open, TransitionStatus TransitionStatus);
