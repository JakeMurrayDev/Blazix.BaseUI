# Switch

A control that indicates whether a setting is on or off.

Rendered docs: `/components/switch`

## Usage guidelines

- Switch controls need an accessible name. Use a wrapping `label`, sibling label with `NativeButton`, or Field integration.

## Anatomy

```razor
@using Blazix.BaseUI.Switch

<SwitchRoot>
    <SwitchThumb />
</SwitchRoot>
```

## Examples

### Labeling a switch

```razor
<label>
    <SwitchRoot />
    Notifications
</label>
```

### Rendering as a native button

```razor
<div>
    <label for="notifications-switch">Notifications</label>
    <SwitchRoot id="notifications-switch" NativeButton="true">
        <SwitchThumb />
    </SwitchRoot>
</div>
```

### Form integration

```razor
<Form Model="@model">
    <FieldRoot Name="notifications">
        <FieldLabel>
            <SwitchRoot />
            Notifications
        </FieldLabel>
    </FieldRoot>
</Form>
```

## API reference

Parts: `SwitchRoot` and `SwitchThumb`.

Root exposes controlled and uncontrolled checked state, disabled/read-only/required state, form parameters, `UncheckedValue`, `NativeButton`, binding callbacks, and render/class/style hooks. Root and Thumb expose checked, unchecked, disabled, read-only, required, and Field state data attributes.
