namespace Blazix.BaseUI.Progress;

/// <summary>
/// Describes the completion status of the progress bar.
/// </summary>
public enum ProgressStatus
{
    /// <summary>
    /// The progress value is unknown (value is <see langword="null"/>).
    /// </summary>
    Indeterminate,

    /// <summary>
    /// The task is in progress but not yet complete.
    /// </summary>
    Progressing,

    /// <summary>
    /// The task has completed.
    /// </summary>
    Complete
}
