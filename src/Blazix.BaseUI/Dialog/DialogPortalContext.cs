namespace Blazix.BaseUI.Dialog;

/// <summary>
/// Provides cascading state from the <see cref="DialogPortal"/> to child dialog components.
/// </summary>
internal sealed class DialogPortalContext
{
    /// <summary>
    /// Gets or sets a value indicating whether the portal content remains mounted when the dialog is closed.
    /// </summary>
    public bool KeepMounted { get; set; }
}
