namespace Blazix.BaseUI.Autocomplete;

/// <summary>
/// Represents a group of autocomplete items supplied to <see cref="AutocompleteRoot{TValue}"/>.
/// </summary>
/// <typeparam name="TValue">The autocomplete item value type.</typeparam>
/// <param name="Items">The items in the group.</param>
/// <param name="Label">An optional label for the group.</param>
public sealed record AutocompleteOptionGroup<TValue>(
    IReadOnlyList<TValue> Items,
    string? Label = null);
