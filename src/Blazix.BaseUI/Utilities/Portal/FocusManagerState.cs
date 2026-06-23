using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Utilities.Portal;

/// <summary>
/// Represents the focus management state for a portal, used by
/// <see cref="FloatingFocusManager.FloatingFocusManager"/> to coordinate focus behavior.
/// </summary>
internal sealed class FocusManagerState
{
    /// <summary>
    /// Gets or sets a value indicating whether focus is fully trapped (modal mode).
    /// </summary>
    public bool Modal { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the floating element is open.
    /// </summary>
    public bool Open { get; set; }

    /// <summary>
    /// Gets or sets the callback to change the open state.
    /// </summary>
    public Func<bool, string?, Task>? OnOpenChangeAsync { get; set; }

    /// <summary>
    /// Gets or sets the trigger/reference element.
    /// </summary>
    public ElementReference? DomReference { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to close the floating element when focus moves outside.
    /// </summary>
    public bool CloseOnFocusOut { get; set; }
}
