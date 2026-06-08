using System.ComponentModel;

namespace Blazix.BaseUI.Progress;

/// <summary>
/// Internal extension methods for the progress component.
/// </summary>
internal static class Extensions
{
    extension(ProgressStatus status)
    {
        /// <summary>
        /// Converts a <see cref="ProgressStatus"/> to its corresponding HTML data attribute suffix.
        /// </summary>
        public string ToDataAttributeString() =>
            status switch
            {
                ProgressStatus.Indeterminate => "indeterminate",
                ProgressStatus.Progressing => "progressing",
                ProgressStatus.Complete => "complete",
                _ => throw new InvalidEnumArgumentException(nameof(status), (int)status, typeof(ProgressStatus))
            };
    }
}
