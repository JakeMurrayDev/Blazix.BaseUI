# Parity Harness — Session Handoff

**Written:** 2026-07-28 (updated same day, second session)
**Branch:** `claude/component-rendering-tests-ba6566` (worktree `component-rendering-tests-ba6566`)
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
| 5 | Manifest, alias table, `ParityCapturer` | complete (`cdf07b66..cb3de37a`), reviewed clean after 1 fix wave |
| 6 | Finding model, `NodeMatcher`, structural comparators | complete (`cb3de37a..f66a28b9`), reviewed clean after **five** fix waves |
| 7 | Style, custom property, geometry comparators | complete (`f66a28b9..2602bedb`), reviewed clean after 1 fix wave |
| 8 | ARIA, focus, console comparators | complete (`2602bedb..360a00da`), reviewed clean after 1 fix wave |
| 5b | Selector-resolution comparator | **not started — controller-added, see Open items 1** |
| 9 | Animation timeline comparator | in progress |
| 10–17 | Pixels, waivers, baselines, report, canary, fixtures, docs | not started |

Suite at Task 8 close: **187/187**, clean build, 0 lint violations.

## The single most important lesson from this session

**A green suite is not evidence in this codebase.** Task 6 shipped green four consecutive
times — full suite passing, zero warnings, confident report — with a *silent false negative*
inside each time. The mechanism was identical every round: the tests were written around the
data shape the implementation happened to handle, so they could not fail. Examples:

- `NodeMatcher` mispaired an extra wrapper against a same-key sibling, producing five
  Structure findings that were **false about both legs** plus a fabricated Attribute finding,
  on a tree with no defect in it at all.
- Task 7's flat tolerance reported `opacity: 1` equal to `opacity: 0.6` and
  `animation-duration: 0s` equal to `0.5s`, while a code comment asserted that was impossible.
- Task 8 produced a console finding that **could never fail the run**: an unfolded timestamp
  varies per retry attempt, so the plan's own retry rule demotes it to `Severity.Flaky`,
  which is defined as "reported but never fails".

What actually worked, and should be kept doing:

1. **Reviewers that compile a probe** from the verbatim sources and run real inputs through
   it, rather than reasoning from reading. This found the defects listed above; reading did not.
2. **Requiring the implementer to mutation-test** when the suite goes green first try.
3. **Enumerating the input shapes in the dispatch** rather than leaving shape choice to the
   implementer.
4. **Cross-referencing the plan's other sections** — Task 8's blocker only existed in the
   interaction between a comparator and two rules specified 700 lines away.

## Open items

1. **Task 5b — selector-resolution comparator (controller-added, not in the plan).**
   `grep -n 'SelectorUnresolved\|UnresolvedSelectors' <plan>` returns exactly ONE hit: the
   enum member declared in Task 6. **No task in the plan ever compares
   `StepCapture.UnresolvedSelectors` or `NonActionableSelectors` between legs**, so a step
   selector that resolves on React and not on Blazor is captured on both legs and then
   discarded. Write `Diff/SelectorComparator.cs`. Whether "present but not driveable" needs
   its own `FindingKind` or reuses `SelectorUnresolved` with a distinct message is a question
   for the human.

2. **`markers.json` reachability — needs the human, blocks Task 11.**
   `shared/capture.js:109-110` rewrites every `data-blazix-base-ui-*` attribute to
   `data-base-ui-*` *before capture*, so **12 of the 15 committed `markers.json` entries can
   never reach `MarkerComparator`** — only the 3 `data-blazix-otp-*` entries survive. The 12
   will instead surface as generic `AttributeComparator` **errors** needing waivers, rather
   than the `Severity.Info` classification their written justifications exist to provide.
   Worse, `MarkerComparator`'s own doc tells a reader that a surviving prefix means "no
   upstream counterpart", which the manifest contradicts. Three options:
   (i) key `blazorOnly` on the normalized `data-base-ui-*` spelling and classify normalized
   names with no upstream counterpart; (ii) narrow `capture.js`'s rewrite to names that have
   an upstream counterpart; (iii) cut `markers.json` to the 3 reachable entries and move the
   12 to the waiver file. **Recommendation: (i)** — it keeps normalization doing its real job
   (making the 8 genuinely-equivalent markers compare equal) while making classification
   reachable. Resolve before Task 11.

