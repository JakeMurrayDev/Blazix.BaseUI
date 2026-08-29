# ContextMenu Upstream Delta & Impact Report

Date: August 21, 2026
Repository: Blazix.BaseUI
Component: ContextMenu
Source of truth: `.base-ui/packages/react/src/context-menu` @ `1a2ca3c9f8a39bd8c0dda939a7a23b72da226124` (origin/master, 2026-08-03)
Prior audit baseline: `bdcb685fadcca9d18b18f013c052795a53b6aa33` (2026-07-18, the cycle-1 delta-inventory pin)
Verified against local HEAD: `4b2a7923`
Ticket: [#172](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/172) (tranche-2 staleness refresh, upstream sync cycle 1)

## Delta Window

Exactly one upstream commit touches `packages/react/src/context-menu/` in the window
(`9d61f9291`). Per `docs/audits/METHODOLOGY.md` (Q3 corollary — the shared-utility lesson)
the sweep also diffs everything ContextMenu composes: `packages/react/src/menu/`,
`packages/react/src/floating-ui-react/`, `packages/react/src/utils/`, and
`packages/react/src/internals/`. That widens the window to **26 (commit, ContextMenu) pairs**,
each dispositioned in its own row below.

Every disposition row below carries the standard **Verified against** field — local HEAD
`4b2a7923` + upstream pin `1a2ca3c9f` (2026-08-03), audited 2026-08-21. The value is identical in
every row because the whole sweep ran against one tree and one pin.

Local composition is by inheritance — `ContextMenuItem : MenuItem`,
`ContextMenuPositioner : MenuPositioner`, `ContextMenuSubmenuTrigger : MenuSubmenuTrigger`,
`ContextMenuGroupLabel : MenuGroupLabel`, and so on — so every Menu-family verdict below
propagates to ContextMenu without a separate code path. Only `ContextMenuRoot` and
`ContextMenuTrigger` (plus `blazix-baseui-context-menu.js`) are ContextMenu-owned.

## ContextMenu-owned commits

| Upstream | Verdict | User-observable symptom | Evidence | Verified against |
|---|---|---|---|---|
| `9d61f9291` #5269 | **(c) port — JS module** | Press and hold a context-menu trigger with one finger, then put a second finger down without lifting the first (the start of a pinch-zoom or two-finger scroll): the pending long-press timer kept running and the menu still opened ~500 ms later, mid-gesture. The same leak occurred when the trigger became disabled mid-press. | Ported in this change set. `blazix-baseui-context-menu.js`: `cancelLongPress(root)` extracted (clears the pending timer **and** nulls the stored touch position) and called from the disabled branch and the multi-touch branch of `handleTouchStart`, and from the multi-touch branch of `handleTouchMove`; `touchend`/`touchcancel` bind `cancelLongPress` directly; the long-press callback captures the touch position in a local instead of re-reading `root.touchPosition`. Covered by `ContextMenuTestsBase.SecondTouchStartDuringLongPress_CancelsPendingLongPress` and `MultiTouchMoveDuringLongPress_CancelsPendingLongPress` (Server + WASM). | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |

Only the `ContextMenuTrigger.tsx` hunk of `9d61f9291` is ContextMenu-owned; the commit's
`ContextMenuRoot.test.tsx` hunk is test-only, and its Menu-family source hunks
(`useMenuItem`, `useMenuItemCommonProps`, `MenuLinkItem`, `MenuCheckboxItem`) are
dispositioned under Menu sweep [#166](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/166).

## Menu-family commits inherited by ContextMenu

| Upstream | Verdict | User-observable symptom | Evidence | Verified against |
|---|---|---|---|---|
| `79f1443d3` #5363 | **(d:already-present)** | Items inside a disabled root stayed clickable and did not report a disabled state to assistive technology. | `Menu/MenuItem.razor:25` — `ResolvedDisabled => Disabled \|\| (RootContext?.Disabled ?? false)`, consumed at `:103`, `:113`, `:117`, `:127`, `:150`. `ContextMenuItem : MenuItem` (`ContextMenu/ContextMenuItem.cs`), so ContextMenu inherits it unchanged. Landed in PR #209. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `022d979ae` #5342 | **(d:already-present)** | Opening a submenu with the keyboard made VoiceOver announce the trigger's expanded-state change instead of the first submenu item, so the item was never announced. | `Menu/MenuSubmenuTrigger.razor:49` (`ShouldOmitExpanded`) and `:249`. `ContextMenuSubmenuTrigger : MenuSubmenuTrigger`. Landed in PR #209. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `3c55b155c` #5393 | **(a) skip — React-specific** | No runtime content — a Menubar test switched to fake timers. | Test-only upstream (`Menubar.test.tsx`); no production code changed. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |

## Shared-layer commits already landed (#158 batch 1, PR #207) — inheritance verified

| Upstream | Verdict | User-observable symptom | Evidence | Verified against |
|---|---|---|---|---|
| `3dceedea8` #4665 | **(d:already-present)** | Closing a context menu while a non-Base-UI overlay was still open released the page scroll lock that overlay needed; and on pages where `<body>` is the scroll container the lock did nothing and the page scrolled behind the open menu. | `blazix-baseui-menu.js:7` imports `acquireScrollLock` from `blazix-baseui-scroll-lock.js`; `getViewportScroller` at `:93`/`:124`. ContextMenu opens through the Menu positioner, so it acquires the fixed lock. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `84ac4b797` #4485 | **(d:already-present)** | Pinch-zooming on a touch device dragged the open context menu around the screen instead of leaving it anchored. | `Menu/MenuPositioner.razor:211` sets `shiftLayoutViewport`, threaded to `blazix-baseui-menu.js:1647`. `ContextMenuPositioner : MenuPositioner`. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `bd2f34ddb` #5299 | **(d:already-present)** | A reopened context menu could flash at coordinates left over from its previous open; at full size those coordinates can overflow the layout viewport and make mobile Chrome zoom the page out. | `blazix-baseui-floating.js:639` `parkPositionerAtViewportOrigin`, called from `resetPositioner` (`:665`) and `disposePositioner` (`:675`); `blazix-baseui-menu.js:17` imports the shared positioner. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `692bc8748` #5370 | **(d:already-present)** | A context menu anchored to the physical left grew from the wrong edge while auto-resizing, so its content slid sideways instead of expanding in place. | `blazix-baseui-menu.js:1836` `applyAnchoringStyles`, applied at `:1867` from `setupMenuAutoResize`. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `1a2ca3c9f` #5401 | **(d:moot)** | Interrupting a closing popup (reopening mid-exit) released the popup's locked size immediately, so the replacement animation started from a collapsed box and visibly jumped. | Mechanism inspected: `setupMenuAutoResize` in `blazix-baseui-menu.js:1859-1888`. The menu path never locks the popup size — `setPopupCssSize` is called exactly once, with `'auto'` (`:1881`), and there is no transition that holds `--popup-width`/`--popup-height` across an animation. The upstream fix guards the *release* of a size lock that the menu popup never takes, so the collapsed-box jump cannot arise. Contrast `blazix-baseui-popup-viewport.js:32` and `blazix-baseui-popover.js`, which do lock size and which PR #207 patched. Revisit if a Menu viewport morph ever locks size (methodology G3). | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |

### Observation (no change made)

`blazix-baseui-menu.js` writes `rootState.autoResizeCommitted` (`:1884`) and
`rootState.liveDimensions` (`:1872`) but never reads either — the `ResizeObserver` installed by
`setupMenuAutoResize` feeds state nothing consumes. This is pre-existing and unrelated to this
ticket, so it is recorded rather than deleted (`CLAUDE.md` §3). It is also why the `#5401`
verdict above is `d:moot` rather than a port: the machinery that *would* need the fix is inert.

## Shared-layer commits owned by #158 batch 2 — not re-dispositioned here

Sweep [#158](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/158) owns the shared cluster
so it is dispositioned once rather than across 12+ component sweeps. Batch 2 has **not** landed
as of local HEAD `4b2a7923`. Recorded here is the ContextMenu-side symptom and current state, so
the ContextMenu record is not silently blank on them; the verdicts belong to #158.

The verdict on the `dc9a4577f` … `071e89201` rows is **defer-with-spec** in the rubric's own
sense (Resolving uncertainty, tier 2; Q6.3): "A deferral is a recorded debt, not a skip." The
debt is owned by #158, and the ContextMenu-side symptom and evidence are written down here so
each deferral is specified rather than blank. The final row, `6feeb1f54`, is **not** deferred —
#158 resolved it as `(d:moot)`, and it is listed here only because it belongs to the same shared
cluster.

| Upstream | Verdict | User-observable symptom | Evidence | Verified against |
|---|---|---|---|---|
| `dc9a4577f` #5384 | **defer-with-spec → #158** | Activating a context-menu submenu trigger with Android TalkBack does not open the submenu. | Not ported: `rg -c isVirtual src/Blazix.BaseUI/wwwroot/blazix-baseui-menu.js` returns no matches (exit 1), so no virtual/synthesized-pointer classification exists on the activation path ContextMenu shares with Menu. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `9a5c3850f` #5265 | **defer-with-spec → #158** | In Safari, scrolling a long context menu under a stationary pointer moves the highlight to whichever item slides under the cursor, fighting keyboard navigation. | Not ported: no zero-delta pointer-move guard exists in the list-navigation hover paths of `blazix-baseui-menu.js`. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `595c0fa08` #5340 | **defer-with-spec → #158** | Swapping a context-menu group label lets the outgoing instance clear the incoming one's registered id, so the group loses its accessible name. | Weaker guard: `Menu/MenuGroupLabel.razor:98` tests `hasRegisteredId` ("did I ever register") rather than ownership, and `ContextMenuGroupLabel : MenuGroupLabel` inherits it. Contrast the instance-ownership pattern PR #206 established for `FieldsetLegend`. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `8b2282a5e` #5388 | **defer-with-spec → #158** | Return focus after closing a context menu may use the wrong focus-visible modality, so a focus ring appears (or fails to appear) for the wrong input type. | Unverified: `blazix-baseui-floating.js:3267` tracks `lastInteractionType` per focus-manager instance and reads it at dispose (`:3664`), which may already give upstream's reset-on-open semantics; the close-time snapshot half is unconfirmed. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `7397c99ba` #5339 | **defer-with-spec → #158** | Popup-handle calls made during mount can be dropped, so a detached-trigger context menu does not respond to its handle. | Unblocked — the Handle surface decision was ratified on #157 and executed in PR #203 — but not yet evaluated against the port's handle system. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `3b5715cc7` #5387 | **defer-with-spec → #158** | Popup-handle state-machine regressions leave a detached-trigger context menu stuck open or unopenable. | Unblocked (PR #203), not yet evaluated. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `071e89201` #5394 | **defer-with-spec → #158** | Unused handle attachments are made for triggers that never use them; no user-visible symptom is claimed upstream beyond the wasted attachment. | Unblocked (PR #203), not yet evaluated. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `6feeb1f54` #5357 | **(d:moot)** — resolved on #158 | No user-observable symptom: upstream replaced string literals with constants of identical value. | The port models change reasons as typed C# enums (`Menu/Enumerations.cs:39-112`), so literal drift is structurally impossible. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |

## React-specific commits — (a) skip

Every row in this table carries the verdict **(a) skip — React-specific**; the reason column is
both the symptom restatement and the evidence. None changes DOM output, ARIA, focus order,
keyboard/pointer behavior, timing constants, or the public API surface.

| Upstream | Why no ContextMenu symptom | Verified against |
|---|---|---|
| `166e8ac01` #5400 | Development-mode-only duplicate-trigger warning cost. The port ships no equivalent dev diagnostic, so no user-facing behavior exists to change. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `ee38be3e2` #5250 | Store-selector code size. Shipped bytes only; rendered output identical. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `b089a7ccc` #5309 | React 17 legacy-mode portal mounting order. Blazor renders the portal subtree through `ContextMenuPortal`, which has no legacy/concurrent split. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `54cfcc188` #5386 | TypeScript declaration emission for published internals. No runtime content. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `ce7358672` #5298 | Module export/packaging move. No runtime content. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `1e64978b1` #5372 | Avoids redundant React re-renders during lazy flip; the flip decision itself is unchanged, so the popup lands in the same place. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `a407327a8` #5353 | Test-only timing stabilization. No runtime content. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `006a72a99` #5341 | Moves which React component owns a cleanup effect. Recorded **revisit-on-symptom** per #158; no divergence observed in ContextMenu open/close ordering to date. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `b38becd6e` #5337 | `useEffect` vs layout-effect timing. Recorded **revisit-on-symptom** per #158; no divergence observed in ContextMenu open/close ordering to date. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |

## Test coverage

| Behavior | Test |
|---|---|
| Second finger during a pending long press cancels the gesture | `ContextMenuTestsBase.SecondTouchStartDuringLongPress_CancelsPendingLongPress` (Server + WASM) |
| Multi-touch move during a pending long press cancels the gesture | `ContextMenuTestsBase.MultiTouchMoveDuringLongPress_CancelsPendingLongPress` (Server + WASM) |
| Disabling the root cancels a pending long press | `ContextMenuTestsBase.DisablingRoot_CancelsPendingLongPress` (pre-existing; still passing after the refactor) |

Both new tests were confirmed to fail against `4b2a7923` without the JS change and pass with it.
They drive `blazix-baseui-context-menu.js` directly with synthesized `touchstart`/`touchmove`
events, following the pre-existing `DisablingRoot_CancelsPendingLongPress` pattern, because
Playwright cannot deliver genuine multi-finger touch input.
