# Tooltip

Tooltip labels or describes an element on hover or focus.

## Anatomy

```razor
@using Blazix.BaseUI.Tooltip

<TooltipProvider>
    <TooltipRoot>
        <TooltipTrigger>Save</TooltipTrigger>
        <TooltipPortal>
            <TooltipPositioner SideOffset="8">
                <TooltipPopup>
                    <TooltipArrow />
                    Save changes
                </TooltipPopup>
            </TooltipPositioner>
        </TooltipPortal>
    </TooltipRoot>
</TooltipProvider>
```

## Examples

### Detached triggers

Use a handle when the trigger and root cannot be colocated.

```razor
@using Blazix.BaseUI.Tooltip

<TooltipTrigger Handle="@demoTooltip">
    Trigger
</TooltipTrigger>

<TooltipRoot Handle="@demoTooltip">
    <TooltipPortal>
        <TooltipPositioner SideOffset="8">
            <TooltipPopup>Detached tooltip</TooltipPopup>
        </TooltipPositioner>
    </TooltipPortal>
</TooltipRoot>

@code {
    private readonly TooltipHandle demoTooltip = TooltipHandleFactory.CreateHandle();
}
```

### Multiple triggers

Use trigger ids and typed payloads when one tooltip root serves multiple triggers.

```razor
<TooltipTypedTrigger TPayload="string"
                     Handle="@demoTooltip"
                     Id="bold"
                     Payload="@("Bold")">
    B
</TooltipTypedTrigger>
<TooltipTypedTrigger TPayload="string"
                     Handle="@demoTooltip"
                     Id="italic"
                     Payload="@("Italic")">
    I
</TooltipTypedTrigger>

<TooltipRoot Handle="@demoTooltip">
    <ChildContentWithPayload Context="payloadContext">
        <TooltipPortal>
            <TooltipPositioner SideOffset="8">
                <TooltipPopup>
                    @payloadContext.Payload
                </TooltipPopup>
            </TooltipPositioner>
        </TooltipPortal>
    </ChildContentWithPayload>
</TooltipRoot>

@code {
    private readonly TooltipHandle<string> demoTooltip =
        TooltipHandleFactory.CreateHandle<string>();
}
```

### Controlled mode with multiple triggers

Control `Open` and `TriggerId` together when external state determines which trigger owns the tooltip.

### Animating the Tooltip

Animate the positioner, popup, and payload changes through `TooltipViewport`.

## Parts

Parts: `TooltipProvider`, `TooltipRoot`, `TooltipTrigger`, `TooltipTypedTrigger<TPayload>`, `TooltipPortal`, `TooltipPositioner`, `TooltipPopup`, `TooltipArrow`, `TooltipViewport`, and `TooltipHandle`.

Triggers expose `data-popup-open`, `data-trigger-disabled`, and `data-base-ui-tooltip-trigger`. Positioned parts expose `data-side`, `data-align`, `data-open`, `data-closed`, and `data-instant`; popups add `data-starting-style` and `data-ending-style`; arrows add `data-uncentered`.
