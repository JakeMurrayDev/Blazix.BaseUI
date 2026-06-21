# Forms

A guide to using Blazix.BaseUI components in forms.

Rendered docs: `/handbook/forms`

## Building form fields

Compose a field from `FieldRoot` + `FieldLabel` + `FieldControl<TValue>` + `FieldDescription` + `FieldError`. The root wires label/description/`aria-describedby` and exposes state attributes (`data-valid`, `data-invalid`, `data-touched`, `data-dirty`, `data-filled`, `data-focused`).

## Naming form controls

Give each field a `Name`; `FieldRoot.Name` takes precedence over `FieldControl.Name`.

## Labeling control groups

Group controls with `FieldsetRoot` + one `FieldsetLegend`; `Disabled` on the root disables the group.

## Submitting data

Read the bound `Model`, or use `OnFormSubmit` to receive `FormSubmitEventArgs.Values` keyed by field name.

## Validation

`Form` validates on submit by default (`ValidationMode`: `OnSubmit` / `OnBlur` / `OnChange`); `FieldError` renders messages.

- **Native constraint validation** — native HTML attributes (`required`, `pattern`, …) surfaced via `FieldError`.
- **Server-side errors** — pass `Errors` (keyed by field name).

## Blazor integration

`Form` builds and cascades an `EditContext` from `Model`. Add a `DataAnnotationsValidator` and annotate your model to validate with data annotations; branch on `OnValidSubmit` / `OnInvalidSubmit`.
