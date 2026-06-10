namespace Blazix.BaseUI.Tooltip;

/// <summary>
/// Represents the state of the <see cref="TooltipTrigger"/> component.
/// </summary>
/// <param name="Open">Indicates whether the tooltip is currently open for this trigger.</param>
/// <param name="Disabled">Indicates whether the trigger is currently disabled.</param>
public readonly record struct TooltipTriggerState(bool Open, bool Disabled);
