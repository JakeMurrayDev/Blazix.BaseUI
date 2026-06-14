# Checkbox Group

Provides shared state to a series of checkboxes.

Rendered docs: `/components/checkbox-group`

## Usage guidelines

- Label the group with `aria-labelledby`, Fieldset, or another visible grouping label.
- Child checkboxes should provide `Value` so the group can track selected values.

## Anatomy

```razor
@using Blazix.BaseUI.Checkbox
@using Blazix.BaseUI.CheckboxGroup

<CheckboxGroup>
    <CheckboxRoot Value="http">
        <CheckboxIndicator />
    </CheckboxRoot>
</CheckboxGroup>
```

## Examples

### Labeling a checkbox group

```razor
@using Blazix.BaseUI.Checkbox
@using Blazix.BaseUI.CheckboxGroup

<div id="protocols-label">Allowed network protocols</div>
<CheckboxGroup aria-labelledby="protocols-label">
    <label>
        <CheckboxRoot Value="http" />
        HTTP
    </label>
</CheckboxGroup>
```

### Parent checkbox

Make the group controlled, pass `AllValues`, and add `Parent="true"` to the controlling checkbox.

```razor
<CheckboxGroup Value="@selectedValues"
               ValueChanged="@(value => selectedValues = value)"
               AllValues="@allValues">
    <CheckboxRoot Parent="true" />
    <CheckboxRoot Value="api" />
    <CheckboxRoot Value="docs" />
</CheckboxGroup>
```

### Nested parent checkbox

Nested groups can coordinate parent state by updating the outer group when the inner group becomes fully selected or partially selected.

## API reference

### CheckboxGroup

Provides shared state to child `CheckboxRoot` controls. Use `Value`/`ValueChanged` for controlled state, `DefaultValue` for uncontrolled state, and `AllValues` for parent checkbox support.

Data attributes include `data-disabled`, `data-valid`, `data-invalid`, `data-touched`, `data-dirty`, `data-filled`, and `data-focused`.
