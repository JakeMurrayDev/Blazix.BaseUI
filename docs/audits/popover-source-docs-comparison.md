# Popover Source Docs Comparison

Date: 2026-06-07

## Scope

Compared the audited Blazor Popover component against the vendored React Base UI Popover documentation and generated API docs under `.base-ui/docs/src/app/(docs)/react/components/popover`.

The comparison used the upstream docs application through PNPM, the in-app browser, the generated `types.md` API reference, and the local Blazor component surface under `src/BlazorBaseUI/Popover`.

## Source Docs Commands

| Command | Result | Log |
| --- | --- | --- |
| `pnpm --version` | GREEN: `11.1.3` available. | Console inspection |
| `pnpm -C .base-ui docs:dev` | GREEN: Base UI docs app started on `http://localhost:3005`, Next.js reported ready. | `docs/audits/logs/popover-source-docs-dev.log` |
| In-app browser inspection of `http://127.0.0.1:3005/react/components/popover` | GREEN: page title `Popover · Base UI`; API reference and all expected headings loaded; no page errors. | `docs/audits/logs/popover-source-docs-browser.log` |
| `pnpm -C .base-ui docs:validate "(docs)/react/components/popover"` | GREEN: processed one `page.mdx` file and one `types.md` file; no files needed updating. | `docs/audits/logs/popover-source-docs-validate.log` |

## Source Docs Inventory

| React docs section | Documented behavior | Blazor equivalent | Status |
| --- | --- | --- | --- |
| Anatomy | `Root`, `Trigger`, `Portal`, `Backdrop`, `Positioner`, `Popup`, `Arrow`, `Viewport`, `Title`, `Description`, `Close`. | Matching Popover parts exist under `src/BlazorBaseUI/Popover`. | Verified |
| Opening on hover | `openOnHover`, `delay`, and hover-triggered open timing. | `PopoverTrigger.OpenOnHover`, `Delay`, `CloseDelay`, hover timers and hover/click engagement in component JS. | Verified by Popover Playwright and bUnit suites |
| Detached triggers | `Popover.createHandle()` connects triggers outside `Root`. | `PopoverHandleFactory.CreateHandle()` and `PopoverHandle<TPayload>` connect detached `PopoverTrigger` and `PopoverRoot`. | Verified by multi-trigger Playwright tests |
| Multiple triggers | Shared handle or contained triggers; active trigger determines content. | Contained and detached trigger registry, active trigger id, payload, and positioner re-anchor behavior. | Verified by multi-trigger Playwright tests |
| Payload child rendering | Root children can receive `payload` from active trigger. | `PopoverRoot.ChildContent` receives `PopoverRootPayloadContext` with active `Payload`. | Verified by bUnit and Playwright payload tests |
| Controlled mode with multiple triggers | Controlled `open`, `onOpenChange`, `triggerId`, and trigger `id`. | `Open`, `OpenChanged`, `OnOpenChange`, `TriggerId`, `DefaultTriggerId`, and trigger `Id`. | Verified by bUnit root tests and Playwright controlled scenarios |
| Animating Popover | Positioner and popup expose side, align, transition, width, height, and viewport content transition data. | Positioner, popup, and viewport emit matching transition, side/align, instant, activation direction, and CSS variable state. | Verified by Popover viewport, transition, and positioning tests |
| API reference | Generated `types.md` documents Root, Trigger, Portal, Backdrop, Positioner, Popup, Arrow, Title, Description, Close, Viewport, `createHandle`, and `Handle`. | Blazor API surface maps each React part to a Razor component or handle class. | Verified |

## API Surface Comparison

