namespace Blazix.BaseUI.Menu;

/// <summary>
/// Represents the state of a <see cref="MenuRadioItem"/> component.
/// </summary>
/// <param name="Disabled">Whether the radio item is disabled.</param>
/// <param name="Highlighted">Whether the radio item is highlighted.</param>
/// <param name="Checked">Whether the radio item is selected.</param>
public readonly record struct MenuRadioItemState(
    bool Disabled,
    bool Highlighted,
    bool Checked);
