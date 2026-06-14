# Drawer

A dialog that slides in from the edge of the screen.

Rendered docs: `/components/drawer`

Drawer builds on Dialog semantics and adds edge placement, swipe dismissal, optional swipe-open areas, snap points, nested drawer state, and provider-driven page indentation.

## Anatomy

```razor
@using Blazix.BaseUI.Drawer

<DrawerRoot>
    <DrawerTrigger />
    <DrawerPortal>
        <DrawerBackdrop />
        <DrawerViewport>
            <DrawerPopup>
                <DrawerContent>
                    <DrawerTitle />
                    <DrawerDescription />
                    <DrawerClose />
                </DrawerContent>
            </DrawerPopup>
        </DrawerViewport>
    </DrawerPortal>
</DrawerRoot>
```

## Examples

### Snap points

Provide `SnapPoints` to let a bottom drawer rest at preset heights. Bind `SnapPoint` when app state needs to know which point is active.

```razor
<DrawerRoot SnapPoints="snapPoints"
            @bind-SnapPoint="snapPoint"
            DefaultSnapPoint="defaultSnapPoint">
    <DrawerTrigger>Open snap drawer</DrawerTrigger>
    <DrawerPortal>
        <DrawerBackdrop />
        <DrawerViewport>
            <DrawerPopup>
                <DrawerContent>
                    <DrawerTitle>Release checklist</DrawerTitle>
                    <DrawerDescription>Drag the sheet between snap points.</DrawerDescription>
                    <DrawerClose>Done</DrawerClose>
                </DrawerContent>
            </DrawerPopup>
        </DrawerViewport>
    </DrawerPortal>
</DrawerRoot>

@code {
    private readonly IReadOnlyList<DrawerSnapPoint> snapPoints = [(DrawerSnapPoint)"18rem", (DrawerSnapPoint)1];
    private readonly DrawerSnapPoint defaultSnapPoint = (DrawerSnapPoint)"18rem";
    private DrawerSnapPoint? snapPoint = (DrawerSnapPoint)"18rem";
}
```

## API reference

### Root

Groups all drawer parts, delegates dialog accessibility semantics, and manages swipe and snap-point state. Does not render its own element.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Open` | `bool?` | `null` | Whether the drawer is currently open. When set, the component operates in controlled mode. |
| `DefaultOpen` | `bool` | `false` | Whether the drawer is initially open. To render a controlled drawer, use `Open` instead. |
| `Modal` | `DrawerModalMode` | `True` | Controls modal behavior: `True` traps focus, locks scroll, and blocks outside pointer interaction; `TrapFocus` traps focus only; `False` allows outside interaction. |
| `ActionsRef` | `DrawerRootActions?` | `null` | A reference to imperative actions (`Close`, `Unmount`) for controlling the drawer programmatically. |
| `Handle` | `IDialogHandle?` | `null` | A handle that associates the drawer with detached triggers placed outside the Root. |
| `OpenChanged` | `EventCallback<bool>` | — | Callback invoked when the open state changes, supporting two-way binding. |
| `OnOpenChange` | `EventCallback<DrawerOpenChangeEventArgs>` | — | Callback invoked when the drawer is opened or closed, with the reason for the change. The event can be canceled or can prevent unmounting on close. |
| `OnOpenChangeComplete` | `EventCallback<bool>` | — | Callback invoked after opening or closing animations complete. |
| `DisablePointerDismissal` | `bool` | `false` | Determines whether outside pointer presses can dismiss the drawer. |
| `TriggerId` | `string?` | `null` | The id of the trigger the drawer is associated with in controlled mode. |
| `DefaultTriggerId` | `string?` | `null` | The id of the trigger the drawer is initially associated with. |
| `SwipeDirection` | `DrawerSwipeDirection` | `Down` | The direction used to dismiss the drawer by swipe. |
| `SnapToSequentialPoints` | `bool` | `false` | When true, swipe velocity cannot skip over intermediate snap points. |
| `SnapPoints` | `IReadOnlyList<DrawerSnapPoint>?` | `null` | The snap points used to size and position the drawer. Numeric values between 0 and 1 are viewport fractions; larger numbers are pixels; strings support `px` and `rem`. |
| `SnapPoint` | `DrawerSnapPoint?` | `null` | The controlled active snap point. An explicit null value is meaningful and represents no active snap point. |
| `SnapPointChanged` | `EventCallback<DrawerSnapPoint?>` | — | Callback invoked when the active snap point changes, supporting two-way binding. |
| `DefaultSnapPoint` | `DrawerSnapPoint?` | first snap point | The initial snap point when uncontrolled. An explicit null value starts with no active snap point. |
| `OnSnapPointChange` | `EventCallback<DrawerSnapPointChangeEventArgs>` | — | Callback invoked before the active snap point changes. The event can be canceled. |
| `ChildContent` | `RenderFragment<DrawerRootPayloadContext>?` | `null` | The parts of the drawer. The context exposes the payload passed by the active trigger. |

### Provider

Tracks open drawers beneath it so Indent and IndentBackground can react to drawer activity. Does not render its own element.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `ChildContent` | `RenderFragment?` | `null` | The app or page subtree whose drawers should report to this provider. |

### Indent

A visual wrapper that can scale or offset page content while a drawer is open. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<DrawerIndentState>>?` | `null` | Replaces the rendered element with a different tag or composes it with another component. |
| `ClassValue` | `Func<DrawerIndentState, string>?` | `null` | Returns a CSS class based on the provider's active state. |
| `StyleValue` | `Func<DrawerIndentState, string>?` | `null` | Returns a CSS style based on the provider's active state. |

