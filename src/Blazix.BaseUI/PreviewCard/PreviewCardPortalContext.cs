namespace Blazix.BaseUI.PreviewCard;

/// <summary>
/// Provides portal configuration for child components of a <see cref="PreviewCardPortal"/>.
/// </summary>
internal sealed class PreviewCardPortalContext
{
    /// <summary>
    /// Gets or sets a value indicating whether the portal is kept mounted in the DOM while the popup is hidden.
    /// </summary>
    public bool KeepMounted { get; set; }
}
