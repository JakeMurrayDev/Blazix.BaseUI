# Accordion Source Docs Comparison

Date: 2026-06-26

## Source Route

Source: `.base-ui/docs/src/app/(docs)/react/components/accordion/page.mdx`

Rendered route checked through the in-app browser:

- `http://localhost:3005/react/components/accordion`

Observed docs sections:

- `Accordion`
- `Anatomy`
- `Examples`
- `Open multiple panels`
- `API reference`
- `Root`
- `Item`
- `Header`
- `Trigger`
- `Panel`

PNPM docs validation:

- Command: `pnpm --dir .base-ui --filter docs run validate`
- Result: passed, no files needed updating.
- Log: `docs/audits/logs/accordion-source-docs-pnpm-2026-06-26.txt`

## Blazor Comparison Route

Route checked through the in-app browser:

- `http://127.0.0.1:5309/tests/accordion/server?keepMounted=true&hiddenUntilFound=true&animated=true&animationDuration=123&multiple=true&showThirdItem=true&showFourthItem=true`

Observed matching parts:

| Source docs part | Blazor rendered part | Result |
| --- | --- | --- |
| `Accordion.Root` | `[data-testid="accordion-root"]` rendered as `div` with no role. | Match |
| `Accordion.Item` | `[data-testid="accordion-item-1"]` rendered item open/closed state attributes. | Match |
| `Accordion.Header` | `[data-testid="accordion-header-1"]` rendered as `h3`. | Match |
| `Accordion.Trigger` | `[data-testid="accordion-trigger-1"]` rendered as `button type="button"`. | Match |
| `Accordion.Panel` | `[data-testid="accordion-panel-1"]` rendered as `div role="region"`. | Match |

Observed matching behavior:

| Source docs/API behavior | Blazor observation | Result |
| --- | --- | --- |
| Multiple panels are supported with `multiple`. | Test route rendered with `multiple=true` and four items. | Match |
| Closed searchable panels use hidden-until-found behavior. | Initial panel rendered `hidden="until-found"` and `data-closed`. | Match |
| Closed mounted panels remain in the DOM when mounted/searchable. | Initial panel existed in the DOM while closed. | Match |
| Trigger reflects panel state. | Closed trigger had `aria-expanded="false"`; open trigger had `aria-expanded="true"` and `data-panel-open`. | Match |
| Trigger references panel only while open. | Closed trigger had no `aria-controls`; open trigger had `aria-controls`. | Match |
| Panel labels itself from trigger. | Panel rendered `aria-labelledby` pointing to the trigger ID. | Match |
| Panel uses region semantics. | Panel rendered `role="region"`. | Match |
| Keyboard activation works. | Space activation opened a closed trigger/panel. | Match |

Evidence:

- `docs/audits/logs/accordion-in-app-browser-comparison-2026-06-26.json`

## Notes

React Base UI intentionally maps `Accordion.Root` value state to no root open/closed data attribute. The Blazor root likewise does not emit `data-open` or `data-closed`; open/closed attributes belong to item, trigger, and panel surfaces.
