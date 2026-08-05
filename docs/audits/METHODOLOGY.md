# Audit Methodology: Upstream Change Classification

> **Status: RATIFIED 2026-08-04** (wayfinder ticket
> [#150](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/150), map
> [#144](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/144)).
> This is the canonical rubric for upstream-delta audits. A condensed pointer
> lives in `AGENTS.md` ("Documentation and Audit Artifact Placement") and a
> session stub in `.claude/rules/upstream-classification.md` (optional local
> tooling — untracked, since `.claude` is gitignored; every normative
> requirement lives in this file and the `AGENTS.md` pointer). Mined from this
> repository's audit precedent: `docs/audits/drawer-upstream-delta-2026-07.md`
> (PR #130), `docs/audits/menu-functional-audit.md` (PR #124),
> `docs/audits/popover-functional-audit.md` (PR #122),
> `docs/audits/dialog-functional-audit.md` (PR #123), the context-menu repair
> (PR #125), and the PR #130 inline review discussion.

## Purpose

Every upstream-delta audit walks a window of Base UI (React) commits and must
disposition each one. This rubric makes the decision procedure explicit so
audits classify consistently and record the same evidence.

**A verdict attaches to a *(commit, component)* pair, never to the commit
alone** (see Gray zones, G2). A commit may also decompose into per-hunk
verdicts (Q6.1).

## The four classes

| Class | Audit-doc vocabulary | Meaning |
| --- | --- | --- |
| **(a) Skip — React-specific** | VERIFIED-N/A, "React module mechanics", "React store mechanics", TEST/CLEANUP/DX | The change exists only to serve React's runtime/tooling; the observable contract is unchanged, and the Blazor-native equivalent is identified. |
| **(b) Transfer to C#/Razor** | PORTED / Repaired (component files) | Structural DOM output, ARIA attributes, roving tabindex values, component parameters, event args, context wiring — behavior that lives in `.razor`/`.cs`. |
| **(c) Transfer to JS module** | PORTED / Repaired (`wwwroot/*.js` + regenerated `.min.js`) | Behavior living in the ported JS layer: positioning, focus management, observers, gestures/high-frequency events, typeahead visibility, dismissal listeners. |
| **(d) No-op — sub-label mandatory** | ALREADY-PRESENT, "Present (inherited)", "Architecturally moot", "Accounted for" | See below — every (d) verdict must carry one of two sub-labels. |

### The mandatory (d) sub-label

The two flavors of (d) have different revisitability profiles, so the verdict
must name which one it is:

- **`(d:already-present)`** — the behavior is already correct locally (often
  inherited from a prior audit's shared-module port). **Requires a file:line
  citation or a passing test.** Essentially final.
- **`(d:moot)`** — the local architecture cannot reproduce the defect the fix
  repairs. **Requires naming the exact local mechanism inspected**, so a later
  audit can cheaply re-open the call when a new consumer exercises the path
  (see Gray zones, G3 — the dialog `touchend` fix was moot in June and ported
  three weeks later when Drawer's swipe pipeline made it observable).

## The symptom-restatement rule (hard rule)

**No (a) or (d) verdict may be justified by naming a React mechanism.** The
written verdict must state the user-observable *symptom* the upstream change
addresses and why that symptom cannot arise (or already does not arise) in the
Blazor port. "It's a React re-render fix" is not a classification — see Gray
zones, G1, where mechanism-based classification would have wrongly skipped a
DOM-contract bug the Blazor port shared.

*Exemption:* Q1 skips with no runtime content (test-only, docs/JSDoc-only,
lint conformance) have no runtime symptom to restate; per-commit verification
notes still apply.

## Resolving uncertainty (three tiers, in order)

When an audit cannot confidently classify a commit:

1. **Default-port.** If you cannot positively verify the change is
   React-specific or already handled, presume it transfers. Uncertainty is
   evidence *for* porting, never against. **Default-skip does not exist.**
2. **Defer-with-spec** (the pressure valve): when a faithful port would mutate
   a shared, fragile system without adequate validation, deferring is
   legitimate — but only with the exact upstream mechanism written down (the
   menu audit's #4231/#4723 `blockPointerEvents` spec is the template). A
   deferral is a recorded debt, not a skip.
3. **Escalate to a human** in exactly two cases: breaking public-API changes
   (Q6.4 — parameter removals wait for an approved API pass), and conflicts
   where the auditor believes upstream's behavior is wrong or clashes with a
   deliberate local divergence — parity-vs-improvement disputes are the
   maintainer's to settle (PR #130 precedent: 1:1 parity trumps local
   "improvements").

## The ordered decision questions

Apply in order to each upstream commit (or, per Q6.1, to each independent hunk
of a commit).

### Q1. Does the change alter any observable contract?

Observable contract = DOM structure, attributes/ARIA, data-attributes, CSS
variables, focus order/visibility, keyboard/pointer behavior, timing constants,
animation/transition semantics, or the public API surface.

- **No** — JSDoc/typings text that leaves the upstream API surface unchanged
  (an API-affecting type change *is* a contract change per the "public API
  surface" clause above), test-only changes, lint conformance, dead-code
  removal verified unreachable with no observable side effect (a removal that
  drops part of the API surface is a contract change), and bundle restructures
  or internal refactors whose observable output is verified identical →
  **(a) Skip**. *Precedent requires verification, not assumption*:
  the drawer delta marked `43d11ebcf` (#5233, popup bundle-size restructure)
  VERIFIED-N/A only after "prop wiring verified identical", and out-of-path
  commits were dispositioned "with per-commit justification retained".
- **Yes, or unclear** → continue.

### Q2. Is the *mechanism* React-internal? Then restate the fix as its user-observable symptom.

React-internal mechanisms: store ownership, ref lifecycles, re-render
suppression/dedup, hook ordering, synthetic-event quirks, kept-mounted effect
guards, `useEffect` cleanup races.

Do **not** classify on the mechanism (see the symptom-restatement rule).
Restate the commit as the symptom it fixes ("closed kept-mounted menu retains
`tabindex=0` on a stale item") and ask whether the Blazor port shares the
state shape that produces that symptom:

- **Symptom cannot arise locally** (no equivalent stale state, no equivalent
  code path) → **(d:moot)** — citing the exact local mechanism inspected.
- **Symptom can arise locally** → the "React fix" has a structural root cause
  Blazor shares; continue to Q3–Q5.

### Q3. Is the behavior already present locally?

Check (i) whether a prior audit's shared-module port already delivered it —
shared `blazix-baseui-floating.js` / popups fixes propagate to every consumer —
and (ii) whether the local port predates the commit with an equivalent design.

- **Yes** → **(d:already-present)**. Cite file:line or a passing test, as the
  menu audit did for `e6dc73dfa` (#5093): "Present (inherited).
  `blazix-baseui-floating.js:3401-3404` … No change."
- **No** → continue.

*Corollary (the shared-utility lesson, recorded in the menu, dialog, and
drawer audits): the delta must always be diffed against the shared
floating/popups/composite infrastructure, not only the component directory —
the first-pass Popover audit missed four shared fixes.*

### Q4. Does the fixed behavior live in the ported JS layer?

Per `.claude/rules/js-interop-rules.md`, the JS layer owns high-frequency
events (drag, swipe, scroll), native browser APIs (observers, visualViewport,
CloseWatcher), event suppression, and focus management. If the upstream change
touches `floating-ui-react`, dismissal listeners, gesture math, typeahead
element visibility, or per-frame visual writes:

- → **(c) Transfer to JS module**, into the matching `wwwroot/blazix-baseui-*.js`
  (+ regenerate `.min.js` with the vendored terser). Precedent standard is a
  **1:1 port with upstream constants preserved** (drawer swipe engine: "all
  constants (40/10/1/50/16/80 px·ms) … 1:1 closure port of `useSwipeDismiss.ts`"),
  and per-frame JS→.NET interop is eliminated rather than mirrored (#4980
  analog: ".NET receives edge-triggered notifications only").

### Q5. Otherwise it is structural — transfer to C#/Razor.

Render output, ARIA/`aria-*` wiring, roving `tabindex` values, cascading
context, component parameters, `EventArgs` surfaces, controlled-state
reconciliation:

- → **(b) Transfer to C#/Razor**, matching React's attribute ordering where
  observable (menu popup `aria-labelledby` "placed after `id` to mirror
  React's popupProps order").

### Q6. Cross-cutting annotations (apply to any class)

1. **Split dispositions.** A commit may decompose into per-hunk verdicts:
   `bf831b754` (#4920) was "PORTED (gate) / ALREADY-PRESENT (detection)";
   `fe2101a31` (#5034) was a React-tree refactor (N/A) with a "portable
   nugget" (touch initial focus) verified present.
2. **Approximation.** A behavioral port may deliberately approximate
   upstream's timing model when the exact mechanism doesn't map — recorded as
   "accounted for (approximation)" with the divergence named (#4990 `restMs`
   submenu hover). Approximation is an *annotation on a (b)/(c) port verdict*,
   never a third (d) sub-label — a (d) verdict still requires
   `already-present` or `moot`. (The historical "Accounted for" phrasing in
   old audit docs maps to (d) or to this annotation depending on context; new
   records use the explicit forms.)
3. **Deferral with spec.** See Resolving uncertainty, tier 2.
4. **Breaking-API deferral.** Upstream removal of public API (e.g. #4891
   `SubmenuRoot` prop `Omit`s) is deferred to an API pass rather than ported,
   because parameter removal is a breaking change; the forwarded params are
   verified inert. See Resolving uncertainty, tier 3.
5. **Blazor-only hazards.** The JS↔.NET boundary can require code with **no
   upstream analog**: PR #130 review accepted exactly one CodeRabbit finding —
   serializing `OnSwipeOpen`/`OnSwipeClose` — *because* "upstream's `setOpen`
   is synchronous" while Blazor Server interop is async. Conversely, two
   findings were rejected (and withdrawn) because upstream has the identical
   code and 1:1 parity trumps local "improvements". The rubric cuts both ways:
   parity is the default defense, and asynchrony/circuit timing is the one
   place extra code is legitimate.

## Evidence bar (all classes)

- **(a)/(d) require positive verification**, not absence of evidence: identify
  the Blazor-native equivalent (file:line), or demonstrate the symptom cannot
  reproduce. The menu audit's "Rejected Findings" table exists because three
  sub-agent claims failed this bar in *both* directions.
- **(b)/(c) require red/green or parity tests** where feasible (Playwright
  Server + WASM; bUnit for attribute output), and JS ports regenerate the
  minified module.
- **Classification is per-consumer and revisitable** — see Gray zones G2/G3.
- **Stale claims:** dispositions made in a planning phase must be re-verified
  against current local HEAD before landing in a disposition table (G4).

## The standard disposition row

Every audited commit gets one row (per component) in the audit doc's
disposition table, with these fields — the SHA in a predictable position so
"where has commit X been dispositioned?" is a grep across `docs/audits/`:

| Field | Content |
| --- | --- |
| **Upstream** | Short SHA + upstream PR number (e.g. `e0c111994` #5110) |
| **Verdict** | Class letter with (d) sub-label, e.g. `(b)`, `(c)`, `(d:moot)`; split verdicts per Q6.1 |
| **Symptom** | One-line user-observable symptom restatement (or "no runtime content" for Q1 skips) |
| **Evidence** | file:line / test name / mechanism inspected / deferral-spec pointer |
| **Verified against** | Local HEAD SHA + upstream pin SHA + date |

There is **no central commit×component ledger** — per-component audit docs are
the single source of truth; the uniform row format is what makes
cross-component queries cheap. Backfilling pre-rubric audit docs into this
format is explicitly out of scope.

---

## Gray zones the precedent reveals

### G1. "React re-render fix" with a structural root cause Blazor shares

`992c52b78` (#4931, "Remove kept-mounted tabIndex workaround") reads as React
kept-mounted effect mechanics, but the symptom — a closed kept-mounted menu's
stale-highlighted item retaining `tabindex=0` — is a DOM-contract bug the
Blazor port had too. The menu audit ported it to the four `.razor` item
families as `tabindex = (open && highlighted) ? 0 : -1`. Mechanism-based
classification would have wrongly skipped it. The symptom-restatement rule is
the guard.

### G2. The same commit earns different dispositions per component

`e0c111994` (#5110, rendered trigger id ownership) is the canonical case:

- **Popover** — **(b/c) PORTED**: `resolveRenderedTriggerId(...)` added in
  `blazix-baseui-popover.js` + `PopoverRoot.razor` reassociation, with
  Playwright regression (popover audit).
- **Dialog** — **(d:moot)**: the trigger's registration key *is* its rendered
  DOM id, so the rendered-vs-internal divergence "cannot arise as it does in
  React"; flagged, not patched speculatively (dialog audit).
- **Drawer** — **(a) VERIFIED-N/A**: "React store mechanics; the Blazor
  trigger registration path … already yields rendered-trigger precedence"
  (drawer delta).

A rubric verdict therefore attaches to a *(commit, component)* pair, never to
the commit alone.

### G3. "Moot" calls are revisitable when a later consumer exercises the path

`ea3818dec` (#5096, dialog `touchend` outside-press): the dialog audit
(2026-06-30) dispositioned it "Accounted for — architecturally moot" because
the Blazor JS dismissal path never had React's touch-count guard. Three weeks
later the drawer delta **PORTED** it into `blazix-baseui-dialog.js` ("touch
pointer events excluded from the pointerdown path; `touchend` path added with
upstream's event-target resolution") because the Drawer's swipe/touch pipeline
made the difference observable. This is why `(d:moot)` must record exactly
which mechanism was inspected.

### G4. Stale planning-phase claims

The drawer delta found that a planning-phase gap claim for `d4ee8ae78` (#5024)
was stale — the fix was already present in `blazix-baseui-floating.js` from
the Popover audit. Claims made before the code sweep must be re-verified
against current HEAD before landing in a disposition table.

---

## Evidence catalog — concrete precedent per class

### (a) Skip — React-specific

| Upstream | What it was | Precedent citation |
| --- | --- | --- |
| `43d11ebcf` #5233 — Popup bundle-size restructure | React module mechanics; "prop wiring verified identical" | drawer-upstream-delta-2026-07.md, delta table |
| `4cc8e31ca` #5151, `a47b1df37` #5036, `7a0fd2f84` #5165 — JSDoc / published-types text only | No runtime content | drawer-upstream-delta-2026-07.md |
| `ccfe02679` #5101 — ESLint `mui/no-floating-cleanup` `void` prefixes | Lint conformance; "No runtime behavior change. Blazix `enqueueFocus` callers already do not retain the rAF-cancel handle" | popover-functional-audit.md, second-pass table |
| `db574a044` #4970 — menu focus flake | Test-only change in `DialogRoot.test.tsx`; "No production code changed upstream; nothing to port" | dialog-functional-audit.md |
| `16685b208` #5109 — Root owns the store | "store ownership is a React mechanic whose Blazor equivalent (`DialogRootContext` owned by `DialogRoot`) predates the window" | drawer-upstream-delta-2026-07.md |

### (b) Transfer to C#/Razor — structural DOM/ARIA/state

| Upstream | What was ported | Precedent citation |
| --- | --- | --- |
| `5e0f3e73e` #4826 — group labels in radio groups | `MenuRadioGroup.razor`: `CascadingValue<IMenuGroupContext>` + `SetLabelId` + emitted `aria-labelledby` | menu-functional-audit.md, Repairs §1 |
| `992c52b78` #4931 — kept-mounted roving tabindex | Four item `.razor` families: `tabindex = (open && highlighted) ? 0 : -1` | menu-functional-audit.md, Repairs §3 (also gray zone G1) |
| `e0c111994` #5110 — rendered trigger id ownership (Popover) | `PopoverRoot.razor` reassociation + `resolveRenderedTriggerId` (mixed b/c port) | popover-functional-audit.md, delta table |
| (current-state parity) `onOpenChange` event details | `PopoverOpenChangeEventArgs` extended with `Event`, `Trigger`, `TriggerId`, `InteractionType` — React event-detail surface translated to Blazor `EventArgs` | popover-functional-audit.md |

### (c) Transfer to JS module

| Upstream | What was ported | Precedent citation |
| --- | --- | --- |
| `21b199703` #4867 + #5105/#5057/#5181 — swipe dismiss engine | 1:1 closure port of `useSwipeDismiss.ts` in `blazix-baseui-drawer.js`; all constants preserved; per-frame JS→.NET interop eliminated | drawer-upstream-delta-2026-07.md |
| `e6dc73dfa` #5093 — keyboard-close visible focus | `enqueueFocus` gains `focusVisible` in shared `blazix-baseui-floating.js`; inherited by Menu/Dialog/Drawer in later audits | popover-functional-audit.md (port); menu/dialog audits (inheritance) |
| `7a5019998` #4195 — typeahead skips CSS-hidden items | `isMenuItemVisible` in `blazix-baseui-menu.js`, applied to typeahead only (arrow-nav intentionally unchanged, matching upstream scope) | menu-functional-audit.md, Repairs §4 |
| `c9c90dce2` #4838 — modifier-preserving keyboard clicks | `dispatchClickWithModifiers` in `blazix-baseui-button.js`, incl. Space-keyup `defaultPrevented` guard | drawer-upstream-delta-2026-07.md |
| (repair, PR #125) context-menu repeated right-click reposition + native-menu suppression + `button === 2` activation gate | `blazix-baseui-context-menu.js` + `ContextMenuTrigger.razor` (mixed b/c) | PR #125, commit `f0d874e9` |

### (d) No-op — already fixed locally / architecturally moot

| Upstream | Determination | Precedent citation |
| --- | --- | --- |
| `d4ee8ae78` #5024 — confirmation return focus | `(d:already-present)` for Menu/Dialog/Drawer — inherited from the Popover audit's shared `blazix-baseui-floating.js` port; drawer delta also corrected a stale planning-phase gap claim (G4) | menu/dialog audits ("Present (inherited)"); drawer-upstream-delta-2026-07.md |
| `4292cfaa6` #5030, pointer-down-reset half | `(d:moot)`: "Blazix instantiates a fresh focus-manager closure per open … `isPointerDown` is closure-local and cannot leak across opens" | popover-functional-audit.md, second-pass; dialog-functional-audit.md |
| `205a9a05a`/`fe05694f2` #4125/#4581 — preserve dialog focus on pointer leave | `(d:moot)`: "Blazor never force-focuses the popup on pointer-leave … so the focus-stealing the React fix repairs cannot arise" | menu-functional-audit.md (also Rejected Findings) |
| `802a5ba86` #5010 — kept-mounted viewport morph reset | `(d:moot)` for Popover ("no persistent `lastHandled` guard that survives a close"); N/A for Dialog (viewport performs no morph) | popover-functional-audit.md; dialog-functional-audit.md |
