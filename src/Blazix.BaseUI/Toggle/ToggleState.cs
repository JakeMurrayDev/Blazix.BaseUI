namespace Blazix.BaseUI.Toggle;

/// <summary>
/// Represents the current state of a <see cref="Toggle"/> component.
/// </summary>
/// <param name="Pressed">Gets whether the toggle is currently pressed.</param>
/// <param name="Disabled">Gets whether the toggle should ignore user interaction.</param>
public sealed record ToggleState(bool Pressed, bool Disabled)
{
    internal static ToggleState Default { get; } = new(Pressed: false, Disabled: false);
}
