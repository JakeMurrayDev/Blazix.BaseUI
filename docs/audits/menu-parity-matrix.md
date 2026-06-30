# Menu Parity Matrix

Date: 2026-06-30 · React Base UI source: `.base-ui` @ `748f4228d`

Confirms every React Menu hook/utility/part/upstream-fix has a verified Blazor equivalent. Legend: ✅ present · 🔧 repaired this audit · 📝 accounted-for (see functional audit Residual Risk) · ⬛ N/A.

## Parts — element, role, key attributes

| Part | React tag / role | Blazor `Tag` / role | Key attributes (React → Blazor) | Status |
| --- | --- | --- | --- | --- |
| Root | none | none | element-less context provider | ✅ |
| Trigger | `button` (or CompositeItem in menubar) | `button`/`div` (NativeButton) | `aria-haspopup=menu`, `aria-expanded`, `aria-controls` (open), `data-popup-open`+`data-pressed`, `id` | ✅ |
| Portal | FloatingPortal | `FloatingPortal` | `keepMounted` (default false), container=body | ✅ |
| Positioner | `div` | `div` role=presentation | `data-side`/`data-align`/`data-open`/`data-closed`/`data-anchor-hidden`/`data-nested`/`data-instant`, CSS vars, `hidden`, side/align defaults (submenu inline-end, ctx-menu start) | ✅ |
| Popup | `div` role=menu | `div` role=menu | `id`, **`aria-labelledby`=trigger id**, `tabindex=-1`, `data-open/closed/side/align/starting/ending/instant/nested`, `data-rootownerid` | 🔧 (aria-labelledby) |
| Arrow | `div` aria-hidden | `div` aria-hidden | `data-side/align/open/closed/uncentered` | ✅ |
| Backdrop | `div` role=presentation | `div` role=presentation | `hidden`, `data-open/closed/starting/ending`, `pointer-events:none` when reason=triggerHover, `user-select:none`+`-webkit-` | ✅ |
| Item | `div` role=menuitem | `div`/`button` role=menuitem | `id`, `tabindex=(open&&highlighted)?0:-1`, `data-highlighted`, `data-disabled`/`aria-disabled`, `data-label`, closeOnClick default **true** | 🔧 (tabindex open-gate) |
| LinkItem | `a` role=menuitem | `a` role=menuitem | as Item, closeOnClick default **false**, no disabled | 🔧 (tabindex) |
| CheckboxItem | `div` role=menuitemcheckbox | `div`/`button` | `aria-checked`, `data-checked`/`data-unchecked`, closeOnClick **false** | 🔧 (tabindex) |
| CheckboxItem.Indicator | `span` aria-hidden | `span` aria-hidden | `keepMounted` (false), `data-checked/unchecked/disabled/starting/ending` | ✅ |
| RadioGroup | `div` role=group | `div` role=group | `role=group`, **`aria-labelledby`=label id**, `aria-disabled` | 🔧 (#4826) |
| RadioItem | `div` role=menuitemradio | `div`/`button` | `aria-checked`, `data-checked/unchecked`, closeOnClick **false** | 🔧 (tabindex) |
| RadioItem.Indicator | `span` aria-hidden | `span` aria-hidden | `keepMounted`, transition data-attrs | ✅ (📝 robustness) |
| Group | `div` role=group | `div` role=group | `aria-labelledby` from GroupLabel | ✅ |
| GroupLabel | `div` role=presentation | `div` role=presentation | `id`, registers into group context | ✅ |
| SubmenuRoot | none (wraps Root) | none (wraps Root) | nested; React omits handle/triggerId | ✅ (📝 #4891 extra params) |
| SubmenuTrigger | `div` role=menuitem | `div`/`button` role=menuitem | `aria-haspopup`, `aria-expanded`, `aria-controls`, `tabindex=(open\|\|highlighted)?0:-1`, `data-popup-open`, openOnHover default **true** | ✅ |
| Viewport | `div` | `div` | `data-activation-direction`/`data-transitioning`/`data-instant`, `data-current`/`data-previous`, CSS vars | ✅ |
| Separator | re-export | `Blazix.BaseUI.Separator.Separator` | role=separator | ✅ |
| Handle / createHandle | `MenuHandle` | `IMenuHandle`/`MenuHandleFactory` | open(triggerId)/close/isOpen | ✅ |

## Hooks / utilities → Blazor equivalent

| React hook / utility | Blazor equivalent | Status |
| --- | --- | --- |
| `MenuStore` / `popupStoreSelectors` | `MenuRootContext` + `MenuRoot.razor` state/selectors | ✅ |
| `useListNavigation` (arrow/Home/End, loop, orientation, `disabledIndices:[]`) | `blazix-baseui-menu.js handleGlobalKeyDown` (lines 150-300) | ✅ |
| `useTypeahead` (multi-char, 500ms reset, repeated-char cycle, **skip CSS-hidden**) | `menu.js` typeahead loops + **`isMenuItemVisible`** | 🔧 (#4195 hidden-skip) |
| `useButton` Space-on-keydown (#4053) | `menu.js` Enter/Space activate on keydown | ✅ |
| `useClick`/`useFocus` (trigger) | `MenuTrigger`/`MenuTypedTrigger` pointerdown/click/keydown | 🔧 (typed-trigger interaction type) |
| `useHover` + `safePolygon` (close-delay triangle) | `floating.js createHoverInteraction` + `safePolygon` | ✅ |
| `safePolygon` `blockPointerEvents` scoping (#4231/#4723) | threaded but not applied | 📝 deferred refinement |
| `useHover` restMs submenu open (#4990) | `allowMouseEnter`-gated delay swap | 📝 approximation |
| `FloatingFocusManager` (#5093/#5030/#5024/#4775) | shared `<FloatingFocusManager>` + `floating.js` | ✅ inherited |
| `markOthers` / `data-base-ui-inert` (#3955) | `floating.js applyAttributeToOthers`/`markOthers` | ✅ inherited |
| `useDismiss` (escape/outside/focus-out) | `menu.js` global keydown + `handleGlobalPointerDown` | ✅ |
| `useDismiss` excludes **all** `triggerElements` from outside-press (multi-trigger switch) | `MenuRoot` `knownTriggerIds` → JS `setTriggerIds`; `handleGlobalPointerDown` excludes any registered trigger | 🔧 repaired |
| `usePopupViewport` content transitions (#4060) | `MenuViewport` + `menu.js onViewportTriggerChange` | ✅ |
| `useAnchoredPopupScrollLock` (modal) | `menu.js setRootOpen` scroll-lock + `acquireScrollLock` | ✅ |
| `useListNavigation` pointer-leave focus guard (#4125/#4581) | n/a — Blazor has no pointerleave→focus path | ⬛ moot |
| `useListNavigation` clear-highlight clipped (#4604) | C# per-item `HandleMouseLeave` (common path) | 📝 partial |

## Upstream fix coverage

| Fix | Status |
| --- | --- |
| #4826 group labels in radio groups | 🔧 repaired |
| popup aria-labelledby (=trigger id) | 🔧 repaired |
| #4931 kept-mounted item tabindex open-gate | 🔧 repaired |
| #4195 typeahead skip CSS-hidden | 🔧 repaired |
| MenuTypedTrigger interaction-type | 🔧 repaired |
| multi-trigger switch (outside-press excludes all triggers) | 🔧 repaired |
| #5093/#5030/#5024/#4775/#3955 (shared focus/inert) | ✅ inherited |
| #4053 Space-on-keydown | ✅ present |
| #4893 controlled hover-leave close | ✅ present |
| #4892/#3858 submenu trigger disabled/highlight/warn | ✅ present |
| #4134 submenu closeDelay | ✅ present |
| #4060 viewport transitions | ✅ present |
| #4231/#4723 safePolygon pointer-events scope | 📝 deferred |
| #4990 submenu rest-delay | 📝 approximation |
| #4604 clipped clear-highlight | 📝 partial |
| #4891 SubmenuRoot prop omission | 📝 API-surface |
| #4377/#4922 menubar | ⬛ MenuBar component |
| #5028/#5027/#4944 composite/grid | ⬛ N/A (combobox) |
