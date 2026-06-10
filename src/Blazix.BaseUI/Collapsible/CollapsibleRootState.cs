namespace Blazix.BaseUI.Collapsible;

/// <summary>
/// Represents the render state of the <see cref="CollapsibleRoot"/> component.
/// </summary>
/// <param name="Open">Whether the collapsible is currently open.</param>
/// <param name="Disabled">Whether the collapsible is disabled.</param>
/// <param name="TransitionStatus">The current transition status of the collapsible.</param>
public sealed record CollapsibleRootState(
    bool Open,
    bool Disabled,
    TransitionStatus TransitionStatus);