| Data attribute | Description |
| --- | --- |
| `data-active` | Present when at least one drawer under the nearest provider is open. |
| `data-inactive` | Present when no drawer under the nearest provider is open. |

| CSS variable | Description |
| --- | --- |
| `--drawer-swipe-progress` | The current swipe progress for the active drawer. |
| `--drawer-height` | The frontmost open drawer height, when available. |

### IndentBackground

A background layer that can react to provider activity behind an indented page. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<DrawerIndentBackgroundState>>?` | `null` | Replaces the rendered element with a different tag or composes it with another component. |
| `ClassValue` | `Func<DrawerIndentBackgroundState, string>?` | `null` | Returns a CSS class based on the provider's active state. |
| `StyleValue` | `Func<DrawerIndentBackgroundState, string>?` | `null` | Returns a CSS style based on the provider's active state. |

| Data attribute | Description |
| --- | --- |
| `data-active` | Present when at least one drawer under the nearest provider is open. |
| `data-inactive` | Present when no drawer under the nearest provider is open. |

### Trigger

A button that opens or closes the drawer. Renders a `<button>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Disabled` | `bool` | `false` | Whether the trigger should ignore user interaction. |
| `NativeButton` | `bool` | `true` | Whether the component renders a native `<button>` element. Set to false when replacing it with a non-button element. |
| `Id` | `string?` | `null` | The id of the trigger element. Also used to specify the active trigger in controlled mode. |
| `Handle` | `DrawerHandle<TPayload>?` | `null` | A handle that associates this trigger with a detached drawer. |
| `Payload` | `TPayload?` | `default` | A payload to pass to the drawer when it is opened. |
| `Render` | `RenderFragment<RenderProps<DrawerTriggerState>>?` | `null` | Replaces the rendered element with a different tag or composes it with another component. |
| `ClassValue` | `Func<DrawerTriggerState, string>?` | `null` | Returns a CSS class based on the component's state. |
| `StyleValue` | `Func<DrawerTriggerState, string>?` | `null` | Returns a CSS style based on the component's state. |

| Data attribute | Description |
| --- | --- |
| `data-popup-open` | Present when this trigger is associated with the open drawer. |
| `data-disabled` | Present when the trigger is disabled. |

### Portal

Moves the drawer into a different part of the DOM, by default the document `<body>`. Does not render its own element.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `KeepMounted` | `bool` | `false` | Whether the portal contents stay mounted in the DOM while the drawer is closed. |
| `Container` | `string` | `"body"` | A CSS selector for the container the portal renders into. |
| `ChildContent` | `RenderFragment?` | `null` | The parts of the drawer to portal. |

### Backdrop

An overlay displayed beneath the drawer popup. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `ForceRender` | `bool` | `false` | Whether the backdrop is forced to render even when the drawer is nested. |
| `Render` | `RenderFragment<RenderProps<DrawerBackdropState>>?` | `null` | Replaces the rendered element with a different tag or composes it with another component. |
| `ClassValue` | `Func<DrawerBackdropState, string>?` | `null` | Returns a CSS class based on the component's state. |
| `StyleValue` | `Func<DrawerBackdropState, string>?` | `null` | Returns a CSS style based on the component's state. |

| Data attribute | Description |
| --- | --- |
| `data-open` | Present when the drawer is open. |
| `data-closed` | Present when the drawer is closed. |
| `data-starting-style` | Present when the backdrop is animating in. |
| `data-ending-style` | Present when the backdrop is animating out. |

| CSS variable | Description |
| --- | --- |
| `--drawer-swipe-progress` | The current dismiss swipe progress. |
| `--drawer-swipe-strength` | A multiplier used by swipe-aware styles. |

### Viewport

A presentation container that owns swipe-dismiss and snap-point DOM wiring. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<DrawerViewportState>>?` | `null` | Replaces the rendered element with a different tag or composes it with another component. |
| `ClassValue` | `Func<DrawerViewportState, string>?` | `null` | Returns a CSS class based on the component's state. |
| `StyleValue` | `Func<DrawerViewportState, string>?` | `null` | Returns a CSS style based on the component's state. |

