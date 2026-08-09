# Component Parity Harness — Design

**Date:** 2026-07-27

**Decision consolidation:** 2026-08-09

**Status:** Approved; Wayfinder decisions resolved; remaining implementation follows the pipeline plan

## Purpose

Automate the comparison currently done by hand in `docs/audits/*-parity-matrix.md`: capture the
upstream React demo and the Blazix port, compare their observable behavior, and either fail or record
one precise, reviewed deviation.

The suite answers three questions:

1. Does a named Blazix fixture lack behavior its paired upstream fixture exposes?
2. Is each observed difference a defect or a reviewed limitation?
3. Has a reviewed limitation regressed, disappeared, or changed identity?

It does not infer repository-wide parity from a partial corpus.

## Context and constraints

| Fact | Consequence |
| --- | --- |
| `.base-ui` is gitignored and absent from worktrees and CI | Live React capture is local/manual; committed React baselines make PR CI independent of Node and `.base-ui` |
| Upstream currently has 114 Tailwind demos across 38 components | Milestone 1 deliberately validates 29 named fixtures; issue #176 owns expansion to the then-current remainder |
| React fixtures are upstream demos, not forks | Actions and completion checks address common ARIA/role/browser-observable state rather than added React hooks |
| Server and WASM have different scheduling and transport | Both legs are required; elapsed time may differ, but the completion contract and verdict do not |
| No supported public browser API exposes descendant Blazor render-batch completion in both modes | Each action declares its observable consequence; private renderer APIs, fixed settle delays, host generation counters, and global quiet horizons are forbidden |
| Timing tests share a four-thread test process | Timing comparators run in a non-parallel collection |

## Ratified decisions

| Decision | Choice |
| --- | --- |
| React source | Committed baseline by default; `PARITY_LIVE=1` and baseline writes are local/manual |
| Milestone corpus | Fixed denominator of 29 named fixtures; validates the method and only the evidence named in the report |
| Remaining corpus | #176 starts after all first-29 findings have dispositions; recount at the then-pinned SHA |
| Render modes | React reference plus required Blazor Server and WASM candidates |
| Action completion | Required per-action observable postconditions; typed, non-waivable failure when unmet |
| Selector findings | `SelectorUnresolved` and `SelectorNonActionable` are distinct exact identities |
| Comparator trust | Repair node correspondence and run-aware timeline evaluation before waivers |
| Waivers | Exact, expiring, reviewed finding records; no wildcard/quarantine semantics |
| Reporting | HTML and JSON retain primary evidence, exact scope, retries, waivers, and milestone denominator |
| Unattended trigger | Required baseline-only PR check plus daily metadata-only staleness canary |
| Architecture | One C# Playwright runner; one capture script; explicit exhaustive comparator registry |

## Architecture and run flow

```text
manifest + committed React baseline
              |
              v
      action dispatch on one leg
              |
              v
   declared completion postcondition
              |
              v
 portal gate + mutation quiescence
              |
              v
 capture -> comparator registry -> findings
              |
              v
 retry correlation -> exact waivers -> HTML/JSON verdict
```

The live integration core constructs one `ComparisonContext` for every paired step and passes
`FixtureEntry.PixelThreshold` through to the pixel comparator. The registry names every supported
`FindingKind` exactly once and fails an exhaustiveness test when a kind is added without a comparator.

In live mode, missing `react-fixtures/dist/` fails before navigation with the exact build command.
Baseline mode never needs that bundle. The deliberately broken canary traverses the same capture,
context construction, registry, and comparison composition as real fixtures in both Server and WASM.

## Invocation and baselines

| Variable | Effect |
| --- | --- |
| `PARITY_LIVE=1` | Capture React in-process from the locally built bundle |
| `PARITY_WRITE_BASELINES=1` | Persist the live React captures; implies live mode |
| `PARITY_FIXTURES=<glob>` | Diagnostic subset only; never satisfies the 29-fixture milestone |
| `PARITY_REPORT_DIR=<path>` | Override report output |

Each committed baseline records the pinned upstream SHA, source/demo content hash, fixture/step
scope, capture schema version, browser/OS pixel provenance, and generated time. A local live/write run
fails on a source-hash mismatch with the refresh command.

PR CI uses baseline mode only: no `PARITY_LIVE`, `.base-ui` clone, pnpm install, or React build. A PR
that changes the declared upstream pin or baseline provenance inconsistently fails. Upstream movement
outside the repository is detected by a separate daily metadata-only canary, which creates or updates
one deduplicated tracking issue but does not freeze unrelated pull requests.

