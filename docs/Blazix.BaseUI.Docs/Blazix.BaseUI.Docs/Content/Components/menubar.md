# Menubar

A menu bar providing commands and options for your application.

Rendered docs: `/components/menubar`

## Anatomy

```razor
@using Blazix.BaseUI.Menu
@using Blazix.BaseUI.MenuBar

<MenuBarRoot>
    <MenuRoot Context="_">
        <MenuTrigger>File</MenuTrigger>
        <MenuPortal>
            <MenuPositioner>
                <MenuPopup>
                    <MenuItem>New</MenuItem>
                    <MenuSubmenuRoot>
                        <MenuSubmenuTrigger>Export</MenuSubmenuTrigger>
                    </MenuSubmenuRoot>
                </MenuPopup>
            </MenuPositioner>
        </MenuPortal>
    </MenuRoot>
</MenuBarRoot>
```

## API reference

### Root

The container for a horizontal or vertical set of menus. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Disabled` | `bool` | `false` | Whether the whole menubar should ignore user interaction. |
| `LoopFocus` | `bool` | `true` | Whether arrow-key focus wraps from the last trigger back to the first. |
| `Modal` | `bool` | `true` | Whether open menus in the menubar use modal behavior. |
| `Orientation` | `Orientation` | `Horizontal` | The orientation of the menubar. |
| `Render` | `RenderFragment<RenderProps<MenuBarRootState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<MenuBarRootState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<MenuBarRootState, string?>?` | `null` | Returns a CSS style based on state. |

| Data attribute | Description |
| --- | --- |
| `data-orientation` | The orientation of the menubar: `horizontal` or `vertical`. |
| `data-has-submenu-open` | Present when any menu in the menubar is open. |
| `data-modal` | Present when the menubar is modal. |
