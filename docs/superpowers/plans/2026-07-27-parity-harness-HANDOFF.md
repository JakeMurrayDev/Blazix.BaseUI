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
| 9 | Animation timeline comparator | complete (`360a00da..22e3a415`), reviewed clean after **three** fix waves |
| 10 | Screenshots, pixel diff, frame seeking | complete (`22e3a415..9781d477`), reviewed clean after 2 fix waves |
| 5b | Selector-resolution comparator | **not started — controller-added, see Open items 1** |
| 11–17 | Waivers, baselines, report, canary, fixtures, docs | not started |

Suite at Task 10 close: **294/294**, clean build, 0 lint violations.

**All ten plan comparators now exist** — structure, attribute, marker, computed style, custom
property, geometry, ARIA snapshot, focus, console, timeline, pixel. **None has ever run end to
end.** No production code constructs a `ComparisonContext`, and there is no comparator registry;
that is Task 14's runner. `ComparisonContext.PixelThreshold` is likewise not yet fed from
`FixtureEntry.PixelThreshold`. Treat every comparator as unit-tested but never integrated, and
expect the first end-to-end run to surface things no unit test could.

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

2. ~~**`markers.json` reachability**~~ **SETTLED 2026-07-29 — option (i) implemented**
   (`9ad2f926`, seam closed in `1ea7f679`). `manifest/markers.json` is now keyed on the
   **post-normalization** spelling: 12 `data-base-ui-*` entries plus the 3 `data-blazix-otp-*`
   ones normalization never touches. `MarkerComparator` classifies a candidate attribute that
   is either listed (→ `Info` with its reason) or `data-blazix-`-prefixed and unlisted
   (→ `Error`). An unlisted `data-base-ui-*` name is deliberately *not* claimed — it falls to
   `AttributeComparator` as a one-sided finding, which is right, because a Blazor-only
   attribute wearing an upstream name is a parity defect rather than a marker.

   **The partition this rests on was verified independently and is exactly true:** `src/`
   renders 20 distinct `data-blazix-base-ui-*` names, `.base-ui` renders 11 distinct
   `data-base-ui-*` names, and the 12 listed entries are precisely the Blazix names with no
   upstream counterpart while the 8 unlisted are precisely those that have one. That is what
   lets the comparator classify without consulting the React leg — and therefore without
   taking a `NodeMatcher` dependency and its untrustworthy-`Pairs` caveat.

   **Known residual:** a listed name that the React leg also carries produces a `Marker/Info`
   beside the `Attribute` finding, and the `Info`'s text ("Blazor-only marker…") is wrong for
   that case. Unreachable at today's upstream. The clean fix needs `MarkerComparator` to read
   the reference leg, i.e. the `NodeMatcher` dependency deliberately declined. Revisit only if
   upstream ever adopts one of the 12 names — `data-base-ui-focus-guard` sits one word from
   the listed `data-base-ui-focus-guard-type`.

3. ~~**Task 7 epsilon deviation**~~ **SETTLED 2026-07-29 — the length-scoped rule stands**
   (rationale corrected in `ebb704a8`/`1ea7f679`; no behaviour change). Tolerance applies only
   to length-carrying runs (`px`, `%`, and `matrix()`/`matrix3d()` translation arguments);
   everything else requires exact equality. The brief's flat epsilon reported `opacity: 1`
   equal to `0.6` and `animation-duration: 0s` equal to `0.5s`, while the plan's own prescribed
   commit message says the tolerance exists so "sub-pixel layout noise is absorbed **without
   weakening colour or keyword equality**" — the mandated mechanism defeated the mandated
   purpose. Note the tolerance is half a *unit*, not half a pixel, so a percentage gets half a
   percent (~1.5px on a 300px `--transform-origin`); `background-image` gradient stops and
   `flex-basis` were both read back from headless Chromium as retaining authored percentages
   in the computed value, while `grid-template-columns` resolves to px and does not.

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

10. **L2 timeline invariants are derived over the whole step, not per run — mislabelled, never
    silent.** `TimelineComparator.Evaluate` builds its `finished` set from every terminal event
    anywhere in the step, so a terminal from run 1 satisfies a start in run 2, and
    `lastTerminal` is the last terminal in the *step* rather than in the run a removal
    interrupted. Two probed consequences: a step where the node leaves during its **second**
    run emits no `present-at-transitionend` finding at all — run 1's `transitionend` carried
    the same property name, so the leg reads `Satisfied`; and where both legs unmount mid-run
    but only one completed an earlier run, the output states that leg **satisfied two
    obligations it demonstrably broke** — `present-at-transitionend` via the `finished` set and
    `removed-after-transitionend` via `lastTerminal` — sending a reader to the wrong leg. A run,
    an insertion or a removal that one leg recorded and the other did not always reaches L1 —
    what `TimelineSequence.Normalize` never drops or collapses is a run/`added`/`removed` event,
    not the timeline, which it also strips of untracked attribute mutations, of a consecutive
    duplicate attribute signature and of every `from` but a removal's — which makes this a
    **mislabelled positive, not a silent pass**, the same standing `Pairs` was given in item 5.
    (Equal signatures leave **two** ways for the derived states still to differ: the step's final
    snapshot, which `AttributeRemoval` reads and `Normalize` never sees, and an attribute
    mutation's `from`, which `data-open-flipped-before-starting-style-cleared` reads and
    `Normalize` drops. The second is closed to the two invariants above — neither reads a `from`
    but a removal's, which the signature does carry — so for them the snapshot is the whole of
    the enumeration, and a difference in that snapshot is what the structure comparator
    reports.) Accepted
    deliberately at Task 9's close rather than fixed; making L2 run-aware is a rewrite of
    `Evaluate`. **Task 11 must not let a waiver keyed on an L2 invariant name imply the
    invariant was measured on the run that broke it, and reporting should prefer L1's diff to
    L2's naming where both fire on one step.** Smaller and related: L3 pairs runs by index, so
    on a step where the legs ran unequal numbers of runs the values printed beside a real
    finding come from a different run. Both limits are written up in `TimelineComparator`'s
    class `<remarks>`; read them there.

