namespace Blazix.BaseUI.Tooltip;

/// <summary>
/// Groups all parts of the tooltip and manages open/close state.
/// Does not render an HTML element.
/// </summary>
public sealed partial class TooltipRoot;

/// <summary>
/// Provides imperative actions for the tooltip.
/// </summary>
public sealed class TooltipRootActions
{
    /// <summary>
    /// Gets or sets the action that unmounts the tooltip manually.
    /// </summary>
    public Action? Unmount { get; internal set; }

    /// <summary>
    /// Gets or sets the action that closes the tooltip imperatively.
    /// </summary>
    public Action? Close { get; internal set; }

    /// <summary>
    /// Gets or sets the action that opens the tooltip imperatively.
    /// </summary>
    public Action? Open { get; internal set; }

    /// <summary>
    /// Gets or sets the action that opens the tooltip imperatively with a specific trigger ID.
    /// </summary>
    public Action<string>? OpenWithTriggerId { get; internal set; }
}

/// <summary>
/// Provides the current payload value to the tooltip's child content.
/// </summary>
/// <param name="Payload">The payload from the active trigger, or <see langword="null"/> if no payload is set.</param>
public readonly record struct TooltipRootPayloadContext(object? Payload);