| React docs API | Blazor API | Notes |
| --- | --- | --- |
| `Popover.Root.defaultOpen` | `PopoverRoot.DefaultOpen` | Same uncontrolled initial-open responsibility. |
| `Popover.Root.open` | `PopoverRoot.Open` plus `OpenChanged` | Blazor bindable parameter replaces React controlled prop pattern. |
| `Popover.Root.onOpenChange` | `PopoverRoot.OnOpenChange` with `PopoverOpenChangeEventArgs` | Includes `Open`, `Reason`, `Cancel()`, `AllowPropagation()`, `IsCanceled`, `IsPropagationAllowed`, and `PreventUnmountOnClose()` through shared `OpenChangeEventArgs<TReason>`. Browser `event` and raw `trigger` element are not exposed as C# objects; trigger identity is modeled through active trigger id and handle state. |
| `Popover.Root.actionsRef` | `PopoverRoot.ActionsRef` with `PopoverRootActions` | Supports `Unmount` and `Close`. |
| `Popover.Root.defaultTriggerId` | `PopoverRoot.DefaultTriggerId` | Same initial active-trigger association. |
| `Popover.Root.handle` | `PopoverRoot.Handle` | Uses `IPopoverHandle` / `PopoverHandle<TPayload>`. |
| `Popover.Root.modal` | `PopoverRoot.Modal` | Blazor enum `PopoverModalMode.False`, `True`, `TrapFocus` maps React `false`, `true`, and `'trap-focus'`. |
| `Popover.Root.onOpenChangeComplete` | `PopoverRoot.OnOpenChangeComplete` | Same post-transition callback responsibility. |
| `Popover.Root.triggerId` | `PopoverRoot.TriggerId` | Same controlled active trigger association. |
| `Popover.Root.children` payload render function | `RenderFragment<PopoverRootPayloadContext> ChildContent` | Blazor-native render fragment equivalent. No RenderFragment caching is used. |
| `Popover.Trigger.handle` | `PopoverTrigger.Handle` | Connects detached triggers. |
| `Popover.Trigger.nativeButton` | `PopoverTrigger.NativeButton` | Preserves native/non-native trigger semantics. |
| `Popover.Trigger.payload` | `PopoverTrigger.Payload` | Supports typed handle payloads through `PopoverHandle<TPayload>`. |
| `Popover.Trigger.openOnHover` | `PopoverTrigger.OpenOnHover` | Same hover-open opt-in. |
| `Popover.Trigger.delay` | `PopoverTrigger.Delay` | Same default of 300 ms. |
| `Popover.Trigger.closeDelay` | `PopoverTrigger.CloseDelay` | Same default of 0 ms. |
| `Popover.Trigger.id` | `PopoverTrigger.Id` | Forwarded to rendered element and used for active trigger selection. |
| `className` / `style` function props | `ClassValue` / `StyleValue` plus `AdditionalAttributes` | Blazor uses state functions for component-owned class/style and attribute splatting for literal `class` / `style`. |
| `render` prop | `Render` parameter using `RenderElement<TState>` | Blazor render-prop equivalent. |
| `Popover.Portal.container` | `PopoverPortal.Container` | Blazor accepts a CSS selector string and defaults to `body`; React accepts DOM element, shadow root, ref, or null. This is an intentional interop shape difference, with equivalent portal-target behavior in Blazor. |
| `Popover.Portal.keepMounted` | `PopoverPortal.KeepMounted` | Same keep-mounted behavior. |
| Positioner `side`, `align`, offsets, arrow padding, anchor, collision, sticky, method | `PopoverPositioner` parameters | Side, align, side/align offset numbers and callbacks, collision boundary/padding/avoidance, anchor, sticky, and absolute/fixed positioning are present. |
| Popup `initialFocus` / `finalFocus` | `PopoverPopup.InitialFocus` / `FinalFocus` | Blazor focus options map into component JS focus handling. |
| `Popover.Close.nativeButton` | `PopoverClose.NativeButton` | Same native/non-native close semantics. |
| `Popover.createHandle()` | `PopoverHandleFactory.CreateHandle()` | Factory creates non-generic or typed handle. |
| `Popover.Handle.isOpen` | `IPopoverHandle.IsOpen` | Same readonly state visibility. |

## Attribute Comparison

