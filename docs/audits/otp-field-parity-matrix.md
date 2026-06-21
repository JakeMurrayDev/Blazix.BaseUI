# OTP Field Parity Matrix

Date: 2026-06-21

## Source to Blazor Mapping

| React source item | React responsibility | Blazor equivalent | Verification |
| --- | --- | --- | --- |
| `OTPFieldRoot` | Root state owner; renders group and hidden input. | `OtpFieldRoot.razor`, `OtpFieldRoot.cs`, `OtpFieldRootState.cs`. | bUnit root tests; Playwright initial attributes; docs/demo build. |
| `OTPFieldInput` | Individual slot input with render-order index and event handling. | `OtpFieldInput.razor`, `OtpFieldInput.cs`, `OtpFieldInputState.cs`. | bUnit input tests; Playwright keyboard/paste/readonly tests. |
| `OTPFieldRootContext` | Passes active index, value, state, callbacks, validation config, labels, and form flags to slots. | `OtpFieldRootContext.cs` cascaded by root. | bUnit attribute and label tests. |
| `getOTPFieldInputState` | Converts root state to slot state. | `OtpFieldInputState.FromRoot`. | bUnit state/data-attribute assertions. |
| `otp.ts` `stripOTPWhitespace` | Remove whitespace from nullable values. | `OtpFieldUtilities.StripWhitespace`. | `OtpFieldUtilitiesTests`. |
| `otp.ts` `normalizeOTPValueWithDetails` | Strip, validate, custom-normalize, revalidate, clamp, report rejection. | `OtpFieldUtilities.NormalizeWithDetails`. | bUnit utility/root tests; Playwright invalid typing and paste tests. |
| `otp.ts` `normalizeOTPValue` | Return normalized value only. | `OtpFieldUtilities.Normalize`. | bUnit utility/root tests. |
| `otp.ts` `replaceOTPValue` | Replace from slot index while preserving suffix. | `OtpFieldUtilities.ReplaceValue`. | bUnit utilities; Playwright paste from slot behavior. |
| `otp.ts` `removeOTPCharacter` | Remove one character if in bounds. | `OtpFieldUtilities.RemoveCharacter`. | bUnit utilities; Playwright keyboard deletion. |
| `stateAttributesMapping.ts` root mapping | Suppress `value`/`length`; emit Field validity attrs. | `OtpFieldAttributeUtilities.AddRootStateAttributes`. | bUnit root complete/Field tests. |
| `stateAttributesMapping.ts` input mapping | Suppress `value`/`index`; emit Field validity attrs. | `OtpFieldAttributeUtilities.AddInputStateAttributes`. | bUnit input/root tests; Playwright disabled/read-only tests. |
| `OTPFieldRootDataAttributes` | Root `data-*` contract. | `OtpFieldAttributeUtilities.AddRootStateAttributes`. | bUnit and Playwright assertions. |
| `OTPFieldInputDataAttributes` | Slot `data-*` contract. | `OtpFieldAttributeUtilities.AddInputStateAttributes`. | bUnit and Playwright assertions. |
| `useControlled` | Controlled/uncontrolled value behavior. | `SetParametersAsync`, `IsControlled`, `currentValue`, `Value`, `DefaultValue`, `ValueChanged`. | bUnit controlled test. |
| `useValueChanged` | Clear errors, update Field, validation, completion, focus after value changes. | `SetValueAsync`, `ResolvePendingEffects`, `OnAfterRenderAsync`. | bUnit callbacks; Playwright typing/paste/auto-submit. |
| Controlled render synchronization | React controlled render updates visible slot values from state. | JS `initialize`/`update` `syncInputs(root, hiddenInput.value)` keeps DOM values aligned with the hidden input/current state. | Playwright controlled parameter test; in-app browser verification. |
| `useIsoLayoutEffect` | Update Field filled state after value changes. | Lifecycle state updates and `FieldContext.SetFilled`. | Field bUnit tests. |
| `useValueAsRef` | Read latest value inside stable handlers. | `CurrentValue` computed property. | Controlled bUnit test; pending effect behavior. |
| `useStableCallback` | Stable event handlers. | Instance methods with persistent component fields. | Build and tests. |
| `useFieldRootContext` | Field disabled, name, state, validation, focused, touched, dirty, filled. | `FieldRootContext` cascading parameter and field registration. | Field bUnit tests. |
| `useFormContext` | Clear form errors. | `FormContext.ClearErrors`. | Code audit; Field/Form integration. |
| `useLabelableContext` | Label id and description id integration. | `LabelableContext` cascading parameter. | Field label/description bUnit test. |
| `useAriaLabelledBy` | Label first slot and group correctly. | `ResolvedAriaLabelledBy`, `InputAriaLabelledBy`, input filtering. | Field label/description bUnit test. |
| `useLabelableId` | Generate first input id when needed. | `ResolvedFirstInputId`, `GetInputId`. | bUnit id tests. |
| `useRegisterFieldControl` | Register first visible slot as Field control. | `RegisterFieldControl`. | Field bUnit test and code audit. |
| `CompositeList` | Track input list and count. | `inputs` list plus registration/unregistration. | bUnit grouped/order tests; input-count warning code audit. |
| `useCompositeListItem` | Guess index from render order. | `OtpFieldRootContext.RegisterInput`. | bUnit render-order and grouped layout coverage. |
| `useDirection` | RTL-aware horizontal navigation. | `DirectionProviderContext` and JS computed direction. | Code audit; keyboard path uses direction. |
| `useRenderElement` | Render replacement and state-based class/style. | Shared `RenderElement<TState>`. | Build and docs/demo usage. |
| `visuallyHidden`, `visuallyHiddenInput` | Hidden input styles. | `HiddenInputStyleWithoutName`, `HiddenInputStyleWithName`. | bUnit hidden input assertions; code audit. |
| `ownerDocument` | Resolve external form by id. | JS `requestSubmit`. | Playwright auto-submit; code audit. |
| `contains` | Detect blur inside root. | JS `root.contains(event.relatedTarget)`. | Code audit; focus/blur Playwright paths. |
| `stopEvent` | Prevent default and stop propagation for keyboard navigation/mutation. | JS `stopEvent`. | Playwright keyboard tests. |
| `warn` / `SafeReact` length warning | Warn invalid length or input count mismatch. | `ILogger<OtpFieldRoot>` warnings. | Code audit. |
| `warn` first-slot `aria-label` | Warn when ignored first-slot label lacks a real label. | JS one-time console warning using internal marker and `input.labels`. | Code audit. |
| Clipboard warning | Warn when paste text cannot be read. | JS `console.warn` catch branch. | Code audit. |

