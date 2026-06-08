namespace Blazix.BaseUI.Tabs;

/// <summary>
/// Represents the state of the <see cref="TabsRoot{TValue}"/> component,
/// exposed to <c>ClassValue</c> and <c>StyleValue</c> callbacks.
/// </summary>
/// <param name="Orientation">The orientation of the tabs.</param>
/// <param name="ActivationDirection">The direction of the most recent tab activation.</param>
public sealed record TabsRootState(Orientation Orientation, ActivationDirection ActivationDirection)
{
    internal static TabsRootState Default { get; } = new(
        Orientation: Orientation.Horizontal,
        ActivationDirection: ActivationDirection.None);
}
