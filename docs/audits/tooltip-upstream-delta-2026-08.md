# Tooltip Upstream Delta & Impact Report

Date: August 21, 2026
Repository: Blazix.BaseUI
Component: Tooltip
Source of truth: `.base-ui/packages/react/src/tooltip` @ `1a2ca3c9f8a39bd8c0dda939a7a23b72da226124` (origin/master, 2026-08-03)
Prior audit baseline: `bdcb685fadcca9d18b18f013c052795a53b6aa33` (2026-07-18, the cycle-1 delta-inventory pin)
Verified against local HEAD: `4b2a7923`
Ticket: [#171](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/171) (tranche-2 staleness refresh, upstream sync cycle 1)

## Delta Window

Tooltip's transitive import graph is 144 files (BFS from `packages/react/src/tooltip` through
`floating-ui-react/*`, `internals/*`, `utils/*` and `packages/utils/*`). Seventeen commits in the
window touch it — **17 (commit, Tooltip) pairs**, each dispositioned in its own row below. One is
Tooltip-owned.

Every row shares the same **Verified against** value — local HEAD `4b2a7923`, upstream pin
`1a2ca3c9f` (2026-08-03), audited 2026-08-21 — so it is stated once here rather than repeated in
17 rows.

**Outcome: no new ports.** The one Tooltip-owned commit is a normalization of a type union the
port does not model, and the two shared popup fixes that reach the Tooltip viewport are already
present with upstream references in the code.

## Tooltip-owned commits

| Upstream | Verdict | User-observable symptom | Evidence |
|---|---|---|---|
| `67840f641` #5270 | **split — (a) skip / (d:moot) per hunk** | See the per-hunk table below. | Nominally a test-coverage commit; its two source hunks are dispositioned separately per Q6.1. |

