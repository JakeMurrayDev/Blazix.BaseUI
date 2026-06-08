namespace Blazix.BaseUI.Dialog;

/// <summary>
/// Represents the state of the <see cref="DialogBackdrop"/> component.
/// </summary>
/// <param name="Open">Indicates whether the dialog is currently open.</param>
/// <param name="TransitionStatus">The current transition status of the dialog.</param>
public readonly record struct DialogBackdropState(bool Open, TransitionStatus TransitionStatus);
