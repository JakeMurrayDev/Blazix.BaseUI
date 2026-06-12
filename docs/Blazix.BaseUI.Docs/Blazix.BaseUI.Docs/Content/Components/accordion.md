# Accordion

A set of collapsible panels with headings.

Rendered docs: `/components/accordion`

## Anatomy

```razor
@using Blazix.BaseUI.Accordion

<AccordionRoot TValue="string">
    <AccordionItem>
        <AccordionHeader>
            <AccordionTrigger />
        </AccordionHeader>
        <AccordionPanel />
    </AccordionItem>
</AccordionRoot>
```

## Examples

### Open multiple panels

By default only one panel is open at a time. Set `Multiple="true"` to let the user open several panels at once.

```razor
<AccordionRoot TValue="string" Multiple="true">
    ...
</AccordionRoot>
```

## API reference

### Root

Groups all parts of the accordion. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Value` | `TValue[]?` | `null` | The controlled value of the item(s) that should be expanded. To render an uncontrolled accordion, use `DefaultValue` instead. |
| `DefaultValue` | `TValue[]` | `[]` | The uncontrolled value of the item(s) that should be initially expanded. |
| `Disabled` | `bool` | `false` | Determines whether the component should ignore user interaction. |
| `Multiple` | `bool` | `false` | Determines whether multiple items can be open at the same time. |
| `Orientation` | `Orientation` | `Vertical` | The visual orientation of the accordion. Controls arrow-key direction for roving focus. |
| `LoopFocus` | `bool` | `true` | Determines whether focus loops back to the first item when the end is reached with arrow keys. |
| `HiddenUntilFound` | `bool` | `false` | Allows the browser's built-in page search to find and expand panel contents. Overrides `KeepMounted`. |
| `KeepMounted` | `bool` | `false` | Determines whether closed panels stay in the DOM. Ignored when `HiddenUntilFound` is used. |
| `ValueChanged` | `EventCallback<TValue[]>` | — | Callback invoked when the value changes, supporting two-way binding. |
| `OnValueChange` | `EventCallback<AccordionValueChangeEventArgs<TValue>>` | — | Callback invoked when an item is expanded or collapsed. |
| `Render` | `RenderFragment<RenderProps<AccordionRootState<TValue>>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<AccordionRootState<TValue>, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<AccordionRootState<TValue>, string?>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-orientation`, `data-disabled`.

### Item

Groups an accordion header with the corresponding panel. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Value` | `TValue?` | auto | A unique value identifying this item. Generated automatically when omitted (string values). |
| `Disabled` | `bool` | `false` | Determines whether the component should ignore user interaction. |
| `OnOpenChange` | `EventCallback<CollapsibleOpenChangeEventArgs>` | — | Callback invoked when the panel is opened or closed. |
| `Render` | `RenderFragment<RenderProps<AccordionItemState<TValue>>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<AccordionItemState<TValue>, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<AccordionItemState<TValue>, string?>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-open`, `data-closed`, `data-disabled`, `data-index`, `data-orientation`.

### Header

A heading that labels the corresponding panel. Renders an `<h3>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<AccordionHeaderState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<AccordionHeaderState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<AccordionHeaderState, string?>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-open`, `data-closed`, `data-disabled`, `data-index`, `data-orientation`.

### Trigger

A button that opens and closes the corresponding panel. Renders a `<button>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Disabled` | `bool?` | `null` | Determines whether the trigger ignores user interaction. Inherits from the parent item when `null`. |
| `NativeButton` | `bool` | `true` | Determines whether the component renders a native `<button>` element. |
| `Render` | `RenderFragment<RenderProps<AccordionTriggerState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<AccordionTriggerState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<AccordionTriggerState, string?>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-panel-open`, `data-disabled`, `data-index`, `data-orientation`, `data-value`.

### Panel

A collapsible panel with the accordion item contents. Renders a `<div>` element by default.

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `KeepMounted` | `bool?` | `null` | Determines whether the panel stays in the DOM while closed. Inherits from Root when `null`. |
| `HiddenUntilFound` | `bool?` | `null` | Allows the browser's built-in page search to find and expand the panel contents. Inherits from Root when `null`. |
| `Render` | `RenderFragment<RenderProps<AccordionPanelState>>?` | `null` | Replaces the rendered element. |
| `ClassValue` | `Func<AccordionPanelState, string?>?` | `null` | Returns a CSS class based on state. |
| `StyleValue` | `Func<AccordionPanelState, string?>?` | `null` | Returns a CSS style based on state. |

Data attributes: `data-open`, `data-closed`, `data-disabled`, `data-index`, `data-orientation`, `data-starting-style`, `data-ending-style`.

CSS variables: `--accordion-panel-height`, `--accordion-panel-width`.
