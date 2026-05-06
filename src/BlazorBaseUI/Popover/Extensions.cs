namespace BlazorBaseUI.Popover;

/// <summary>
/// Provides extension methods for converting popover enumerations to their data attribute string representations.
/// </summary>
internal static class Extensions
{
    /// <summary>
    /// Converts an <see cref="PopoverInstantType"/> value to its corresponding data attribute string.
    /// </summary>
    public static string? ToDataAttributeString(this PopoverInstantType instant) => instant switch
    {
        PopoverInstantType.Click => "click",
        PopoverInstantType.Dismiss => "dismiss",
        PopoverInstantType.Focus => "focus",
        PopoverInstantType.TriggerChange => "trigger-change",
        _ => null
    };

}
