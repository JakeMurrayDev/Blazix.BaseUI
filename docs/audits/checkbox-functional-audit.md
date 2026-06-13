# Checkbox and CheckboxGroup Functional Audit

Date: 2026-05-29
Follow-up update: 2026-05-30

Component: `Checkbox.Root`, `Checkbox.Indicator`, `CheckboxGroup`

React source audited: `.base-ui/packages/react/src/checkbox`, `.base-ui/packages/react/src/checkbox-group`

Blazor source audited: `src/BlazorBaseUI/Checkbox`, `src/BlazorBaseUI/CheckboxGroup`, `src/BlazorBaseUI/wwwroot/blazor-baseui-checkbox.js`

Spec artifacts:

- `../base-ui-specs/checkbox/SPEC.md`
- `../base-ui-specs/checkbox/pitfalls.md`
- `../base-ui-specs/checkbox-group/SPEC.md`
- `../base-ui-specs/checkbox-group/pitfalls.md`

Proof artifacts:

- `docs/audits/checkbox-parity-matrix.md`
- `docs/audits/logs/checkbox-dotnet-build.log`
- `docs/audits/logs/checkbox-bunit-tests.log`
- `docs/audits/logs/checkbox-playwright-tests.log`
- `docs/audits/logs/checkbox-js-syntax-check.log`
- `docs/audits/logs/checkbox-lint-rules.log`
- `docs/audits/logs/checkbox-git-diff-check.log`

Tool note: Serena and Context7 tools were not exposed in this session. The audit used the local React Base UI source, repository files, `rg`, and targeted shell inspection.

## React Parts Found

- `CheckboxRoot`
- `CheckboxRootContext`
- `CheckboxIndicator`
- `useStateAttributesMapping`
- `CheckboxRootDataAttributes`
- `CheckboxIndicatorDataAttributes`
- `CheckboxGroup`
- `CheckboxGroupContext`
- `useCheckboxGroupParent`
- `CheckboxGroupDataAttributes`

## Resolved Gaps

| Gap | React source behavior | Blazor repair | Verification |
| --- | --- | --- | --- |
| Enter toggled checkbox | React does not activate checkbox with Enter; Enter submits the default form submitter when present. | JS keydown path now prevents checkbox toggle and clicks `form.elements` default submitter after propagation. | Playwright `Enter_DoesNotToggleState`, `Enter_SubmitsFormWithoutToggling`, group Enter tests |
| Native button mode absent | React supports `nativeButton`, moving the id to the button root and omitting hidden input id. | Added `NativeButton`; root conditionally renders `button type="button"` with native disabled state. | bUnit `NativeButtonUsesExplicitIdOnRootAndOmitsHiddenInputId`; Playwright native button test |
| `form` prop absent | React forwards `form` to checkbox input and unchecked hidden input. | Added `Form` parameter and applied it to both inputs. | bUnit `FormPropPassesToCheckboxInputAndUncheckedHiddenInput`; Playwright form submission |
| Disabled non-native aria missing | React non-native disabled checkbox exposes `aria-disabled="true"`. | Added default `aria-disabled` for disabled non-native roots and group-propagated disabled children. | bUnit disabled ARIA tests |
| User ARIA overrides not rigid | React default props merge before user element props. | Attribute builders now omit generated `role`, `aria-checked`, `aria-labelledby`, `aria-invalid`, and related attributes when user values exist. | bUnit override tests |
| External descriptions lost | React `getDescriptionProps` combines external and field description ids. | Root and group combine external `aria-describedby` with labelable descriptions. | bUnit description combination tests |
| Field label overrode explicit label | React lets explicit `aria-labelledby` win. | Root and group preserve explicit `aria-labelledby`. | bUnit label override test |
| Labelable id target incorrect | React registers the actual control id: hidden input for non-native, native button for native mode. | Labelable context now uses the refreshed input id or native root id. | bUnit id/native tests |
| Parent checkbox id mismatch | React parent root id is the prefix used by `aria-controls`. | Group parent id now owns both parent root id and child id prefix. | bUnit parent `aria-controls` test |
| Parent mixed cycle lost snapshot | React restores the last mixed child snapshot on the third parent toggle. | `CheckboxGroupParent` now tracks uncontrolled child snapshots and restores them after all/off cycle. | bUnit parent mixed snapshot test |
| Parent all/off branch used saved snapshot as current value | React decides whether to set all or none with the current group value length. | Parent all/off branch now uses `GetValue()` for the current-value comparison while still using the saved snapshot for disabled preservation. | bUnit `ParentCheckbox_TogglesBackOnAfterAllChildrenWereTurnedOff` |
| Empty parent checked state differed | React `checked = value.length === allValues.length`. | Removed non-empty guard. | bUnit parent state coverage |
| Disabled child preservation used wrong basis | React computes parent all/none from saved uncontrolled snapshot. | Parent all/none calculations now use the saved snapshot and disabled-state map. | bUnit parent cycle tests |
| Event details incomplete | React change details include reason, cancellation, and propagation allowance. | Checkbox and group event args now expose `Reason=None`, `Cancel`, `IsCanceled`, `AllowPropagation`, and `IsPropagationAllowed`; checkbox args preserve modifier keys. | bUnit event-detail tests |
| Indicator allowed missing root context | React context hook throws outside `Checkbox.Root`. | `CheckboxIndicator` now throws the React-equivalent context error. | bUnit `ThrowsWhenRenderedOutsideCheckboxRoot` |
| Indicator exit used fixed timers | React waits for DOM animation/transition completion. | Indicator now uses shared `blazor-baseui-animations.min.js` for entry and exit completion. | bUnit indicator tests; Playwright suite; JS syntax check |
| Canceled or derived-state change could paint source-inconsistent attributes | React applies checkbox root attributes only from committed state/context after cancellation, controlled props, indeterminate props, and parent group state are resolved. | Added `AllowsOptimisticState` gating so controlled, indeterminate, parent-enabled group, standalone `OnCheckedChange`, and group `OnValueChange` paths dispatch immediately without optimistic checked attributes; `resetState` clears any pending optimistic timer. | bUnit optimistic-state flag tests; browser mutation-frame check |
| Minified JS stale | Runtime imports the minified checkbox module. | Regenerated `blazor-baseui-checkbox.min.js` from the source module. | JS syntax checks and Playwright run |
| Lint script was not portable on macOS | `grep -P` emitted warnings under BSD grep. | Converted textual lint script patterns to portable extended regex. | `checkbox-lint-rules.log` |

