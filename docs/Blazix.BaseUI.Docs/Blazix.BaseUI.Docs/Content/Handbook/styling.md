# Styling

A guide to styling Blazix.BaseUI components with any styling solution.

Rendered docs: `/handbook/styling`

Blazix.BaseUI components are unstyled and ship no CSS. Each part renders a plain HTML element you style yourself.

## Style hooks

- **CSS classes** — the standard `class` attribute, or a `ClassValue` function that receives the part's state record.
- **Data attributes** — state hooks such as Switch's `[data-checked]` / `[data-unchecked]`.
- **CSS variables** — dynamic values such as Popover's `--available-height` and `--anchor-width`.
- **Style attribute** — the standard `style` attribute, or a `StyleValue` function for state-driven inline styles.

Check each component's API reference for its full list of data attributes and CSS variables.

## Tailwind CSS

Apply utility classes through `class` / `ClassValue`; target states with arbitrary variants like `data-[checked]:`.

## Plain CSS

Apply your own class names through `class`, then style them in any stylesheet.
