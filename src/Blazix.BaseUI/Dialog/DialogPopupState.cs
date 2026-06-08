namespace Blazix.BaseUI.Dialog;

/// <summary>
/// Represents the state of the <see cref="DialogPopup"/> component.
/// </summary>
/// <param name="Open">Indicates whether the dialog is currently open.</param>
/// <param name="TransitionStatus">The current transition status of the dialog.</param>
/// <param name="Nested">Indicates whether this dialog is nested within another dialog.</param>
/// <param name="NestedDialogOpen">Indicates whether a nested dialog within this dialog is currently open.</param>
public readonly record struct DialogPopupState(bool Open, TransitionStatus TransitionStatus, bool Nested, bool NestedDialogOpen);
