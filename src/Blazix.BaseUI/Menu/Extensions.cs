namespace Blazix.BaseUI.Menu;

/// <summary>
/// Provides extension methods for converting menu enumeration values to their data attribute string representations.
/// </summary>
internal static class Extensions
{
    /// <summary>
    /// Converts an <see cref="MenuInstantType"/> value to its corresponding data attribute string.
    /// </summary>
    /// <param name="instant">The instant type value to convert.</param>
    /// <returns>The data attribute string representation, or <see langword="null"/> if the value is not recognized.</returns>
    public static string? ToDataAttributeString(this MenuInstantType instant) => instant switch
    {
        MenuInstantType.Click => "click",
        MenuInstantType.Dismiss => "dismiss",
        MenuInstantType.Group => "group",
        MenuInstantType.TriggerChange => "trigger-change",
        _ => null
    };

}
