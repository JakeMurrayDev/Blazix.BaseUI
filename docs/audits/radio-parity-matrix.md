# Radio and RadioGroup Parity Matrix

Date: 2026-05-28

Component scope: `RadioGroup`, `Radio.Root`, `Radio.Indicator`

React source audited:

- `.base-ui/packages/react/src/radio-group/RadioGroup.tsx`
- `.base-ui/packages/react/src/radio-group/RadioGroupContext.ts`
- `.base-ui/packages/react/src/radio/root/RadioRoot.tsx`
- `.base-ui/packages/react/src/radio/root/RadioRootContext.ts`
- `.base-ui/packages/react/src/radio/indicator/RadioIndicator.tsx`
- `.base-ui/packages/react/src/radio/utils/stateAttributesMapping.ts`

## Hook and Utility Matrix

| React source behavior | Blazor equivalent | Status |
| --- | --- | --- |
| `useRender` / `useRenderElement` | `RenderElement<TState>` for group, root, and indicator | Verified |
| `RadioGroupContext` | `RadioGroupContext<TValue>` cascade with value, disabled, read-only, required, name, form, validation, and field state | Verified |
| `RadioRootContext` | `RadioRootContext` cascade for indicator state | Verified |
| `useControlled` | `SetParametersAsync` detects supplied `Value`; uncontrolled mode uses `DefaultValue` and `internalValue` | Verified |
| Controlled `value` without callback | `Value` presence controls selection even when `ValueChanged` is absent | Verified |
| `onValueChange` cancel details | `RadioGroupValueChangeEventArgs<TValue>` with cancellation before internal state mutation | Verified |
| `useValueChanged` | `HandleValueChangedAsync` clears form errors, updates dirty/filled state, and runs change validation | Verified |
| `useRegisterFieldControl` | `FieldContext.RegisterFocusHandlerFunc(FocusFirstRadioAsync)` and child radio registration | Verified |
| `useFieldRootContext` | `FieldContext` integration for disabled, focused, touched, dirty, filled, validity, and validation | Verified |
| `useFormContext.clearErrors` | `FormContext?.ClearErrors(ResolvedName)` | Verified |
| `useLabelableContext` | `LabelableContext` control id, label id, and description wiring | Verified |
| `useFieldsetRootContext` | `FieldsetContext` legend id fallback for group labelling | Verified |
| `contains` blur guard | JS `isBlurWithinGroup` plus Blazor blur lifecycle | Verified |
| `CompositeRoot` roving navigation | Group JS registration, DOM-order sorting, wrapping, disabled skip, and focus movement | Verified |
| `enableHomeAndEndKeys={false}` | No Home/End navigation path exists in Blazor radio JS | Verified |
| `modifierKeys={[SHIFT]}` | Shift does not block arrow navigation in JS handler | Verified |
| `CompositeItem` | `data-radio-item` registration and roving `tabindex` on grouped roots | Verified |
| `useButton` non-native disabled handling | Non-native roots use `aria-disabled` and no HTML `disabled` attribute | Verified |
| `nativeButton` | `NativeButton` renders a `button type="button"` root and uses HTML `disabled` | Verified |
| `useBaseUiId` | generated root/control ids through `Guid.ToIdString()` and labelable context | Verified |
| `useLabelableId` | explicit id maps to hidden input for non-native root and button root for native mode | Verified |
| `useAriaLabelledBy` | explicit `aria-labelledby` override, label fallback, hidden-input label fallback behavior | Verified |
| `getDescriptionProps` | external `aria-describedby` combined with field descriptions | Verified |
| `serializeValue` | `SerializeValue`: null to empty string, string passthrough, JSON with string fallback | Verified |
| Hidden input click path | Blazor click/input handlers select through the hidden input state model; JS prevents default keyboard scroll | Verified |
| `ownerWindow(PointerEvent)` dispatch | Accounted by native Blazor event handling plus hidden input selection; no synthetic pointer dispatch required | Accounted |
| `visuallyHidden` / `visuallyHiddenInput` | hidden radio input style in Razor markup | Verified |
| `useTransitionStatus` | `RadioIndicator` transition state with initial checked render excluded from starting style | Verified |
| `useOpenChangeComplete` | shared animations JS waits for exit animation/transition completion before unmount | Verified |
| `stateAttributesMapping` | state-driven data attributes on root and indicator | Verified |

## Attribute Matrix

| Part | React attributes | Blazor status |
| --- | --- | --- |
| `RadioGroup` root | `role="radiogroup"` unless overridden | Present |
| `RadioGroup` root | `aria-required`, `aria-disabled`, `aria-readonly` | Present |
| `RadioGroup` root | `aria-labelledby` from user, label, or fieldset legend | Present |
| `RadioGroup` root | `aria-describedby` from user plus field description | Present |
| `RadioGroup` root | focus, blur, and keydown capture behavior | Present |
| `RadioGroup` root | field validity data attributes | Present |
| `RadioGroup` hidden input | React does not render a group-level hidden input | Matched |
| `Radio.Root` root | `role="radio"` | Present |
| `Radio.Root` root | `aria-checked` | Present |
| `Radio.Root` root | `aria-disabled` for non-native disabled root | Present |
| `Radio.Root` root | `aria-required`, `aria-readonly`, `aria-invalid` | Present |
| `Radio.Root` root | `aria-labelledby` and `aria-describedby` | Present |
| `Radio.Root` root | roving `tabindex` in group | Present |
| `Radio.Root` root | `data-radio-item` in group | Present |
| `Radio.Root` root | state data attributes from `stateAttributesMapping` | Present |
| `Radio.Root` hidden input | `type="radio"`, `name`, `form`, `value`, `checked`, `disabled`, `required`, `readonly` | Present |
| `Radio.Root` hidden input | `tabindex="-1"`, `aria-hidden="true"`, visually hidden style | Present |
| `Radio.Root` hidden input | id only when non-native | Present |
| `Radio.Root` native button | explicit id on root button, no hidden input id, `type="button"` | Present |
| `Radio.Indicator` root | state data attributes from `stateAttributesMapping` | Present |
| `Radio.Indicator` root | `data-starting-style` and `data-ending-style` transition attributes | Present |
| `Radio.Indicator` root | no initial `data-starting-style` when first render is checked | Present |

## Browser Coverage Matrix

| State or behavior | Coverage |
| --- | --- |
| Click selection | Server and WASM Playwright |
| Selection changes | Server and WASM Playwright |
| Selected radio remains selected on repeated click | Server and WASM Playwright |
| Arrow next/previous | Server and WASM Playwright, no skips |
| Arrow wrap first/last | Server and WASM Playwright, no skips |
| Space selection | Server and WASM Playwright |
| Enter prevention/no selection | bUnit and JS path |
| Disabled group interaction blocking | Server and WASM Playwright |
| Disabled data attributes | Server and WASM Playwright |
| Disabled aria attributes | Server and WASM Playwright |
| Read-only interaction blocking | Server and WASM Playwright |
| Value change callback | Server and WASM Playwright |
| Controlled external state | Server and WASM Playwright |
| Focus and blur state | Server and WASM Playwright |
| Native form submission data | Server and WASM Playwright |
| Empty form submission state | Server and WASM Playwright |
| Native button mode | Server and WASM Playwright |
| Indicator mount/transition behavior | bUnit |
