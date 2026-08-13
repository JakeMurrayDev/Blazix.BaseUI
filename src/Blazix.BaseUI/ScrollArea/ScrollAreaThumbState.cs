namespace Blazix.BaseUI.ScrollArea;

/// <summary>
/// Represents the state of the <see cref="ScrollAreaThumb"/> component.
/// </summary>
/// <param name="Scrolling">Whether this thumb's axis is being scrolled.</param>
/// <param name="Orientation">The scrollbar orientation.</param>
public readonly record struct ScrollAreaThumbState(bool Scrolling, Orientation Orientation);
