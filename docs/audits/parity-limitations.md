# Parity harness limitations

**Date:** 2026-08-19 (§1, §2 and §6 updated 2026-08-21 for the #213 re-baseline)

**Deliverable:** Task 17 of
[`2026-07-27-parity-harness-pipeline.md`](../superpowers/plans/2026-07-27-parity-harness-pipeline.md).

**Companion:** [`parity-milestone1-dispositions.md`](parity-milestone1-dispositions.md) records the
disposition of every milestone-1 finding. This document records what the harness cannot tell you.

> This page is hand-written. Task 17 also specifies a `LimitationsWriter` that generates the waiver
> section from the run's active waiver records; it is not implemented, and there are no active
> waivers for it to generate from (§4). When it lands, §4 becomes generated output and this banner
> must say so.

## 1. The exact denominator

| Dimension | Value | Source |
| --- | --- | --- |
| Declared milestone-1 fixtures | **29** | `tests/Blazix.BaseUI.Parity.Tests/manifest/milestone-1.json` |
| Distinct components | **26** | same file (`componentCount`) |
| Declared steps across the corpus | **87** | `manifest/fixtures.json` |
| Themes captured per fixture | **1** (`light`) | `manifest/fixtures.json` — no fixture declares `dark` |
| Pixel threshold | `0.001` for every fixture | `manifest/fixtures.json` |
| Actions with `actionOnly` instead of observable predicates | **0** | `manifest/fixtures.json` |
| Blazor legs per fixture | **2** (Interactive Server, Interactive WebAssembly) | `ParityLeg` in `Capture/CaptureBundle.cs` |
| Executed | **29 / 29** fixtures, **58 / 58** candidate legs, once | PR [#178](https://github.com/JakeMurrayDev/Blazix.BaseUI/pull/178) |

The design spec's prose says "29 named fixtures across 20 components". The machine-checked component
count in the manifest is **26**; use 26.

The denominator is fixed at 29 and never shrinks for filters, failures, missing legs, exclusions, or
completion failures. A `PARITY_FIXTURES` run is diagnostic and cannot contribute to it.

The corpus is 29 of the **116** upstream Tailwind demos counted at the current pin `1a2ca3c9f…`;
it was 114 at the superseded `bdcb685f…` pin, so the denominator moves when the pin does. It
validates the method and the named evidence — never repository-wide or component-wide parity.
Expansion to the remainder, and the recount that fixes the exact denominator, are owned by
[#176](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/176).

## 2. The baseline pin, and what still lags it

The two-pin gap this section used to describe is closed. **The committed baselines were recaptured at
`1a2ca3c9f8a39bd8c0dda939a7a23b72da226124` on 2026-08-21** (PR for
[#213](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/213)), which is the same pin every
cycle-1 audit and every #180-#200 fix was authored against.

| Fact | Value | Source |
| --- | --- | --- |
| Upstream SHA the committed React baselines are captured at | `1a2ca3c9f8a39bd8c0dda939a7a23b72da226124` (2026-08-03) | `baselines/metadata.json` `declaredRepositoryPin`; `baselines/chromium-macos-arm64/metadata.json` `upstreamSha` |
| When they were captured | `2026-08-21T05:15:00Z` | `baselines/chromium-macos-arm64/metadata.json` `generatedAtUtc` |
| Upstream SHA the cycle-1 audits and sweeps run against | the same `1a2ca3c9f…` | issue [#157](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/157) |
| Previous baseline pin, superseded | `bdcb685fadcca9d18b18f013c052795a53b6aa33` (2026-07-18, `@base-ui/monorepo` 1.6.0) | git history of `baselines/metadata.json` |

The pin is declared by hand in `baselines/metadata.json` and the baseline writer refuses to capture
against anything else (`BaselineStore.ValidateLiveProvenance`). Bumping the pin is therefore an
explicit reviewed edit, never a side effect of whichever revision `.base-ui` happens to sit at.

Three things still lag that pin, and every one of them bounds what may be claimed:

1. **No harness run at the new pin has been recorded.** The only committed run evidence — #178's
   4356 findings, 4149 blocking — was produced against `bdcb685f…` and describes the port as it was
   before the cycle-1 sweeps. It does not transfer. A full serial re-run is A-3.1 prerequisite (c),
   still outstanding.
2. **The Blazor fixture ports are still frozen at `bdcb685f…` class surfaces.** Five contract tests
   fail on exactly this — `CalibrationFixtureContractTests.FrozenRazorPortsCarryTheExactUpstreamTailwindClassMultisets`,
   `Task16BatchAFixtureContractTests.PreservesEveryUpstreamTailwindClassMultiset`,
   `Task16FloatingMenuFixtureContractTests.PreservesTheExactUpstreamTailwindClassDefinitions`,
   `Task16HighRiskFixtureContractTests.PinsExactEffectiveTailwindClassMultisetsFromPinnedReactSources`,
   and `Task16NavigationMenuHeroFixtureContractTests.PreservesTheExactUpstreamTailwindClassSurface`.
   Until the ports are re-diffed against the pinned React sources, a class-level difference reported
   by the comparator cannot be attributed to the components rather than to a stale port.
3. **Upstream has moved past the pin again.** The scheduled canary compares the declared pin against
   `mui/base-ui` `master`, so it reports `drift` whenever the repository is deliberately pinned
   behind head — as it is by design between sync cycles. After this refresh that alarm means "time to
   consider cycle 2", not "the baselines are stale relative to this repository's own source". Read the
   canary body for which of the two it is: the recorded pin now matches the pin the port is written
   against, and only the observed revision differs.

Baselines are also **platform-exact**. The recorded `platform` block reads `chromium`
`143.0.7499.4`, `macos`, `arm64` — those are the literal metadata values, not prose. The browser
version is unchanged across the re-baseline, so pixel movement in the refreshed set is attributable
to upstream rather than to a browser bump. There is no Linux platform set, so the pixel dimension has
no Linux-compatible baseline and the required CI job the spec describes cannot yet be activated (§7).

## 3. What the harness structurally cannot observe

The harness compares rendered output at step boundaries. Everything below is outside that window,
which is exactly why the milestone-1 source-parity sweep (population B of the dispositions ledger)
existed at all — roughly 100 real behavioral defects were found by reading the port against
`.base-ui`, in components whose fixtures the harness executed cleanly.

**Interaction mechanics**

- Event propagation and consumer-callback invocation counts (an ancestor `onclick` firing twice per
  root click is invisible to a DOM/pixel diff).
- Pointer geometry: hover rest timers, safePolygon, slip-out release, sloppy-touch distance and
  duration thresholds.
- Keyboard semantics beyond the focus path captured at step end: which key was consumed, whether
  `preventDefault`/`stopPropagation` ran, typeahead buffers.
- Focus lifecycle between steps: which manager restored focus, how many times, and to what.

**Timing**

- Anything that settles after a step's declared completion predicates plus the portal gate and
  two-frame mutation quiescence. State that arrives later is not captured.
- On the Blazor Server leg specifically, completion predicates can resolve before the SignalR round
  trip that starts an entry transition, so the settle poll can race past a transition that has not
  begun. Observed effect: `popover/hero` reported 42 / 41 / 11 findings across three runs.
  **Per-fixture finding counts on the Server leg are not reproducible and must not be quoted as
  stable** until the settle protocol gains a transition-start gate.

**Environment**

- Dark mode: every fixture declares `light` only.
- Browsers other than Chromium, operating systems other than macOS, architectures other than arm64.
- RTL, reduced motion, virtual keyboard, iOS/Safari-specific paths, high-DPI variants.

**Comparator scope**

- `class` differences are reported as `Info`, never `Error`, by design: `size-3.5` and `h-3.5 w-3.5`
  are class-set different and computed-style identical, so computed style is the real assertion.
- `ApiSurface` is named by the spec as a separate committed-snapshot subsystem and a Task 18 CI
  activation dependency. **It does not exist in this repository** — no implementation, no committed
  snapshot — so no `ApiSurface` finding can be produced and the API check the required CI job would
  invoke has nothing to run.

**Test-harness blind spots that shaped the fixes**

- bUnit has no pointer or focus model. Five shared-JS parity items were therefore unprovable in unit
  tests and had to be retired with Playwright coverage instead (§5).
- Running the whole Playwright assembly as one parallel run is unreliable on the development machine
  (fixture stalls, contention-induced false failures). The dependable protocol is serial, per test
  class. This constrains how and when parity or Playwright evidence can be regenerated.

## 4. Active waivers: none

`tests/Blazix.BaseUI.Parity.Tests/waivers/waivers.json` is `[]`.

Read that as *nothing is suppressed*, not as *nothing differs*. The last full run reported **4356
findings, 4149 of them blocking**, and they are outstanding: see A-3.1 in
[`parity-milestone1-dispositions.md`](parity-milestone1-dispositions.md).

Why the registry is empty rather than populated:

- A waiver's identity is the exact six-tuple `(fixture, leg, step, nodePath, kind, property)` of a
  real Error from a real run (`Waivers/Waiver.cs`).
- `WaiverMatcher` emits a blocking diagnostic for any waiver matching zero findings, for any
  ambiguous match, for any expired entry, and for any non-waivable kind
  (`CorrespondenceUncertain`, `ActionCompletionUnmet`, and `FixtureError`); the selector kinds are
  waivable.
- No run report is committed, so no finding identity from the #178 run is citable. Writing waivers
  from PR prose would produce unused entries, which block the suite instead of documenting anything.

When waivers do get written, each one needs a non-whitespace reason, an `accepted-limitation` or
`deferred-defect` disposition, a durable documentation link (or an open, owned issue with acceptance
criteria for a deferred defect), and a future ISO expiry. There is no grace period.

## 5. The retired Playwright-blocked cluster

Five parity items were deferred across PRs #186–#189 for one shared reason: they live in shared JS
(`createHoverInteraction`, `createEscapeKeyHandler`, the touch dismissal machine, the focus guards)
consumed by Menu, Select, Combobox, Popover, Tooltip, PreviewCard, and NavigationMenu, and **bUnit
cannot observe any of them**. Landing them under unit tests would have meant untested changes to
shared infrastructure.

PR [#190](https://github.com/JakeMurrayDev/Blazix.BaseUI/pull/190) retired the whole cluster with
Playwright coverage inherited by both render modes, each test proven non-vacuous by flipping its gate
in both the `.js` source and the regenerated `.min.js` that the runtime actually loads:

1. `restMs` is a genuine rest timer for Tooltip and Popover — sweeping the cursor across a trigger no
   longer opens the popup.
2. A tooltip trigger nested inside another no longer opens both tooltips.
3. Escape closes one popup per keypress, focused root first, without leaking into enclosing dialogs.
4. Sloppy-touch outside press: >10 px dismisses mid-gesture, >5 px arms dismissal at touchend, a >1 s
   long press never dismisses, and a clean tap dismisses via the browser-synthesized mousedown.
5. NavigationMenu closes when focus leaves it, and Safari no longer lands focus on an invisible guard.

The follow-ups #190 listed are closed: restMs for standalone Menu roots and NavigationMenu (#198),
sloppy touch for Select/Menu/non-modal Dialog (#197), the combobox empty-list Escape corner (#199),
and the four pre-existing master test failures (#191, #192, #194, #195, with #193/#196 for the
disabled-callback siblings).

These fixes ship with nine deviations that are deliberate and reviewed, and they are limitations of
the port as it stands. A tenth bullet follows them: it is not a deviation but the pre-existing
gap #190 recorded without adopting, dispositioned as B-189.19 in
[`parity-milestone1-dispositions.md`](parity-milestone1-dispositions.md).

- Escape's `preventDefault`/`stopPropagation` are unconditional — cancellation is not knowable
  synchronously across the async .NET boundary, where upstream skips them for a canceled change.
- Same-phase capture siblings (tooltip vs. select vs. menu) cannot suppress each other; multi-family,
  multi-open corners are registration-order dependent.
- Select's Escape pick is last-in-Map, reachable only programmatically.
- Safari's compositionend-before-keydown hold is not replicated in the shared IME guard
  (`popover.js` has the faithful pattern).
- `touchState` is retained until the synthesized mousedown, where upstream nulls it at touchend.
- Popover uses one global touch machine, so gesture-based dismissal of a *second* open root whose
  gesture starts inside another root is lost; taps still dismiss.
- Touch test primitives (synthetic `TouchEvent`s plus CDP) are Chromium-only; `firefox`/`webkit` runs
  would need alternates.
- NavigationMenu's global focus-out handler ignores a null `relatedTarget`, because Blazor re-renders
  transiently produce one and the alternative caused spurious closes. Residual gap: a programmatic
  `blur()` or focus into browser chrome leaves the menu open where upstream closes.
- NavigationMenu's viewport un-inert in the after-outside guard branch is JS-synchronous against an
  async .NET state sync (upstream uses `flushSync`); a mid-window re-render could transiently restore
  `inert` in the inline/no-positioner configuration.
- *(not a deviation — tracked blocker B-189.19)* NavigationMenu's viewport always renders both focus
  guards; upstream renders none when closed inline, so a closed inline menu exposes two dead tab
  stops.

Two further shipped deviations from earlier waves: `restMs` keys on `isOpen` because the port's JS
state has no `mounted` equivalent, so the full delay still applies during an exit transition (#189);
and Slider does not call `OnDragMove` without a realtime subscriber attached, so a drag that leaves
and returns to its origin does not commit where upstream would (#185).

## 6. What may and may not be claimed today

The design spec permits exactly one milestone claim, and it requires that "all findings have recorded
dispositions **and no unwaived errors remain**". The second half is false: 4149 blocking findings
from the last full run are outstanding. **The bounded milestone claim is therefore not published.**

What the evidence does support, stated in full:

> At upstream Base UI SHA `bdcb685fadcca9d18b18f013c052795a53b6aa33`, the parity harness executed
> 29/29 declared Milestone 1 fixtures — 26 components, 87 steps, `light` theme only, pixel threshold
> 0.001, zero `actionOnly` actions — against both Blazor Server and Blazor WebAssembly through the
> production capture, comparison, and reporting pipeline, on the recorded platform set
> `chromium` 143.0.7499.4 / `macos` / `arm64`.
> All 58 candidate legs completed, with zero `ActionCompletionUnmet`, `FixtureError`,
> `SelectorUnresolved`, or `SelectorNonActionable` findings and zero settle timeouts, and the
> deliberately broken canary produced its known findings in both render modes. This establishes that
> the method executes end to end over the named corpus. It does **not** establish that the compared
> output matches: the run reported 4356 findings (4149 blocking), no waivers are defined, and those
> differences have not been individually dispositioned. It is not evidence of repository-wide or
> component-wide parity, and it says nothing about upstream `1a2ca3c9f`.

That statement is now **historical**: the baselines it was produced against were superseded on
2026-08-21 (§2). It remains the only recorded run evidence, and it still describes `bdcb685f…`.
No re-run at the current pin has been recorded, so nothing stronger may be claimed yet.

## 7. CI coverage, and what is deliberately not automated

`.github/workflows/parity.yml` provides:

- a **scheduled and manually dispatchable freshness canary** that compares the declared upstream pin
  and the committed baseline provenance against upstream's tracked revision using repository and API
  metadata only — no `.base-ui` clone, no pnpm, no live capture — and creates or updates exactly one
  deduplicated tracking issue on drift. Drift fails the scheduled job; it is an alarm, not branch
  protection;
- an **offline provenance and waiver validation job** on pull requests touching the harness, which
  re-runs the same checks with `--offline` plus the script's own test suite.

It deliberately does **not** provide the required baseline-mode PR job the spec describes. That job
needs, and this repository does not yet have:

1. Linux-captured (or explicitly per-OS) pixel baselines — the committed set is macOS-only;
2. the committed-snapshot `ApiSurface` check, which has no implementation (§3);
3. dispositioned milestone-1 findings, since the job must block on any unwaived stable Error and
   4149 are outstanding;
4. a measured full-corpus runtime on CI hardware to set timeout, cache, and sharding policy — the
   only measurement that exists is 7m19s on a developer machine, and the suite must run serially
   there.

`.github/workflows/lint.yml` is unchanged and remains the repository's only required check. Repository
CI is otherwise lint-only: a green check proves formatting and analyzer rules, not correctness.

## 8. Start-gate status for #176

The #176 start gate asks for a recorded disposition per milestone-1 finding, where "explicitly
tracked unresolved blocker" is one of the four permitted dispositions. Every finding named by the
merged evidence now has one:
[`parity-milestone1-dispositions.md`](parity-milestone1-dispositions.md).

That is a weaker condition than the spec's milestone-completion criteria, which additionally require
that no unwaived Error remains. The gate is met; the milestone is not complete; the bounded claim
stays unpublished until a re-baselined, re-run, fully dispositioned corpus exists.
