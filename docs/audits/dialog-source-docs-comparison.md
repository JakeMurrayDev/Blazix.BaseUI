# Dialog Source Docs Comparison

Date: 2026-06-29

## Scope

Compared the React Base UI Dialog documentation / generated API metadata against the Blazix.BaseUI Dialog docs and API metadata.

Source docs (authoritative, PNPM-validated this audit):

- `.base-ui/docs/src/app/(docs)/react/components/dialog/page.mdx`
- `.base-ui/docs/src/app/(docs)/react/components/dialog/types.md`
- `.base-ui/docs/src/app/(docs)/react/components/dialog/types.ts`

Local docs:

- `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Content/Components/dialog.md`
- `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Data/DialogApi.cs`

## Commands

| Command | Result | Log |
| --- | --- | --- |
| `pnpm -C .base-ui docs:validate "(docs)/react/components/dialog"` | Passed; 1 `page.mdx` + 1 `types.md` processed, **no files needed updating** (committed source docs match expected generated output — the API surface is authoritative and in sync). | `docs/audits/logs/dialog-source-docs-validate.log` |
| In-app browser: Blazix docs `http://localhost:5216/components/dialog` (Auto render mode) | Passed; page rendered (`Dialog · Blazix.BaseUI`), hero "Edit profile" demo opened as a modal with backdrop, title, description, form fields, and Cancel/Save changes. | `docs/audits/logs/dialog-in-app-browser.json` |

## API surface comparison (React `types.md` ↔ Blazix `DialogApi.cs`)

| React API | Blazix API | Status |
| --- | --- | --- |
| `Dialog.Root.open` | `DialogRoot.Open` / `OpenChanged` | Present |
| `Dialog.Root.defaultOpen` | `DialogRoot.DefaultOpen` | Present |
| `Dialog.Root.modal` (`boolean \| 'trap-focus'`) | `DialogRoot.Modal` (`DialogModalMode`) | Present |
| `Dialog.Root.onOpenChange` | `DialogRoot.OnOpenChange` / `DialogOpenChangeEventArgs` | Present; args now expose `Trigger`/`TriggerId`/`InteractionType`/`Event` (React `ChangeEventDetails.trigger`/`event`). |
| `Dialog.Root.onOpenChangeComplete` | `DialogRoot.OnOpenChangeComplete` | Present |
| `Dialog.Root.disablePointerDismissal` | `DialogRoot.DisablePointerDismissal` | Present |
| `Dialog.Root.actionsRef` (`{unmount, close}`) | `DialogRoot.ActionsRef` (`DialogRootActions`) | Present |
| `Dialog.Root.handle` | `DialogRoot.Handle` (`IDialogHandle`) | Present |
| `Dialog.Root.triggerId` / `defaultTriggerId` | `DialogRoot.TriggerId` / `DefaultTriggerId` | Present |
| `Dialog.Root.children` (payload render fn) | `DialogRoot.ChildContent` (`RenderFragment<DialogRootPayloadContext>`) | Present |
| `Dialog.Root.ChangeEventReason` (7 reasons) | `DialogOpenChangeReason` | Present (`imperative-action` ↔ `ImperativeAction`; `Swipe`/`CloseWatcher` are unused extras) |
| `Dialog.Trigger` `nativeButton`/`payload`/`id`/`handle`; data `data-popup-open`/`data-disabled` | `DialogTrigger` `NativeButton`/`Payload`/`Id`/`Handle`; same data-attrs | Present |
| `Dialog.Portal` `keepMounted`/`container` | `DialogPortal` `KeepMounted`/`Container` | Present |
| `Dialog.Backdrop` `forceRender`; data `data-open/closed/starting-style/ending-style` | `DialogBackdrop` `ForceRender`; same data-attrs | Present |
| `Dialog.Popup` `initialFocus`/`finalFocus`; data + `--nested-dialogs` | `DialogPopup` `InitialFocus`/`FinalFocus` (`FocusTarget`); same data + CSS var | Present; `finalFocus` callback now evaluated at close with the close interaction type. |
| `Dialog.Viewport`; data incl. `data-nested`/`data-nested-dialog-open` | `DialogViewport`; same data-attrs | Present |
| `Dialog.Title` (`h2`) / `Dialog.Description` (`p`) | `DialogTitle` / `DialogDescription` (+ `Id`) | Present |
| `Dialog.Close` `nativeButton`; `data-disabled` | `DialogClose` `NativeButton`/`Disabled`/`FocusableWhenDisabled`; `data-disabled` | Present (Blazor adds `FocusableWhenDisabled`, a faithful native-button extension) |
| `Dialog.createHandle` / `Dialog.Handle` (`open`/`openWithPayload`/`close`/`isOpen`) | `DialogHandleFactory.CreateHandle()` / `DialogHandle<TPayload>` (`Open`/`OpenWithPayload`/`Close`/`IsOpen`) | Present |
| `InteractionType` (`mouse\|touch\|pen\|keyboard\|''`) | interaction-type strings + `FocusTarget.Callback(Func<string,…>)` | Present |

## Demo coverage (informational)

React docs ship demos for: hero, close-confirmation, nested, inside-scroll, outside-scroll, uncontained, detached-triggers (simple + controlled), multiple triggers. The Blazix docs page currently ships the hero ("Edit profile") demo plus a controlled-open example. Additional demo parity is a documentation-content task, not a component functional gap; the underlying behaviors (nesting, close-confirmation via cancelable `OnOpenChange`, detached/multiple triggers, scrollable viewport) are all implemented and exercised by the bUnit + Playwright suites.

## Assessment

The source documentation comparison surfaced no Dialog implementation gap beyond the two repaired this audit (open-change trigger association and close-interaction-type `finalFocus`). The React source docs are PNPM-validated and in sync; the Blazix API metadata mirrors the React public surface, with the documented Blazor translations (cancelable `OnOpenChange` event model; `FocusTarget` union for `initialFocus`/`finalFocus`; `ElementReference`/`TriggerId` in place of DOM element refs).
