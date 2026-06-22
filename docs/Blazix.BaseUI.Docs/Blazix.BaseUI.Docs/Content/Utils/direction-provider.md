# Direction Provider

Enables right-to-left behavior for Blazix.BaseUI components.

Rendered docs: `/utils/direction-provider`

`DirectionProvider` lets descendant components adjust their behavior — arrow-key navigation, alignment, and positioning — for right-to-left text. It does not set the `dir` HTML attribute or `direction` CSS; your own markup or styles must still do that.

## Anatomy

Wrap it around your app or a group of components:

```razor
@using Blazix.BaseUI.DirectionProvider

<DirectionProvider Direction="Direction.Rtl">
    @* Your app or a group of components *@
</DirectionProvider>
```

## Resolving the direction

`Direction` accepts `Ltr`, `Rtl`, or `Undefined` (the default). When left `Undefined`, the direction resolves from `CultureInfo.CurrentCulture` — RTL cultures resolve to `Rtl`, everything else to `Ltr`.

## Setting the visual direction

`DirectionProvider` only feeds behavior. Set the matching `dir` attribute (or `direction` CSS) yourself so the layout flips visually:

```razor
<div dir="rtl">
    <DirectionProvider Direction="Direction.Rtl">
        @* ... *@
    </DirectionProvider>
</div>
```
