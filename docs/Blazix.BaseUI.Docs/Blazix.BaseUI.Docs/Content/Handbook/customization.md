# Customization

A guide to customizing the behavior of Blazix.BaseUI components.

Rendered docs: `/handbook/customization`

## Change events

Change events (`OnOpenChange`, `OnValueChange`, `OnCheckedChange`, …) pass a strongly-typed args object:

- `Reason` — a type-safe enum describing the cause of the change.
- `Cancel()` / `IsCanceled` — block the component's internal state change.
- `AllowPropagation()` / `IsPropagationAllowed` — let the DOM event propagate when Blazix stops it.
- `PreventUnmountOnClose()` — keep the popup mounted after its close transition.

### Canceling a change

Call `args.Cancel()` to block a specific transition (gated on `args.Reason`) while leaving the component uncontrolled.

### Allowing propagation

Pressing Escape stops propagation by default so parent popups don't close together; call `args.AllowPropagation()` to opt out.

## Controlling components with state

Components are uncontrolled by default (`DefaultOpen` / `DefaultValue` sets an uncontrolled initial state). Bind your own state with `@bind-Open` / `@bind-Value` to control them and drive state from outside the root.
