# Fieldset

Groups related form controls with an accessible legend.

Rendered docs: `/components/fieldset`

## Usage guidelines

- Use one `FieldsetLegend` to label the group.
- Use `Disabled` on the root when the whole group should stop accepting input.
- Compose Field parts inside the fieldset so labels, descriptions, and errors stay local to each control.

## Anatomy

```razor
@using Blazix.BaseUI.Fieldset

<FieldsetRoot>
    <FieldsetLegend>Billing details</FieldsetLegend>
</FieldsetRoot>
```

## API reference

### Root

Groups related controls and associates them with the registered legend through `aria-labelledby`. `data-disabled` is present when disabled.

### Legend

Labels the fieldset, renders a `div` by default, and supplies the id used by the root.
