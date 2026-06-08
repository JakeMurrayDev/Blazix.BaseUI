using Microsoft.AspNetCore.Components;

namespace Blazix.BaseUI.Toolbar;

/// <summary>
/// Provides cascading state from a <see cref="ToolbarRoot"/> to its descendants.
/// </summary>
internal sealed class ToolbarRootContext
{
    /// <summary>
    /// Gets or sets whether the toolbar should ignore user interaction.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets the orientation of the toolbar.
    /// </summary>
    public Orientation Orientation { get; set; }

    /// <summary>
    /// Gets or sets the callback to register a toolbar item for keyboard navigation.
    /// </summary>
    public Action<ElementReference> RegisterItem { get; set; } = null!;

    /// <summary>
    /// Gets or sets the callback to unregister a toolbar item from keyboard navigation.
    /// </summary>
    public Action<ElementReference> UnregisterItem { get; set; } = null!;
}