## Implementation Summary

- Added `NativeButton`, `Form`, `InputElement`, root id ownership, hidden input form forwarding, native button rendering, and labelable-control synchronization to `CheckboxRoot`.
- Rebuilt checkbox root attribute generation around explicit user overrides, combined description ids, disabled ARIA, parent `aria-controls`, and state data attributes.
- Reworked checkbox JS to keep Space toggling, prevent Enter toggling, implement implicit form submission, preserve modifiers, and update native/non-native state.
- Gated checkbox JS optimistic paint so cancel-capable, controlled, indeterminate, and parent-enabled group paths never expose transient attributes that React source would not derive from committed state.
- Reworked `CheckboxIndicator` to use native Blazor lifecycle plus shared DOM animation interop instead of fixed timers.
- Added reason and propagation details to checkbox and checkbox-group event args.
- Repaired parent checkbox group id, checked-state, disabled-state, and mixed-snapshot algorithms.
- Added focused bUnit coverage for every repaired attribute and logic branch.
- Added Playwright coverage for Enter behavior, form submission, native button mode, and group Enter behavior.

## Verification Commands

| Command | Result | Log |
| --- | --- | --- |
| `dotnet build BlazorBaseUI.slnx` | Passed, 0 warnings, 0 errors | `docs/audits/logs/checkbox-dotnet-build.log` |
| `dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --filter "FullyQualifiedName~CheckboxRootTests\|FullyQualifiedName~CheckboxIndicatorTests\|FullyQualifiedName~CheckboxGroupTests"` | Passed, 114/114 | `docs/audits/logs/checkbox-bunit-tests.log` |
| `dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~Checkbox"` | Passed, 140/140, 0 skipped | `docs/audits/logs/checkbox-playwright-tests.log` |
| `node --check src/BlazorBaseUI/wwwroot/blazor-baseui-checkbox.js` and `node --check src/BlazorBaseUI/wwwroot/blazor-baseui-checkbox.min.js` | Passed | `docs/audits/logs/checkbox-js-syntax-check.log` |
| `bash scripts/lint-rules.sh` | Passed, 0 violations | `docs/audits/logs/checkbox-lint-rules.log` |
| `git diff --check` | Passed, no whitespace errors | `docs/audits/logs/checkbox-git-diff-check.log` |
| `dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --filter "FullyQualifiedName~Checkbox"` | Passed, 162/162 | Follow-up flicker repair, 2026-05-30 |
| `dotnet build BlazorBaseUI.slnx` | Passed, 0 warnings, 0 errors | Follow-up flicker repair, 2026-05-30 |
| Browser mutation-frame check on `http://localhost:5228/checkbox` | Passed; canceled checkbox never emitted `data-checked` or `aria-checked="true"` before, during, or after the click | Screenshot: `.claude/tmp-files/checkbox-cancel-no-flicker.png` |
| Browser immediate-state check on uncontrolled `Basic Checkbox` | Passed; uncontrolled non-cancelable checkbox still updated data attributes immediately | Follow-up flicker repair, 2026-05-30 |

## Manual Checks

- Compared React root, indicator, group, parent helper, contexts, data-attribute definitions, and tests against the Blazor port.
- Confirmed no local checkbox or checkbox-group spec existed before this audit; created both specs and pitfalls files.
- Confirmed every React root attribute has a Blazor equivalent or explicit attribute-override path.
- Confirmed unchecked hidden input only renders under React-equivalent conditions.
- Confirmed non-native id belongs to the hidden input and native id belongs to the button root.
- Confirmed Enter no longer toggles standalone or grouped checkboxes.
- Confirmed implicit form submission uses the default submitter and does not check the checkbox.
- Confirmed parent `aria-controls` values match child input ids.
- Confirmed group disabled state propagates to children as disabled input state plus non-native `aria-disabled`.
- Confirmed indicator mount and unmount are DOM-animation driven.
- Confirmed runtime JS uses the minified module that was regenerated from source.
- Confirmed cancelable `OnCheckedChange` no longer paints checked for a frame when `Cancel()` rejects the change.
- Confirmed uncontrolled non-cancelable checkbox optimism remains active.

## Final State

No remaining Checkbox, Checkbox.Indicator, or CheckboxGroup parity gaps were identified in the audited local React source after the repairs above.
