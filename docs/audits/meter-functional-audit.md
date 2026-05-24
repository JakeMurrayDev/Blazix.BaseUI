# Meter Functional Audit

Date: 2026-05-24

## Scope

Component audited: `Meter`

React source inspected:

- `.base-ui/packages/react/src/meter/root/MeterRoot.tsx`
- `.base-ui/packages/react/src/meter/root/MeterRootContext.ts`
- `.base-ui/packages/react/src/meter/label/MeterLabel.tsx`
- `.base-ui/packages/react/src/meter/value/MeterValue.tsx`
- `.base-ui/packages/react/src/meter/track/MeterTrack.tsx`
- `.base-ui/packages/react/src/meter/indicator/MeterIndicator.tsx`
- `.base-ui/packages/react/src/utils/formatNumber.ts`
- `.base-ui/packages/react/src/utils/valueToPercent.ts`
- `.base-ui/packages/react/src/utils/useRegisteredLabelId.ts`
- `.base-ui/packages/utils/src/visuallyHidden.ts`

`../base-ui-specs/meter/SPEC.md` and `../base-ui-specs/meter/pitfalls.md` were not present in this checkout. The audit therefore used `.base-ui` React source as the authoritative specification.

Relevant Meter parts found: `MeterRoot`, `MeterLabel`, `MeterValue`, `MeterTrack`, `MeterIndicator`.

## Implementation Plan

1. Compare React Meter root, label, value, track, indicator, and utilities against the Blazor port.
2. Add failing bUnit contract tests for confirmed parity gaps.
3. Repair only Meter implementation and direct Meter demo/test usages.
4. Add Playwright assertions for browser-visible accessibility parity.
5. Run build, focused bUnit tests, Playwright tests, and lint command with captured logs.

## Resolved Gaps

| Gap | React behavior | Blazor repair |
| --- | --- | --- |
| Hidden NVDA shim missing | `MeterRoot` appends a visually hidden `span role="presentation"` containing `x` | `MeterRoot` now composes child content with the hidden presentation span and exact visually-hidden style intent |
| Label role missing | `MeterLabel` renders `role="presentation"` | `MeterLabel` now emits role by default |
| Label id lifecycle incomplete | `useRegisteredLabelId` registers current id and unregisters on dispose | `MeterLabel` re-registers when the resolved id changes and clears on dispose |
| Context error semantics missing | `useMeterRootContext` throws outside root with the Base UI diagnostic message | `MeterLabel`, `MeterValue`, and `MeterIndicator` throw `InvalidOperationException` with the React-equivalent message outside `MeterRoot` |
| Track incorrectly required root context | React `MeterTrack` does not consume root context | `MeterTrack` now renders outside root with an empty state |
| `aria-valuetext` override blocked | React `elementProps` override default props | Meter default attributes now yield to matching additional attributes |
| Default aria text culture mismatch | React default is raw numeric string plus `%` | Default `aria-valuetext` now uses JavaScript-compatible raw number formatting |
| Locale formatting missing | React `locale` feeds `Intl.NumberFormat` | `MeterRoot` now has `Locale` and formats through matching culture resolution |
| Format options missing | React `format` accepts `Intl.NumberFormatOptions` | `MeterRoot.Format` is now strongly typed as `NumberFormatOptions?`; legacy .NET format strings are exposed through `FormatString` |
| Significant digit formatting ignored | React forwards significant digit options to `Intl.NumberFormat` | Decimal formatting now handles significant digit rounding, padding, grouping, and `minimumIntegerDigits` |
| Indicator style precedence reversed | React user style can override intrinsic style keys | `MeterIndicator` now places intrinsic style before user `StyleValue` output |
| Indicator literal style precedence incomplete | React user `style` can override intrinsic style keys | `MeterIndicator` now filters and reapplies literal `style` after intrinsic styles and before `StyleValue` |
| Zero-range percent clamped | React `valueToPercent` does not guard division by zero | Blazor `ValueToPercent` now mirrors the React formula |
| RenderFragment cached content | User instruction forbids cached RenderFragment content | `MeterValue` now computes content during render without caching a `RenderFragment` field |