Pixel baselines used by required CI are Linux-captured or explicitly per-OS. The required job installs
Chromium and Linux Skia native assets; pixel comparison is not silently waived.

## Fixture manifest and action completion

Every action has exactly one completion contract: either a non-empty `complete` list, all of whose
predicates must hold, or `actionOnly` with a non-empty reason. This supports one load-bearing predicate
without preventing a high-value open/select action from requiring a conjunction.

```jsonc
{
  "id": "popover/hero",
  "component": "popover",
  "react": "popover/demos/hero/tailwind/index.tsx",
  "blazor": "Popover/Hero",
  "themes": ["light"],
  "pixelThreshold": 0.001,
  "steps": [
    { "name": "initial" },
    {
      "name": "open",
      "do": [{
        "click": "@trigger",
        "complete": [
          { "selector": "@popup", "state": "visible" },
          { "selector": "@trigger", "attribute": "aria-expanded", "equals": "true" }
        ]
      }],
      "settle": "animation"
    },
    {
      "name": "escape",
      "do": [{ "key": "Escape", "complete": [{ "selector": "@popup", "state": "detached" }] }],
      "settle": "animation"
    }
  ]
}
```

Supported predicates are:

- alias-backed selector state: `attached`, `detached`, `visible`, or `hidden`;
- DOM attribute or property equality;
- input value equality;
- focus equality or inequality.

Aliases are expanded before waiting and before recording finding identity. Manifest loading rejects a
missing contract, both contract forms on one action, an empty predicate list, an unknown predicate,
or a blank `actionOnly.reason`.

`actionOnly` is legal only for an intentionally no-render/browser-only action with no stronger
common predicate. It is not a convenience escape. Focus uses focus equality; clicks, keys, typing,
selection, open/close, and validation ordinarily require state predicates. The reason is report
metadata, not a waiver.

For every step, the capturer:

1. starts a fresh timeline recording before the first action;
2. dispatches an action;
3. waits for every declared predicate on that same leg;
4. begins the next action only after the current contract completes;
5. after all actions, runs the portal gate and two-frame mutation quiescence;
6. stops that step's timeline and captures the final state.

The fixture-host generation counter rejected by #142 is not part of this design. A fixture-specific
token is also absent: the #177 prototype showed it arrived with the meaningful DOM predicate and
added no capability.

### Completion failure

If a declared consequence misses its configurable deadline, emit non-waivable
`ActionCompletionUnmet/Error` with fixture, leg, step, zero-based action index, verb, expanded
selector, predicate/expected value, and bounded observed state. Stop later actions in that step, then
quiesce and capture DOM, ARIA, console, timeline, and screenshots for diagnosis. Do not throw away
the parity evidence and do not treat temporary quiet as success.

Selector absence, selector non-actionability, and completion failure remain three different typed
facts. A failure on both legs is still an invalid action contract/shared fixture failure.

## First-milestone scope and claim

The first milestone contains 29 named fixtures across 20 components and all designed mechanism
classes. Its denominator never shrinks because of filters, failures, missing legs, exclusions, or
completion failures.

Milestone completion requires:

1. the production runner executes all 29 fixtures in both Server and WASM;
2. every action completes or has a valid `actionOnly` declaration;
3. the canary produces its known findings through the same composition in both modes;
4. every finding has a component fix, justified waiver, harness fix, or explicit unresolved blocker;
5. no unwaived Error remains, and retry/Flaky results remain visible;
6. baseline metadata and the report record the full evidence scope.

The only permitted claim is:

> At upstream Base UI SHA `<sha>`, the parity harness executed `<executed>/29` declared Milestone 1
> fixtures against both Blazor Server and WebAssembly through the production capture, comparison,
> waiver, and reporting pipeline. The corpus spans 20 components and every designed mechanism class.
> All findings have recorded dispositions and no unwaived errors remain. This validates the parity
> method and establishes parity evidence only for the named fixtures, steps, modes, capture
> dimensions, tolerances, retries, and waivers in the report; it is not evidence of repository-wide
> or component-wide parity.

If `<executed>` is less than 29, or a tracked blocker still leaves a required Error undisposed, the
milestone is incomplete. A unit suite, canary, filtered run, or green CI cadence cannot satisfy it.

Issue #176 owns corpus expansion. It begins after all first-29 findings have dispositions, not after
an arbitrary green period. Its first action is to recount upstream demos at the then-pinned SHA and
reconcile the exact remainder. Every upstream Tailwind demo at that SHA ultimately needs an
executable two-mode fixture or an explicit reasoned exclusion.

