using System.Collections.Concurrent;
using System.Reflection;

namespace Blazix.BaseUI.Select;

internal static class SelectValueResolver
{
    private static readonly ConcurrentDictionary<Type, ItemShape> Shapes = new();

    internal static bool TryGetLabel(object? item, out object? label)
    {
        label = null;
        if (item is null)
        {
            return false;
        }

        var property = GetShape(item.GetType()).Label;
        if (property is null)
        {
            return false;
        }

        label = property.GetValue(item);
        return label is not null;
    }

    internal static bool TryGetValue(object? item, out object? value)
    {
        value = null;
        if (item is null)
        {
            return false;
        }

        var shape = GetShape(item.GetType());
        if (shape.Label is null || shape.Value is null)
        {
            return false;
        }

        value = shape.Value.GetValue(item);
        return true;
    }

    private static ItemShape GetShape(Type type) => Shapes.GetOrAdd(type, static currentType =>
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
        var label = currentType.GetProperty("Label", flags) ?? currentType.GetProperty("label", flags);
        var value = currentType.GetProperty("Value", flags) ?? currentType.GetProperty("value", flags);
        return new ItemShape(label, value);
    });

    private readonly record struct ItemShape(PropertyInfo? Label, PropertyInfo? Value);
}
