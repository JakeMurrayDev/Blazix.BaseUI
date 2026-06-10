namespace Blazix.BaseUI.Menu;

/// <summary>
/// Describes the state of the <see cref="MenuViewport"/> component.
/// </summary>
public readonly record struct MenuViewportState(string? ActivationDirection, bool Transitioning, MenuInstantType Instant);
