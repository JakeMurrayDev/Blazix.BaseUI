namespace Blazix.BaseUI.Menu;

/// <summary>
/// Provides shared state for a <see cref="MenuRadioItem"/> and its descendant <see cref="MenuRadioItemIndicator"/>.
/// </summary>
internal sealed class MenuRadioItemContext
{
    /// <summary>
    /// Gets or sets whether the radio item is selected.
    /// </summary>
    public bool Checked { get; set; }

    /// <summary>
    /// Gets or sets whether the radio item is highlighted.
    /// </summary>
    public bool Highlighted { get; set; }

    /// <summary>
    /// Gets or sets whether the radio item is disabled.
    /// </summary>
    public bool Disabled { get; set; }
}
