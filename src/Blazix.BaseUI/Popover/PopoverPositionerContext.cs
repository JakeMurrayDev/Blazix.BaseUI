using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Popover;

/// <summary>
/// Provides cascading state and callbacks from the <see cref="PopoverPositioner"/> to the <see cref="PopoverArrow"/>.
/// </summary>
internal sealed class PopoverPositionerContext
{
    /// <summary>
    /// Gets or sets the side of the anchor element the popover is positioned against.
    /// </summary>
    public Side Side { get; set; }

    /// <summary>
    /// Gets or sets the alignment of the popover relative to the specified side.
    /// </summary>
    public Align Align { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the anchor element is hidden from view.
    /// </summary>
    public bool AnchorHidden { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the arrow is not centered relative to the popup.
    /// </summary>
    public bool ArrowUncentered { get; set; }

    /// <summary>
    /// Gets or sets a delegate that returns the arrow element reference.
    /// </summary>
    public Func<ElementReference?> GetArrowElement { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that sets the arrow element reference.
    /// </summary>
    public Action<ElementReference?> SetArrowElement { get; set; } = null!;
}
