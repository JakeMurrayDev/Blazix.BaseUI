namespace Blazix.BaseUI.Dialog;

/// <summary>
/// Represents the state of the <see cref="DialogClose"/> component.
/// </summary>
/// <param name="Disabled">Indicates whether the close button is disabled.</param>
public readonly record struct DialogCloseState(bool Disabled);
