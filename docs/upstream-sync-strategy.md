# Upstream Sync Strategy

How the Blazor port tracks upstream [Base UI](https://github.com/mui/base-ui) (React). Ratified via
[Wayfinder map: upstream Base UI sync strategy (#144)](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/144);
each section links the decision ticket holding its full rationale.

Upstream is vendored at `.base-ui/`, pinned at a single SHA. The port's sync problem is **behavioral
drift, not surface area**: all 40 upstream components and all 275 exported parts have Blazor
counterparts ([coverage-gap report, #146](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/146)),
so the work is classifying and transferring upstream behavior changes, not building missing components.

## Cadence ([#149](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/149))

- **Monthly scheduled evaluation** — fetch `.base-ui` remote-tracking refs, refresh the delta
  inventory, then decide *from the inventory* whether to re-pin and run a sweep cycle. A thin delta
  may skip a cycle. Manual off-schedule evaluations are allowed (e.g. upstream cuts a release or
  lands a critical fix).
- **One repo-wide cadence** — no separate shared-layer cadence. The shared popup/positioning/focus
  layer's special status is expressed as *ordering inside each cycle* (see Prioritization), not as a
  faster clock. One pin, one inventory, one cadence.
- **Atomic re-pin per cycle** — the `.base-ui` working tree moves **exactly once, at cycle start**;
  every sweep in that cycle audits against that one SHA. The rubric's per-row pin recording is an
  audit trail documenting unavoidable lag (sweeps land over weeks), not license to pin surfaces
  differently by design.

## Cycle lifecycle

1. **Evaluate** — the monthly session fetches refs and refreshes the inventory, fed by the
   upstream-watch digest ([#156](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/156)) when it
   has run. The digest feeds evaluations, never blocks them: evaluations stay runnable manually if
   the digest is late.
2. **Verdict** — sweep or skip, a human call. Skip months create no tickets.
3. **Epic** — on a sweep verdict, the same sitting re-pins and seeds one cycle epic plus its sweep
   tickets (see Tracking shape).
4. **Sweeps** — executed per the cycle's ordering; every commit in scope gets a standard disposition
   row per the rubric. Closing the epic closes the cycle.

## Tracking shape ([#152](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/152))

- **One epic per sweep-verdict cycle**, carrying the cycle's identity: the pin SHA the cycle audits
  against, a link to the refreshed inventory/digest, and the verdict.
- **Sweep tickets as native sub-issues** of the epic, granulated by **audit-set family**
  (Combobox+Autocomplete, Field+Fieldset+Form, Menu family, …) plus a dedicated shared-layer ticket.
  Shared commits get one disposition per (commit, component) pair inside the family ticket.
- **Ordering as native dependency edges** — the shared-layer sweep blocks popup-family sweeps only;
  tranche ordering beyond that is priority, not blocking.
- **Standing `upstream-sync` label** on the epic, sweeps, and digest issues for cross-cycle queries.

## Classification rubric

The canonical rubric lives at [docs/audits/METHODOLOGY.md](audits/METHODOLOGY.md) — verdict classes,
ordered questions, the symptom-restatement rule, the three-tier uncertainty default, the standard
disposition row, and the mandatory port test citation
([PR #154](https://github.com/JakeMurrayDev/Blazix.BaseUI/pull/154) +
[PR #155](https://github.com/JakeMurrayDev/Blazix.BaseUI/pull/155) amendment, ratified on
[#150](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/150)). This document does not restate
it; sweeps follow METHODOLOGY.md directly.

## Prioritization within a cycle ([#151](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/151))

1. **Shared popup/positioning/focus layer first**, as its own sweep ticket. It blocks sweeps of
   components sitting on the floating/popup layer, and only those — non-popup sweeps run in
   parallel. Shared commits are dispositioned in this one ticket instead of re-litigated across a
   dozen component sweeps — record granularity is unchanged: one disposition row per applicable
   (commit, component) pair, per the rubric — and component sweeps then diff against an
   already-fixed shared base (the structural fix for the Popover audit's missed shared fixes).
2. **Tranche 1 — delta-first**: the named framework-neutral fixes from the inventory, largest family
   first. Known, concrete bugs with commits already identified — highest value per effort.
3. **Tranche 2 — staleness refreshes** for the stalest audit corners only.
4. **Defer the rest explicitly** — zero-delta trivial surfaces wait for the next re-pin; anything
   deferred still gets its standard disposition row (defer-with-spec or revisit trigger recorded) in
   the sweep that owns it. Deferral is a scope call, not a rubric bypass.

Each sweep ticket carries its component's "expand test coverage" source-diff skim as an explicit
checklist item — upstream's test sweeps hide real source refactors.

### First cycle (2026-08)

Seeded as [Upstream sync cycle 1 (#157)](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/157):
re-pin `bdcb685fadcc` → `1a2ca3c9f8a3` (2026-08-03, the exact upper bound of the classified
[delta inventory](https://github.com/JakeMurrayDev/Blazix.BaseUI/blob/research/upstream-delta-inventory/docs/research/upstream-delta-inventory.md)),
then: shared-layer sweep; non-popup tranche 1 (Field/Fieldset/Form, ScrollArea, Slider, Progress) in
parallel; popup tranche 1 (Combobox/Autocomplete, Drawer, Toast, Menu family, Select) after the
shared sweep; tranche-2 refreshes (NavigationMenu, Meter, Tabs, Tooltip, ContextMenu, MenuBar,
Slider). Deferred this cycle: popup-handle fixes #5339/#5387/#5394 (defer-with-spec; the Handle
surface decision was ratified on #157 and executed 2026-08-19 — MenuSubmenuRoot/ContextMenuRoot no
longer expose detached-trigger params), effect-timing refactors #5337/#5341 (revisit-on-symptom),
zero-delta surfaces, and audit refreshes beyond the stalest corners.

## Parity-harness boundary ([#152](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/152))

The sync effort and the [parity harness (map #134)](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/134)
are independent efforts with no blocking edges between them — **soft convergence**:

- The rubric's mandatory test citation is the **only doneness gate** for a sweep port row.
- Once the harness runs end-to-end, a covering parity fixture becomes an accepted — and, where one
  exists, preferred — form of that citation. Not a new gate.
- Two-way feed: divergences the harness surfaces enter the next cycle's inventory as sweep inputs;
  ported fixes trigger fixture re-baselining on the harness side.

## Provenance

Decided on [map #144](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/144). Research inputs:
[delta inventory (#145)](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/145),
[coverage gap (#146)](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/146),
[audit freshness (#147)](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/147),
[rubric draft (#148)](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/148). Decisions:
cadence [#149](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/149),
rubric [#150](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/150),
prioritization [#151](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/151),
tracking shape [#152](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/152).
