namespace Blazix.BaseUI.MenuBar;

/// <summary>
/// Represents the state of the <see cref="MenuBarRoot"/> component.
/// </summary>
/// <param name="Disabled">Whether the menubar is disabled.</param>
/// <param name="HasSubmenuOpen">Whether any submenu within the menubar is open.</param>
/// <param name="Modal">Whether the menubar is modal.</param>
/// <param name="Orientation">The orientation of the menubar.</param>
public readonly record struct MenuBarRootState(
    bool Disabled,
    bool HasSubmenuOpen,
    bool Modal,
    Orientation Orientation);
