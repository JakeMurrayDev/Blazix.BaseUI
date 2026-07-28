# Parity Harness — Session Handoff

**Written:** 2026-07-28
**Branch:** `claude/component-rendering-tests-ba6566` (worktree `component-rendering-tests-ba6566`)
**HEAD at handoff:** `12d6f36a`
**Merge base:** `b713a6f9`

Spec: `docs/superpowers/specs/2026-07-27-component-parity-harness-design.md`
Plan: `docs/superpowers/plans/2026-07-27-parity-harness-pipeline.md`

This file duplicates the working-tree ledger at `.superpowers/sdd/progress.md`, which is
gitignored scratch and does not survive `git clean -fdx`. If the two disagree, the ledger
plus `git log` are authoritative — they are written as work happens.

## Execution method

Subagent-driven development (`superpowers:subagent-driven-development`): one implementer
subagent per task, then a task reviewer, then a fix wave for Critical/Important findings,
then re-review. **The user specified `opus` for every subagent role** — do not fall back to
the skill's cheaper-tier defaults without asking.

Helper scripts live in the superpowers plugin cache under
`skills/subagent-driven-development/scripts/`: `task-brief PLAN N`, `review-package BASE HEAD`,
`sdd-workspace`.

## Task status

| # | Task | State |
| --- | --- | --- |
| 1 | Project skeleton, fixture routes | complete, reviewed clean |
| 2 | Single Tailwind `parity.css` | complete, reviewed clean |
| 3 | React bundle of 114 base-ui demos | complete, reviewed clean |
| 4 | `shared/capture.js` + capture models | complete, reviewed clean |
| 5 | Manifest, alias table, `ParityCapturer` | implemented + fixed; **REVIEW NOT RUN** |
| 6–17 | Comparators, waivers, baselines, report, canary, fixtures | not started |

**Resume at:** the Task 5 review. Its base is `cdf07b66`; head at handoff is `12d6f36a`.
Generate the package with `review-package cdf07b66 12d6f36a` and dispatch the task reviewer
using `.superpowers/sdd/task-5-brief.md` and `.superpowers/sdd/task-5-report.md`. If the
scratch directory is gone, regenerate the brief with `task-brief <plan> 5`; the report is
not reproducible, so tell the reviewer it is unavailable rather than inventing one.

## Open items

1. **Round-trip settle gap (unsolved, deliberately unmasked).** Click → Blazor render batch
   is not synchronised. No `Task.Delay` was added to hide it. Proposed fix: a render-generation
   counter on the fixture host. **Must be solved before Task 15**, or interaction steps will
   capture mid-batch.
2. **Tailwind scans comments.** The word "table" in a `CaptureProbe.razor` comment emits a
   `.table` rule, so `pnpm parity:css` regenerates 3 lines that differ from the committed
   stylesheet. Cosmetic; pre-existing since `dfa240f1`.
3. **Custom-property payload.** ~42 props/element survive the `--tw-` filter — Tailwind *theme*
   variables inherited from `:root`. Identical on both legs, so the comparator emits no
   findings; payload cost only. Revisit only if baselines prove unwieldy.
4. **No `appsettings.Development.json`** on the parity host; `DetailedErrors` is set in code
   in Task 4 instead.

## Invalidations

- **Node paths were re-namespaced in `e1fbe534`** (`root > …`, `portal(N) > …`). Any capture
  or baseline produced before that commit is invalid. No baselines exist yet, so nothing
  needs regenerating today.

## Defects the review loop caught (context for confidence)

Each of these produced a green build while measuring nothing:

- React reference rendered **completely unstyled** — the Vite bundle compiled zero Tailwind
  utilities (21 KB preflight vs Blazor's 149 KB). Fixed by linking the host-served generated
  stylesheet on both legs; equality is now asserted per-run by `SharedStylesheetTests`.
- `import.meta.glob` matched **zero demos** — Vite joins a `/`-prefixed glob to the project
  root before consulting `resolve.alias`. Clean build, empty bundle.
- **Two React copies** were bundled; every demo's hooks would have run against an instance
  that never rendered them. React leg now runs upstream's pinned 19.2.5.
- **`_bl_<guid>` attributes unfiltered** — `RenderElement.razor:124` captures a reference on
  every element, GUID changes each run, so every Blazix element would have diffed.
- **Portalled popups collided** — one shared path dictionary let a portal overwrite the main
  tree's geometry.
- **`AvatarFallback` could not instantiate** — `TimeProvider` was unregistered on both legs,
  and `avatar/hero` is the first fixture of Task 16's sanity batch.

Three of these were defects in the plan document, not in the implementations. The plan has
since been corrected for all of them.

## Environment facts that bite

- `.base-ui` is gitignored and lives at the **main repo root**, so it is **absent from this
  worktree**. Resolve it via `BaseUiLocator` / `PARITY_BASE_UI_PATH`, never a relative path.
- `TreatWarningsAsErrors=true` repo-wide — an unused `using` (`IDE0005`) fails the build.
- Commits in this repo carry **no `Co-Authored-By` trailer**.
- `vite` is pinned at `7.1.12`; `7.1.14` was never published.
- The parity host needs `ASPNETCORE_ENVIRONMENT=Development` when run manually, or static web
  assets are disabled and `blazor.web.js` is served as an empty 200.
