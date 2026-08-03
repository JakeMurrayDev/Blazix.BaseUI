# Coverage Gap: Upstream Base UI Components vs. Blazor Port (#146)

Research date: 2026-08-03.

Sources (primary only):

- Upstream pin: vendored clone at `.base-ui`, commit `bdcb685fadcca9d18b18f013c052795a53b6aa33` (2026-07-18), `packages/react/src/`.
- Upstream HEAD: `mui/base-ui` `origin/master` at `1a2ca3c9f8a39bd8c0dda939a7a23b72da226124` (2026-08-03), inspected via a separate blobless clone (the vendored clone was not modified).
- Port: `src/Blazix.BaseUI/` in this repository.

Part inventories were taken from each upstream component's `index.parts.ts` (or `index.ts` for single-part components) and compared against the `.razor`/`.cs` part components in the matching `src/Blazix.BaseUI/<Component>/` directory.

## Summary

| Metric | Count |
|---|---|
| Upstream public component namespaces at pin | 40 |
| Fully ported (every exported part has a Blazor counterpart) | 40 |
| Partially ported (parts missing) | 0 |
| Upstream components with no Blazor counterpart | 0 |
| New upstream components since the pin | 0 |
| Upstream exported parts total (incl. cross-namespace re-exports and Handles) | 275 — all present |
| React-only utility entry points with no (needed) 1:1 counterpart | 3 (`merge-props`, `use-render`, `unstable-use-media-query`) |

Upstream delta since the pin (`bdcb685..origin/master`, `packages/react/src`): 373 files changed, but **zero** new component directories and **zero** changes to any `index.parts.ts`/component `index.ts` — the public part surface is identical. The only added source files are internal utilities: `packages/react/src/internals/getDisabledMountTransitionStyles.ts` and `packages/react/src/internals/useAnchorPositioning.ts`. (Behavioral drift in existing files is out of scope here; see the port-audit process for that.)

## 1. Upstream components with no Blazor counterpart

None. All 40 public component namespaces exported from `packages/react/src/index.ts` at the pin have a directory in `src/Blazix.BaseUI/`.

| Upstream | Blazor | 
|---|---|
| — | — |

## 2. Upstream components new since the pin

None. `git ls-tree origin/master packages/react/src/` at `1a2ca3c` lists exactly the same component directories as the pin, and the diff `bdcb685..origin/master -- 'packages/react/src/*/index.parts.ts' 'packages/react/src/*/index.ts'` is empty.

| New upstream component | Notes |
|---|---|
| — | — |

## 3. Partially-ported components (missing parts)

None — every exported part in every component maps to a Blazor part component. Two surface-shape notes (not missing functionality):

| Component | Upstream export | Blazor realization | Note |
|---|---|---|---|
| AlertDialog | `AlertDialogHandle` / `createAlertDialogHandle` (`packages/react/src/alert-dialog/handle.ts`) | `AlertDialogRoot.Handle` parameter of type `IDialogHandle` (`src/Blazix.BaseUI/AlertDialog/AlertDialogRoot.razor`) | Upstream's class is a type-only nominal brand over `DialogHandle` (no runtime behavior of its own). The port reuses the shared dialog handle; there is no branded `AlertDialogHandle` type. Functionally equivalent. |
| Menubar | `Menubar` (`packages/react/src/menubar/index.ts`) | `MenuBarRoot` (`src/Blazix.BaseUI/MenuBar/MenuBarRoot.razor`) | Naming deviation only (`Menubar` vs `MenuBar.MenuBarRoot`). |

React hook/function exports that are part of component namespaces map to Blazor idioms rather than 1:1 components:

| Upstream hook/function | Upstream path | Blazor realization |
|---|---|---|
| `Combobox.useFilter`, `Combobox.useFilteredItems` (also re-exported by Autocomplete) | `packages/react/src/combobox/root/utils/useFilter.ts`, `useFilteredItems.ts` | Filtering built into `ComboboxRoot`/`AutocompleteRoot` (`src/Blazix.BaseUI/Combobox/ComboboxRoot.razor`, `ComboboxRootContext.cs`) |
| `Toast.useToastManager`, `Toast.createToastManager` | `packages/react/src/toast/useToastManager.ts`, `createToastManager.ts` | `ToastManager` / `ToastManagerContext` (`src/Blazix.BaseUI/Toast/ToastManager.cs`) |
| `Direction.useDirection` | `packages/react/src/internals/direction-context/DirectionContext.ts` | Cascading `DirectionProviderContext` (`src/Blazix.BaseUI/DirectionProvider/DirectionProviderContext.cs`) |

