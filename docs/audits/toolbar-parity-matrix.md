# Toolbar Parity Matrix

Audit target: Blazor Toolbar port against React Base UI Toolbar and internal Composite source.

## Source Files

- React Toolbar: `.base-ui/packages/react/src/toolbar`
- React Composite: `.base-ui/packages/react/src/internals/composite`
- Blazor Toolbar: `src/BlazorBaseUI/Toolbar`
- Blazor Toolbar JS: `src/BlazorBaseUI/wwwroot/blazor-baseui-toolbar.js`
- Blazor tests: `tests/BlazorBaseUI.Tests/Toolbar` and `tests/BlazorBaseUI.Playwright.Tests/.../Toolbar`

## Component and Utility Coverage

| React source | React responsibility | Blazor equivalent | Status |
| --- | --- | --- | --- |
| `ToolbarRoot.tsx` | Defaults `disabled=false`, `loopFocus=true`, `orientation=horizontal`; renders toolbar role and orientation attributes; provides context; uses `CompositeRoot`. | `ToolbarRoot.razor` builds `role`, `aria-orientation`, `data-orientation`, `data-disabled`; cascades `ToolbarRootContext`; initializes toolbar JS in `OnAfterRenderAsync`. | Verified |
| `CompositeRoot.tsx` / `useCompositeRoot.ts` | Roving focus, orientation-specific arrow keys, RTL reversal, loop handling, modifier-key bypass, native input cursor boundary behavior, focus-time input selection. | `blazor-baseui-toolbar.js` owns roving focus, `dir`/computed-style RTL handling, loop handling, modifier checks, native input boundary checks, and focus-time selection. | Repaired and verified |
| `CompositeRoot` default `enableHomeAndEndKeys=false` | Home/End are ignored unless explicitly enabled. Toolbar does not enable them. | JS handles arrow keys only. Playwright asserts Home and End do not move focus. | Repaired and verified |
| `CompositeList` / `useCompositeListItem` | Maintains composite item metadata and DOM-ordered map. | Toolbar parts register `ElementReference`s; JS stores a `Set`, filters connected descendants, and sorts by `compareDocumentPosition`. | Repaired and verified |
| `disabledIndices` in `ToolbarRoot.tsx` | Excludes disabled non-focusable items from composite navigation while allowing focusable disabled items. | JS `isItemFocusable` excludes native-disabled and non-focusable disabled items; keeps `data-focusable` disabled items navigable. | Repaired and verified |
| `useButton` | Native button type, non-native role, disabled/focusable disabled attributes, activation suppression. | `AccessibilityUtilities.ApplyNativeButtonAttributes` and `ApplyButtonAttributes`; JS suppresses disabled activation before Blazor handlers run. | Repaired and verified |
| `useFocusableWhenDisabled` | Uses `aria-disabled` for focusable disabled controls instead of native disabled. | Toolbar button/input emit `aria-disabled="true"` when focusable disabled; non-focusable native controls emit `disabled`. | Verified |
| `ToolbarButton.tsx` | Renders `button`; inherits root/group disabled; emits orientation, disabled, focusable attributes. | `ToolbarButton.razor` mirrors attributes and context inheritance, registers as composite item. | Verified |
| `ToolbarGroup.tsx` | Renders `div`, role group, cascades disabled, emits orientation/disabled attributes. | `ToolbarGroup.razor` mirrors role, data attributes, and disabled cascade. | Verified |
| `ToolbarInput.tsx` | Renders `input`; inherits disabled; supports focusable disabled; participates in composite navigation while preserving text cursor behavior. | `ToolbarInput.razor` mirrors attributes and registration; JS implements selection and boundary navigation. | Repaired and verified |
| `ToolbarInput.tsx` disabled key branch | Disabled input keydown stops every key except `ArrowLeft` and `ArrowRight`; Tab remains native browser focus movement. | JS stops disabled input keydown for non-Tab, non-horizontal-arrow keys before composite navigation. | Repaired and verified |
| `ToolbarLink.tsx` | Renders `a`; emits orientation; links remain active even when toolbar disabled. | `ToolbarLink.razor` emits orientation only, does not inherit disabled, registers as composite item. | Verified |
| `ToolbarSeparator.tsx` | Renders shared separator with orientation perpendicular to toolbar. | `ToolbarSeparator.razor` delegates to shared `Separator` with inverted orientation. | Verified |
| Data attribute modules | Root/button/group/input/link/separator expose orientation and relevant disabled/focusable attributes. | bUnit Toolbar tests verify attribute presence/absence for all public parts; Playwright verifies behavior-dependent attributes. | Verified |
| React render prop path | `render`, `className`, `style`, state, and element props are merged through Base UI render utilities. | All parts use `RenderElement<TState>` with `Render`, `ClassValue`, `StyleValue`, `State`, `ComponentAttributes`, and `AdditionalAttributes`. | Verified |

## Resolved Gaps

| Gap | Evidence before repair | Repair | Verification |
| --- | --- | --- | --- |
| RTL horizontal navigation was LTR-only. | `red-playwright-focused.log` failed RTL `ArrowLeft` navigation. | Added `dir`/computed-style direction detection and reversed forward/backward keys in JS. | `green-playwright-focused.log`, `toolbar-playwright-server.log`, in-app browser RTL check. |
| Home/End moved focus even though Toolbar does not enable those keys in React. | `red-playwright-focused.log` failed Home behavior. | Removed Home/End handling from Toolbar JS and renamed tests to assert no movement. | `green-playwright-focused.log`, `toolbar-playwright-server.log`, in-app browser Home check. |
| First tab stop could be a disabled non-focusable item. | `red-playwright-focused.log` failed first-tab-stop scenario. | `updateTabIndexes` now chooses the first focusable item and skips non-focusable disabled controls. | `green-playwright-focused.log`, `toolbar-playwright-server.log`. |
| Native input arrow handling ignored cursor boundaries and focus selection. | `red-playwright-focused.log` failed input cursor/selection behavior. | Added JS native input detection, full-value focus selection, and boundary-aware arrow navigation. | `green-playwright-focused.log`, `toolbar-playwright-server.log`, in-app browser input check. |
| Disabled focusable controls could invoke activation handlers. | `red-playwright-disabled-activation.log` showed click count `3`. | Added root-level listeners for click, pointer, keydown, and keyup suppression on disabled items. | `green-playwright-focused.log`, `toolbar-playwright-server.log`, in-app browser disabled click check. |
| Disabled focusable inputs could still process vertical composite arrows. | Manual source audit of `ToolbarInput.tsx` disabled `onKeyDown` branch. | Added disabled input key suppression for every key except Tab, `ArrowLeft`, and `ArrowRight`. | `toolbar-playwright-server.log`. |
| Roving tab stop did not resync on focus. | Manual source audit of JS focus path. | Added `focusin` listener to update tab indexes from the focused item. | Covered by Playwright tab/navigation tests. |

## Final Assessment

All React Toolbar behaviors found in the audited source are implemented directly in C# or in the component-specific toolbar JS module. DOM-heavy focus management remains in JS. Blazor state propagation uses `OnParametersSet`, `OnAfterRenderAsync`, and registration callbacks rather than React-style manual state loops.
