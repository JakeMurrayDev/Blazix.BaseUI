# Switch Parity Matrix

Date: 2026-05-30

Component scope: `Switch.Root`, `Switch.Thumb`

React source audited:

- `.base-ui/packages/react/src/switch/root/SwitchRoot.tsx`
- `.base-ui/packages/react/src/switch/root/SwitchRootContext.ts`
- `.base-ui/packages/react/src/switch/thumb/SwitchThumb.tsx`
- `.base-ui/packages/react/src/switch/stateAttributesMapping.ts`
- `.base-ui/packages/react/src/switch/root/SwitchRootDataAttributes.ts`
- `.base-ui/packages/react/src/switch/thumb/SwitchThumbDataAttributes.ts`
- `.base-ui/packages/react/src/switch/root/SwitchRoot.test.tsx`
- `.base-ui/packages/react/src/switch/thumb/SwitchThumb.test.tsx`

## Hook and Utility Matrix

| React source behavior | Blazor equivalent | Status |
| --- | --- | --- |
| `useControlled` for `checked` | `Checked.HasValue` controlled mode, otherwise `DefaultChecked` initializes `isChecked` | Verified |
| `useBaseUiId` | `switchId = Guid.NewGuid().ToIdString()` for the internal non-native root id | Verified |
| `useLabelableId` | `ResolvedControlId` from explicit `id` or generated fallback, assigned to hidden input or native button | Verified |
| `useLabelableContext` | `LabelableContext` cascade for label id, description ids, and control id registration | Verified |
| `useAriaLabelledBy` explicit value | `ExplicitAriaLabelledBy` prevents generated label id assignment | Verified |
| `useAriaLabelledBy` field label | `LabelableContext.LabelId` becomes root `aria-labelledby` when no explicit value exists | Verified |
| `useAriaLabelledBy` native label fallback | JS resolves native label association from the hidden input and updates root `aria-labelledby` | Verified |
| `useButton` non-native | JS handles click, Enter, Space, visible-root click callback dispatch, disabled blocking, and read-only callback-preserving blocking for non-native root | Verified |
| `useButton` native | Native button root with `type="button"` and HTML `disabled` when disabled | Verified |
| `useFocusableWhenDisabled` behavior | Non-native disabled root has `aria-disabled`, `tabindex=-1`, and JS propagation suppression | Verified |
| `ownerWindow(PointerEvent)` click dispatch | JS dispatches `PointerEvent` or `MouseEvent` to the hidden input with modifier keys | Verified |
| `useMergedRefs` input ref | Component exposes `InputElement`; field validation registration receives current value and validation callback | Verified |
| `useRegisterFieldControl` | `RegisterFieldControl` registers id, field-first resolved `Name`, value getter, validation callback, and root focus callback | Verified |
| `useFormContext.clearErrors` | `FormContext?.ClearErrors(ResolvedName)` on committed checked changes | Verified |
| `useValueChanged` | Blazor parameter/state lifecycle calls `HandleCheckedChanged` when the checked value changes | Verified |
| `useIsoLayoutEffect` initial filled sync | `OnInitialized` sets initial validation value and filled state from `CurrentChecked` | Verified |
| `validation.getValidationProps` | Root receives `aria-invalid`, field validity data attributes, and combined root descriptions | Verified |
| `validation.getInputValidationProps` | Hidden input receives field description ids through `aria-describedby` | Verified |
| `validation.change` | `HandleCheckedChanged` commits change validation, including debounce support | Verified |
| `validation.commit` on blur | `HandleBlur` commits blur validation with the current checked value | Verified |
| `createChangeEventDetails(REASONS.none, event)` | `SwitchCheckedChangeEventArgs` exposes `Reason=None`, `Cancel`, `IsCanceled`, and current-event modifier keys with stale details cleared on ignored changes | Verified |
| `visuallyHidden` | Fixed hidden style used when no `name` is present | Verified |
| `visuallyHiddenInput` | Absolute hidden input style used when `name` is present | Verified |
| `mergeProps` default/user/validation merge | Root builder omits generated attributes when user attributes are supplied and combines descriptions | Verified |
| `useRenderElement` | `RenderElement<SwitchRootState>` and `RenderElement<SwitchRootState>` for root and thumb render replacement, with generated thumb state attributes skipped when user attributes are supplied | Verified |
| `SwitchRootContext.Provider` | `CascadingValue` publishes fresh `SwitchRootContext` on state changes | Verified |
| `useSwitchRootContext` missing-context guard | `SwitchThumb.OnParametersSet` throws outside `SwitchRoot` | Verified |
| `stateAttributesMapping` | Root and thumb emit checked, unchecked, disabled, readonly, required, valid, invalid, touched, dirty, filled, focused data attributes | Verified |
| `EMPTY_OBJECT` value omission | Razor omits `value` when `Value` is null | Accounted |
| `suppressHydrationWarning` | No direct Blazor equivalent required for the server/WASM render paths used here | Accounted |

## Attribute Matrix

