namespace Blazix.BaseUI.Menu;

/// <summary>
/// Provides the current payload value to the menu's child content.
/// </summary>
/// <param name="Payload">The payload from the active trigger, or <see langword="null"/> if no payload is set.</param>
public readonly record struct MenuRootPayloadContext(object? Payload);
