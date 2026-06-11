# Popover Parity Matrix

Audit target: Blazor Popover port against React Base UI Popover and floating-ui source.

## Source Files

- React Popover: `.base-ui/packages/react/src/popover`
- React floating utilities: `.base-ui/packages/react/src/floating-ui-react`
- React popup/focus utilities: `.base-ui/packages/react/src/utils`
- Blazor Popover: `src/BlazorBaseUI/Popover`
- Blazor floating utilities: `src/BlazorBaseUI/Floating*`, `src/BlazorBaseUI/Portal`, `src/BlazorBaseUI/wwwroot/blazor-baseui-floating.js`
- Blazor Popover JS: `src/BlazorBaseUI/wwwroot/blazor-baseui-popover.js`
- Blazor tests: `tests/BlazorBaseUI.Tests/Popover` and `tests/BlazorBaseUI.Playwright.Tests/.../Popover`

## Component and Utility Coverage

| React source | React responsibility | Blazor equivalent | Status |
| --- | --- | --- | --- |
| `PopoverRoot.tsx` | Owns open state, controlled/uncontrolled state, modal mode, hover/click delays, payload, transition status, open-complete callback, floating root context, trigger focus guards, and imperative handle. | `PopoverRoot.razor`, `PopoverRootContext.cs`, `blazor-baseui-popover.js` own state, context delegates, trigger registry, default-open hydration, open reason strings, trigger focus guards, transition complete, and handle-like root methods. | Repaired and verified |
| `PopoverStore.ts` | Stores active trigger element, trigger focus target, payload, popup/positioner/title/description ids, instant type, has viewport, close part count, open reason, and floating root context. | `PopoverRootContext` exposes matching state and callbacks; root updates active trigger, payload, popup id, title/description ids, viewport presence, instant type, focus manager modal state, and JS root state. | Repaired and verified |
| `PopoverTrigger.tsx` | Renders button semantics, `aria-haspopup`, `aria-expanded`, `aria-controls`, `data-popup-open`, `data-pressed`, click trigger marker, payload, hover/click open, focus guards, and disabled behavior. | `PopoverTypedTrigger.razor` and trigger JS register trigger/focus target by id, emit all audited attributes, handle click/hover timing, focus guards, disabled state, and payload. | Repaired and verified |
| `useClick.ts` | Opens/closes on press, distinguishes patient click after hover, tracks pointer interaction type, supports stick-if-open, and suppresses unintended outside close. | `blazor-baseui-popover.js` implements patient/impatient threshold, pointerdown sequencing, same-click outside-press suppression, sticky hover click, and active trigger press behavior. | Repaired and verified |
| `useHover.ts` / `useHoverFloatingInteraction` | Opens by mouse hover with delay, closes with safe polygon behavior, keeps floating region interactive, and coordinates with press state. | `PopoverTypedTrigger.razor` plus `blazor-baseui-popover.js` and shared `blazor-baseui-floating.js` implement hover open/close, re-entry recovery, delayed fallback, floating element updates, and modal engagement after click. | Repaired and verified |
| `PopoverPortal.tsx` | Renders only when mounted or keepMounted; wraps `FloatingPortal`; forwards render props and portal attributes; controls non-modal outside focus guards. | `PopoverPortal.razor` uses `BlazorBaseUI.Portal.FloatingPortal`, forwards `Render` and additional attributes, keeps mounted according to root state, and renders outside focus guards when React would. | Repaired and verified |
| `FloatingPortal.tsx` | Provides portal context, outside guards, `aria-owns`, focus inside enable/disable, and render prop support. | `Portal/FloatingPortal.razor` forwards render/attrs and exposes portal context used by `FloatingFocusManager`; focus guard behavior is mirrored with Blazor `FocusGuard`. | Repaired and verified |
| `PopoverPositioner.tsx` | Positions popup against active trigger, supports side/align/offset/collision/sticky/RTL/viewport, sets CSS variables, and keeps mounted when portal requests it. | `PopoverPositioner.razor`, `PositionerInterop`, `blazor-baseui-popover.js`, and `blazor-baseui-floating.js` update active trigger anchors, auto-update subscriptions, CSS variables, data-side/data-align/data-anchor-hidden, and teardown guards. | Repaired and verified |
| `useAnchorPositioning` / floating `autoUpdate` | Recalculates position when anchor, options, viewport, or content changes. | Shared floating JS detects trigger/anchor option changes, rebuilds auto-update subscriptions, and updates the positioner from active root state. | Repaired and verified |
| `PopoverPopup.tsx` | Renders dialog popup with focusable popup props, title/description references, transition attributes, initial/final focus, restore focus, hover floating interaction, toolbar key stop, and close part provider. | `PopoverPopup.razor` emits `role`, `tabindex`, `data-base-ui-focusable`, ARIA references, side/align/open/closed/transition/instant attrs, focus target interop, toolbar key stop, and close part state. | Repaired and verified |
| `FloatingFocusManager.tsx` | Manages initial focus, return/final focus, restore focus, inside guards, portal guards, focus-out close, nested floating scopes, previous/next focus targets, and modal trap. | `FloatingFocusManager.razor` and `blazor-baseui-floating.js` create JS managers with root/node context, previous/next focus targets, descendant floating detection, restore focus, pointerdown cleanup, and modal/non-modal guard behavior. | Repaired and verified |
| `PopoverBackdrop.tsx` | Renders backdrop only when mounted/open and not hover-only modal; emits open/closed and transition attributes. | `PopoverBackdrop.razor` mirrors open/mounted/hover gating, data-open/data-closed, transition attributes, and render prop state. | Repaired and verified |
| Internal modal inert behavior | Modal popover marks outside content inert while preserving trigger cutout behavior. | `PopoverPositioner.razor` renders a valid internal inert backdrop with `data-base-ui-inert` and `data-blazor-base-ui-inert`; JS updates clipping around the active trigger. | Repaired and verified |
| `PopoverViewport.tsx` / `usePopupViewport` | Renders a div viewport, wraps current content in `[data-current]`, remounts on trigger payload changes, exposes activation direction, transitioning, and instant state. | `PopoverViewport.razor` wraps current content, remounts via active trigger/payload tracking, updates activation/transition state from JS, and initializes auto-resize. | Repaired and verified |
| `PopoverArrow.tsx` | Renders arrow and exposes side/align/uncentered data from positioner context. | Existing `PopoverArrow` integration with `PopoverPositionerContext` and positioner JS remains covered by bUnit and Playwright arrow tests. | Verified |
| `PopoverTitle.tsx` | Renders heading, registers id for `aria-labelledby`, and supports render props. | Existing `PopoverTitle` registration and popup ARIA mapping remain covered by bUnit and Playwright title tests. | Verified |
| `PopoverDescription.tsx` | Renders paragraph, registers id for `aria-describedby`, and supports render props. | Existing `PopoverDescription` registration and popup ARIA mapping remain covered by bUnit and Playwright description tests. | Verified |
| `PopoverClose.tsx` | Renders close button semantics and closes with close reason/focus lifecycle. | Existing `PopoverClose` behavior is verified through close, modal, and focus Playwright tests; popup close part count drives modal focus manager mode. | Verified |
| `getDisabledMountTransitionStyles` / transition status mapping | Applies starting/ending/open/closed transition data and instant type attributes. | `TransitionAttributeHelper`, root transition status, popup/positioner/backdrop state, and JS transition complete callbacks mirror the audited transition attributes. | Repaired and verified |
| `FOCUSABLE_POPUP_PROPS` | Adds focusable popup marker for focus utilities. | `PopoverPopup.razor` emits `data-base-ui-focusable=""`; shared focus JS consumes the marker for descendant popup detection. | Repaired and verified |

