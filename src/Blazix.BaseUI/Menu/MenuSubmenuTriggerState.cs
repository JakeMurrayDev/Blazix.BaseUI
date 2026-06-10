namespace Blazix.BaseUI.Menu;

/// <summary>
/// Represents the state of a <see cref="MenuSubmenuTrigger"/> component.
/// </summary>
/// <param name="Disabled">Whether the submenu trigger is disabled.</param>
/// <param name="Highlighted">Whether the submenu trigger is highlighted.</param>
/// <param name="Open">Whether the corresponding submenu is open.</param>
public readonly record struct MenuSubmenuTriggerState(
    bool Disabled,
    bool Highlighted,
    bool Open);
