namespace Blazix.BaseUI.Select;

/// <summary>
/// Represents the state of a <see cref="SelectArrow"/> component.
/// </summary>
/// <param name="Open">Whether the select popup is open.</param>
/// <param name="Side">Which side the popup is positioned relative to the trigger.</param>
/// <param name="Align">How the popup is aligned relative to the specified side.</param>
/// <param name="Uncentered">Whether the arrow is uncentered.</param>
public readonly record struct SelectArrowState(
    bool Open,
    Side Side,
    Align Align,
    bool Uncentered);
