namespace Blazix.BaseUI.DirectionProvider;

/// <summary>
/// Provides the current text direction context to descendant components.
/// </summary>
internal sealed class DirectionProviderContext
{
    /// <summary>
    /// Gets or sets the reading direction of the text.
    /// </summary>
    public Direction Direction { get; set; }
}
