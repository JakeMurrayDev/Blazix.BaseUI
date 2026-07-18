# Drawer Upstream Delta & Impact Report

Date: July 18, 2026
Repository: Blazix.BaseUI
Component: Drawer
Source of truth: `.base-ui/packages/react/src/drawer` @ `bdcb685fa` (origin/master, 2026-07-18)
Prior audit baseline: `7c25be77ce8d8bf811a1c5dcc7d41dc48d717c47` (2026-05-27, the last upstream master commit before the original Drawer port audit)

## Delta Window

233 first-parent commits landed upstream between the baseline and head. 47 touch the Drawer's transitive dependency graph, computed by import-graph BFS from `packages/react/src/drawer` through the dialog internals, `internals/*`, `floating-ui-react/*`, `utils/*`, and `packages/utils/*`. Per this repository's audit methodology, shared floating/popup utilities are always swept in addition to the component directory.

## Upstream Delta Catalog — Final Disposition

Verdicts: **PORTED** = landed in this change set. **ALREADY-PRESENT** = verified in the pre-existing Blazor code. **VERIFIED-N/A** = React-only mechanics with the Blazor-native equivalent identified.

### Drawer-behavior commits

| SHA | PR | Change | Disposition |
|---|---|---|---|
| `21b199703` | #4867 | Swipe dismiss drag performance: per-move state moved out of React state; imperative `syncDragStyles` (transition freeze + `translate3d` transform + movement CSS vars, style snapshot/restore); sqrt overshoot damping for snap drags | **PORTED** — swipe engine rewritten in `blazix-baseui-drawer.js` as a 1:1 closure port of `useSwipeDismiss.ts`: all constants (40/10/1/50/16/80 px·ms), transform matrix parsing, direction locking at ≥1 px, directional sqrt damping, snapshot/restore drag styles |
| `5b51e3996` | #4980 | Native swipe drive: viewport claims `touchmove` in a capture-phase native listener; per-frame re-rasterization eliminated | **PORTED** — capture-phase document `touchmove` pipeline added; the Blazor analog (per-`pointermove` JS→.NET `OnSwipeProgress` interop) eliminated: all per-frame visuals (popup transform/vars, backdrop, parent popup, provider indents) are now written directly by JS; .NET receives edge-triggered notifications only |
| `51eb5c7dd` | #5105 | Reliable swipe-to-open: pending-swipe activation (`canStart` re-evaluated per move), displacement preservation from scroll-edge activation, deterministic `outsidePressEnabledRef` guard replacing timing-based suppression | **PORTED** — pending-swipe activation in the engine; the 750 ms `suppressOutsidePressUntil` timer replaced with `setOutsidePressEnabled(rootId, enabled)` exported from `blazix-baseui-dialog.js`, re-enabled by a window-capture `pointerdown` restore listener |
| `6b19b6ea2` | #5112 | Anti-flash on swipe-area re-grab: `swipeAreaActiveRef` prevents `resetSwipe()` from zeroing movement vars while the swipe area drives the popup | **PORTED** — `root.swipeAreaActive` flag; `setRootOpen` skips the engine reset while the swipe area is driving |
| `b167c8561` | #5057 | Commit swipe on primary-button release: `buttons:0` pointermove treated as release; non-primary takeover cancels | **PORTED** — engine tracks `event.buttons`; release-through-move and takeover-cancel implemented |
| `136d38d2c` | #5181 | Swipe math dedup: `closestSnapPointIndex`, touch-reversal velocity guard, release velocity from the last ≤80 ms drag sample (16 ms min window) | **PORTED** — all three behaviors implemented; the swipe-release strength scalar is produced in JS at release, committed to `DrawerViewportContext.SwipeStrength` via `OnSwipeRelease` interop, and rendered live by `DrawerPopup` |
| `c1ceade59` | #4353 | New `Drawer.VirtualKeyboardProvider`: visualViewport keyboard inset CSS var, focused-field scroll alignment with slack, synchronous tap-to-focus inside `touchend` (iOS), hit-slop probing, blocked-tap sentinel, off-screen focus trick, untrusted click redispatch | **PORTED** — new `DrawerVirtualKeyboardProvider` component (`.razor` + `.cs` + `DrawerVirtualKeyboardContext`); the full behavior implemented in JS (`initializeVirtualKeyboardProvider`), wired into the viewport touch pipeline with zero .NET round-trips; constants 60/16/48/10/16 match upstream exactly |
| `16685b208` | #5109 | Root owns the store; `DrawerHandle extends DialogHandle` + factory | **ALREADY-PRESENT** — `Drawer/DrawerHandle.cs`; store ownership is a React mechanic whose Blazor equivalent (`DialogRootContext` owned by `DialogRoot`) predates the window |
| `43d11ebcf` | #5233 | Popup bundle-size restructure; prop wiring verified identical | **VERIFIED-N/A** — React module mechanics |
| `823e1f46c` | #5192 | Shared `popupStateMapping` helpers; attribute output verified unchanged | **VERIFIED-N/A** |
| `4cc8e31ca` #5151, `a47b1df37` #5036, `7a0fd2f84` #5165 | | JSDoc / published-types text only | **VERIFIED-N/A** |
| `bf831b754` | #4920 | Platform detection module; CloseWatcher gate becomes `open && isTopmost && android` | **PORTED (gate) / ALREADY-PRESENT (detection)** — the missing topmost gate implemented: only the topmost open drawer (no open nested dialogs) keeps a CloseWatcher; Android UA fallback regex matches `packages/utils/src/platform/os.ts` |