3. **Task 7 epsilon deviation — reversible, flag if unintended.** The brief's Step 3 mandates
   one flat epsilon on every numeric run. That was changed to tolerance **only on
   length-carrying runs** (`px`, `%`, and `matrix()`/`matrix3d()` translation arguments),
   exact equality otherwise, because the flat rule silently reported `opacity: 1` equal to
   `0.6`. Basis: the plan's own prescribed commit message says the tolerance exists so
   "sub-pixel layout noise is absorbed **without weakening colour or keyword equality**".

4. **Round-trip settle gap (unsolved, deliberately unmasked).** Click → Blazor render batch is
   not synchronised. No `Task.Delay` was added to hide it. Proposed fix: a render-generation
   counter on the fixture host. **Must be solved before Task 15**, or interaction steps will
   capture mid-batch.

5. **`Pairs` is not fully trustworthy, permanently.** Accepted at Task 6's closing review after
   a 27-tree probe. `NodeMatchResult.Pairs` can contain a real element paired with a **layout
   wrapper** or the **wrong same-key sibling**, and in both cases `Relaxed == false` — the flag
   that exists to warn consumers does not cover it. When it fires, the wrapped subtree reports
   one-sided on *both* legs, so no Attribute/ComputedStyle/Geometry finding will ever name it
   and a dropped ARIA attribute inside it is invisible. The run still fails on Structure, so it
   is a **mislabelled positive, not a silent pass** — that distinction is why it shipped. The
   full limit list is in the `<remarks>` on `NodeMatcher.Match`; read it there.

6. **Console normalisation leaves a residual volatile-token class.** Folding the ISO instant
   closed the unfailable-finding hole for timestamps, not for the class. Query strings survive
   whole (Blazor circuit ids arrive that way), GUIDs are unfolded, and Vite's `?t=` blocks the
   position fold. The structural fix belongs in **Task 11's retry and waiver policy**, not in
   widening a regex against text nobody has captured. Documented in `ConsoleComparator`'s
   `Normalize` remarks.

7. **Console waiver keys — Task 11 must decide knowingly.** `ConsoleComparator` puts the
   *normalised* message in `Finding.Property` so a waiver can silence one message rather than a
   step's whole console output. Cost: waiver authors hand-write the placeholders, and any later
   change to the normalisation rules silently invalidates every console waiver into
   `UnusedWaivers`. Argues for prefix/substring matching on `Property` for `FindingKind.Console`
   rather than the plan's exact-match-or-`*`.

8. **Tailwind scans comments.** The word "table" in a `CaptureProbe.razor` comment emits a
   `.table` rule, so `pnpm parity:css` regenerates 3 lines that differ from the committed
   stylesheet. Cosmetic; pre-existing.

9. **Task 14 should assert the React bundle exists.** `react-fixtures/dist/` is gitignored and
   built only by a manual `pnpm parity:build`; the csproj copies it but never builds it, and
   repo CI is lint-only. Failure is loud (empty `dist/` serves nothing at `/react`;
   `SharedStylesheetTests` catches a stale stylesheet) but the message should be actionable.

## Invalidations

- **Node paths were re-namespaced in `e1fbe534`** (`root > …`, `portal(N) > …`). Any capture
  or baseline produced before that commit is invalid. No baselines exist yet.

## Environment facts that bite

- `.base-ui` is gitignored and lives at the **main repo root**, so it is **absent from this
  worktree**. Resolve it via `BaseUiLocator` / `PARITY_BASE_UI_PATH`, never a relative path.
- `TreatWarningsAsErrors=true` repo-wide — an unused `using` (`IDE0005`) fails the build.
- Commits in this repo carry **no `Co-Authored-By` trailer**.
- `vite` is pinned at `7.1.12`; `7.1.14` was never published.
- The parity host needs `ASPNETCORE_ENVIRONMENT=Development` when run manually, or static web
  assets are disabled and `blazor.web.js` is served as an empty 200.
- `.superpowers/` is gitignored, so every task report lives on disk untracked. That is why
  this file exists.
