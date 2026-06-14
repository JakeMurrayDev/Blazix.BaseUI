# Menu

A list of actions in a dropdown, enhanced with keyboard navigation.

Rendered docs: `/components/menu`

## Anatomy

```razor
@using Blazix.BaseUI.Menu

<MenuRoot Context="_">
    <MenuTrigger />
    <MenuPortal>
        <MenuBackdrop />
        <MenuPositioner>
            <MenuPopup>
                <MenuArrow />
                <MenuItem />
                <MenuLinkItem />

                <MenuSubmenuRoot>
                    <MenuSubmenuTrigger />
                </MenuSubmenuRoot>

                <MenuGroup>
                    <MenuGroupLabel />
                </MenuGroup>

                <MenuRadioGroup>
                    <MenuRadioItem>
                        <MenuRadioItemIndicator />
                    </MenuRadioItem>
                </MenuRadioGroup>

                <MenuCheckboxItem>
                    <MenuCheckboxItemIndicator />
                </MenuCheckboxItem>

                <MenuViewport />
            </MenuPopup>
        </MenuPositioner>
    </MenuPortal>
</MenuRoot>
```

## Examples

### Open on hover

Add `OpenOnHover` to the trigger when pointer hover should open the menu. Use `Delay` and `CloseDelay` to tune the timing.

```razor
<MenuRoot Context="_">
    <MenuTrigger OpenOnHover="true">Add to playlist</MenuTrigger>
    <MenuPortal>
        <MenuPositioner SideOffset="8">
            <MenuPopup>
                <MenuItem>Get Up!</MenuItem>
                <MenuItem>Inside Out</MenuItem>
                <MenuItem>Night Beats</MenuItem>
            </MenuPopup>
        </MenuPositioner>
    </MenuPortal>
</MenuRoot>
```

### Checkbox items

Use `MenuCheckboxItem` and `MenuCheckboxItemIndicator` for settings that toggle on or off.

```razor
<MenuCheckboxItem @bind-Checked="showMinimap">
    <MenuCheckboxItemIndicator>✓</MenuCheckboxItemIndicator>
    Minimap
</MenuCheckboxItem>
```

### Radio items

Use `MenuRadioGroup`, `MenuRadioItem`, and `MenuRadioItemIndicator` for one-of-many selections.

```razor
<MenuRadioGroup @bind-Value="sortBy">
    <MenuRadioItem Value="@("date")">
        <MenuRadioItemIndicator>✓</MenuRadioItemIndicator>
        Date
    </MenuRadioItem>
    <MenuRadioItem Value="@("name")">
        <MenuRadioItemIndicator>✓</MenuRadioItemIndicator>
        Name
    </MenuRadioItem>
</MenuRadioGroup>
```

### Close on click

Set `CloseOnClick` when an item should override its default close behavior.

```razor
// Close the menu when a checkbox item is clicked.
<MenuCheckboxItem CloseOnClick="true" />

// Keep the menu open when a normal item is clicked.
<MenuItem CloseOnClick="false" />
```

### Group labels

Use `MenuGroupLabel` inside a `MenuGroup` or `MenuRadioGroup` to label a section.

```razor
<MenuGroup>
    <MenuGroupLabel>Workspace</MenuGroupLabel>
    <MenuCheckboxItem>Minimap</MenuCheckboxItem>
    <MenuCheckboxItem>Search</MenuCheckboxItem>
</MenuGroup>
```

### Nested menu

Place a `MenuSubmenuRoot` inside a parent popup and use `MenuSubmenuTrigger` as the item that opens the nested popup.

```razor
<MenuRoot Context="_">
    <MenuTrigger>Song</MenuTrigger>
    <MenuPortal>
        <MenuPositioner SideOffset="8">
            <MenuPopup>
                <MenuItem>Add to Library</MenuItem>
                <MenuSubmenuRoot>
                    <MenuSubmenuTrigger>Add to Playlist</MenuSubmenuTrigger>
                    <MenuPortal>
                        <MenuPositioner SideOffset="-4" AlignOffset="-4">
                            <MenuPopup>
                                <MenuItem>Get Up!</MenuItem>
                                <MenuItem>Inside Out</MenuItem>
                                <MenuItem>Night Beats</MenuItem>
                            </MenuPopup>
                        </MenuPositioner>
                    </MenuPortal>
                </MenuSubmenuRoot>
            </MenuPopup>
        </MenuPositioner>
    </MenuPortal>
</MenuRoot>
```

