# Code Snippet Server First-Open Lag

## Summary

The docs code snippet expander shows a noticeable first-open delay in server-side interactive rendering. The visible delay is not caused by the `Demo` component's CSS transition: after the page is interactive, the code viewport class changes within a few milliseconds and then runs the expected 500 ms expand animation.

## Evidence

- The snippet body is already rendered while collapsed; opening the snippet does not lazily create the `CodeBlock`.
- `CodeBlock` syntax highlighting runs from `OnAfterRenderAsync`, not from the `Show code` click path.
- The docs snippet uses `CollapsibleRoot` and `CollapsibleTrigger`, but does not use `CollapsiblePanel`, so panel measurement interop is not part of this path.
- Local Playwright timing showed settled first-click class mutations around 3-5 ms in Server mode and 8-18 ms in WASM mode.
- Clicks made immediately after prerendered markup appears can occur before the interactive runtime is ready, so the click does not update the component until interactivity is established.

## Deferred Follow-Up

Investigate whether `CollapsibleRoot`/`CollapsibleTrigger` should provide a client-side optimistic open path, an interactive-ready affordance, or docs-specific disabled/loading treatment for prerendered Server mode. This should be handled separately from docs styling because the lag sits in the interactive component/event path rather than the expand animation CSS.

## Follow-Up Investigation

Other components are affected by the same first-interaction window. The issue is not specific to the docs code snippet or to `CollapsibleTrigger`: the server-prerendered HTML exposes interactive-looking controls before the Blazor Server circuit has attached event handlers. A click in that window does not apply after interactivity arrives; the user needs a later click.

Confirmed with a Playwright probe that delayed `blazor.web.js` and clicked visible prerendered controls before hydration:

| Surface | Pre-interactive click applied after hydration? | Later click after hydration |
| --- | --- | --- |
| Docs code expander (`CollapsibleTrigger`) | No | Opened |
| Accordion demo trigger | No | Opened |
| Demo source tab (`TabsTab`) | No | Selected |
| Demo variant trigger (`SelectTrigger`) | No | Opened |
| Alert Dialog trigger | No | Opened |
| Autocomplete trigger | No | Opened |
| Theme toggle (`SwitchRoot`) | No | Toggled |
| Mobile navigation (`DrawerTrigger`) | No | Opened |

Once the same page was already interactive, the representative controls responded normally. Local Playwright timings for the interactive click-to-state-change path were roughly 200-335 ms for the Server-mode controls in this run; those timings include browser automation and SignalR round-trip overhead, so they should be treated as coarse confirmation rather than micro-benchmark numbers.

The broader source scan shows the same class of risk for stateful controls whose first visible action is handled through Blazor event callbacks or post-render JavaScript interop, including Accordion, Alert/Dialog/Drawer/Popover/Menu-family triggers, Autocomplete/Select, Tabs, Toggle/Switch/Checkbox/Radio groups, Slider/NumberField/Form controls, Toast actions, and docs-only controls like `CopyButton`, `RuntimeSwitch`, and `ThemeToggle`. Static display components and already-rendered default state are not meaningfully affected by this specific first-open lag. Plain native links can still navigate before hydration, though any Blazor callback attached to the same click waits for interactivity.

Implication: a Collapsible-only optimistic open path would fix the visible code snippet symptom but leave the same prerender affordance problem on other Server-rendered controls. A docs-level interactive-ready affordance, disabling/hiding interactive controls until hydration, or a broader component-level hydration strategy would cover the real scope more honestly.

## Resolution

Implemented a docs-shell hydration gate instead of a Collapsible-specific optimistic path. The prerendered docs body now starts with `blazix-docs-preinteractive`, capture-phase event handling suppresses non-link interactive controls while that class is present, and `MainLayout` removes the gate on the first interactive render via `blazixDocs.markInteractive()`. This prevents the first pre-hydration click from being accepted visually and then lost, while preserving normal native link navigation before hydration.
