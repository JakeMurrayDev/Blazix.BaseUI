namespace Blazix.BaseUI.Tooltip;

/// <summary>
/// Provides portal configuration for child components of a <see cref="TooltipPortal"/>.
/// </summary>
internal sealed class TooltipPortalContext
{
    /// <summary>
    /// Gets or sets a value indicating whether the portal is kept mounted in the DOM while the popup is hidden.
    /// </summary>
    public bool KeepMounted { get; set; }
}
