namespace Blazix.BaseUI.Toolbar;

/// <summary>
/// Represents the current state of a <see cref="ToolbarButton"/> component.
/// </summary>
/// <param name="Disabled">Gets whether the button should ignore user interaction.</param>
/// <param name="Orientation">Gets the orientation inherited from the parent toolbar.</param>
/// <param name="Focusable">Gets whether the button remains focusable when disabled.</param>
public sealed record ToolbarButtonState(bool Disabled, Orientation Orientation, bool Focusable);
