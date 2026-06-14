# Field

Provides labeling, descriptions, validation messages, and state hooks for form controls.

Rendered docs: `/components/field`

## Usage guidelines

- Use `FieldLabel` for the visible label and let Field wire the control id.
- Put helper text in `FieldDescription` so it is included in `aria-describedby`.
- Use `FieldError` for native, custom, Blazor, or form-level validation messages.

## Anatomy

```razor
@using Blazix.BaseUI.Field

<FieldRoot>
    <FieldItem>
        <FieldLabel>Name</FieldLabel>
        <FieldControl TValue="string" />
    </FieldItem>
    <FieldDescription>Visible on your profile.</FieldDescription>
    <FieldError />
    <FieldValidity>
        <span>@context.Error</span>
    </FieldValidity>
</FieldRoot>
```

## API reference

### Root

Provides labeling, validation, and state context for a single form field. The root exposes `data-disabled`, `data-valid`, `data-invalid`, `data-touched`, `data-dirty`, `data-filled`, and `data-focused`.

### Control

Renders the labelable control, usually an input. `FieldRoot.Name` takes precedence over `FieldControl<TValue>.Name` for form values and external errors.

### Label

Labels the control and registers its id with the field.

### Description

Adds helper text to the control's `aria-describedby` relationship.

### Item

Groups a label-control pair and mirrors field state for styling.

### Error

Renders current validation messages, specific native validity matches, or always-visible custom content.

### Validity

Renders child content with `FieldValidityRenderState` and does not add an element of its own.
