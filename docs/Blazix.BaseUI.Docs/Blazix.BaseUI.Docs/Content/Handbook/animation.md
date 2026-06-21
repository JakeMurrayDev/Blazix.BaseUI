# Animation

A guide to animating Blazix.BaseUI components.

Rendered docs: `/handbook/animation`

Components animate with CSS transitions or CSS animations, using data attributes to target their states.

## CSS transitions

- `[data-starting-style]` — the initial style to transition from.
- `[data-ending-style]` — the final style to transition to.

Transitions are recommended over animations because they can be cancelled smoothly midway.

## CSS animations

- `[data-open]` — applied while the component is visible.
- `[data-closed]` — applied before it is hidden.

## Keeping components mounted

Set `KeepMounted` on the `Portal` part to keep a popup rendered while closed.

## JavaScript-driven animation

Drive JavaScript-based animation from a JS module rather than Blazor; the CSS hooks above cover common enter/exit cases without any JavaScript.
