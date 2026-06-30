# Dialog Parity Matrix

Date: 2026-06-29 — React Base UI `.base-ui` @ `748f4228d` ↔ Blazix.BaseUI Dialog/AlertDialog.

## Parts

| React part | Element | Blazor equivalent | Status |
| --- | --- | --- | --- |
| `Dialog.Root` (renders nothing) | — | `DialogRoot` (cascades contexts only) | Present |
| `Dialog.Trigger` | `button` | `DialogTrigger` / `DialogTypedTrigger<TPayload>` | Present |
| `Dialog.Portal` | FloatingPortal → `body` | `DialogPortal` (+ internal modal backdrop) | Present |
| `Dialog.Backdrop` | `div` `role=presentation` | `DialogBackdrop` | Present |
| `Dialog.Popup` | `div` (focus-managed) | `DialogPopup` | Present |
| `Dialog.Viewport` | `div` `role=presentation` | `DialogViewport` | Present |
| `Dialog.Title` | `h2` | `DialogTitle` | Present |
| `Dialog.Description` | `p` | `DialogDescription` | Present |
| `Dialog.Close` | `button` | `DialogClose` | Present |
| `Dialog.createHandle` / `Dialog.Handle` | — | `DialogHandleFactory.CreateHandle()` / `DialogHandle<TPayload>` | Present |
| `AlertDialog.*` (mode `alert-dialog`) | reuses Dialog parts | `AlertDialog*` (subclasses; Root forces `Modal=True`, `DisablePointerDismissal=true`, role `alertdialog`) | Present |

## Root props / API

| React | Blazor | Status |
| --- | --- | --- |
| `open` | `Open` (+ `OpenChanged`) | Present |
| `defaultOpen` | `DefaultOpen` | Present |
| `modal` (`true`/`false`/`'trap-focus'`) | `Modal` (`DialogModalMode.True/False/TrapFocus`) | Present |
| `onOpenChange(open, eventDetails)` | `OnOpenChange(DialogOpenChangeEventArgs)` | Present |
| `onOpenChangeComplete(open)` | `OnOpenChangeComplete` | Present |
| `disablePointerDismissal` | `DisablePointerDismissal` | Present |
| `actionsRef` (`{ unmount, close }`) | `ActionsRef` (`DialogRootActions`) | Present |
| `handle` | `Handle` (`IDialogHandle`) | Present |
| `triggerId` | `TriggerId` | Present |
| `defaultTriggerId` | `DefaultTriggerId` | Present |
| `children` payload render fn | `ChildContent` (`RenderFragment<DialogRootPayloadContext>`) | Present |

## ChangeEventDetails / reasons

| React | Blazor | Status |
| --- | --- | --- |
| reasons `trigger-press`/`outside-press`/`escape-key`/`close-press`/`focus-out`/`imperative-action`/`none` | `DialogOpenChangeReason.{TriggerPress,OutsidePress,EscapeKey,ClosePress,FocusOut,ImperativeAction,None}` | Present (`Swipe`/`CloseWatcher` are emitted by the Drawer, which shares this enum; the Dialog itself never emits them) |
| `cancel()` / `isCanceled` | `args.Cancel()` / `IsCanceled` | Present |
| `allowPropagation()` / `isPropagationAllowed` | `args.AllowPropagation()` / `IsPropagationAllowed` (base `OpenChangeEventArgs`) | Present |
| `preventUnmountOnClose()` | `args.PreventUnmountOnClose()` / `PreventUnmount` | Present |
| `trigger` (Element) | `args.Trigger` (`ElementReference?`) + `args.TriggerId` | **Present (repaired this audit)** |
| `event` (DOM event) | `args.Event` (`EventArgs?`) | Present (surface; null on JS-driven dismissal — Blazor interop boundary) |
| — (React-internal) | `args.InteractionType` | Present (Blazor convenience, matches Popover) |

## Per-part attributes / data-attributes / CSS vars

