# Switch Functional Audit

Date: 2026-05-30

Component: `Switch.Root`, `Switch.Thumb`

React source audited: `.base-ui/packages/react/src/switch`

Blazor source audited: `src/BlazorBaseUI/Switch`, `src/BlazorBaseUI/wwwroot/blazor-baseui-switch.js`

Spec artifacts:

- `../base-ui-specs/switch/SPEC.md`
- `../base-ui-specs/switch/pitfalls.md`

Proof artifacts:

- `docs/audits/switch-parity-matrix.md`
- `docs/audits/logs/switch-bunit-tests.log`
- `docs/audits/logs/switch-dotnet-build.log`
- `docs/audits/logs/switch-playwright-tests.log`
- `docs/audits/logs/switch-js-minify.log`
- `docs/audits/logs/switch-js-syntax-check.log`
- `docs/audits/logs/switch-lint-rules.log`
- `docs/audits/logs/switch-git-diff-check.log`

Tool note: Serena and Context7 tools were not exposed in this session. The audit used the local React Base UI source, repository files, `rg`, and targeted shell inspection.

## React Parts Found

- `SwitchRoot`
- `SwitchRootContext`
- `SwitchThumb`
- `stateAttributesMapping`
- `SwitchRootDataAttributes`
- `SwitchThumbDataAttributes`
- `useControlled`
- `useButton`
- `useFocusableWhenDisabled`
- `useAriaLabelledBy`
- `useLabelableId`
- `useRegisterFieldControl`
- `useValueChanged`
- `useRenderElement`
- `createChangeEventDetails`
- `visuallyHidden`
- `visuallyHiddenInput`

## Resolved Gaps

| Gap | React source behavior | Blazor repair | Verification |
| --- | --- | --- | --- |
| `form` prop absent | React applies `form` to the checkbox input and unchecked hidden input. | Added `Form` parameter and forwarded it to both inputs. | bUnit `SetsFormOnInputsOnly`; Playwright `FormAttribute_AppliesToHiddenInputsOnly` |
| Disabled non-native root ARIA incomplete | React non-native switch has `aria-disabled="true"` and no HTML `disabled`. | Root attribute builder emits `aria-disabled` only for non-native disabled roots. | bUnit `UsesAriaDisabledInsteadOfHtmlDisabled`; Playwright disabled callback test |
| Disabled click still reached user callback | React `useButton` prevents disabled non-native activation. | JS disabled click path now prevents default and stops propagation. | Playwright `DisabledSwitch_HasAriaDisabledAndDoesNotInvokeClickCallback` |
| Explicit ARIA and role overrides were not rigid | React user element props can override generated root props. | Attribute builder skips generated `role`, checked ARIA, validity ARIA, label ARIA, and data attributes when supplied. | bUnit `RoleAttributeOverridesBuiltInRole`, label override tests |
| Field description wiring incomplete | React applies field description ids to the input and combines external plus field descriptions on the root. | Added `InputAriaDescribedBy` and combined root `aria-describedby`. | bUnit `CombinesExternalAriaDescribedByWithFieldDescription` |
| Native label fallback absent | React derives fallback `aria-labelledby` from the hidden input's native label association. | JS resolves associated native labels, assigns stable ids, and updates root `aria-labelledby`. | Playwright `LinkedLabel_ProvidesFallbackAriaLabelledBy`, dynamic id update test |
| Native-button id ownership incomplete | React applies explicit/generated id to the native button root and omits it from the hidden input. | `UpdateLabelableControlId` and render paths now target the native button root in native mode. | bUnit native id test; Playwright `NativeButton_LinkedLabelTargetsButton` |
| Hidden input visual hiding did not match source | React switches between `visuallyHidden` and `visuallyHiddenInput` based on name presence. | Added source-equivalent style constants and `HiddenInputStyle`. | bUnit hidden input attribute tests; source audit |
| Modifier event details lost | React dispatches a hidden input click with pointer modifier keys and exposes them in change details. | JS dispatches `PointerEvent`/`MouseEvent` with modifiers; event args expose Shift/Ctrl/Alt/Meta. | bUnit `OnCheckedChangeReceivesModifierKeys`; Playwright modifier test |
| Field registration missed `SwitchRoot.Name` | React registers field control with `nameProp` for form errors and validation. | Added `RegisterFieldControl`, value getter, validation callback, and focus callback. | bUnit `FieldRootUsesSwitchNameForFormErrors` |
| Thumb outside root rendered silently | React context hook throws when `Switch.Thumb` is outside `Switch.Root`. | `SwitchThumb.OnParametersSet` throws the Base UI context error. | bUnit `ThrowsOutsideRoot` |
| Thumb context could become stale | React context provider publishes new state to thumb after root changes. | Root now publishes a fresh `SwitchRootContext` when state changes and no longer marks the cascade as fixed. | bUnit `UpdatesThumbStyleHooksWhenCheckedStateChanges`; Playwright `Thumb_HasMatchingDataAttributes` |
| JS module update path lost input and label state | React effects and refs track updated input id/native label association. | JS `updateState` now refreshes input focus handling and fallback label state. | Playwright dynamic fallback label test |
| `Field.Root.Name` precedence was reversed | React resolves input submission name as `fieldName ?? nameProp`. | `ResolvedName` now uses `FieldContext.Name` before `SwitchRoot.Name`. | bUnit `FieldRootNameTakesPrecedenceOverSwitchName` |
| Ignored input changes retained stale modifier details | React change details are derived from the current event only. | `HandleInputChangeAsync` now consumes and clears captured click details before disabled, read-only, no-op, or canceled exits. | bUnit `ClearsModifierKeysFromIgnoredInputChanges` |
| Non-native keyboard activation skipped the visible root click callback | React `useButton` activation invokes the root click path before hidden input activation. | JS keyboard activation now dispatches a visible-root click and then the hidden-input click, with suppression to prevent duplicate toggles. | Playwright `Enter_InvokesOnClickCallback`, `Space_InvokesOnClickCallback` |
| Read-only keyboard activation did not preserve callback parity | React read-only activation blocks state changes while preserving the user click callback. | JS read-only Enter and Space dispatch only the visible-root click callback and skip hidden-input activation. | Playwright `ReadOnlySwitch_KeyboardInvokesClickCallbackWithoutToggling` |
| Thumb generated data attributes overrode user data attributes | React `useRenderElement` lets user element props override generated state props. | `SwitchThumb` now emits generated data attributes only when the user did not supply the attribute. | bUnit `DataAttributesCanBeOverridden` |
| Minified JS stale | Runtime imports the minified switch module. | Regenerated `blazor-baseui-switch.min.js` from source with Terser. | JS syntax check; Playwright suite |

