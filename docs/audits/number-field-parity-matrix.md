# NumberField React-to-Blazor Parity Matrix

| React source surface | Required behavior | Blazor equivalent | Verification |
| --- | --- | --- | --- |
| `NumberFieldRoot` controlled/uncontrolled state | `value` prop controls including `null`; omitted value uses `defaultValue` | `SetParametersAsync` detects supplied `Value`; Playwright host omits `Value` for uncontrolled | bUnit `Value_AcceptsNullValue`, Playwright 64/64 |
| `inputValue` and `allowInputSyncRef` | Preserve raw typed text until blur; sync on controlled changes and step actions | `allowInputSync`, `SetAllowInputSync`, `HasPendingCommit`, raw `InputValue` context | bUnit `PreservesRawInputUntilBlur` |
| `lastChangedValueRef` | Commit latest changed value after buttons, wheel, scrub, keyboard | `lastChangedValue` and `HandleValueCommitted` | bUnit commit reason tests; Playwright button/wheel/scrub tests |
| Hidden native input | Submit canonical value with `type=number`, `name`, `form`, `min`, `max`, `step`, `required`, disabled, hidden from AT | Root hidden input renders all required attrs and serializes with invariant value; `step="any"` preserved | bUnit `Form_SetsFormOnHiddenInput`, `HiddenInput_AllowsStepAny`; Playwright form validity |
| `allowOutOfRange` | Direct input may exceed min/max; step interactions still clamp | `ValidateNumber` clamps by reason; input reasons bypass clamp only when enabled | bUnit allow-out-of-range tests; Playwright native overflow |
| `toValidatedNumber` | Clamp, optional snap, directional/nearest step, remove floating point errors | `NumberFieldUtilities.ToValidatedNumber`, `RemoveFloatingPointErrors` | bUnit step and precision tests |
| `parseNumber` | Locale separators, Arabic/Persian/fullwidth/Han digits, Unicode signs, percent/permille, currency/unit, control chars, Infinity rejection | `NumberFieldUtilities.ParseNumber` | bUnit exotic parse tests |
| `formatNumber` / max precision | Locale/format-aware display and controlled max-precision fallback | `NumberFieldUtilities.FormatNumber` | bUnit format/control tests |
| `getAllowedNonNumericKeys` | Locale decimal/group/literal/currency, percent/permille gating, signs by min | C# allowed-string checks and JS keydown filter config | bUnit invalid input; Playwright percent blocked/allowed |
| `NumberFieldInput.onInput` | Immediate raw text update, parseable value changes only | `HandleInput` updates raw `InputValue`, parses through utility | bUnit input-change reason tests |
| `NumberFieldInput.onBlur` | Commit parsed/validated value; format display; mark touched/focused | `HandleBlur` validates, commits, formats, field state update | bUnit blur validation; Playwright blur tests |
| `NumberFieldInput.onKeyDown` | Synchronous preventDefault for invalid keys and action keys | JS `keydown` listener prevents DOM defaults; Blazor handles value changes | Playwright keyboard tests |
| `NumberFieldInput.onPaste` | Read clipboard plain text, prevent default, parse, emit `inputPaste` | JS paste listener invokes `OnPasteText`; root sets raw input and value | Playwright `Paste_EventUsesClipboardTextAndReason` |
| `useNumberFieldButton` | Native button attrs, disabled/read-only guards, click/pointer press, auto-repeat | Increment/decrement components plus JS `startAutoChange` | bUnit button tests; Playwright press-and-hold |
| Wheel scrub | Focused input wheel changes value unless disabled/read-only/Ctrl | JS wheel listener calls `OnWheelChange` | Playwright wheel tests |
| Scrub area | Pointer-drag movement changes by `movement * stepAmount`, commits on pointerup | JS sends movement magnitude; `OnScrubMove(double movement, ...)` | Playwright `ScrubArea_UsesPointerMovementMagnitude` |
| Scrub pointer lock | Await pointer lock; record denied state; skip WebKit/touch | JS `startScrub` awaits `requestPointerLock` and returns denial | Source review; scrub Playwright |
| Scrub viewport teleport | Teleport rect based on element rect plus half teleport distance, visual viewport fallback | JS `getViewportRect` matches React utility | Source review; scrub Playwright |
| Scrub cursor scale | Track visual viewport scale and invert cursor scale | JS visual viewport resize subscription updates `visualScaleRef` | Source review |
| State attributes mapping | `data-scrubbing`, disabled, readonly, required, validity, touched, dirty, filled, focused | Root/input/group/buttons/scrub/cursor component attrs | Existing and added bUnit state tests |
| `useRenderElement` | Custom render receives state and merged attrs | `RenderElement<TState>` with state, class/style functions, component attrs | Existing render override tests |
| Field/form/label contexts | Field focus/touched/dirty/filled, validation, label association, form error clearing | Field/Form/Labelable contexts retained and updated | bUnit field integration tests |
| React source spec scar tissue | Missing spec must be filled for future ports | Added `../base-ui-specs/number-field/SPEC.md` and `pitfalls.md` | Manual check |
