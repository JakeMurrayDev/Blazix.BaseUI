namespace Blazix.BaseUI.PreviewCard;

/// <summary>
/// Provides extension methods for preview card types.
/// </summary>
internal static class Extensions
{
    /// <summary>
    /// Converts a <see cref="PreviewCardInstantType"/> value to its corresponding data attribute string.
    /// </summary>
    /// <param name="instantType">The instant type to convert.</param>
    /// <returns>The data attribute string, or <see langword="null"/> if not applicable.</returns>
    public static string? ToDataAttributeString(this PreviewCardInstantType instantType) => instantType switch
    {
        PreviewCardInstantType.Focus => "focus",
        PreviewCardInstantType.Dismiss => "dismiss",
        _ => null
    };
}
