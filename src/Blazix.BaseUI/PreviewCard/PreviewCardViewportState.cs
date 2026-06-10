namespace Blazix.BaseUI.PreviewCard;

/// <summary>
/// Represents the state of the <see cref="PreviewCardViewport"/> component.
/// </summary>
/// <param name="ActivationDirection">The direction of the active trigger change transition.</param>
/// <param name="Transitioning">Indicates whether viewport content is transitioning.</param>
/// <param name="Instant">The current instant transition type.</param>
public readonly record struct PreviewCardViewportState(string? ActivationDirection, bool Transitioning, PreviewCardInstantType Instant);
