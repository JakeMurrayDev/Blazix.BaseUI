# Research: what does an unattended parity run require?

**Date:** 2026-08-03 · **Resolves:** [#135](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/135) · **Feeds:** #136 (trigger decision)

All branch paths below refer to `enhancement/feature-parity-check` (inspected via `git show`,
head `27101e3f`); master paths refer to `b713a6f9`. Harness design:
`docs/superpowers/specs/2026-07-27-component-parity-harness-design.md`; plan:
`docs/superpowers/plans/2026-07-27-parity-harness-pipeline.md`; session state:
`docs/superpowers/plans/2026-07-27-parity-harness-HANDOFF.md`.

## Summary — the decision-relevant facts

1. **There is nothing to run unattended yet.** The suite's entry point (`ParityRunner`,
   `ParityTests`, Task 14), waivers (Task 11), baselines (Task 12), and 28 of the 29
   first-milestone fixtures do not exist. Every comparator is unit-tested but none has run end to
   end (HANDOFF, "Task status"). The trigger decision is about the state *after* Tasks 11–14 land.
2. **Committed baselines do remove `.base-ui` from the run — by design.** A plain `dotnet test`
   "uses the committed baselines and the committed `parity.css`, and needs neither `.base-ui` nor
   Node" (branch `tests/Blazix.BaseUI.Parity.Tests/README.md`, final section). Only
   `pnpm parity:css`, `pnpm parity:build`, and `PARITY_LIVE=1`/`PARITY_WRITE_BASELINES=1` need the
   checkout. So a CI job has two very different shapes: **baseline mode** (cheap, no Node, no
   `.base-ui`) and **live mode** (needs a base-ui clone *with its own `pnpm install`*).
3. **Two concrete Linux blockers exist before any CI run:** the pixel comparator's csproj only
   ships macOS Skia natives ("a Linux or Windows run needs `SkiaSharp.NativeAssets.Linux`" —
   comment in `Blazix.BaseUI.Parity.Tests.csproj`), and pixel baselines captured on the dev Mac
   will not rasterize text identically on Linux Chromium (CoreText vs FreeType), despite the
   DejaVu Sans pin and `--font-render-hinting=none`.
4. **Money is not the constraint.** The repo is public; standard `ubuntu-latest` runners
   (4 vCPU / 16 GB for public repos) are free without minute limits. The costs are wall-clock
   latency (~8–15 min per baseline-mode run at 29 fixtures, ~25–45 min at 114) and maintenance.
5. **Baseline staleness is invisible in CI.** The staleness check compares baselines against a
   *locally built* bundle (`dist/source-hash.txt`); with no `.base-ui` and no bundle in CI, an
   unattended run trusts whatever baselines are committed. A separate cheap canary (compare the
   baseline-recorded base-ui SHA against `git ls-remote` upstream) is the only unattended way to
   notice drift.

---

## 1. What a GitHub Actions run needs

### Current state (master)

- CI is lint-only: `.github/workflows/lint.yml` is the only workflow. It runs on
  `ubuntu-latest`, uses `actions/setup-dotnet@v4` with `dotnet-version: '10.0.x'`, builds
  `Blazix.BaseUI.slnx` with `/p:TreatWarningsAsErrors=true`, and runs `scripts/lint-rules.sh`.
  Its `paths` filter (`src/**`, `scripts/**`, `.editorconfig`, `Directory.Build.props`) means it
  would not even trigger on parity-test-only changes.
- The last 8 lint runs took **77–166 s wall clock** (`gh run list --workflow lint.yml`),
  *including* building two Blazor WebAssembly client projects (`Blazix.BaseUI.Demo.Client`,
  `Blazix.BaseUI.Docs.Client` are in the slnx) with **no workload install step**. This is direct
  evidence that building the parity WASM client (`Microsoft.NET.Sdk.BlazorWebAssembly`, no AOT)
  needs no `wasm-tools` workload — plain SDK restore suffices.
