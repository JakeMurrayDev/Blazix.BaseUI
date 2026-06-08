namespace Blazix.BaseUI.Popover;

/// <summary>
/// Represents the state of the <see cref="PopoverTrigger"/> component.
/// </summary>
/// <param name="Open">Indicates whether the popover is currently open.</param>
/// <param name="Disabled">Indicates whether the trigger is currently disabled.</param>
public readonly record struct PopoverTriggerState(bool Open, bool Disabled);
