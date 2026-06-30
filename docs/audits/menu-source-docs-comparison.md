# Menu Source-Docs Comparison

Date: 2026-06-30

Compares the Blazix.BaseUI Menu docs (Blazor docs site, `:5216` `/components/menu`) against the live React Base UI docs (`pnpm --filter docs dev`, `:3005` `/react/components/menu`, source `.base-ui` @ `748f4228d`).

## Demo parity (1:1)

| React demo (`menu/demos/*`) | Blazor demo (`Demos/Menu/*`) | Present | Behavior match |
| --- | --- | --- | --- |
| hero | Hero (Css/Tailwind) | ✅ | popup labelled by trigger; items navigable; typeahead |
| open-on-hover | OpenOnHover | ✅ | hover-opens with delay; safePolygon close |
| checkbox-items | CheckboxItems | ✅ | `aria-checked`, indicator keepMounted |
| radio-items | RadioItems | ✅ | single-selection `aria-checked` |
| group-labels | GroupLabels | ✅ | **radio group + checkbox group both `aria-labelledby`** (was the #4826 gap) |
| submenu | Submenu | ✅ | submenu hover/arrow open, nested orientation |
| arrow | Arrow | ✅ | `data-side/align/uncentered` |
| detached-triggers-simple | DetachedTriggersSimple | ✅ | handle.open(id) |
| detached-triggers-full | DetachedTriggersFull | ✅ | payload + multiple triggers |
| detached-triggers-controlled | DetachedTriggersControlled | ✅ | controlled triggerId |
| (close-on-click — inline in docs) | covered via `CloseOnClick` param | ✅ | Item default true; others false |

## Anatomy / API reference parity

React `page.mdx` documents these parts; all present in the Blazor port:

`Root, Trigger, Portal, Backdrop, Positioner, Popup, Viewport, Arrow, Item, LinkItem, SubmenuRoot, SubmenuTrigger, Group, GroupLabel, RadioGroup, RadioItem, RadioItemIndicator, CheckboxItem, CheckboxItemIndicator, Separator` + `createHandle`/`Handle`.

## Live behavioral comparison (captured this audit)

| Behavior | React (`:3005`) | Blazor (`:5216`) | Match |
| --- | --- | --- | --- |
| Hero popup `aria-labelledby` | == trigger `id` | == trigger `id` | ✅ |
| GroupLabels radio group | `aria-labelledby` → "Sort" presentation label | same | ✅ (repaired) |
| GroupLabels checkbox group | `aria-labelledby` → "Workspace" | same | ✅ |
| Active item `tabindex` | `0` on highlighted item | `0` on highlighted item | ✅ |
| Item roles | menuitem / menuitemcheckbox / menuitemradio | identical | ✅ |
| Trigger | `aria-haspopup=menu`, `aria-expanded`, `aria-controls` (open) | identical | ✅ |

## Notes / deliberate differences

- The Blazor docs render through .NET Auto render mode (Server + WASM) rather than Next.js; visual styling (CSS Modules / Tailwind variants) mirrors the React hero demo styles per repository guidelines.
- React docs auto-generate component ids as `base-ui-_r_*`; Blazor uses GUID-derived ids. Only the id *values* differ; the `aria-labelledby`/`aria-controls` *relationships* are identical.
- Behavioral refinements not yet ported (safePolygon `pointer-events` scoping #4231/#4723, submenu `restMs` #4990) are documented in `menu-functional-audit.md` Residual Risk; they are not observable as missing demos and do not change the documented API surface.
