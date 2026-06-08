namespace Blazix.BaseUI.Tabs;

/// <summary>
/// Represents the state of the <see cref="TabsTab{TValue}"/> component,
/// exposed to <c>ClassValue</c> and <c>StyleValue</c> callbacks.
/// </summary>
/// <param name="Active">Whether the tab is currently active.</param>
/// <param name="Disabled">Whether the tab is disabled.</param>
/// <param name="Orientation">The orientation of the tabs.</param>
public sealed record TabsTabState(bool Active, bool Disabled, Orientation Orientation)
{
    internal static TabsTabState Default { get; } = new(
        Active: false,
        Disabled: false,
        Orientation: Orientation.Horizontal);
}
