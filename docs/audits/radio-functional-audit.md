# Radio and RadioGroup Functional Audit

Date: 2026-05-28

Component: `RadioGroup`, `Radio.Root`, `Radio.Indicator`

React source audited: `.base-ui/packages/react/src/radio-group`, `.base-ui/packages/react/src/radio`

Blazor source audited: `src/BlazorBaseUI/RadioGroup`, `src/BlazorBaseUI/Radio`, `src/BlazorBaseUI/wwwroot/blazor-baseui-radio.js`

Spec artifacts:

- `../base-ui-specs/radio/SPEC.md`
- `../base-ui-specs/radio/pitfalls.md`

Proof artifacts:

- `docs/audits/radio-parity-matrix.md`
- `docs/audits/logs/radio-dotnet-build.log`
- `docs/audits/logs/radio-bunit-tests.log`
- `docs/audits/logs/radio-playwright-tests.log`
- `docs/audits/logs/radio-playwright-wasm-arrowup-single.log`
- `docs/audits/logs/radio-js-syntax-check.log`
- `docs/audits/logs/radio-lint-rules.log`
- `docs/audits/logs/radio-git-diff-check.log`

Tool note: Serena and Context7 tools were not exposed in this session after tool discovery. The audit used the local React Base UI source, repository files, `rg`, and targeted shell inspection.

## React Parts Found

- `RadioGroup`
- `RadioGroupContext`
- `Radio.Root`
- `RadioRootContext`
- `Radio.Indicator`
- `stateAttributesMapping`

## Resolved Gaps

| Gap | React source behavior | Blazor repair | Verification |
| --- | --- | --- | --- |
| Group rendered a hidden input | React relies on child radio hidden inputs and does not render a group-level hidden radio input. | Removed the group-level hidden radio input. | bUnit `DoesNotRenderGroupLevelHiddenRadioInput`; Playwright form data checks |
| Controlled value detection used callback presence | React `useControlled` treats supplied `value` as controlled even without `onValueChange`. | `RadioGroup.SetParametersAsync` now detects supplied `Value` and treats it as controlled mode. | bUnit `ValueParameterControlsSelectionWithoutValueChanged` |
| `form` was not propagated | React group `form` is passed through context to every child radio input. | Added `Form` to `RadioGroup` and context; child hidden inputs render `form`. | bUnit `FormPropPassesToEachRadioInput`; Playwright form tests |
| Root id target was incorrect | React maps explicit id to the hidden input in non-native mode. | Non-native root uses an internal id; hidden input receives the explicit/generated control id. | bUnit `ExplicitIdAssociatesHiddenInputNotNonNativeRoot` |
| Native button mode absent | React supports `nativeButton`, using a native `button` root and moving the explicit id to that root. | Added `NativeButton`; root renders `button type="button"` with native disabled behavior; hidden input omits the id. | bUnit `NativeButtonUsesExplicitIdOnRootAndOmitsHiddenInputId`; Playwright `NativeButton_RendersButtonRootAndSelects` |
| Disabled non-native aria missing | React `useButton` exposes non-native disabled state through ARIA rather than HTML `disabled`. | Added `aria-disabled="true"` to disabled non-native roots. | bUnit `HasAriaDisabledWhenDisabled`; Playwright `DisabledGroup_PropagatesAriaDisabled` |
| Standalone empty value mismatch | React checks a standalone radio only when `value === ''`. | `CurrentChecked` now treats standalone empty string as checked. | bUnit `StandaloneEmptyValueIsChecked` |
| Null serialization mismatch | React `serializeValue` serializes null to an empty input value. | `SerializeValue` now maps null to `""` and keeps a separate JS null marker for navigation. | bUnit `NullValueSerializesToEmptyInputValue`; Playwright navigation still passes |
| User ARIA overrides were not rigid | React merges default props before element props, allowing user override. | Group/root attribute builders now detect explicit ARIA attributes and preserve external descriptions. | bUnit override and description checks |
| Indicator allowed missing root context | React context hook throws outside `Radio.Root`. | `RadioIndicator` throws when `RadioRootContext` is missing. | bUnit `ThrowsWithoutRadioRootContext` |
| Indicator initial checked transition differed | React `useTransitionStatus` does not add starting style on first checked render. | Initial checked indicator render omits `data-starting-style`. | bUnit `InitiallyCheckedIndicatorDoesNotUseStartingStyle` |
| Indicator exit used fixed timing | React completion is based on DOM transition/animation completion. | `RadioIndicator` now uses shared animations JS for entry/exit completion and unmount. | bUnit transition coverage; JS syntax check |
| Arrow navigation had excess interop round trips | React CompositeRoot owns DOM-order keyboard navigation. | Group JS capture handler now performs DOM-order arrow navigation, focus movement, disabled/read-only checks, and one value callback. | Playwright 52/52, no skips |
| WASM arrow tests were skipped | Previous test class skipped four WASM arrow tests under concurrent load. | Removed skips and added a serialized RadioGroup Playwright collection using existing repo pattern. | Playwright 52/52, no skips |

