# Parity Harness — SDD Handoff

**Updated:** 2026-08-09

**Branch:** `enhancement/feature-parity-check`

**Decision state:** Wayfinder #134 decisions resolved; follow the consolidated spec and plan

Spec: `docs/superpowers/specs/2026-07-27-component-parity-harness-design.md`

Plan: `docs/superpowers/plans/2026-07-27-parity-harness-pipeline.md`

## Current implementation state

Tasks 1–10 exist and are unit/component tested. At consolidation commit `27101e3f`, no production
code constructs `ComparisonContext`, no exhaustive comparator registry or runner exists, and no
comparator has run end to end. The current manifest contains only the placeholder `switch/hero`.

Do not treat the historical green unit suite as parity evidence. The first integration run is an
explicit discovery gate and is expected to expose assumptions that isolated tests missed.

| Work | State |
| --- | --- |
| Tasks 1–10 | Complete at unit/component level |
| Task 5b selector comparators | Not started; semantics resolved by #140 |
| Task 5c action completion | Not started; contract resolved by #177 |
| Task 10b/14a integration core | Not started; ordering resolved by #141 |
| Task 10c trust repair | Not started; required by #138 before waivers |
| Tasks 11–18 | Not started; all blocking policy decisions resolved |
| Tasks 15–17 first-29 evidence | Not started |
| #176 expansion | Charted follow-up; starts after first-29 disposition gate |

## Authoritative execution order

```text
5b -> 5c -> 10b/14a -> 10c -> 11 -> 12 -> 13 -> 14b -> 18 -> 15 -> 16 -> 17 -> #176
```

- 14a is unwaived live discovery only.
- 10c must land before any waiver or parity certification.
- 18 activates required baseline-only PR CI after 14b creates the production test surface.
- 15–16 must execute all 29 fixtures in both modes; 17 publishes only the bounded claim.

## Resolved decision ledger

### Selector semantics — #140

- Add `SelectorNonActionable`; keep `SelectorUnresolved` for absence only.
- Compare the two captured collections separately as ordinal multisets.
- Emit Error in either direction; exact expanded selector is `Property`, `NodePath` is empty, and
  values are invariant decimal counts.
- Use two comparators and register both exhaustively. Message text is never identity.

### Settle and action completion — #142 and #177

- The fixture-host render-generation counter is rejected. It acknowledged the host render while a
  descendant async update remained about 225–242 ms away in Server and WASM.
- Every action declares exactly one completion contract: a non-empty all-of predicate list or
  `actionOnly` with a non-empty reason.
- Predicates cover selector state, attribute/property equality, input value, and focus.
- Arm a fresh timeline before actions; dispatch -> await consequence -> next action; then portal gate
  and mutation quiescence -> stop timeline -> capture.
- A missed consequence emits non-waivable `ActionCompletionUnmet/Error`, stops later actions in that
  step, then quiesces and captures diagnostic state.
- No private renderer API, fixture token, fixed settle delay, or global quiet horizon.

### Integration order — #141

- Pull registry, `ComparisonContext`, pixel-threshold propagation, live orchestration, bundle
  precondition, two-mode canary, and two-mode real smoke into Task 10b/14a.
- Keep waiver/baseline/report/public theory layers downstream.
- Missing live React bundle fails early with the exact build command.

### Comparator quality bar — #138

- “Mislabelled positive, never silent pass” is not waiver-safe.
- After 14a findings and before Task 11, Task 10c repairs NodeMatcher correspondence, run-aware L2,
  and misleading unequal-run L3 pairing.
- Structure precedes pair-dependent evidence; L1 precedes subordinate L2.
- Before repair, wrapper/mispair Structure and multi-run L2 are non-waivable and cannot certify
  equality.

### Waivers and retry — #139

- Exact six fields: fixture, leg, step, nodePath, kind, property. No wildcard or quarantine.
- Required reason, disposition, appropriate link, and future ISO expiry.
- Strict JSON; unused, expired, malformed, duplicate, and ambiguous entries block.
- Console-only prefix is narrowly allowed for one documented volatile suffix class and exactly one
  match; all other matching is exact.
- One waiver consumes one distinct Error identity and never cascades.
- Retry correlates machine identity, not messages/values. Stable Structure/L1 never demote because
  subordinate labels vary. Execution failures are never Flaky.
- `ActionCompletionUnmet`, harness uncertainty, incomplete evidence, and execution failures are
  non-waivable.

### Unattended trigger — #136

- Required baseline-only PR check, both Server and WASM, Chromium, Linux Skia, Linux-compatible
  pixels, provenance/pin validation, and committed API snapshot.
- No Base UI clone, pnpm, or live/write mode in CI.
- Daily/manual metadata-only freshness canary opens or updates one deduplicated drift issue. Scheduled
  upstream drift alarms but does not block unrelated PRs.
- Activate after 14b; set timeout/cache/sharding from measured integrated runs.

### Milestone scope — #137 and #176

- The denominator is fixed at 29. Missing fixtures, actions, modes, or failures lower executed count;
  they never shrink the denominator.
- The result validates the method and named evidence only, not repository/component parity.
- Reports must contain upstream SHA, `<executed>/29`, ids, steps, modes, dimensions, exclusions,
  retries, thresholds, and waivers.
- #176 starts after every first-29 finding has a disposition. Recount upstream at the then-pinned SHA
  and chart the exact remainder; do not wait for an arbitrary green cadence.

## Primary implementation traps

- `.base-ui` lives at the main repository root and is absent from worktrees. Resolve it through
  `BaseUiLocator`/`PARITY_BASE_UI_PATH`.
- `react-fixtures/dist/` is gitignored. Live integration must validate it before navigation; baseline
  CI must not require it.
- `TreatWarningsAsErrors=true`; an unused import fails the build.
- `parity.css` must be regenerated whenever fixture class strings change.
- Pixel baselines need explicit Linux/per-OS provenance before required CI.
- `startTimeline()` must be per step and paired with `stopTimeline()`; do not inherit a previous
  animation step's recording.
- A waiver is reviewed evidence, not a way to make an uncertain harness green.
- Commits carry no `Co-Authored-By` trailer.

## Milestone exit language

Use the exact bounded claim in the spec. Completion requires 29/29, both Blazor modes, successful
action completion, canary findings in both modes, all findings disposed, no unwaived Error, and
visible retry/Flaky evidence. A unit suite, canary alone, filtered run, or green cadence is not the
milestone.

## Closed former open items

The previous handoff's selector-kind question, host-counter proposal, blind-spot acceptance, console
waiver matching, runner ordering, trigger, 29-fixture claim, and corpus ownership are resolved above.
Do not reopen them during Tasks 11–17. New evidence may create a new issue, but implementation must
not silently substitute a different policy.

Residual implementation observations already scheduled by the plan—such as per-step timeline
teardown, animation iteration/delay capture, React bundle validation, and conservative console retry
tests—remain work, not unresolved product decisions.

## Handoff condition

The Wayfinder planning destination is reached. SDD can execute the remaining plan without a policy
pause. #134 may close after #143 records this durable consolidation.