## Implementation Summary

- Added `Form`, source-equivalent hidden input styles, field control registration, explicit ARIA merge rules, field description wiring, and native label fallback support to `SwitchRoot`.
- Reworked root JS interop to dispatch real click events with modifier keys, block disabled propagation, preserve read-only click callback behavior, focus the visible root from the hidden input, and update native label fallback state.
- Repaired switch context publication so `SwitchThumb` receives checked/unchecked transitions in real browser rendering.
- Added `SwitchChangeReason.None` and modifier-key event details to `SwitchCheckedChangeEventArgs`.
- Added `SwitchThumb` missing-context enforcement.
- Extended bUnit and Playwright coverage for repaired attributes, label behavior, form ownership, modifier keys, disabled propagation, field errors, field-name precedence, non-native keyboard callback parity, read-only keyboard callback parity, and thumb state transitions.

## Verification Commands

| Command | Result | Log |
| --- | --- | --- |
| `npx --yes terser src/BlazorBaseUI/wwwroot/blazor-baseui-switch.js --compress --mangle --module -o src/BlazorBaseUI/wwwroot/blazor-baseui-switch.min.js` | Passed | `docs/audits/logs/switch-js-minify.log` |
| `node --check src/BlazorBaseUI/wwwroot/blazor-baseui-switch.js` | Passed | `docs/audits/logs/switch-js-syntax-check.log` |
| `node --check src/BlazorBaseUI/wwwroot/blazor-baseui-switch.min.js` | Passed | `docs/audits/logs/switch-js-syntax-check.log` |
| `dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --filter "FullyQualifiedName~BlazorBaseUI.Tests.Switch" -v normal` | Passed, 75/75 | `docs/audits/logs/switch-bunit-tests.log` |
| `dotnet build BlazorBaseUI.slnx` | Passed, 0 warnings, 0 errors | `docs/audits/logs/switch-dotnet-build.log` |
| `bash scripts/lint-rules.sh` | Passed, 0 violations | `docs/audits/logs/switch-lint-rules.log` |
| `dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~BlazorBaseUI.Playwright.Tests.Tests.Switch" -v normal` | Passed, 82/82 | `docs/audits/logs/switch-playwright-tests.log` |
| `git diff --check` | Passed, no whitespace errors | `docs/audits/logs/switch-git-diff-check.log` |

