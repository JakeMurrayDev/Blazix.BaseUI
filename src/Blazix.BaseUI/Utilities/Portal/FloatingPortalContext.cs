using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Utilities.Portal;

/// <summary>
/// Provides cascading state from a <see cref="Portal"/> to descendant floating components,
/// exposing the portal node and focus management state so that
/// <see cref="FloatingFocusManager.FloatingFocusManager"/> and <see cref="FocusGuard.FocusGuard"/>
/// can coordinate focus behavior across portal boundaries.
/// </summary>
internal sealed class FloatingPortalContext
{
    /// <summary>
    /// Gets or sets the element reference of the portal container node.
    /// </summary>
    public ElementReference? PortalNode { get; set; }

    /// <summary>
    /// Gets or sets the current focus manager state for the portal.
    /// When non-null, a <see cref="FloatingFocusManager.FloatingFocusManager"/> is actively managing
    /// focus within this portal.
    /// </summary>
    public FocusManagerState? FocusManagerState { get; set; }

    /// <summary>
    /// Gets or sets the element reference of the "before outside" focus guard.
    /// </summary>
    public ElementReference? BeforeOutsideGuard { get; set; }

    /// <summary>
    /// Gets or sets the element reference of the "after outside" focus guard.
    /// </summary>
    public ElementReference? AfterOutsideGuard { get; set; }

    /// <summary>
    /// Gets or sets the element reference of the "before inside" focus guard.
    /// </summary>
    public ElementReference? BeforeInsideGuard { get; set; }

    /// <summary>
    /// Gets or sets the element reference of the "after inside" focus guard.
    /// </summary>
    public ElementReference? AfterInsideGuard { get; set; }
}
