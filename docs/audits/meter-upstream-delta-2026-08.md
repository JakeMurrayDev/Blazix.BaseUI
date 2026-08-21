# Meter Upstream Delta & Impact Report

Date: August 21, 2026
Repository: Blazix.BaseUI
Component: Meter
Source of truth: `.base-ui/packages/react/src/meter` @ `1a2ca3c9f8a39bd8c0dda939a7a23b72da226124` (origin/master, 2026-08-03)
Prior audit baseline: `bdcb685fadcca9d18b18f013c052795a53b6aa33` (2026-07-18, the cycle-1 delta-inventory pin)
Verified against local HEAD: `4b2a7923`
Ticket: [#169](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/169) (tranche-2 staleness refresh, upstream sync cycle 1)

> The previous Meter audit doc was deleted in `7a8f9560`, so this record is rebuilt from scratch
> and is pin-dated by construction. Because there is no prior record to diff against, it covers
> both the delta window **and** a current-state comparison of the five Meter parts against
> upstream at the pin. The current-state pass surfaced one pre-window gap, recorded under
> "Deferred with spec" below.

## Delta Window

Meter's transitive import graph is 36 files (BFS from `packages/react/src/meter` through
`internals/*`, `utils/*` and `packages/utils/*`). Exactly **one** commit in the window touches
it.

| Upstream | Verdict | User-observable symptom | Evidence |
|---|---|---|---|
| `595c0fa08` #5340 | **(b) port — C#/Razor** | Swapping the meter's label component let the outgoing instance clear the incoming one's registered id, so the meter lost its accessible name: `aria-labelledby` pointed at nothing. Blazor makes this the common case rather than a race, because it disposes a removed component **after** its replacement has initialized — the reverse of React's cleanup ordering. | Ported in this change set. `Meter/MeterRootContext.cs` — `SetLabelIdAction` becomes `Action<object, string?>`; `Meter/MeterRoot.razor` tracks `labelOwner` and ignores a clear from a stale instance; `Meter/MeterLabel.razor` passes `this`. Ownership is tracked by **instance**, not by id value, so a replacement reusing the same explicit id also survives — the pattern PR #206 established for `FieldsetLegend`. Covered by `MeterLabelTests.KeepsAriaLabelledByWhenLabelIsReplaced` and `KeepsAriaLabelledByWhenReplacementLabelReusesTheSameId`. |
| `086eeaded` #5312 | **(a) skip — React-specific** | No runtime content on the Meter side — the commit adds `MeterLabel.test.tsx` and nothing else under `meter/`. | Its source hunks are all Progress (`ProgressRoot.tsx`, `ProgressValue.tsx`) and belong to the Progress record; the Progress clamped-value fix `c02319dfc` (#5389) landed in PR #201. |

