using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Menu;

/// <summary>
/// Provides shared state for a <see cref="MenuPositioner"/> and its descendant components.
/// </summary>
internal sealed class MenuPositionerContext
{
    /// <summary>
    /// Gets the unique identifier for this positioner instance.
    /// </summary>
    public string? NodeId { get; init; }

    /// <summary>
    /// Gets or sets which side the popup is positioned relative to the trigger.
    /// </summary>
    public Side Side { get; set; }

    /// <summary>
    /// Gets or sets how the popup is aligned relative to the specified side.
    /// </summary>
    public Align Align { get; set; }

    /// <summary>
    /// Gets or sets whether the anchor is hidden.
    /// </summary>
    public bool AnchorHidden { get; set; }

    /// <summary>
    /// Gets or sets whether the arrow is uncentered.
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
