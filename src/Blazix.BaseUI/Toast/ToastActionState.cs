namespace Blazix.BaseUI.Toast;

/// <summary>
/// Represents the public state of a <see cref="ToastAction"/>.
/// </summary>
/// <param name="Type">The toast type.</param>
public sealed record ToastActionState(string? Type);