React-only utility entry points with no dedicated Blazor counterpart (the concept is covered by port infrastructure or is not applicable):

| Upstream entry point | Path | Blazor status |
|---|---|---|
| `merge-props` | `packages/react/src/merge-props/` | Covered by `AttributeUtilities.cs` / `RenderProps.cs` infrastructure |
| `use-render` | `packages/react/src/use-render/` | Covered by `RenderElement.cs` / `RenderElement.razor` |
| `unstable-use-media-query` | `packages/react/src/unstable-use-media-query/` | No counterpart (unstable React hook, not exported from the main `index.ts`) |

## 4. Port components/dirs with no upstream counterpart (local additions)

Listed without judgment. Paths relative to `src/Blazix.BaseUI/`.

| Local addition | Kind |
|---|---|
| `Base/` (`ControlBase.cs`, `ExpressionFormatter.cs`, `ReverseStringBuilder.cs`) | Form-control infrastructure (adapted from ASP.NET Core internals) |
| `Portal/` (`Portal.cs`, `Portal.razor`) | Shared portal primitive (upstream portals live inside each component and share internal utilities) |
| `Utilities/` (`Drawer`, `FloatingDelayGroup`, `FloatingFocusManager`, `FloatingTree`, `FocusGuard`, `LabelableProvider`, `Portal`, `SlopwatchSuppressAttribute.cs`) | Ports of upstream *internal* machinery (`packages/react/src/floating-ui-react/`, `internals/`) surfaced as shared utilities |
| `Dialog/DialogTypedTrigger.razor`, `Drawer/DrawerTypedTrigger.razor`, `Menu/MenuTypedTrigger.{cs,razor}`, `Popover/PopoverTypedTrigger.razor`, `PreviewCard/PreviewCardTypedTrigger.razor`, `Tooltip/TooltipTypedTrigger.razor` | Blazor-specific typed-trigger pattern (payload-generic triggers) |
| Root-level infrastructure: `RenderElement.{cs,razor}`, `RenderProps.cs`, `RenderUtilities.cs`, `AttributeUtilities.cs`, `AccessibilityUtilities.cs`, `EventUtilities.cs`, `TransitionAttributeHelper.cs`, `TransitionLifecycleManager.cs`, `ComponentHandleBase.cs`, `IFloatingRootContext.cs`, `InteractionTypeDetector.cs`, `PositionerConfig.cs`, `PositionerInterop.cs`, `OffsetData.cs`, `OpenChangeEventArgs.cs`, `SidePadding.cs`, `FocusTarget.cs`, `Enumerations.cs`, `Extensions.cs` | Cross-cutting port infrastructure (upstream equivalents live in `internals/`, `utils/`, `use-render/`, `merge-props/`) |
| Per-component `*State.cs`, `*Context.cs`, `EventArgs.cs`, `Enumerations.cs`, `Extensions.cs` files | Porting pattern (state records, cascading contexts, typed event args); not upstream part exports |

## Appendix: per-component part inventory (pin)

Status: OK = all upstream parts present in the port. Upstream citation is the component's `index.parts.ts` (or `index.ts` where noted) under `packages/react/src/`.

