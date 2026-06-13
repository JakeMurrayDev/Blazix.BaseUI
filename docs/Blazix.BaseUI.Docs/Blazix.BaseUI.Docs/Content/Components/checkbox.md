# Checkbox

Checkbox renders a binary or mixed-state input with hidden form input support.

## Import

```razor
@using Blazix.BaseUI.Checkbox
```

## Anatomy

```razor
<CheckboxRoot DefaultChecked="true">
    <CheckboxIndicator>x</CheckboxIndicator>
</CheckboxRoot>
```

## Notes

- `Checked` and `CheckedChanged` make the checkbox controlled.
- `Indeterminate` represents a mixed state.
- `Name` and `Value` participate in form submission.
