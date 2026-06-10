namespace Blazix.BaseUI.DirectionProvider;

/// <summary>
/// Provides the current text direction context to descendant components.
/// </summary>
/// <param name="Direction">The reading direction of the text.</param>
internal sealed record DirectionProviderContext(Direction Direction);