| Data attribute | Description |
| --- | --- |
| `data-open` | Present when the drawer is open. |
| `data-closed` | Present when the drawer is closed. |
| `data-starting-style` | Present when the viewport is animating in. |
| `data-ending-style` | Present when the viewport is animating out. |
| `data-nested` | Present when this drawer is nested in another drawer. |

### Popup

A focus-managed container for drawer contents. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `InitialFocus` | `FocusTarget?` | `null` | The focus target when the drawer opens. Supports default, none, element, and callback targets. |
| `FinalFocus` | `FocusTarget?` | `null` | The focus target when the drawer closes. Supports default, none, element, and callback targets. |
| `Render` | `RenderFragment<RenderProps<DrawerPopupState>>?` | `null` | Replaces the rendered element with a different tag or composes it with another component. |
| `ClassValue` | `Func<DrawerPopupState, string>?` | `null` | Returns a CSS class based on the component's state. |
| `StyleValue` | `Func<DrawerPopupState, string>?` | `null` | Returns a CSS style based on the component's state. |

| Data attribute | Description |
| --- | --- |
| `data-open` | Present when the drawer is open. |
| `data-closed` | Present when the drawer is closed. |
| `data-starting-style` | Present when the popup is animating in. |
| `data-ending-style` | Present when the popup is animating out. |
| `data-expanded` | Present when the active snap point is the numeric value 1. |
| `data-swipe-direction` | Indicates the dismiss direction: up, down, left, or right. |
| `data-swiping` | Present while the drawer is being swiped. |
| `data-nested-drawer-open` | Present when this drawer owns an open nested drawer. |
| `data-nested-drawer-swiping` | Present when a nested drawer is being swiped. |

| CSS variable | Description |
| --- | --- |
| `--nested-drawers` | The number of nested drawers currently open beneath this popup. |
| `--drawer-height` | The measured height of this drawer when it owns a nested drawer. |
| `--drawer-frontmost-height` | The height of the frontmost nested drawer. |
| `--drawer-snap-point-offset` | The pixel offset for the active snap point. |
| `--drawer-swipe-progress` | The current dismiss swipe progress. |
| `--drawer-swipe-strength` | A multiplier used by swipe-aware styles. |

