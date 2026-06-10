using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.NavigationMenu;

/// <summary>
/// Provides positioning state for the <see cref="NavigationMenuPositioner"/> and its descendant components.
/// </summary>
internal sealed class NavigationMenuPositionerContext
{
    /// <summary>
    /// Gets or sets the side on which the popup is positioned.
    /// </summary>
    public Side Side { get; set; }

    /// <summary>
    /// Gets or sets the alignment of the popup relative to the side.
    /// </summary>
    public Align Align { get; set; }

    /// <summary>
    /// Gets or sets whether the anchor element is hidden from view.
    /// </summary>
    public bool AnchorHidden { get; set; }

    /// <summary>
    /// Gets or sets whether the arrow is not centered relative to the anchor.
    /// </summary>
    public bool ArrowUncentered { get; set; }

    /// <summary>
    /// Gets the delegate that returns the arrow element reference.
    /// </summary>
    public Func<ElementReference?> GetArrowElement { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that sets the arrow element reference.
    /// </summary>
    public Action<ElementReference?> SetArrowElement { get; init; } = null!;
}
