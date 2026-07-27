# Component Parity Harness — Design

**Date:** 2026-07-27
**Status:** Approved (design); implementation plan pending

## Purpose

Automate the comparison currently done by hand in `docs/audits/*-parity-matrix.md`: for every base-ui
component demo, capture what React renders and what the Blazix port renders, diff them across several
dimensions, and either fail the build or record the difference as a documented, justified deviation.

The suite answers three questions:

1. Does the Blazix implementation lack functionality the upstream component has?
2. Where the two differ, is the difference a defect or an accepted Blazor limitation?
3. Has an accepted limitation silently become a regression, or silently been fixed?

## Context and constraints

Facts established while exploring the repo, all of which shape the design:

| Fact | Consequence |
| --- | --- |
| `.base-ui` is a gitignored local checkout at the main repo root (`.gitignore:373`), absent from worktrees | The React side cannot run in CI; anything derived from it must be snapshotted and committed |
| CI is lint-only (`.github/workflows/lint.yml`) — no test job exists | The suite is a local/developer tool first; a CI job is a later, separate decision |
| base-ui ships 114 `tailwind/` demo variants across 38 components under `docs/src/app/(docs)/react/components/<c>/demos/<demo>/tailwind/index.tsx` | These are the React fixtures; no React authoring required |
| Those demos are plain React with no Next.js coupling | They are standalone Vite-buildable (base-ui's own `test/regressions` uses this pattern) |
| base-ui uses Tailwind v4.2.4, CSS-first | One Tailwind build can scan both `.tsx` and `.razor` via `@source`, giving both sides one stylesheet |
| The Blazix docs site already ports demos 1:1 by name (`Demos/Select/{Hero,Grouped,Multiple,ObjectValues}`) but styles them with plain CSS | Blazor fixtures are a restyle of existing ports, not new authoring |
| base-ui emits 11 `data-base-ui-*` attributes; Blazix emits 23 `data-blazix-*` attributes, 20 of them `data-blazix-base-ui-*` | The marker relationship is a prefix rename, not a bespoke mapping table |
| Blazix emits both forms inconsistently — `PopoverPopup.razor:200` uses `data-base-ui-focusable`, `DrawerPopup.razor:375` uses `data-blazix-base-ui-focusable` | A known-shaped defect the suite should catch on day one |
| `xunit.runner.json` runs `maxParallelThreads: 4` | Timing-sensitive comparators must be isolated into a serial collection |

## Decisions

| Decision | Choice |
| --- | --- |
| React baseline strategy | Committed baselines by default; `PARITY_LIVE=1` recaptures |
| Fixture corpus | First milestone: 29 fixtures (a quarter of the 114); remainder follows once findings are triaged |
| Verdict model | Waiver file with a required `reason`; unwaived diffs fail |
| Output | Tailwind-styled HTML report + machine-readable JSON |
| Render modes | Blazor Server and WASM (plus a free Server-vs-WASM cross-check) |
| Extra dimensions | ARIA snapshot, keyboard/focus path, API surface, CSS custom properties + floating geometry |
| Architecture | One C# runner; React side pre-built to a static bundle |

### Why one C# runner

Both sides are captured by the same C# Playwright driver, in the same browser context, with the same
injected `capture.js`. This is what makes a pixel diff meaningful: two Playwright installs would mean
two browser revisions, and screenshots would then differ for reasons unrelated to the components.
Node is required only to rebuild the React bundle when base-ui is bumped, never at test time.

Rejected alternatives: two native runners sharing a capture script (gives up browser-build parity,
and requires keeping two drivers in lockstep on viewport, DPR, reduced-motion, fonts, and settle
timing); an all-Node pipeline (puts parity results outside `dotnet test` in a .NET-first repo, and
moves the waiver registry away from the components it describes).

## Architecture

```
tests/Blazix.BaseUI.Parity.Tests/
├─ Blazix.BaseUI.Parity.Tests/          xUnit v3 + Playwright driver, diff engine, report
│    Infrastructure/  Capture/  Diff/  Waivers/  Report/  Tests/
├─ Blazix.BaseUI.Parity.Fixtures/       Blazor Web App hosting the 114 Tailwind fixtures
│    …Fixtures/ (server host)  …Fixtures.Client/ (Server + WASM routes per fixture)
├─ react-fixtures/                      Vite app; globs base-ui tailwind demos into routes
│    vite.config.mts  src/  dist/ (gitignored)
├─ shared/
│    capture.js                         injected into both sides by the same runner
├─ react-fixtures/src/parity.css        Tailwind input, scanning .tsx AND .razor
│  …/Blazix.BaseUI.Parity.Tests/wwwroot/parity.css   generated output, loaded by both sides
├─ manifest/fixtures.json               the 114 pairs + interaction scripts
├─ manifest/markers.json                data-blazix-* classification
├─ manifest/naming.json                 React prop → Blazor parameter conventions
├─ baselines/                           committed React capture bundles, screenshots, api/
└─ waivers/waivers.json
```

### Why a dedicated Blazor fixtures app

The fixture host must load **only** `parity.css`. Site chrome, layout CSS, or a Bootstrap reset
carried by the existing `Blazix.BaseUI.Playwright.Tests.Client` would appear in `getComputedStyle`
output and read as a component discrepancy. Stylesheet isolation is a correctness requirement, and
it is what rules out reusing the existing test app.

### Run flow

1. `pnpm --dir tests/Blazix.BaseUI.Parity.Tests/react-fixtures build` → static `dist/`
   *(only when base-ui changes)*
2. `dotnet test` — an assembly fixture starts a Kestrel static-file server for `dist/` and the Blazor
   fixtures app
3. For each manifest entry × `{react, blazor-server, blazor-wasm}`: navigate → settle → inject
   `capture.js` → replay the interaction script → collect a `CaptureBundle`. The React leg is skipped
   when baselines are current and `PARITY_LIVE` is unset.

### Invocation

Modes are selected by environment variable, following the repo's existing `PLAYWRIGHT_*` convention:

| Variable | Effect |
| --- | --- |
| `PARITY_LIVE=1` | Recapture the React leg in-process instead of loading committed baselines |
| `PARITY_WRITE_BASELINES=1` | Persist the captured React leg to `baselines/` (implies `PARITY_LIVE`) |
| `PARITY_FIXTURES=<glob>` | Restrict the run to matching fixture ids, e.g. `popover/*` |
| `PARITY_REPORT_DIR=<path>` | Override the report output folder |

`pnpm parity:baseline` is a thin wrapper that rebuilds the React bundle and runs
`dotnet test` with `PARITY_WRITE_BASELINES=1`.
4. Diff bundles → apply waivers → one xUnit assertion per fixture × mode
5. Emit `parity-report/` and `parity-result.json`

### Baseline staleness

Each baseline records the base-ui git SHA and a content hash of the demo files it derives from. If
the built bundle's hashes do not match the baselines, the run **fails** with
`baselines stale — run pnpm parity:baseline`, rather than diffing against stale output.

### Determinism knobs

Applied identically to both sides: viewport 1000×700, DPR 1, `--font-render-hinting=none`, a pinned
local font, `timezoneId: UTC`, `prefers-color-scheme: light`. `reducedMotion: reduce` is applied for
visual and style captures and **deliberately not** for animation captures, which run in a separate
browser context with motion enabled.

Dark mode is a per-fixture `themes` field defaulting to `["light"]` — opt-in per fixture rather than
tripling every run.

## Fixture manifest

```jsonc
{
  "id": "popover/hero",
  "component": "Popover",
  "react": "popover/demos/hero/tailwind/index.tsx",
  "blazor": "Popover/Hero",
  "themes": ["light"],
  "pixelThreshold": 0.001,
  "steps": [
    { "name": "initial" },
    { "name": "open",   "do": [{ "click": "@trigger" }], "settle": "animation" },
    { "name": "escape", "do": [{ "key": "Escape" }],     "settle": "animation" }
  ]
}
```

### First-milestone corpus

29 of the 114 demos. Selected to maximize **distinct mechanism coverage** rather than even spread —
the purpose of the first quarter is to surface discrepancies and calibrate tolerances, not to be
uniformly representative. 20 of 38 components are touched, and every mechanism class appears.

| Class | Fixtures |
| --- | --- |
| Sanity (no floating, no animation) | `switch/hero`, `avatar/hero`, `separator/hero`, `progress/hero`, `meter/hero` |
| Animation and mount/unmount | `collapsible/hero`, `accordion/multiple`, `dialog/hero`, `drawer/hero`, `toast/hero` |
| Floating and positioning | `popover/hero`, `tooltip/hero`, `preview-card/hero`, `menu/arrow`, `select/hero` |
| Composite and keyboard nav | `select/grouped`, `menu/checkbox-items`, `menubar/hero`, `tabs/hero`, `toolbar/hero` |
| Form and validation | `field/hero`, `form/hero`, `number-field/hero`, `checkbox/hero`, `otp-field/hero` |
| High-risk | `popover/detached-triggers-simple`, `navigation-menu/hero`, `scroll-area/hero`, `combobox/hero` |

`accordion/multiple` is preferred over `accordion/hero` because it exercises independent open state;
`menu/arrow` over `menu/hero` because it adds arrow positioning geometry.

**All 29 already have Blazor Tailwind ports** under
`docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Components/Demos/<Component>/<Demo>/Tailwind/`
(90 such ports exist repo-wide). Fixture authoring is therefore neither new component work nor a
restyle from CSS — the structure and component usage are already correct, and the task is to
**sync the class strings verbatim from the React demo**.

That sync is load-bearing, not cosmetic. The existing ports paraphrase: `Switch/Hero/Tailwind` uses
`ease-in-out` where base-ui uses `ease-[ease]`, and `h-3.5 w-3.5` where base-ui uses `size-3.5`.
Transplanting base-ui's exact strings ensures any surviving computed-style difference is attributable
to the **component** — a missing `data-checked` attribute means `data-checked:bg-white` never
applies — rather than to how the demo was written.

### Element addressing

The React demos are upstream files and are **not** forked, so `data-parity` hooks cannot be added to
them — forking would forfeit the guarantee that the React side is known-correct upstream usage.

Steps therefore address elements through **ARIA/role selectors**, expanded from a per-component alias
table: `@trigger` → `[aria-haspopup],[aria-expanded]`, `@popup` → `[role=dialog],[role=menu],[role=listbox]`,
`@item(n)`, `@input`.

Roles are the one contract both implementations are obliged to honour. When a selector resolves on
React but not on Blazor, the harness records a `SelectorUnresolved` **finding** rather than erroring —
the addressing scheme doubles as a parity check.

### Step action vocabulary

`click`, `hover`, `key`, `type` (with `into`), `focus`, `blur`, `scroll`, `wait`. `settle` is one of
`render` (default: quiescence) or `animation` (quiescence plus timeline capture).

## Capture contract

`shared/capture.js` is injected via `AddInitScriptAsync` into both hosts by the same C# runner —
literally the same bytes, so DOM walking, path computation, and the style allowlist cannot drift
between legs. It exposes `window.__parity.capture(step)` and `startTimeline()` / `stopTimeline()`.

`CaptureBundle` per step:

| Field | Content |
| --- | --- |
| `dom` | normalized tree (tag, attributes, text) |
| `aria` | Playwright `AriaSnapshotAsync()` — same C# call for both legs |
| `styles` | allowlisted computed properties, keyed by node path |
| `customProps` | `--*` values, keyed by node path |
| `geometry` | bounding rect, plus rect-relative-to-anchor for floating parts |
| `focus` | node path of `activeElement` |
| `console` | errors and warnings |
| `timeline` | attribute/transition/animation events (animation steps only) |
| `screenshot` | captured by the runner: fixture root plus each portal container |

### Style allowlist

Approximately 60 properties, not all ~340: box model, flex/grid, typography, colour/background/
opacity/shadow, transform, transition/animation, overflow, visibility, pointer-events, cursor,
outline, z-index. Diffing everything drowns real findings in vendor-prefixed noise.

## Normalization

Applied to both sides:

- Comment nodes dropped, including Blazor's `<!--Blazor:…-->` markers.
- **Generated ids are symbolized, not stripped.** React `useId` values and Blazor GUIDs differ for the
  same concept, so each id becomes `#id1`, `#id2`… in document order, and every reference
  (`aria-labelledby`, `aria-controls`, `aria-describedby`, `aria-activedescendant`, `for`) is rewritten
  through the same table. The relationship is preserved and diffed; the arbitrary string is not. A
  popup whose `aria-controls` points at the wrong node still fails.
- `class` is **excluded from the attribute diff and reported as `Info` only**. Tailwind admits
  multiple spellings of the same result — `size-3.5` and `h-3.5 w-3.5` are computed-style identical
  but class-set different — so class-set equality produces false positives. `ComputedStyle` is the
  real assertion; the `Info` entry exists so an author can see class drift when triaging a style
  finding.
- Text nodes whitespace-normalized.
- The `style` attribute is excluded from the attribute diff; inline positioning styles are covered by
  the computed-style and geometry comparators, which apply numeric tolerance rather than string
  equality.

### Node matching

Normalize, then match greedily on `(tag, role, accessible-name, ordinal)`. Leftovers on either side
become `NodeAdded` / `NodeRemoved` findings — which is how an extra Blazor wrapper element surfaces.
Paths render as `div[role=presentation] > button[role=combobox]`.

### `data-blazix-*` handling

1. **Rename, don't exclude.** `data-blazix-base-ui-X` → `data-base-ui-X` during normalization, then
   diff normally. This resolves 8 of the 20 prefixed markers against their upstream counterparts for
   free (`click-trigger`, `focus-guard`, `focusable`, `inert`, `navigation-menu-trigger`, `portal`,
   `scroll-locked`, `swipe-ignore`), and makes a *missing* Blazor marker a genuine finding rather
   than a silent pass.
2. **Exception list** (`manifest/markers.json`) for markers with no upstream counterpart —
   `positioner`, `composite-item`, `list-item`, `toast-root`, `accordion-root`, `active`,
   `disable-scrollbar`, `focus-guard-type`, `label`, `navigation-menu-viewport-target`,
   `popover-arrow`, `scroll-area-disable-scrollbar`, and the `data-blazix-otp-*` set. Each declares
   `blazorOnly: true` plus a reason, and is dropped from the diff.
3. **Ratchet:** any `data-blazix-*` in a capture that neither matches after renaming nor appears in
   `markers.json` fails the run. New markers must be classified when they are added.

The remaining three upstream markers — `data-base-ui-slider-control`, `-slider-indicator`,
`-tooltip-trigger` — are already emitted by Blazix in **unprefixed** form (`SliderControl.razor:193`,
`SliderIndicator.razor:112`, `TooltipTypedTrigger.razor:22`), so they match upstream directly and need
no rename. This is the same inconsistency noted in the context table: Blazix uses both the prefixed
and unprefixed conventions across different components. The rename rule is deliberately one-way and
idempotent — an already-unprefixed marker passes through untouched — so it handles both conventions
without the suite having to care which a given component chose.

## Comparators

Each emits typed findings: `{ kind, severity, fixture, leg, step, nodePath, property, reactValue, blazorValue }`.
Severity is `Error` (unwaived → fail), `Info`, or `Flaky`.

| Comparator | Checks | Tolerance |
| --- | --- | --- |
| `Structure` | node presence, ordering, depth | exact |
| `Attribute` | all attributes except `style` and `class`, post-normalization | exact |
| `AriaSnapshot` | role + accessible name + state tree | exact |
| `ComputedStyle` | allowlisted properties per matched node | ±0.5px numeric |
| `CustomProperty` | `--*` values | ±0.5px numeric |
| `Geometry` | bounding rect, rect-relative-to-anchor | ±1px |
| `Focus` | `activeElement` path per step and per key | exact |
| `Console` | errors/warnings present on one side only | exact |
| `Marker` | `data-blazix-*` classification ratchet | exact |
| `Timeline` | animation sequence and phase ordering | see below |
| `Pixel` | screenshot mismatch ratio | `pixelThreshold`, default 0.1% |
| `ApiSurface` | `types.md` props vs `[Parameter]` properties | exact |

## Animation comparison

`startTimeline()` installs a `MutationObserver` (`attributes`, `attributeOldValue`, `subtree`,
`childList`) plus capture-phase listeners for `transitionstart/end/cancel` and
`animationstart/end/cancel`, timestamped from `performance.now()` relative to the trigger dispatch,
running until quiescent.

Raw millisecond comparison across React, Blazor Server (round-trip latency) and WASM (3× timeouts)
would be pure flake. The timeline is therefore compared in three layers:

**L1 — Sequence (strict, primary assertion).** The ordered event list with timestamps erased:
`data-starting-style` added → removed → `transitionstart` → `transitionend`; on close
`data-ending-style` added → `transitionend` → node removed. An element that vanishes instantly
instead of animating out has a structurally different sequence.

**L2 — Phase ordering (strict).** Invariants derived from the timeline: did the node mount before the
transition started; was it still in the DOM at `transitionend`; did `data-open` flip before or after
`data-starting-style` cleared.

**L3 — Duration (advisory).** Each side's observed duration is checked against *its own* declared CSS
duration, not against the other side. Server latency delays the animation's start without failing
anything, while a transition that ran 0ms or 3× its declared length still fails. Cross-side duration
deltas are reported as `Info`, never `Error`.

### Deterministic mid-animation frames

Rather than racing screenshots against a running transition, the capture pauses and seeks:
`getAnimations()` on the animating subtree, then `pause()` and set `currentTime` to 0/25/50/75/100%
of the declared duration, screenshotting each. Fully deterministic, and it catches wrong easing,
wrong direction, and wrong transform origin — which a start/end comparison misses entirely.

## Visual comparison

Same browser build, viewport, DPR, stylesheet, and pinned font on both sides. Per step: screenshot
the fixture root **plus each portal container separately** — popups render to `document.body`, outside
the root, so a root-only screenshot would silently miss every floating component.

Compared per-pixel with a colour tolerance and a total-mismatch threshold (`pixelThreshold`, default
0.1%, per-fixture). A red-overlay diff PNG goes into the report.

A literal 0.0% diff is unlikely on text-heavy fixtures even within one browser. The threshold is
calibrated per fixture, not waived wholesale.

**LLM comparison is deliberately out of the pipeline.** The report is structured and LLM-readable, so
triage-by-paste remains available. An automated `--explain` pass classifying unwaived findings as
defect-vs-limitation is a possible follow-up, but the suite must not depend on a non-deterministic
step.

## Waivers

```jsonc
{
  "fixture": "drawer/hero",   "leg": "*",   "step": "*",
  "nodePath": "div[role=dialog]",
  "kind": "ComputedStyle",    "property": "transition-duration",
  "reason": "Blazor render batching defers the starting-style removal by one frame.",
  "docLink": "docs/audits/drawer-parity-matrix.md#animation",
  "expires": "2026-12-31"
}
```

Keyed on `(fixture, leg, step, nodePath, kind, property)` with `*` wildcards. Three rules keep the
file honest:

- **`reason` is required and non-empty**, enforced by a schema test. This is what turns "accepted
  Blazor limitation" into documentation.
- **Unused waivers fail the run.** A waiver that no longer matches means the discrepancy was fixed and
  the waiver is stale. base-ui applies the same rule to its regression blacklist
  (`unusedBlacklistPatterns`).
- **`expires`** warns, then fails, so temporary waivers get revisited.

A fixture-level waiver with `kind: "*"` **is** the quarantine mechanism; no second mechanism exists.

`docs/audits/parity-limitations.md` is **generated** from the waiver file, so the documented
limitation list cannot drift from what the suite tolerates.

## Report

Output folder `parity-report/` (`index.html` + `assets/`) — not one self-contained file, since 114
fixtures × 3 legs × N steps of base64 PNGs would be unusable. Tailwind-styled, containing:

- summary counts by component, kind, and leg
- filter chips (kind, severity, component, leg)
- per fixture: three-up screenshots (React | Server | WASM) with diff overlay, tabbed by step
- DOM/attribute diff as a two-column tree
- computed-style table showing only differing properties
- animation timeline as a three-track gantt
- waived findings collapsed, reason visible
- Blazor-only markers listed as `Info`

`parity-result.json` carries the same data machine-readably.

## API surface diff

`types.md` is autogenerated markdown with a `| Prop | Type | Default | Description |` table per part
under `### Root` / `### Thumb` headings — parseable in C# with no TypeScript tooling.

Parse into `{ part → props }`, reflect the Blazix assembly for `[Parameter]` properties, and diff.
`manifest/naming.json` carries the conventions (`onCheckedChange` → `OnCheckedChange`, `className` →
`ClassValue`, `render` → `Render`, `style` → `StyleValue`) plus per-prop overrides.

Findings: `PropMissing` (upstream prop with no Blazor equivalent — the direct answer to "is a function
lacking?"), `PropExtra`, and `TypeMismatch` as advisory.

Because `.base-ui` is gitignored, the parsed API surface is snapshotted into
`baselines/api/<component>.json` and committed — so this check runs with no base-ui checkout and no
browser.

## Reliability

**Settle protocol** (same shape both sides): wait for the fixture root's `data-interactive` marker
(reflecting `RendererInfo.IsInteractive`, as the existing Playwright test pages already do), then
`document.fonts.ready`, then two consecutive animation frames with no mutations. React skips only the
interactivity gate.

**Parallelism is split.** `[Collection("ParityStatic")]` runs parallel under the existing
`maxParallelThreads: 4`; `[Collection("ParityTiming")]` runs serial. CPU contention would otherwise
skew exactly the durations the L3 check measures.

**Flake handling stays minimal.** A leg that throws is retried once; findings that differ between
attempts are marked `Flaky`, reported, and never failed.

**Error handling.** A missing base-ui checkout still runs the API and baseline checks, and fails
`PARITY_LIVE=1` with instructions. A stale React bundle fails with the refresh command. A 404 or unhandled
Blazor error becomes a `FixtureError` finding rather than aborting the run — though an unhandled
Blazor exception is always `Error`.

## Testing the harness

The diff engine is code and can be wrong. In-project unit tests cover:

- **Normalizer** — id symbolization preserves `aria-controls` relationships; class-set comparison;
  marker rename.
- **Comparators** — synthetic bundle pairs with known diffs produce the expected findings.
- **Waiver matcher** — wildcard resolution, unused-waiver detection, missing-reason rejection.

Plus a **canary fixture**: a deliberately broken Blazor fixture with a known missing attribute, a
wrong colour, and a suppressed animation, which the suite **must** flag. If the canary passes, the
harness is broken. This guards the otherwise-invisible failure mode where capture silently returns
empty and the run reports a serene zero findings.

## Implementation sequencing

The first milestone is 29 fixtures; this ordering exists so pipeline defects are not baked into them.

1. **Skeleton** — projects, shared `parity.css`, `capture.js`, and **one** pair (Switch/Hero)
   end-to-end through every comparator and the report.
2. **Canary fixture + harness unit tests.**
3. **Four hard-class pairs** — Collapsible/Hero (animation), Popover/Hero (portal, floating, custom
   properties), Select/Grouped (composite, keyboard), Field/Hero (validation). Tolerances are
   calibrated here.
4. **API-surface diff across all 38 components** — no fixtures, no browser, fast, and the step most
   likely to surface missing functionality immediately.
5. **The remaining 24 fixtures** of the first-milestone corpus, in component batches, triaging
   fix-or-waive as they land. (Steps 1 and 3 already cover 5 of the 29.)
6. **Generated `parity-limitations.md` + README.**

The remaining 85 fixtures are a follow-up milestone, scoped once the first quarter's findings show
whether the signal-to-noise ratio justifies the authoring cost.

## Out of scope

- The remaining 85 fixtures beyond the first-milestone 29. The pipeline is built to carry them; only
  the authoring is deferred.
- A CI job. CI is lint-only today, and the React bundle depends on a gitignored checkout; wiring this
  into CI is a separate decision once baselines have proven stable.
- LLM-based classification of findings (`--explain`).
- Dark-mode capture for all fixtures (available per fixture via `themes`, off by default).
- Cross-browser capture (Firefox/WebKit). The pixel-diff guarantee depends on a single browser build.
