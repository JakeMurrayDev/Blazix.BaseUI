namespace Blazix.BaseUI.Menu;

/// <summary>
/// Provides shared state for a <see cref="MenuPortal"/> and its descendant components.
/// </summary>
internal sealed class MenuPortalContext
{
    /// <summary>
    /// Gets or sets whether the portal contents should remain mounted when the menu is closed.
    /// </summary>
    public bool KeepMounted { get; set; }
}
