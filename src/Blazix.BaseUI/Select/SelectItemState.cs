namespace Blazix.BaseUI.Select;

/// <summary>
/// Represents the state of a <see cref="SelectItem{TValue}"/> component.
/// </summary>
/// <param name="Disabled">Whether the item is disabled.</param>
/// <param name="Selected">Whether the item is selected.</param>
/// <param name="Highlighted">Whether the item is highlighted.</param>
public readonly record struct SelectItemState(
    bool Disabled,
    bool Selected,
    bool Highlighted);
