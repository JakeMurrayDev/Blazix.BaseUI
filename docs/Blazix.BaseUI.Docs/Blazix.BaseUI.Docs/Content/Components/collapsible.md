# Collapsible

A collapsible panel controlled by a button.

Rendered docs: `/components/collapsible`

## Anatomy

```razor
@using Blazix.BaseUI.Collapsible

<CollapsibleRoot>
    <CollapsibleTrigger />
    <CollapsiblePanel />
</CollapsibleRoot>
```

## API reference

### Root

Groups the trigger and panel while owning the open state. Use `Open`/`OpenChanged` for controlled state and `DefaultOpen` for uncontrolled state.

Data attributes: `data-open`, `data-closed`, `data-disabled`, `data-starting-style`, `data-ending-style`.

### Trigger

A button that opens and closes the panel. Renders a native `<button>` by default.

Data attributes: `data-panel-open`, `data-disabled`, `data-starting-style`, `data-ending-style`.

### Panel

The collapsible content area. Use `KeepMounted` to keep closed content in the DOM or `HiddenUntilFound` for browser find-in-page support.

Data attributes: `data-open`, `data-closed`, `data-disabled`, `data-starting-style`, `data-ending-style`.

CSS variables: `--collapsible-panel-height`, `--collapsible-panel-width`.
