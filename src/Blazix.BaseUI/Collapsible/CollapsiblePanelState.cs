namespace Blazix.BaseUI.Collapsible;

/// <summary>
/// Represents the render state of the <see cref="CollapsiblePanel"/> component.
/// </summary>
/// <param name="Open">Whether the collapsible panel is currently open.</param>
/// <param name="Disabled">Whether the collapsible is disabled.</param>
/// <param name="TransitionStatus">The current transition status of the panel.</param>
public sealed record CollapsiblePanelState(
    bool Open,
    bool Disabled,
    TransitionStatus TransitionStatus);
