# Preview Card

A popup that appears when a link is hovered, showing a preview for sighted users.

Rendered docs: `/components/preview-card`

## Usage guidelines

- Popup content should reflect the link destination.
- Do not put essential information only inside the preview card.
- Preview cards are a visual enhancement for sighted pointer and keyboard users.

## Anatomy

```razor
@using Blazix.BaseUI.PreviewCard

<PreviewCardRoot>
    <PreviewCardTrigger />
    <PreviewCardPortal>
        <PreviewCardBackdrop />
        <PreviewCardPositioner>
            <PreviewCardPopup>
                <PreviewCardArrow />
                <PreviewCardViewport />
            </PreviewCardPopup>
        </PreviewCardPositioner>
    </PreviewCardPortal>
</PreviewCardRoot>
```

## Examples

### Detached triggers

Use a handle when the preview card content is defined away from its link trigger.

```razor
<PreviewCardTrigger Handle="@demoPreviewCard" href="#">
    Link
</PreviewCardTrigger>

<PreviewCardRoot Handle="@demoPreviewCard">
    ...
</PreviewCardRoot>

@code {
    private readonly PreviewCardHandle demoPreviewCard =
        PreviewCardHandleFactory.CreateHandle();
}
```

### Multiple triggers

Multiple links can share one preview card. Typed payloads let each link provide different preview content.

```razor
<PreviewCardTypedTrigger TPayload="PreviewPayload"
                         Handle="@demoPreviewCard"
                         Payload='@new PreviewPayload("Trigger 1")'
                         href="#">
    Trigger 1
</PreviewCardTypedTrigger>

<PreviewCardRoot Handle="@demoPreviewCard">
    <ChildContentWithPayload Context="payloadContext">
        <PreviewCardPortal>
            <PreviewCardPositioner SideOffset="8">
                <PreviewCardPopup>
                    Opened by @((payloadContext.Payload as PreviewPayload)?.Title)
                </PreviewCardPopup>
            </PreviewCardPositioner>
        </PreviewCardPortal>
    </ChildContentWithPayload>
</PreviewCardRoot>

@code {
    private readonly PreviewCardHandle<PreviewPayload> demoPreviewCard =
        PreviewCardHandleFactory.CreateHandle<PreviewPayload>();
}
```

### Controlled mode with multiple triggers

Control `Open` and `TriggerId` together when application state chooses the active link. `OnOpenChange` reports the initiating trigger id.

### Animating the Preview Card

Animate position on `PreviewCardPositioner`, size on `PreviewCardPopup`, and content changes through `PreviewCardViewport`. The viewport wraps current content with `data-current` and exposes direction-aware transition attributes.

## API reference

### Root

Manages open state, active trigger id, payload, transitions, actions, and handles.

| Parameter | Type | Default |
| --- | --- | --- |
| `Open` | `bool?` | `null` |
| `DefaultOpen` | `bool` | `false` |
| `TriggerId` | `string?` | `null` |
| `DefaultTriggerId` | `string?` | `null` |
| `ActionsRef` | `PreviewCardRootActions?` | `null` |
| `Handle` | `IPreviewCardHandle?` | `null` |
| `OpenChanged` | `EventCallback<bool>` | - |
| `OnOpenChange` | `EventCallback<PreviewCardOpenChangeEventArgs>` | - |
| `OnOpenChangeComplete` | `EventCallback<bool>` | - |
| `ChildContent` | `RenderFragment?` | `null` |
| `ChildContentWithPayload` | `RenderFragment<PreviewCardRootPayloadContext>?` | `null` |

### Trigger

Renders an anchor that opens on hover or focus. Parameters: `Delay`, `CloseDelay`, `UseJsHover`, `Id`, `Handle`, `Payload`, `Render`, `ClassValue`, `StyleValue`, and `ChildContent`. The active trigger exposes `data-popup-open`.

### Portal

`KeepMounted`, `Container`, and `ChildContent` control where mounted popup parts render.

### Backdrop

Supports `Render`, `ClassValue`, `StyleValue`, and `ChildContent`. It exposes `data-open`, `data-closed`, `data-starting-style`, and `data-ending-style`.

### Positioner

Positions the popup. Key parameters: `Side`, `Align`, `SideOffset`, `AlignOffset`, `CollisionPadding`, `CollisionBoundary`, `ArrowPadding`, `Sticky`, `DisableAnchorTracking`, `PositionMethod`, `CollisionAvoidance`, `Anchor`, `Render`, `ClassValue`, `StyleValue`, and `ChildContent`.

Data and CSS: `data-side`, `data-align`, `data-open`, `data-closed`, `data-anchor-hidden`, `--available-width`, `--available-height`, `--anchor-width`, `--anchor-height`, `--transform-origin`, `--positioner-width`, and `--positioner-height`.

### Popup

The focusable preview container. Supports `Render`, `ClassValue`, `StyleValue`, and `ChildContent`; exposes placement, open/closed, transition attributes, `--popup-width`, and `--popup-height`.

### Viewport

Wraps changing payload content and exposes `data-current`, `data-previous`, `data-activation-direction`, `data-transitioning`, and `data-instant`.

### Arrow

Supports `Render`, `ClassValue`, `StyleValue`, and `ChildContent`; exposes `data-side`, `data-align`, `data-open`, `data-closed`, and `data-uncentered`.

### Handle

Create handles with `PreviewCardHandleFactory.CreateHandle()` or `CreateHandle<TPayload>()`. Handles expose `IsOpen`, `ActiveTriggerId`, `Open(triggerId)`, and `Close()`.
