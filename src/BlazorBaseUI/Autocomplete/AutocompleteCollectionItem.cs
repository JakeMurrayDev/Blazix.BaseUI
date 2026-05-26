namespace BlazorBaseUI.Autocomplete;

/// <summary>
/// Provides an item and index to <see cref="AutocompleteCollection{TValue}"/>.
/// </summary>
/// <typeparam name="TValue">The autocomplete item value type.</typeparam>
/// <param name="Item">The item value.</param>
/// <param name="Index">The item index.</param>
public readonly record struct AutocompleteCollectionItem<TValue>(TValue Item, int Index);
