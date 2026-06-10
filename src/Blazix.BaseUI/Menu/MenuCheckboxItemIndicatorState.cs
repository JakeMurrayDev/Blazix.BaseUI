namespace Blazix.BaseUI.Menu;

/// <summary>
/// Represents the state of a <see cref="MenuCheckboxItemIndicator"/> component.
/// </summary>
/// <param name="Checked">Whether the checkbox item is checked.</param>
/// <param name="Disabled">Whether the checkbox item is disabled.</param>
/// <param name="Highlighted">Whether the checkbox item is highlighted.</param>
/// <param name="TransitionStatus">The current transition animation status.</param>
public readonly record struct MenuCheckboxItemIndicatorState(
    bool Checked,
    bool Disabled,
    bool Highlighted,
    TransitionStatus TransitionStatus);