`595c0fa08` is nominally owned by the shared-layer sweep
[#158](https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/158), whose 2026-08-20 correction
enumerated the unguarded registration sites. **`MeterLabel` was not on that list** — this audit
found it as a ninth site. It is ported here rather than deferred to #158 batch 2, because the
fix is entirely in Meter component code and the batch-2 list would otherwise miss it. Batch 2
should treat `MeterLabel` as done.

## Deferred with spec — pre-window parity gap

Upstream `a7521e9a7` (#4904, "[meter] Sync value text with indicator", 2026-05-26) **predates the
window base** and is therefore not a delta-window row. The from-scratch current-state pass found
the port does not carry it, in three linked places. It is recorded here as debt rather than
ported, because it collides with behavior three existing contract tests deliberately encode —
a parity-vs-local-divergence question that per `docs/audits/METHODOLOGY.md` (Resolving
uncertainty, tier 3) is the maintainer's to settle.

**Upstream mechanism, exactly:**

```ts
const rawPercentage = valueToPercent(valueProp, min, max);
const percentageValue = clamp(Number.isNaN(rawPercentage) ? 0 : rawPercentage, 0, 100);
const clampedValue   = clamp(Number.isNaN(valueProp) ? min : valueProp, min, max);

const formattedValue = format
  ? formatNumber(valueProp, locale, format)
  : formatNumber(percentageValue / 100, locale, { style: 'percent' });

// 'aria-valuenow': clampedValue
// MeterIndicator width: `${percentageValue}%`   (the same percentageValue)
```

**The three local divergences:**

| Site | Local | Upstream | Symptom |
|---|---|---|---|
| `Meter/MeterRoot.razor:159` | `aria-valuenow` = raw `Value` | `clampedValue` | With a value outside `[Min, Max]`, `aria-valuenow` falls outside `aria-valuemin`/`aria-valuemax` — an ARIA validity violation. `NaN` is emitted verbatim. |
| `Meter/MeterRoot.razor:193-194` (`FormatValue` default branch) | `value / 100.0` as `P0` | `percentageValue / 100` as a percent, where `percentageValue` is relative to `Min`/`Max` | With any range other than the default 0–100 the announced text disagrees with the rendered fill. `Min=0, Max=200, Value=100` announces "100%" for a bar filled to 50%. |
| `Meter/MeterIndicator.razor:94` | unclamped, NaN-unsafe `ValueToPercent` | `percentageValue` (clamped 0–100, NaN→0) | Out-of-range values emit `width:150%` or `width:-25%`; `Min == Max` emits `width:Infinity%`. |

**Why it is not ported here.** Three contract tests assert the current behavior on purpose:
`MeterIndicatorTests.UsesReactValueToPercentWhenRangeIsZero` asserts `width:Infinity%`;
`MeterRootTests.UsesJavaScriptNumberStringForDefaultAriaValueText` asserts
`aria-valuetext == "100000000000000000000%"` for a value of 1e20 against the default max of 100;
`UsesInvariantRawValueForDefaultAriaValueText` asserts the raw invariant value string. Porting
#4904 rewrites all three. Note also that the local default `aria-valuetext`
(`Meter/MeterRoot.razor:187`, `$"{FormatJavaScriptNumber(value)}%"`) matches neither current
upstream nor upstream's pre-#2267 form, which used `percentageValue`.

**Proposed fix if the maintainer accepts the parity change:** add `clampedValue`/`percentageValue`
to `MeterRoot.razor` and `PercentageValue` to `MeterRootContext`, exactly as
`Progress/ProgressRoot.razor:143-145` already does (`SliderUtilities.Clamp`, NaN fallbacks), have
`MeterIndicator` consume `Context.PercentageValue`, and update the three tests. That is roughly a
20-line change; it was prototyped during this audit and reverted.

## Current-state comparison — the other four parts

| Part | Upstream at pin | Local | Verdict |
|---|---|---|---|
| `MeterRoot` | `role="meter"`, `aria-valuemin`/`aria-valuemax`/`aria-valuenow`/`aria-valuetext`/`aria-labelledby`, plus a visually hidden `<span role="presentation">x</span>` that forces NVDA to read the label (upstream issue #4184) | Same attribute set and ordering; hidden span present | **match**, except `aria-valuenow` per the deferral above. Covered by `MeterRootTests.RendersHiddenPresentationSpanForScreenReaders` and the ARIA tests. |
| `MeterTrack` | plain element, no intrinsic attributes | same | **match** |
| `MeterIndicator` | `inset-inline-start:0; height:inherit; width:<percentage>%` | same shape | **match**, except the percentage clamp per the deferral above |
| `MeterValue` | renders `formattedValue`, or the child render prop with `(formattedValue, value)` | `MeterValue.razor:79-84` — same, `ChildContent(FormattedValue, Value)` | **match** |
| `MeterLabel` | `role="presentation"`, registers its id with the root | same | **match** after this change set's `#5340` port |

## Test coverage

| Behavior | Test |
|---|---|
| Swapping the label keeps the meter's accessible name | `MeterLabelTests.KeepsAriaLabelledByWhenLabelIsReplaced` |
| A replacement label reusing the same explicit id keeps the name | `MeterLabelTests.KeepsAriaLabelledByWhenReplacementLabelReusesTheSameId` |

Both were added to `IMeterLabelContract` first, per `.claude/rules/testing-instructions.md`.