### Navigate to another page

Use `MenuLinkItem` when a menu item should render as an anchor.

```razor
<MenuLinkItem href="/components/menu">
    Go to Menu docs
</MenuLinkItem>
```

### Open a dialog

Control the dialog state and open it from the menu item's click handler.

```razor
@using Blazix.BaseUI.Dialog
@using Blazix.BaseUI.Menu

<MenuRoot Context="_">
    <MenuTrigger>Open menu</MenuTrigger>
    <MenuPortal>
        <MenuPositioner>
            <MenuPopup>
                <MenuItem @onclick="() => dialogOpen = true">
                    Open dialog
                </MenuItem>
            </MenuPopup>
        </MenuPositioner>
    </MenuPortal>
</MenuRoot>

<DialogRoot @bind-Open="dialogOpen">
    <DialogPortal>
        <DialogBackdrop />
        <DialogPopup>
            ...
        </DialogPopup>
    </DialogPortal>
</DialogRoot>

@code {
    private bool dialogOpen;
}
```

### Detached triggers

Create a menu handle and pass it to both a detached trigger and the root when the trigger lives outside the root's render tree.

```razor
<MenuTrigger Handle="projectMenu">
    Actions
</MenuTrigger>

<MenuRoot Handle="projectMenu" Context="_">
    <MenuPortal>
        <MenuPositioner>
            <MenuPopup>
                <MenuItem>Edit</MenuItem>
                <MenuItem>Share</MenuItem>
            </MenuPopup>
        </MenuPositioner>
    </MenuPortal>
</MenuRoot>

@code {
    private readonly MenuHandle projectMenu = MenuHandleFactory.CreateHandle();
}
```

### Multiple triggers

A single root can be opened by more than one trigger. Use payloads when the menu content should depend on the trigger that opened it.

```razor
<MenuRoot Context="menu">
    <MenuTrigger Payload="@((object?)"File")">File</MenuTrigger>
    <MenuTrigger Payload="@((object?)"Edit")">Edit</MenuTrigger>

    <MenuPortal>
        <MenuPositioner>
            <MenuPopup>
                <MenuViewport>
                    <MenuItem>@menu.Payload action</MenuItem>
                </MenuViewport>
            </MenuPopup>
        </MenuPositioner>
    </MenuPortal>
</MenuRoot>
```

### Controlled mode with multiple triggers

Control the root's open state and trigger id when several triggers share one menu.

```razor
<MenuTypedTrigger TPayload="string"
                  id="playback-trigger"
                  Handle="menu"
                  Payload="@("playback")">
    Playback
</MenuTypedTrigger>

<MenuRoot Handle="menu"
          Open="@isOpen"
          OpenChanged="open => isOpen = open"
          TriggerId="@activeTriggerId"
          Context="payload">
    ...
</MenuRoot>

@code {
    private readonly MenuHandle<string> menu = MenuHandleFactory.CreateHandle<string>();
    private bool isOpen;
    private string? activeTriggerId;
}
```

### Arrow

Place `MenuArrow` inside the popup and set `ArrowPadding` on the positioner to keep it away from popup edges.

```razor
<MenuPositioner SideOffset="8" ArrowPadding="8">
    <MenuPopup>
        <MenuArrow />
        <MenuItem>Add to Library</MenuItem>
    </MenuPopup>
</MenuPositioner>
```

### Animating the Menu

With detached triggers, transition the positioner and popup as the active trigger changes. Wrap changing content in `MenuViewport` for direction-aware content transitions.

