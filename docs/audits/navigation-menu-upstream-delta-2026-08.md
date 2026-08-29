# NavigationMenu Upstream Delta & Impact Report

Date: August 21, 2026
Repository: Blazix.BaseUI
Component: NavigationMenu
Source of truth: `.base-ui/packages/react/src/navigation-menu` @ `1a2ca3c9f8a39bd8c0dda939a7a23b72da226124` (origin/master, 2026-08-03)
Prior audit baseline: `bdcb685fadcca9d18b18f013c052795a53b6aa33` (2026-07-18, the cycle-1 delta-inventory pin)
Verified against local HEAD: `4b2a7923`
Ticket: [#168](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/168) (tranche-2 staleness refresh, upstream sync cycle 1)

## Delta Window

NavigationMenu was the stalest surface on the audit-freshness matrix ([#147](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/147)). Its transitive import graph is 164 files; 16 commits in the window touch it, one of them NavigationMenu-owned. Adding `692bc8748` (#5370) — which does not touch the graph upstream but which PR #207 implemented for this component in Razor — gives **17 (commit, NavigationMenu) pairs**, each dispositioned in its own row below.

Every disposition row below carries the standard **Verified against** field — local HEAD `4b2a7923` + upstream pin `1a2ca3c9f` (2026-08-03), audited 2026-08-21. The value is identical in every row because the whole sweep ran against one tree and one pin.

That one — `09124a6b2` (#5271) — is one of the five heavy refactors the delta inventory flagged for a source-side skim (`NavigationMenuTrigger` ±191 lines). This audit walked every hunk of it rather than accepting the "expand test coverage" title.

**Outcome: no ports.** The refactor decomposes into one genuine upstream bug fix whose symptom the port cannot reproduce, one already-present behavior, and five verified no-ops.

## NavigationMenu-owned commits

| Upstream | Verdict | User-observable symptom | Evidence | Verified against |
|---|---|---|---|---|
| `09124a6b2` #5271 | **split — (d:moot) / (d:already-present) / (a) skip per hunk** | See the per-hunk table below. | Nominally a test-coverage commit; its seven source hunks are dispositioned separately per Q6.1. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |

## `09124a6b2` (#5271) — per-hunk disposition (Q6.1)

| Upstream hunk | Verdict | User-observable symptom | Evidence |
|---|---|---|---|
| `isOutsideMenuEvent.ts` — the `nodeChildrenContains` fallback changes from `[]` to `false` | **(d:moot)** | This is the commit's real bug fix. `[]` is truthy in JavaScript, so whenever the floating tree or node id was absent, `!nodeChildrenContains` was `false` and the whole predicate returned `false` — "never outside". A navigation menu in that state did not close when focus left it. | Mechanism inspected: `blazix-baseui-navigation-menu.js:106-121` `handleGlobalFocusOut`. The port does not compute a single boolean expression with an array fallback; it iterates root states and skips with explicit predicates — `isRootFocusGuard(rootState, relatedTarget)` (`:116`) and `isInsideNavigationMenu(...) \|\| isInsideAnyNavigationMenu(...)` (`:117`) — then calls `closeNavigationMenuOnFocusOut` (`:119`). There is no truthiness coercion, and no branch where an absent tree makes every focus-out read as "inside". |
| `isOutsideMenuEvent.ts` — the trailing `!(contains(popupElement, relatedTarget) && relatedTarget?.hasAttribute('data-base-ui-focus-guard'))` clause is deleted | **(a) skip — React-specific** | No user-observable symptom: the clause was unreachable. The same `&&` chain already requires `!contains(popupElement, relatedTarget)`, so `contains(popupElement, relatedTarget)` is necessarily `false` by the time the deleted clause is evaluated. | Verified by reading the surrounding chain in `isOutsideMenuEvent.ts` at the pin. The port's focus-guard handling is explicit and separate (`isRootFocusGuard`, `blazix-baseui-navigation-menu.js:116`). |
| `NavigationMenuTrigger.tsx` — `setAutoSizes`, `clearFixedSizes`, `scheduleAutoSizeReset`, `handleValueChange`, `handleInterruptedMutationResize`, `syncCurrentSize` and `getMutationBaseline` all take the popup/positioner elements as parameters instead of reading them from the closure, and their `if (!popupElement \|\| !positionerElement) return;` guards are dropped | **(d:moot)** | Because the callbacks read `popupElement` from the enclosing scope at call time rather than at schedule time, a size-var write queued in one animation frame could land on a different popup element if the active item changed in between. | Mechanism inspected: the port performs no sizing in `NavigationMenuTrigger.razor`. All of it lives in `blazix-baseui-navigation-menu.js`, where `syncPopupAutoSize(rootState)` (`:1272`) re-reads `rootState.popupElement`/`rootState.positionerElement` on each invocation and serializes overlapping runs with an `AbortController` (`:1280-1287`), so a stale frame returns instead of writing. There is no React closure to capture the wrong element. |
| `NavigationMenuTrigger.tsx` — the mutation-observer effect gains `!positionerElement` to its bail-out guard and `positionerElement` to its dependency array | **(d:already-present)** | Before the fix the observer could be installed while the positioner was still null; every callback then bailed inside the handlers, and because the positioner was not a dependency the effect never re-ran when it appeared — so the popup silently failed to size itself to its content. | `blazix-baseui-navigation-menu.js:882-884`: assigning `rootState.positionerElement` is immediately followed by `installPositionerResizeListener(rootState)` and `syncPopupAutoSize(rootState)`, so the sizing re-runs the moment the positioner arrives. `syncPopupAutoSize` also re-checks both elements at `:1276`. |
| `NavigationMenuTrigger.tsx` — `handleValueChange` loses its `syncPositioner` option | **(a) skip — React-specific** | No user-observable symptom: the option was dead. `handleValueChange` is only reached when `getMutationBaseline` returned `syncPositioner: false` (the `true` branch routes to `handleInterruptedMutationResize` and returns), and the `false` branch already wrote `measuredWidth`/`measuredHeight` — exactly what the unconditional code now writes. | Verified against the call site in the mutation-observer callback at the pin. |
| `NavigationMenuTrigger.tsx` — `handleInterruptedMutationResize` drops its third fallback (`width \|\| currentWidth \|\| prevSizeRef.current.width` → `width \|\| currentWidth`) | **(a) skip — React-specific** | No user-observable symptom: the third fallback was unreachable. `currentWidth` is supplied by `getMutationBaseline`, which already returns `popup.offsetWidth \|\| prevSizeRef.current.width`, so `prevSizeRef` had already been applied. | Verified by reading `getMutationBaseline` at the pin. |
| `NavigationMenuTrigger.tsx` — `scheduleAutoSizeReset` drops the abort-controller/owner staleness re-check inside `runOnceAnimationsFinish` | **(a) skip — React-specific** | No user-observable symptom: the check was redundant. `cancelAutoSizeReset(true)` aborts the previous controller before a new one is stored, and `runOnceAnimationsFinish` honours the abort signal, so a stale callback never runs to reach the check. | Verified by reading `cancelAutoSizeReset` and `runOnceAnimationsFinish` at the pin. The port's equivalent guard (`blazix-baseui-navigation-menu.js:1285`) is retained; it is inert for the same reason and is not removed (`CLAUDE.md` §3). |

## Shared-layer commits — inheritance verified

| Upstream | Verdict | User-observable symptom | Evidence | Verified against |
|---|---|---|---|---|
| `84ac4b797` #4485 | **(d:already-present)** | Pinch-zooming on a touch device dragged the open navigation-menu popup around the screen instead of leaving it anchored. | `blazix-baseui-navigation-menu.js:1376` and `:1419` pass `shiftLayoutViewport: true` into the shared positioner. Landed #158 batch 1 (PR #207). | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `692bc8748` #5370 | **(d:already-present)** | A left-anchored navigation-menu popup grew from the wrong edge while auto-resizing, so its content slid sideways instead of expanding in place. | `NavigationMenu/NavigationMenuPopup.razor:91-96` carries the logic in C# with an explicit `// Upstream #5370` reference — the one consumer PR #207 implemented in Razor rather than JS. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `1a2ca3c9f` #5401 | **(d:moot)** | Interrupting a closing popup released its locked size as soon as the canceled animation's `finished` promise rejected, so the replacement animation started from a collapsed box and jumped. | Mechanism inspected: `syncPopupAutoSize` (`blazix-baseui-navigation-menu.js:1272-1305`). The port measures under `auto` and commits fixed pixel values inside a single `requestAnimationFrame`; it never schedules a release tied to `getAnimations().finished` (`grep getAnimations` on the module returns nothing). There is no animation-completion release that could mis-fire. Same reasoning as the Menu/ContextMenu determination on [#172](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/172). | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |

## Owned by #158 batch 2 — recorded, not re-dispositioned

Batch 2 has **not** landed as of `4b2a7923`.

The verdict on these rows is **defer-with-spec** in the rubric's own sense (Resolving
uncertainty, tier 2; Q6.3) — "A deferral is a recorded debt, not a skip." The debt is owned by
#158; the NavigationMenu-side symptom and evidence are written down here so the deferral is
specified.

| Upstream | Verdict | User-observable symptom | Evidence | Verified against |
|---|---|---|---|---|
| `8b2282a5e` #5388 | **defer-with-spec → #158** | Focus returning to the trigger after the menu closes may use the wrong focus-visible modality, so a focus ring appears (or fails to appear) for the wrong input type. | Unverified: `blazix-baseui-floating.js:3267` tracks `lastInteractionType` per focus-manager instance and reads it at dispose (`:3664`); the close-time snapshot half is unconfirmed. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `7397c99ba` #5339 | **defer-with-spec → #158** | Popup-handle calls made during mount are dropped, so a detached-trigger navigation menu does not respond to its handle. | Unblocked — the Handle surface decision was ratified on #157 and executed in PR #203 — but not yet evaluated against the port's handle system. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `3b5715cc7` #5387 | **defer-with-spec → #158** | Popup-handle state-machine regressions leave a detached-trigger navigation menu stuck open or unopenable. | Unblocked (PR #203), not yet evaluated. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `071e89201` #5394 | **defer-with-spec → #158** | Unused handle attachments are made for triggers that never use them; upstream claims no user-visible symptom beyond the wasted attachment. | Unblocked (PR #203), not yet evaluated. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |

## (d:moot) — reach the graph but not any executed NavigationMenu path

| Upstream | Reasoning | Verified against |
|---|---|---|
| `dc9a4577f` #5384 | Recognizes Android TalkBack synthesized presses so a **Menu** submenu trigger opens. NavigationMenu has no submenu trigger of that kind and does not share `blazix-baseui-menu.js`'s activation path. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `9a5c3850f` #5265 | Ignores zero-delta WebKit pointer moves during **list** scrolling so a highlight does not follow the cursor. NavigationMenu runs no list navigation and has no highlighted item. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |

## (a) skip — React-specific

Every row in this table carries the verdict **(a) skip — React-specific**; the reason column is
both the symptom restatement and the evidence. None changes DOM output, ARIA, focus order,
keyboard/pointer behavior, timing constants or the public API surface.

| Upstream | Why no NavigationMenu symptom | Verified against |
|---|---|---|
| `166e8ac01` #5400 | Development-mode-only duplicate-trigger warning cost; the port ships no equivalent dev diagnostic. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `ee38be3e2` #5250 | Store-selector code size. Rendered output identical. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `b089a7ccc` #5309 | React 17 legacy-mode portal mounting; `NavigationMenuPortal` has no legacy/concurrent split. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `54cfcc188` #5386 | TypeScript declaration emission. No runtime content. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `ce7358672` #5298 | Module export/packaging move. No runtime content. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `1e64978b1` #5372 | Re-render avoidance during lazy flip; the flip decision is unchanged, so the popup lands in the same place. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |
| `006a72a99` #5341 | Relocates which React component owns a cleanup effect. **Revisit-on-symptom** per #158; no NavigationMenu open/close divergence observed to date. | `4b2a7923` + `1a2ca3c9f` · 2026-08-21 |

## Test coverage

No ports landed, so no new tests. `NavigationMenuTestsServer` and `NavigationMenuTestsWasm` were run green against `4b2a7923` (23/23 each, per-class and serial) to confirm the audited behavior is intact.
