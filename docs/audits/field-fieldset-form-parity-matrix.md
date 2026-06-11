# Field, Fieldset, and Form Parity Matrix

Date: 2026-05-28

React source audited:

- `.base-ui/packages/react/src/field`
- `.base-ui/packages/react/src/fieldset`
- `.base-ui/packages/react/src/form`
- `.base-ui/packages/react/src/internals/field-register-control`
- `.base-ui/packages/react/src/internals/labelable-provider`
- `.base-ui/packages/react/src/internals/form-context`
- `.base-ui/packages/react/src/internals/field-constants`

## Hook and Utility Matrix

| React hook/utility | React responsibility | Blazor equivalent | Verification |
| --- | --- | --- | --- |
| `useRenderElement` | Default element rendering, render replacement, state attributes, merged props. | `RenderElement<TState>` with merged attributes, render replacement, `PreventDefaultOnSubmit`, and controlled input update association. | Build, bUnit custom render, Playwright form/field slice |
| `fieldValidityMapping` | Emits presence-only field state data attributes. | `FieldAttributeUtilities.AddFieldStateAttributes`. | `DoesNotRenderFalseStateDataAttributes`, 140 Playwright field/form checks |
| `getCombinedFieldValidityData` | Combines external invalid/form-error state into validity data. | `FieldAttributeUtilities.GetCombinedValidityData`. | Field/Form bUnit and Playwright invalid-submit/focus checks |
| `LabelableProvider` | Tracks control id, label id, and message ids. | `LabelableContext` created by `FieldRoot`. | Description/error aria-describedby tests |
| `useLabel` / labelable label id registration | Registers label id and associates control. | `FieldLabel` id registration and native/non-native label handling. | Field label Playwright coverage |
| `useFieldControlRegistration` | Owns active control registration, effective name fallback, initial value, registered validity, and form registry refresh. | `FieldControlRegistration`, `FieldRoot.RegisterFieldControl`, dynamic `EffectiveName`, and stable `FieldRegistry` registration. | `UsesFieldControlNameFallbackForErrors`, native-required Playwright test |
| `useRegisterFieldControl` | Registers/unregisters a control with root from the control side. | `FieldControl.RegisterFieldControl` in initialization/parameter update/dispose. | Form name fallback and all Field Playwright checks |
| `useFieldValidation` | Native validity read, custom validation, async stale-commit guard, debounce, revalidation, custom validity sync. | `FieldValidation` plus `FieldControl.GetNativeValidityAsync`, `SetCustomValidityAsync`, and JS field module. | Native-required, validation mode, async validation, and Field.Validity tests |
| `isOnlyValueMissing` | Suppresses required-only invalid state until dirty. | `FieldValidation.IsOnlyValueMissing` with submit-time dirty marking. | `NativeRequiredValidatesOnSubmit` and value-missing Playwright paths |
| `useTimeout` | Debounces validation. | `Timer` in `FieldValidation.CommitDebounced`. | Validation-on-change Playwright checks |
| `useTransitionStatus` | Adds `starting`/`ending` transition status for conditional parts. | `FieldError` and `FieldValidity` transition status state. | FieldError bUnit and Playwright visibility checks |
| `useOpenChangeComplete` | Keeps the exiting error mounted until completion. | `FieldError` stores last rendered errors and clears mount after ending render. | FieldError Playwright and bUnit rendering checks |
| `FormContext` | Shared errors, registry, validation mode, submit-attempt ref, clear-errors callback. | `FormContext` and `FieldRegistry`. | Form Playwright `OnFormSubmit`, invalid submit, actionsRef tests |
| `Form` submit handler | Validates registered fields, focuses first invalid, submits values when valid. | `Form.HandleSubmitAsync`, `FieldRegistry.ValidateAllAsync`, first-invalid focus. | Form Playwright 18/18 across Server/WASM |
| `FieldsetRootContext` | Shares disabled state and legend id. | `FieldsetRootContext` with stable context object. | Fieldset bUnit tests |
| `useBaseUiId` | Explicit or generated ids. | `AttributeUtilities.GetIdOrDefault`. | Field label/description/error and fieldset legend tests |
| Label mousedown utility | Prevents text selection and focuses non-native label target. | `blazor-baseui-field.js` `addLabelMouseDownListener`. | JS syntax check and label Playwright coverage |
| Focus utility | Focuses registered invalid field/control. | `blazor-baseui-field.js` `focusElement`, `FieldControl.FocusAsync`, `Form.FocusFirstInvalidFieldAsync`. | Form invalid-submit Playwright tests |