```razor
<MenuRoot Handle="menu" Modal="MenuModalMode.False" Context="payload">
    <MenuPortal>
        <MenuPositioner class="animated-positioner">
            <MenuPopup class="animated-popup">
                <MenuViewport class="animated-viewport">
                    ...
                </MenuViewport>
            </MenuPopup>
        </MenuPositioner>
    </MenuPortal>
</MenuRoot>
```

## API reference

### Root

Groups all parts of a menu and manages open state, trigger association, payloads, and keyboard navigation. Does not render its own element.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Open` | `bool?` | `null` | The controlled open state. |
| `DefaultOpen` | `bool` | `false` | The uncontrolled open state used on the initial render. |
| `Modal` | `MenuModalMode` | `True` | Controls modal behavior while open. |
| `Direction` | `Direction` | `Undefined` | Text direction for logical submenu positioning. |
| `LoopFocus` | `bool` | `true` | Whether focus wraps through menu items. |
| `Orientation` | `MenuOrientation` | `Vertical` | Visual orientation for roving focus. |
| `HighlightItemOnHover` | `bool` | `true` | Whether pointer movement highlights items. |
| `Disabled` | `bool` | `false` | Whether the menu ignores trigger interaction. |
| `CloseParentOnEsc` | `bool` | `false` | Whether Escape closes the parent menu in a submenu. |
| `TriggerId` | `string?` | `null` | The associated trigger id in controlled trigger-selection scenarios. |
| `DefaultTriggerId` | `string?` | `null` | The initially associated trigger id. |
| `Handle` | `IMenuHandle?` | `null` | Connects detached triggers with this root. |
| `ActionsRef` | `MenuRootActions?` | `null` | Imperative actions for closing or unmounting the menu. |
| `OpenChanged` | `EventCallback<bool>` | — | Supports two-way binding of open state. |
| `OnOpenChange` | `EventCallback<MenuOpenChangeEventArgs>` | — | Fires when the menu opens or closes. |
| `OnOpenChangeComplete` | `EventCallback<bool>` | — | Fires after open or close transitions complete. |
| `ChildContent` | `RenderFragment<MenuRootPayloadContext>?` | `null` | Menu contents with access to the active trigger payload. |

### Trigger

A button that opens the menu. Renders a `<button>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Disabled` | `bool` | `false` | Whether the trigger ignores user interaction. |
| `NativeButton` | `bool` | `true` | Renders a native `<button>` instead of a `<div>`. |
| `OpenOnHover` | `bool` | `false` | Opens the menu when the trigger is hovered. |
| `Delay` | `int` | `100` | Delay before hover open, in milliseconds. |
| `CloseDelay` | `int` | `0` | Delay before hover close, in milliseconds. |
| `Handle` | `IMenuHandle?` | `null` | Connects this trigger with a detached root. |
| `Payload` | `object?` | `null` | Payload passed to the menu when opened. |
| `Render` | `RenderFragment<RenderProps<MenuTriggerState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<MenuTriggerState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<MenuTriggerState, string?>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-popup-open`, `data-pressed`.

### TypedTrigger

A generic trigger that associates a typed payload with the menu. Renders a `<button>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `TPayload` | type parameter | — | Payload type associated with the trigger and handle. |
| `Handle` | `MenuHandle<TPayload>?` | `null` | Typed handle for detached trigger patterns. |
| `Payload` | `TPayload?` | `default` | Typed payload passed to the menu when opened. |
| `Disabled`, `NativeButton`, `OpenOnHover`, `Delay`, `CloseDelay`, `Render`, `ClassValue`, `StyleValue` | same as Trigger |  | Same behavior as `MenuTrigger`. |

### Portal

Moves the menu content into a different part of the DOM, by default the document `<body>`.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `KeepMounted` | `bool` | `false` | Keeps portal contents mounted while the menu is closed. |
| `Container` | `string` | `"body"` | CSS selector for the portal container. |
| `ChildContent` | `RenderFragment?` | `null` | The portal contents. |

### Backdrop

