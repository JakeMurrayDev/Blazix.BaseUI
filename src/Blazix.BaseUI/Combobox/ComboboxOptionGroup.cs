namespace Blazix.BaseUI.Combobox;

/// <summary>
/// Represents a group of combobox items supplied to <see cref="ComboboxRoot{TValue}"/>.
/// </summary>
/// <typeparam name="TValue">The combobox item value type.</typeparam>
/// <param name="Items">The items in the group.</param>
/// <param name="Label">An optional label for the group.</param>
public sealed record ComboboxOptionGroup<TValue>(
    IReadOnlyList<TValue> Items,
    string? Label = null);