### Dialog / floating shared-dependency commits

| SHA | PR | Change | Disposition |
|---|---|---|---|
| `d4ee8ae78` | #5024 | Record body-focus target in `FloatingFocusManager` focusout for confirmation-dialog return focus | **ALREADY-PRESENT** — `blazix-baseui-floating.js` focusout branch verified during review (the planning-phase gap claim was stale) |
| `ea3818dec` | #5096 | Outside-press accepts `touchend` with exactly one changed touch and zero remaining touches (touch dismissal without backdrop) | **PORTED** — `blazix-baseui-dialog.js`: touch pointer events excluded from the pointerdown path; `touchend` path added with upstream's event-target resolution |
| `fe2101a31` | #5034 | Dedupe default initial focus | **ALREADY-PRESENT** (behavioral) — single initial-focus path in the Blazor port; browser focus tests pass |
| `e6dc73dfa` | #5093 | Keyboard-close return focus uses `focus({ focusVisible: true })` | **ALREADY-PRESENT** — `blazix-baseui-floating.js` `enqueueFocus`/`returnFocusVisible` |
| `4292cfaa6` | #5030 | Non-modal focus-out close and tabindex management | **ALREADY-PRESENT** — verified in `blazix-baseui-floating.js`; non-modal drawer Playwright tests pass |
| `ae858c8b0` #4945, `3e59f51cb` #5032 | | Dead code removal | **VERIFIED-N/A** |
| `0ce0930e4` | #4925 | Dialog store viewport-element bookkeeping | **ALREADY-PRESENT** — `SetViewportElement` lifecycle verified |
| `e0c111994` | #5110 | Rendered trigger id precedence in `popupStoreUtils` | **VERIFIED-N/A** — React store mechanics; the Blazor trigger registration path (`RegisterTriggerElement`/`SetOpenWithTriggerIdAsync`) already yields rendered-trigger precedence |
| `b9c572962` | #4885 | Reset `preventUnmountOnClose` on reopen | **ALREADY-PRESENT** — `PreventUnmount` verified cleared on reopen |
| `fb464b235` | #4886 | Close popup when active trigger unmounts | **VERIFIED-N/A** — React store subscription mechanics; Blazor disposal path verified equivalent |
| `fd52af405` #5038 | | Tooltip/preview-card store dedupe | **VERIFIED-N/A** — not in the dialog runtime path |
| `c9c90dce2` | #4838 | `useButton`: keyboard clicks on non-`<button>` tags carry modifier keys | **PORTED** — `dispatchClickWithModifiers` in `blazix-baseui-button.js`, including the Space-keyup `defaultPrevented` guard |