## Diagnostic Commands

| Command | Result | Follow-up |
| --- | --- | --- |
| `dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --filter "FullyQualifiedName~SwitchRootTests"` | Passed after root repairs, 56/56 at that point | Superseded by final namespace run |
| `dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --filter "FullyQualifiedName~Switch"` | Failed because the filter matched unrelated Menu tests containing "Switch" in test names | Replaced with namespace filter `FullyQualifiedName~BlazorBaseUI.Tests.Switch` |
| Parallel focused and namespace bUnit runs | One run hit a static web assets cache file lock | Re-ran sequentially; final unit log passed |
| Initial Playwright Switch run | Failed 2/72 on `Thumb_HasMatchingDataAttributes` for server and WASM | Repaired switch context publication; final Playwright passed 76/76 |
| First full Playwright run after adding dynamic-label coverage | Failed 1/76 due a synchronous assertion reading the label id before server-side fallback sync completed | Changed the test to wait for the expected id and root `aria-labelledby`; final Playwright passed 76/76 |
| Focused bUnit regressions for field-name precedence, stale modifier cleanup, and thumb data-attribute overrides | Failed before repair on reversed field-name precedence, stale `ShiftKey`, and generated thumb data attributes overriding user attributes | Repaired root name resolution, input-change detail cleanup, and thumb attribute merge rules; final bUnit passed 75/75 |
| Focused Playwright regressions for non-native Enter, Space, and read-only keyboard click callbacks | Failed 6/6 before JS repair because keyboard activation did not invoke the visible root click callback | Repaired JS keyboard activation dispatch; final Playwright passed 82/82 |

## Manual Checks

- Compared React `SwitchRoot.tsx`, `SwitchThumb.tsx`, `SwitchRootContext.ts`, `stateAttributesMapping.ts`, data-attribute files, and Switch React tests against the Blazor port.
- Confirmed all React root parameters have a Blazor equivalent or explicit accounting: `checked`, `defaultChecked`, `disabled`, `readOnly`, `required`, `name`, `form`, `value`, `uncheckedValue`, `nativeButton`, `onCheckedChange`, `inputRef`, `render`, `className`, `style`, and unmatched element props.
- Confirmed `Field.Root.Name` takes precedence over `SwitchRoot.Name` for submitted hidden input names and field integration.
- Confirmed non-native id ownership matches React: the visible root gets an internal id and the labelable id belongs to the hidden checkbox input.
- Confirmed native-button id ownership matches React: the button root owns the id and the hidden input omits it.
- Confirmed every root ARIA/data attribute from `rootProps`, `validation.getValidationProps`, `getButtonProps`, and `stateAttributesMapping` is present or overrideable.
- Confirmed hidden checkbox attributes match React, including `aria-hidden`, `aria-describedby`, `tabindex`, `checked`, `disabled`, `required`, `form`, `name`, `value`, and source-equivalent visual hiding.
- Confirmed unchecked hidden input renders only when unchecked, named, and `UncheckedValue` is supplied.
- Confirmed disabled non-native roots block state changes and user click callbacks.
- Confirmed non-native Enter and Space invoke the visible root click callback and then hidden input activation.
- Confirmed read-only roots block state changes while preserving visible-root click callbacks for pointer and keyboard activation.
- Confirmed focus from the hidden input redirects to the visible root.
- Confirmed fallback `aria-labelledby` updates when the hidden input id changes.
- Confirmed ignored hidden-input changes do not retain stale pointer modifier details for later changes.
- Confirmed `SwitchThumb` mirrors root state data attributes, lets user-supplied data attributes override generated attributes, and throws outside a root.
- Confirmed runtime JS uses the regenerated minified module.

## Final State

No remaining Switch root or thumb parity gaps were identified in the audited local React source after the repairs above.
