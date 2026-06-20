# Toggle

Toggle is a two-state button that can be pressed or unpressed.

## Usage guidelines

- Give icon-only toggles an accessible name with `aria-label`.
- Use `ToggleGroup` when several toggles share selection state.

## Anatomy

```razor
@using Blazix.BaseUI.Toggle

<Toggle aria-label="Favorite">
    Favorite
</Toggle>
```

## Parts

Parts: `Toggle`.

`Toggle` supports controlled state with `Pressed`, uncontrolled state with `DefaultPressed`, two-way binding with `PressedChanged`, and cancellable changes through `OnPressedChange`.

Toggle exposes `aria-pressed`, `data-pressed`, and `data-disabled`. When rendered as a non-native or grouped disabled control, it uses `aria-disabled`.