- There is no `global.json`; the SDK pin lives only in the workflow (`10.0.x`).

### Baseline-mode run (the intended CI shape)

Per the design spec ("Run flow", lines 85–94) and branch README, `dotnet test` with committed
baselines needs:

| Requirement | Cost on `ubuntu-latest` | Evidence |
| --- | --- | --- |
| .NET 10 SDK | ~0 (10.0.3xx preinstalled on ubuntu-24.04; `setup-dotnet` is near-instant) | actions/runner-images Ubuntu2404 README; lint.yml already does this |
| Solution + parity project build | ~2–4 min (lint's full-solution build is 77–166 s; parity adds 2 projects + Playwright/Skia restore) | lint run history |
| Playwright Chromium (pinned by Microsoft.Playwright 1.57.0) | ~1–3 min uncached; ~10 s restore with `actions/cache` on `~/.cache/ms-playwright` | playwright.dev/dotnet/docs/ci; cache limit 10 GB/repo, 7-day LRU eviction (docs.github.com caching guide) |
| `SkiaSharp.NativeAssets.Linux` package reference | one csproj line, must be added | csproj comment says so explicitly |
| Node / pnpm / `.base-ui` | **not needed** | branch README: "A plain `dotnet test` uses the committed baselines and the committed `parity.css`, and needs neither `.base-ui` nor Node." |
| `ASPNETCORE_ENVIRONMENT=Development` | **not needed in the workflow** — `ParityServerAssemblyFixture` sets it on the spawned `dotnet run --no-build` process itself | `Fixtures/ParityServerAssemblyFixture.cs` (`Environment = { ["ASPNETCORE_ENVIRONMENT"] = "Development" }`); the empty-200 `blazor.web.js` trap only bites manual hosting |

Also required but free: the assembly fixture launches the host via `dotnet run --no-build`, so
the test build must precede it (it does — ProjectReference), and it polls up to 60 s for
readiness.

**What baseline mode does NOT verify:** that the committed baselines are fresh. The staleness
check (spec "Baseline staleness", Task 12 `BaselineStore.AssertFresh`) compares the baseline's
recorded content hash against `react-fixtures/dist/source-hash.txt`, which only exists after a
local `pnpm parity:build`. `dist/` is gitignored (`.gitignore:386` on the branch) and nothing
builds it automatically (HANDOFF open item 9). In CI, with no dist, the check has nothing to
compare against — the run silently trusts the committed captures. Note the design already
half-anticipates this: API-surface snapshots are committed precisely "so this check runs with no
base-ui checkout and no browser" (spec, line ~394).

### Live-mode run (`PARITY_LIVE=1` — recapture React in CI)

Everything above, plus:

| Requirement | Cost | Evidence |
| --- | --- | --- |
| A base-ui checkout at the pin | clone of `mui/base-ui` @ `bdcb685fa` (~1 min). The pin is recorded only in prose in `docs/audits/*.md` — there is no machine-readable pin file; a workflow would hardcode it | `git grep bdcb685` hits only audit docs |
| `pnpm install` **inside the base-ui checkout** | several minutes (upstream monorepo) — `vite.config.mts` refuses to build unless `<base-ui>/node_modules/react` exists, because the React leg must run the React version upstream pins | `react-fixtures/vite.config.mts` (throws "Run `pnpm install` in the checkout") |
| pnpm itself | not preinstalled on ubuntu-24.04 (Node 22 is); `corepack enable` or `pnpm/action-setup`, ~10 s | actions/runner-images Ubuntu2404 README |
| `pnpm install --dir react-fixtures` + `pnpm parity:build` | ~1–2 min (five devDependencies + react/react-dom; Vite build of 114 demos ~30–90 s) | `react-fixtures/package.json` |
| `PARITY_BASE_UI_PATH` env var | free — CI clone won't be at the repo root that `BaseUiLocator`'s 12-level walk-up expects | `Infrastructure/BaseUiLocator.cs`, `scripts/resolve-base-ui-source.mjs` |

### The cross-OS pixel problem (applies to both modes)

Baselines include PNG screenshots (`baselines/{fixture}/{step}.png`, Task 12) that will be
captured on the developer's Mac. A Linux CI run compares Linux-Chromium-rendered Blazor
screenshots against those Mac-rendered React PNGs. The harness pins DejaVu Sans
(`react-fixtures/src/parity.css`) and launches with `--font-render-hinting=none
--disable-lcd-text` (`Fixtures/PlaywrightFixture.cs`), which controls *within-OS* variance, but
macOS (CoreText) and Linux (FreeType) rasterize glyphs differently, and the spec itself scopes
the guarantee to "a single browser build" (spec, out-of-scope: cross-browser). Options, in
increasing effort: waive/ignore pixel findings in CI; capture baselines on Linux (via a
live-mode CI job or a container matching CI); or accept a per-OS baseline set. **Any per-PR or
cron decision in #136 that includes the pixel comparator must pick one.**

---

## 2. Runtime and cost

### What one captured fixture-leg actually does

From `Capture/ParityCapturer.cs`, `Infrastructure/SettleProtocol.cs`, `Capture/ScreenshotSet.cs`,
`shared/capture.js`:

- **Navigate + settle:** wait for the capture API to report interactive, `document.fonts.ready`,
  then two consecutive mutation-free animation frames with a portal-mid-mount gate (30 s
  deadline). On a quiet page this is tens of milliseconds; the WASM leg adds runtime
  download/boot from localhost (~1–3 s per fresh context).
- **Per step:** replay actions (Playwright actionability waits), settle again, run the
  `capture.js` DOM walk (computed-style allowlist per node, custom properties, geometry,
  timeline), take one `AriaSnapshotAsync`, and screenshot **each capture root** (fixture root +
  every portal), 5 s timeout per shot.
- **Animation steps only:** wait for the animation to actually run (`settle: animation`), then
  pause-and-seek to 5 fractions (0/25/50/75/100 %) and screenshot every root at each — i.e. an
  animation step takes 6× the screenshots plus seek round-trips, and these run in the
  **serialized** `ParityTiming` collection because "CPU contention skews the durations the L3
  timeline check measures" (spec, Reliability; Task 14).
- Comparators are in-process CPU work over the captured bundles — milliseconds. **Capture
  dominates; the "×11 comparators" multiplies findings, not browser time.**

Reasoned per-fixture-leg estimates (localhost server, headless Chromium): **~3–6 s** for a
2-step non-animation fixture; **~10–20 s** for an animation fixture (settle-for-real-duration +
30 screenshots-ish + serial execution).

### Per-run totals

A CI baseline-mode run captures only the two Blazor legs (React comes from baselines — spec Run
flow step 3). `maxParallelThreads: 4` applies to the static collection
(`xunit.runner.json`); the timing collection is serial.

| Scale | Live captures (baseline mode) | Capture wall-clock (est.) | Whole job (build + browsers + capture) |
| --- | --- | --- | --- |
| 29 fixtures × 2 Blazor legs | 58 | ~4–8 min (static ÷4 parallel; animation serial dominates) | **~8–15 min** warm cache |
| 114 fixtures × 2 Blazor legs | 228 | ~15–35 min | **~25–45 min** |
| +`PARITY_LIVE` (3 legs) | +29 / +114 React captures | +~2–10 min | + base-ui clone & monorepo `pnpm install` (~5–10 min more) |

These are estimates grounded in the per-step mechanics above, not measurements — **no end-to-end
run has ever happened** (HANDOFF: "None has ever run end to end"). The first real
`PARITY_FIXTURES=switch/* dotnet test` run after Task 14 should be timed and this table
corrected. A second-order cost: committed baselines grow the repo (~2.5 steps × ~1.3 roots ×
29 fixtures ≈ 100 PNGs, plus 5-frame sets per animation step; likely single-digit MB at 29
fixtures, tens of MB at 114).

### Dollar cost

$0. The repo is public, and GitHub-hosted standard runners are free for public repositories with
no minute cap; `ubuntu-latest` for public repos is 4 vCPU / 16 GB / 14 GB SSD, currently
ubuntu-24.04 (docs.github.com → Actions → runners reference; billing doc: private-repo Linux
would be $0.006/min after the plan allowance). The real budget is **PR feedback latency** and
**maintenance of the workflow + baselines**.

---

## 3. Alternatives to per-PR CI

| Option | What it catches | What it misses | Setup burden |
| --- | --- | --- | --- |
| **Per-PR CI** (baseline mode) | Any Blazor-leg regression vs committed baselines, before merge; the canary fixture guards capture-went-silent | Upstream drift (baselines only refresh manually); pixel findings unless the cross-OS problem is solved; adds ~10–15 min to every PR (29 fixtures) growing to ~30–45 (114) | Workflow + Playwright cache + SkiaSharp Linux natives + a pixel-comparator decision; also a `paths` filter so docs PRs skip it |
| **Scheduled (cron)** — e.g. nightly/weekly on master | Same regressions, at cadence instead of merge time; can afford live mode (clone base-ui @ pin, recapture, even compare pin vs upstream HEAD) since latency doesn't block anyone | Regressions land first, get found later; needs failure *delivery* (auto-file an issue — a red run nobody sees is a manual cadence with extra steps) | Same as per-PR, plus notification wiring; caveat: scheduled workflows in public repos auto-disable after 60 days without repo activity (docs.github.com, schedule event) |
| **Local pre-merge hook** (git hook or documented gate) | Everything the suite can catch, on the machine where `.base-ui`, baselines, and the baseline OS already agree — no cross-OS pixel problem, live mode is one env var away | Unenforceable (skippable, and hooks don't sync via git); serial ~10–20 min on the dev machine per merge; single-developer bus factor | Lowest: a `parity:check` script + one line in the PR checklist; no workflow, no Linux Skia, no baseline-OS decision |
| **Manual cadence + staleness canary** | The canary — a tiny CI job (<1 min, no browser) that fails when the baselines' recorded base-ui SHA no longer matches `git ls-remote` upstream master, or when baselines exceed an age budget — catches "nobody has looked in ages" unattended | All actual component regressions between manual runs; the canary checks *freshness*, not *parity* | Smallest possible workflow (a script + cron); pairs naturally with the local-hook option |

Orthogonal fact for #136: the **API-surface check** (spec, "Error handling") runs from committed
`baselines/api/*.json` with **no browser and no base-ui** — it is per-PR-CI-shaped even if the
browser suite is not, and would be minutes-cheap at any fixture count.

## 4. Existing constraints (verified)

- **CI is lint-only** — `.github/workflows/lint.yml` is the sole workflow on master, and its
  `paths` filter excludes `tests/**`. Memory/HANDOFF concur: "repo CI is lint-only (no tests!)".
- **`TreatWarningsAsErrors=true`** repo-wide (`Directory.Build.props:3`) — the parity projects
  already build clean under it (they're built by the same solution rules), so CI adds no new
  warning risk, but any workflow-added code must hold the line.
- **`ASPNETCORE_ENVIRONMENT=Development`** — required by the host or `blazor.web.js` serves as an
  empty 200 (HANDOFF, Environment facts), but the assembly fixture already injects it into the
  server process it spawns; only manually-hosted runs (e.g. a debugging session on the runner)
  need to export it.
- **`.base-ui` is gitignored at the main repo root** (`.gitignore:373`), absent from worktrees
  and CI; pinned at `bdcb685fa` in audit-doc prose only. `react-fixtures/dist/` is gitignored
  (branch `.gitignore:386`); baselines and waivers ARE committed (comment, same block).
- **Round-trip settle gap** (HANDOFF open item 4) must be solved before Task 15, or interaction
  steps capture mid-render-batch — an unattended run inherits whatever flake that leaves.
