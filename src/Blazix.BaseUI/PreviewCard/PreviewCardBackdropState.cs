namespace Blazix.BaseUI.PreviewCard;

/// <summary>
/// Represents the state of the <see cref="PreviewCardBackdrop"/> component.
/// </summary>
/// <param name="Open">Indicates whether the preview card is currently open.</param>
/// <param name="TransitionStatus">The current transition status of the backdrop.</param>
public readonly record struct PreviewCardBackdropState(bool Open, TransitionStatus TransitionStatus);
