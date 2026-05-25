# Tabs Functional Audit

Date: 2026-05-25

## Scope

Component audited: `Tabs`

React source inspected:

- `.base-ui/packages/react/src/tabs/root/TabsRoot.tsx`
- `.base-ui/packages/react/src/tabs/root/TabsRootContext.ts`
- `.base-ui/packages/react/src/tabs/root/stateAttributesMapping.ts`
- `.base-ui/packages/react/src/tabs/root/TabsRootDataAttributes.ts`
- `.base-ui/packages/react/src/tabs/list/TabsList.tsx`
- `.base-ui/packages/react/src/tabs/list/TabsListContext.ts`
- `.base-ui/packages/react/src/tabs/list/TabsListDataAttributes.ts`
- `.base-ui/packages/react/src/tabs/tab/TabsTab.tsx`
- `.base-ui/packages/react/src/tabs/tab/TabsTabDataAttributes.ts`
- `.base-ui/packages/react/src/tabs/panel/TabsPanel.tsx`
- `.base-ui/packages/react/src/tabs/panel/TabsPanelDataAttributes.ts`
- `.base-ui/packages/react/src/tabs/indicator/TabsIndicator.tsx`
- `.base-ui/packages/react/src/tabs/indicator/TabsIndicatorCssVars.ts`
- `.base-ui/packages/react/src/tabs/indicator/TabsIndicatorDataAttributes.ts`
- `.base-ui/packages/react/src/tabs/indicator/prehydrationScript.min.ts`

Framework-agnostic spec created:

- `../base-ui-specs/tabs/SPEC.md`
- `../base-ui-specs/tabs/pitfalls.md`

Relevant Tabs parts found: `TabsRoot`, `TabsList`, `TabsTab`, `TabsPanel`, `TabsIndicator`.

## Implementation Plan

1. Compare React Tabs root, list, tab, panel, indicator, contexts, data attributes, and tests against the Blazor port.
2. Add failing bUnit contract tests for confirmed parity gaps.
3. Add Playwright assertions for browser-visible keyboard, disabled, modifier-key, and indicator behavior.
4. Repair Tabs C# and component-specific JS only.
5. Run focused unit tests, Playwright Tabs tests, solution build, lint, and diff checks with captured logs.

## Resolved Gaps

| Gap | React behavior | Blazor repair |
| --- | --- | --- |
| Controlled mode detection incomplete | `value !== undefined` controls selection even without a change callback | `TabsRoot` now tracks `Value` parameter presence in `SetParametersAsync` |
| Implicit initial selection missing | Omitted `defaultValue` triggers automatic initial selection/fallback and reason `initial` | Uncontrolled root now evaluates registered tabs and sends automatic `initial` reason |
| Explicit null default conflated with omission | `defaultValue={null}` selects no tab and sends no initial event | Root tracks explicit default presence and preserves null |
| Automatic fallback reasons missing | Disabled/missing selected tabs use reasons `disabled` and `missing` | Added `TabsValueChangeReason` and reason-aware value change args |
| Automatic changes cancelable | React ignores cancellation for automatic initial/disabled/missing changes | Automatic notifications bypass cancellation and commit internal state first |
| Controlled disabled/missing fallback incorrect | Controlled roots preserve parent value exactly | Automatic fallback is skipped when `Value` is supplied |
| Activation direction incomplete | Direction is computed from tab DOM order, with comparable value fallback | Root/tab/list now compute and propagate activation direction through context |
| Root orientation attr missing | Root emits `data-orientation` | Root now emits `data-orientation` |
| List data attrs missing | List emits `data-orientation` and `data-activation-direction` | List now emits both attributes |
| RTL keyboard branch missing | Horizontal previous/next reverse in RTL | List consumes `DirectionProviderContext` and swaps ArrowLeft/ArrowRight |
| Modifier arrows navigated | Composite ignores modifier-key navigation | C# and JS key handlers now ignore Shift/Ctrl/Alt/Meta arrows |
| Disabled tabs skipped by roving focus | Disabled tabs are focusable but not activatable | JS navigation includes disabled tabs; activation is blocked for disabled tabs |
| JS registration race | React composite list has registered items before navigation | Tabs JS queues tab registrations that arrive before list initialization and hydrates them later |
| Disabled active tab tabbable | React does not place disabled selected tabs in the tab order | `TabsTab` returns `tabindex=-1` when disabled |
| Non-main click activated tab | React activation is main-button only | Click activation checks `MouseEventArgs.Button == 0` |
| External handlers skipped | React still composes external handlers when activation is skipped | Click/focus additional handlers now run for active and disabled tabs |
| Panel `data-index` missing | Panels emit current composite index | Root tracks panel metadata and panels emit reindexed `data-index` |
| Panel hidden semantics incorrect | Exiting panels remain mounted and not `hidden` until exit completes | Panel mounted state now drives `hidden`/`data-hidden`; open state drives `tabIndex`/`inert` |
| Panel transition status missing | Panels emit `data-starting-style` and `data-ending-style` | Panel state carries `TransitionStatus`; JS waits for exit animations before unmount |
| No-motion panel exits painted visibly | React completes no-animation exits before visible stacked panels can paint | Tabs JS arms a no-motion handoff, waits until the incoming panel is visible, then hides the outgoing panel before paint |
| Panel inert missing | Inactive panels emit `inert` | Panels now emit `inert` when not open |
| Indicator orientation attr missing | Indicator emits `data-orientation` | Indicator now emits `data-orientation` |
| Indicator CSS vars mismatched | React uses `--active-tab-*` variables | Indicator now writes `--active-tab-left/right/top/bottom/width/height` |
| Indicator resize coverage incomplete | List and tabs notify all indicators on resize/register/unregister | JS resize observer now tracks list, tabs, and multiple indicator refs |
| Indicator null value behavior incomplete | Indicator returns null when value is null | Indicator render is disabled when root value is null |
| Indicator pre-hydration branch missing | `renderBeforeHydration` emits a static script after the indicator | `RenderBeforeHydration` now emits the static pre-hydration geometry script |

