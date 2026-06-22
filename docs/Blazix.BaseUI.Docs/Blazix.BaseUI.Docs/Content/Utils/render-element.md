# Render Element

The element-rendering primitive behind every Blazix.BaseUI component part.

Rendered docs: `/utils/render-element`

`RenderElement<TState>` centralizes element output for every part. It merges the consumer's attributes with the component-computed ones, composes `class` and `style` from state, and dispatches to either a consumer-supplied `Render` function or the default HTML element.

You rarely use `RenderElement` directly — components wrap it for you, and you customize output through the [`Render`](/handbook/composition) parameter each part already exposes. It is documented here for authors building their own parts on top of the same primitive.

## Key parameters

- `Tag` — the default HTML element to render.
- `State` — the part's public state, passed to `ClassValue`/`StyleValue` and the `Render` function.
- `Render` — a `RenderFragment<RenderProps<TState>>` that overrides the default element.
- `ClassValue` / `StyleValue` — functions that compute `class`/`style` from `State`.
- `ComponentAttributes` — internal attributes (aria-*, data-*, role) merged on top of the consumer's attributes.
- `Enabled` — when `false`, nothing renders.

## Capturing the element reference

Capture the rendered element with `@ref` and expose it through a public `Element` property:

```razor
<RenderElement TState="MyComponentState"
               Tag="div"
               State="state"
               Render="Render"
               ComponentAttributes="componentAttributes"
               @attributes="AdditionalAttributes"
               @ref="renderElementReference" />

@code {
    private RenderElement<MyComponentState>? renderElementReference;

    public ElementReference? Element => renderElementReference?.Element;
}
```

`RenderElement.Element` is `ElementReference?`, so the public `Element` property returns `null` before the first render.