11. **L3 measures a keyframe run against `animation-duration`, which declares one iteration
    rather than the whole run.** `TimelineComparator.Families` now measures each kind of run
    against its own declaration — a transition against `transition-duration`, a keyframe
    animation against `animation-duration` — and separates the two runs, so a node that
    transitions and animates over one window is two spans rather than one. The residual:
    `animation-iteration-count` is **not** in `capture.js`'s `STYLE_PROPS` allowlist, so a
    keyframe animation set to repeat a fixed number of times runs to a multiple of what it
    declares and is reported as breaking its own declaration **on both legs alike** — the
    symmetric-overrun shape this layer has twice been fixed to remove. One iteration is assumed,
    which is what a component's enter and exit animation is, and the endlessly repeating kind
    (a spinner) never reaches `animationend` and so closes no run and is never measured.
    **Whoever next touches `capture.js` should add `animation-iteration-count` to `STYLE_PROPS`
    and teach `Duration` to multiply**, at which point this residual closes; the `stopTimeline()`
    leak already has that file open. Written up on `TimelineComparator.Duration`'s `<remarks>`.

    **A second route to the same signature, rarer, same mitigation:** `animation-delay` is not in
    `STYLE_PROPS` either. A **negative** `animation-delay` starts the animation already part way
    through, so `animationstart` fires at once and the measured span is the declared duration
    less the delay; `Overruns` compares an absolute distance, so that reads as a symmetric
    **under**run `Error` on byte-identical legs. **Not introduced by any recent change, so there
    is no regression to hunt for** — `transition-delay` *is* captured but is never read, so a
    negative one has always done the same to a transition run. Add `animation-delay` to
    `STYLE_PROPS` and subtract a negative delay in the same pass as the iteration count.

    **The deferred `startTimeline()` work, briefed here because this is the only item holding
    `capture.js` open.** `startTimeline()` runs for `settle: animation` steps only, so every
    other step reports whatever has accumulated since the last animation step armed a
    recording. The fix is to record per step — call `startTimeline()` for every step, and give
    `stopTimeline()`, which still has no caller, one. Three facts whoever takes it must carry,
    all established by probe rather than by reading:

    - **The teardown is leg-dependent.** `seekAnimations()` detaches the recording only when it
      finds something to seek, so on a step where one leg animates and the other does not, the
      *following* non-animation step's timelines differ by construction: the animating leg's is
      frozen at the previous step's contents while the other's keeps accumulating. It is a
      derivative of a difference the pixel/frame comparator already reports loudly — the frames
      exist on one leg only — so it hides nothing, and it disappears the moment a per-step
      recording lands.
    - **An animation step's timeline carries more than the step.** The teardown happens at the
      first *seek*, not at `capture()`, so the record the next step inherits is the previous
      step's plus whatever fell in the `capture()` → first-seek window, which is where the
      settled screenshots are taken. `ParityCapturerTests.LeavesTheStepAfterAnAnimationStep…`
      asserts the two steps' timelines equal; that holds for the probe, whose window is empty,
      and is not the general shape. The comment on the assertion now says so.
    - **The resume artifact is closed** (Task 10: `resumeAnimations()` holds its promise for two
      animation frames before resolving, so the phase crossing it causes is dispatched while the
      recording is still detached). Before that it was dispatched a frame after the resume
      returned, and the capturer re-arms after one aria-snapshot round trip — shorter than a
      frame — so it was recorded in the *next* step, which two consecutive animation steps (a
      popup that opens and then closes) produce as a matter of course. Pinned by
      `CaptureScriptTests.ResumingDoesNotResolveUntilThePhaseCrossingItCausesHasBeenDispatched`
      and end to end by `ParityCapturerTests.KeepsItsOwnResumeOutOfTheNextAnimationStepsRecord`.
      One residue, never observed in 15 consecutive runs: the two-frame wait carries a 250 ms
      fallback timer — the guard `SettleProtocol`'s quiesce loop carries, against the same hang
      — so a page not servicing animation frames would resolve early and the artifact would
      return. Such a page fails `SettleProtocol` first.

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
