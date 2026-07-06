# NumberField Functional Audit

Date: 2026-07-05

## Scope

Audited the Blazor NumberField port against React Base UI source at `.base-ui` HEAD `95cf9e0339567518ccdf82628c8ef4f3d67cad07` and fetched upstream `origin/master` at `ca246a6068d98f8fa27fa1c382743184550a0360`.

React source files reviewed:

- `root/NumberFieldRoot.tsx`
- `root/useNumberFieldButton.ts`
- `root/useNumberFieldStepperButton.ts`
- `input/NumberFieldInput.tsx`
- `increment/NumberFieldIncrement.tsx`
- `decrement/NumberFieldDecrement.tsx`
- `group/NumberFieldGroup.tsx`
- `scrub-area/NumberFieldScrubArea.tsx`
- `scrub-area-cursor/NumberFieldScrubAreaCursor.tsx`
- `utils/parse.ts`
- `utils/validate.ts`
- React NumberField tests under `.base-ui/packages/react/src/number-field`

## Upstream Delta And Impact Report

No NumberField source files changed between local `.base-ui` HEAD and fetched `origin/master`. The audit therefore focused on already-present upstream fixes that were not fully reflected in the Blazor port.

| Upstream fix | Impact | Blazor repair |
| --- | --- | --- |
| Safe integer defaults and empty-step seeding | Empty steppers seed `0`; fully negative ranges clamp to the in-range value nearest zero. | `DefaultMin`/`DefaultMax`; empty `IncrementValueInternalAsync` calls validation with `0` and no direction. |
| Precision preservation | Direct input and no-edit blur must not collapse values through displayed rounded text. | Blur path preserves authoritative numeric value unless manual input or explicit rounding options exist; hidden value uses shortest invariant numeric text. |
| Canceled change commit gating | Vetoed changes must not fire `OnValueCommitted` or leak stale `lastChangedValue`. | `SetValueInternalAsync` returns `false` on cancellation before updating `lastChangedValue`; keyboard/button/wheel/scrub commits are gated on accepted changes. |
| Focusable disabled steppers | Buttons use unavailable semantics without invalid `aria-readonly`. | Increment/decrement render `aria-disabled="true"` and `data-disabled`; no native `disabled` or `aria-readonly` for the focusable-disabled path. |
| Read-only focus state | Read-only input remains focusable and updates focused/touched state. | Focus/blur handlers now block only disabled state before field focus bookkeeping. |
| Hidden input parity | Hidden input must be read-only when root is read-only and handle focus/autofill. | Hidden input renders `readonly`, redirects focus, and processes change/autofill unless disabled/read-only. |
| Paste semantics | Paste inserts into current selection and restores caret. | JS paste handler splices clipboard text into the current value and restores the selection after render. |
| Home/End key handling | Home/End prevent defaults only when min/max exists. | JS keydown filter allows native caret movement when the relevant bound is absent. |
| Allowed input symbols | Allowed symbols come from `Intl.NumberFormat.formatToParts`; format controls are valid. | JS key filter derives non-digit formatter parts; C# validation allows Unicode format controls and out-of-range minus entry. |
| Scrub cleanup | Scrub movement is async, pointer-lock aware, and commit-safe. | Scrub move awaits accepted increments; scrub end commits only after a changed value. |

## Repairs Applied

- Reworked NumberField root context step APIs to return accepted-change status and carry commit values.
- Added JavaScript-safe default bounds, shortest hidden-input serialization, read-only hidden input, hidden focus redirect, and hidden autofill/change handling.
- Preserved direct-input precision while keeping formatter-backed explicit rounding.
- Synchronized dirty visible input before button stepping and stepped keyboard interactions from dirty text when needed.
- Updated increment/decrement state attributes and ARIA semantics to match React focusable-disabled behavior.
- Updated JS key filtering for `formatToParts`, `allowOutOfRange`, Home/End, Unicode format controls, and paste splicing.
- Added unit and Playwright coverage for canceled commits, empty blur, read-only focus state, hidden `readonly`, allow-out-of-range underflow, paste caret behavior, Home/End caret behavior, and focusable disabled buttons.
- Updated `../base-ui-specs/number-field/SPEC.md` and `pitfalls.md` with the repaired behavior.

## Source Testing

- React jsdom source tests: `docs/audits/logs/number-field-source-react-jsdom.txt`
- React Chromium source tests: `docs/audits/logs/number-field-source-react-chromium.txt`
- React docs validation: `docs/audits/logs/number-field-source-docs-validate.txt`
- In-app browser docs comparison: `docs/audits/logs/number-field-in-app-browser-docs-comparison.json`

## Result

All audited React hooks, utilities, attributes, state branches, and upstream fixes have a Blazor implementation or verified equivalent in `docs/audits/number-field-parity-matrix.md`.
