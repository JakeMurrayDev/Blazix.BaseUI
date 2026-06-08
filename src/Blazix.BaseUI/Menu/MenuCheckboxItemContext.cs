namespace Blazix.BaseUI.Menu;

/// <summary>
/// Provides shared state for a <see cref="MenuCheckboxItem"/> and its descendant <see cref="MenuCheckboxItemIndicator"/>.
/// </summary>
internal sealed class MenuCheckboxItemContext
{
    /// <summary>
    /// Gets or sets whether the checkbox item is checked.
    /// </summary>
    public bool Checked { get; set; }

    /// <summary>
    /// Gets or sets whether the checkbox item is highlighted.
    /// </summary>
    public bool Highlighted { get; set; }

    /// <summary>
    /// Gets or sets whether the checkbox item is disabled.
    /// </summary>
    public bool Disabled { get; set; }
}