## Component Attribute Matrix

| Component | React attributes/data/ARIA | Blazor equivalent |
| --- | --- | --- |
| `Field.Root` | `data-disabled`, `data-valid`, `data-invalid`, `data-touched`, `data-dirty`, `data-filled`, `data-focused`. | Same presence-only attributes through `FieldAttributeUtilities`. |
| `Field.Control` | `id`, effective `name`, `disabled`, `autofocus`, `aria-labelledby`, combined `aria-describedby`, `aria-invalid`, field data attributes, input/focus/blur/key handlers. | Same computed attributes; explicit ARIA is preserved; `value` updates are associated with `oninput`. |
| `Field.Label` | `id`, generated `for` for native labels, field data attributes, mousedown focus handling. | Same attributes; explicit `for` preserved; JS handles non-native focus behavior. |
| `Field.Description` | `id`, field data attributes, message-id registration. | Same attributes and registration with id-change cleanup. |
| `Field.Error` | `id`, field data attributes, `data-starting-style`, `data-ending-style`, default error content. | Same attributes; form errors and multiple errors render with React-equivalent precedence. |
| `Field.Item` | Field state data attributes and disabled state. | Same presence-only state hooks with fieldset-disabled propagation. |
| `Field.Validity` | Render prop receives validity data and transition status. | `RenderFragment<FieldValidityRenderState>` receives state/errors/error/value/initialValue/transition status. |
| `Fieldset.Root` | `fieldset`, `aria-labelledby`, `data-disabled`; no native `disabled` attribute from Base UI disabled state. | Same. |
| `Fieldset.Legend` | `div`, `id`, `data-disabled`. | Same. |
| `Form` | `form`, default `novalidate`, overridable `noValidate={false}`, submit handler, render replacement. | Same through `NoValidate`, form attributes, and `RenderElement`. |

## Behavior Matrix

| Behavior | React source | Blazor repair |
| --- | --- | --- |
| Root-name precedence | `effectiveName = name ?? registeredFieldName`; control receives root name. | `FieldRoot.EffectiveName` and `FieldControl.ResolvedName` use root name first. |
| Control-name fallback | Registered control name becomes effective field name when root name is absent. | Control registration stores name; form errors and values use fallback. |
| Form values in validate | `validate(value, formValues)` receives registry values. | `ValidateWithFormValues` and `GetFormValues()` pass registered values. |
| Submit required validation | Registered validator sets `markedDirtyRef.current = true` before commit. | `ValidateRegisteredControlAsync` sets `markedDirty = true` before commit. |
| Form error precedence | Default `Field.Error` renders form errors; specific matches do not. | `FieldError` match logic mirrors that precedence. |
| Multiple errors | React renders a `ul` for multiple errors. | Blazor renders `ul > li` for multiple errors. |
| External ARIA | React element props override generated defaults. | Attribute builders skip generated labels/descriptions when user values exist or combine id refs where React does. |
| No model requirement | React has no model/EditContext. | `Form` creates a fallback `EditContext(new object())`. |
| Native noValidate override | React defaults `noValidate=true` and allows false. | `Form.NoValidate` defaults true and can be disabled. |
| Controlled input updates | React input state is native DOM-first during input. | `RenderElement` can associate `value` updates with `oninput` for `FieldControl`. |
