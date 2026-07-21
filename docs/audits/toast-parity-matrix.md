# Toast React-to-Blazor Parity Matrix

Date: 2026-07-21
React reference: `bdcb685fadcca9d18b18f013c052795a53b6aa33`

## Components

| React source | Hook/utility/branch | Blazor equivalent | Verification |
| --- | --- | --- | --- |
| `ToastProvider` | store creation, prop synchronization, external manager events | `ToastProvider`, `ToastStore.SyncProviderProperties`, `ToastManager` subscription | Dynamic provider bUnit tests |
| `ToastViewport` | `useState` slices for empty/toasts/focus/expanded/previous focus | `ToastStore` state plus native component rendering | bUnit; Server/WASM Playwright |
| `ToastViewport` | non-empty `useEffect` listener lifecycle | `updateViewport` JS binding/unbinding | Listener rebind Playwright test |
| `ToastViewport` | F6 and previous-focus capture | JS owner-window key handler plus `OnGlobalFocusHotkey` | F6 Playwright tests |
| `ToastViewport` | three FocusGuards | Three conditional `FocusGuard` instances | DOM and focus tests |
| `ToastViewport` | Tab/Shift+Tab guard routing | direct JS viewport routing and C# timer resume | zero-limit and focus tests |
| `ToastViewport` | focus-visible expansion | JS `matches(':focus-visible')`, store pause/focus | focus tests |
| `ToastViewport` | window blur/focus timer control | JS owner-window handlers and .NET callbacks | code audit and browser suite |
| `ToastViewport` | document touch pointer down | owner-document capture listener | swipe/touch browser tests |
| `ToastViewport` | deferred mouseleave | `touchActive`, ending-state tracking, `FlushMouseLeave` | browser regression coverage |
| `ToastViewport` | high-priority live mirror | keyed visually hidden atomic alerts | bUnit attribute coverage |
| `ToastRoot` | transition-status lifecycle | starting/ending state plus all-animation completion JS | close/remove and swipe tests |
| `ToastRoot` | height layout effect | observer-backed JS measurement with dedupe | store/unit and browser suite |
| `ToastRoot` | swipe state machine | component JS pointer capture/damping/velocity/axis logic | swipe and active-pointer tests |
| `ToastRoot` | limited/ending focus behavior | `inert`, `aria-hidden`, focus routing filters | limit-zero browser test |
| `ToastContent` | expanded/behind state | `ToastContentState` and data attributes | bUnit part tests |
| `ToastTitle` | render-if-content, ID registration | renderability utility plus `Render` presence, store title ID | render-only bUnit test |
| `ToastDescription` | render-if-content, ID registration | renderability utility plus `Render` presence, store description ID | bUnit part tests |
| `ToastAction` | manager action props, `useButton`, close rules | `ToastActionOptions`, shared button JS, async disposal | non-native browser tests |
| `ToastClose` | `useButton`, close current toast | shared button JS and manager close | non-native and close tests |
| `ToastPortal` | `FloatingPortalLite`, empty state type, container | shared `Portal`, `ToastPortalState`, selector/element target | build and API audit |
| `ToastPositioner` | option merge and explicit anchor removal | parameter presence tracking and field-by-field precedence | code audit/build |
| `ToastPositioner` | floating initialization/update/reset | `PositionerInterop`, shared floating JS | build and existing positioning tests |
| `ToastArrow` | floating arrow metadata | `ToastArrowState` data attributes | bUnit part tests |

## Store and Manager

