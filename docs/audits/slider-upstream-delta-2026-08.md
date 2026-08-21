# Slider Upstream Delta & Impact Report

Date: August 21, 2026
Repository: Blazix.BaseUI
Component: Slider
Source of truth: `.base-ui/packages/react/src/slider` @ `1a2ca3c9f8a39bd8c0dda939a7a23b72da226124` (origin/master, 2026-08-03)
Prior audit baseline: `bdcb685fadcca9d18b18f013c052795a53b6aa33` (2026-07-18, the cycle-1 delta-inventory pin)
Verified against local HEAD: `4b2a7923`
Ticket: [#174](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/174) (tranche-2 staleness refresh, upstream sync cycle 1)

## Delta Window

Four upstream commits touch `packages/react/src/slider/` in the window. Per
`docs/audits/METHODOLOGY.md` (Q3 corollary) the sweep also walks Slider's transitive import
graph — 161 files reached by BFS from `packages/react/src/slider` through `internals/*`,
`utils/*`, `field/*`, and `packages/utils/*` — which surfaces 17 further commits, dispositioned
below.

`6f262b1c7` (#5391), the tranche-1 fix, landed in PR #205 before this refresh. This document is
the staleness refresh on top of it and re-verifies that fix as present.

**Outcome: no new ports.** Every remaining hunk is either already present, architecturally
moot, or a React-only refactor. Details and evidence below.

## Slider-owned commits

| Upstream | Verdict | User-observable symptom | Evidence |
|---|---|---|---|
| `6f262b1c7` #5391 | **(b) + (c) ported — landed PR #205** | Dragging one thumb of a range slider past its neighbour could produce a `NaN` minimum-distance comparison that silently rejected the drag; and moving focus from one thumb straight to another fired field-level blur validation as if the whole slider had been left. | `Slider/SliderUtilities.cs`, `Slider/SliderThumb.razor`, `blazix-baseui-slider.js` (`isBlurWithinThumbs`). Covered by `SliderThumbTests` (bUnit) and `ISliderThumbContract`. Re-verified present at `4b2a7923`. |
| `c5c771b59` #5277 | **split — see per-hunk table below** | — | — |
| `6e059c2b0` #5322 | **(a) skip — React-specific** | No runtime content — test setup pinned a timezone and locale. | Test-only upstream; no production code changed. |
| `6feeb1f54` #5357 | **(d:moot)** | No user-observable symptom: upstream replaced ad-hoc change-reason string literals with shared constants of identical value. | The port models change reasons as a typed C# enum (`Slider/Enumerations.cs:48`), so literal drift is structurally impossible. Determination ratified on [#158](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/158). |

### `c5c771b59` (#5277) per-hunk dispositions

Nominally a test-coverage commit, it carries six source hunks (Q6.1 split disposition).

| Upstream hunk | Verdict | User-observable symptom | Evidence |
|---|---|---|---|
| `SliderRoot.tsx` — `disabled` reset moved from render phase into a layout effect, with `active` added to the deps | **(d:already-present)** | A slider disabled while a thumb was still marked active kept reporting that thumb as active/dragging, and Firefox and Safari kept keyboard focus on the disabled control. | `Slider/SliderRoot.razor:372-375` clears `activeThumbIndex` whenever `ResolvedDisabled` is set; `:377-380` arms `needsBlurOnDisable` on the disabled edge. Both already run in the Blazor parameter-set lifecycle, which is the port's equivalent of upstream's layout effect. |
| `SliderRoot.tsx` / `SliderRootContext.ts` — `setValue(details?)` becomes `setValue(details)`; the `REASONS.none` fallback is dropped | **(d:already-present)** | A programmatic value change could reach `onValueChange` carrying a placeholder `none` reason instead of a real one. | `Slider/SliderRootContext.cs:47` — `Func<double[], SliderChangeReason, int, Task<bool>>`; the reason parameter is non-optional, so no fallback path exists. `SliderChangeReason.None` remains declared (`Slider/Enumerations.cs:53`) to mirror upstream's reason union, and is never produced by the port. |
| `SliderThumb.tsx` — `getMidpoint(event.currentTarget, …)` replaces `getMidpoint(thumbRef.current, …)` and the `thumbRef.current != null` guard is dropped | **(d:moot)** | If the thumb's ref was null at pointer-down, the grab offset was never recorded, so the following drag snapped the thumb's centre to the pointer instead of preserving where the user grabbed it. | Mechanism inspected: `blazix-baseui-slider.js:242-249`. The port has no React ref in this path — it resolves the pressed element from a live DOM query (`thumbArray[closestIndex]`) at press time and computes the offset from that element. A stale-or-null ref cannot arise. |
| `SliderThumb.tsx` — `if (!thumbRef.current) return;` removed from the blur handler | **(d:moot)** | Blurring a thumb whose ref had already detached skipped the active-index reset, leaving the slider stuck reporting an active thumb. | Mechanism inspected: `Slider/SliderThumb.razor:555` `HandleBlur` — the port's blur path has no ref guard to short-circuit on, and delegates to `HandleFieldBlurAsync` unconditionally. |
| `utils/getPushedThumbValues.ts` — `values.length === 0` early return and the `?? nextValues[i]` fallbacks removed | **(a) skip — React-specific** | No user-observable symptom: upstream deleted defensive branches its new tests proved unreachable; deleting them changes no output. | The port's equivalents (`blazix-baseui-slider.js:740` index-range guard, `:756`/`:768` `!== undefined` fallbacks) are inert for the same reason — the only call sites (`:702`, `:722`) pass an in-range `pressedIndex` already validated at `:640`, and either a full `initialValues` array or none at all (in which case `baseInitialValues` falls back to the complete `values` array at `:747`). Retained rather than deleted, per `CLAUDE.md` §3. |
| `utils/resolveThumbCollision.ts` — `neighborIndex` bounds check removed | **(a) skip — React-specific** | No user-observable symptom: same dead-branch removal. | The port keeps the equivalent guard at `blazix-baseui-slider.js:705`. `neighborIndex` is `targetIndex ± 1` chosen by swap direction, so it is always in range; the guard never fires. |
| `SliderValue.tsx` — the `for` attribute is built with `Array.from(...).join(' ')` instead of an accumulating loop that skipped falsy ids | **(a) skip — React-specific** | No user-observable symptom: with every thumb carrying a generated input id, both forms emit the identical space-separated `for` list. | `Slider/SliderValue.razor:122-139` `GetHtmlFor` orders by thumb index and filters empty ids before joining. Upstream's new form drops the filter; the port's is equal on the guaranteed-id path and strictly safer if an id were ever absent, so the filter is retained (methodology tier 3: this is a no-op-in-practice style difference, not a parity dispute). |

## Adjacent-family commits reaching Slider's import graph

These land in Field/Form or the shared popup layer. Their verdicts belong to the owning
component tickets; recorded here is the **(commit, Slider)** pair only, so the Slider record is
not silently blank on them.

| Upstream | (commit, Slider) verdict | Reasoning |
|---|---|---|
| `db86f44d8` #5390, `d060189d4` #5290, `293a0f1ed` #5287 | **(d:already-present)** via Field/Form | Landed in PR #206. A Slider inside a Field inherits the fixed required-validity, `data-dirty` and first-invalid-focus behavior with no Slider-side code. |
| `8846f9937` #5383 | **(a) skip** | Form serialization of portalled `CheckboxGroup`/`RadioGroup` values. Slider renders no group and contributes a single named input per thumb, so no Slider symptom exists. |
| `32cabb778` #5281 | **(a) skip — React-specific** | No runtime content beyond `FieldError`/`FieldItem`/`FieldsetLegend` cleanup; Field-owned. |
| `595c0fa08` #5340 | **owned by [#158](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/158) batch 2** | `Slider/SliderLabel.razor:119` still clears the registered id unconditionally on dispose. Slider-side symptom: swapping the slider's label lets the outgoing instance clear the incoming one's id, so the slider loses its accessible name. Recorded on #158; not re-dispositioned here. |
| `1a2ca3c9f` #5401, `166e8ac01` #5400, `071e89201` #5394, `3b5715cc7` #5387, `8b2282a5e` #5388, `7397c99ba` #5339, `b089a7ccc` #5309, `9a5c3850f` #5265, `dc9a4577f` #5384 | **(d:moot)** | Popup lifecycle, popup handles, focus-manager modality, list navigation and submenu activation. Slider renders no popup, opens no floating element and runs no list navigation; these reach its import graph only through shared barrel re-exports, not through any executed Slider path. |
| `54cfcc188` #5386, `ee38be3e2` #5250, `006a72a99` #5341 | **(a) skip — React-specific** | TypeScript declaration emission, store-selector code size, and React effect-ownership relocation respectively. None changes DOM output, ARIA, focus order, keyboard/pointer behavior, timing constants or the public API surface. `006a72a99` remains **revisit-on-symptom** per #158; no Slider divergence observed. |

## Test coverage

No ports landed in this refresh, so no new tests were added. The re-verified `#5391` behavior is
covered by `SliderThumbTests` (bUnit) and `ISliderThumbContract`, added in PR #205. The existing
`SliderTestsServer` / `SliderTestsWasm` Playwright classes were run green against `4b2a7923`
with this document's changes applied.
