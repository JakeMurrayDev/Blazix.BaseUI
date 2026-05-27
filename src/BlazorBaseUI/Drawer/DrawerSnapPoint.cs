using System.Globalization;

namespace BlazorBaseUI.Drawer;

/// <summary>
/// Represents a drawer snap point value. Numeric values between 0 and 1 are viewport fractions,
/// numeric values greater than 1 are pixels, and strings support <c>px</c> or <c>rem</c> units.
/// </summary>
public readonly record struct DrawerSnapPoint
{
    private readonly double? numberValue;
    private readonly string? stringValue;

    private DrawerSnapPoint(double numberValue)
    {
        this.numberValue = numberValue;
        stringValue = null;
    }

    private DrawerSnapPoint(string stringValue)
    {
        this.stringValue = stringValue;
        numberValue = null;
    }

    /// <summary>
    /// Gets the numeric snap point value, if this snap point is numeric.
    /// </summary>
    public double? NumberValue => numberValue;

    /// <summary>
    /// Gets the string snap point value, if this snap point is unit-based.
    /// </summary>
    public string? StringValue => stringValue;

    /// <summary>
    /// Gets a value indicating whether this snap point is numeric.
    /// </summary>
    public bool IsNumber => numberValue.HasValue;

    /// <summary>
    /// Converts a numeric value to a drawer snap point.
    /// </summary>
    public static implicit operator DrawerSnapPoint(double value) => new(value);

    /// <summary>
    /// Converts an integer value to a drawer snap point.
    /// </summary>
    public static implicit operator DrawerSnapPoint(int value) => new(value);

    /// <summary>
    /// Converts a unit string to a drawer snap point.
    /// </summary>
    public static implicit operator DrawerSnapPoint(string value) => new(value);

    /// <inheritdoc />
    public override string ToString()
    {
        return numberValue.HasValue
            ? numberValue.Value.ToString("G", CultureInfo.InvariantCulture)
            : stringValue ?? string.Empty;
    }
}
