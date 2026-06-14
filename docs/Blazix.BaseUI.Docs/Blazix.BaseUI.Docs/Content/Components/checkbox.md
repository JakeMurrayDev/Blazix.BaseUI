# Checkbox

An easily stylable checkbox component with form integration and mixed-state support.

Rendered docs: `/components/checkbox`

## Usage guidelines

- Form controls need an accessible name. Wrap the checkbox in a `<label>`, or use Field components for richer form layouts.
- Use `NativeButton="true"` for sibling-label patterns where the visible control should own the id.

## Anatomy

```razor
@using Blazix.BaseUI.Checkbox

<CheckboxRoot>
    <CheckboxIndicator />
</CheckboxRoot>
```

## Examples

### Labeling a checkbox

```razor
@using Blazix.BaseUI.Checkbox

<label>
    <CheckboxRoot>
        <CheckboxIndicator />
    </CheckboxRoot>
    Accept terms and conditions
</label>
```

### Rendering as a native button

```razor
@using Blazix.BaseUI.Checkbox

<div>
    <label for="notifications-checkbox">Enable notifications</label>
    <CheckboxRoot id="notifications-checkbox" NativeButton="true">
        <CheckboxIndicator />
    </CheckboxRoot>
</div>
```

### Form integration

```razor
@using Blazix.BaseUI.Checkbox
@using Blazix.BaseUI.Field
@using Blazix.BaseUI.Form

<Form Model="@model">
    <FieldRoot Name="stayLoggedIn">
        <FieldLabel>
            <CheckboxRoot />
            Stay logged in for 7 days
        </FieldLabel>
    </FieldRoot>
</Form>
```

## API reference

### Root

Renders a hidden checkbox input plus a visible root element. Use `Checked`/`CheckedChanged` for controlled state, `DefaultChecked` for uncontrolled state, and `Indeterminate` for mixed state.

Data attributes include `data-checked`, `data-unchecked`, `data-indeterminate`, `data-disabled`, `data-readonly`, `data-required`, and field-state attributes.

### Indicator

Renders the visual check or mixed-state mark. Use `KeepMounted` when exit animations need the indicator to remain in the DOM.
