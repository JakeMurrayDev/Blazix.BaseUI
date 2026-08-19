# Milestone-1 parity dispositions

**Date:** 2026-08-19

**Corpus:** the 29 fixtures declared in `tests/Blazix.BaseUI.Parity.Tests/manifest/milestone-1.json`

**Purpose:** satisfy the start gate on [#176](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/176)
— "every finding from that corpus has a recorded disposition: component defect/fix, justified waiver,
harness defect/fix, or explicitly tracked unresolved blocker."

This ledger is reconstructed from merged pull requests #178 and #180–#200, the committed baseline
provenance, and the waiver registry. It records a disposition for every finding those sources name.
It does **not** make a parity claim; the bounded claim the design spec permits is discussed in
[`parity-limitations.md`](parity-limitations.md) and is **not** published.

## What counts as a finding

Two populations came out of the milestone-1 corpus, and they must not be conflated.

| Population | Produced by | Recorded in |
| --- | --- | --- |
| **A — harness findings** | The production capture/compare pipeline running the 29 fixtures against React, Blazor Server, and Blazor WASM | §A below |
| **B — source-parity findings** | Reading the Blazor port against `.base-ui` for behavior the harness structurally cannot observe (event propagation, keyboard/focus, ARIA wiring, timing) | §B below |

Population B exists because the harness compares rendered output. PR #178's own run notes say so, and
the sweep that produced B was launched precisely because a green screenshot diff is not a behavioral
guarantee.

## Waivers

`tests/Blazix.BaseUI.Parity.Tests/waivers/waivers.json` is `[]` and stays `[]`.

No finding in this ledger is waived. This is deliberate, not an oversight: a waiver's identity is the
exact six-tuple `(fixture, leg, step, nodePath, kind, property)` of a real Error produced by a real
run (`Waivers/Waiver.cs`), and `WaiverMatcher` blocks on any entry that matches zero findings. Writing
waivers for findings whose machine identities were never captured would break the suite instead of
documenting anything. Every disposition below is therefore a fix, a retirement, a recorded decline, or
an explicitly tracked blocker.

## A. Harness findings from the 29-fixture run

**Evidence:** PR [#178](https://github.com/JakeMurrayDev/Blazix.BaseUI/pull/178), one unfiltered live
run at upstream pin `bdcb685fadcca9d18b18f013c052795a53b6aa33`, capture schema 3, chromium
143.0.7499.4 on macos/arm64, 7m19s wall clock.

### A.1 Execution findings — none outstanding

| Measure | Result | Disposition |
| --- | --- | --- |
| Fixtures executed | 29 / 29 | met |
| Complete candidate legs (Server + WASM) | 58 / 58 | met |
| `ActionCompletionUnmet` | 0 | none raised |
| `FixtureError` | 0 | none raised |
| `SelectorUnresolved` / `SelectorNonActionable` | 0 / 0 | none raised |
| Settle timeouts, report diagnostics | 0 / 0 | none raised |
| Deliberately broken canary | passes in both render modes | met |

The four non-waivable kinds — `ActionCompletionUnmet`, `FixtureError`, `SelectorUnresolved`,
`SelectorNonActionable` — produced zero findings, so nothing in this class needed a disposition.

### A.2 Harness defects — fixed

| ID | Symptom | Disposition | Evidence |
| --- | --- | --- | --- |
| A-2.1 | `ReportOutputSafety.ContainsMachinePath` classified legitimate root-relative URLs as machine-local paths, so JSON rendering aborted and **no report at all** could be written for any fixture that served an `href`. | fixed — denylist replaced by an allowlist of the route roots the fixture sites serve | #178 |
| A-2.2 | `navigation-menu/hero` manifest selectors were anchored on a Blazor-only `data-orientation` attribute, so the React leg never resolved them and the menu never opened on the reference leg. | fixed — selectors re-anchored on shared attributes | #178 |
| A-2.3 | Per-fixture finding counts are not reproducible on the Server leg (`popover/hero` varied 42 / 41 / 11 across three runs): completion predicates resolve before the SignalR round trip that starts the entry transition, so the settle poll can race past a transition that has not begun. Execution health is unaffected; exact counts are not quotable. | **tracked blocker** — needs a transition-start gate in the settle protocol before per-fixture counts are evidence | #178; see A-3.2 |

### A.3 Comparator differences — one explicitly tracked blocker

The run reported **4356 findings, 4149 of them blocking**, with 0 waivers defined and 0 applied. PR
#178 landed them deliberately undisposed and made no parity claim: it established that all 29 fixtures
*execute and compare cleanly*, not that the compared output matches.

That population has **not** been reduced to per-finding dispositions, and it cannot be from the
evidence that exists: no run report is committed, so the individual six-field identities were never
recorded in the repository. It is dispositioned here as one explicit unresolved blocker with named
prerequisites.

| ID | Blocker | Prerequisites before the population can be dispositioned |
| --- | --- | --- |
| A-3.1 | 4149 blocking comparator findings from the pin-`bdcb685` run remain undisposed. The `ParityStaticShard*`/`ParityTiming` theories assert `verdict.Blocking == false`, so on the last recorded evidence the suite does not pass; no run since #178 has been recorded either way. | (a) the component fixes PR #178 names as prerequisites for its own green state — Menu, OtpField, NavigationMenu, `select.js`, `collapsible.js` — of which the Menu and NavigationMenu work landed in #182/#189/#192 while the OtpField and `collapsible.js` items have **not** landed on master; (b) a re-baseline, because the committed baselines are at `bdcb685` and cycle-1 work runs at pin `1a2ca3c9f`; (c) a full serial re-run (the parity project must not run concurrently with other Playwright work on this machine); (d) a committed run report so identities become citable. |
| A-3.2 | Server-leg finding counts are not reproducible (A-2.3), so a re-run's per-fixture numbers cannot be compared against #178's until the settle protocol gains a transition-start gate. | Settle-protocol change, then a repeat run to demonstrate stability. |
| A-3.3 | Pixel baselines exist only for `chromium-macos-arm64`. The design spec requires Linux-captured or explicitly per-OS pixel baselines before the required CI job may block. | Generate a Linux platform set, or scope the required job's pixel dimension explicitly. |

Consequence: the milestone-1 bounded claim in the design spec (criterion 5, "no unwaived Error
remains") is **not** satisfied and must not be published. The #176 start gate is a narrower
condition — a recorded disposition per finding, where "explicitly tracked unresolved blocker" is one
of the four permitted dispositions — and A-3.1 to A-3.3 are that record. See
[`parity-limitations.md`](parity-limitations.md) for the exact statement of what is and is not
established.

## B. Source-parity findings from the milestone-1 sweep

Working method, tooling notes, and the batch order are in
[`parity-sweep-handoff-2026-08-16.md`](parity-sweep-handoff-2026-08-16.md).

Disposition vocabulary used below:

- **fixed** — landed on master; the cited PR carries the upstream citation and the test.
- **retired** — deferred by one PR, then landed by a later one; both are cited.
- **declined** — deliberately not ported, with the reason recorded so it is not re-litigated.
- **maintainer decision** — a public-API or architecture choice, not mechanical parity; recorded and
  waiting on the maintainer, not on work.
- **deviation** — a reviewed, deliberate divergence from upstream that ships.

### B.1 Checkbox and Switch — PR #180

| ID | Symptom | Disposition |
| --- | --- | --- |
| B-180.1 | Checkbox: the internally dispatched synthetic input click bubbled, so an ancestor `onclick` fired twice per root click (upstream #5176 / `ddc1a4adf`). | fixed |
| B-180.2 | Checkbox: a disabled checkbox still submitted its `uncheckedValue` hidden input. | fixed |
| B-180.3 | Checkbox: no `aria-labelledby` fallback to an associated native `<label>`. | fixed |
| B-180.4 | Checkbox: the read-only/disabled change path left the native input's `checked` flipped. | fixed |
| B-180.5 | Checkbox: disabled `keydown` called `preventDefault`, killing Tab and shortcuts. | fixed |
| B-180.6 | Switch: same #5176 click-propagation defect. | fixed |
| B-180.7 | Switch: disabled switch still submitted its `uncheckedValue` hidden input. | fixed |
| B-180.8 | Switch: `aria-invalid` was not gated on `disabled` (`useFieldValidation.ts:333`). | fixed |
| B-180.9 | Switch: a `nextChecked == CurrentChecked` short-circuit suppressed a change upstream emits. | fixed |
| B-180.10 | Switch: `keydown`/`keyup` activated for events that did not originate on the element or whose default was already prevented. | fixed |
| B-180.11 | Ancestor-click-fires-once coverage mirroring upstream's `ddc1a4adf` tests was flagged as a follow-up rather than written. | retired — #196 and #193 landed the disabled-consumer-callback cases with tests |

### B.2 Select and Combobox — PR #181

| ID | Symptom | Disposition |
| --- | --- | --- |
| B-181.1 | Combobox: pressing the trigger and releasing outside it did not cancel the open (upstream #5159, `BOUNDARY_OFFSET = 5`). | fixed |
| B-181.2 | Combobox: trigger `tabindex` was not `inputInsidePopup ? 0 : -1`, leaving a spurious tab stop. | fixed |
| B-181.3 | Combobox: trigger `aria-controls` pointed at the list rather than the popup when open with the input inside the popup. | fixed |
| B-181.4 | Combobox: `aria-required` was not gated on `inputInsidePopup`. | fixed |
| B-181.5 | Select: typeahead reset window was 500 ms instead of upstream's 750 ms. | fixed |
| B-181.6 | Select: printable keys leaked to page shortcuts while the popup was open. | fixed |
| B-181.7 | Select: a failed typeahead match poisoned subsequent keystrokes. | fixed |
| B-181.8 | Select: repeated first letters collapsed even when a label legitimately starts with a doubled letter ("Aaron", "Llama"). | fixed |
| B-181.9 | Select: pointer-leave focus fallback dropped focus to `<body>` when `Select.List` was used. | fixed |
| B-181.10 | Upstream `9798cd1e8` (#5194) — React-internal handler dedup. | declined — refactor-only, no observable behavior |
| B-181.11 | Upstream `def0eade0` (#5195) locale filtering. | maintainer decision — no `Locale` parameter; `CurrentCultureIgnoreCase` is the runtime-locale analogue |

### B.3 Menu family and shared composite — PR #182

| ID | Symptom | Disposition |
| --- | --- | --- |
| B-182.1 | A submenu stayed queued to open after Chrome dropped `mouseleave` (upstream #5153). | fixed — opt-in `guardStaleOpen` |
| B-182.2 | Menu had no slip-out release cancel at all (upstream #5159); a press-and-release-elsewhere still opened the menu. | fixed |
| B-182.3 | List navigation and typeahead did not skip natively `:disabled` items (upstream #5185). | fixed |
| B-182.4 | Upstream `2437d817e` composite nested-list reorder `MutationObserver`. | declined — the port re-queries the DOM per navigation, so reordering is always read fresh |
| B-182.5 | Upstream's `findRootOwnerId` early return in the slip-out handler. | declined — omitted consistently with Select/Combobox; containment checks cover the observable cases |

### B.4 ScrollArea and Button — PR #183

| ID | Symptom | Disposition |
| --- | --- | --- |
| B-183.1 | No overscroll thumb feedback during Safari rubber-band overscroll (upstream #5145). | fixed |
| B-183.2 | Touch-modality scrolls were not treated as user-driven, so iOS momentum scrolls never revealed the scrollbar (upstream #5157). | fixed |
| B-183.3 | The thumb never received `data-scrolling`. | fixed |
| B-183.4 | Wheeling at a scroll edge was swallowed instead of chaining to the page. | fixed |
| B-183.5 | Track `pointerdown` with a degenerate max thumb offset produced a non-finite scroll position. | fixed |
| B-183.6 | Custom-tag keyboard clicks: `<a href>` did not activate on Space keyup and Space keydown scrolled the page (upstream #4838). | fixed |
| B-183.7 | `aria-disabled="false"` was not emitted when `FocusableWhenDisabled`; consumer `type`/`role` did not win. | fixed |

### B.5 Cross-cutting open/close race — PR #184

| ID | Symptom | Disposition |
| --- | --- | --- |
| B-184.1 | A close arriving while `SetOpenAsync` awaited `OnOpenChange` was silently dropped by the redundant-call early return, so a stale hover-open could reopen Menu/Select/Combobox after Escape or an outside press. | fixed — monotonic `openChangeVersion` bumped **before** the early return, in all three roots |

### B.6 Radio and Slider — PR #185

20 gaps fixed (12 Radio, 8 Slider), 4 deferred.

| ID | Symptom | Disposition |
| --- | --- | --- |
| B-185.1 | Radio: hidden input is a labelable control, so `<label>` activation produced a real bubbling click — the same #5176 defect an earlier audit had recorded as not applicable. | fixed (and the earlier audit's verdict corrected) |
| B-185.2 | Radio: no `aria-labelledby` fallback to an associated native `<label>`. | fixed |
| B-185.3 | Radio: `aria-invalid` not gated on `disabled`. | fixed |
| B-185.4 | Radio: `readonly` is inert on radio inputs, so a label click really flipped a read-only radio. | fixed |
| B-185.5 | Radio: the keydown guard conflated disabled and read-only, and lacked `useButton`'s origin check. | fixed |
| B-185.6 | RadioGroup: target resolved after `preventDefault`; arrow navigation ignored modifier keys and text direction. | fixed |
| B-185.7 | Slider: `OnValueCommitted` fired when no change had been applied, on both the input-change and drag-release paths. | fixed |
| B-185.8 | Slider: `data-dragging` and the active-thumb z-index were absent during a drag. | fixed |
| B-185.9 | Slider: per-thumb `Disabled` ignored in press hit-testing and closest-thumb search. | fixed |
| B-185.10 | Slider: `aria-invalid` missing on the root and the thumb's hidden range input. | fixed |
| B-185.11 | Slider: decimal precision could make `Math.Round` throw. | fixed |
| B-185.12 | Slider: Home/End did not stop propagation or respect an already-prevented default; the pointer guard ran on a disabled control. | fixed |
| B-185.13 | Slider: thumb blur `relatedTarget` handling and `restoringFocusVisible` suppression. | maintainer decision — needs focus/blur moved from Razor bindings into JS `[JSInvokable]`s, dropping bUnit coverage and adding a Server round trip per focus event |
| B-185.14 | Slider: `tabindex="-1"` on the control/thumb wrappers. | maintainer decision — deliberate local additions asserted by existing tests |
| B-185.15 | Slider: cancelable `OnKeyDown` args. | maintainer decision — new public args type |
| B-185.16 | Upstream `99018b2c7` (#5003) prehydration-script bundling. | declined — moot for Blazor |
| B-185.17 | Slider: with no realtime subscriber attached, JS never calls `OnDragMove`, so a drag that leaves and returns to its origin does not commit where upstream would. | deviation — recorded; with a subscriber attached, behavior matches upstream |

### B.7 Dialog and Popover — PR #186

11 fixed, 6 deferred, 1 adjacent defect reported.

| ID | Symptom | Disposition |
| --- | --- | --- |
| B-186.1 | Dialog: a callback `FinalFocus` was resolved at open time, so a controlled close restored focus to a stale target. | fixed |
| B-186.2 | Dialog: nested counts did not propagate past one level, controlled dialogs never notified, and an unmounting nested root left the parent permanently elevated. | fixed |
| B-186.3 | Dialog: internal and user backdrops shared one slot, so one of them was marked inert/`aria-hidden` while open. | fixed |
| B-186.4 | Dialog: non-modal outside dismissal fired on press rather than a completed click for mouse/pen. | fixed |
| B-186.5 | Dialog: Title/Description ids were not applied on the first render. | fixed |
| B-186.6 | Popover: a consumer-supplied id lost to the generated popup id and `aria-controls`. | fixed |
| B-186.7 | Popover: close-time `FinalFocus` resolution (same defect as B-186.1). | fixed |
| B-186.8 | Popover: keyboard activation bypassed the button module; hover was not re-evaluated when `Disabled`/`OpenOnHover` changed; touch presses did not suppress hover. | fixed |
| B-186.9 | Popover: `aria-expanded`/`data-popup-open` were not restricted to the owning trigger. | fixed |
| B-186.10 | Popover: no IME composition guard, unscoped Escape handling, pen not mapped to the mouse outside-press rule. | fixed |
| B-186.11 | Dialog runs two focus managers (C# `FloatingFocusManager` and JS `focusPopup`); `markOthers` ref-counting unions both avoid-lists and return focus runs twice. | maintainer decision — consolidation plus migration of the dropped options |
| B-186.12 | `DialogHandle` per-mount lifecycle (upstream #5149). | maintainer decision — `ComponentHandleBase` is shared by four families; public behavior change |
| B-186.13 | Modal outside-press requiring the owning backdrop. | maintainer decision — applying upstream's rule as-is would make modal dialogs undismissable, because the port's internal backdrop is `PointerEvents="none"` |
| B-186.14 | Popover sloppy-touch dismissal timing. | retired — landed in #190 (full state machine), extended to Menu/Select/non-modal Dialog in #197 |
| B-186.15 | Popover `restMs` hover semantics. | retired — landed in #190 for Tooltip and Popover, extended to standalone Menu roots and NavigationMenu in #198 |
| B-186.16 | `data-instant="trigger-change"` without a Viewport. | declined — needs a positioner-scoped "animations finished" signal the port lacks; half the fix is worse than none |
| B-186.17 | `PopoverRoot.SyncImplicitActiveTrigger` runs during each trigger registration, so with several triggers the first registered becomes the implicit active trigger; upstream applies it only when the final `triggerCount` is 1. | **tracked blocker** — reported in #186, still open, recorded in the sweep handoff as a genuine defect |

### B.8 Tooltip and PreviewCard — PRs #187 and #188

| ID | Symptom | Disposition |
| --- | --- | --- |
| B-187.1 | Tooltip: instant-dismiss mapping was inverted — a trigger press animated and an outside click was instant, the reverse of upstream. | fixed |
| B-187.2 | Tooltip: unmounting the active trigger left the popup open against a stale anchor. | fixed |
| B-187.3 | Tooltip: dismissal ran on click rather than `pointerdown`; a disabled trigger closed a sibling's tooltip; hover did not re-initialize when `DisableHoverablePopup` changed. | fixed |
| B-187.4 | PreviewCard: focus opening was not gated on `:focus-visible`, so pointer-driven focus opened the card. | fixed |
| B-187.5 | PreviewCard: a touch tap's synthesized `mouseenter` opened the card (`mouseOnly` gate missing). | fixed |
| B-187.6 | PreviewCard: the popup was not bound to the hover interaction, so moving the pointer onto the card closed it. | fixed |
| B-187.7 | PreviewCard: no `blockFocusOpen` after Escape/trigger press; no `data-instant`; the card stayed open when its active trigger unmounted. | fixed |
| B-187.8 | `restMs` rest-timer hover open (shared `createHoverInteraction`). | retired — #190 |
| B-187.9 | Nested tooltip triggers opened both tooltips. | retired — #190 |
| B-187.10 | Global Escape closed only the first-registered open root and leaked the event. | retired — #190 (and the combobox empty-list corner by #199) |
| B-187.11 | Detached, handle-backed triggers bypassed the JS interaction layer, losing safePolygon, `mouseOnly`, and popup-hover keep-open. | retired — #188 (`internal` interface members only; no public API change) |
| B-187.12 | Delay-group same-member reopen conflates per-member and group instant-phase signals in one context field. | **tracked blocker** — needs its own design; recorded in #187 |
| B-187.13 | Blur closes unconditionally; upstream defers and skips when focus moved into the popup, another trigger, or off-page. | **tracked blocker** — recorded in #187 |
| B-188.1 | `UseJsHover` was a public parameter with no upstream counterpart selecting a permanently degraded C# hover path, forcing every hover fix to be written twice. | fixed — parameter removed, JS owns hover (breaking change, recorded in #188) |
| B-188.2 | Mouse handlers were attached unconditionally, costing a Server round trip per hover in/out. | fixed |
| B-188.3 | `DoesNotOpenOnMouseEnterAfterTouchPointerDown` could not observe the `mouseOnly` gate under bUnit and was deleted. | fixed — re-expressed as Playwright coverage in #188 |
| B-188.4 | Tooltip clears `blockFocusOpen` only on blur; upstream and PreviewCard also clear on mouseleave. | **tracked blocker** — pre-existing asymmetry, flagged in #188 |

### B.9 NavigationMenu and Drawer — PR #189

20 fixed, 5 skipped, 1 deviation.

| ID | Symptom | Disposition |
| --- | --- | --- |
| B-189.1 | NavigationMenu: switching triggers re-applied the full open delay instead of retargeting the viewport instantly. | fixed |
| B-189.2 | NavigationMenu: disabled triggers still opened on hover and ArrowDown/ArrowRight. | fixed |
| B-189.3 | NavigationMenu: the viewport was inerted on any blur, including focus moves inside it. | fixed |
| B-189.4 | NavigationMenu: the popup never re-anchored under `DisableAnchorTracking`, staying pinned to the first trigger. | fixed |
| B-189.5 | NavigationMenu: a default-open menu animated in from its unpositioned origin. | fixed |
| B-189.6 | NavigationMenu: Escape did not dismiss the innermost open root and did not ignore IME composition; outside press did not wait for a completed click. | fixed |
| B-189.7 | NavigationMenu: RTL was not honored, close-size was not read from the positioner CSS vars, `OnOpenChangeComplete` fired for open as well as close. | fixed |
| B-189.8 | Drawer: `TriggerId`/`DefaultTriggerId` were never forwarded to the root. | fixed |
| B-189.9 | Drawer: nested counts were relative, so nested state and backdrop progress drifted past one level. | fixed |
| B-189.10 | Drawer: close-time `FinalFocus` re-resolution, left inconsistent by #186. | fixed |
| B-189.11 | Drawer: trigger ownership was not strict. | fixed |
| B-189.12 | Drawer: swipe/progress were not forwarded along the ancestor chain; virtual-keyboard suspension was not honored. | fixed |
| B-189.13 | NavigationMenu focus-guard `isOutsideEvent` handling. | retired — #190 moved guard focus decisions into native JS listeners with Playwright coverage |
| B-189.14 | NavigationMenu `actionsRef`. | maintainer decision — new public `Parameter` |
| B-189.15 | Full C# ownership of the content `data-open`/`data-closed`. | declined — JS writes them synchronously; moving them to C# adds visible Server latency |
| B-189.16 | Viewport guards/target `div` rendered only when `!hasPositioner`. | declined — would break content portaling |
| B-189.17 | `data-disabled` on the NavigationMenu trigger. | declined — upstream has no such attribute; the gate uses `aria-disabled` |
| B-189.18 | `restMs` keys on `isOpen` because the port's JS state has no `mounted`, so the full delay still applies during the exit transition. | deviation — recorded, conservative |
| B-189.19 | The viewport always renders both focus guards; upstream renders none when closed inline, so a closed inline menu exposes two dead tab stops. | **tracked blocker** — pre-existing, recorded in #190 |

### B.10 The Playwright-blocked shared-JS cluster — PR #190

Five items were deferred across #186–#189 for one shared reason: they live in shared JS
(`createHoverInteraction`, `createEscapeKeyHandler`, the touch machine, the focus guards) and bUnit
has no pointer or focus model, so no bUnit test could prove them. #190 retired the whole cluster with
Playwright coverage inherited by both render modes, each test proven non-vacuous by flipping its gate
in both the `.js` and the regenerated `.min.js`.

| ID | Retired item | Evidence |
| --- | --- | --- |
| B-190.1 | `restMs` is now a genuine rest timer for Tooltip and Popover; sweeping the cursor across a trigger no longer opens the popup. | #190 (`1766a02a`, `849253ca`) |
| B-190.2 | A tooltip trigger nested inside another no longer opens both tooltips. | #190 (`0f2472d6`, `849253ca`) |
| B-190.3 | Escape closes one popup per keypress, focused root first, and no longer leaks into enclosing dialogs. | #190 (`a394288d`, `85d821d3`) |
| B-190.4 | Sloppy-touch outside press: >10 px dismisses mid-gesture, >5 px arms dismissal at touchend, >1 s long press never dismisses, a clean tap dismisses via the synthesized mousedown. | #190 (`03eb25b9`, `9940c7b9`, `966dbd8d`) |
| B-190.5 | NavigationMenu closes when focus leaves it; Safari no longer lands focus on an invisible guard. | #190 (`bd6e7f11`, `a6bf7f77`) |

#190's own recorded deviations (Escape `preventDefault` unconditional; same-phase capture siblings
cannot suppress each other; `touchState` retained until the synthesized mousedown; one global touch
machine so a second open root's gesture dismissal is lost; Chromium-only touch test primitives;
Select's escape pick is last-in-Map; NavigationMenu's null `relatedTarget` exemption; the viewport
un-inert/`flushSync` window) are **deviations — recorded, deliberate** and are reproduced in
[`parity-limitations.md`](parity-limitations.md) rather than re-listed here.

### B.11 #190's follow-up list — closed

| ID | Follow-up | Disposition |
| --- | --- | --- |
| B-190.6 | `restMs` for standalone Menu roots and NavigationMenu's hand-rolled hover. | fixed — #198 |
| B-190.7 | Sloppy-touch extension to Select, Menu, and non-modal Dialog. | fixed — #197 |
| B-190.8 | Combobox empty-list Escape bubbling corner. | fixed — #199 |
| B-190.9 | Pre-existing master test failure: Button Playwright class hang. | fixed — #191 |
| B-190.10 | Pre-existing: `Menu.ArrowUp_OpensMenuAndFocusesLastItem` (both modes) and 7 of 11 MenuBar failures. | fixed — #192 |
| B-190.11 | Pre-existing: `Combobox.DisabledReadonlyRequiredAttributesAreExposed`. | fixed — #194 (drifted assertion) |
| B-190.12 | Pre-existing: `Dialog.OnOpenChange_FiresOnOutsidePress` on WASM. | fixed — #195 |
| B-190.13 | Pre-existing: `Switch.DisabledSwitch_HasAriaDisabledAndDoesNotInvokeClickCallback`. | fixed — #193, with the checkbox sibling in #196 |

## Summary

| Population | Findings | Disposition |
| --- | --- | --- |
| A.1 execution | 0 raised | nothing outstanding |
| A.2 harness defects | 3 | 2 fixed, 1 tracked blocker |
| A.3 comparator differences | 4149 blocking (undisposed as individuals) | 1 explicitly tracked blocker with 3 named prerequisites |
| B source-parity | 118 named findings | 82 fixed, 13 retired, 8 declined, 8 maintainer decisions, 2 recorded deviations, 5 tracked blockers |

The B counts are the row counts of the §B tables (`grep -cE '^\| B-[0-9.]+ \|'`), not an estimate.
The eight further deviations #190 recorded about its own fixes are listed in
[`parity-limitations.md`](parity-limitations.md) instead of being given rows here.

Nothing is waived. Nothing is silently accepted. The three items the design spec calls
milestone-completion criteria 4 and 5 — "every finding has a disposition" and "no unwaived Error
remains" — split here: the first is satisfied by this ledger, the second is not, and A-3.1 says
exactly why.
