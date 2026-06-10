namespace Blazix.BaseUI.Button;

/// <summary>
/// Represents the state of a <see cref="Button"/> component.
/// </summary>
/// <param name="Disabled">Whether the button should ignore user interaction.</param>
public sealed record ButtonState(bool Disabled);
