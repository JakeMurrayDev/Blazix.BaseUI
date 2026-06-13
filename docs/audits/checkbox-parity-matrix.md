# Checkbox and CheckboxGroup Parity Matrix

Date: 2026-05-29
Follow-up update: 2026-05-30

Component scope: `Checkbox.Root`, `Checkbox.Indicator`, `CheckboxGroup`

React source audited:

- `.base-ui/packages/react/src/checkbox/root/CheckboxRoot.tsx`
- `.base-ui/packages/react/src/checkbox/root/CheckboxRootContext.ts`
- `.base-ui/packages/react/src/checkbox/indicator/CheckboxIndicator.tsx`
- `.base-ui/packages/react/src/checkbox/utils/useStateAttributesMapping.ts`
- `.base-ui/packages/react/src/checkbox-group/CheckboxGroup.tsx`
- `.base-ui/packages/react/src/checkbox-group/CheckboxGroupContext.ts`
- `.base-ui/packages/react/src/checkbox-group/useCheckboxGroupParent.ts`

## Hook and Utility Matrix

| React source behavior | Blazor equivalent | Status |
| --- | --- | --- |
| `useRenderElement` | `RenderElement<TState>` for root, indicator, and group | Verified |
| `CheckboxRootContext` | `CheckboxRootContext` cascade with root state for indicator | Verified |
| `useCheckboxRootContext` missing-context guard | `CheckboxIndicator.OnParametersSet` throws the same Base UI context error | Verified |
| `CheckboxGroupContext` | `CheckboxGroupContext` cascade with value, default value, all values, disabled, validation, setter, parent helper, and child registration | Verified |
| `useControlled` for checkbox checked | `Checked.HasValue` controlled mode, otherwise `DefaultChecked` or group default membership | Verified |
| `useControlled` for group value | `Value is not null` controlled mode, otherwise `DefaultValue ?? []` | Verified |
| `useBaseUiId` | `Guid.ToIdString()` generated ids for root/control and group parent ids | Verified |
| `useButton` non-native disabled | Non-native root has ARIA disabled state and no root HTML disabled attribute | Verified |
| `nativeButton` | `NativeButton` renders `button type="button"` and uses HTML disabled | Verified |
| `getDefaultFormSubmitter` | JS iterates `form.elements` and clicks the first submit button/input | Verified |
| `ownerWindow(PointerEvent)` click dispatch | JS dispatches `PointerEvent` or `MouseEvent` click to hidden input with modifier keys | Verified |
| Source-derived visual state avoids pre-commit checked paint | `AllowsOptimisticState` disables JS optimistic attributes for controlled checkbox, indeterminate checkbox, parent-enabled group, standalone `OnCheckedChange`, and group `OnValueChange` paths | Verified |
| `useAriaLabelledBy` | explicit `aria-labelledby` preserved; labelable fallback supplied when absent | Verified |
| `getDescriptionProps` | external `aria-describedby` combined with field descriptions | Verified |
| `useRegisterFieldControl` | labelable control id and field focus handler target the actual hidden input/native button | Verified |
| `useFieldRootContext` | field disabled, focused, touched, dirty, filled, valid, and validation state | Verified |
| `useFieldItemContext` | field item disabled contributes to checkbox disabled state | Verified |
| `useFormContext.clearErrors` | `FormContext?.ClearErrors(ResolvedName)` on standalone/group value changes | Verified |
| `useValueChanged` | Blazor lifecycle updates dirty/filled state and commits validation | Verified |
| `createChangeEventDetails` | event args expose reason `None`, cancel state, propagation allowance, and checkbox modifier keys | Verified |
| `visuallyHidden` / `visuallyHiddenInput` | checkbox input style switches between fixed visually hidden and absolute visually hidden input styles | Verified |
| `useStateAttributesMapping` | root and indicator emit checked/unchecked/indeterminate and field validity data attributes | Verified |
| `useTransitionStatus` | indicator tracks starting and ending status without initial checked starting style | Verified |
| `useOpenChangeComplete` | shared animations JS waits for DOM animation/transition completion before unmount | Verified |
| `useCheckboxGroupParent` | `CheckboxGroupParent` tracks id, checked, indeterminate, disabled states, parent cycle, and child snapshot | Verified |
| `useStableCallback` | Blazor event callbacks and context setters use current component state without stale closure loops | Accounted |
| `areArraysEqual` | order-insensitive `ArraysEqual` for dirty checks | Verified |

## Attribute Matrix

