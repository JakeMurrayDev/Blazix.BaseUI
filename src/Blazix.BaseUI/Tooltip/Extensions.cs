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

    /// <summary>
    /// Converts a <see cref="TooltipOpenChangeReason"/> value to its JavaScript interaction string.
    /// </summary>
    /// <param name="reason">The reason to convert.</param>
    /// <returns>The JavaScript interaction reason.</returns>
    public static string ToReasonString(this TooltipOpenChangeReason reason) => reason switch
    {
        TooltipOpenChangeReason.TriggerHover => "trigger-hover",
        TooltipOpenChangeReason.TriggerFocus => "trigger-focus",
        TooltipOpenChangeReason.TriggerPress => "trigger-press",
        TooltipOpenChangeReason.OutsidePress => "outside-press",
        TooltipOpenChangeReason.EscapeKey => "escape-key",
        TooltipOpenChangeReason.Disabled => "disabled",
        TooltipOpenChangeReason.ImperativeAction => "imperative-action",
        _ => "none"
    };
}
