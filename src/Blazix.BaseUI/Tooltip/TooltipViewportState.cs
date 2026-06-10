namespace Blazix.BaseUI.Tooltip;

/// <summary>
/// Represents the state of the <see cref="TooltipViewport"/> component.
/// </summary>
/// <param name="Instant">The current instant transition type.</param>
/// <param name="ActivationDirection">The direction of the activation transition, or <see langword="null"/> if not transitioning between triggers.</param>
/// <param name="Transitioning">Whether a viewport transition between triggers is in progress.</param>
public readonly record struct TooltipViewportState(
    TooltipInstantType Instant,
    string? ActivationDirection,
    bool Transitioning);
