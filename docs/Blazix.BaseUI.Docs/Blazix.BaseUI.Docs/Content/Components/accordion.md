# Accordion

Accordion is a set of collapsible panels with headings. Use it when a page needs compact sections that reveal supporting content without navigating away.

## Import

```razor
@using Blazix.BaseUI.Accordion
```

## Anatomy

Import the component and assemble its parts:

```razor
<AccordionRoot TValue="string" DefaultValue="@(["install"])">
    <AccordionItem TValue="string" Value="install">
        <AccordionHeader>
            <AccordionTrigger>Install</AccordionTrigger>
        </AccordionHeader>
        <AccordionPanel>
            dotnet add package Blazix.BaseUI
        </AccordionPanel>
    </AccordionItem>
</AccordionRoot>
```

## Examples

### Open multiple panels

You can set up the accordion to allow multiple panels to be open at the same time using the `Multiple` parameter.

```razor
<AccordionRoot TValue="string"
               Value="@openPanels"
               ValueChanged="@(next => openPanels = next)"
               Multiple>
    <AccordionItem TValue="string" Value="accessibility">
        <AccordionHeader>
            <AccordionTrigger>Accessibility</AccordionTrigger>
        </AccordionHeader>
        <AccordionPanel KeepMounted="true">
            Trigger and panel IDs are linked automatically.
        </AccordionPanel>
    </AccordionItem>
</AccordionRoot>

@code {
    private string[] openPanels = ["accessibility"];
}
```

## State Patterns

- Use `DefaultValue` for an uncontrolled accordion with initial open panels.
- Use `Value` and `ValueChanged` for a controlled accordion where app state owns the open panels.
- Set `Multiple` when more than one panel can stay open.
- Use `OnValueChange` to inspect or cancel the next value before state updates.

## API

### AccordionRoot

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Value` | `TValue[]?` | `null` | Controlled array of open item values. |
| `DefaultValue` | `TValue[]` | `[]` | Initial open item values for uncontrolled usage. |
| `Multiple` | `bool` | `false` | Allows more than one panel to remain open. |
| `Disabled` | `bool` | `false` | Disables user interaction for all items. |
| `Orientation` | `Orientation` | `Vertical` | Controls arrow-key direction. |
| `LoopFocus` | `bool` | `true` | Wraps arrow-key focus across triggers. |
| `KeepMounted` | `bool` | `false` | Keeps closed panels in the DOM unless overridden. |
| `HiddenUntilFound` | `bool` | `false` | Uses `hidden="until-found"` so browser find-in-page can reveal closed content. |
| `OnValueChange` | `EventCallback<AccordionValueChangeEventArgs<TValue>>` | - | Cancelable callback invoked before state and binding callbacks update. |

### AccordionItem

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Value` | `TValue?` | generated for `string` | Unique item value. Non-string values must be provided. |
| `Disabled` | `bool` | `false` | Disables this item. |
| `OnOpenChange` | `EventCallback<CollapsibleOpenChangeEventArgs>` | - | Cancelable callback invoked when the item opens or closes. |

### AccordionHeader

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Render` | `RenderFragment<RenderProps<AccordionHeaderState>>?` | `null` | Replaces the default `h3` element or composes the header with another component. |
| `ClassValue` | `Func<AccordionHeaderState, string?>?` | `null` | Styles from item state such as `Open`, `Disabled`, `Index`, `Orientation`, and `Hidden`. |
| `StyleValue` | `Func<AccordionHeaderState, string?>?` | `null` | Adds state-based inline styles to the rendered header. |

### AccordionTrigger

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Disabled` | `bool?` | `null` | Overrides inherited item disabled state. |
| `NativeButton` | `bool` | `true` | Applies native button behavior by default. |
| `ClassValue` | `Func<AccordionTriggerState, string?>?` | `null` | Styles from trigger state such as `Open`, `Disabled`, `Index`, `Orientation`, and `Hidden`. |

### AccordionPanel

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `KeepMounted` | `bool?` | `null` | Overrides root `KeepMounted` for this panel. |
| `HiddenUntilFound` | `bool?` | `null` | Overrides root `HiddenUntilFound` for this panel. |
| `ClassValue` | `Func<AccordionPanelState, string?>?` | `null` | Styles from panel state including `TransitionStatus`. |
| `StyleValue` | `Func<AccordionPanelState, string?>?` | `null` | Adds styles while preserving measured panel CSS variables. |

## Keyboard

- `Tab` moves through triggers in document order.
- `Enter` or `Space` toggles the focused trigger.
- `ArrowDown` and `ArrowUp` move between triggers in vertical orientation.
- `ArrowRight` and `ArrowLeft` move between triggers in horizontal orientation.
- `Home` and `End` move to the first and last trigger.
- Disabled items are skipped by arrow-key navigation.

## Styling Hooks

- Items, headers, and panels expose `data-open`, `data-closed`, `data-disabled`, `data-hidden`, `data-index`, and `data-orientation`.
- Triggers expose `data-panel-open`, `data-disabled`, `data-hidden`, `data-index`, `data-orientation`, and `data-value`.
- Panels expose `data-starting-style` and `data-ending-style` during measured height animation.
- Panels publish `--accordion-panel-height` and `--accordion-panel-width`.

```razor
<AccordionPanel ClassValue="@(_ => "h-[var(--accordion-panel-height)] overflow-hidden transition-[height] duration-300 data-[ending-style]:h-0 data-[starting-style]:h-0")">
    Animated panel content
</AccordionPanel>
```

## Find In Page

Set `HiddenUntilFound` on the root or an individual panel when closed content should remain discoverable by the browser's built-in find-in-page behavior. When the browser reveals a hidden panel, Accordion opens the associated item through the same value-change pipeline.
