# Radio

An easily stylable radio button for choosing one option from a group.

Rendered docs: `/components/radio`

## Usage guidelines

- Radio controls need an accessible name. Use an enclosing `label`, explicit ARIA labelling, or the Field and Fieldset components.
- Radio is always used with `RadioGroup` so one value is selected from the set.

## Anatomy

```razor
@using Blazix.BaseUI.Radio
@using Blazix.BaseUI.RadioGroup

<RadioGroup TValue="string">
    <RadioRoot TValue="string" Value="@("ssd")">
        <RadioIndicator />
    </RadioRoot>
</RadioGroup>
```

## Examples

### Labeling a radio group

```razor
<div id="storage-type-label">Storage type</div>

<RadioGroup TValue="string" aria-labelledby="storage-type-label">
    ...
</RadioGroup>
```

```razor
<label>
    <RadioRoot TValue="string" Value="@("ssd")" />
    SSD
</label>
```

### Rendering as a native button

```razor
<div id="storage-type">Storage type</div>

<RadioGroup TValue="string"
            DefaultValue="@("ssd")"
            aria-labelledby="storage-type">
    <div>
        <label for="storage-type-ssd">SSD</label>
        <RadioRoot TValue="string"
                   Value="@("ssd")"
                   id="storage-type-ssd"
                   NativeButton="true">
            <RadioIndicator />
        </RadioRoot>
    </div>
</RadioGroup>
```

```razor
@using Blazix.BaseUI
@using Blazix.BaseUI.Radio
@using Blazix.BaseUI.RadioGroup
@using Microsoft.AspNetCore.Components.Rendering

<div id="storage-type">Storage type</div>

<RadioGroup TValue="string"
            DefaultValue="@("ssd")"
            aria-labelledby="storage-type">
    <RadioRoot TValue="string"
               Value="@("ssd")"
               NativeButton="true"
               Render="@RenderWrappedButton" />
</RadioGroup>

@code {
    private RenderFragment<RenderProps<RadioRootState>> RenderWrappedButton => props => builder =>
    {
        builder.OpenElement(0, "label");
        builder.OpenElement(1, "button");
        builder.AddMultipleAttributes(2, props.Attributes);
        if (props.ElementReferenceCallback is not null)
        {
            builder.AddElementReferenceCapture(3, props.ElementReferenceCallback);
        }
        builder.CloseElement();
        builder.AddContent(4, "SSD");
        builder.CloseElement();
    };
}
```

### Form integration

```razor
@using Blazix.BaseUI.Field
@using Blazix.BaseUI.Fieldset
@using Blazix.BaseUI.Form
@using Blazix.BaseUI.Radio
@using Blazix.BaseUI.RadioGroup

<Form Model="@model">
    <FieldRoot Name="storageType">
        <FieldsetRoot Render="@RenderFieldsetAsRadioGroup">
            <FieldsetLegend>Storage type</FieldsetLegend>
            <FieldItem>
                <FieldLabel>
                    <RadioRoot TValue="string" Value="@("ssd")" />
                    SSD
                </FieldLabel>
            </FieldItem>
            <FieldItem>
                <FieldLabel>
                    <RadioRoot TValue="string" Value="@("hdd")" />
                    HDD
                </FieldLabel>
            </FieldItem>
        </FieldsetRoot>
    </FieldRoot>
</Form>

@code {
    private readonly object model = new();

    private RenderFragment<RenderProps<FieldsetRootState>> RenderFieldsetAsRadioGroup => props =>
        RenderUtilities.CreateComponent(typeof(RadioGroup<string>), props);
}
```

## API reference

### RadioGroup

Provides shared single-selection state to a set of RadioRoot items.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Value` | `TValue?` | `null` | Controlled selected value. Supplying `Value` makes the group controlled. |
| `DefaultValue` | `TValue?` | `null` | Initially selected value for uncontrolled usage. |
| `Disabled` | `bool` | `false` | Disables every radio in the group. |
| `ReadOnly` | `bool` | `false` | Prevents user changes while keeping radios focusable. |
| `Required` | `bool` | `false` | Requires a selected value for form submission. |
| `Name` | `string?` | `null` | Field name applied to child radio inputs. |
| `Form` | `string?` | `null` | Owning form id for child radio inputs. |
| `ValueChanged` | `EventCallback<TValue?>` | `default` | Two-way binding callback. |
| `OnValueChange` | `EventCallback<RadioGroupValueChangeEventArgs<TValue>>` | `default` | Pre-commit value-change callback; call `Cancel()` to prevent the update. |
| `Render` | `RenderFragment<RenderProps<RadioGroupState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<RadioGroupState, string?>?` | `null` | Returns CSS classes based on group state. |
| `StyleValue` | `Func<RadioGroupState, string?>?` | `null` | Returns inline styles based on group state. |
| `ChildContent` | `RenderFragment?` | `null` | RadioRoot items. |

### Root

Represents one radio button and renders a hidden sibling `input type="radio"` for native forms.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Value` | `TValue` | `required` | Unique identifying value in the group. |
| `Disabled` | `bool` | `false` | Disables this radio. |
| `ReadOnly` | `bool` | `false` | Prevents this radio from being selected by the user. |
| `Required` | `bool` | `false` | Requires a selected value for form submission. |
| `NativeButton` | `bool` | `false` | Renders the visible radio as a native button. |
| `Name` | `string?` | `null` | Field name for the hidden input. |
| `Render` | `RenderFragment<RenderProps<RadioRootState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<RadioRootState, string?>?` | `null` | Returns CSS classes based on radio state. |
| `StyleValue` | `Func<RadioRootState, string?>?` | `null` | Returns inline styles based on radio state. |
| `ChildContent` | `RenderFragment?` | `null` | Indicator or custom visual contents. |

### Indicator

Indicates whether the radio is selected. It unmounts when unchecked unless `KeepMounted` is true.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `KeepMounted` | `bool` | `false` | Keeps the indicator in the DOM when unchecked. |
| `Render` | `RenderFragment<RenderProps<RadioIndicatorState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<RadioIndicatorState, string?>?` | `null` | Returns CSS classes based on indicator state. |
| `StyleValue` | `Func<RadioIndicatorState, string?>?` | `null` | Returns inline styles based on indicator state. |
| `ChildContent` | `RenderFragment?` | `null` | Indicator visual contents. |

RadioGroup exposes `data-disabled`, `data-readonly`, `data-required`, `data-valid`, `data-invalid`, `data-touched`, `data-dirty`, `data-filled`, and `data-focused`. Root and Indicator also expose `data-checked` and `data-unchecked`; Root adds `data-radio-item` in a group, and Indicator adds `data-starting-style` / `data-ending-style` during transitions.
