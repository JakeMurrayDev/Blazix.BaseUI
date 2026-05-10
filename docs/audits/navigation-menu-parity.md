# NavigationMenu React vs. Blazor Parity Audit

Date: 2026-05-10
Branch: bunchofchanges
Scope: `src/BlazorBaseUI/NavigationMenu/**`, `src/BlazorBaseUI/wwwroot/blazor-baseui-navigation-menu.js`, NavigationMenu bUnit contracts/tests, and NavigationMenu Playwright tests.

## Verdict

NavigationMenu parity findings from the 2026-05-09 audit are closed in the current working tree. The port keeps DOM-heavy behavior in JavaScript and exposes the React source's observable attributes, ARIA state, data attributes, focus routing, dismiss behavior, placement callbacks, and controlled/uncontrolled value semantics through Blazor-native component state.

## Parity Matrix

| React source | Blazor equivalent | Verdict |
|---|---|---|
| `root/NavigationMenuRoot.tsx` | `NavigationMenuRoot.razor`, `NavigationMenuRootContext.cs`, `NavigationMenuRootState.cs`, transition lifecycle | Covered |
| `root/NavigationMenuRootContext.ts` | `NavigationMenuRootContext.cs` plus JS element registration | Covered |
| `trigger/NavigationMenuTrigger.tsx` | `NavigationMenuTrigger.razor` plus trigger JS listeners | Covered |
| `trigger/NavigationMenuTriggerDataAttributes.ts` | trigger data attributes and state rendering | Covered |
| `content/NavigationMenuContent.tsx` | `NavigationMenuContent.razor` plus JS viewport-target reparenting | Covered |
| `content/NavigationMenuContentDataAttributes.ts` | content state attributes, hidden/inert/transition states | Covered |
| `viewport/NavigationMenuViewport.tsx` | `NavigationMenuViewport.razor`, viewport target, focus guards, JS focus helpers | Covered |
| `positioner/NavigationMenuPositioner.tsx` | `NavigationMenuPositioner.razor`, `PositionerInterop`, floating JS callback | Covered |
| `positioner/*CssVars.ts`, `positioner/*DataAttributes.ts` | computed `data-side`, `data-align`, `data-anchor-hidden`, `data-instant`, floating CSS variables | Covered |
| `popup/NavigationMenuPopup.tsx` | `NavigationMenuPopup.razor`, popup state, origin-side/RTL handling | Covered |
| `popup/*CssVars.ts`, `popup/*DataAttributes.ts` | popup state attributes and JS size syncing | Covered |
| `list/NavigationMenuList.tsx` | `NavigationMenuList.razor`, composite JS initialization, key forwarding | Covered |
| `list/NavigationMenuDismissContext.ts` | JS-native dismiss equivalent for Escape, outside press, focus-out, and link propagation | Covered by equivalent |
| `item/NavigationMenuItem.tsx` / context | `NavigationMenuItem.razor`, `NavigationMenuItemContext.cs` | Covered |
| `link/NavigationMenuLink.tsx` / data attributes | `NavigationMenuLink.razor`, close-on-click, blur forwarding, active state | Covered |
| `portal/NavigationMenuPortal.tsx` / context | `NavigationMenuPortal.razor`, `NavigationMenuPortalContext.cs`, shared Portal | Covered |
| `backdrop/NavigationMenuBackdrop.tsx` / data attributes | `NavigationMenuBackdrop.razor`, state attributes | Covered |
| `arrow/NavigationMenuArrow.tsx` / data attributes | `NavigationMenuArrow.razor`, state attributes | Covered |
| `icon/NavigationMenuIcon.tsx` | `NavigationMenuIcon.razor`, popup-open state | Covered |
| `utils/constants.ts` | `data-base-ui-navigation-menu-trigger` emitted by trigger | Covered |
| `utils/isOutsideMenuEvent.ts` | JS containment checks for root, triggers, popup, viewport, viewport target, content, and nested menus | Covered |

## Hook and Utility Accounting

