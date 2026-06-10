namespace Blazix.BaseUI.Progress;

/// <summary>
/// The context shared by all progress child components, providing access to the
/// root component's state and formatted values.
/// </summary>
internal sealed class ProgressRootContext
{
    /// <summary>
    /// Error thrown when a progress part is rendered outside of a <see cref="ProgressRoot"/>.
    /// </summary>
    public const string MissingContextMessage = "Base UI: ProgressRootContext is missing. Progress parts must be placed within <Progress.Root>.";

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
    public double? Value { get; set; }

    /// <summary>
    /// Gets or sets the component state.
    /// </summary>
    public ProgressRootState State { get; set; } = new(ProgressStatus.Indeterminate);

    /// <summary>
    /// Gets or sets the current progress status.
    /// </summary>
    public ProgressStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the callback used by <see cref="ProgressLabel"/> to register its id
    /// with the root for <c>aria-labelledby</c> association.
    /// </summary>
    public Action<string?> SetLabelIdAction { get; set; } = _ => { };
}
