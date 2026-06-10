namespace Blazix.BaseUI.Menu;

/// <summary>
/// Represents the state of a <see cref="MenuCheckboxItem"/> component.
/// </summary>
/// <param name="Disabled">Whether the checkbox item is disabled.</param>
/// <param name="Highlighted">Whether the checkbox item is highlighted.</param>
/// <param name="Checked">Whether the checkbox item is checked.</param>
public readonly record struct MenuCheckboxItemState(
    bool Disabled,
    bool Highlighted,
    bool Checked);
