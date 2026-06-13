# Collapsible

Collapsible renders one trigger and one panel for showing or hiding supporting content.

## Import

```razor
@using Blazix.BaseUI.Collapsible
```

## Anatomy

```razor
<CollapsibleRoot DefaultOpen="true">
    <CollapsibleTrigger>Details</CollapsibleTrigger>
    <CollapsiblePanel>More information</CollapsiblePanel>
</CollapsibleRoot>
```

## Notes

- `Open` and `OpenChanged` make the component controlled.
- `DefaultOpen` sets the initial uncontrolled state.
- `KeepMounted` keeps closed content in the DOM.
