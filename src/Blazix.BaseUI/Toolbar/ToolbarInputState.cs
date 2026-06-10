namespace Blazix.BaseUI.Toolbar;

/// <summary>
/// Represents the current state of a <see cref="ToolbarInput"/> component.
/// </summary>
/// <param name="Disabled">Gets whether the input should ignore user interaction.</param>
/// <param name="Orientation">Gets the orientation inherited from the parent toolbar.</param>
/// <param name="Focusable">Gets whether the input remains focusable when disabled.</param>
public sealed record ToolbarInputState(bool Disabled, Orientation Orientation, bool Focusable);