## Parity Matrix

| React hook/utility/branch | Blazor equivalent | Verification |
| --- | --- | --- |
| `MeterRootContext` fields: `formattedValue`, `max`, `min`, `setLabelId`, `value` | `MeterRootContext` fields: `FormattedValue`, `Max`, `Min`, `SetLabelIdAction`, `Value` | bUnit context and label association tests |
| `formatNumberValue(value, locale, format)` default percent branch | `MeterRoot.FormatValue` default percent branch with `Locale`/`FormatProvider` | bUnit default value and locale tests |
| Raw number string coercion for default `aria-valuetext` | `MeterRoot.FormatJavaScriptNumber` | bUnit culture, small-number, and large-number tests |
| `formatNumberValue` explicit format branch | `MeterRoot.FormatNumber` for `NumberFormatOptions`; `FormatString` compatibility branch for .NET numeric formats | bUnit `FormatsValueWithNumberFormatOptionsAndLocale`, `FormatsValueWithSignificantDigitOptions`, `FormatsValueWithMinimumIntegerAndSignificantDigitOptions`, and `FormatsValueWithFormatString` |
| `getAriaValueText(formattedValue, value)` callback branch | `GetAriaValueText` callback branch | bUnit callback override test |
| `aria-valuetext` external prop override | Additional attribute override for `aria-valuetext` | bUnit override test |
| `useMeterRootContext` missing-provider branch | Shared `MeterRootContext.MissingContextMessage` and root-dependent component guards | bUnit missing-context message tests |
| `useRegisteredLabelId(idProp, setLabelId)` | `ResolvedId`, `RegisterLabelId`, `Dispose` cleanup | bUnit id generation, id change, and dispose cleanup tests |
| `visuallyHidden` NVDA shim | `VisuallyHiddenStyle` and hidden presentation span | bUnit and Playwright hidden span tests |
| `MeterValue` function child branch | `Func<string, double, RenderFragment>` child branch | bUnit child content argument test |
| `MeterValue` default child branch | `GetResolvedContent()` default branch | bUnit formatted value tests |
| `valueToPercent(value, min, max)` | `MeterIndicator.ValueToPercent` exact formula | bUnit zero-range and width tests |
| `useRenderElement` default element and render prop | `RenderElement<TState>` with `Render`, `ClassValue`, `StyleValue`, attributes, child content | bUnit render/class/style/additional attribute tests |
| Meter state interfaces are empty | `MeterRootState` empty record | Verified no active, disabled, focus, data-state, or JS branches exist in React Meter source |
| JS interop branches | None in React Meter source | No Blazor JS interop added |

## Verification Commands

| Command | Result | Log |
| --- | --- | --- |
| `dotnet build BlazorBaseUI.slnx` | Passed, 0 warnings, 0 errors | `docs/audits/meter-artifacts/meter-dotnet-build.txt` |
| `dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --filter "FullyQualifiedName~BlazorBaseUI.Tests.Meter"` | Passed, 67/67 | `docs/audits/meter-artifacts/meter-bunit-tests.txt` |
| `dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~BlazorBaseUI.Playwright.Tests.Tests.Meter"` | Passed, 22/22 | `docs/audits/meter-artifacts/meter-playwright-tests.txt` |
| `bash scripts/lint-rules.sh` | Failed outside Meter scope | `docs/audits/meter-artifacts/meter-lint-rules.txt` |
| `git diff --check` | Passed | `docs/audits/meter-artifacts/meter-git-diff-check.txt` |

## Lint Result

`scripts/lint-rules.sh` failed on this macOS environment because the script invokes shell features/options not available to the interpreter used here (`declare -A`) and `grep -P`, which BSD `grep` does not support. After those tool errors, the script reported three existing Rule 05 violations outside Meter:

- `src/BlazorBaseUI/wwwroot/blazor-baseui-scroll-lock.js:257`
- `src/BlazorBaseUI/wwwroot/blazor-baseui-scroll-lock.js:268`
- `src/BlazorBaseUI/Popover/PopoverPositioner.razor:11`

No Meter file was reported by the lint summary.
