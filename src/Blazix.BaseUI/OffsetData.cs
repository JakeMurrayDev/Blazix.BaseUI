namespace Blazix.BaseUI;

/// <summary>
/// Data provided to offset functions for computing dynamic side/align offsets.
/// Contains the dimensions of the anchor and popup elements along with the computed placement.
/// </summary>
/// <param name="AnchorWidth">The width of the anchor element in pixels.</param>
/// <param name="AnchorHeight">The height of the anchor element in pixels.</param>
/// <param name="PopupWidth">The width of the popup element in pixels.</param>
/// <param name="PopupHeight">The height of the popup element in pixels.</param>
/// <param name="Side">The computed side of the placement (e.g., "top", "bottom", "left", "right").</param>
/// <param name="Align">The computed alignment of the placement (e.g., "start", "center", "end").</param>
public sealed record OffsetData(
    double AnchorWidth,
    double AnchorHeight,
    double PopupWidth,
    double PopupHeight,
    string Side,
    string Align);
