# Field, Fieldset, and Form Functional Audit

Date: 2026-05-28

Components: `Field.Root`, `Field.Control`, `Field.Label`, `Field.Description`, `Field.Error`, `Field.Item`, `Field.Validity`, `Fieldset.Root`, `Fieldset.Legend`, `Form`

React source audited: `.base-ui/packages/react/src/field`, `.base-ui/packages/react/src/fieldset`, `.base-ui/packages/react/src/form`, and supporting internals.

Blazor source audited: `src/BlazorBaseUI/Field`, `src/BlazorBaseUI/Fieldset`, `src/BlazorBaseUI/Form`, `src/BlazorBaseUI/RenderElement.razor`, and `src/BlazorBaseUI/wwwroot/blazor-baseui-field.js`.

Spec artifacts:

- `../base-ui-specs/field/SPEC.md`
- `../base-ui-specs/field/pitfalls.md`
- `../base-ui-specs/fieldset/SPEC.md`
- `../base-ui-specs/fieldset/pitfalls.md`
- `../base-ui-specs/form/SPEC.md`
- `../base-ui-specs/form/pitfalls.md`

Proof artifacts:

- `docs/audits/field-fieldset-form-parity-matrix.md`
- `docs/audits/logs/field-fieldset-form-dotnet-build.log`
- `docs/audits/logs/field-fieldset-form-bunit-tests.log`
- `docs/audits/logs/field-fieldset-form-playwright-tests.log`
- `docs/audits/logs/field-fieldset-form-js-syntax-check.log`
- `docs/audits/logs/field-fieldset-form-lint-rules.log`
- `docs/audits/logs/field-fieldset-form-git-diff-check.log`

Tool note: Serena and Context7 tools were not exposed in this session. The audit used the local React Base UI source, repository files, `rg`, and targeted shell inspection.

## React Parts Found

- Field: `FieldRoot`, `FieldControl`, `FieldLabel`, `FieldDescription`, `FieldError`, `FieldItem`, `FieldValidity`.
- Field internals: labelable provider, field root context, field control registration, field validation, field validity mapping, combined validity helper.
- Fieldset: `FieldsetRoot`, `FieldsetLegend`, fieldset context.
- Form: `Form`, form context, field registry, actions ref, submit handler.

## Resolved Gaps

| Gap | React source behavior | Blazor repair | Verification |
| --- | --- | --- | --- |
| Field data attributes emitted boolean values | React emits presence-only data attributes. | Added `FieldAttributeUtilities.AddFieldStateAttributes` and converted Field parts to presence-only hooks. | bUnit `DoesNotRenderFalseStateDataAttributes`, Playwright Field slice |
| Field.Control name precedence was inverted | Root name overrides control name. | `FieldControl.ResolvedName` now uses root name first. | bUnit and Playwright form name coverage |
| Control-name fallback was missing from form errors | Root effective name falls back to registered control name. | Added `FieldControlRegistration` and `FieldRoot.EffectiveName`. | bUnit `UsesFieldControlNameFallbackForErrors` |
| Native validity was incomplete | React reads DOM `ValidityState`, `validationMessage`, and sets custom validity. | Added `FieldNativeValiditySnapshot`, DOM validity JS calls, and custom validity sync. | Playwright `NativeRequiredValidatesOnSubmit`, JS syntax check |
| Submit validation did not mark required fields dirty | React registered validators mark dirty before commit. | `ValidateRegisteredControlAsync` marks dirty before validating. | Playwright native-required Server/WASM |
| Revalidate-on-change after submit was incomplete | React on-submit mode revalidates on change after submit attempt. | `FieldValidation.CommitAsync` supports revalidate-only flow and form submit-attempt state. | Playwright validation-on-submit/on-change scenarios |
| Async validation could overwrite newer commits | React ignores stale async results by commit id. | `FieldValidation` tracks validation commit ids. | Playwright async validation scenario |
| Form values were not passed to field validation | React passes current registry values to `validate(value, formValues)`. | Added `ValidateWithFormValues` and registry value collection. | Playwright `ValidateReceivesFormValues` |
| Field.Error specific match rendered form errors | React specific validity matches only inspect validity state. | `FieldError` match logic now separates `MatchValidity` from form errors. | bUnit `SpecificMatchDoesNotRenderForFormErrorOnly` |
| Field.Error multiple errors were not rendered as a list | React renders multiple error strings as `ul > li`. | `FieldError` renders multiple errors as a list. | bUnit `RendersMultipleFormErrorsAsList` |
| Error/description message ids could go stale | React effects clean up changed ids and unmounts. | Description and error parts unregister stale ids and dispose registrations. | bUnit aria-described-by checks |
| Explicit ARIA could be overwritten | React element props override generated default props. | Label/control builders preserve explicit `for`, `aria-labelledby`, and external descriptions. | bUnit `PreservesExternalAriaDescribedByOnControl` |
| Field.Validity did not expose the full React render state | React render prop receives validity data and transition status. | Added `FieldValidityRenderState` and combined validity rendering. | FieldValidity Playwright tests |
| Fieldset disabled rendered native `disabled` | React fieldset disabled state is context/state only. | Removed native `disabled`; retained `data-disabled` and context propagation. | bUnit `DisabledAddsStateHookWithoutNativeDisabledAttribute` |
| Fieldset data attributes emitted false values | React data attrs are presence-only. | Converted root and legend to presence-only `data-disabled`. | Fieldset bUnit |
| Form required a model/EditContext | React Form has no model requirement. | `Form` creates a fallback edit context when neither model nor edit context is supplied. | bUnit `RendersWithoutModel` |
| Form cached child render fragments | React recomputes render output; caching render fragments can stale state. | Removed cached child content and render directly through the Blazor render flow. | Build and Form tests |
| Form submit did not include registry validity and focus parity | React validates registry fields and focuses the first invalid field. | `Form.HandleSubmitAsync` validates all registered fields and focuses the first invalid field. | Form Playwright invalid-submit tests |
| Form `noValidate` was hard-coded | React defaults `noValidate=true` but allows false. | Added `Form.NoValidate` defaulting true and omitting `novalidate` when false. | bUnit `NoValidateCanBeDisabled` |
| Managed submit could allow browser navigation | Blazor-managed submit callbacks need default prevention. | Added `RenderElement.PreventDefaultOnSubmit` for the Blazor Form render path. | Playwright Form and Field submit suites |
| Controlled input replacement was unstable | React input value updates are DOM-first during input. | `RenderElement` supports explicit update-attribute association; `FieldControl` binds `value` to `oninput`. | Playwright `FilledChangesWhenValueChangedExternally` |

