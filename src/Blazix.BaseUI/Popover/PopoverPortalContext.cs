namespace Blazix.BaseUI.Popover;

/// <summary>
/// Provides cascading state from the <see cref="PopoverPortal"/> to child popover components.
/// </summary>
internal sealed class PopoverPortalContext
{
    /// <summary>
    /// Gets or sets a value indicating whether the portal content remains mounted when the popover is closed.
    /// </summary>
    public bool KeepMounted { get; set; }
}
