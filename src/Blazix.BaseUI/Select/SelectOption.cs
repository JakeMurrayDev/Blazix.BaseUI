namespace Blazix.BaseUI.Select;

using Microsoft.AspNetCore.Components;

/// <summary>
/// Represents a selectable option with a value and display label.
/// Used with the <c>Items</c> parameter on <see cref="SelectRoot{TValue}"/>
/// to provide label resolution before items mount.
/// </summary>
/// <typeparam name="TValue">The type of value.</typeparam>
/// <param name="Disabled">Whether the option is excluded from typeahead matching.</param>
/// <param name="LabelContent">Optional rich label content rendered by <c>SelectValue</c>.</param>
public sealed record SelectOption<TValue>(
    TValue Value,
    string Label,
    bool Disabled = false,
    RenderFragment? LabelContent = null);