## Implementation Summary

- Added shared attribute helpers for case-insensitive attribute checks and id-reference combination.
- Expanded `RadioGroupContext<TValue>` with `Form` and nullable value selection.
- Reworked `RadioGroup` controlled/uncontrolled lifecycle, field integration, group attributes, blur handling, and value deserialization.
- Reworked `Radio.Root` id ownership, native button rendering, hidden input attributes, null serialization, field focus ownership, and ARIA/data attributes.
- Reworked `Radio.Indicator` context guard and animation-completion lifecycle through shared JS.
- Reworked `blazor-baseui-radio.js` to keep DOM-order navigation and focus management in JS.
- Added bUnit contract coverage for controlled mode, form propagation, native button mode, id ownership, null serialization, disabled ARIA, standalone empty values, and indicator context/transition behavior.
- Added Playwright coverage for disabled ARIA, native form data, empty native form data, native button mode, and no-skip WASM arrow navigation.

## Verification Commands

| Command | Result | Log |
| --- | --- | --- |
| `dotnet build BlazorBaseUI.slnx` | Passed, 0 warnings, 0 errors | `docs/audits/logs/radio-dotnet-build.log` |
| `dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --filter "FullyQualifiedName~RadioGroup|FullyQualifiedName~RadioRoot|FullyQualifiedName~RadioIndicator"` | Passed, 112/112 | `docs/audits/logs/radio-bunit-tests.log` |
| `dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~RadioGroup"` | Passed, 52/52, 0 skipped | `docs/audits/logs/radio-playwright-tests.log` |
| `dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName=BlazorBaseUI.Playwright.Tests.Tests.RadioGroup.RadioGroupTestsWasm.ArrowUp_MovesToPreviousRadio"` | Passed, 1/1 diagnostic reproduction | `docs/audits/logs/radio-playwright-wasm-arrowup-single.log` |
| `node --check src/BlazorBaseUI/wwwroot/blazor-baseui-radio.js` and `node --check src/BlazorBaseUI/wwwroot/blazor-baseui-radio.min.js` | Passed | `docs/audits/logs/radio-js-syntax-check.log` |
| `bash scripts/lint-rules.sh` | Passed, 0 violations. macOS `grep -P` warnings are recorded before the zero-violation summary. | `docs/audits/logs/radio-lint-rules.log` |
| `git diff --check` | Passed, no whitespace errors | `docs/audits/logs/radio-git-diff-check.log` |

## Manual Checks

- Compared React group, root, indicator, context, and data-attribute files against the Blazor port.
- Confirmed the group no longer emits a hidden radio input.
- Confirmed native form ownership is per child radio input and includes external `form`.
- Confirmed controlled mode is based on supplied `Value`, including null.
- Confirmed non-native id ownership targets the hidden input and native button id ownership targets the root button.
- Confirmed `aria-disabled` is present on disabled non-native roots and native disabled mode uses the HTML `disabled` attribute.
- Confirmed root hidden inputs include required native form attributes and visually hidden styling.
- Confirmed explicit ARIA attributes override generated defaults where React element props override default props.
- Confirmed `aria-describedby` preserves external ids while appending field descriptions.
- Confirmed null radio value navigation preserves null identity separately from serialized empty input value.
- Confirmed arrow navigation remains DOM-heavy JS and no longer depends on a bubbled Blazor event for movement.
- Confirmed indicator mount, starting style, ending style, and unmount timing follow animation completion instead of fixed timers.
- Confirmed Server and WASM browser coverage includes click, keyboard, disabled, read-only, focus, form, controlled, and native-button states.

## Final State

No remaining Radio or RadioGroup parity gaps were identified in the audited local React source after the repairs above. The enabled automated suite now has no RadioGroup Playwright skips.