`67840f641` (#5270) is nominally a test-coverage commit with two source hunks; split disposition
per Q6.1.

| Upstream hunk | Verdict | User-observable symptom | Evidence |
|---|---|---|---|
| `TooltipStore.ts` — constructor parameters `initialState`, `floatingId`, `nested` become required | **(a) skip — React-specific** | No user-observable symptom: a TypeScript signature tightening on an internal store, with every call site already passing all three arguments. | No runtime content; the port has no equivalent optional-argument store constructor. |
| `TooltipTrigger.tsx` — `getDelay(delayRef.current, 'open'\|'close')` replaces two inline `typeof … === 'object' ? …[prop] : undefined` reads | **(d:moot)** | Upstream's inline reads only understood the object form of the delay-group value, so a **numeric** group delay was silently ignored: a group configured to open instantly did not make a tooltip with its own longer `delay` open instantly, and a numeric provider delay never supplied the group's close delay. | Mechanism inspected: `Tooltip/TooltipTypedTrigger.razor:462-480` (`GetEffectiveDelay`) and `:482-485` (`GetEffectiveCloseDelay`). The port has no `number \| { open, close }` union to normalize — `TooltipProvider` exposes two separate `int?` parameters (`TooltipProvider.razor:23` `Delay`, `:30` `CloseDelay`), and the instant-open decision is made from explicit booleans (`RootContext.IsDelayGroupInstantPhase` at `:469`, `ProviderContext.IsInInstantPhase()` at `:474`) rather than by inspecting a polymorphic delay value. The misread upstream fixed cannot occur. |

## Shared-layer commits reaching the Tooltip viewport — already present

| Upstream | Verdict | User-observable symptom | Evidence |
|---|---|---|---|
| `1a2ca3c9f` #5401 | **(d:already-present)** | Interrupting a closing tooltip viewport (retargeting mid-exit) released the popup's locked size as soon as the canceled animation's `finished` promise rejected, so the replacement animation started from a collapsed box and visibly jumped. | `blazix-baseui-tooltip-viewport.js:159-175` — the `attach(animationList)` recursion re-reads `getRunningAnimations` on rejection and waits for replacements, carrying the explicit `// Upstream #5401` reference. |
| `692bc8748` #5370 | **(d:already-present)** | A tooltip anchored to the physical left grew from the wrong edge during auto-resize, so its content slid sideways while opening instead of expanding in place. | `blazix-baseui-tooltip-viewport.js:189` and `:371` (both carrying explicit `// Upstream #5370` references); `Tooltip/TooltipViewport.razor:130` passes an explicit `"ltr"` because `TooltipRootContext` carries no direction. Landed in #158 batch 1 (PR #207). |

## Shared-layer commits owned by #158 batch 2 — recorded, not re-dispositioned

Batch 2 has **not** landed as of `4b2a7923`. The verdict on these rows is **defer-with-spec** in
the rubric's own sense (Resolving uncertainty, tier 2; Q6.3) — "A deferral is a recorded debt,
not a skip." The debt is owned by [#158](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/158);
the Tooltip-side symptom and evidence are written down here so the deferral is specified.

| Upstream | Verdict | User-observable symptom | Evidence |
|---|---|---|---|
| `8b2282a5e` #5388 | **defer-with-spec → #158** | Focus returning to the trigger after a tooltip closes may use the wrong focus-visible modality, so a focus ring appears (or fails to appear) for the wrong input type. | Unverified: `blazix-baseui-floating.js:3267` tracks `lastInteractionType` per focus-manager instance and reads it at dispose (`:3664`), which may already give upstream's reset-on-open semantics; the close-time snapshot half is unconfirmed. |
| `7397c99ba` #5339 | **defer-with-spec → #158** | Popup-handle calls made during mount are dropped, so a detached-trigger tooltip does not respond to its handle. | Unblocked — the Handle surface decision was ratified on #157 and executed in PR #203 — but not yet evaluated against `Tooltip/TooltipHandle.cs`. |
| `3b5715cc7` #5387 | **defer-with-spec → #158** | Popup-handle state-machine regressions leave a detached-trigger tooltip stuck open or unopenable. | Unblocked (PR #203), not yet evaluated against `Tooltip/TooltipHandle.cs`. |
| `071e89201` #5394 | **defer-with-spec → #158** | Unused handle attachments are made for triggers that never use them; upstream claims no user-visible symptom beyond the wasted attachment. | Unblocked (PR #203), not yet evaluated against `Tooltip/TooltipHandle.cs`. |

## (d:moot) — reach the graph but not any executed Tooltip path

| Upstream | Reasoning |
|---|---|
| `dc9a4577f` #5384 | Recognizes Android TalkBack synthesized presses so a **submenu** trigger opens. Tooltip has no submenu and does not open on press; the trigger opens on hover/focus. |
| `9a5c3850f` #5265 | Ignores zero-delta WebKit pointer moves during **list** scrolling so the highlight does not follow the cursor. Tooltip runs no list navigation and has no highlighted item. |
| `8f795a8fd` #5264 | `usePreviousValue` switches from `!==` to `Object.is`, so a value that becomes `NaN` no longer re-sets state on every render and a `+0`/`-0` transition is no longer missed. The port replicates no previous-value helper — `grep -rn "PreviousValue" src/Blazix.BaseUI/Tooltip src/Blazix.BaseUI/Utilities` returns nothing — so neither symptom has a local site. |

## (a) skip — React-specific

| Upstream | Why no Tooltip symptom |
|---|---|
| `166e8ac01` #5400 | Development-mode-only duplicate-trigger warning cost; the port ships no equivalent dev diagnostic. |
| `ee38be3e2` #5250 | Store-selector code size. Shipped bytes only; rendered output identical. |
| `b089a7ccc` #5309 | React 17 legacy-mode portal mounting order; `TooltipPortal` has no legacy/concurrent split. |
| `54cfcc188` #5386 | TypeScript declaration emission for published internals. No runtime content. |
| `ce7358672` #5298 | Module export/packaging move. No runtime content. |
| `1e64978b1` #5372 | Avoids redundant React re-renders during lazy flip; the flip decision is unchanged, so the tooltip lands in the same place. |
| `006a72a99` #5341 | Relocates which React component owns a cleanup effect. **Revisit-on-symptom** per #158; no Tooltip open/close divergence observed to date. |

## Test coverage

No ports landed, so no new tests. `TooltipTestsServer` and `TooltipTestsWasm` were run green
against `4b2a7923` to confirm the audited behavior is intact.
