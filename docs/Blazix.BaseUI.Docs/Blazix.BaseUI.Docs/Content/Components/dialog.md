# Dialog

A popup that opens on top of the page.

Rendered docs: `/components/dialog`

Dialog provides modal and non-modal overlay behavior with focus management, outside-press dismissal, trigger association, and portal rendering. Use Alert Dialog instead when the user must explicitly confirm or cancel a destructive action.

## Anatomy

```razor
@using Blazix.BaseUI.Dialog

<DialogRoot>
    <DialogTrigger />
    <DialogPortal>
        <DialogBackdrop />
        <DialogPopup>
            <DialogTitle />
            <DialogDescription />
            <DialogClose />
        </DialogPopup>
    </DialogPortal>
</DialogRoot>
```

## Examples

### Control the open state

Bind `Open` and `OpenChanged` when the dialog should be opened from app state or from a control outside the trigger.

```razor
<button type="button" @onclick="@(() => open = true)">Open from state</button>

<DialogRoot Open="open" OpenChanged="@(value => open = value)">
    <DialogPortal>
        <DialogBackdrop />
        <DialogPopup>
            <DialogTitle>Controlled dialog</DialogTitle>
            <DialogDescription>This dialog is opened and closed by Blazor state.</DialogDescription>
            <DialogClose>Close</DialogClose>
        </DialogPopup>
    </DialogPortal>
</DialogRoot>

@code {
    private bool open;
}
```

## API reference

### Root

Groups all parts of the dialog and manages open state, modality, trigger association, and payloads. Does not render its own element.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Open` | `bool?` | `null` | Whether the dialog is currently open. When set, the component operates in controlled mode. |
| `DefaultOpen` | `bool` | `false` | Whether the dialog is initially open. To render a controlled dialog, use `Open` instead. |
| `Modal` | `DialogModalMode` | `True` | Controls modal behavior: `True`, `TrapFocus`, or `False`. |
| `ActionsRef` | `DialogRootActions?` | `null` | A reference to imperative actions (`Close`, `Unmount`) for controlling the dialog programmatically. |
| `Handle` | `IDialogHandle?` | `null` | A handle that associates the dialog with detached triggers placed outside the Root. |
| `OpenChanged` | `EventCallback<bool>` | — | Callback invoked when the open state changes, supporting two-way binding. |
| `OnOpenChange` | `EventCallback<DialogOpenChangeEventArgs>` | — | Callback invoked when the dialog is opened or closed. The event includes the reason and can be canceled. |
| `OnOpenChangeComplete` | `EventCallback<bool>` | — | Callback invoked after any opening or closing animations complete. |
| `DisablePointerDismissal` | `bool` | `false` | Whether outside pointer presses can dismiss the dialog. |
| `TriggerId` | `string?` | `null` | The id of the trigger the dialog is associated with in controlled mode. |
| `DefaultTriggerId` | `string?` | `null` | The id of the trigger the dialog is initially associated with. |
| `ChildContent` | `RenderFragment<DialogRootPayloadContext>?` | `null` | The parts of the dialog. The context exposes the active trigger payload. |

### Trigger

A button that opens or closes the dialog. Renders a `<button>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Disabled` | `bool` | `false` | Whether the trigger should ignore user interaction. |
| `NativeButton` | `bool` | `true` | Whether the component renders a native `<button>` element. |
| `Id` | `string?` | `null` | The id of the trigger element. Also used to specify the active trigger in controlled mode. |
| `Handle` | `DialogHandle<TPayload>?` | `null` | A handle that associates this trigger with a detached dialog. |
| `Payload` | `TPayload?` | `default` | A payload to pass to the dialog when it is opened. |
| `Render` | `RenderFragment<RenderProps<DialogTriggerState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<DialogTriggerState, string>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<DialogTriggerState, string>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-popup-open`, `data-disabled`.

### Portal

Moves the dialog into a different part of the DOM, by default the document `<body>`. Does not render its own element.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `KeepMounted` | `bool` | `false` | Whether the portal contents stay mounted in the DOM while the dialog is closed. |
| `Container` | `string` | `"body"` | A CSS selector for the container the portal renders into. |
| `ChildContent` | `RenderFragment?` | `null` | The parts of the dialog to portal. |

### Backdrop

An overlay displayed beneath the popup. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `ForceRender` | `bool` | `false` | Whether the backdrop is forced to render even when the dialog is nested. |
| `Render` | `RenderFragment<RenderProps<DialogBackdropState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<DialogBackdropState, string>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<DialogBackdropState, string>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-open`, `data-closed`, `data-starting-style`, `data-ending-style`.

### Viewport

An optional presentation container that can position or scroll the popup. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<DialogViewportState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<DialogViewportState, string>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<DialogViewportState, string>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-open`, `data-closed`, `data-starting-style`, `data-ending-style`, `data-nested`, `data-nested-dialog-open`.

### Popup

A focus-managed container for the dialog contents. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `InitialFocus` | `FocusTarget?` | `null` | The focus target when the dialog opens. |
| `FinalFocus` | `FocusTarget?` | `null` | The focus target when the dialog closes. |
| `Render` | `RenderFragment<RenderProps<DialogPopupState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<DialogPopupState, string>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<DialogPopupState, string>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-open`, `data-closed`, `data-starting-style`, `data-ending-style`, `data-nested`, `data-nested-dialog-open`.

CSS variables: `--nested-dialogs`.

### Title

An accessible title that labels the dialog. Renders an `<h2>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Id` | `string?` | auto | The id used for `aria-labelledby`. Generated automatically when omitted. |
| `Render` | `RenderFragment<RenderProps<DialogTitleState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<DialogTitleState, string>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<DialogTitleState, string>?` | `null` | Returns a CSS style based on state. |

### Description

An accessible description for the dialog. Renders a `<p>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Id` | `string?` | auto | The id used for `aria-describedby`. Generated automatically when omitted. |
| `Render` | `RenderFragment<RenderProps<DialogDescriptionState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<DialogDescriptionState, string>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<DialogDescriptionState, string>?` | `null` | Returns a CSS style based on state. |

### Close

A button that closes the dialog. Renders a `<button>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Disabled` | `bool` | `false` | Whether the close button should ignore user interaction. |
| `NativeButton` | `bool` | `true` | Whether the component renders a native `<button>` element. |
| `FocusableWhenDisabled` | `bool` | `false` | Whether the button remains focusable when disabled. |
| `Render` | `RenderFragment<RenderProps<DialogCloseState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<DialogCloseState, string>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<DialogCloseState, string>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-disabled`.
