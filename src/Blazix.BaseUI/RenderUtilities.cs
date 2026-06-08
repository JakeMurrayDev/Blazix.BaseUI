using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Blazix.BaseUI;

/// <summary>
/// Provides factory methods for creating <see cref="RenderFragment"/> instances
/// that render HTML elements or Blazor components with pre-computed attributes.
/// Intended for use inside <c>Render</c> prop functions.
/// </summary>
public static class RenderUtilities
{
    /// <summary>
    /// Creates a <see cref="RenderFragment"/> that renders an HTML element
    /// with the specified tag, attributes, element reference callback, and child content.
    /// </summary>
    /// <param name="tag">The HTML element tag name (e.g. <c>"button"</c>, <c>"div"</c>).</param>
    /// <param name="attributes">The merged attribute dictionary to splat on the element.</param>
    /// <param name="elementReferenceCallback">
    /// An optional callback that captures the <see cref="ElementReference"/> of the rendered element.
    /// </param>
    /// <param name="childContent">Optional child content to render inside the element.</param>
    public static RenderFragment CreateElement(
        string tag,
        IReadOnlyDictionary<string, object> attributes,
        Action<ElementReference>? elementReferenceCallback,
        RenderFragment? childContent) => (RenderTreeBuilder builder) =>
    {
        builder.OpenElement(0, tag);
        builder.AddMultipleAttributes(1, attributes);
        if (elementReferenceCallback is not null)
            builder.AddElementReferenceCapture(2, elementReferenceCallback);
        builder.AddContent(3, childContent);
        builder.CloseElement();
    };

    /// <summary>
    /// Creates a <see cref="RenderFragment"/> that renders an HTML element
    /// using the attributes, element reference callback, and child content
    /// from the specified <see cref="RenderProps{TState}"/>.
    /// </summary>
    /// <param name="tag">The HTML element tag name.</param>
    /// <param name="props">The render props containing attributes, callback, and child content.</param>
    public static RenderFragment CreateElement<TState>(
        string tag,
        RenderProps<TState> props) =>
        CreateElement(tag, props.Attributes, props.ElementReferenceCallback, props.ChildContent);

    /// <summary>
    /// Creates a <see cref="RenderFragment"/> that renders an HTML element
    /// using the attributes and child content from the specified <see cref="RenderProps{TState}"/>,
    /// combining its element reference callback with an additional one.
    /// </summary>
    /// <param name="tag">The HTML element tag name.</param>
    /// <param name="props">The render props containing attributes, callback, and child content.</param>
    /// <param name="additionalCallback">
    /// An additional callback to invoke with the element reference,
    /// combined with the one already present in <paramref name="props"/>.
    /// </param>
    public static RenderFragment CreateElement<TState>(
        string tag,
        RenderProps<TState> props,
        Action<ElementReference>? additionalCallback) =>
        CreateElement(
            tag,
            props.Attributes,
            Combine(props.ElementReferenceCallback, additionalCallback),
            props.ChildContent);

    /// <summary>
    /// Creates a <see cref="RenderFragment"/> that renders a Blazor component
    /// with the specified type, attributes, and child content.
    /// </summary>
    /// <param name="componentType">The component <see cref="Type"/> to render.</param>
    /// <param name="attributes">
    /// The attribute dictionary. Entries matching component parameter names are set as parameters;
    /// the rest flow into <c>AdditionalAttributes</c>.
    /// </param>
    /// <param name="childContent">Optional child content passed as the <c>ChildContent</c> parameter.</param>
    public static RenderFragment CreateComponent(
        Type componentType,
        IReadOnlyDictionary<string, object> attributes,
        RenderFragment? childContent) => (RenderTreeBuilder builder) =>
    {
        builder.OpenComponent(0, componentType);
        builder.AddMultipleAttributes(1, attributes);
        if (childContent is not null)
            builder.AddAttribute(2, "ChildContent", childContent);
        builder.CloseComponent();
    };

    /// <summary>
    /// Creates a <see cref="RenderFragment"/> that renders a Blazor component
    /// using the attributes and child content from the specified <see cref="RenderProps{TState}"/>.
    /// </summary>
    /// <param name="componentType">The component <see cref="Type"/> to render.</param>
    /// <param name="props">The render props containing attributes and child content.</param>
    public static RenderFragment CreateComponent<TState>(
        Type componentType,
        RenderProps<TState> props) =>
        CreateComponent(componentType, props.Attributes, props.ChildContent);

    private static Action<ElementReference>? Combine(
        Action<ElementReference>? first,
        Action<ElementReference>? second) => (first, second) switch
    {
        (not null, not null) => el => { first(el); second(el); },
        (not null, null) => first,
        (null, not null) => second,
        _ => null
    };
}
