# Toolbar

Toolbar groups related controls into one roving-focus command surface.

## Anatomy

```razor
@using Blazix.BaseUI.Toolbar

<ToolbarRoot aria-label="Formatting">
    <ToolbarGroup>
        <ToolbarButton>Bold</ToolbarButton>
        <ToolbarButton>Italic</ToolbarButton>
    </ToolbarGroup>
    <ToolbarSeparator />
    <ToolbarLink href="/docs">Docs</ToolbarLink>
    <ToolbarInput aria-label="Search" />
</ToolbarRoot>
```

## Examples

### Using with Menu

```razor
@using Blazix.BaseUI
@using Blazix.BaseUI.Menu
@using Blazix.BaseUI.Toolbar

<ToolbarRoot aria-label="Text tools">
    <MenuRoot>
        <MenuTrigger Render="@RenderMenuTriggerAsToolbarButton">
            Font
        </MenuTrigger>
        <MenuPortal>
            <MenuPositioner>
                <MenuPopup>
                    <MenuItem>Serif</MenuItem>
                    <MenuItem>Sans serif</MenuItem>
                </MenuPopup>
            </MenuPositioner>
        </MenuPortal>
    </MenuRoot>
</ToolbarRoot>

@code {
    private RenderFragment<RenderProps<MenuTriggerState>> RenderMenuTriggerAsToolbarButton => props =>
        RenderUtilities.CreateComponent(typeof(ToolbarButton), props);
}
```

### Using with Tooltip

```razor
@using Blazix.BaseUI
@using Blazix.BaseUI.Toolbar
@using Blazix.BaseUI.Tooltip

<TooltipProvider>
    <ToolbarRoot aria-label="Formatting">
        <TooltipRoot>
            <TooltipTrigger Render="@RenderTooltipTriggerAsToolbarButton" aria-label="Bold">
                B
            </TooltipTrigger>
            <TooltipPortal>
                <TooltipPositioner SideOffset="8">
                    <TooltipPopup>Bold</TooltipPopup>
                </TooltipPositioner>
            </TooltipPortal>
        </TooltipRoot>
    </ToolbarRoot>
</TooltipProvider>

@code {
    private RenderFragment<RenderProps<TooltipTriggerState>> RenderTooltipTriggerAsToolbarButton => props =>
        RenderUtilities.CreateComponent(typeof(ToolbarButton), props);
}
```

### Using with Number Field

```razor
@using Blazix.BaseUI
@using Blazix.BaseUI.NumberField
@using Blazix.BaseUI.Toolbar

<ToolbarRoot aria-label="Image controls">
    <NumberFieldRoot>
        <NumberFieldDecrement Render="@RenderDecrementAsToolbarButton">-</NumberFieldDecrement>
        <NumberFieldInput Render="@RenderInputAsToolbarInput" aria-label="Zoom percentage" />
        <NumberFieldIncrement Render="@RenderIncrementAsToolbarButton">+</NumberFieldIncrement>
    </NumberFieldRoot>
</ToolbarRoot>

@code {
    private RenderFragment<RenderProps<NumberFieldRootState>> RenderDecrementAsToolbarButton => props =>
        RenderUtilities.CreateComponent(typeof(ToolbarButton), props);

    private RenderFragment<RenderProps<NumberFieldRootState>> RenderInputAsToolbarInput => props =>
        RenderUtilities.CreateComponent(typeof(ToolbarInput), props);

    private RenderFragment<RenderProps<NumberFieldRootState>> RenderIncrementAsToolbarButton => props =>
        RenderUtilities.CreateComponent(typeof(ToolbarButton), props);
}
```

## Parts

Parts: `ToolbarRoot`, `ToolbarButton`, `ToolbarLink`, `ToolbarInput`, `ToolbarGroup`, and `ToolbarSeparator`.

Toolbar supports arrow-key roving focus with `Orientation` and `LoopFocus`. Home and End navigation are not part of the source toolbar behavior.

Toolbar exposes `role="toolbar"`, `aria-orientation`, `data-orientation`, and `data-disabled`. Items expose `data-focusable` when a disabled item remains in the roving-focus order.
