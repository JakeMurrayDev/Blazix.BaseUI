namespace BlazorBaseUI.Tabs;

/// <summary>
/// Represents the state of the <see cref="TabsPanel{TValue}"/> component,
/// exposed to <c>ClassValue</c> and <c>StyleValue</c> callbacks.
/// </summary>
/// <param name="Hidden">Whether the panel is currently hidden.</param>
/// <param name="Orientation">The orientation of the tabs.</param>
/// <param name="ActivationDirection">The direction of the most recent tab activation.</param>
/// <param name="TransitionStatus">The current transition status of the panel.</param>
public sealed record TabsPanelState(
    bool Hidden,
    Orientation Orientation,
    ActivationDirection ActivationDirection,
    TransitionStatus TransitionStatus)
{
    internal static TabsPanelState Default { get; } = new(
        Hidden: true,
        Orientation: Orientation.Horizontal,
        ActivationDirection: ActivationDirection.None,
        TransitionStatus: TransitionStatus.Undefined);
}
