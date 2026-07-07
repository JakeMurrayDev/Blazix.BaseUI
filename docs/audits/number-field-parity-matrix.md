# NumberField React-to-Blazor Parity Matrix

Date: 2026-07-05

| React surface | Required behavior | Blazor equivalent | Verification |
| --- | --- | --- | --- |
| `NumberFieldRoot` controlled state | Explicit `value={null}` is controlled empty; omitted value uses `defaultValue`. | `SetParametersAsync` detects supplied `Value`; hosts omit `Value` for uncontrolled cases. | `Value_AcceptsNullValue`; Playwright dynamic update tests. |
| `minWithDefault` / `maxWithDefault` | Defaults are JS safe integer bounds. | `NumberFieldUtilities.DefaultMin/DefaultMax`. | Unit min/max and empty seed tests. |
| Empty `incrementValue` | Seed `0` with no direction and clamp nearest zero. | Empty `IncrementValueInternalAsync` calls validation with `0`, no direction. | Increment/decrement empty click tests. |
| `setValue` | Validate by reason, support cancellation, return accepted-change status. | `SetValueInternalAsync` returns `Task<bool>` and preserves stale commit guards. | Canceled keyboard and blur tests. |
| `lastChangedValueRef` | Commit only the latest accepted value. | `lastChangedValue` plus `GetCommitValue`. | Keyboard/button/wheel/scrub commit tests. |
| `allowInputSyncRef` | Preserve raw text while typing; sync on blur/programmatic changes. | `allowInputSync`, `SetInputValueDirect`, `HasPendingCommit`. | Raw input and precision tests. |
| Hidden native input | `type=number`, canonical value, name/form/min/max/step, disabled, readOnly, required, hidden from AT. | Root hidden input renders all attrs, `readonly`, focus redirect, change handler. | Hidden input unit tests; form Playwright tests. |
| `allowOutOfRange` | Only direct input bypasses clamping; step interactions clamp. | `ValidateNumber` gates clamp by reason. | Unit overflow/underflow/clamp tests; native validity Playwright. |
| Input focus/blur | Disabled blocks focus; read-only still tracks focus/touched. | `HandleFocus`/`HandleBlur` update field state before read-only value guard. | `ReadOnly_FocusStateTracksFocusAndBlur`. |
| Input blur precision | No-edit blur keeps authoritative value; explicit rounding options round. | Blur path distinguishes manual input and rounding options. | Precision preservation tests. |
| Input clear | Empty blur commits only when something changed or pending. | Empty blur checks accepted change and pending commit. | Empty unchanged blur test. |
| Keydown filter | Invalid text blocked synchronously in the DOM. | NumberField JS `keydown` listener. | Playwright percent and minus tests. |
| Home/End | Native caret movement unless min/max exists. | JS only prevents Home/End with relevant bound. | `HomeEndWithoutBoundsUseNativeCaretMovement`. |
| Allowed symbols | Derived from `Intl.NumberFormat.formatToParts`; compact excluded. | JS `getFormatParts`; C# allows supported digits and format controls. | Source tests; Playwright character tests. |
| `parseNumber` | Locale separators, signs, percent/permille, currency/unit, format controls, numeral systems, Infinity rejection. | `NumberFieldUtilities.ParseNumber`. | Exotic parse unit tests. |
| `toValidatedNumber` | Snap before clamp, clamp before/after rounding, preserve non-step values. | `NumberFieldUtilities.ToValidatedNumber`. | Step, snap, precision tests. |
| Floating point cleanup | Delta-bounded cleanup unless explicit rounding options exist. | `RemoveFloatingPointErrors`. | Fractional step tests. |
| Paste | Prevent default, splice into selection, parse full text, restore caret. | JS paste handler calls `OnPasteText(nextText)` and restores selection. | `Paste_InsertsAtCaretAndRestoresSelection`. |
| Increment/decrement buttons | Focusable-disabled semantics; no `aria-readonly` on buttons. | `aria-disabled`, `data-disabled`, no native disabled in focusable path. | Unit button attr tests; Playwright disabled button checks. |
| Dirty step from buttons | Sync dirty input before button stepping. | `SyncDirtyInputAsync` in increment/decrement. | Dirty click/pointer tests. |
| Keyboard stepping | Step from dirty text when unsynced; otherwise numeric state. | `HandleKeyDown` passes parsed current override. | Dirty keyboard/precision tests. |
| Wheel scrub | Focused wheel changes only when enabled, not Ctrl, not disabled/read-only. | JS non-passive wheel listener and `OnWheelChange`. | Wheel Playwright tests. |
| Press and hold | Start delay and repeat tick; no commit after canceled/no accepted change. | JS auto-change plus accepted-change tracking. | Unit hold tests; Playwright hold test. |
| Scrub area | Movement magnitude times step amount; pointer lock and viewport handling. | JS scrub module plus async `OnScrubMove`. | Scrub Playwright and source review. |
| Scrub commit | Commit on end only after an accepted scrub value change. | `hasScrubValueChanged`. | Scrub commit tests. |
| State attributes | Root, input, group, steppers, scrub parts expose state data attrs. | `NumberFieldRootState` propagated through `RenderElement`. | Unit state attribute tests. |
| Field/label/description | Label targets visible input; descriptions merge into `aria-describedby`. | `LabelableContext.SetControlId`, `aria-labelledby`, combined `aria-describedby`. | Field integration unit checks; in-app docs comparison. |
| Docs/source parity | React docs anatomy/API shape present in Blazor docs. | Blazor docs page exposes same headings and controls. | `number-field-in-app-browser-docs-comparison.json`. |
