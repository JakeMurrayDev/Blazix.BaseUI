using Blazix.BaseUI.Popover;
using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.PreviewCard;

/// <summary>
/// Provides positioning state for child components of a <see cref="PreviewCardPositioner"/>.
/// </summary>
internal sealed class PreviewCardPositionerContext
{
    /// <summary>
    /// Gets or sets the side of the anchor the preview card is positioned on.
    /// </summary>
    public Side Side { get; set; }

    /// <summary>
    /// Gets or sets the alignment of the preview card relative to the anchor.
    /// </summary>
    public Align Align { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the anchor element is hidden from view.
    /// </summary>
    public bool AnchorHidden { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the arrow is uncentered relative to the popup.
    /// </summary>
    public bool ArrowUncentered { get; set; }

    /// <summary>
    /// Gets or sets the delegate that returns the arrow element reference.
    /// </summary>
    public Func<ElementReference?> GetArrowElement { get; set; } = null!;

    /// <summary>
    /// Gets or sets the delegate that stores the arrow element reference.
    /// </summary>
    public Action<ElementReference?> SetArrowElement { get; set; } = null!;
}