## Verification Commands

| Command | Result | Log |
| --- | --- | --- |
| `dotnet build BlazorBaseUI.slnx -v minimal` | Passed, 0 warnings, 0 errors | `docs/audits/logs/field-fieldset-form-dotnet-build.log` |
| `dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --no-build --filter "FullyQualifiedName~BlazorBaseUI.Tests.Field|FullyQualifiedName~BlazorBaseUI.Tests.Fieldset|FullyQualifiedName~BlazorBaseUI.Tests.Form" -v minimal` | Passed, 42/42 | `docs/audits/logs/field-fieldset-form-bunit-tests.log` |
| `dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --no-build --filter "FullyQualifiedName~FieldTests|FullyQualifiedName~FieldErrorTests|FullyQualifiedName~FieldValidityTests|FullyQualifiedName~FormTests" -v minimal` | Passed, 140/140 | `docs/audits/logs/field-fieldset-form-playwright-tests.log` |
| `node --check src/BlazorBaseUI/wwwroot/blazor-baseui-field.js` and `node --check src/BlazorBaseUI/wwwroot/blazor-baseui-field.min.js` | Passed | `docs/audits/logs/field-fieldset-form-js-syntax-check.log` |
| `bash scripts/lint-rules.sh` | Passed, 0 violations. macOS `grep -P` warnings are recorded before the zero-violation summary. | `docs/audits/logs/field-fieldset-form-lint-rules.log` |
| `git diff --check` | Passed, no whitespace errors | `docs/audits/logs/field-fieldset-form-git-diff-check.log` |

## Manual Checks

- Compared Field React root/control/label/description/error/item/validity source against Blazor components.
- Compared Fieldset React root/legend source against Blazor components.
- Compared Form React source and form context against Blazor form registry and submit flow.
- Confirmed all field data attributes are presence-only and applied consistently across root/control/label/description/error/item.
- Confirmed root-name precedence and control-name fallback for attributes, form values, and form errors.
- Confirmed native validity, custom validity, async validation, debounce, revalidation, and required-only dirty suppression are implemented or accounted for.
- Confirmed `Field.Error` default and specific-match rendering follows React precedence.
- Confirmed form-level errors participate in aria-invalid, data-invalid, submit blocking, and focus targeting.
- Confirmed `Field.Validity` receives combined validity data and transition status.
- Confirmed Fieldset disabled state propagates by context without a native fieldset `disabled` attribute.
- Confirmed Form no longer requires a model, supports `NoValidate=false`, validates registered fields, and focuses the first invalid registered field.
- Confirmed DOM-heavy validity, label focus, and control focus operations remain in the component-specific JS module.

## Final State

No remaining Field, Fieldset, or Form parity gaps were identified in the audited local React source after the repairs above. The relevant bUnit and Playwright suites pass with no skipped tests.