| Element | React docs attributes | Blazor status |
| --- | --- | --- |
| Trigger | `data-popup-open`, `data-pressed`; audited source also requires `type`, `tabindex`, `aria-haspopup`, `aria-expanded`, `aria-controls`, disabled state, and click-trigger marker. | Present and covered by bUnit trigger tests plus Playwright click, hover, disabled, and focus tests. |
| Backdrop | `data-open`, `data-closed`, `data-starting-style`, `data-ending-style`. | Present and covered by backdrop/modal tests. |
| Positioner | `data-open`, `data-closed`, `data-align`, `data-side`, `data-anchor-hidden`, transition attrs, CSS variables. | Present and covered by positioning, transition, and multi-trigger tests. |
| Popup | `data-open`, `data-closed`, `data-align`, `data-instant`, `data-side`, `data-starting-style`, `data-ending-style`, `--popup-height`, `--popup-width`. | Present and covered by popup, viewport, focus, transition, and browser modal probe logs. |
| Arrow | `data-open`, `data-closed`, `data-uncentered`, `data-align`, `data-side`. | Present through positioner context and covered by arrow tests. |
| Viewport | `data-activation-direction`, `data-current`, `data-instant`, `data-previous`, `data-transitioning`, popup dimension CSS variables. | Present and covered by viewport bUnit and Playwright tests. |
| Title / Description / Close | Render/class/style APIs; title and description register popup ARIA ids; close emits close reason. | Present and covered by popup ARIA and close tests. |

## Consumer Test Coverage

Local source-level consumers of Popover types outside `src/BlazorBaseUI/Popover` are:

| Consumer | Popover dependency | Test command | Result |
| --- | --- | --- | --- |
| Tooltip | Uses Popover state/enumeration types for popup, trigger, arrow, positioner, and context parity. | `dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --filter "FullyQualifiedName~BlazorBaseUI.Tests.Tooltip|FullyQualifiedName~BlazorBaseUI.Tests.PreviewCard" -v minimal` | GREEN: 171 passed |
| PreviewCard | Uses Popover state/enumeration types for popup, arrow, and positioner parity. | Same bUnit command above. | GREEN: included in 171 passed |
| Tooltip | Browser behavior for hover, focus, close, actions, portal, and positioning. | `dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~BlazorBaseUI.Playwright.Tests.Tests.Tooltip|FullyQualifiedName~BlazorBaseUI.Playwright.Tests.Tests.PreviewCard" -v minimal` | GREEN: 124 passed |
| PreviewCard | Browser behavior for hover, focus, close, actions, portal, and positioning. | Same Playwright command above. | GREEN: included in 124 passed |

An exploratory broad bUnit command was also run for `Tooltip|PreviewCard|Select|Toolbar|Tabs|NavigationMenu`. It failed with 49 unrelated failures because the filter selected entire non-consumer suites, including `Select` and `NavigationMenu`, and also matched substring names such as `Selector`. The failing broad log is preserved as `docs/audits/logs/popover-consumers-bunit.log` but is not used as the Popover consumer verdict.

## Upstream Integration Checks

React source tests outside the Popover package exercise Popover integration with Toolbar, Select, NavigationMenu, and Tabs. Local `rg` inspection found no equivalent non-Popover local tests that import or render Popover in those component test suites. Current Blazor Popover Playwright coverage already includes the underlying interaction categories:

| Upstream React integration | Local coverage present |
| --- | --- |
| Toolbar button rendered as `Popover.Trigger`, including disabled keyboard behavior. | Popover trigger keyboard and disabled behavior is covered in Popover bUnit/Playwright tests. No local Toolbar-plus-Popover integration test file exists. |
| Select inside Popover, including outside press and Escape ownership. | Popover outside press, Escape, nested floating ownership, and focus-out behavior are covered. No local Select-inside-Popover integration test file exists. |
| NavigationMenu with nested Popover. | Nested Popover ownership is covered by `PopoverNestedTestPage`. No local NavigationMenu-plus-Popover integration test file exists. |
| Tabs inside Popover, with initial focus. | Popover focus and tab navigation are covered by `PopoverFocusTestPage` and `PopoverTabNavigationTestPage`. No local Tabs-inside-Popover integration test file exists. |

## Final Assessment

The source docs comparison found no additional Popover implementation gaps beyond the already repaired audit scope. The documented anatomy, examples, generated API props, data attributes, CSS variables, handle behavior, hover behavior, payload behavior, controlled active-trigger behavior, transition markers, and viewport markers are present in the Blazor port or represented by Blazor-native equivalents.

Direct Blazor components that consume Popover types, Tooltip and PreviewCard, pass both targeted bUnit and Playwright suites. Upstream-only component integration scenarios are documented above as coverage observations because those exact local integration tests do not exist outside the Popover suite.
