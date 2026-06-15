# Navigation Menu

A collection of links and menus for website navigation.

Rendered docs: `/components/navigation-menu`

## Anatomy

```razor
@using Blazix.BaseUI.NavigationMenu

<NavigationMenuRoot>
    <NavigationMenuList>
        <NavigationMenuItem>
            <NavigationMenuTrigger>
                Products
                <NavigationMenuIcon />
            </NavigationMenuTrigger>
            <NavigationMenuContent>
                <NavigationMenuLink Href="/components/menu" />
            </NavigationMenuContent>
        </NavigationMenuItem>
    </NavigationMenuList>

    <NavigationMenuPortal>
        <NavigationMenuBackdrop />
        <NavigationMenuPositioner>
            <NavigationMenuPopup>
                <NavigationMenuArrow />
                <NavigationMenuViewport />
            </NavigationMenuPopup>
        </NavigationMenuPositioner>
    </NavigationMenuPortal>
</NavigationMenuRoot>
```

## Examples

### Nested submenus

Nest a second `NavigationMenuRoot` inside a content panel when a top-level item should open another flyout.

### Nested inline submenus

For second-level navigation that stays in the same panel, omit the nested portal and render the nested list with its own viewport and default active value.

### Custom links

Use `Render` when a router-specific link needs to receive the attributes from `NavigationMenuLink`.

```razor
@using Blazix.BaseUI
@using Blazix.BaseUI.NavigationMenu

<NavigationMenuLink Href="/components/navigation-menu"
                    Render="@RenderLink">
    Navigation Menu
</NavigationMenuLink>

@code {
    private RenderFragment<RenderProps<NavigationMenuLinkState>> RenderLink => props =>
        RenderUtilities.CreateElement("a", props);
}
```

### Large menus

Compress the panel while preventing overflow:

```css
.Content,
.Popup {
    max-height: var(--available-height);
    overflow: hidden;
}
```

Or make the panel scrollable:

```css
.Content,
.Popup {
    max-height: var(--available-height);
}

.Content {
    overflow-y: auto;
}
```

## API reference

### Root

Groups all parts of the navigation menu and manages the active item value. Renders a `<nav>` element for top-level menus and a `<div>` element when nested.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Value` | `string?` | `null` | The controlled active item value. A non-null value displays that item's content. |
| `DefaultValue` | `string?` | `null` | The initial active item value for uncontrolled mode. |
| `Delay` | `int` | `50` | The delay in milliseconds before hover opens an item. |
| `CloseDelay` | `int` | `50` | The delay in milliseconds before hover out closes the menu. |
| `Orientation` | `NavigationMenuOrientation` | `Horizontal` | The visual orientation used for item layout and keyboard navigation. |
| `ValueChanged` | `EventCallback<string?>` | — | Callback invoked when the active value changes, supporting two-way binding. |
| `OnValueChange` | `EventCallback<NavigationMenuValueChangeEventArgs>` | — | Callback invoked when the active value changes. The event can be canceled. |
| `OnOpenChangeComplete` | `EventCallback<bool>` | — | Callback invoked after open or close transition animations complete. |
| `Render` | `RenderFragment<RenderProps<NavigationMenuRootState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<NavigationMenuRootState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<NavigationMenuRootState, string?>?` | `null` | Returns a CSS style based on state. |
| `ChildContent` | `RenderFragment?` | `null` | The navigation menu parts to render. |

| Data attribute | Description |
| --- | --- |
| `data-orientation` | The menu orientation: horizontal or vertical. |

### List

Contains navigation menu items. Renders a `<ul>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<NavigationMenuListState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<NavigationMenuListState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<NavigationMenuListState, string?>?` | `null` | Returns a CSS style based on state. |
| `ChildContent` | `RenderFragment?` | `null` | The navigation menu items. |

### Item

Associates a trigger with content. Renders an `<li>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Value` | `string?` | `null` | The unique value that identifies this item. When omitted, a value is generated. |
| `Render` | `RenderFragment<RenderProps<NavigationMenuItemState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<NavigationMenuItemState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<NavigationMenuItemState, string?>?` | `null` | Returns a CSS style based on state. |
| `ChildContent` | `RenderFragment?` | `null` | The trigger, content, link, or nested root for this item. |

### Trigger

A button that opens or closes the item's content. Renders a `<button>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Disabled` | `bool` | `false` | Determines whether the trigger should ignore user interaction. |
| `Render` | `RenderFragment<RenderProps<NavigationMenuTriggerState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<NavigationMenuTriggerState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<NavigationMenuTriggerState, string?>?` | `null` | Returns a CSS style based on state. |
| `ChildContent` | `RenderFragment?` | `null` | The trigger contents. |

| Data attribute | Description |
| --- | --- |
| `data-blazix-base-ui-navigation-menu-trigger` | Present on trigger elements for JavaScript coordination. |
| `data-popup-open` | Present when this trigger's item is active. |
| `data-pressed` | Present when this trigger's item is active. |

### Icon

An optional indicator inside a trigger. Renders a `<span>` element by default and uses a down caret as default content.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<NavigationMenuIconState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<NavigationMenuIconState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<NavigationMenuIconState, string?>?` | `null` | Returns a CSS style based on state. |
| `ChildContent` | `RenderFragment?` | `null` | The icon contents. |

