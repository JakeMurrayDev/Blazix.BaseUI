# Portal

Renders its content in a different part of the DOM.

Rendered docs: `/utils/portal`

`Portal` moves its content out of the normal document flow and appends it elsewhere — by default to `<body>`. This is useful for overlays that must escape `overflow: hidden` or stacking-context constraints of their parent. It renders a `<div>`.

## Anatomy

```razor
@using Blazix.BaseUI.Portal

<Portal>
    @* Content rendered into <body> *@
</Portal>
```

## Choosing a target

Set `Target` to any CSS selector to render into a different parent element. It defaults to `"body"`.

```razor
<Portal Target="#overlay-root">
    @* Content rendered into #overlay-root *@
</Portal>
```

## Composition

Like every part, `Portal` accepts a `Render` parameter to change the element it renders or compose it with your own component. See the [Composition](/handbook/composition) guide.
