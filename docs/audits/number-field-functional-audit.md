# NumberField Functional Audit Verification Report

Date: 2026-05-31

## Scope

Audited the BlazorBaseUI NumberField port against the React Base UI source under `.base-ui/packages/react/src/number-field`.

Primary React sources checked:

- `root/NumberFieldRoot.tsx`
- `input/NumberFieldInput.tsx`
- `root/useNumberFieldButton.ts`
- `scrub-area/NumberFieldScrubArea.tsx`
- `scrub-area-cursor/NumberFieldScrubAreaCursor.tsx`
- `increment/NumberFieldIncrement.tsx`
- `decrement/NumberFieldDecrement.tsx`
- `group/NumberFieldGroup.tsx`
- `utils/parse.ts`
- `utils/validate.ts`
- `utils/constants.ts`
- `utils/stateAttributesMapping.ts`

## Repairs Applied

- Added `NumberFieldUtilities` for React-equivalent parsing, formatting, validation, step snapping, default precision cleanup, hidden input serialization, and allowed-character checks.
- Added missing `AllowOutOfRange`, `Form`, and `Step="any"` support.
- Preserved raw user input during typing and deferred visible formatting to blur or step interactions.
- Fixed controlled-value detection so explicit `Value=null` is controlled while omitted `Value` remains uncontrolled.
- Added hidden input `form`, `step`, `min`, `max`, required/disabled, and canonical value behavior.
- Moved DOM-only preventDefault and paste handling into the NumberField JS module.
- Added synchronous browser key filtering for invalid characters, percent/permille symbols, duplicate signs, duplicate decimal/currency/percent symbols, and navigation/action keys.
- Fixed paste to use clipboard text and emit `InputPaste`.
- Fixed scrub behavior to use pointer movement magnitude, not only direction.
- Fixed scrub viewport teleport bounds and async pointer-lock handling.
- Added input render update tracking with `UpdatesAttributeName="value"` and `UpdatesAttributeEventName="oninput"`.
- Removed non-stub logic from `NumberFieldInput.cs`.
- Updated Server/WASM Playwright host pages so uncontrolled cases omit `Value` instead of passing `Value=null`.
- Added bUnit contracts and tests for raw-input preservation, form association, `allowOutOfRange`, `step="any"`, Unicode/permille parsing, and React inputMode behavior.
- Added browser tests for paste reason, key filtering, hidden native validity, `step="any"`, and scrub movement magnitude.

## Command Log

Final verification commands:

```bash
dotnet build BlazorBaseUI.slnx -v minimal
```

Result: Build succeeded, 0 warnings, 0 errors.

```bash
bash scripts/lint-rules.sh
```

Result: 0 textual lint violations. Analyzer-enforced structural rules were covered by the solution build.

```bash
git diff --check
```

Result: No whitespace errors.

```bash
dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --filter "FullyQualifiedName~NumberField" --logger "console;verbosity=detailed"
```

Result: Passed 249/249.

```bash
dotnet build tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj -v minimal
```

Result: Build succeeded, 0 warnings, 0 errors.

```bash
dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~NumberField" --logger "console;verbosity=detailed"
```

Result: Passed 64/64. Full log: `docs/audits/logs/number-field-playwright.log`.

```bash
npx --yes terser src/BlazorBaseUI/wwwroot/blazor-baseui-number-field.js -o src/BlazorBaseUI/wwwroot/blazor-baseui-number-field.min.js -c -m
```

Result: Minified bundle regenerated successfully.

In-app browser manual checks on `http://127.0.0.1:5261/tests/numberfield/server`:

- Filled `6` with `max=5` and `allowOutOfRange=true`: display value `6`.
- Hidden native number input validity: `rangeOverflow=true`, `stepMismatch=false` with `stepAny=true`.
- Increment button disabled while value was out of range.
- Percent key blocked when percent formatting was absent.
- Percent input accepted when `formatStyle=percent`, with display value `0.12`.
- Form submission emitted `quantity=4`.

## Automated Log Artifact

The final Playwright execution log is stored at:

```text
docs/audits/logs/number-field-playwright.log
```

Summary from the log:

```text
Test Run Successful.
Total tests: 64
     Passed: 64
```

## Manual Source Checks

- Confirmed React input default `inputMode` starts as `numeric`.
- Confirmed `allowOutOfRange` only bypasses clamp for direct input reasons.
- Confirmed React `removeFloatingPointErrors` rounds through Intl default maximum fraction digits when no explicit format precision is supplied.
- Confirmed React keydown prevention is synchronous and cannot be implemented safely in Blazor event callbacks alone.
- Confirmed React scrub increments by `movement * stepAmount`; Blazor now passes movement magnitude through JS interop.
- Confirmed React paste reads `clipboardData.getData("text/plain")`, prevents the default input event, and emits `inputPaste`.

## Result

No known functional gaps remain in the audited NumberField surface.