| Data attribute | Description |
| --- | --- |
| `data-popup-open` | Present when the parent item is active. |

### Content

The content panel associated with an item. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `KeepMounted` | `bool` | `false` | Keeps the content mounted while closed. |
| `Render` | `RenderFragment<RenderProps<NavigationMenuContentState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<NavigationMenuContentState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<NavigationMenuContentState, string?>?` | `null` | Returns a CSS style based on state. |
| `ChildContent` | `RenderFragment?` | `null` | The content to display when the item is active. |

| Data attribute | Description |
| --- | --- |
| `data-open` | Present when this content is active. |
| `data-closed` | Present when this content is inactive. |
| `data-starting-style` | Present when the content is animating in. |
| `data-ending-style` | Present when the content is animating out. |
| `data-activation-direction` | The direction from which the new content was activated. |

### Link

A navigation link. Renders an `<a>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Active` | `bool` | `false` | Marks the link as representing the current page. |
| `CloseOnClick` | `bool` | `false` | Determines whether clicking the link closes the navigation menu. |
| `Href` | `string?` | `null` | The URL the link navigates to. |
| `Render` | `RenderFragment<RenderProps<NavigationMenuLinkState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<NavigationMenuLinkState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<NavigationMenuLinkState, string?>?` | `null` | Returns a CSS style based on state. |
| `ChildContent` | `RenderFragment?` | `null` | The link contents. |

| Data attribute | Description |
| --- | --- |
| `data-active` | Present when `Active` is true. |

### Backdrop

An optional overlay rendered beneath the popup. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<NavigationMenuBackdropState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<NavigationMenuBackdropState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<NavigationMenuBackdropState, string?>?` | `null` | Returns a CSS style based on state. |
| `ChildContent` | `RenderFragment?` | `null` | The backdrop contents. |

| Data attribute | Description |
| --- | --- |
| `data-open` | Present when the menu is open. |
| `data-closed` | Present when the menu is closed. |
| `data-starting-style` | Present when the menu is animating in. |
| `data-ending-style` | Present when the menu is animating out. |

### Portal

Moves popup content into another part of the DOM, by default the document `<body>`. Does not render its own element.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `KeepMounted` | `bool` | `false` | Keeps portal contents mounted while the menu is closed. |
| `Container` | `string` | `"body"` | A CSS selector for the container the portal renders into. |
| `ChildContent` | `RenderFragment?` | `null` | The portal contents. |

### Positioner

Positions the popup relative to the active trigger or explicit anchor. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Side` | `Side` | `Bottom` | Which side of the anchor to place the popup against. |
| `Align` | `Align` | `Center` | How to align the popup relative to the specified side. |
| `SideOffset` | `int` | `0` | The distance between the anchor and popup, in pixels. |
| `AlignOffset` | `int` | `0` | The offset along the alignment axis, in pixels. |
| `CollisionPadding` | `int` | `5` | The padding between the popup and the collision boundary. |
| `CollisionBoundary` | `CollisionBoundary` | `ClippingAncestors` | The boundary the popup is kept within. |
| `CollisionAvoidance` | `CollisionAvoidance?` | `null` | Customizes side, align, and fallback collision behavior. |
| `ArrowPadding` | `int` | `5` | The minimum distance between the arrow and popup edges. |
| `Sticky` | `bool` | `false` | Keeps the popup in view when the anchor scrolls out of view. |
| `DisableAnchorTracking` | `bool` | `false` | Prevents the popup from tracking layout shifts of its anchor. |
| `PositionMethod` | `PositionMethod` | `Absolute` | The CSS position method used for the popup. |
| `Anchor` | `ElementReference?` | `null` | An explicit anchor element. When omitted, the active trigger is used. |
| `Render` | `RenderFragment<RenderProps<NavigationMenuPositionerState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<NavigationMenuPositionerState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<NavigationMenuPositionerState, string?>?` | `null` | Returns a CSS style based on state. |
| `ChildContent` | `RenderFragment?` | `null` | The positioned popup contents. |