### SwipeArea

An optional edge region that can open the drawer with a swipe gesture. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Disabled` | `bool` | `false` | Whether the swipe area should ignore swipe gestures. |
| `SwipeDirection` | `DrawerSwipeDirection?` | opposite root direction | The direction that opens the drawer. Defaults to the opposite of `Root.SwipeDirection`. |
| `Id` | `string?` | auto | The id of the swipe area. It also acts as a trigger id for swipe-open association. |
| `Render` | `RenderFragment<RenderProps<DrawerSwipeAreaState>>?` | `null` | Replaces the rendered element with a different tag or composes it with another component. |
| `ClassValue` | `Func<DrawerSwipeAreaState, string>?` | `null` | Returns a CSS class based on the component's state. |
| `StyleValue` | `Func<DrawerSwipeAreaState, string>?` | `null` | Returns a CSS style based on the component's state. |

| Data attribute | Description |
| --- | --- |
| `data-open` | Present when the drawer is open. |
| `data-closed` | Present when the drawer is closed. |
| `data-disabled` | Present when the swipe area is disabled. |
| `data-swipe-direction` | Indicates the swipe direction that opens the drawer. |
| `data-swiping` | Present while a swipe-open gesture is active. |

### Content

Wraps the drawer content and marks it as ignored by swipe-dismiss gesture starts. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<DrawerContentState>>?` | `null` | Replaces the rendered element with a different tag or composes it with another component. |
| `ClassValue` | `Func<DrawerContentState, string>?` | `null` | Returns a CSS class based on the component's state. |
| `StyleValue` | `Func<DrawerContentState, string>?` | `null` | Returns a CSS style based on the component's state. |

| Data attribute | Description |
| --- | --- |
| `data-drawer-content` | Present on the content element so swipe dismissal ignores gestures that begin inside it. |

### Title

An accessible title that labels the drawer. Renders an `<h2>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Id` | `string?` | auto | The id used for `aria-labelledby`. Generated automatically when omitted. |
| `Render` | `RenderFragment<RenderProps<DrawerTitleState>>?` | `null` | Replaces the rendered element with a different tag or composes it with another component. |
| `ClassValue` | `Func<DrawerTitleState, string>?` | `null` | Returns a CSS class based on the component's state. |
| `StyleValue` | `Func<DrawerTitleState, string>?` | `null` | Returns a CSS style based on the component's state. |

### Description

An accessible description for the drawer. Renders a `<p>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Id` | `string?` | auto | The id used for `aria-describedby`. Generated automatically when omitted. |
| `Render` | `RenderFragment<RenderProps<DrawerDescriptionState>>?` | `null` | Replaces the rendered element with a different tag or composes it with another component. |
| `ClassValue` | `Func<DrawerDescriptionState, string>?` | `null` | Returns a CSS class based on the component's state. |
| `StyleValue` | `Func<DrawerDescriptionState, string>?` | `null` | Returns a CSS style based on the component's state. |

### Close

A button that closes the drawer. Renders a `<button>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Disabled` | `bool` | `false` | Whether the close button should ignore user interaction. |
| `NativeButton` | `bool` | `true` | Whether the component renders a native `<button>` element. Set to false when replacing it with a non-button element. |
| `FocusableWhenDisabled` | `bool` | `false` | Whether the button remains focusable when disabled. |
| `Render` | `RenderFragment<RenderProps<DrawerCloseState>>?` | `null` | Replaces the rendered element with a different tag or composes it with another component. |
| `ClassValue` | `Func<DrawerCloseState, string>?` | `null` | Returns a CSS class based on the component's state. |
| `StyleValue` | `Func<DrawerCloseState, string>?` | `null` | Returns a CSS style based on the component's state. |

| Data attribute | Description |
| --- | --- |
| `data-disabled` | Present when the close button is disabled. |
