namespace Blazix.BaseUI.Toolbar;

/// <summary>
/// Provides cascading state from a <see cref="ToolbarGroup"/> to its descendants.
/// </summary>
internal sealed class ToolbarGroupContext
{
    /// <summary>
    /// Gets or sets whether all toolbar items in the group should ignore user interaction.
    /// </summary>
    public bool Disabled { get; set; }
}
