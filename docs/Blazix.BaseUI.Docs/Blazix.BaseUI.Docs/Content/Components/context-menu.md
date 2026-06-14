# Context Menu

A menu that appears at the pointer on right click or long press.

Rendered docs: `/components/context-menu`

## Anatomy

```razor
@using Blazix.BaseUI.ContextMenu

<ContextMenuRoot>
    <ContextMenuTrigger />
    <ContextMenuPortal>
        <ContextMenuBackdrop />
        <ContextMenuPositioner>
            <ContextMenuPopup>
                <ContextMenuArrow />
                <ContextMenuItem />
                <ContextMenuLinkItem />
                <ContextMenuSeparator />

                <ContextMenuSubmenuRoot>
                    <ContextMenuSubmenuTrigger />
                </ContextMenuSubmenuRoot>

                <ContextMenuGroup>
                    <ContextMenuGroupLabel />
                </ContextMenuGroup>

                <ContextMenuRadioGroup>
                    <ContextMenuRadioItem>
                        <ContextMenuRadioItemIndicator />
                    </ContextMenuRadioItem>
                </ContextMenuRadioGroup>

                <ContextMenuCheckboxItem>
                    <ContextMenuCheckboxItemIndicator />
                </ContextMenuCheckboxItem>
            </ContextMenuPopup>
        </ContextMenuPositioner>
    </ContextMenuPortal>
</ContextMenuRoot>
```

## Examples

### Nested menu

Place a `ContextMenuSubmenuRoot` inside the parent popup and use `ContextMenuSubmenuTrigger` for the item that opens it.

## API reference

Context Menu exposes root, trigger, portal, backdrop, positioner, popup, arrow, item, link item, submenu, group, radio, checkbox-item, and separator parts. Most popup and item parts inherit their behavior from the Menu primitives, but consumers import them from `Blazix.BaseUI.ContextMenu`.
