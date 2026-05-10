namespace BlazorBaseUI.Dialog;

/// <summary>
/// Extension methods for converting dialog enumerations to data attribute strings.
/// </summary>
internal static class Extensions
{
    public static string? ToDataAttributeString(this DialogModalMode mode) => mode switch
    {
        DialogModalMode.False => "false",
        DialogModalMode.True => "true",
        DialogModalMode.TrapFocus => "trap-focus",
        _ => null
    };

    public static string ToRoleString(this DialogRole role) => role switch
    {
        DialogRole.AlertDialog => "alertdialog",
        _ => "dialog"
    };
}
