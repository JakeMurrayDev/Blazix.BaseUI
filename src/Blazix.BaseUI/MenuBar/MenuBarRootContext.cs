using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.MenuBar;

/// <summary>
/// Provides cascading context for the <see cref="MenuBarRoot"/> component and its descendants.
/// </summary>
internal sealed class MenuBarRootContext
{
    /// <summary>
    /// Gets or sets a value indicating whether the menubar is disabled.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether any submenu within the menubar is open.
    /// </summary>
    public bool HasSubmenuOpen { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the menubar is modal.
    /// </summary>
    public bool Modal { get; set; }

    /// <summary>
    /// Gets or sets the orientation of the menubar.
    /// </summary>
    public Orientation Orientation { get; set; }

    /// <summary>
    /// Gets or sets the callback to register a menu item element with the menubar.
    /// </summary>
    public Action<ElementReference> RegisterItem { get; set; } = null!;

    /// <summary>
    /// Gets or sets the callback to unregister a menu item element from the menubar.
    /// </summary>
    public Action<ElementReference> UnregisterItem { get; set; } = null!;

    /// <summary>
    /// Gets or sets the callback to notify the menubar that a submenu has opened or closed.
    /// </summary>
    public Action<bool, string?> SetHasSubmenuOpen { get; set; } = null!;

    /// <summary>
    /// Gets or sets the function that returns whether any submenu is currently open.
    /// </summary>
    public Func<bool> GetHasSubmenuOpen { get; set; } = null!;

    /// <summary>
    /// Gets or sets the function that returns the menubar's root element reference.
    /// </summary>
    public Func<ElementReference?> GetElement { get; set; } = null!;
}
