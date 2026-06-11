# Select Parity Matrix

Date: 2026-06-10
Source root: `.base-ui/packages/react/src/select`
Port root: `src/Blazix.BaseUI/Select`

| React source item | Required behavior | Blazor equivalent | Status |
| --- | --- | --- | --- |
| `SelectRoot.tsx` | Canonical value/open/mounted/item/form state | `SelectRoot.razor`, `SelectRootContext.cs` | Verified |
| `store.ts` selectors | Fine-grained selected, item, popup, label, side, transition state | Context properties plus `StateChanged` dispatch | Verified |
| `SelectTrigger.tsx` | Combobox ARIA, field attrs, popup state attrs, mouseup gate, cancel-open, forwarded handlers | `SelectTrigger.razor`, `SelectTriggerState.cs`, JS event helpers | Repaired and verified |
| `useButton` | Native button semantics and disabled/read-only interaction blocking | Trigger button attributes and guarded event paths | Verified |
| `useTimeout` in trigger | Deferred mouseup listener and 400 ms selection gate | C# delay/cancellation and JS mouseup handling | Repaired and verified |
| `useMergedRefs`, `useValueAsRef`, `useStableCallback` | Stable element refs and callbacks | ElementReference registration, context setters, guarded handlers | Verified |
| `useFieldRootContext`, `useLabelableContext`, `useLabelableId` | Field label/description/name/disabled/invalid integration | Existing Blazor field and label contexts | Verified |
| `SelectValue.tsx` | Placeholder, label formatting, object and multiple values | `SelectValue.razor` | Verified |
| `SelectIcon.tsx` | Open state data attributes | `SelectIcon.razor` | Verified |
| `SelectPortal.tsx` | Mounted/force-mounted portal behavior | `SelectPortal.razor` | Verified |
| `SelectBackdrop.tsx` | Internal backdrop open/mounted/transition state | `SelectBackdrop.razor`, select JS | Verified |
| `SelectPositioner.tsx` | Floating position, side/align attrs, align-item fallback, resize behavior | `SelectPositioner.razor`, `blazix-baseui-floating.js`, `blazix-baseui-select.js` | Repaired and verified |
| `useAnchorPositioning` | DOM geometry, physical side, available size, transform origin | Shared floating JS plus Select positioner callbacks | Repaired and verified |
| `SelectPopup.tsx` | Listbox/presentation role switching, focus, transition attrs, popup ref | `SelectPopup.razor`, select JS | Repaired and verified |
| Popup utils | Align selected/first item to trigger and release positioning after geometry | `blazix-baseui-select.js` scheduler and watchdog | Repaired and verified |
| `SelectArrow.tsx` | Arrow side/align/open attrs and CSS vars | `SelectArrow.razor` | Verified |
| `SelectList.tsx` | Own listbox role when present, list ref lifetime | `SelectList.razor` | Repaired and verified |
| `SelectGroup.tsx` | Group context propagation | `SelectGroup.razor`, `SelectGroupContext.cs` | Verified |
| `SelectGroupLabel.tsx` | Label id registration, no role | `SelectGroupLabel.razor`, group context | Repaired and verified |
| `SelectItem.tsx` | Option role, disabled/read-only guards, highlighted/selected state, pointer selection | `SelectItem.razor` | Repaired and verified |
| `SelectItemText.tsx` | Text ref registration and selected text display | `SelectItemText.razor` | Verified |
| `SelectItemIndicator.tsx` | Selected transition mount/unmount state | `SelectItemIndicator.razor` | Repaired and verified |
| `SelectScrollArrow.tsx` | Scroll arrow visibility and hover scroll behavior | `SelectScrollUpArrow.razor`, `SelectScrollDownArrow.razor`, select JS | Verified |
| `useTransitionStatus` | Open/closed/starting/ending transition state | Component transition state and `OnTransitionEnd` | Repaired and verified |
| `useOpenChangeComplete` | Completion callback after current transition only | `SelectRoot.razor`, indicator/arrow completion handling | Repaired and verified |
| `useIsoLayoutEffect` | Layout-time DOM registration and cleanup | `OnAfterRenderAsync`, dispose methods, JS registration | Repaired and verified |
| `useRenderElement` | Custom render element, state, attrs, class/style functions | `RenderElement<TState>` across Select parts | Verified |
| Data attribute files | Source data attributes emitted only when applicable | Component state dictionaries and tests | Repaired and verified |
| ARIA attributes | String boolean ARIA and source role ownership | Component attributes and bUnit assertions | Repaired and verified |
| Hidden inputs | Native form, autofill, validation, multiple serialization | `SelectRoot.razor` form input rendering/change handling | Repaired and verified |
| Source docs | Docs output matches source | PNPM docs validation | Verified |
| Source tests | React source baseline used for comparison | PNPM jsdom/typescript/browser logs | Accounted |

## Source Component Test Coverage

| Source test group | Chromium | Firefox | WebKit | Notes |
| --- | --- | --- | --- | --- |
| Root | Pass | 1 source-runner failure | 1 source-runner failure | Failures are in React source browser run, not Blazor port |
| Trigger | Pass | Pass | Pass | Covered in Blazor bUnit and Playwright |
| Popup | Pass | Pass | Pass | Covered in Blazor bUnit and Playwright |
| Positioner | Pass | Pass | Pass | Covered in Blazor bUnit and Playwright |
| Item | Pass | Pass | Pass | Covered in Blazor bUnit |
| Value | Pass | Pass | Pass | Covered in Blazor bUnit |
| List | Pass | Pass | Pass | Covered in Blazor bUnit |
| Group/GroupLabel | Pass | Pass | Pass | Covered in Blazor bUnit |
| ItemText/Indicator | Pass | Pass | Pass | Covered in Blazor bUnit |
| Arrow/Scroll arrows/Icon/Portal/Backdrop | Pass | Pass | Pass | Covered by source and Blazor tests where ported |

## Validation Judgment

The Blazor Select port now has verified equivalents for all React source hooks, utilities, contexts, attributes, and DOM-heavy behaviors identified during the audit. DOM geometry and focus behaviors remain in JavaScript by design and are covered by browser tests.
