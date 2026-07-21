namespace Blazix.BaseUI.Select;

/// <summary>
/// Defines a virtual rectangle used as a Select positioning anchor.
/// </summary>
/// <param name="X">The viewport-relative horizontal coordinate.</param>
/// <param name="Y">The viewport-relative vertical coordinate.</param>
/// <param name="Width">The rectangle width.</param>
/// <param name="Height">The rectangle height.</param>
public readonly record struct SelectVirtualAnchor(double X, double Y, double Width = 0, double Height = 0);