An optional overlay displayed beneath the menu popup.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<MenuBackdropState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<MenuBackdropState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<MenuBackdropState, string?>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-open`, `data-closed`, `data-starting-style`, `data-ending-style`.

### Positioner

Positions the popup relative to the trigger or explicit anchor.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Side` | `Side?` | `null` | Side of the anchor to place the popup against. |
| `Align` | `Align?` | `null` | Alignment relative to the anchor. |
| `SideOffset` | `int` | `0` | Distance between anchor and popup. |
| `AlignOffset` | `int` | `0` | Offset along the alignment axis. |
| `CollisionPadding` | `int` | `5` | Padding from the collision boundary. |
| `CollisionBoundary` | `CollisionBoundary` | `ClippingAncestors` | Boundary the popup is kept within. |
| `CollisionAvoidance` | `CollisionAvoidance?` | `null` | Custom collision behavior. |
| `ArrowPadding` | `int` | `5` | Minimum distance between arrow and popup edges. |
| `Sticky` | `bool` | `false` | Keeps the popup in view while the anchor scrolls. |
| `DisableAnchorTracking` | `bool` | `false` | Disables tracking anchor position changes. |
| `PositionMethod` | `PositionMethod` | `Absolute` | CSS position method used for the popup. |
| `Anchor` | `ElementReference?` | `null` | Explicit anchor element. |
| `Render` | `RenderFragment<RenderProps<MenuPositionerState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<MenuPositionerState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<MenuPositionerState, string?>?` | `null` | Returns a CSS style based on state. |

CSS variables: `--anchor-width`, `--anchor-height`, `--available-width`, `--available-height`, `--transform-origin`.

### Popup

A focus-managed container for menu items.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `FinalFocus` | `FocusTarget?` | `null` | Element to return focus to when the menu closes. |
| `Render` | `RenderFragment<RenderProps<MenuPopupState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<MenuPopupState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<MenuPopupState, string?>?` | `null` | Returns a CSS style based on state. |

Data attributes include `data-side`, `data-align`, `data-open`, `data-closed`, `data-starting-style`, `data-ending-style`, `data-instant`, `data-nested`, and `data-rootownerid`.

### Viewport

An optional content viewport used for animated menu content transitions.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<MenuViewportState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<MenuViewportState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<MenuViewportState, string?>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-activation-direction`, `data-transitioning`, `data-current`, `data-instant`.

### Arrow

An optional arrow that points from the popup toward the anchor.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<MenuArrowState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<MenuArrowState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<MenuArrowState, string?>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-side`, `data-align`, `data-open`, `data-closed`, `data-uncentered`.

### Item

An interactive menu item. Renders a `<div>` element by default, or a `<button>` when `NativeButton` is true.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Disabled` | `bool` | `false` | Whether the item ignores user interaction. |
| `NativeButton` | `bool` | `false` | Renders a native `<button>` instead of a `<div>`. |
| `CloseOnClick` | `bool` | `true` | Whether clicking the item closes the menu. |
| `Label` | `string?` | `null` | Text label for keyboard text navigation. |
| `Id` | `string?` | `null` | Item element id. |
| `Render` | `RenderFragment<RenderProps<MenuItemState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<MenuItemState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<MenuItemState, string?>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-disabled`, `data-highlighted`, `data-label`.

### LinkItem

A menu item that navigates to a URL. Renders an `<a>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Label` | `string?` | `null` | Text label for keyboard text navigation. |
| `CloseOnClick` | `bool` | `false` | Whether clicking the link closes the menu. |
| `Id` | `string?` | `null` | Link item element id. |
| `Render` | `RenderFragment<RenderProps<MenuLinkItemState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<MenuLinkItemState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<MenuLinkItemState, string?>?` | `null` | Returns a CSS style based on state. |

### SubmenuRoot

Groups all parts of a nested menu.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Open` | `bool?` | `null` | Controlled submenu open state. |
| `DefaultOpen` | `bool` | `false` | Initial uncontrolled open state. |
| `Disabled` | `bool` | `false` | Whether the submenu ignores user interaction. |
| `CloseParentOnEsc` | `bool` | `false` | Whether Escape also closes the parent menu. |
| `LoopFocus` | `bool` | `true` | Whether focus loops within the submenu. |
| `HighlightItemOnHover` | `bool` | `true` | Whether pointer movement highlights items. |
| `Orientation` | `MenuOrientation` | `Vertical` | Visual orientation of the submenu. |
| `Direction` | `Direction` | `Undefined` | Text direction for logical positioning. |
| `Handle` | `IMenuHandle?` | `null` | Handle for detached trigger patterns. |
| `TriggerId` | `string?` | `null` | Associated trigger id. |
| `DefaultTriggerId` | `string?` | `null` | Initial associated trigger id. |
| `ActionsRef` | `MenuRootActions?` | `null` | Imperative submenu actions. |
| `OpenChanged` | `EventCallback<bool>` | — | Supports two-way binding. |
| `OnOpenChange` | `EventCallback<MenuOpenChangeEventArgs>` | — | Fires when the submenu opens or closes. |
| `OnOpenChangeComplete` | `EventCallback<bool>` | — | Fires after transitions complete. |

