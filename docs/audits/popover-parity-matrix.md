# Popover Parity Matrix

Date: 2026-06-27

Audit target: Blazix.BaseUI Popover against React Base UI Popover at `.base-ui` commit `748f4228d`.

## Component and Utility Coverage

| React source | React responsibility | Blazix equivalent | Status |
| --- | --- | --- | --- |
| `popover/root/PopoverRoot.tsx` | Open state, controlled/uncontrolled state, active trigger id, modal mode, open reason, interaction type, actions ref, and open-complete callback. | `PopoverRoot.razor`, `PopoverRootContext.cs`, `PopoverHandle.cs`, `blazix-baseui-popover.js`. | Verified |
| `utils/popups/popupStoreUtils.ts` | Rendered trigger id ownership and trigger element association. | `resolveRenderedTriggerId(...)`, `ResolveRenderedTriggerIdOwnershipAsync()`, active trigger reassociation fields. | Repaired and verified |
| `popover/store/PopoverStore.ts` | Popup/positioner/title/description ids, active trigger element, trigger focus target, payload, viewport state, close part count, and modal focus state. | `PopoverRootContext`, `ComponentHandleBase`, `PopoverHandle<TPayload>`, root sync callbacks. | Repaired and verified |
| `popover/trigger/PopoverTrigger.tsx` | Button semantics, disabled handling, hover/click open, payload, active trigger id, `aria-controls`, trigger attrs, and focus guards. | `PopoverTypedTrigger.razor` with root/handle registration, trigger attrs, click/hover interop, focus guards, and handle metadata fallback. | Repaired and verified |
| `floating-ui-react/hooks/useClick.ts` | Press open/close, patient-click threshold, pointer type capture, and outside-press sequencing. | `PopoverTypedTrigger.razor`, `PopoverClose.razor`, `blazix-baseui-popover.js` pointer/open reason state. | Verified |
| `floating-ui-react/hooks/useHover.ts` | Hover open/close delays, safe polygon, and floating-region hover ownership. | `initializeHoverInteraction`, hover callback methods, and shared floating hover module. | Verified |
| `utils/useAnchoredPopupScrollLock.ts` | Modal scroll lock, including touch-open viewport-width tolerance. | `VIEWPORT_WIDTH_TOLERANCE_PX`, `shouldLockScroll`, `syncScrollLock` in `blazix-baseui-popover.js`. | Repaired and verified |
| `popover/portal/PopoverPortal.tsx` | Portal mounting, keep-mounted behavior, and focus guard ownership. | `PopoverPortal.razor` plus shared portal/focus guard implementation. | Verified |
| `popover/positioner/PopoverPositioner.tsx` | Active-trigger anchoring, side/align/offset/collision positioning, anchor-hidden state, inert backdrop, and CSS variables. | `PopoverPositioner.razor`, `PositionerInterop`, shared floating JS, Popover JS root state. | Repaired and verified |
| `popover/popup/PopoverPopup.tsx` | Dialog popup, focusable marker, ARIA labels/descriptions, initial/final focus, close part registration, transition attrs, and hover floating target. | `PopoverPopup.razor`, `FloatingFocusManager`, focus target interop, close count sync, popup attrs. | Repaired and verified |
| `floating-ui-react/components/FloatingFocusManager.tsx` | Initial focus, final focus, restore focus, no-scroll return focus, keyboard-close visible focus (#5093), close interaction type, non-modal focus-out and tabindex self-write marker (#5030), nested floating scopes, and focus guards. | `FloatingFocusManager.razor`, `PopoverFloatingRootContextAdapter.cs`, `CloseInteractionType`, JS focus manager state, `preventScroll`/`focusVisible` return-focus interop, and `data-tabindex` self-write in `handleTabIndex`. | Repaired and verified |
| `popover/backdrop/PopoverBackdrop.tsx` | Backdrop open/closed and transition attrs; hover-open modal backdrop remains unhidden but non-interactive. | `PopoverBackdrop.razor` with mount-only `hidden` and hover `pointer-events: none`. | Repaired and verified |
| `popover/viewport/PopoverViewport.tsx` | Current/previous wrappers, activation direction, instant state, transitioning state, and popup dimension variables. | `PopoverViewport.razor` and viewport JS callbacks. | Verified |
| `popover/close/PopoverClose.tsx` | Close button semantics and close interaction propagation. | `PopoverClose.razor` with pointer type capture and close press event details. | Repaired and verified |
| `popover/arrow/PopoverArrow.tsx` | Arrow side/align/open/closed/uncentered attrs from positioner context. | Existing `PopoverArrow` and positioner context. | Verified |
| `popover/title/PopoverTitle.tsx` | Title id registration and popup `aria-labelledby`. | Existing `PopoverTitle` registration. | Verified |
| `popover/description/PopoverDescription.tsx` | Description id registration and popup `aria-describedby`. | Existing `PopoverDescription` registration. | Verified |

## Attribute Coverage

| Part | Required React attributes/data | Blazix status |
| --- | --- | --- |
| Trigger | `type`, `tabindex`, `aria-haspopup`, `aria-expanded`, `aria-controls`, disabled semantics, `data-popup-open`, `data-pressed`, `data-base-ui-click-trigger`. | Present for contained and detached triggers; covered by bUnit and Playwright. |
| Popup | `id`, `role="dialog"`, `tabindex="-1"`, `data-base-ui-focusable`, `aria-labelledby`, `aria-describedby`, `data-side`, `data-align`, `data-open`, `data-closed`, `data-instant`, `data-starting-style`, `data-ending-style`, `--popup-width`, `--popup-height`. | Present; covered by popup/focus/transition tests. |
| Positioner | `role="presentation"`, `data-side`, `data-align`, `data-open`, `data-closed`, `data-anchor-hidden`, position CSS variables, inert backdrop path. | Present; extra transition style attrs removed. |
| Backdrop | `data-open`, `data-closed`, transition attrs, `hidden` only when unmounted. | Present; hover-open no longer hidden. |
| Internal inert backdrop | `role="presentation"`, `inert`, Base UI inert marker attrs, trigger cutout style. | Present through positioner rendering and JS cutout tracking. |
| Viewport | `data-current`, `data-previous`, `data-activation-direction`, `data-transitioning`, `data-instant`, popup dimension CSS variables. | Present; docs and tests cover space-separated activation axis tokens. |
| Close | Native/non-native button attrs and close press event path. | Present; pointer/keyboard close type propagated. |

## Event and Lifecycle Parity

| React behavior | Blazix implementation | Verification |
| --- | --- | --- |
| `onOpenChange(open, eventDetails)` can cancel state changes and receives trigger/event metadata. | `PopoverOpenChangeEventArgs` exposes `Open`, `Reason`, `Event`, `Trigger`, `TriggerId`, `InteractionType`, and `Cancel()`. | `OnOpenChangeReceivesTriggerEventDetails`, close press tests. |
| Programmatic open returns focus to the active/rendered trigger, not the opener. | Active trigger focus target is tracked through root and handle state; rendered id ownership is resolved after render. | `Focus_ProgrammaticOpenReturnsFocusToActiveTrigger`, #5110 regression test. |
| Close final-focus callback receives close interaction type. | `CloseInteractionType` flows through `PopoverFloatingRootContextAdapter` into `FinalFocusTarget`. | `Focus_FinalFocusCallbackReceivesMouseCloseType`. |
| Close return/final focus does not scroll the document to an offscreen trigger or refocus the previous active popup during trigger handoff. | Shared floating return-focus and Popover final-focus interop call `focus({ preventScroll: true })`; wheel-scroll, visible-trigger, and scrolled-trigger handoff cases are covered. | `ScrollWhilePopoverOpen_DoesNotClosePopover`, `Focus_ReturnToOffscreenTrigger_DoesNotScrollDocument`, `OutsidePressOnAnotherTrigger_DoesNotScrollBackToActivePopover`, `OutsidePressOnVisibleTrigger_OpensNextPopoverWithoutReturningToPrevious`. |
| Keyboard (Escape) close returns focus with a visible focus ring (#5093). | `enqueueFocus` accepts `focusVisible`; dispose return-focus passes `focusVisible: lastInteractionType === 'keyboard'`. | Minified-output verification of `focusVisible`; `Focus_EscapeClose*` and `Tab_FromTrigger_FocusesPopupFirst` confirm no focus-return regression (engine-specific ring is Safari/Firefox-only). |
| Manager's own `tabindex="0"` write does not freeze later tabindex management (#5030). | `handleTabIndex` writes `data-tabindex="0"` alongside `tabindex="0"` so the externally-authored-tabindex early-return is not tripped. | `Tab_FromTrigger_FocusesPopupFirst`; minified-output verification of `setAttribute("data-tabindex","0")`. |
| Modal popup records in-popup focus target on backdrop-press for confirmation return focus (#5024). | Modal-scoped `focusout` recorder calls `addPreviouslyFocusedElement(target)` when focus is lost to `body` from inside the popup. | Modal/backdrop/focus Playwright subset (62 passed); dispose cleanup verified. |
| Initial focus is not stolen back once focus moves to another in-popup element (#4775). | `setInitialFocus` early-returns when focus already moved inside the popup before the scheduled frame runs. | Initial-focus/focus Playwright subset (62 passed). |
| Modal touch scroll lock follows viewport-width tolerance. | JS scroll lock helper mirrors upstream tolerance logic. | Static guard and full Playwright suite. |
| Interop teardown tolerates circuit/module disposal. | Popover interop guards catch `JSDisconnectedException`, `TaskCanceledException`, and `ObjectDisposedException`. | Static guard test. |

## Verification Summary

- `dotnet build Blazix.BaseUI.slnx -v minimal`: passed, 0 warnings (re-verified second pass).
- Popover bUnit: 129 passed with `--no-build` after the full solution build passed (re-verified second pass).
- Popover Playwright: 166 effective passed across Server and WASM (second-pass bulk run reported 4 WASM page-load timeouts, all reconciled green on isolated re-run).
- `bash scripts/lint-rules.sh`: 0 violations.
- PNPM source docs validation: passed, no generated updates.
- In-app browser comparison: source and Blazix docs rendered expected Popover docs/API signals.

Final status: all audited React Popover hooks, utilities, attributes, and upstream fixes have a verified Blazor equivalent or an explicit Blazor API-shape translation.
