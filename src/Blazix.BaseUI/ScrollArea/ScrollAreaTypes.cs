namespace Blazix.BaseUI.ScrollArea;

/// <summary>
/// Configures the pixel threshold that must be crossed before overflow edge attributes are applied.
/// </summary>
/// <param name="XStart">The horizontal start threshold.</param>
/// <param name="XEnd">The horizontal end threshold.</param>
/// <param name="YStart">The vertical start threshold.</param>
/// <param name="YEnd">The vertical end threshold.</param>
public readonly record struct ScrollAreaOverflowEdgeThreshold(
    double XStart,
    double XEnd,
    double YStart,
    double YEnd)
{
    /// <summary>
    /// Gets a threshold with all edges set to zero.
    /// </summary>
    public static ScrollAreaOverflowEdgeThreshold Zero { get; } = new(0, 0, 0, 0);

    /// <summary>
    /// Creates a threshold that applies the same value to every edge.
    /// </summary>
    /// <param name="value">The threshold in CSS pixels.</param>
    public static ScrollAreaOverflowEdgeThreshold All(double value)
    {
        var normalizedValue = Math.Max(0, value);
        return new(normalizedValue, normalizedValue, normalizedValue, normalizedValue);
    }

    public static implicit operator ScrollAreaOverflowEdgeThreshold(double value) => All(value);

    public static implicit operator ScrollAreaOverflowEdgeThreshold(int value) => All(value);

    internal ScrollAreaOverflowEdgeThreshold Normalize() => new(
        Math.Max(0, XStart),
        Math.Max(0, XEnd),
        Math.Max(0, YStart),
        Math.Max(0, YEnd));
}

internal readonly record struct ScrollAreaSize(double Width, double Height)
{
    public static ScrollAreaSize Zero { get; } = new(0, 0);
}

internal readonly record struct ScrollAreaHiddenState(bool X, bool Y, bool Corner)
{
    public static ScrollAreaHiddenState Default { get; } = new(true, true, true);
}
