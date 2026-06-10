namespace Blazix.BaseUI.Dialog;

/// <summary>
/// Represents the state of the <see cref="DialogRoot"/> component.
/// </summary>
/// <param name="Open">Indicates whether the dialog is currently open.</param>
public readonly record struct DialogRootState(bool Open);
