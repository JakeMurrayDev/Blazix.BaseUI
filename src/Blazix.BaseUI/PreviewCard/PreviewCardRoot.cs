namespace Blazix.BaseUI.PreviewCard;

/// <summary>
/// Groups all parts of the preview card and manages open/close state.
/// Does not render an HTML element.
/// </summary>
public sealed partial class PreviewCardRoot;

/// <summary>
/// Provides imperative actions for the preview card.
/// </summary>
public sealed class PreviewCardRootActions
{
    /// <summary>
    /// Gets or sets the action that unmounts the preview card manually.
    /// </summary>
    public Action? Unmount { get; internal set; }

    /// <summary>
    /// Gets or sets the action that closes the preview card imperatively.
    /// </summary>
    public Action? Close { get; internal set; }

    /// <summary>
    /// Gets or sets the action that opens the preview card imperatively.
    /// </summary>
    public Action? Open { get; internal set; }

    /// <summary>
    /// Gets or sets the action that opens the preview card imperatively with a specific trigger ID.
    /// </summary>
    public Action<string>? OpenWithTriggerId { get; internal set; }
}

/// <summary>
/// Provides the current payload value to the preview card's child content.
/// </summary>
/// <param name="Payload">The payload from the active trigger, or <see langword="null"/> if no payload is set.</param>
public readonly record struct PreviewCardRootPayloadContext(object? Payload);