| React signal | Blazor | Status |
| --- | --- | --- |
| Trigger `aria-haspopup="dialog"` (hardcoded, incl. AlertDialog) | same | Present |
| Trigger `aria-expanded`, `aria-controls`, `id`, `data-popup-open`, `data-disabled` | same | Present |
| Backdrop `role=presentation`, `hidden`, `data-open/closed/starting-style/ending-style`, `user-select:none`+`-webkit-`, nested suppression unless `forceRender` | same (`ForceRender`) | Present |
| Popup `id`, `role` (`dialog`/`alertdialog`), `aria-labelledby`, `aria-describedby`, `tabindex=-1`, `data-open/closed/starting-style/ending-style/nested/nested-dialog-open`, `--nested-dialogs` | same | Present |
| Popup `aria-modal` | React omits (uses `markOthers` aria-hidden); Blazor emits `aria-modal="true"` for modal/trap-focus | Divergent (shared `blazix-baseui-floating.js`, cross-component; more-accessible direction; not altered) |
| Viewport `role=presentation`, `hidden`, transition + nested data-attrs, `pointer-events:none` while closed | same | Present |
| Title `h2`+id (→ `aria-labelledby`); Description `p`+id (→ `aria-describedby`); Close `button`+`data-disabled` | same | Present |
| Popup composite-key `stopPropagation` (Arrow/Home/End) | `setupCompositeKeySuppression` in `blazix-baseui-dialog.js` | Present |

## Hooks / utilities / behavior

| React | Blazor | Status |
| --- | --- | --- |
| `useDialogRoot` / `DialogStore` / `DialogInteractions` | `DialogRoot` state + `DialogRootContext` + `ComponentHandleBase` | Present (idiomatic Blazor lifecycle, no manual sync loops) |
| `useDismiss` (escape `isTopmost`, outside-press, touch) | `blazix-baseui-dialog.js` global keydown (topmost) + `setupOutsideClickListener`/`setupBackdropClickListener` | Present |
| `useScrollLock(open && modal===true)` | `acquireScrollLock` (modal `'true'` only) | Present |
| `FloatingFocusManager` (modal trap, initial/return focus, `markOthers`, restore-focus="popup") | shared `createFloatingFocusManager` (`blazix-baseui-floating.js`) via `DialogPopup`/`blazix-baseui-dialog.js` | Present |
| Default touch initial focus → popup (suppress virtual keyboard) | `getInitialFocusTarget` (`isTouchInteraction → floatingElement`) | Present |
| `finalFocus` evaluated at **close** with close interaction type | `FinalFocus` callback deferred at open + re-resolved at close (`ResolveFinalFocusForClose` + `ResolveCloseInteractionType` + JS `setFinalFocusElement`/`cleanupFocusManager` override) | **Present (repaired this audit)** |
| Nested-dialog count propagation (`onNestedDialogOpen/Close`, `isTopmost`) | `OnNestedDialogOpen/Close`, `NestedDialogCount`, topmost-only dismissal | Present |
| `preventUnmountingOnClose` mount retention | `TransitionLifecycleManager` + `PreventUnmountingOnClose` | Present |
| Detached-trigger handle sharing one store | `DialogHandle`/`ComponentHandleBase` subscriber model | Present |

## Upstream fixes (delta)

| Upstream | Blazor status |
| --- | --- |
| `#5093` keyboard-close visible focus | Present (inherited shared `floating.js`) |
| `#5030` tabindex `data-tabindex` marker | Present (inherited) |
| `#5024` modal confirmation return-focus recorder | Present (inherited) |
| `#4775` don't steal initial focus after move-inside | Present (inherited) |
| `#5034` touch initial-focus dedupe | Present (behavior); React-tree refactor N/A |
| `#5096` touch outside-press without backdrop | Accounted (moot — Blazor uses pointerdown/click, never had React's touch-count rejection) |
| `#4970` menu focus flake | N/A (test-only) |
| `#5010` viewport morph reopen | N/A (Dialog viewport has no morph) |
| `#4849` programmatic focus return | Accounted (pre-open `activeElement` capture covers common cases; same as Popover) |
| `#5110` rendered trigger id ownership | Accounted (Blazor trigger registration key == DOM id; mismatch cannot reproduce as in React) |
| `#4945` remove dead popup paths | N/A (never ported; one harmless `order`-branch divergence noted) |

Legend: **Present (repaired this audit)** = gap closed by this audit; Present = already at parity; Accounted = evaluated, no change required with recorded justification; N/A = not applicable to the Blazor architecture; Divergent = intentional/shared cross-component difference.