| Part | React attributes | Blazor status |
| --- | --- | --- |
| `Checkbox.Root` root | default `span` | Present |
| `Checkbox.Root` root | optional native `button type="button"` | Present |
| `Checkbox.Root` root | id on internal root for non-native mode | Present |
| `Checkbox.Root` root | explicit/generated control id on native button mode | Present |
| `Checkbox.Root` root | `role="checkbox"` unless overridden | Present |
| `Checkbox.Root` root | `aria-checked="true\|false\|mixed"` unless overridden | Present |
| `Checkbox.Root` root | `aria-disabled="true"` for non-native disabled | Present |
| `Checkbox.Root` root | native button `disabled` when disabled | Present |
| `Checkbox.Root` root | `aria-readonly`, `aria-required`, `aria-invalid` | Present |
| `Checkbox.Root` root | `aria-labelledby` user override or labelable fallback | Present |
| `Checkbox.Root` root | `aria-describedby` user ids plus field descriptions | Present |
| `Checkbox.Root` parent | `data-parent` | Present |
| `Checkbox.Root` parent | `aria-controls` child input ids | Present |
| `Checkbox.Root` root | focus and blur handlers | Present |
| `Checkbox.Root` root | state data attributes | Present |
| `Checkbox.Root` checkbox input | `type="checkbox"` | Present |
| `Checkbox.Root` checkbox input | `id` only when non-native | Present |
| `Checkbox.Root` checkbox input | `checked`, `disabled`, `required` | Present |
| `Checkbox.Root` checkbox input | `form`, `name`, `value` | Present |
| `Checkbox.Root` checkbox input | `aria-hidden="true"`, `tabindex="-1"` | Present |
| `Checkbox.Root` checkbox input | visually hidden style | Present |
| `Checkbox.Root` unchecked input | `type="hidden"`, `form`, `name`, `value` | Present |
| `Checkbox.Indicator` root | default `span` | Present |
| `Checkbox.Indicator` root | state data attributes | Present |
| `Checkbox.Indicator` root | `data-starting-style`, `data-ending-style` | Present |
| `CheckboxGroup` root | default `div` | Present |
| `CheckboxGroup` root | `id` | Present |
| `CheckboxGroup` root | `role="group"` unless overridden | Present |
| `CheckboxGroup` root | `aria-labelledby` user override or label fallback | Present |
| `CheckboxGroup` root | `aria-describedby` user ids plus field descriptions | Present |
| `CheckboxGroup` root | field validity data attributes | Present |

## Behavior Matrix

| Behavior | Coverage |
| --- | --- |
| Click toggles standalone checkbox | Server and WASM Playwright |
| Space toggles standalone checkbox | Server and WASM Playwright |
| Enter does not toggle standalone checkbox | Server and WASM Playwright |
| Enter submits default form submitter without toggling | Server and WASM Playwright |
| Disabled click and keyboard blocking | Server and WASM Playwright; bUnit |
| Read-only click and keyboard blocking | Server and WASM Playwright; bUnit |
| Focus redirect from hidden input to root | Server and WASM Playwright |
| Controlled external state updates | Server and WASM Playwright; bUnit |
| Native button root rendering and keyboard behavior | Server and WASM Playwright; bUnit |
| Unchecked hidden form value | bUnit |
| External `form` ownership | bUnit; Playwright form submission |
| `aria-disabled` on disabled non-native roots | bUnit |
| User role/ARIA override | bUnit |
| Combined `aria-describedby` | bUnit |
| Indicator default render suppression | bUnit |
| Indicator keep mounted | bUnit |
| Indicator checked and indeterminate state data | bUnit |
| Indicator missing context error | bUnit |
| Indicator animation lifecycle | bUnit plus shared JS syntax and Playwright smoke coverage |
| Group child toggling | Server and WASM Playwright; bUnit |
| Group Enter does not toggle | Server and WASM Playwright |
| Group disabled propagation | bUnit |
| Group event cancellation | bUnit |
| Canceled, indeterminate, and parent-derived state avoids one-frame source-inconsistent flicker | bUnit optimistic-state flag tests; browser mutation-frame check |
| Group event reason and propagation details | bUnit |
| Parent `aria-controls` id matching | bUnit |
| Parent mixed snapshot restore | bUnit |
| Parent all-off-all cycle from initially checked state | bUnit |
| Parent disabled checked child preservation | bUnit |

## Source Accounting

| React source file | Blazor file(s) | Status |
| --- | --- | --- |
| `checkbox/root/CheckboxRoot.tsx` | `CheckboxRoot.razor`, `EventArgs.cs`, `Enumerations.cs`, `blazor-baseui-checkbox.js` | Verified |
| `checkbox/root/CheckboxRootContext.ts` | `CheckboxRootContext.cs` | Verified |
| `checkbox/indicator/CheckboxIndicator.tsx` | `CheckboxIndicator.razor`, `CheckboxIndicatorState.cs`, shared animations JS | Verified |
| `checkbox/utils/useStateAttributesMapping.ts` | `CheckboxRootState`, `CheckboxIndicatorState`, attribute builders | Verified |
| `checkbox/root/CheckboxRootDataAttributes.ts` | root attribute builder | Verified |
| `checkbox/indicator/CheckboxIndicatorDataAttributes.ts` | indicator attribute builder | Verified |
| `checkbox-group/CheckboxGroup.tsx` | `CheckboxGroup.razor`, `EventArgs.cs`, `Enumerations.cs` | Verified |
| `checkbox-group/CheckboxGroupContext.ts` | `CheckboxGroupContext.cs` | Verified |
| `checkbox-group/useCheckboxGroupParent.ts` | `CheckboxGroupParent` | Verified |
| `checkbox-group/CheckboxGroupDataAttributes.ts` | group attribute builder | Verified |
