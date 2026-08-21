# Parity corpus recount at pin `1a2ca3c9f` — 2026-08-21

Resolves the first action of [#176](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/176):
recount the upstream Tailwind demos at the then-pinned Base UI SHA, reconcile them against
`tests/Blazix.BaseUI.Parity.Tests/manifest/fixtures.json`, replace the nominal 85 with the exact
remaining set, inventory which already have a Blazor Tailwind port, and split the remainder into
mechanism/dependency-aware batches.

The pin this recount is taken at is `1a2ca3c9f8a39bd8c0dda939a7a23b72da226124` — the pin the
committed baselines now carry after #213, and the pin every cycle-1 audit and #180–#200 fix was
authored against. Before #213 the baselines were at `bdcb685f…` and this recount would have had two
answers.

## 1. The count

| | Count | Source |
| --- | ---: | --- |
| Upstream Tailwind demos at `1a2ca3c9f` | **116** | `docs/src/app/(docs)/react/components/**/demos/*/tailwind/index.tsx` in the pinned checkout |
| Upstream Tailwind demos at `bdcb685f` (the count #176 was written against) | 114 | same glob, `git ls-tree bdcb685f…` |
| Already executable fixtures | 29 | `manifest/fixtures.json` |
| **Remaining** | **87** | 116 − 29 |

The nominal **85 in #176 is replaced by 87.** Two demos were added between the two pins and none
were removed:

- `dialog/demos/focus-management/tailwind/index.tsx`
- `tabs/demos/animated-panels/tailwind/index.tsx`

Every one of the 29 manifest `react` paths still resolves at `1a2ca3c9f`, so the existing corpus
needs no repair — the reconciliation found zero orphaned manifest entries.

## 2. Existing Blazor Tailwind ports

`docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Components/Demos/<Component>/<Demo>/Tailwind/`
already holds demo-shaped Tailwind ports named after the upstream demo slugs. Of the 87 remaining:

| Existing port | Count |
| --- | ---: |
| Tailwind port exists | **48** |
| CSS port only — Tailwind variant must be authored | 16 |
| No docs demo at all | 23 |

The demo app under `demo/Blazix.BaseUI.Demo/` is **not** a source of ports: its `*Section.razor`
components are showcase-shaped, not upstream-demo-shaped, and carry their own markup.

**A docs port is a seed, not a drop-in.** Spot-checking `scroll-area/both` against
`ScrollArea/BothScrollbars/Tailwind/ScrollAreaBothScrollbarsTailwind.razor`: the structure matches
part-for-part, but the scrollbar track reads `bg-black/10 dark:bg-white/15` locally against upstream's
`bg-black/12 dark:bg-white/12`. That is exactly the kind of difference the comparator reports as a
real `ComputedStyle` finding, so each port has to be re-diffed against its upstream source when it is
promoted to a fixture rather than copied across.

Two naming caveats found while reconciling: upstream `menubar` is `MenuBar` in the docs tree, and
upstream `scroll-area/both` is ported as `ScrollArea/BothScrollbars`. Slug-to-PascalCase matching
alone under-reports.

## 3. Every batch is gated on A-3.1

The corpus does not pass today. `docs/audits/parity-milestone1-dispositions.md` A-3.1 records 4149
blocking comparator findings as one explicit unresolved blocker, and the write-baseline run for #213
reported `2 blocking leg(s)` on 26 of the 29 fixtures — the blocker is live, not historical. Only
`separator/hero`, `progress/hero` and `meter/hero` came back clean.

Adding fixtures to a corpus in that state multiplies an undisposed finding population instead of
producing evidence. **No batch below may add a fixture until A-3.1 is disposed.** #213 closes A-3.1's
prerequisite (b), the re-baseline. What remains is (a) the OtpField and `collapsible.js` src fixes
#178 named, which have still not landed on master, (c) a full serial re-run, and (d) a committed run
report so waiver identities become citable.

Recording the batches now is still useful: it is the recount deliverable, and it fixes the
denominator that #176's completion criteria are measured against.

## 4. Batches

Ordered by dependency. Batch C is gated on source fixes beyond A-3.1; the rest are ordered by
signal-to-noise.

### Batch A — Non-popup primitives

**19 fixtures.** Ports: 16 Tailwind, 0 CSS-only, 3 none.

No floating layer, no portal, no scroll lock — rendering, form state and keyboard only. Highest signal-to-noise in the set and the only batch with no upstream-source or harness prerequisite, so it goes first once A-3.1 clears.

- `slider`, `toggle`, `toggle-group`, `radio`, `checkbox-group`, `button`, `input` and `fieldset` have **no parity fixture today**, so this batch pays the one-time cost of first fixture plumbing for eight components (manifest entry, `Fixtures/<Component>/` Blazor fixture, alias entries where the `*` defaults do not resolve).
- `form/zod` binds a Zod resolver and `form/form-action` a React `<form action>` server action. Both are candidates for #176's *explicit, reasoned exclusion* disposition rather than a fixture; decide and record it in the batch issue instead of silently dropping them.
- `tabs/animated-panels` is one of the two demos new at this pin, and `parity.css` gained its `[transition:opacity_175ms_ease,translate_350ms_cubic-bezier(...)]` and `motion-safe:data-*:data-[activation-direction=*]` utilities in #213 — the animation timeline dimension is in play.

| Fixture id | Upstream demo path | Existing Blazor port |
| --- | --- | --- |
| `slider/edge-alignment` | `slider/demos/edge-alignment/tailwind/index.tsx` | Tailwind |
| `slider/hero` | `slider/demos/hero/tailwind/index.tsx` | Tailwind |
| `slider/range-slider` | `slider/demos/range-slider/tailwind/index.tsx` | Tailwind |
| `slider/vertical` | `slider/demos/vertical/tailwind/index.tsx` | Tailwind |
| `toggle/hero` | `toggle/demos/hero/tailwind/index.tsx` | Tailwind |
| `toggle-group/hero` | `toggle-group/demos/hero/tailwind/index.tsx` | Tailwind |
| `toggle-group/multiple` | `toggle-group/demos/multiple/tailwind/index.tsx` | Tailwind |
| `radio/hero` | `radio/demos/hero/tailwind/index.tsx` | Tailwind |
| `checkbox-group/hero` | `checkbox-group/demos/hero/tailwind/index.tsx` | Tailwind |
| `button/hero` | `button/demos/hero/tailwind/index.tsx` | Tailwind |
| `button/loading` | `button/demos/loading/tailwind/index.tsx` | Tailwind |
| `input/hero` | `input/demos/hero/tailwind/index.tsx` | Tailwind |
| `fieldset/hero` | `fieldset/demos/hero/tailwind/index.tsx` | Tailwind |
| `accordion/hero` | `accordion/demos/hero/tailwind/index.tsx` | Tailwind |
| `tabs/animated-panels` | `tabs/demos/animated-panels/tailwind/index.tsx` | none |
| `scroll-area/both` | `scroll-area/demos/both/tailwind/index.tsx` | Tailwind |
| `scroll-area/scroll-fade` | `scroll-area/demos/scroll-fade/tailwind/index.tsx` | Tailwind |
| `form/form-action` | `form/demos/form-action/tailwind/index.tsx` | none |
| `form/zod` | `form/demos/zod/tailwind/index.tsx` | none |

### Batch B — Menu family and hover mechanics

**9 fixtures.** Ports: 8 Tailwind, 0 CSS-only, 1 none.

Shares `src/Blazix.BaseUI/wwwroot/blazix-baseui-menu.js` list navigation and submenu chaining, and `blazix-baseui-context-menu.js`. `popover/open-on-hover` belongs here rather than with Popover because it exercises the same hover-open path.

- Carries known recorded debt: B-182.5 (`findRootOwnerId` slip-out cancel, deferred-with-spec) lands directly on `menu/open-on-hover`, and the deferred safePolygon (#4231/#4723) and restMs (#4990) refinements are in the same surface. Expect findings that are already-named debt; the batch issue should cite them up front so they are not re-litigated as new.

| Fixture id | Upstream demo path | Existing Blazor port |
| --- | --- | --- |
| `menu/group-labels` | `menu/demos/group-labels/tailwind/index.tsx` | Tailwind |
| `menu/hero` | `menu/demos/hero/tailwind/index.tsx` | Tailwind |
| `menu/open-on-hover` | `menu/demos/open-on-hover/tailwind/index.tsx` | Tailwind |
| `menu/radio-items` | `menu/demos/radio-items/tailwind/index.tsx` | Tailwind |
| `menu/submenu` | `menu/demos/submenu/tailwind/index.tsx` | Tailwind |
| `context-menu/hero` | `context-menu/demos/hero/tailwind/index.tsx` | Tailwind |
| `context-menu/submenu` | `context-menu/demos/submenu/tailwind/index.tsx` | Tailwind |
| `context-menu/with-menu` | `context-menu/demos/with-menu/tailwind/index.tsx` | none |
| `popover/open-on-hover` | `popover/demos/open-on-hover/tailwind/index.tsx` | Tailwind |

### Batch C — Detached triggers and the handle surface

**15 fixtures.** Ports: 11 Tailwind, 0 CSS-only, 4 none.

One mechanism — `ComponentHandleBase` and handle-backed trigger routing — across six components.

- **Gated on source fixes that have not landed on master.** B-187.11 (`TooltipHandle` has no `RootId`, so detached handle-backed triggers bypass the JS interaction layer) and B-188.1/.2/.3 (`UseJsHover` parameter and unconditionally attached mouse handlers) are recorded `not landed` in the dispositions ledger. Porting these fixtures first would generate findings that restate known source defects. This batch runs last.

| Fixture id | Upstream demo path | Existing Blazor port |
| --- | --- | --- |
| `menu/detached-triggers-controlled` | `menu/demos/detached-triggers-controlled/tailwind/index.tsx` | Tailwind |
| `menu/detached-triggers-full` | `menu/demos/detached-triggers-full/tailwind/index.tsx` | Tailwind |
| `menu/detached-triggers-simple` | `menu/demos/detached-triggers-simple/tailwind/index.tsx` | Tailwind |
| `popover/detached-triggers-controlled` | `popover/demos/detached-triggers-controlled/tailwind/index.tsx` | Tailwind |
| `popover/detached-triggers-full` | `popover/demos/detached-triggers-full/tailwind/index.tsx` | Tailwind |
| `preview-card/detached-triggers-controlled` | `preview-card/demos/detached-triggers-controlled/tailwind/index.tsx` | Tailwind |
| `preview-card/detached-triggers-full` | `preview-card/demos/detached-triggers-full/tailwind/index.tsx` | Tailwind |
| `preview-card/detached-triggers-simple` | `preview-card/demos/detached-triggers-simple/tailwind/index.tsx` | Tailwind |
| `tooltip/detached-triggers-controlled` | `tooltip/demos/detached-triggers-controlled/tailwind/index.tsx` | Tailwind |
| `tooltip/detached-triggers-full` | `tooltip/demos/detached-triggers-full/tailwind/index.tsx` | Tailwind |
| `tooltip/detached-triggers-simple` | `tooltip/demos/detached-triggers-simple/tailwind/index.tsx` | Tailwind |
| `dialog/detached-triggers-controlled` | `dialog/demos/detached-triggers-controlled/tailwind/index.tsx` | none |
| `dialog/detached-triggers-simple` | `dialog/demos/detached-triggers-simple/tailwind/index.tsx` | none |
| `alert-dialog/detached-triggers-controlled` | `alert-dialog/demos/detached-triggers-controlled/tailwind/index.tsx` | none |
| `alert-dialog/detached-triggers-simple` | `alert-dialog/demos/detached-triggers-simple/tailwind/index.tsx` | none |

### Batch D — Modal surfaces — Dialog, AlertDialog, Drawer

**17 fixtures.** Ports: 2 Tailwind, 0 CSS-only, 15 none.

Shares scroll lock, the two `createFloatingFocusManager` entry paths, nesting, and the drawer drag/swipe JS.

- **Harness capability prerequisite, confirmed.** `StepAction` (`Infrastructure/FixtureManifest.cs:69-115`) declares exactly `click`, `hover`, `key`, `type`/`into`, `focus`, `blur`, `scroll` and `wait`. There is **no pointer-drag or pointer-path verb**, and no viewport or virtual-keyboard verb. `drawer/swipe-area`, `drawer/snap-points` and `drawer/virtual-keyboard-aware` cannot be expressed today. Extending the action vocabulary is a prerequisite for part of this batch and should be split out as its own issue rather than buried in it; the alternative is an `actionOnly` declaration, which weakens the fixture to an unverified action and needs a written reason.
- Heaviest port debt in the set: 15 of 17 have no docs demo at all.

| Fixture id | Upstream demo path | Existing Blazor port |
| --- | --- | --- |
| `dialog/close-confirmation` | `dialog/demos/close-confirmation/tailwind/index.tsx` | none |
| `dialog/focus-management` | `dialog/demos/focus-management/tailwind/index.tsx` | none |
| `dialog/inside-scroll` | `dialog/demos/inside-scroll/tailwind/index.tsx` | none |
| `dialog/nested` | `dialog/demos/nested/tailwind/index.tsx` | none |
| `dialog/outside-scroll` | `dialog/demos/outside-scroll/tailwind/index.tsx` | none |
| `dialog/uncontained` | `dialog/demos/uncontained/tailwind/index.tsx` | none |
| `alert-dialog/hero` | `alert-dialog/demos/hero/tailwind/index.tsx` | Tailwind |
| `drawer/close-confirmation` | `drawer/demos/close-confirmation/tailwind/index.tsx` | none |
| `drawer/indent-provider` | `drawer/demos/indent-provider/tailwind/index.tsx` | none |
| `drawer/mobile-nav` | `drawer/demos/mobile-nav/tailwind/index.tsx` | none |
| `drawer/nested` | `drawer/demos/nested/tailwind/index.tsx` | none |
| `drawer/non-modal` | `drawer/demos/non-modal/tailwind/index.tsx` | none |
| `drawer/position` | `drawer/demos/position/tailwind/index.tsx` | none |
| `drawer/snap-points` | `drawer/demos/snap-points/tailwind/index.tsx` | Tailwind |
| `drawer/swipe-area` | `drawer/demos/swipe-area/tailwind/index.tsx` | none |
| `drawer/uncontained` | `drawer/demos/uncontained/tailwind/index.tsx` | none |
| `drawer/virtual-keyboard-aware` | `drawer/demos/virtual-keyboard-aware/tailwind/index.tsx` | none |

### Batch E — Combobox and Autocomplete

**17 fixtures.** Ports: 1 Tailwind, 16 CSS-only, 0 none.

Shares the filtering, virtualization and async-list surface. `autocomplete` has no parity fixture today.

- **Heaviest authoring debt**: 16 of 17 have a CSS-only docs demo, so a Tailwind variant must be authored for each before it can be a fixture.
- `async*`, `virtualized` and `command-palette` are nondeterministic by construction. Each step needs an explicit `complete` contract; a fixture without one produces `ActionCompletionUnmet`, which is **non-waivable** (`Diff/ComparatorRegistry.cs:23-29`).

| Fixture id | Upstream demo path | Existing Blazor port |
| --- | --- | --- |
| `combobox/async-multiple` | `combobox/demos/async-multiple/tailwind/index.tsx` | CSS only |
| `combobox/async-single` | `combobox/demos/async-single/tailwind/index.tsx` | CSS only |
| `combobox/creatable` | `combobox/demos/creatable/tailwind/index.tsx` | CSS only |
| `combobox/grouped` | `combobox/demos/grouped/tailwind/index.tsx` | CSS only |
| `combobox/input-inside-popup` | `combobox/demos/input-inside-popup/tailwind/index.tsx` | CSS only |
| `combobox/multiple` | `combobox/demos/multiple/tailwind/index.tsx` | CSS only |
| `combobox/virtualized` | `combobox/demos/virtualized/tailwind/index.tsx` | CSS only |
| `autocomplete/async` | `autocomplete/demos/async/tailwind/index.tsx` | CSS only |
| `autocomplete/auto-highlight` | `autocomplete/demos/auto-highlight/tailwind/index.tsx` | CSS only |
| `autocomplete/command-palette` | `autocomplete/demos/command-palette/tailwind/index.tsx` | CSS only |
| `autocomplete/fuzzy-matching` | `autocomplete/demos/fuzzy-matching/tailwind/index.tsx` | CSS only |
| `autocomplete/grid` | `autocomplete/demos/grid/tailwind/index.tsx` | CSS only |
| `autocomplete/grouped` | `autocomplete/demos/grouped/tailwind/index.tsx` | CSS only |
| `autocomplete/hero` | `autocomplete/demos/hero/tailwind/index.tsx` | Tailwind |
| `autocomplete/inline` | `autocomplete/demos/inline/tailwind/index.tsx` | CSS only |
| `autocomplete/limit` | `autocomplete/demos/limit/tailwind/index.tsx` | CSS only |
| `autocomplete/virtualized` | `autocomplete/demos/virtualized/tailwind/index.tsx` | CSS only |

### Batch F — Second fixtures for covered components — Select, Toast, NavigationMenu, OtpField

**10 fixtures.** Ports: 10 Tailwind, 0 CSS-only, 0 none.

Every component here already has a working fixture, so the plumbing exists and each addition is a manifest entry plus one Blazor fixture. All ten have a Tailwind port already.

- `otp-field` inherits A-3.1(a): the OtpField src fixes #178 named as prerequisites for its own green state have not landed on master.
- `navigation-menu/nested*` sits on the viewport/hover layout behavior recorded during the NavigationMenu work — unstyled popups covering sibling triggers is a fixture-authoring hazard, not a component defect.

| Fixture id | Upstream demo path | Existing Blazor port |
| --- | --- | --- |
| `select/multiple` | `select/demos/multiple/tailwind/index.tsx` | Tailwind |
| `select/object-values` | `select/demos/object-values/tailwind/index.tsx` | Tailwind |
| `toast/anchored` | `toast/demos/anchored/tailwind/index.tsx` | Tailwind |
| `toast/position` | `toast/demos/position/tailwind/index.tsx` | Tailwind |
| `navigation-menu/nested` | `navigation-menu/demos/nested/tailwind/index.tsx` | Tailwind |
| `navigation-menu/nested-inline` | `navigation-menu/demos/nested-inline/tailwind/index.tsx` | Tailwind |
| `otp-field/alphanumeric` | `otp-field/demos/alphanumeric/tailwind/index.tsx` | Tailwind |
| `otp-field/focused-placeholder` | `otp-field/demos/focused-placeholder/tailwind/index.tsx` | Tailwind |
| `otp-field/grouped` | `otp-field/demos/grouped/tailwind/index.tsx` | Tailwind |
| `otp-field/password` | `otp-field/demos/password/tailwind/index.tsx` | Tailwind |

## 5. Suggested order

| # | Batch | Fixtures | Gate beyond A-3.1 |
| --: | --- | ---: | --- |
| 1 | A — non-popup primitives | 19 | none |
| 2 | F — second fixtures for covered components | 10 | `otp-field` inherits A-3.1(a) |
| 3 | B — menu family and hover mechanics | 9 | none; expect known recorded debt |
| 4 | D — modal surfaces | 17 | action-vocabulary extension for 3 drawer demos |
| 5 | E — Combobox and Autocomplete | 17 | 16 Tailwind ports to author |
| 6 | C — detached triggers and the handle surface | 15 | B-187.11 and B-188.1/.2/.3 must land on master first |

A and F are the two batches that can be executed as written the day A-3.1 clears. C is last because
it is the only batch whose findings are predictable in advance and therefore worthless as evidence
until the named source fixes land.

## 6. What this does not settle

- **The exclusion set is not decided.** #176's completion criteria require every demo to hold either
  an executable fixture or an explicit, reasoned exclusion. This document proposes `form/zod` and
  `form/form-action` as the obvious candidates and settles nothing else; each batch issue owns the
  exclusion calls inside its own range.
- **Batch sizes are fixture counts, not effort estimates.** Batch A is 19 fixtures but pays first-time
  plumbing for eight components; Batch F is 10 fixtures on plumbing that already exists.
- **No fixture is authored here.** This is the recount, the reconciliation and the split — the first
  action #176 names, and nothing past it.

## 7. Provenance

| | |
| --- | --- |
| Upstream pin | `1a2ca3c9f8a39bd8c0dda939a7a23b72da226124` |
| Local HEAD | `4b2a7923` plus the #213 re-baseline |
| Demo glob | `docs/src/app/(docs)/react/components/**/demos/*/tailwind/index.tsx` |
| Manifest | `tests/Blazix.BaseUI.Parity.Tests/manifest/fixtures.json`, 29 entries |
| Port inventory root | `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Components/Demos/` |
| Date | 2026-08-21 |
