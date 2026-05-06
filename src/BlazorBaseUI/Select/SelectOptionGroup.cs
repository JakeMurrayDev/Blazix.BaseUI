namespace BlazorBaseUI.Select;

/// <summary>
/// Represents a group of selectable options with an optional group label.
/// Used with the <c>ItemGroups</c> parameter on <see cref="SelectRoot{TValue}"/>
/// to provide label resolution across grouped items before they mount.
/// Mirrors the React Base UI <c>{ items: [...] }</c> group shape accepted by
/// <c>resolveSelectedLabel</c>.
/// </summary>
/// <typeparam name="TValue">The type of value.</typeparam>
/// <param name="Label">An optional group label (not used for value resolution).</param>
/// <param name="Items">The options contained in this group.</param>
public sealed record SelectOptionGroup<TValue>(string? Label, IReadOnlyList<SelectOption<TValue>> Items);
