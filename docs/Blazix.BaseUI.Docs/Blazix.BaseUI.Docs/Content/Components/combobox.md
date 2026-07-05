# Combobox

An input combined with a list of predefined items to select.

Rendered docs: `/components/combobox`

## Usage guidelines

- Combobox is a filterable Select. Use it when values are restricted to predefined items and the list is large enough to need filtering.
- Do not use Combobox for free-form search widgets. Use Autocomplete when arbitrary input should be accepted.
- Use Select when no input is rendered.
- Form controls need an accessible name. Label `ComboboxInput` when the input is visible outside the popup. Use `ComboboxLabel` for the input-inside-popup pattern, where the trigger is the form control.

## Anatomy

```razor
@using Blazix.BaseUI.Combobox

<ComboboxRoot TValue="string">
    <ComboboxLabel />

    <ComboboxInputGroup>
        <ComboboxInput />
        <ComboboxTrigger />
        <ComboboxIcon />
        <ComboboxClear />
        <ComboboxValue />

        <ComboboxChips>
            <ComboboxChip>
                <ComboboxChipRemove />
            </ComboboxChip>
        </ComboboxChips>
    </ComboboxInputGroup>

    <ComboboxPortal>
        <ComboboxBackdrop />
        <ComboboxPositioner>
            <ComboboxPopup>
                <ComboboxArrow />
                <ComboboxStatus />
                <ComboboxEmpty />

                <ComboboxList>
                    <ComboboxRow>
                        <ComboboxItem TValue="string">
                            <ComboboxItemIndicator />
                        </ComboboxItem>
                    </ComboboxRow>

                    <ComboboxSeparator />

                    <ComboboxGroup>
                        <ComboboxGroupLabel />
                    </ComboboxGroup>

                    <ComboboxCollection TValue="string" />
                </ComboboxList>
            </ComboboxPopup>
        </ComboboxPositioner>
    </ComboboxPortal>
</ComboboxRoot>
```

## Examples

### Typed wrapper component

Wrap the generic root when a reusable strongly typed combobox is useful.

```razor
@typeparam TValue

<ComboboxRoot TValue="TValue" @attributes="AdditionalAttributes">
    @ChildContent
</ComboboxRoot>
```

### Multiple select

Enable `Multiple` and render selected values with `ComboboxValue`, `ComboboxChips`, `ComboboxChip`, and `ComboboxChipRemove`.

### Input inside popup

Render `ComboboxInput` inside `ComboboxPopup` for a searchable select popup. In this pattern, `ComboboxLabel` labels the trigger.

### Grouped

Use `ItemGroups` for grouped data and render `ComboboxGroup` with `ComboboxGroupLabel` in the list.

```csharp
private static readonly IReadOnlyList<ComboboxOptionGroup<Produce>> Groups =
[
    new(Fruits, "Fruits"),
    new(Vegetables, "Vegetables"),
];
```

### Async search (single)

Control `InputValue`, disable built-in filtering, and replace `Items` as remote results arrive. Keep the selected item in `Items` so it remains selectable while new results stream in.

### Async search (multiple)

Use the same external filtering pattern with `Multiple` and `Values`. Merge selected values into the current result list so selected chips remain backed by mounted item data.

### Creatable

Add a synthetic create item when the query has no exact match. Cancel the synthetic value change, open a dialog, then add and select the new item after confirmation.

### Virtualized

Use `Virtualized`, `FilteredItems`, and Blazor's `Virtualize` component for large lists. Pass stable `Index`, `aria-setsize`, and `aria-posinset` values to `ComboboxItem` so keyboard and assistive-technology metadata remain correct.

#### Memoizing items

React's `React.memo` guidance maps to small stable Blazor item components. For large enough datasets, prefer virtualization because the initial mount cost dominates.

## API reference

Parts: `ComboboxRoot<TValue>`, `ComboboxLabel`, `ComboboxInputGroup`, `ComboboxInput`, `ComboboxTrigger`, `ComboboxIcon`, `ComboboxClear`, `ComboboxValue`, `ComboboxChips`, `ComboboxChip`, `ComboboxChipRemove`, `ComboboxPortal`, `ComboboxBackdrop`, `ComboboxPositioner`, `ComboboxPopup`, `ComboboxArrow`, `ComboboxStatus`, `ComboboxEmpty`, `ComboboxList`, `ComboboxRow`, `ComboboxItem<TValue>`, `ComboboxItemIndicator`, `ComboboxGroup`, `ComboboxGroupLabel`, `ComboboxCollection<TValue>`, and `ComboboxSeparator`.

Root exposes controlled and uncontrolled selected-value, multiple-value, input-value, and open-state parameters; form serialization; static and grouped item data; externally filtered items; callbacks; and virtualized rendering support. Input, trigger, popup, positioner, item, chip, and indicator parts expose state data attributes for styling open, placement, selected, highlighted, disabled, readonly, validation, and transition states.