| React hook/utility | Blazor or JS equivalent |
|---|---|
| `useRenderElement` | `RenderElement<TState>` across all parts. |
| `useControlled` | `Value`, `ValueChanged.HasDelegate`, `DefaultValue`, and `ApplyValue`; controlled `null` remains closed. |
| `useTransitionStatus` / `useOpenChangeComplete` | `TransitionLifecycleManager`, root transition callbacks, `OnOpenChangeComplete`. |
| `useStableCallback`, refs, value refs | Stable component instance methods and JS root-state fields. |
| `useIsoLayoutEffect` DOM branches | JS registration and observer functions after render. |
| `useTimeout`, `useAnimationFrame` | JS timers and `requestAnimationFrame` for hover and size syncing. |
| `useFloatingTree`, floating node ids | `FloatingTree`, `FloatingNode`, root/parent context, JS nested containment. |
| `useHoverReferenceInteraction`, `useHoverFloatingInteraction`, `safePolygon` | JS hover timers, safe polygon cone, content/popup/viewport hover listeners. |
| `useClick` | Blazor click handler plus native JS capture for patient-click and stale hover timer cancellation. |
| `useDismiss` | JS document `keydown`, `mousedown`, and `focusout` listeners with close reasons. |
| `CompositeRoot`, `CompositeItem` | `NavigationMenuList` initializes shared composite JS; triggers/links/content expose focusable DOM with tested keyboard/dismiss behavior. |
| `useButton` | Trigger renders as `button`, preserves tab order, uses `aria-disabled`, and emits `type=button`. |
| `useAnchorPositioning`, `usePositioner`, `adaptiveOrigin` | `PositionerInterop` and shared floating JS; placement callback updates component state. |
| `useBaseUiId`, `useId` | generated root popup/viewport/target ids and item ids. |
| `FocusGuard`, tabbable helpers, `stopEvent` | Blazor `FocusGuard` plus JS tabbable lookup and native `preventDefault` / `stopPropagation`. |
| `ReactDOM.createPortal`, `FloatingPortal` | shared Portal for portal part; JS viewport-target reparenting for content target semantics. |
| Hooks not used by NavigationMenu source | Marked not applicable after source grep. |

## Attribute Rigidity

| Part | Attribute coverage |
|---|---|
| Root | `nav` or nested `div`, `data-orientation`, state-driven class/style, additional attributes. `aria-orientation` intentionally omitted to match React root output. |
| Trigger | `tabindex=0`, `type=button`, `aria-expanded`, `aria-controls`, `aria-disabled`, `data-base-ui-navigation-menu-trigger`, `data-popup-open`, `data-pressed`, focus guards, and `aria-owns` owner span. |
| Content | `data-open`, `data-closed`, `data-activation-direction`, transition data attributes, hidden/inert handling, focus/blur forwarding. |
| Viewport | generated `id`, blur handler, inert state, viewport target wrapper, focus guards. |
| Positioner | `role=presentation`, `hidden`, `inert`, `data-open`, `data-closed`, computed `data-side`, computed `data-align`, `data-anchor-hidden`, `data-instant`. |
| Popup | generated `id`, `tabindex=-1`, state data attributes, computed side/align state, anchor-hidden state, origin-side style handling. |
| Link | `aria-current`, active data attribute, click close propagation, blur forwarding, additional attributes. |
| Backdrop, Arrow, Icon, Item, List, Portal | Attributes covered by bUnit contracts and NavigationMenu part tests. |

## Verification

The detailed command log and proof checklist live in `.audit/navigation-menu-verification-report.md`.

Primary automated browser log: `.audit/navigation-menu-playwright.log`.

Final NavigationMenu verification:

```text
node --check src/BlazorBaseUI/wwwroot/blazor-baseui-navigation-menu.js
PASS

dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --filter "FullyQualifiedName~BlazorBaseUI.Tests.NavigationMenu"
Passed: 135

dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~BlazorBaseUI.Playwright.Tests.Tests.NavigationMenu"
Total tests: 24
Passed: 24
```

## Notes

Verification was run from `.claude/tmp-files/bbui-sdk10` with a temporary `global.json` pinned to SDK `10.0.203`. The repository has no checked-in `global.json`, so running from the repo root selected an installed .NET 11 preview SDK and failed the Playwright build before tests. No project files were changed to work around that environment issue.