| Part | React attributes | Blazor status |
| --- | --- | --- |
| `Switch.Root` non-native root | default `span` | Present |
| `Switch.Root` native root | `button type="button"` when `nativeButton=true` | Present |
| `Switch.Root` root | internal id for non-native mode | Present |
| `Switch.Root` root | explicit/generated control id for native button mode | Present |
| `Switch.Root` root | `role="switch"` unless overridden | Present |
| `Switch.Root` root | `aria-checked="true\|false"` unless overridden | Present |
| `Switch.Root` root | `aria-disabled="true"` for disabled non-native root | Present |
| `Switch.Root` root | native `disabled` on native button mode | Present |
| `Switch.Root` root | `aria-readonly="true"` when read-only | Present |
| `Switch.Root` root | `aria-required="true"` when required | Present |
| `Switch.Root` root | explicit, field-label, or native-label fallback `aria-labelledby` | Present |
| `Switch.Root` root | external plus field-description `aria-describedby` | Present |
| `Switch.Root` root | `aria-invalid="true"` when invalid unless overridden | Present |
| `Switch.Root` root | `tabindex="0"` or `-1` for disabled non-native mode | Present |
| `Switch.Root` root | focus and blur handlers | Present |
| `Switch.Root` root | click handler via JS interop and user `@onclick` preservation | Present |
| `Switch.Root` root | state data attributes | Present |
| `Switch.Root` checkbox input | `type="checkbox"` | Present |
| `Switch.Root` checkbox input | `id` only when not native button | Present |
| `Switch.Root` checkbox input | `checked` | Present |
| `Switch.Root` checkbox input | `disabled` | Present |
| `Switch.Root` checkbox input | `required` | Present |
| `Switch.Root` checkbox input | `form` | Present |
| `Switch.Root` checkbox input | `aria-hidden="true"` | Present |
| `Switch.Root` checkbox input | field-description `aria-describedby` | Present |
| `Switch.Root` checkbox input | `tabindex="-1"` | Present |
| `Switch.Root` checkbox input | visually hidden style | Present |
| `Switch.Root` checkbox input | `name` from `Field.Root` first, then root `Name` | Present |
| `Switch.Root` checkbox input | `value` only when supplied | Present |
| `Switch.Root` unchecked input | `type="hidden"` | Present |
| `Switch.Root` unchecked input | `form`, `name`, `value` | Present |
| `Switch.Thumb` root | default `span` | Present |
| `Switch.Thumb` root | render replacement | Present |
| `Switch.Thumb` root | state data attributes unless user-supplied attributes override them | Present |

## Behavior Matrix

| Behavior | Coverage |
| --- | --- |
| Click toggles uncontrolled switch | Server and WASM Playwright; bUnit |
| Hidden input click toggles switch | Server and WASM Playwright; bUnit |
| Enter toggles non-native switch | Server and WASM Playwright |
| Space toggles non-native switch | Server and WASM Playwright |
| Enter invokes non-native visible-root click callback | Server and WASM Playwright |
| Space invokes non-native visible-root click callback | Server and WASM Playwright |
| Native button responds to Enter and Space | Server and WASM Playwright; bUnit |
| Disabled switch does not toggle by click or keyboard | Server and WASM Playwright; bUnit |
| Disabled non-native root does not invoke user click callback | Server and WASM Playwright |
| Read-only switch does not toggle by click or keyboard | Server and WASM Playwright; bUnit |
| Visible root click callback still runs for read-only roots | Server and WASM Playwright |
| Read-only keyboard activation invokes click callback without toggling | Server and WASM Playwright |
| Focus redirects from hidden input to visible root | Server and WASM Playwright |
| Controlled external state updates root and thumb state | Server and WASM Playwright; bUnit |
| `OnCheckedChange` cancellation prevents state change | bUnit |
| `OnCheckedChange` receives modifier keys | Server and WASM Playwright; bUnit |
| Ignored input changes clear captured modifier details | bUnit |
| Field errors keyed by `SwitchRoot.Name` apply to the switch | bUnit |
| `Field.Root.Name` takes precedence over `SwitchRoot.Name` for submitted input name | bUnit |
| Field disabled state flows into switch disabled state | bUnit and source audit |
| Field description ids are applied to root and input | bUnit |
| Native wrapping label toggles switch | Server and WASM Playwright |
| Explicit native label toggles non-native switch through hidden input | Server and WASM Playwright |
| Native-label fallback sets root `aria-labelledby` | Server and WASM Playwright |
| Native-label fallback updates when input id changes | Server and WASM Playwright |
| Native button label targets the button id and not hidden input id | Server and WASM Playwright |
| Form submission includes checked value and custom value | Server and WASM Playwright |
| Form submission excludes unchecked native checkbox value by default | Server and WASM Playwright |
| Form submission includes unchecked value when supplied | Server and WASM Playwright |
| External form ownership through `form` | Server and WASM Playwright; bUnit |
| Root and thumb data attributes update together | Server and WASM Playwright; bUnit |
| User-supplied thumb data attributes override generated state attributes | bUnit |
| `Switch.Thumb` throws outside `Switch.Root` | bUnit |

## Source Accounting

| React source file | Blazor file(s) | Status |
| --- | --- | --- |
| `switch/root/SwitchRoot.tsx` | `SwitchRoot.razor`, `EventArgs.cs`, `Enumerations.cs`, `blazor-baseui-switch.js` | Verified |
| `switch/root/SwitchRootContext.ts` | `SwitchRootContext.cs` | Verified |
| `switch/thumb/SwitchThumb.tsx` | `SwitchThumb.razor` | Verified |
| `switch/stateAttributesMapping.ts` | `SwitchRootState`, root and thumb attribute builders | Verified |
| `switch/root/SwitchRootDataAttributes.ts` | root attribute builder | Verified |
| `switch/thumb/SwitchThumbDataAttributes.ts` | thumb attribute builder | Verified |
| React root tests | `SwitchRootTests.cs`, `SwitchTestsBase.cs` | Verified |
| React thumb tests | `SwitchThumbTests.cs`, `SwitchRootTests.cs`, `SwitchTestsBase.cs` | Verified |