## Parity Matrix

| React hook/utility/branch | Blazor equivalent | Verification |
| --- | --- | --- |
| `useControlled({ controlled: valueProp, default: defaultValueProp })` | `SetParametersAsync` value-presence tracking plus `CurrentValue` | bUnit controlled value tests |
| `hasExplicitDefaultValueProp` | `hasExplicitDefaultValue` / `hasDefaultValueParameter` | bUnit explicit null and automatic initial tests |
| `shouldNotifyInitialValueChangeRef` | `shouldNotifyInitialValueChange` | bUnit initial reason and uncancelable automatic tests |
| `shouldHonorDisabledDefaultValueRef` | `shouldHonorDisabledDefaultValue` | bUnit disabled default/fallback tests |
| `didRegisterTabsRef` | `didRegisterTabs` | bUnit missing/disabled fallback tests |
| `notifyAutomaticValueChange` | `NotifyAutomaticValueChangeAsync` | bUnit reason assertions |
| `createChangeEventDetails` | `TabsValueChangeEventArgs<TValue>` | bUnit cancellation, reason, source-event tests |
| `computeActivationDirection` DOM branch | `TabsTab.DetectActivationDirectionAsync` / root registered order | bUnit and Playwright activation direction tests |
| `computeActivationDirection` comparable fallback | `TabsRoot.ComputeActivationDirection` comparable branch | bUnit controlled/dynamic value coverage |
| `TabsRootContext` value/id/panel callbacks | `TabsRootContext<TValue>` tab and panel maps | bUnit `aria-controls` / `aria-labelledby` tests |
| `CompositeList` panel metadata | `RegisterPanelInfo` and current `GetPanelIndexByValue` | bUnit `data-index` tests |
| `CompositeRoot` key handling | `TabsList.HandleKeyDownAsync` plus JS preventDefault/focus | Playwright keyboard navigation tests |
| `disabledIndices={EMPTY_ARRAY}` | JS roving focus includes disabled tabs | Playwright disabled focus tests |
| `useButton({ focusableWhenDisabled: true })` | `aria-disabled`, disabled activation guard, JS focusable roving item | bUnit and Playwright disabled tab tests |
| `onClick` active/disabled guard | `TabsTab.HandleClickAsync` | bUnit active/right-click tests |
| `onFocus` active/disabled/activateOnFocus guard | `TabsTab.HandleFocusAsync` and JS activation guard | bUnit focus handler and Playwright activate-on-focus tests |
| `registerTabResizeObserverElement` | `observeTabForIndicators` / `unobserveTabForIndicators` | Playwright indicator CSS variable test |
| `registerIndicatorUpdateListener` | JS `dotNetRefs` set per list | Playwright indicator update tests |
| `useTransitionStatus(open)` | `TabsPanel` `isMounted` + `TransitionStatus` | bUnit panel state tests |
| `useOpenChangeComplete` | JS `waitForPanelAnimations` and `OnPanelTransitionEnded` | bUnit transition data attrs and Playwright panel transition tests |
| `useAnimationsFinished` no-animation branch | Capture-phase JS no-motion detection plus DOM-mutation handoff to the incoming panel | Playwright no-animation switch tests in Server and WASM |
| `useAnimationsFinished` animated branch | JS `getAnimations()` wait with CSS motion fallback detection and open-panel cleanup after Blazor unmount | Playwright animated switch tests in Server and WASM |
| `inertValue(!open)` | `inert` emitted when panel is not open | bUnit inert test |
| `stateAttributesMapping` | `TransitionAttributeHelper` and component data attrs | bUnit attribute tests |
| `TabsIndicatorCssVars` | `BuildPositionStyle` with `--active-tab-*` | Playwright CSS variable test |
| `renderBeforeHydration` script | `PrehydrationScript` rendered after indicator when requested | bUnit pre-hydration script test |
| React `useRenderElement` | Shared `RenderElement<TState>` with state/class/style/render attrs | Existing render/class/style tests |