| Data attribute | Description |
| --- | --- |
| `data-blazix-base-ui-positioner` | Present on the positioning element. |
| `data-side` | The side the popup is placed on. |
| `data-align` | The alignment of the popup relative to the anchor. |
| `data-positioned` | Present after JavaScript has computed placement. |
| `data-open` | Present when the popup is open. |
| `data-closed` | Present when the popup is closed. |
| `data-anchor-hidden` | Present when the anchor is hidden by collision detection. |
| `data-instant` | Indicates instant positioning behavior. |

| CSS variable | Description |
| --- | --- |
| `--available-width` | The width available before the popup overflows the viewport. |
| `--available-height` | The height available before the popup overflows the viewport. |
| `--anchor-width` | The width of the positioning anchor. |
| `--anchor-height` | The height of the positioning anchor. |
| `--transform-origin` | The origin to use for scale animations. |
| `--positioner-width` | The fixed width of the positioner. |
| `--positioner-height` | The fixed height of the positioner. |

### Popup

The popup shell that hosts the shared viewport. Renders a `<nav>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<NavigationMenuPopupState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<NavigationMenuPopupState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<NavigationMenuPopupState, string?>?` | `null` | Returns a CSS style based on state. |
| `ChildContent` | `RenderFragment?` | `null` | The popup contents. |

| Data attribute | Description |
| --- | --- |
| `data-side` | The side the popup is placed on. |
| `data-align` | The alignment of the popup relative to the anchor. |
| `data-open` | Present when the popup is open. |
| `data-closed` | Present when the popup is closed. |
| `data-anchor-hidden` | Present when the anchor is hidden by collision detection. |
| `data-starting-style` | Present when the popup is animating in. |
| `data-ending-style` | Present when the popup is animating out. |

| CSS variable | Description |
| --- | --- |
| `--popup-width` | The fixed width of the popup. |
| `--popup-height` | The fixed height of the popup. |

### Viewport

Hosts the active content panel and focus guards for content transitions. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<NavigationMenuViewportState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<NavigationMenuViewportState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<NavigationMenuViewportState, string?>?` | `null` | Returns a CSS style based on state. |
| `ChildContent` | `RenderFragment?` | `null` | Additional viewport contents. |

| Data attribute | Description |
| --- | --- |
| `data-blazix-base-ui-navigation-menu-viewport-target` | Present on the inner viewport target element. |

### Arrow

An optional arrow that points from the popup toward the active trigger. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<NavigationMenuArrowState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<NavigationMenuArrowState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<NavigationMenuArrowState, string?>?` | `null` | Returns a CSS style based on state. |
| `ChildContent` | `RenderFragment?` | `null` | The arrow contents. |

| Data attribute | Description |
| --- | --- |
| `data-side` | The side the popup is placed on. |
| `data-align` | The alignment of the popup relative to the anchor. |
| `data-open` | Present when the popup is open. |
| `data-closed` | Present when the popup is closed. |
| `data-uncentered` | Present when the arrow cannot be centered on the anchor. |
