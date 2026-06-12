# Checkbox Group

Checkbox Group manages a collection of related checkbox values and can coordinate parent selection.

## Import

```razor
@using Blazix.BaseUI.CheckboxGroup
@using Blazix.BaseUI.Checkbox
```

## Anatomy

```razor
<CheckboxGroup DefaultValue="@(["docs"])">
    <CheckboxRoot Value="docs"><CheckboxIndicator>x</CheckboxIndicator></CheckboxRoot>
    <CheckboxRoot Value="tests"><CheckboxIndicator>x</CheckboxIndicator></CheckboxRoot>
</CheckboxGroup>
```

## Notes

- Child checkboxes should provide `Value`.
- `AllValues` enables a parent checkbox to toggle all values.
- Group `Disabled` state cascades to children.