## Selector comparison

`UnresolvedSelectors` and `NonActionableSelectors` are compared separately as ordinal multisets, so
repeated failures preserve multiplicity and categories never cancel:

- `SelectorUnresolved`: the expanded selector matched no attached element;
- `SelectorNonActionable`: an attached element could not be driven.

Each difference is `Error` in either direction. `Property` is the exact expanded selector,
`NodePath` is empty, and `ReferenceValue`/`CandidateValue` are invariant decimal counts including
zero. Message text is presentation only. Two separate `IComparator` implementations own the two
kinds, and the registry must include both.

## Node correspondence and comparator trust

Node correspondence is a prerequisite to waivers. Before Task 10c, the integration core is discovery
only: no waiver, baseline certification, or equality claim may be derived from one-sided Structure
output. Task 10c must make every downstream pair correspondence-backed or emit a stable explicit
uncertainty finding. Its adversarial acceptance corpus includes childless same-key wrapper collision,
corroboration cross-pair, wrapper plus identity change, and reorder below a stepped level. Truthful
Structure output and downstream Attribute, ARIA, style, and geometry coverage must survive.

A Structure waiver never cascades to descendants and absence of pair-dependent findings never means
the subtree agrees.

## Findings and comparators

Findings carry `{ kind, severity, fixture, leg, step, nodePath, property, referenceValue,
candidateValue }`. `Message` is presentation only.

| Kind | Contract |
| --- | --- |
| `Structure`, `Attribute`, `AriaSnapshot`, `ComputedStyle`, `CustomProperty`, `Geometry`, `Focus`, `Console`, `Marker`, `Pixel` | Existing exact/tolerance contracts from Tasks 6–10 |
| `SelectorUnresolved` | Ordinal-multiset difference in absent expanded selectors |
| `SelectorNonActionable` | Ordinal-multiset difference in attached-but-undriveable selectors |
| `ActionCompletionUnmet` | Non-waivable per-leg action consequence failure |
| `Timeline` | L1 sequence, run-aware L2 phase obligations, and L3 duration evidence |
| `ApiSurface` | Separate committed-snapshot subsystem; required by CI activation |

## Animation comparison and reporting precedence

Timeline capture is per step, armed before actions and stopped after completion plus quiescence.

- **L1 sequence** is strict and primary. It retains run, insertion, and removal differences.
- **L2 phase ordering** is evaluated per explicit run. Its stable `Property` encodes invariant,
  family/property, and zero-based run ordinal (for example
  `present-at-transitionend@transition:opacity#1`) before becoming waiver-eligible. This keeps the
  exact six-field waiver identity without hiding duplicate runs.
- **L3 duration** judges each leg against its own declaration. When run counts differ, do not print
  index-cross-paired values; a valid per-leg overrun may remain without a misleading opposite value.
  Keyframe declarations account for finite iteration count and negative animation/transition delay;
  byte-identical repeated-animation or negative-delay runs must not become symmetric false Errors.

Reports present L1 before and above L2 whenever both fire. L2 is subordinate diagnostic detail, not
the headline determination of which leg broke the step. Structure one-sided/reorder evidence appears
before pair-dependent findings, and uncertain correspondence is explicit.

## Waivers

A waiver is a reviewed record for exactly one stable finding identity, never a suppression language.

```jsonc
{
  "fixture": "drawer/hero",
  "leg": "Server",
  "step": "close",
  "nodePath": "portal(1) > div[role=dialog]",
  "kind": "ComputedStyle",
  "property": "transition-duration",
  "propertyMatch": "exact",
  "reason": "The accepted product-specific divergence is documented in the linked audit.",
  "disposition": "accepted-limitation",
  "docLink": "docs/audits/drawer-parity-matrix.md#animation",
  "expires": "2026-12-31"
}
```

The six identity fields are exact `fixture`, `leg`, `step`, `nodePath`, `kind`, and `property`.
No field accepts `*`, glob, regex, arrays, or omission-as-wildcard. Case-sensitive ordinal matching
is used for paths and properties. One entry consumes one distinct Error identity and never cascades
to descendants, siblings, kinds, legs, steps, runs, or report groups. If one identity can occur more
than once, add a stable discriminator before allowing it to be waived.

`propertyMatch` defaults to `exact`. `prefix` is legal only for `Console`, must include the level and
a stable semantic stem, must describe an observed volatile suffix, must be a short-lived
`deferred-defect` linked to an issue with examples from at least two attempts, and must match exactly
one finding. Substring, suffix, regex, and bare-level prefixes are forbidden.

