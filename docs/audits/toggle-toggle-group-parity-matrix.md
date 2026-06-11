# Toggle and ToggleGroup Parity Matrix

## Toggle

| React source behavior | Blazor equivalent | Verification |
| --- | --- | --- |
| `valueProp || undefined` feeds `useBaseUiId`; empty string generates an ID. | `ResolveValue()` treats null and empty as generated stable IDs. | `GroupedEmptyStringValues_AreResolvedToUniqueGeneratedValues` |
| Standalone controlled/uncontrolled pressed state via `useControlled`. | `Pressed` controls state; `DefaultPressed` initializes local state. | Existing and refreshed Toggle bUnit/Playwright tests |
| Grouped pressed state derives from `groupContext.value.includes(value)`. | `CurrentPressed` checks `GroupContext.Value.Contains(resolvedValue)`. | `ControlledValue_SetsPressedToggles`, `UncontrolledDefaultValue_SetsPressedToggles` |
| `form` is consumed and never reaches DOM. | `BuildForwardedAdditionalAttributes()` strips `form`. | `NativeButton_OverridesUserTypeAndOmitsForm` |
| User `type` cannot override `type="button"`. | Component attributes win over additional attributes. | `NativeButton_OverridesUserTypeAndOmitsForm` |
| Native standalone disabled uses `disabled`, not `aria-disabled`. | Standalone native disabled emits `disabled`; no `aria-disabled`. | Existing Toggle tests |
| Grouped native disabled exposes `aria-disabled`. | Grouped native toggles emit `aria-disabled="true|false"`. | `GroupedNativeToggle_ExposesAriaDisabledState` |
| Non-native button emits role, tab index, keyboard activation. | Non-native mode emits `role="button"`, `tabindex`, JS keydown/keyup activation. | Toggle Playwright keyboard tests |
| `data-pressed` and `data-disabled` are presence-only. | Attributes are emitted only when true. | Toggle bUnit and Playwright attribute tests |
| Click creates Base UI change details with reason `none`, event, cancellation, propagation state. | `TogglePressedChangeEventArgs` exposes `Reason`, `Event`, `Cancel`, `AllowPropagation`, state flags. | Event args construction and grouped callback tests |
| Grouped click calls group value handler, then local `onPressedChange`, then aborts local state on cancel. | `HandleClickAsync` calls `GroupContext.SetGroupValueAsync`, then `OnPressedChange` with shared cancellation state. | `GroupedToggle_OnPressedChangeFiresOnClick`, group cancellation tests |
| In ToggleGroup, item participates in composite focus. | Normal group mode registers with ToggleGroup JS and C# render-time item order. | ToggleGroup keyboard Playwright tests |
| In Toolbar-contained ToggleGroup, item participates in Toolbar focus. | Toolbar mode registers Toggle elements with `ToolbarRootContext` and suppresses ToggleGroup key handlers. | `ToolbarMode_ArrowNavigationUsesToolbarRoot` |

## ToggleGroup

| React source behavior | Blazor equivalent | Verification |
| --- | --- | --- |
| Default element `div`, root role `group`. | `RenderElement` default tag `div`, `role="group"`. | `RendersAsDivByDefault`, `HasRoleGroup` |
| Controlled `value` or uncontrolled `defaultValue`; missing default is empty array. | `Value`, `DefaultValue`, `internalValue`. | Value control bUnit tests |
| `isValueInitialized` true when `value` or `defaultValue` supplied. | `SetParametersAsync` tracks supplied parameters and exposes `IsValueInitialized`. | Context and missing value coverage |
| Disabled resolves from local prop or Toolbar root. | `ResolvedDisabled => Disabled || ToolbarContext.Disabled`. | `ToolbarDisabled_DisablesToggleGroupAndChildren` |
| `multiple=false`: new value is `[newValue]` or `[]`. | `SetGroupValueInternalAsync` writes single-item or empty list. | Single-mode bUnit/Playwright tests |
| `multiple=true`: append on press; remove first matching value on depress. | No dedupe on press; `RemoveAt(index)` on depress. | `Multiple_AllowsMultiplePressed` |
| `onValueChange` receives event details and can cancel internal update. | `ToggleGroupValueChangeEventArgs` supports event detail parity and `Cancel()`. | `OnValueChange_CanBeCanceled` |
| `data-disabled`, `data-orientation`, `data-multiple` derive from state. | Presence/data value attributes emitted from `BuildComponentAttributes()`. | ToggleGroup attribute bUnit tests |
| `aria-orientation` comes from `CompositeRoot` outside Toolbar. | Blazor emits `aria-orientation` only when not inside Toolbar. | `HasAriaOrientation*`, Toolbar disabled test |
| Outside Toolbar, group owns composite roving focus. | JS `initializeGroup`, `registerToggle`, `syncGroupTabIndexes`, navigation functions. | ToggleGroup Server keyboard Playwright tests |
| Initial highlighted item is first enabled item, not pressed item. | C# and JS tab-index sync use first enabled until user-highlighted item exists. | `InitialRovingTabIndex_UsesFirstEnabledToggleNotPressedToggle`, `Tab_FocusesFirstEnabledToggle` |
| Horizontal RTL reverses left/right. | `HandleKeyDownAsync` maps RTL ArrowLeft to next and ArrowRight to previous. | `RtlHorizontal_ArrowLeftMovesToNextToggle` |
| Cross-axis keys ignored. | JS prevent/default and Blazor navigation only handle orientation keys. | `HorizontalOrientation_IgnoresVerticalArrowKeys` |
| Home/End enabled. | JS `navigateToFirst`/`navigateToLast`. | `HomeEnd_MoveFocusToFirstAndLastToggle` |
| Disabled items skipped. | JS `isToggleDisabled()` checks `data-disabled`, `disabled`, and `aria-disabled="true"`. | `ArrowNavigation_SkipsDisabledToggle` |
| Toolbar branch does not create nested group composite root. | `ToggleGroup` skips ToggleGroup JS initialization in Toolbar; Toggle registers with Toolbar root. | `ToolbarMode_ArrowNavigationUsesToolbarRoot` |

## Source Docs Parity Addendum

| React docs/source behavior | Blazor docs equivalent | Verification |
| --- | --- | --- |
| Toggle public hero renders an icon-only `Favorite` button with an accessible name. | Toggle demo icon-only buttons now expose `Favorite`, `Star`, and `Bookmark` names. | In-app browser source-docs check: `namelessPressedButtons: 0` on `/toggle`. |
| ToggleGroup public multiple demo labels icon-only buttons and labels the group as `Text formatting options`. | Icon Toggle Group now has `aria-label="Text formatting options"` and button names `Bold`, `Italic`, `Underline`, `Strikethrough`. | In-app browser source-docs check on `/toggle-group`. |
| ToggleGroup public alignment hero labels icon-only alignment buttons. | Alignment Toggle Group now has `aria-label="Text alignment"` and named alignment buttons. | In-app browser source-docs check on `/toggle-group`. |
| Private ToggleGroup experiment prevents controlled single-select from becoming empty. | Required Selection demo uses controlled value plus `OnValueChange.Cancel()` for empty values. | In-app browser check confirmed `Align center` stayed `aria-pressed="true"` after clicking it. |
| React source tests use `DirectionProvider` to reverse horizontal RTL keyboard navigation. | RTL Direction demo wraps ToggleGroup in Blazor `DirectionProvider Direction="Direction.Rtl"`. | In-app browser check confirmed ArrowLeft moved focus from `Toggle bold` to `Toggle italic`. |
