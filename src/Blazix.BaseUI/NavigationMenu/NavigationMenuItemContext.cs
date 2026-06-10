namespace Blazix.BaseUI.NavigationMenu;

/// <summary>
/// Provides per-item context for the <see cref="NavigationMenuItem"/> and its descendant components.
/// </summary>
internal sealed class NavigationMenuItemContext
{
    /// <summary>
    /// Gets the unique value identifying this item.
    /// </summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>
    /// Gets the HTML id of the trigger element for this item.
    /// </summary>
    public string TriggerId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the HTML id of the content element for this item.
    /// </summary>
    public string ContentId { get; init; } = string.Empty;
}
