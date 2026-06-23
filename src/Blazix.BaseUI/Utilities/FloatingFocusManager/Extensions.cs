namespace Blazix.BaseUI.Utilities.FloatingFocusManager;

/// <summary>
/// Provides extension methods for converting floating focus manager enumerations to their JS string representations.
/// </summary>
internal static class Extensions
{
    /// <summary>
    /// Converts a <see cref="FocusManagerOrderItem"/> value to its corresponding JS string.
    /// </summary>
    public static string ToJsString(this FocusManagerOrderItem item) => item switch
    {
        FocusManagerOrderItem.Content => "content",
        FocusManagerOrderItem.Floating => "floating",
        FocusManagerOrderItem.Reference => "reference",
        _ => "content"
    };
}
