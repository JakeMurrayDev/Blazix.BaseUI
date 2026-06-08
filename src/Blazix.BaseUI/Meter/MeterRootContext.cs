namespace Blazix.BaseUI.Meter;

/// <summary>
/// The context shared by all meter child components, providing access to the
/// root component's state and formatted values.
/// </summary>
internal sealed class MeterRootContext
{
    internal const string MissingContextMessage = "Base UI: MeterRootContext is missing. Meter parts must be placed within <Meter.Root>.";

    /// <summary>
    /// Gets or sets the formatted value of the component.
    /// </summary>
    public string FormattedValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum value.
    /// </summary>
    public double Max { get; set; }

    /// <summary>
    /// Gets or sets the minimum value.
    /// </summary>
    public double Min { get; set; }

    /// <summary>
    /// Gets or sets the current value.
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// Gets or sets the component state.
    /// </summary>
    public MeterRootState State { get; set; } = new();

    /// <summary>
    /// Gets or sets the callback used by <see cref="MeterLabel"/> to register its id
    /// with the root for <c>aria-labelledby</c> association.
    /// </summary>
    public Action<string?> SetLabelIdAction { get; set; } = null!;
}
