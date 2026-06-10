namespace Blazix.BaseUI.Menu;

/// <summary>
/// Represents the state of a <see cref="MenuTrigger"/> component.
/// </summary>
/// <param name="Open">Whether the corresponding menu is open.</param>
/// <param name="Disabled">Whether the trigger is disabled.</param>
public readonly record struct MenuTriggerState(bool Open, bool Disabled);
