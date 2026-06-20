# Toggle Group

Toggle Group manages shared state and roving focus for a set of toggle buttons.

## Anatomy

```razor
@using Blazix.BaseUI.Toggle
@using Blazix.BaseUI.ToggleGroup

<ToggleGroup>
    <Toggle Value="bold">Bold</Toggle>
    <Toggle Value="italic">Italic</Toggle>
    <Toggle Value="underline">Underline</Toggle>
</ToggleGroup>
```

## Examples

### Multiple

Set `Multiple` when users can press several toggles at once.

```razor
<ToggleGroup Multiple="true" DefaultValue="@(["bold", "italic"])">
    <Toggle Value="bold">Bold</Toggle>
    <Toggle Value="italic">Italic</Toggle>
    <Toggle Value="underline">Underline</Toggle>
</ToggleGroup>
```

## Parts

Parts: `ToggleGroup` and child `Toggle` components.

Toggle Group supports controlled state with `Value`, uncontrolled state with `DefaultValue`, two-way binding with `ValueChanged`, cancellable changes with `OnValueChange`, `Orientation`, `LoopFocus`, and `Multiple`.

The group exposes `role="group"`, `data-orientation`, `data-disabled`, and `data-multiple`. It sets `aria-orientation` when it owns composite navigation outside Toolbar.