## Attribute Matrix

| Element | React attribute | Blazor status |
| --- | --- | --- |
| Root | `role="group"` | Implemented. |
| Root | `aria-describedby` | Implemented with caller + Field description merge. |
| Root | `aria-labelledby` | Implemented with caller/labelable merge. |
| Root | `data-complete` | Implemented. |
| Root | `data-disabled` | Implemented. |
| Root | `data-readonly` | Implemented. |
| Root | `data-required` | Implemented. |
| Root | `data-valid` | Implemented. |
| Root | `data-invalid` | Implemented. |
| Root | `data-touched` | Implemented. |
| Root | `data-dirty` | Implemented. |
| Root | `data-filled` | Implemented. |
| Root | `data-focused` | Implemented. |
| Hidden input | `type`, `id`, `form`, `name`, `value` | Implemented. |
| Hidden input | `autocomplete`, `inputmode`, `minlength`, `maxlength`, `pattern` | Implemented. |
| Hidden input | `disabled`, `readonly`, `required` | Implemented. |
| Hidden input | `aria-hidden`, `tabindex`, visually hidden `style` | Implemented. |
| Input | `id`, `value`, `type`, `inputmode`, `autocomplete` | Implemented. |
| Input | `autocorrect`, `spellcheck`, `enterkeyhint`, first-only `maxlength`, `tabindex` | Implemented. |
| Input | `disabled`, `form`, `pattern`, `readonly`, `required` | Implemented. |
| Input | `aria-labelledby`, `aria-invalid`, `aria-label` | Implemented. |
| Input | all React input `data-*` state attributes | Implemented. |

## Event Matrix

| React event reason | React source branch | Blazor equivalent | Verification |
| --- | --- | --- | --- |
| `input-change` | Slot input, hidden input, autofill. | `HandleSlotInputAsync`, `HandleHiddenInputChangeAsync`, `CommitSlotInputAsync`, `CommitHiddenInputAsync`. | bUnit root events; Playwright typing and hidden autofill. |
| `input-clear` | Empty slot input removes a character. | `CommitSlotInputAsync` with empty raw value. | bUnit event path; code audit. |
| `input-paste` | Pasted text replaces from slot. | JS paste handler + `HandleSlotPasteAsync`. | Playwright paste. |
| `keyboard` | Delete, Backspace, Ctrl/Cmd+Backspace. | JS keydown handler + `HandleSlotKeyDownAsync`. | Playwright keyboard deletion. |
| completion `input-change` | Accepted input reaches length. | Pending completion with `OtpFieldCompleteReason.InputChange`. | bUnit callback; Playwright typing completion. |
| completion `input-paste` | Accepted paste reaches length, including complete-to-complete paste. | Pending completion with `OtpFieldCompleteReason.InputPaste`. | Playwright paste completion; code audit. |
| invalid `input-change` | Input or hidden autofill rejects characters. | `ReportValueInvalidAsync(... InputChange)`. | bUnit and Playwright invalid typing. |
| invalid `input-paste` | Pasted text rejects characters. | `ReportValueInvalidAsync(... InputPaste)`. | Playwright paste. |
