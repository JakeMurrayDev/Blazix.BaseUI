namespace Blazix.BaseUI.Popover;

/// <summary>
/// Represents the state of the <see cref="PopoverViewport"/> component.
/// </summary>
/// <param name="ActivationDirection">The direction from which the viewport was activated, or <see langword="null"/> if not transitioning.</param>
/// <param name="Transitioning">Indicates whether the viewport is currently transitioning between content.</param>
public readonly record struct PopoverViewportState(string? ActivationDirection, bool Transitioning, PopoverInstantType Instant);
