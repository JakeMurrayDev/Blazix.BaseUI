namespace Blazix.BaseUI.Dialog;

/// <summary>
/// Represents the state of the dialog trigger component.
/// </summary>
/// <param name="Disabled">Indicates whether the trigger is disabled.</param>
/// <param name="Open">Indicates whether the dialog is currently open.</param>
public readonly record struct DialogTriggerState(bool Disabled, bool Open);
