namespace Blazix.BaseUI.Toolbar;

/// <summary>
/// Represents the current state of a <see cref="ToolbarRoot"/> component.
/// </summary>
/// <param name="Disabled">Gets whether the toolbar should ignore user interaction.</param>
/// <param name="Orientation">Gets the orientation of the toolbar.</param>
public sealed record ToolbarRootState(bool Disabled, Orientation Orientation);