| Upstream component | Parts (upstream export names) | # | Blazor dir | Status |
|---|---|---|---|---|
| `accordion` | Root, Item, Header, Trigger, Panel | 5 | `Accordion/` | OK |
| `alert-dialog` | Root, Trigger, Handle, Backdrop, Close, Description, Popup, Portal, Title, Viewport (Backdrop..Viewport re-exported from `dialog/`) | 10 | `AlertDialog/` | OK (Handle via shared `IDialogHandle`, see §3) |
| `autocomplete` | Root, Trigger, Value, Item, InputGroup, Arrow, Backdrop, Clear, Collection, Empty, Group, GroupLabel, Icon, Input, List, Popup, Portal, Positioner, Row, Status, Separator (most re-exported from `combobox/`) | 21 | `Autocomplete/` | OK |
| `avatar` | Root, Image, Fallback | 3 | `Avatar/` | OK |
| `button` (`index.ts`) | Button | 1 | `Button/` | OK |
| `checkbox` | Root, Indicator | 2 | `Checkbox/` | OK |
| `checkbox-group` (`index.ts`) | CheckboxGroup | 1 | `CheckboxGroup/` | OK |
| `collapsible` | Root, Trigger, Panel | 3 | `Collapsible/` | OK |
| `combobox` | Root, Value, Trigger, Chip, ChipRemove, Chips, Clear, Collection, Empty, Group, GroupLabel, Icon, Input, InputGroup, Item, ItemIndicator, Label, List, Popup, Portal, Positioner, Row, Status, Separator | 24 | `Combobox/` | OK |
| `context-menu` | Root, Trigger, Arrow, Backdrop, CheckboxItem, CheckboxItemIndicator, Group, GroupLabel, Item, LinkItem, Popup, Portal, Positioner, RadioGroup, RadioItem, RadioItemIndicator, SubmenuRoot, SubmenuTrigger, Separator (menu re-exports) | 19 | `ContextMenu/` | OK |
| `csp-provider` (`index.ts`) | CSPProvider | 1 | `Csp/` | OK |
| `dialog` | Root, Trigger, Portal, Backdrop, Popup, Title, Description, Close, Viewport, Handle | 10 | `Dialog/` | OK |
| `direction-provider` | Provider | 1 | `DirectionProvider/` | OK |
| `drawer` | Root, Trigger, Portal, Backdrop, Popup, Content, Title, Description, Close, Viewport, Indent, IndentBackground, Provider, SwipeArea, VirtualKeyboardProvider, Handle | 16 | `Drawer/` | OK |
| `field` | Root, Label, Control, Description, Error, Validity, Item | 7 | `Field/` | OK |
| `fieldset` | Root, Legend | 2 | `Fieldset/` | OK |
| `form` (`index.ts`) | Form | 1 | `Form/` | OK |
| `input` (`index.ts`) | Input | 1 | `Input/` | OK |
| `menu` | Root, Trigger, Portal, Backdrop, Positioner, Popup, Arrow, Item, LinkItem, Group, GroupLabel, RadioGroup, RadioItem, RadioItemIndicator, CheckboxItem, CheckboxItemIndicator, SubmenuRoot, SubmenuTrigger, Viewport, Separator, Handle | 21 | `Menu/` | OK |
| `menubar` (`index.ts`) | Menubar | 1 | `MenuBar/` | OK (naming, see §3) |
| `meter` | Root, Track, Indicator, Label, Value | 5 | `Meter/` | OK |
| `navigation-menu` | Root, List, Item, Trigger, Icon, Content, Link, Viewport, Portal, Positioner, Popup, Arrow, Backdrop | 13 | `NavigationMenu/` | OK |
| `number-field` | Root, Group, Input, Increment, Decrement, ScrubArea, ScrubAreaCursor | 7 | `NumberField/` | OK |
| `otp-field` | Root, Input, Separator | 3 | `OtpField/` | OK |
| `popover` | Root, Trigger, Portal, Backdrop, Positioner, Popup, Arrow, Title, Description, Close, Viewport, Handle | 12 | `Popover/` | OK |
| `preview-card` | Root, Trigger, Portal, Backdrop, Positioner, Popup, Arrow, Viewport, Handle | 9 | `PreviewCard/` | OK |
| `progress` | Root, Track, Indicator, Label, Value | 5 | `Progress/` | OK |
| `radio` | Root, Indicator | 2 | `Radio/` | OK |
| `radio-group` (`index.ts`) | RadioGroup | 1 | `RadioGroup/` | OK |
| `scroll-area` | Root, Viewport, Content, Scrollbar, Thumb, Corner | 6 | `ScrollArea/` | OK |
| `select` | Root, Trigger, Value, Icon, Backdrop, Portal, Positioner, Popup, Arrow, Item, ItemText, ItemIndicator, Group, GroupLabel, ScrollUpArrow, ScrollDownArrow, List, Label, Separator | 19 | `Select/` | OK |
| `separator` (`index.ts`) | Separator | 1 | `Separator/` | OK |
| `slider` | Root, Value, Control, Track, Indicator, Thumb, Label | 7 | `Slider/` | OK |
| `switch` | Root, Thumb | 2 | `Switch/` | OK |
| `tabs` | Root, List, Tab, Indicator, Panel | 5 | `Tabs/` | OK |
| `toast` | Provider, Portal, Viewport, Root, Content, Title, Description, Action, Close, Arrow, Positioner | 11 | `Toast/` | OK |
| `toggle` (`index.ts`) | Toggle | 1 | `Toggle/` | OK |
| `toggle-group` (`index.ts`) | ToggleGroup | 1 | `ToggleGroup/` | OK |
| `toolbar` | Root, Button, Link, Input, Group, Separator | 6 | `Toolbar/` | OK |
| `tooltip` | Provider, Root, Trigger, Portal, Positioner, Popup, Arrow, Viewport, Handle | 9 | `Tooltip/` | OK |

Non-component upstream directories (infrastructure, not part of the public component surface): `floating-ui-react/`, `internals/`, `types/`, `utils/`, plus utility entry points `merge-props/`, `use-render/`, `unstable-use-media-query/` (see §3).
