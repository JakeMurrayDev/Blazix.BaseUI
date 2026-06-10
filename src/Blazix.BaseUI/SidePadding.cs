namespace Blazix.BaseUI;

/// <summary>
/// Specifies per-side collision padding between the popup and the collision boundary edge.
/// </summary>
/// <param name="Top">Padding for the top side in pixels.</param>
/// <param name="Right">Padding for the right side in pixels.</param>
/// <param name="Bottom">Padding for the bottom side in pixels.</param>
/// <param name="Left">Padding for the left side in pixels.</param>
public sealed record SidePadding(double Top = 0, double Right = 0, double Bottom = 0, double Left = 0);