### Out-of-path commits

23 commits reached only via barrel imports or files outside the Drawer runtime path (composite/list-nav beyond `COMPOSITE_KEYS`, select/menu focus, hover-popup stores, combobox scroll helpers, `useIdleCallback`, docs infra, lint): all **VERIFIED-N/A** with per-commit justification retained in the working audit log.

## Parity Gaps Resolved (current-state drift beyond commit messages)

| Gap | Resolution |
|---|---|
| G1 Dismiss-swipe engine fidelity (direction locking, damping, reverse-cancel, pending activation, interactive-element ignore, text-selection guards, scroll-edge touch swipes, sampled release velocity) | Engine rewrite (see #4867/#5105/#5181) |
| G2 Touch scroll orchestration (native capture pipeline, cross-axis preservation, range-input/pinch/selection exemptions) | Ported per `DrawerViewport.tsx` |
| G3 Swiping from `[data-drawer-content]` (touch at scroll edge) | Ported: content excluded only for non-touch pointerdown |
| G4 Per-frame JS→.NET interop | Eliminated; edge-triggered notifications only |
| G5 Swipe-release strength scalar | Produced in JS, committed via `OnSwipeRelease`, rendered live |
| G6 Controlled-dismiss revert | `OnSwipeDismiss` returns an authoritative boolean; `false` reverts immediately, `true` commits immediately, rAF fallback only on interop failure |
| G7 Snap-point resolution drift (closest-by-height fallback, first-segment progress denominator) | Ported per `useDrawerSnapPoints.ts` |
| G8 Snap overshoot damping | `getSnapPointSwipeMovement` sqrt damping ported |
| G9 `--drawer-height` during ending transition | Ported |
| G10 Composite-key containment in popup | JS-attached keydown handler stops propagation of composite keys |
| G11 CloseWatcher topmost gate | Ported |
| G12 Popup height measurement semantics (skip-while-stretched, freeze-while-nested) | Ported |
| G13 Swipe-area mid-close re-grab offset | `resolveClosedOffset` reads the live transform |
| G14 `--drawer-keyboard-inset` + VirtualKeyboardProvider | Ported (#4353) |

## Independent Review and Fix Cycle

An adversarial line-level review of the implementation against upstream sources found 1 blocker, 5 major, and 6 minor defects — all integration-level (the ported engine math was verified 1:1). All twelve were fixed and re-verified:

- **B1**: swipe-release strength pipeline was dead end-to-end (no .NET producer; `clearSwipeRelease` invoked at close start; style-patch clobber). Fixed with the `OnSwipeRelease` producer, open-branch-only clearing, and live-value rendering.
- **M1**: the swipe-dismiss rAF re-check raced the SignalR-delayed root reporter on Blazor Server and could revert an accepted dismiss. Fixed by branching on the authoritative interop return value.
- **M2**: dual `IDisposable`/`IAsyncDisposable` on the indent components meant `Dispose()` never ran, leaking the provider `StateChanged` subscription. Fixed (async-only disposal, unsubscribe first).
- **M3**: the outside-press restore listener registered behind the dialog's document-capture check, swallowing the first outside press after a swipe-open. Fixed via window-capture registration.
- **M4**: settled/programmatic snap-point visual state was not re-applied outside gestures. Fixed (`applySettledSnapPointProgress`).
- **M5**: parent nested-swiping state activated at 0 px instead of upstream's 10 px intent gate. Fixed (`updateNestedSwipeActive` port).
- **m1–m6**: dialog touchend target resolution aligned to upstream, mid-gesture disable reset, non-finite release return value, deduped engine progress notifications, live snap-point-offset rendering, per-part test contracts.

## Build-Infrastructure Note

`NU1902` (AngleSharp 1.4.0, GHSA-pgww-w46g-26qg, transitive via bunit 2.5.3) began failing restore mid-audit when the NuGet advisory database refreshed; the repository treats NuGet audit warnings as errors. Resolved with a direct `AngleSharp 1.5.0` pin in `Blazix.BaseUI.Tests.csproj`. This is environmental, unrelated to the Drawer delta, and verified to reproduce on `master`.
