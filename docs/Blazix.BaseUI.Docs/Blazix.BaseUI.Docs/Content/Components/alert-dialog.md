# Alert Dialog

A dialog that requires a response from the user.

Rendered docs: `/components/alert-dialog`

The Alert Dialog always renders as a modal with pointer dismissal disabled, so clicking the backdrop will not close it. The user must choose an explicit action or press `Escape`. The popup renders through a portal to the `<body>`, overlaying the whole page.

## Anatomy

```razor
@using Blazix.BaseUI.AlertDialog

<AlertDialogRoot>
    <AlertDialogTrigger />
    <AlertDialogPortal>
        <AlertDialogBackdrop />
        <AlertDialogPopup>
            <AlertDialogTitle />
            <AlertDialogDescription />
            <AlertDialogClose />
        </AlertDialogPopup>
    </AlertDialogPortal>
</AlertDialogRoot>
```

## Examples

### Control the open state

Bind `Open` and `OpenChanged` to drive the dialog from your own state. This lets you open it from anywhere — for example, an external button.

```razor
<button type="button" @onclick="@(() => open = true)">Delete account</button>

<AlertDialogRoot Open="open" OpenChanged="@(value => open = value)">
    <AlertDialogPortal>
        <AlertDialogBackdrop />
        <AlertDialogPopup>
            <AlertDialogTitle>Delete your account?</AlertDialogTitle>
            <AlertDialogDescription>You can't undo this action.</AlertDialogDescription>
            <AlertDialogClose>Cancel</AlertDialogClose>
            <AlertDialogClose>Delete account</AlertDialogClose>
        </AlertDialogPopup>
    </AlertDialogPortal>
</AlertDialogRoot>

@code {
    private bool open;
}
```

## API reference

### Root

Groups all parts of the alert dialog. Always renders as a modal with pointer dismissal disabled. Does not render its own element.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Open` | `bool?` | `null` | Whether the dialog is currently open. When set, the component operates in controlled mode. |
| `DefaultOpen` | `bool` | `false` | Whether the dialog is initially open. To render a controlled dialog, use `Open` instead. |
| `ActionsRef` | `DialogRootActions?` | `null` | A reference to imperative actions (`Close`, `Unmount`) for controlling the dialog programmatically. |
| `Handle` | `IDialogHandle?` | `null` | A handle that associates the dialog with detached triggers placed outside the Root. |
| `OpenChanged` | `EventCallback<bool>` | — | Callback invoked when the open state changes, supporting two-way binding. |
| `OnOpenChange` | `EventCallback<DialogOpenChangeEventArgs>` | — | Callback invoked when the dialog is opened or closed, with the reason for the change. |
| `OnOpenChangeComplete` | `EventCallback<bool>` | — | Callback invoked after any opening or closing animations complete. |
| `TriggerId` | `string?` | `null` | The id of the trigger the dialog is associated with in controlled mode. |
| `DefaultTriggerId` | `string?` | `null` | The id of the trigger the dialog is initially associated with. |
| `ChildContent` | `RenderFragment<DialogRootPayloadContext>?` | `null` | The parts of the alert dialog. The context exposes the payload passed by the active trigger. |

### Trigger

A button that opens the alert dialog. Renders a `<button>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Disabled` | `bool` | `false` | Whether the trigger should ignore user interaction. |
| `NativeButton` | `bool` | `true` | Whether the component renders a native `<button>` element. |
| `Id` | `string?` | `null` | The id of the trigger element. Also used to specify the active trigger in controlled mode. |
| `Handle` | `DialogHandle<object?>?` | `null` | A handle that associates this trigger with a detached dialog. |
| `Payload` | `object?` | `null` | A payload to pass to the dialog when it is opened. |
| `Render` | `RenderFragment<RenderProps<DialogTriggerState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<DialogTriggerState, string>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<DialogTriggerState, string>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-popup-open`, `data-disabled`.

### Portal

Moves the alert dialog into a different part of the DOM, by default the document `<body>`. Does not render its own element.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `KeepMounted` | `bool` | `false` | Whether the portal contents stay mounted in the DOM while the dialog is closed. |
| `Container` | `string` | `"body"` | A CSS selector for the container the portal renders into. |
| `ChildContent` | `RenderFragment?` | `null` | The parts of the alert dialog to portal. |

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

An optional scrollable container that positions the popup. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<DialogViewportState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<DialogViewportState, string>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<DialogViewportState, string>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-open`, `data-closed`, `data-starting-style`, `data-ending-style`, `data-nested`, `data-nested-dialog-open`.

### Popup

A container for the alert dialog contents. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `InitialFocus` | `FocusTarget?` | `null` | The element to focus when the dialog is opened. Defaults to the first focusable element. |
| `FinalFocus` | `FocusTarget?` | `null` | The element to focus when the dialog is closed. Defaults to the trigger. |
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

A button that closes the alert dialog. Renders a `<button>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Disabled` | `bool` | `false` | Whether the close button should ignore user interaction. |
| `NativeButton` | `bool` | `true` | Whether the component renders a native `<button>` element. |
| `FocusableWhenDisabled` | `bool` | `false` | Whether the button remains focusable when disabled. |
| `Render` | `RenderFragment<RenderProps<DialogCloseState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<DialogCloseState, string>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<DialogCloseState, string>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-disabled`.
