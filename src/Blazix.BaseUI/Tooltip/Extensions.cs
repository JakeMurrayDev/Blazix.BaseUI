namespace Blazix.BaseUI.Tooltip;

/// <summary>
/// Provides extension methods for tooltip types.
/// </summary>
internal static class Extensions
{
    /// <summary>
    /// Converts a <see cref="TooltipInstantType"/> value to its corresponding data attribute string.
    /// </summary>
    /// <param name="instantType">The instant type to convert.</param>
    /// <returns>The data attribute string, or <see langword="null"/> if not applicable.</returns>
    public static string? ToDataAttributeString(this TooltipInstantType instantType) => instantType switch
    {
        TooltipInstantType.Delay => "delay",
        TooltipInstantType.Focus => "focus",
        TooltipInstantType.Dismiss => "dismiss",
        TooltipInstantType.TrackingCursor => "tracking-cursor",
        _ => null
    };
}
