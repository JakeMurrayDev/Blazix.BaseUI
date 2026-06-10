namespace Blazix.BaseUI.NavigationMenu;

/// <summary>
/// Provides context for the <see cref="NavigationMenuPortal"/> and its descendant components.
/// </summary>
internal sealed class NavigationMenuPortalContext
{
    /// <summary>
    /// Gets or sets whether the portal contents should remain mounted in the DOM when the menu is closed.
    /// </summary>
    public bool KeepMounted { get; set; }
}
