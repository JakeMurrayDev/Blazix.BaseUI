using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Select;

/// <summary>
/// Provides shared state for a <see cref="SelectPositioner"/> and its descendant components.
/// </summary>
internal sealed class SelectPositionerContext
{
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
    /// Gets or sets whether the selected item should be vertically aligned with the trigger.
    /// </summary>
    public bool AlignItemWithTriggerActive { get; set; }

    /// <summary>
    /// Gets or sets whether Floating UI has completed its first positioning pass.
    /// Mirrors React's <c>isPositioned</c> from the <c>useFloating</c> hook
    /// (consumed at <c>SelectPopup.tsx:269</c>) and is used to gate Blazor's
    /// align-item-with-trigger pass so it doesn't run before the positioner has
    /// resolved its initial placement (avoids the FOUC race documented in
    /// <c>blazix-baseui-floating.min.js</c>'s <c>data-positioned</c> hide rule).
    /// Resets to <see langword="false"/> after the close transition and at
    /// the start of each open so sizing is recomputed before visibility.
    /// </summary>
    public bool IsPositioned { get; set; }

    /// <summary>
    /// Gets the delegate that returns the arrow element reference.
    /// </summary>
    public Func<ElementReference?> GetArrowElement { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that sets the arrow element reference.
    /// </summary>
    public Action<ElementReference?> SetArrowElement { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that flips <c>alignItemWithTrigger</c> off at runtime.
    /// Mirrors the React <c>setControlledAlignItemWithTrigger</c> dispatch made
    /// available to the popup so it can disable align-item mode when it cannot
    /// fit within the viewport.
    /// </summary>
    public Action<bool> SetControlledAlignItemWithTrigger { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that returns the scroll-up arrow element reference.
    /// Mirrors the React <c>scrollUpArrowRef</c>. <see langword="null"/> when
    /// no scroll-up arrow is mounted.
    /// </summary>
    public Func<ElementReference?> GetScrollUpArrow { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that registers the scroll-up arrow element reference.
    /// </summary>
    public Action<ElementReference?> SetScrollUpArrow { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that returns the scroll-down arrow element reference.
    /// Mirrors the React <c>scrollDownArrowRef</c>.
    /// </summary>
    public Func<ElementReference?> GetScrollDownArrow { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that registers the scroll-down arrow element reference.
    /// </summary>
    public Action<ElementReference?> SetScrollDownArrow { get; init; } = null!;
}
