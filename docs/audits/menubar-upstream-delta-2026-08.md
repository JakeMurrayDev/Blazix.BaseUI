# MenuBar Upstream Delta & Impact Report

Date: August 21, 2026
Repository: Blazix.BaseUI
Component: MenuBar
Source of truth: `.base-ui/packages/react/src/menubar` @ `1a2ca3c9f8a39bd8c0dda939a7a23b72da226124` (origin/master, 2026-08-03)
Prior audit baseline: `bdcb685fadcca9d18b18f013c052795a53b6aa33` (2026-07-18, the cycle-1 delta-inventory pin)
Verified against local HEAD: `4b2a7923`
Ticket: [#173](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/173) (tranche-2 staleness refresh, upstream sync cycle 1)

## Delta Window

MenuBar's transitive import graph is large because `Menubar.tsx` pulls in the whole Menu family
and the shared floating layer; 20 commits in the window touch it. Adding the two test-only
commits that change no file in the graph (`3c55b155c`, `a407327a8`) gives **22 (commit, MenuBar)
pairs**, each dispositioned in its own row below. Only **one** touches
`packages/react/src/menubar/` itself with runtime content.

Every row shares the same **Verified against** value — local HEAD `4b2a7923`, upstream pin
`1a2ca3c9f` (2026-08-03), audited 2026-08-21 — so it is stated once here rather than repeated in
22 rows.

**Outcome: no new ports.** The single menubar source hunk is architecturally moot, the Menu-family
fixes are inherited from PR #209, and the shared fixes split between "already present from #158
batch 1" and "owned by #158 batch 2".

## MenuBar-owned commits

| Upstream | Verdict | User-observable symptom | Evidence |
|---|---|---|---|
| `6feeb1f54` #5357 | **(d:moot)** | No user-observable symptom: upstream replaced the ad-hoc string literals `'sibling-open'` and `'list-navigation'` in `MenubarContent`'s `hasSubmenuOpen` gate with `REASONS.siblingOpen`/`REASONS.listNavigation` constants of identical value. The class of bug this prevents is a mistyped reason literal silently failing to match, which would drop `data-has-submenu-open` while moving between sibling menus. | Mechanism inspected: `MenuBar/MenuBarRoot.razor:317-337`. The port never consults a change reason here — `SetHasSubmenuOpen` keeps a counter (`openSubmenuCount`) incremented on open and decremented on close, so `hasSubmenuOpen` stays true across a sibling-open or list-navigation transition by construction. There is no literal to mistype. Consistent with the determination ratified on [#158](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/158), which also notes the port models reasons as typed C# enums. |
| `9d61f9291` #5269 | **(a) skip — React-specific** | No runtime content on the MenuBar side — the commit adds `Menubar.test.tsx` and touches no menubar source. | Its ContextMenu and Menu source hunks are dispositioned on [#172](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/172) and #166 respectively. |
| `3c55b155c` #5393 | **(a) skip — React-specific** | No runtime content — the touch-click cooldown test switched to fake timers. | Test-only upstream. |
| `a407327a8` #5353 | **(a) skip — React-specific** | No runtime content — browser test timing stabilization. | Test-only upstream. |

## Current-state surface check

`MenuBarRoot.razor:254-266` emits `role="menubar"`, `id`, `aria-orientation`, `data-orientation`,
`data-has-submenu-open` and `data-modal`. Upstream at the pin emits `{ role: 'menubar', id,
'aria-orientation': orientation }` plus the three `MenubarDataAttributes` (`data-modal`,
`data-orientation`, `data-has-submenu-open`). **Match**, including attribute names and the
conditional presence of `data-modal`/`data-has-submenu-open`.

`MenuBarRoot` registers no label id, so `595c0fa08` (#5340) has no MenuBar-owned site; the
affected registration inside a menubar is `Menu/MenuGroupLabel.razor:98`, which is Menu-owned and
tracked on #158.

## Menu-family commits inherited by MenuBar

| Upstream | Verdict | User-observable symptom | Evidence |
|---|---|---|---|
| `79f1443d3` #5363 | **(d:already-present)** | Items inside a disabled root stayed clickable and did not report a disabled state to assistive technology. | `Menu/MenuItem.razor:25` `ResolvedDisabled`. Landed PR #209. |
| `022d979ae` #5342 | **(d:already-present)** | Opening a submenu with the keyboard made VoiceOver announce the trigger's expanded-state change instead of the first submenu item. | `Menu/MenuSubmenuTrigger.razor:49`, `:249`. Landed PR #209. |

## Shared layer already landed (#158 batch 1, PR #207)

| Upstream | Verdict | User-observable symptom | Evidence |
|---|---|---|---|
| `3dceedea8` #4665 | **(d:already-present)** | Closing a menubar menu while a non-Base-UI overlay was open released the scroll lock that overlay needed; where `<body>` is the scroll container the lock did nothing and the page scrolled behind the open menu. | `blazix-baseui-menu.js:7` imports `acquireScrollLock`; `getViewportScroller` at `blazix-baseui-scroll-lock.js:93`/`:124`. |
| `84ac4b797` #4485 | **(d:already-present)** | Pinch-zooming dragged an open menubar menu around the screen instead of leaving it anchored. | `Menu/MenuPositioner.razor:211` → `blazix-baseui-menu.js:1647`. |
| `692bc8748` #5370 | **(d:already-present)** | A left-anchored menubar menu grew from the wrong edge while auto-resizing, sliding sideways instead of expanding in place. | `blazix-baseui-menu.js:1836`, applied `:1867`. |

## Owned by #158 batch 2 — recorded, not re-dispositioned

Batch 2 has **not** landed as of `4b2a7923`.

The verdict on these rows is **defer-with-spec** in the rubric's own sense (Resolving
uncertainty, tier 2; Q6.3) — "A deferral is a recorded debt, not a skip." The debt is owned by
#158; the MenuBar-side symptom and evidence are written down here so the deferral is specified.

| Upstream | Verdict | User-observable symptom | Evidence |
|---|---|---|---|
| `dc9a4577f` #5384 | **defer-with-spec → #158** | Activating a submenu trigger inside a menubar menu with Android TalkBack does not open the submenu. | Not ported: `grep isVirtual src/Blazix.BaseUI/wwwroot/blazix-baseui-menu.js` returns 0 hits, so no virtual/synthesized-pointer classification exists on the activation path menubar menus share with Menu. |
| `9a5c3850f` #5265 | **defer-with-spec → #158** | In Safari, scrolling a long menubar menu under a stationary pointer moves the highlight to whichever item slides under the cursor. | Not ported: no zero-delta pointer-move guard exists in the list-navigation hover paths of `blazix-baseui-menu.js`. |
| `595c0fa08` #5340 | **defer-with-spec → #158** | Swapping a group label inside a menubar menu lets the outgoing instance clear the incoming one's id, so the group loses its accessible name. | Weaker guard: `Menu/MenuGroupLabel.razor:98` tests `hasRegisteredId` ("did I ever register") rather than ownership. Menu-owned site; `MenuBarRoot` itself registers no label id. |
| `8b2282a5e` #5388 | **defer-with-spec → #158** | Focus returning to the menubar trigger after a menu closes may use the wrong focus-visible modality, so a focus ring appears (or fails to appear) for the wrong input type. | Unverified: `blazix-baseui-floating.js:3267` tracks `lastInteractionType` per focus-manager instance and reads it at dispose (`:3664`); the close-time snapshot half is unconfirmed. |
| `7397c99ba` #5339 | **defer-with-spec → #158** | Popup-handle calls made during mount are dropped, so a detached-trigger menubar menu does not respond to its handle. | Unblocked — the Handle surface decision was ratified on #157 and executed in PR #203 — but not yet evaluated against the port's handle system. |

## (a) skip — React-specific

Every row in this table carries the verdict **(a) skip — React-specific**; the reason column is
both the symptom restatement and the evidence. None changes DOM output, ARIA, focus order,
keyboard/pointer behavior, timing constants or the public API surface.

| Upstream | Why no MenuBar symptom |
|---|---|
| `54cfcc188` #5386 | TypeScript declaration emission. No runtime content. |
| `ce7358672` #5298 | Module export/packaging move. No runtime content. |
| `ee38be3e2` #5250 | Store-selector code size. Rendered output identical. |
| `b089a7ccc` #5309 | React 17 legacy-mode portal mounting; no legacy/concurrent split in the port's portal. |
| `1e64978b1` #5372 | Re-render avoidance during lazy flip; the flip decision is unchanged, so the menu lands in the same place. |
| `8f795a8fd` #5264 | `usePreviousValue` `!==` → `Object.is`. The port replicates no previous-value helper, so neither the `NaN` re-set loop nor the `+0`/`-0` miss has a local site. |
| `006a72a99` #5341 | Relocates which React component owns a cleanup effect. **Revisit-on-symptom** per #158; no MenuBar open/close divergence observed to date. |
| `b38becd6e` #5337 | `useEffect` vs layout-effect timing. **Revisit-on-symptom** per #158; no MenuBar open/close divergence observed to date. |

## Test coverage

No ports landed, so no new tests. `MenuBarTestsServer` and `MenuBarTestsWasm` were run green
against `4b2a7923` to confirm the audited behavior is intact.
