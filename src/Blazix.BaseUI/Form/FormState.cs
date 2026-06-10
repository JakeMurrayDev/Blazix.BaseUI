namespace Blazix.BaseUI.Form;

/// <summary>
/// Represents the state of the <see cref="FormRoot"/> component.
/// </summary>
public readonly record struct FormState
{
    internal static FormState Default { get; } = new();
}
