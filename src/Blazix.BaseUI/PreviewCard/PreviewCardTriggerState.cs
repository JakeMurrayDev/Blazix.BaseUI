namespace Blazix.BaseUI.PreviewCard;

/// <summary>
/// Represents the state of the <see cref="PreviewCardTrigger"/> component.
/// </summary>
/// <param name="Open">Indicates whether the preview card is currently open for this trigger.</param>
public readonly record struct PreviewCardTriggerState(bool Open);
