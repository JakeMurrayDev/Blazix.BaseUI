namespace Blazix.BaseUI.Menu;

/// <summary>
/// Represents the state of a <see cref="MenuPopup"/> component.
/// </summary>
/// <param name="Open">Whether the menu is open.</param>
/// <param name="Side">Which side the popup is positioned relative to the trigger.</param>
/// <param name="Align">How the popup is aligned relative to the specified side.</param>
/// <param name="Instant">The type of instant transition to apply.</param>
/// <param name="TransitionStatus">The current transition animation status.</param>
/// <param name="Nested">Whether the menu is nested inside another menu.</param>
public readonly record struct MenuPopupState(
    bool Open,
    Side Side,
    Align Align,
    MenuInstantType Instant,
    TransitionStatus TransitionStatus,
    bool Nested);
