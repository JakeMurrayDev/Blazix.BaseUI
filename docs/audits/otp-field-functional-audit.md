# OTP Field Functional Audit

Date: 2026-06-21

## Scope

Audited the Blazor `OtpFieldRoot` and `OtpFieldInput` implementation against the Base UI React OTP Field source and docs.

React source files:

- `.base-ui/packages/react/src/otp-field/root/OTPFieldRoot.tsx`
- `.base-ui/packages/react/src/otp-field/input/OTPFieldInput.tsx`
- `.base-ui/packages/react/src/otp-field/root/OTPFieldRootContext.ts`
- `.base-ui/packages/react/src/otp-field/utils/otp.ts`
- `.base-ui/packages/react/src/otp-field/utils/stateAttributesMapping.ts`
- `.base-ui/packages/react/src/otp-field/root/OTPFieldRootDataAttributes.ts`
- `.base-ui/packages/react/src/otp-field/input/OTPFieldInputDataAttributes.ts`
- `.base-ui/docs/src/app/(docs)/react/components/otp-field/page.mdx`
- `.base-ui/docs/src/app/(docs)/react/components/otp-field/types.md`

Blazor files audited and repaired:

- `src/Blazix.BaseUI/OtpField/*`
- `src/Blazix.BaseUI/wwwroot/blazix-baseui-otp-field.js`
- `src/Blazix.BaseUI/wwwroot/blazix-baseui-otp-field.min.js`
- `tests/Blazix.BaseUI.Tests/OtpField/*`
- `tests/Blazix.BaseUI.Playwright.Tests/Blazix.BaseUI.Playwright.Tests/Tests/OtpField/*`
- `docs/Blazix.BaseUI.Docs/**/otp-field*`
- `demo/Blazix.BaseUI.Demo/**/OtpField*`
- `../base-ui-specs/otp-field/*`

## Subagent Audit Inputs

Three independent audit streams were used:

- React source parity: mapped source hooks, utilities, attributes, event reasons, completion timing, and hidden input behavior.
- Blazor implementation pattern parity: compared against local Field, NumberField, Form, Labelable, RenderElement, and JS interop conventions.
- Documentation/demo/test integration: mapped docs routes, demo routes, API tables, render-mode test pages, and evidence placement.

## Resolved Gaps

| Area | Gap | Resolution |
| --- | --- | --- |
| Component surface | OTP Field was not a complete Blazor part set. | Added `OtpFieldRoot`, `OtpFieldInput`, states, context, event args, validation enum, utilities, and JS interop module. |
| Normalization | React normalization order and Unicode clamping were missing. | Added `OtpFieldUtilities` with whitespace stripping, built-in validation, custom normalization, second validation pass, and Rune-based clamping/replacement/removal. |
| Root attributes | Root role, ARIA merges, and state data attributes needed React parity. | Root now renders `role="group"`, merged `aria-describedby`, `aria-labelledby`, and all React root data attributes. |
| Input attributes | Slot-level native attributes and first-slot behavior were incomplete. | Inputs now render derived ids, value, type/mask override, input mode, autocomplete, autocorrect, spellcheck, enter key hint, first-only maxlength, tab index, disabled/read-only/required, pattern, aria-invalid, label inheritance, and all React input data attributes. |
| Hidden validation input | React renders a sibling hidden input with native form validity. | Root now renders a sibling hidden input with React-equivalent attributes, visual hiding, hidden focus forwarding, native validity, custom validity, and external form support. |
| Field integration | Label, description, disabled, name, dirty, touched, focused, filled, and validity integration were required. | Root integrates `FieldRootContext`, `LabelableContext`, and `FormContext`, registers the first slot as field control, updates field state, and clears form errors on accepted changes. |
| Event details | React change details expose cancel and propagation surface. | `OtpFieldValueChangeEventArgs` now supports `Cancel()`, `IsCanceled`, `AllowPropagation()`, and `IsPropagationAllowed`. |
| Completion timing | React completion fires after accepted value update, with stale controlled attempts ignored. | Blazor queues focus/completion and resolves after render only when the current value matches the pending value. |
| Auto-submit | React submits the owning or externally associated form after completion. | JS `requestSubmit` synchronizes visible and hidden values, checks native validity, reports validity, and submits via a temporary hidden submitter. |
| Browser event handling | Prevent-default and focus selection require DOM control. | Component-specific JS delegates mousedown, focusin, focusout, input, keydown, paste, hidden input, focus, native validity, custom validity, and submit behavior. |
| Hidden input double dispatch | Hidden input autofill could be processed by both JS and Blazor event systems. | JS hidden input handler now stops propagation after dispatching to .NET. |
| Read-only key branch | Same-character full-selection key could advance focus in read-only mode. | JS now exits non-navigation key handling early when read-only. |
| First slot `aria-label` warning | React warns when an ignored first-slot `aria-label` has no associated label. | Blazor marks the branch and JS warns once after checking the browser `labels` collection. |
| Controlled DOM value sync | Controlled parent updates changed hidden input state but visible slot DOM properties could remain stale. | JS `initialize` and `update` now synchronize visible slot values from the hidden input value; Playwright and in-app browser coverage verify the branch. |
| Docs | OTP Field docs route, API tables, and component content were absent. | Added docs nav, page, API metadata, content markdown, hero CSS/Tailwind demos, and demo CSS assets. |
| Demo | Demo app did not expose OTP Field. | Added server/client demo pages, showcase section, nav entry, and examples for basic, masked, controlled alphanumeric, disabled, and read-only states. |
| Source spec | No reusable OTP Field spec existed. | Added `../base-ui-specs/otp-field/SPEC.md` and `pitfalls.md`. |
| Tests | No Blazor OTP Field regression coverage existed. | Added bUnit utility/root/input tests and Server/WASM Playwright tests for attributes, typing, paste, keyboard, disabled, read-only, hidden autofill, and auto-submit. |

## Final Audit Result

Every React source utility, hook responsibility, attribute, ARIA attribute, data attribute, event reason, and documented behavior is implemented or mapped to a native Blazor/JS equivalent. DOM-sensitive behavior is kept in `blazix-baseui-otp-field.js`; state, normalization, Field/Form integration, and controlled/uncontrolled lifecycle behavior are implemented in C#.