## Verification Commands

| Command | Result | Log |
| --- | --- | --- |
| `dotnet build BlazorBaseUI.slnx -v minimal` | Passed, 0 warnings, 0 errors | `docs/audits/tabs-artifacts/tabs-dotnet-build.log` |
| `dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --filter "FullyQualifiedName~Tabs" -v minimal` | Passed, 117/117 | `docs/audits/tabs-artifacts/tabs-bunit-tests.log` |
| `dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~Tabs" -v minimal` | Passed, 80/80 with 0 skipped | `docs/audits/tabs-artifacts/tabs-playwright-tests.log` |
| `dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~TabsTestsServer.DisabledTab_CanReceiveKeyboardFocusWithoutActivation\|FullyQualifiedName~TabsTestsServer.DisabledTab_NotActivatedOnFocusWithActivateOnFocus" -v minimal` | Passed, 2/2 | `docs/audits/tabs-artifacts/tabs-playwright-disabled-focus-targeted.log` |
| `bash scripts/lint-rules.sh` | Failed on non-Tabs pre-existing/tooling findings | `docs/audits/tabs-artifacts/tabs-lint-rules.log` |
| `git diff --check` | Passed | `docs/audits/tabs-artifacts/tabs-git-diff-check.log` |

## Lint Result

`scripts/lint-rules.sh` failed in this macOS environment with repeated BSD `grep` errors for `grep -P`, then reported three Rule 05 violations outside Tabs:

- `src/BlazorBaseUI/wwwroot/blazor-baseui-scroll-lock.js:257`
- `src/BlazorBaseUI/wwwroot/blazor-baseui-scroll-lock.js:268`
- `src/BlazorBaseUI/Popover/PopoverPositioner.razor:11`

No Tabs file was reported by the lint summary.

## Manual Checks

- Confirmed every React Tabs data attribute has a Blazor equivalent: root/list/tab/panel/indicator `data-orientation`, shared `data-activation-direction`, tab `data-active`/`data-disabled`, panel `data-index`/`data-hidden`/transition attrs.
- Confirmed ARIA roles and relationships: list `role=tablist`, tabs `role=tab` with `aria-selected` and `aria-controls`, panels `role=tabpanel` with `aria-labelledby`, indicator `role=presentation`.
- Confirmed DOM-heavy behavior remains in JS: roving focus, key `preventDefault`, tab geometry, indicator resize, pre-hydration geometry, and transition completion.
- Confirmed panel switching keeps exactly one visible panel when no CSS motion is present, and animated exits remain visible until their CSS animation completes.
- Confirmed no `RenderFragment` caching was introduced.
- Confirmed unrelated dirty file `CLAUDE.md` was not modified by this audit and must not be staged.
