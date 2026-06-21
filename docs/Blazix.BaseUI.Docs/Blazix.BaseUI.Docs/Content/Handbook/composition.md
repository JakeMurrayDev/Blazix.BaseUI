# Composition

A guide to composing Blazix.BaseUI components with your own Blazor components.

Rendered docs: `/handbook/composition`

Every part accepts a `Render` parameter (`RenderFragment<RenderProps<TState>>`) — the Blazor equivalent of a render prop. The `props` argument carries:

- `props.Attributes` — every computed attribute; apply with `builder.AddMultipleAttributes`.
- `props.State` — the part's current public state.
- `props.ChildContent` — the original child content.
- `props.ElementReferenceCallback` — forward with `builder.AddElementReferenceCapture` for the part's interop.

## Changing the rendered element

Return `RenderUtilities.CreateElement("tag", props)` to swap the element; it applies the attributes, forwards the element reference, and places the child content for you.

## Taking full control

Return a builder render fragment to wrap the part, add attributes, or render your own component — apply `props.Attributes`, forward `props.ElementReferenceCallback`, and place `props.ChildContent`.

## Rendering from state

`props.State` lets the render function vary its content based on the part's state.
