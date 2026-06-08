namespace Blazix.BaseUI.Tabs;

/// <summary>
/// Represents the position of a tab element relative to the tabs list container.
/// </summary>
/// <param name="Left">The left offset in pixels.</param>
/// <param name="Right">The right offset in pixels.</param>
/// <param name="Top">The top offset in pixels.</param>
/// <param name="Bottom">The bottom offset in pixels.</param>
public readonly record struct TabPosition(double Left, double Right, double Top, double Bottom);

/// <summary>
/// Represents the size of a tab element.
/// </summary>
/// <param name="Width">The width in pixels.</param>
/// <param name="Height">The height in pixels.</param>
public readonly record struct TabSize(double Width, double Height);
