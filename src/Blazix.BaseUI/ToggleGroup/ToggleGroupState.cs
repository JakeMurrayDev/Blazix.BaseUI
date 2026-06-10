namespace Blazix.BaseUI.ToggleGroup;

/// <summary>
/// Represents the current state of a <see cref="ToggleGroup"/> component.
/// </summary>
/// <param name="Disabled">Gets whether the toggle group should ignore user interaction.</param>
/// <param name="Multiple">Gets whether multiple items can be pressed at the same time.</param>
/// <param name="Orientation">Gets the orientation of the toggle group.</param>
public sealed record ToggleGroupState(bool Disabled, bool Multiple, Orientation Orientation)
{
    internal static ToggleGroupState Default { get; } = new(
        Disabled: false,
        Multiple: false,
        Orientation: Orientation.Horizontal);
}
