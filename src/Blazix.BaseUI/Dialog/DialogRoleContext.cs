namespace Blazix.BaseUI.Dialog;

/// <summary>
/// Cascading context that allows wrapper components (e.g., AlertDialogRoot)
/// to override the ARIA role used by DialogRoot.
/// </summary>
internal sealed class DialogRoleContext
{
    public DialogRole Role { get; init; } = DialogRole.Dialog;
}
