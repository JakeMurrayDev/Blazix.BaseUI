namespace Blazix.BaseUI.Progress;

/// <summary>
/// Represents the state of the <see cref="ProgressRoot"/> component.
/// </summary>
/// <param name="Status">The current progress status.</param>
public sealed record ProgressRootState(ProgressStatus Status);