Every entry requires a non-whitespace reason, `accepted-limitation` or `deferred-defect` disposition,
an appropriate repository audit/spec link or open issue URL, and an ISO expiry later than review.
Unknown fields, duplicates, malformed links/dates/enums, illegal match modes, zero-match entries,
ambiguous matches, and expired entries block before parity verdicting. There is no expiry grace
period. `Info` and `Flaky` neither require nor consume waivers.

Accepted limitations link durable documentation. Deferred defects link an open owned issue with
acceptance criteria and use a short expiry. Waivers are never allowed for harness uncertainty,
missing evidence, infrastructure failure, comparator defects, incomplete captures, or
`ActionCompletionUnmet`.

Waived primary evidence remains visible in HTML, JSON, and generated limitations with exact scope,
values, reason, disposition, link, expiry, and status. Unused or expired diagnostics cannot be omitted.

## Retry and failure semantics

Retry once to confirm a parity finding, not to erase an error. Correlate attempts with the same
machine identity used by waivers; messages and values do not determine identity.

- The same identity on both attempts is stable even when presentation values change.
- An Error present in one attempt and clean in the corresponding scope in the other may be `Flaky`.
- Errors in the same scope with different identities remain blocking unless a legal exact/console
  prefix waiver resolves them.
- Stable Structure and authoritative L1 errors never demote because subordinate labels vary.
- Timeouts, exceptions, browser/host failures, missing legs, incomplete capture, and failed retries
  are execution failures, never `Flaky`.

## Reports

`parity-result.json` and the offline HTML report include:

- counts by component, kind, severity, leg, fixture, and disposition;
- exact upstream SHA, fixed denominator, executed count, fixture ids, steps, modes, comparator
  dimensions, exclusions, thresholds, retry/Flaky results, and applied waivers;
- three-up React/Server/WASM screenshots and diff overlays;
- Structure before pair-dependent details; L1 before subordinate L2;
- completion failures and skipped dependent actions;
- waived evidence with full review metadata;
- unused, expired, malformed, and ambiguous waiver diagnostics.

Filtered runs are clearly labelled diagnostic and do not regenerate full-corpus limitations or make
milestone claims.

## Required PR CI and freshness canary

After Task 14b provides the production test surface, Task 18 activates:

1. a required baseline-only PR job for relevant changes, initially broad rather than prematurely
   path-filtered;
2. both Server and WASM legs against the same committed React baseline;
3. Chromium, Linux Skia native assets, Linux-compatible pixel baselines, provenance/pin validation,
   and the committed-snapshot API-surface check;
4. a daily and manually dispatchable metadata-only freshness canary.

The PR job blocks on any unwaived stable Error, execution/configuration failure, invalid/unused/
expired/ambiguous waiver, missing baseline, provenance inconsistency introduced by the PR, API
surface failure, or failure to execute either leg. `Info` and proven `Flaky` findings report without
blocking. A timeout, crash, or missing leg never becomes Flaky.

The freshness canary compares baseline provenance and the declared upstream pin with the tracked
upstream revision. Drift fails the scheduled job and creates or updates one deduplicated issue with
recorded/observed revisions, baseline age, and refresh command. It is an alarm, not branch protection.

Time the first integrated 29-fixture run before setting final timeout, cache, or sharding policy.

## Implementation order

Tasks 1–10 are complete at unit/component level. Remaining work is dependency-ordered:

1. Task 5b: distinct selector comparators and exhaustive registry semantics.
2. Task 5c: per-action completion contracts, typed failures, dependent-action stop, per-step timeline.
3. Task 10b/14a: live registry/context integration, bundle precondition, two-mode canary and smoke,
   with no waivers or certification.
4. Task 10c: node correspondence and run-aware timeline trust repair.
5. Task 11: exact waiver policy and conservative retry correlation.
6. Task 12: committed baselines and provenance.
7. Task 13: reports with primary-evidence precedence and evidence scope.
8. Task 14b: public theories, retry, waiver application, assertions, accumulation, report emission.
9. Task 18: required PR job and daily freshness canary.
10. Tasks 15–16: author and dispose findings for all 29 fixtures.
11. Task 17: generated limitations and bounded milestone documentation.
12. Issue #176: expand to the recounted remaining upstream corpus.

## Out of scope

- Implementing the #176 expansion inside Tasks 15–17.
- Live React capture in CI.
- Fixture-specific completion or private renderer instrumentation without a new demonstrated need and
  explicit design review.
- LLM classification, universal dark-mode capture, and cross-browser pixel baselines.
