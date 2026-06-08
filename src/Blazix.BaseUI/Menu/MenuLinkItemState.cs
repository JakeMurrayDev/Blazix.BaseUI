namespace Blazix.BaseUI.Menu;

/// <summary>
/// Represents the state of a <see cref="MenuLinkItem"/> component.
/// </summary>
/// <param name="Highlighted">Whether the menu link item is highlighted.</param>
public readonly record struct MenuLinkItemState(
    bool Highlighted);