## Attribute Coverage

| Element | React attributes | Blazor verification |
| --- | --- | --- |
| Trigger | `type`, `tabindex`, `aria-haspopup`, `aria-expanded`, `aria-controls`, `disabled`/`aria-disabled`, `data-popup-open`, `data-pressed`, `data-base-ui-click-trigger` | bUnit trigger tests and Playwright open/close/disabled/hover-click tests |
| Popup | `id`, `role="dialog"`, `tabindex="-1"`, `data-base-ui-focusable`, `aria-labelledby`, `aria-describedby`, `data-side`, `data-align`, `data-open`, `data-closed`, `data-instant`, transition attrs | bUnit popup tests, Playwright title/description/focus/transition tests |
| Portal | Rendered portal element, forwarded attributes, focus guards, `aria-owns` guard path | bUnit viewport/focus guard checks, Playwright tab/focus tests |
| Positioner | `role="presentation"`, side/align/open/closed/anchor-hidden/instant/transition attrs, CSS variables | bUnit positioner tests, Playwright positioning/multi-trigger tests |
| Backdrop | `data-open`, `data-closed`, transition attrs, user attributes | bUnit backdrop tests, modal/non-modal Playwright tests |
| Internal inert backdrop | `role="presentation"`, `data-base-ui-inert`, `data-blazor-base-ui-inert`, `inert`, clipping style | Browser modal probe, modal inert Playwright tests |
| Viewport | `data-activation-direction`, transition state, instant state, inner `[data-current]` wrapper | bUnit viewport tests, multi-trigger viewport Playwright tests |

## Final Assessment

All audited React Popover components, hooks, utilities, state branches, and marker attributes have a Blazor equivalent implemented in C#, component-specific JavaScript, or shared floating JavaScript. DOM-heavy responsibilities remain in JS. Blazor lifecycle code owns state propagation, registration, and disposal. The final validation matrix is green across bUnit, build, lint, JS syntax, browser automation, Server Playwright, WASM Playwright, and the combined Popover Playwright suite.
