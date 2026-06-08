namespace Blazix.BaseUI.Popover;

/// <summary>
/// Groups all parts of the popover and manages open/close state.
/// Does not render an HTML element.
/// </summary>
public sealed partial class PopoverRoot;

/// <summary>
/// Provides imperative actions for the popover.
/// </summary>
public sealed class PopoverRootActions
{
    /// <summary>
    /// Gets or sets the action that unmounts the popover manually.
    /// When specified, the popover will not be unmounted automatically when closed.
    /// </summary>
    public Action? Unmount { get; internal set; }

    /// <summary>
    /// Gets or sets the action that closes the popover imperatively.
    /// </summary>
    public Action? Close { get; internal set; }
}

/// <summary>
/// Provides the current payload value to the popover's child content.
/// </summary>
/// <param name="Payload">The payload from the active trigger, or <see langword="null"/> if no payload is set.</param>
public readonly record struct PopoverRootPayloadContext(object? Payload);
