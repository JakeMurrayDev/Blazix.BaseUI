# Select

A common form component for choosing a predefined value in a dropdown menu.

Rendered docs: `/components/select`

## Usage guidelines

- Prefer Autocomplete for large filterable lists.
- The popup aligns the selected item with the trigger by default. Disable that with `AlignItemWithTrigger="false"` on `SelectPositioner`.
- Select needs an accessible name. Prefer `SelectLabel` or an ARIA label on the trigger.

## Anatomy

```razor
@using Blazix.BaseUI.Select

<SelectRoot TValue="string">
    <SelectLabel />
    <SelectTrigger>
        <SelectValue TValue="string" />
        <SelectIcon />
    </SelectTrigger>

    <SelectPortal>
        <SelectBackdrop />
        <SelectPositioner>
            <SelectPopup>
                <SelectScrollUpArrow />
                <SelectArrow />
                <SelectList>
                    <SelectItem TValue="string">
                        <SelectItemText />
                        <SelectItemIndicator />
                    </SelectItem>
                    <SelectSeparator />
                    <SelectGroup>
                        <SelectGroupLabel />
                    </SelectGroup>
                </SelectList>
                <SelectScrollDownArrow />
            </SelectPopup>
        </SelectPositioner>
    </SelectPortal>
</SelectRoot>
```

## Examples

### Typed wrapper component

```razor
@typeparam TValue

<SelectRoot TValue="TValue" @attributes="AdditionalAttributes">
    @ChildContent
</SelectRoot>
```

### Formatting the value

Use `Items` for label lookup or `ValueContent` for custom display.

```razor
<SelectRoot TValue="string" Items="@ThemeItems">
    <SelectValue TValue="string" Placeholder="Select theme" />
</SelectRoot>
```

### Labeling and placeholders

```razor
<SelectRoot TValue="string">
    <SelectLabel>Theme</SelectLabel>
    <SelectTrigger>
        <SelectValue TValue="string" Placeholder="Select theme" />
    </SelectTrigger>
</SelectRoot>
```

### Multiple selection, object values, grouped options

Use `Multiple` with `DefaultValues` or `Values`, object values with `ItemToStringValue` and `IsItemEqualToValue`, and grouped options with `ItemGroups`.

## API reference

Parts: `SelectRoot<TValue>`, `SelectLabel`, `SelectTrigger`, `SelectValue<TValue>`, `SelectIcon`, `SelectBackdrop`, `SelectPortal`, `SelectPositioner`, `SelectPopup`, `SelectList`, `SelectArrow`, `SelectItem<TValue>`, `SelectItemText`, `SelectItemIndicator`, `SelectGroup`, `SelectGroupLabel`, `SelectScrollUpArrow`, `SelectScrollDownArrow`, and `SelectSeparator`.

Root exposes controlled and uncontrolled value/open parameters, form serialization parameters, static `Items`/`ItemGroups`, callbacks, and multiple-selection support. Trigger, popup, positioner, items, and indicators expose state data attributes for styling open, placement, highlighted, selected, disabled, and transition states.
