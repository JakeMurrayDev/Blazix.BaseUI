namespace Blazix.BaseUI.NavigationMenu;

/// <summary>
/// Represents the state of a <see cref="NavigationMenuPopup"/> component.
/// </summary>
/// <param name="Open">Whether the popup is currently displayed.</param>
/// <param name="Side">The side on which the popup is positioned.</param>
/// <param name="Align">The alignment of the popup relative to the side.</param>
/// <param name="AnchorHidden">Whether the anchor element is hidden from view.</param>
/// <param name="TransitionStatus">The current transition animation status.</param>
public readonly record struct NavigationMenuPopupState(bool Open, Side Side, Align Align, bool AnchorHidden, TransitionStatus TransitionStatus);
