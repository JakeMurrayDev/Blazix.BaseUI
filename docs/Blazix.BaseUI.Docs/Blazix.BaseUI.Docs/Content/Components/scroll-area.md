# Scroll Area

A native scroll container with custom scrollbars.

Rendered docs: `/components/scroll-area`

## Anatomy

```razor
@using Blazix.BaseUI.ScrollArea

<ScrollAreaRoot>
    <ScrollAreaViewport>
        <ScrollAreaContent />
    </ScrollAreaViewport>
    <ScrollAreaScrollbar>
        <ScrollAreaThumb />
    </ScrollAreaScrollbar>
    <ScrollAreaCorner />
</ScrollAreaRoot>
```

## Examples

### Both scrollbars

Add a horizontal scrollbar and `ScrollAreaCorner` so the two scrollbars do not intersect.

### Gradient scroll fade

```css
.Viewport {
    mask-image: linear-gradient(
        to bottom,
        transparent 0,
        black min(40px, var(--scroll-area-overflow-y-start)),
        black calc(100% - min(40px, var(--scroll-area-overflow-y-end, 40px))),
        transparent 100%
    );
    mask-repeat: no-repeat;
}
```

When a child needs the viewport variables, opt in explicitly:

```css
.Child {
    --scroll-area-overflow-y-start: inherit;
    --scroll-area-overflow-y-end: inherit;
}
```

### Combining with Tabs

```razor
@using Blazix.BaseUI
@using Blazix.BaseUI.ScrollArea
@using Blazix.BaseUI.Tabs

<TabsRoot TValue="string" DefaultValue="@("overview")">
    <ScrollAreaRoot>
        <TabsList TValue="string" Render="@RenderListAsViewport">
            <TabsTab TValue="string" Value="@("overview")">Overview</TabsTab>
            <TabsIndicator />
        </TabsList>
    </ScrollAreaRoot>
</TabsRoot>

@code {
    private RenderFragment<RenderProps<TabsRootState>> RenderListAsViewport => props =>
        RenderUtilities.CreateComponent(typeof(ScrollAreaViewport), props);
}
```

## API reference

Parts: `ScrollAreaRoot`, `ScrollAreaViewport`, `ScrollAreaContent`, `ScrollAreaScrollbar`, `ScrollAreaThumb`, and `ScrollAreaCorner`.

Root, Viewport, and Content expose overflow data attributes such as `data-scrolling`, `data-has-overflow-x`, `data-has-overflow-y`, and edge attributes. Scrollbar exposes `Orientation`, `KeepMounted`, `data-orientation`, `data-hovering`, and `data-scrolling`. Viewport exposes overflow distance CSS variables.