| React source behavior | Blazor equivalent | Status |
| --- | --- | --- |
| Newest-first insertion | `ToastStore.AddToast` insertion order | Verified |
| Generated IDs without mutating input | `CloneWithId` in `ToastManager` | Verified |
| Duplicate ID upsert | presence-aware `ApplyUpdates` | Verified |
| Explicit-null update vs omitted property | backing fields and `Has*` flags | Verified |
| `updateKey` increment | `ToastObject.UpdateKey` refresh | Verified |
| Dynamic timeout/limit prop update | `SyncProviderProperties` | Verified |
| Limited toasts remain mounted/inert | `ApplyLimit`, Root attributes | Verified |
| Zero limit | all active toasts limited; guard restores focus | Verified |
| Loading toast has no active timer | timer gating by type | Verified |
| Pause/resume preserves remaining duration | `ToastTimer` and store pause/resume | Verified |
| Timer completion clears pause bookkeeping | timer finalization/clear paths | Verified |
| Close marks ending, invokes close once | `CloseToast` | Verified |
| Close all clears timers | `CloseToast(null)` | Verified |
| Remove invokes remove and recomputes limit | `RemoveToast` | Verified |
| Promise forces loading/success/error types | `ToastManager.Promise` | Verified |
| Promise clears inherited loading timeout | presence-aware success/error updates | Verified |
| External manager and in-tree manager equivalence | `ToastManager` plus `ToastManagerContext` | Verified |

## Render and Utility Mapping

| React hook/utility | Blazor equivalent | Status |
| --- | --- | --- |
| `useRenderElement` | `RenderElement<TState>` | Verified |
| React node renderability | `ToastRenderFragmentUtilities.IsRenderableValue` | Verified for null/bool/empty/enumerable/zero |
| `useButton` | `blazix-baseui-button` interop used by Action/Close | Verified |
| `useOpenChangeComplete` | root `getAnimations()` settlement | Verified |
| `useTimeout` | cancellable async Toast timer/window-focus sequencing | Verified |
| `FocusGuard` | shared Blazor FocusGuard | Verified |
| `isFocusVisible` | JS `:focus-visible` detection | Verified |
| `ownerDocument`/`ownerWindow` | DOM ownership derived from viewport/root | Verified |
| `addEventListener`/`mergeCleanups` | AbortController listener groups | Verified |
| `resolvePromiseOptions` | `ToastPromiseOption<T>` resolution | Verified |
| `FloatingPortalLite` | `Portal` selector/concrete target | Verified |
| floating-ui positioning | `PositionerInterop` and `blazix-baseui-floating` | Verified |

## Attribute Matrix

| Part | React attributes/state | Blazor output |
| --- | --- | --- |
| Viewport | tabindex, region role, live/atomic/relevant/label, expanded, frontmost height | Exact observable equivalent |
| Root | dialog/alertdialog, modal, labels, hidden/inert, expanded/limited/type/swipe/transition, stack/swipe CSS variables | Exact observable equivalent |
| Content | expanded, behind | Exact |
| Title | ID, type | Exact |
| Description | ID, type | Exact |
| Action | button semantics, type, manager attributes | Exact observable equivalent |
| Close | button semantics, hidden, type | Exact observable equivalent |
| Positioner | role, side, align, anchor hidden, floating variables | Exact observable equivalent |
| Arrow | hidden, side, align, uncentered | Exact |
| Swipe-ignore descendants | `data-base-ui-swipe-ignore` | Exact; legacy selector additionally supported |

## Upstream Fix Mapping

| Upstream fix | Blazor mapping | Proof |
| --- | --- | --- |
| `b35d32224` timer/limit/focus/touch edge cases | store, viewport, root JS, regression tests | bUnit and 26-test Playwright suite |
| `4238b5baa` undo timeout | docs CSS/Tailwind examples and snippets | source-doc comparison |
| `1342a27db` listener/bundle optimization | non-empty listeners, omitted empty height style, deduped interop/minified asset | static audit and browser test |
| `7a0fd2f84` state typing | `ToastPortalState` | build/analyzers |
| `ec5609b9c` render-prop content | Render-aware Title/Description/Action | bUnit |
| `bf831b754` platform detection | Apple+WebKit FocusGuard detection | focused tests/build |
| `c9c90dce2` custom button keyboard fixes | Action/Close shared button interop | Server/WASM Enter/Space tests |

All other direct Toast/docs commits in the audited range are cataloged in the functional audit and either implemented, already equivalent, or confirmed non-runtime.
