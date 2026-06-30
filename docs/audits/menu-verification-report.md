# Menu Verification Report

Date: 2026-06-30 · React Base UI source: `.base-ui` @ `748f4228d`

Every command, analyzer result, and manual check performed to verify the Menu repairs.

## Commands

| # | Command | Result |
| --- | --- | --- |
| 1 | `dotnet build Blazix.BaseUI.slnx -c Debug` (baseline, pre-change) | **0 errors, 0 warnings** |
| 2 | `dotnet build src/Blazix.BaseUI/Blazix.BaseUI.csproj -c Debug` (post-change) | **0 errors, 0 warnings** |
| 3 | `npx terser blazix-baseui-menu.js --module -c -m -o blazix-baseui-menu.min.js` | regenerated (21,812 → 22,133 bytes; imports preserved; `isMenuItemVisible`/`checkVisibility` present) |
| 4 | `dotnet test …Tests --filter FullyQualifiedName~Blazix.BaseUI.Tests.Menu.` (bUnit) | **218 passed, 2 failed** (both pre-existing, see below), 220 total |
| 5 | bUnit on clean tree (`git stash`) for the 2 failures | **2 failed, 0 passed** — confirms pre-existing, unrelated to this audit |
| 6 | `dotnet test …Playwright… --filter FullyQualifiedName~MenuTestsServer` | **85 passed, 0 failed** (29 s) — log: `logs/menu-playwright-server.txt` |
| 7 | `dotnet test …Playwright… --filter FullyQualifiedName~MenuTestsWasm` | **85 passed, 0 failed** (1 m 2 s) — log: `logs/menu-playwright-wasm.txt` |
| 8 | `pnpm --filter docs dev` (React Base UI docs, :3005) | server ready; Menu route compiled and served |
| 9 | Blazor docs `preview_start docs` (:5216) | server ready; `/components/menu` rendered (14 triggers) |

## New tests added

- `MenuRadioGroupTests.GroupLabelWiresAriaLabelledby` — asserts a `Menu.GroupLabel` inside a `MenuRadioGroup` wires `aria-labelledby` to the label's id (role=presentation, text "Sort"). **Pass.**
- `MenuPopupTests.PopupIsLabelledByTrigger` — asserts the popup's `aria-labelledby` equals the trigger's `id`. **Pass.**
- Contracts updated: `IMenuRadioGroupContract`, `IMenuPopupContract`.

## Pre-existing failures (NOT caused by this audit)

Both fail identically on the clean tree (command 5):

- `MenuRootTests.HandleTriggerSwitchUpdatesJavaScriptMenubarContext`
- `MenuRootTests.ControlledTriggerIdSwitchUpdatesJavaScriptMenubarContext`

Both throw `Sequence contains no matching element` from a `.Last(predicate)` over recorded JS menubar-context invocations (`MenuRootTests.cs:614,693`). They concern menubar trigger-switch JS invocation assertions, an area untouched by this audit.

## In-app browser checks (Blazor docs :5216)

| Check | Result |
| --- | --- |
| Hero popup `aria-labelledby` == trigger `id` | ✅ `aadd700f` == `aadd700f` |
| GroupLabels demo: radio group `aria-labelledby` → "Sort" (role=presentation) | ✅ |
| GroupLabels demo: checkbox group `aria-labelledby` → "Workspace" | ✅ |
| Keyboard ArrowDown navigation moves highlight | ✅ Library→Playlist→Play Next |
| Typeahead "p" → "Play Last" | ✅ |
| Roving tabindex (highlighted item `tabindex=0`, others `-1`) while open | ✅ |
| Radio selection (click "Name") moves `aria-checked` | ✅ Date→Name |
| Console errors | ✅ none |

## PNPM source-docs comparison (React Base UI docs :3005)

Live React behavior captured and compared to the Blazor port:

| Behavior | React (source) | Blazor (port) | Match |
| --- | --- | --- | --- |
| Hero popup `aria-labelledby` | == trigger id | == trigger id | ✅ |
| GroupLabels radio group label | `aria-labelledby` → "Sort" (menuitemradio children) | same | ✅ |
| GroupLabels checkbox group label | `aria-labelledby` → "Workspace" (menuitemcheckbox children) | same | ✅ |
| Active item `tabindex` | `0` on highlighted | `0` on highlighted | ✅ |
| Demo set | hero, open-on-hover, checkbox-items, radio-items, group-labels, submenu, arrow, detached-triggers (simple/full/controlled), multiple-triggers | 1:1 present | ✅ |
| Documented parts | 19 + Separator + createHandle/Handle | all present | ✅ |

## Multi-trigger switch fix (follow-up)

Reported bug: in controlled / multiple-trigger / animating-viewport menus, clicking a sibling trigger to switch the open menu "briefly opens then closes."

| Check | Result |
| --- | --- |
| In-app repro (controlled demo): open Library, click Playback | **Before fix:** popup `data-closed` within 100 ms (closed). |
| In-app repro: fresh open of any single trigger | works (isolates the cause to switching while another menu is open) |
| Root cause | `handleGlobalPointerDown` excluded only the active trigger; clicking a sibling trigger → `OnOutsidePress` → close (React `useDismiss` excludes all `triggerElements`) |
| After fix — controlled demo: Library → Playback → Share | ✅ each switch stays `open` with correct items (Add to library… → Play/Add to queue → Share/Copy link) |
| After fix — animating demo (viewport, 3 triggers): Library → Playback → Share | ✅ each switch stays `open`, content morphs (Add to library… → Play now… → Copy link…) |
| New Playwright tests `MultiTrigger_{Contained,Handle}_SwitchWhileOpen` (Server) | ✅ 2 passed |
| New Playwright tests (WASM) | ✅ (run in `menu-switch-validation`) |
| Full Playwright Menu Server after fix | ✅ 87/87 |
| bUnit Menu after fix | ✅ 218 passed, 2 pre-existing failures (unchanged) |

## Net result

Build clean; bUnit 218 pass (+2 new, 0 regressions, 2 pre-existing failures isolated); Playwright Server 85/85 (87/87 after the multi-trigger fix + 2 new tests) and WASM 85/85; in-app browser confirms both a11y repairs, the multi-trigger switch fix, and no regression; live React docs match the Blazor port exactly for every repaired behavior.
