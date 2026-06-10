namespace Blazix.BaseUI.Tabs;

/// <summary>
/// Represents the state of the <see cref="TabsIndicator{TValue}"/> component,
/// exposed to <c>ClassValue</c> and <c>StyleValue</c> callbacks.
/// </summary>
/// <param name="Orientation">The orientation of the tabs.</param>
/// <param name="ActivationDirection">The direction of the most recent tab activation.</param>
/// <param name="ActiveTabPosition">The position of the currently active tab, or <see langword="null"/> if unavailable.</param>
/// <param name="ActiveTabSize">The size of the currently active tab, or <see langword="null"/> if unavailable.</param>
public sealed record TabsIndicatorState(
    Orientation Orientation,
    ActivationDirection ActivationDirection,
    TabPosition? ActiveTabPosition,
    TabSize? ActiveTabSize)
{
    internal static TabsIndicatorState Default { get; } = new(
        Orientation: Orientation.Horizontal,
        ActivationDirection: ActivationDirection.None,
        ActiveTabPosition: null,
        ActiveTabSize: null);
}
