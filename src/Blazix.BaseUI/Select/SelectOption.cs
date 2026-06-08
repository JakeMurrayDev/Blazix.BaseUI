namespace Blazix.BaseUI.Select;

/// <summary>
/// Represents a selectable option with a value and display label.
/// Used with the <c>Items</c> parameter on <see cref="SelectRoot{TValue}"/>
/// to provide label resolution before items mount.
/// </summary>
/// <typeparam name="TValue">The type of value.</typeparam>
public sealed record SelectOption<TValue>(TValue Value, string Label);
