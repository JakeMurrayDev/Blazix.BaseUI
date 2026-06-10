namespace Blazix.BaseUI.Select;

/// <summary>
/// Represents the state of a <see cref="SelectPopup"/> component.
/// </summary>
/// <param name="Open">Whether the select popup is open.</param>
/// <param name="Side">Which side the popup is positioned relative to the trigger.</param>
/// <param name="Align">How the popup is aligned relative to the specified side.</param>
/// <param name="TransitionStatus">The current transition animation status.</param>
/// <param name="AlignItemWithTriggerActive">Whether the popup is currently in align-item-with-trigger mode.</param>
public readonly record struct SelectPopupState(
    bool Open,
    Side Side,
    Align Align,
    TransitionStatus TransitionStatus,
    bool AlignItemWithTriggerActive);