### SubmenuTrigger

A menu item that opens a submenu.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Disabled` | `bool` | `false` | Whether the trigger ignores user interaction. |
| `NativeButton` | `bool` | `false` | Renders a native `<button>` instead of a `<div>`. |
| `OpenOnHover` | `bool` | `true` | Whether hovering opens the submenu. |
| `Delay` | `int` | `100` | Delay before hover open. |
| `CloseDelay` | `int` | `0` | Delay before hover close. |
| `Id` | `string?` | `null` | Trigger element id. |
| `Label` | `string?` | `null` | Text label for keyboard text navigation. |
| `Render` | `RenderFragment<RenderProps<MenuSubmenuTriggerState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<MenuSubmenuTriggerState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<MenuSubmenuTriggerState, string?>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-popup-open`, `data-disabled`, `data-highlighted`, `data-label`.

### Group and GroupLabel

`MenuGroup` groups related items and `MenuGroupLabel` labels a group.

| Part | Parameters |
| --- | --- |
| `MenuGroup` | `Render`, `ClassValue`, `StyleValue`, child content, and additional attributes. |
| `MenuGroupLabel` | `Render`, `ClassValue`, `StyleValue`, child content, and additional attributes. |

### RadioGroup, RadioItem, and RadioItemIndicator

Use radio parts when one item in a set can be selected.

| Part | Key parameters |
| --- | --- |
| `MenuRadioGroup` | `Value`, `DefaultValue`, `Disabled`, `ValueChanged`, `OnValueChange`, `Render`, `ClassValue`, `StyleValue`. |
| `MenuRadioItem` | `Value`, `Disabled`, `NativeButton`, `Label`, `CloseOnClick`, `Id`, `Render`, `ClassValue`, `StyleValue`. |
| `MenuRadioItemIndicator` | `KeepMounted`, `Render`, `ClassValue`, `StyleValue`. |

Radio item data attributes include `data-checked`, `data-unchecked`, `data-disabled`, `data-highlighted`, and `data-label`.

### CheckboxItem and CheckboxItemIndicator

Use checkbox parts for independently toggled menu settings.

| Part | Key parameters |
| --- | --- |
| `MenuCheckboxItem` | `Checked`, `DefaultChecked`, `CloseOnClick`, `OnCheckedChange`, `CheckedChanged`, `Disabled`, `NativeButton`, `Label`, `Id`, `Render`, `ClassValue`, `StyleValue`. |
| `MenuCheckboxItemIndicator` | `KeepMounted`, `Render`, `ClassValue`, `StyleValue`. |

Checkbox item data attributes include `data-checked`, `data-unchecked`, `data-disabled`, `data-highlighted`, and `data-label`.

### Separator

Use the shared `Blazix.BaseUI.Separator.Separator` component as a visual divider between groups of menu items.

### Handle factory

Use `MenuHandleFactory.CreateHandle()` for untyped detached triggers and `MenuHandleFactory.CreateHandle<TPayload>()` for typed payloads.
