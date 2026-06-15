# Popover

An accessible popup anchored to a button.

Rendered docs: `/components/popover`

## Anatomy

```razor
@using Blazix.BaseUI.Popover

<PopoverRoot>
    <PopoverTrigger />
    <PopoverPortal>
        <PopoverBackdrop />
        <PopoverPositioner>
            <PopoverPopup>
                <PopoverArrow />
                <PopoverViewport>
                    <PopoverTitle />
                    <PopoverDescription />
                    <PopoverClose />
                </PopoverViewport>
            </PopoverPopup>
        </PopoverPositioner>
    </PopoverPortal>
</PopoverRoot>
```

## Examples

### Opening on hover

Set `OpenOnHover` on `PopoverTrigger`. Use `Delay` and `CloseDelay` to tune the hover timing.

### Detached triggers

Use a handle when a trigger is rendered outside the root.

```razor
<PopoverTrigger Handle="@demoPopover">Trigger</PopoverTrigger>

<PopoverRoot Handle="@demoPopover">
    <ChildContent Context="payloadContext">
        ...
    </ChildContent>
</PopoverRoot>

@code {
    private readonly PopoverHandle demoPopover = PopoverHandleFactory.CreateHandle();
}
```

### Multiple triggers

Multiple triggers can share one root either by being rendered inside `PopoverRoot` or by sharing a handle. Use trigger ids and typed payloads when each trigger opens different content.

```razor
<PopoverTypedTrigger TPayload="PopoverPayload"
                     Handle="@demoPopover"
                     Id="trigger-1"
                     Payload='@new PopoverPayload("Trigger 1")'>
    Trigger 1
</PopoverTypedTrigger>

<PopoverRoot Handle="@demoPopover">
    <ChildContent Context="payloadContext">
        <PopoverPortal>
            <PopoverPositioner SideOffset="8">
                <PopoverPopup>
                    <PopoverTitle>Popover</PopoverTitle>
                    <PopoverDescription>
                        Opened by @((payloadContext.Payload as PopoverPayload)?.Text)
                    </PopoverDescription>
                </PopoverPopup>
            </PopoverPositioner>
        </PopoverPortal>
    </ChildContent>
</PopoverRoot>

@code {
    private readonly PopoverHandle<PopoverPayload> demoPopover =
        PopoverHandleFactory.CreateHandle<PopoverPayload>();
}
```

### Controlled mode with multiple triggers

Control `Open` and `TriggerId` together when application state owns which trigger is active. Update the active trigger from your trigger event handlers or from the same state that opens the popover.

### Animating the Popover

Animate position on `PopoverPositioner`, size on `PopoverPopup`, and content swaps through `PopoverViewport`. The viewport wraps current content with `data-current` and exposes `data-activation-direction` during trigger changes.

## API reference

### Root

Manages open state, modal mode, active trigger id, payload, transitions, and handles.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Open` | `bool?` | `null` | Controlled open state. |
| `DefaultOpen` | `bool` | `false` | Initial uncontrolled open state. |
| `Modal` | `PopoverModalMode` | `False` | Modal behavior. |
| `TriggerId` | `string?` | `null` | Controlled active trigger id. |
| `DefaultTriggerId` | `string?` | `null` | Initial active trigger id. |
| `ActionsRef` | `PopoverRootActions?` | `null` | Imperative unmount and close actions. |
| `Handle` | `IPopoverHandle?` | `null` | Connects detached triggers. |
| `OpenChanged` | `EventCallback<bool>` | - | Two-way open callback. |
| `OnOpenChange` | `EventCallback<PopoverOpenChangeEventArgs>` | - | Cancelable open-change callback. |
| `OnOpenChangeComplete` | `EventCallback<bool>` | - | Callback after open or close animations complete. |
| `ChildContent` | `RenderFragment<PopoverRootPayloadContext>?` | `null` | Parts rendered with active payload context. |

### Trigger

Renders a button that opens by press, focus, or optional hover.

| Parameter | Type | Default |
| --- | --- | --- |
| `Disabled` | `bool` | `false` |
| `NativeButton` | `bool` | `true` |
| `OpenOnHover` | `bool` | `false` |
| `Delay` | `int` | `300` |
| `CloseDelay` | `int` | `0` |
| `Id` | `string?` | `null` |
| `Handle` | `PopoverHandle<TPayload>?` | `null` |
| `Payload` | `TPayload?` | `default` |
| `Render` | `RenderFragment<RenderProps<PopoverTriggerState>>?` | `null` |
| `ClassValue` | `Func<PopoverTriggerState, string>?` | `null` |
| `StyleValue` | `Func<PopoverTriggerState, string>?` | `null` |
| `ChildContent` | `RenderFragment?` | `null` |

Trigger exposes `data-popup-open`, `data-pressed`, and `data-base-ui-click-trigger`.

### Portal

`KeepMounted`, `Container`, `Render`, and `ChildContent` control where mounted popup parts render.

### Backdrop

Supports `Render`, `ClassValue`, `StyleValue`, and `ChildContent`. It exposes `data-open`, `data-closed`, `data-starting-style`, and `data-ending-style`.

### Positioner

Positions the popup and exposes placement data plus sizing CSS variables.

Key parameters: `Side`, `Align`, `SideOffset`, `SideOffsetFunction`, `AlignOffset`, `AlignOffsetFunction`, `CollisionPadding`, `CollisionPaddingPerSide`, `CollisionBoundary`, `ArrowPadding`, `Sticky`, `DisableAnchorTracking`, `PositionMethod`, `CollisionAvoidance`, `Anchor`, `Render`, `ClassValue`, `StyleValue`, and `ChildContent`.

Data and CSS: `data-side`, `data-align`, `data-open`, `data-closed`, `data-anchor-hidden`, `data-instant`, `--available-width`, `--available-height`, `--anchor-width`, `--anchor-height`, `--transform-origin`, `--positioner-width`, and `--positioner-height`.

### Popup

The focusable dialog container. Parameters: `InitialFocus`, `FinalFocus`, `Render`, `ClassValue`, `StyleValue`, and `ChildContent`.

Popup exposes `data-base-ui-focusable`, placement/open/closed/transition attributes, `data-instant`, `--popup-width`, and `--popup-height`.

### Arrow

Supports `Render`, `ClassValue`, `StyleValue`, and `ChildContent`. It exposes `data-side`, `data-align`, `data-open`, `data-closed`, and `data-uncentered`.

### Title

Registers the popup `aria-labelledby` id. Supports `Render`, `ClassValue`, `StyleValue`, and `ChildContent`.

### Description

Registers the popup `aria-describedby` id. Supports `Render`, `ClassValue`, `StyleValue`, and `ChildContent`.

### Close

Closes the popover. Parameters: `Disabled`, `NativeButton`, `Render`, `ClassValue`, `StyleValue`, and `ChildContent`.

### Viewport

Wraps changing payload content and exposes `data-current`, `data-previous`, `data-activation-direction`, `data-transitioning`, and `data-instant`.

### Handle

Create handles with `PopoverHandleFactory.CreateHandle()` or `CreateHandle<TPayload>()`. Handles expose `IsOpen`, `ActiveTriggerId`, `Open(triggerId)`, and `Close()`.
