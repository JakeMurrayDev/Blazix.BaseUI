namespace Blazix.BaseUI.Combobox;

/// <summary>
/// Provides an item and index to <see cref="ComboboxCollection{TValue}"/>.
/// </summary>
/// <typeparam name="TValue">The combobox item value type.</typeparam>
/// <param name="Item">The item value.</param>
/// <param name="Index">The item index.</param>
public readonly record struct ComboboxCollectionItem<TValue>(TValue Item, int Index);
