# Drawer Parity Matrix

Date: May 27, 2026 (original) · July 18, 2026 (upstream-delta refresh, `.base-ui @ bdcb685fa`)

## Upstream-Delta Refresh — July 18, 2026

Rows added or superseded by the upstream-delta audit (`drawer-upstream-delta-2026-07.md`):

| React source | Required behavior | Blazor equivalent | Status |
| --- | --- | --- | --- |
| `utils/useSwipeDismiss.ts` | Full swipe engine: direction locking (≥1 px), directional sqrt damping, reverse-cancel (10 px), pending-swipe activation with per-move `canStart`, scroll-edge activation with displacement preservation, interactive-element ignore selector, text-selection guards, primary-button release-through-move (#5057), sampled release velocity (≤80 ms window, ≥16 ms min), transition-freeze `translate3d` drag styles with snapshot/restore | `createSwipeDismiss` closure in `blazix-baseui-drawer.js`; constants and formulas 1:1 | Verified |
| `viewport/DrawerViewport.tsx` (rewritten upstream) | Native capture-phase `touchmove` pipeline; cross-axis scroll preservation; range-input/pinch/text-selection exemptions; snap/sequential/velocity release decision tree with touch-reversal guard; swipe-release strength scalar; controlled-dismiss revert; settled/programmatic snap-point re-application; 10 px nested-swipe intent gate | Viewport orchestration in `blazix-baseui-drawer.js`; `OnSwipeRelease`/`OnSwipeDismiss` edge interop in `DrawerViewport.razor`; per-frame .NET interop eliminated (#4980 analog) | Verified |
| `root/useDrawerSnapPoints.ts` (updated upstream) | `closestSnapPointIndex` fallback for non-matching active snap point; first-segment progress denominator; sqrt overshoot damping (`getSnapPointSwipeMovement`) | Ported into drawer JS snap math | Verified |
| `swipe-area/DrawerSwipeArea.tsx` (rewritten upstream) | Deterministic outside-press guard (no timers), drag-open following the pointer, mid-close re-grab from live transform, ≥50 % or ≥0.1 velocity open commit, `swipeAreaActive` anti-flash coordination (#5112) | Drawer JS + `setOutsidePressEnabled` export in `blazix-baseui-dialog.js`; window-capture restore listener | Verified |
| `virtual-keyboard-provider/DrawerVirtualKeyboardProvider.tsx` (#4353) | visualViewport keyboard inset (`--drawer-keyboard-inset`), focused-field scroll alignment with slack, synchronous tap-to-focus in `touchend`, hit-slop probing, blocked-tap sentinel, off-screen focus trick, untrusted click redispatch, pinch-zoom/already-focused guards | New `DrawerVirtualKeyboardProvider.razor`/`.cs` + `DrawerVirtualKeyboardContext.cs`; behavior 100 % in `blazix-baseui-drawer.js` | Verified |
| `handle.ts` (#5109) | `DrawerHandle extends DialogHandle` with factory | `Drawer/DrawerHandle.cs` (pre-existing) | Verified |
| `utils/platform` (#4920) | CloseWatcher only for the topmost open drawer on Android | Topmost gate + UA fallback regex in drawer JS | Verified |
| Dialog `touchend` outside press (#5096) | Touch dismissal without a backdrop: one changed touch, zero remaining, event-target resolution | `blazix-baseui-dialog.js` | Verified |
| `useButton` modifier clicks (#4838) | Keyboard clicks on non-button tags carry modifier keys | `dispatchClickWithModifiers` in `blazix-baseui-button.js` | Verified |
| Automated coverage (refresh) | All swipe/snap/keyboard-provider states validated in a real browser | 24 bUnit tests; 33 Playwright tests (Server + WASM); Dialog/Popover sibling regression 274/274 total | Verified |

| React source | Required behavior | Blazor equivalent | Status |
| --- | --- | --- | --- |
| `root/DrawerRoot.tsx` | Controlled and uncontrolled open state, default open state, modal mode, dismissibility, event reasons, root context, nested drawer state, and snap-point state. Explicit `snapPoint={null}` and `defaultSnapPoint={null}` are valid null values and must not fall back. | `DrawerRoot.razor`, `DrawerRootContext.cs`, `DrawerRootActions.cs`, `DrawerHandle.cs`, `DrawerSnapPoint.cs`, `EventArgs.cs`, `Enumerations.cs`; `SetParametersAsync` tracks explicit snap/default-snap parameter presence. | Verified |
| `root/DrawerRootContext.ts` | Share open state, ids, modal settings, dismissible setting, nested drawer metadata, snap-point data, swipe direction, and action callbacks. | `DrawerRootContext` cascading value plus `DrawerRootActions`. | Verified |
| `root/useDrawerSnapPoints.ts` | Normalize snap points, maintain active snap point, choose release target from drag offset and velocity, support sequential snap movement, and notify `null` before closing from snap points. | Snap state in `DrawerRoot` and directional release math in `blazor-baseui-drawer.js`. | Verified |
| `provider/DrawerProvider.tsx` | Provide nested drawer transform state for indents and background. | `DrawerProvider.razor`, `DrawerProviderContext.cs`, provider registration in `blazor-baseui-drawer.js`. | Verified |
| `provider/DrawerProviderContext.ts` | Share active nested state, swipe progress, and frontmost drawer height for indent/background effects. | `DrawerProviderContext` with `SetVisualState` and component subscriptions. | Verified |
| `indent/DrawerIndent.tsx` | Render nested drawer indent with provider-driven transform variables. | `DrawerIndent.razor` and provider interop registration. | Verified |
| `indent-background/DrawerIndentBackground.tsx` | Render the background transform surface for nested drawers. | `DrawerIndentBackground.razor` and provider interop registration. | Verified |
| `trigger/DrawerTrigger.tsx` | Button trigger toggles drawer, emits open change reason, has `aria-haspopup`, `aria-expanded`, `aria-controls`, and data attributes. | `DrawerTrigger.razor` and `DrawerTypedTrigger.razor`. | Verified |
| `portal/DrawerPortal.tsx` | Portal Drawer overlay content through Dialog portal behavior. | `DrawerPortal.razor` wrapping `DialogPortal`. | Verified |
| `backdrop/DrawerBackdrop.tsx` | Backdrop renders with Drawer state, CSS variables, data attributes, and outside-dismiss behavior. | `DrawerBackdrop.razor` wrapping `DialogBackdrop`; swipe progress sync in `blazor-baseui-drawer.js`. | Verified |
| `backdrop/DrawerBackdropCssVars.ts` | Expose backdrop swipe progress custom property. | Drawer JS sets `--drawer-swipe-progress`. | Verified |
| `backdrop/DrawerBackdropDataAttributes.ts` | Emit open, closed, starting-style, and ending-style attributes. | `DrawerBackdrop` delegates Dialog backdrop state attributes; JS adds transient swipe attrs for Drawer animation integration. | Verified |
| `viewport/DrawerViewport.tsx` | Viewport hosts Drawer popup, measures dimensions, exposes viewport context, prevents lost pointer releases, and supports swipe area interaction. | `DrawerViewport.razor`, `DrawerViewportContext.cs`, `initializeViewport` and document-level pointer tracking in `blazor-baseui-drawer.js`. | Verified |
| `viewport/DrawerViewportContext.tsx` | Share viewport element metadata and swipe direction. | `DrawerViewportContext` cascading value. | Verified |
| `viewport/DrawerViewportDataAttributes.ts` | Emit open, closed, starting-style, ending-style, and nested data attributes. | `DrawerViewport` state and transition attributes. | Verified |
| `popup/DrawerPopup.tsx` | Dialog popup integration, focus management, role and aria metadata, transform variables, swipe close, snap points, and nested drawer coordination. | `DrawerPopup.razor` wrapping `DialogPopup`; popup interop in `blazor-baseui-drawer.js`; Dialog focus bridge in `blazor-baseui-dialog.js`. | Verified |
| `popup/DrawerPopupCssVars.ts` | Expose nested count, drawer height, frontmost height, swipe movement, snap offset, and swipe strength custom properties. | `DrawerPopup` computes nested/height/frontmost variables; Drawer JS writes movement, snap offset, progress, and release strength variables. | Verified |
| `popup/DrawerPopupDataAttributes.ts` | Emit open, closed, direction, swipe-state, and snap-related attributes. | `DrawerPopup` state plus JS-managed swipe and snap attributes. | Verified |
| `swipe-area/DrawerSwipeArea.tsx` | Render an external swipe target that can open the Drawer, with disabled handling. | `DrawerSwipeArea.razor` and `initializeSwipeArea` in `blazor-baseui-drawer.js`. | Verified |
| `swipe-area/DrawerSwipeAreaDataAttributes.ts` | Emit disabled and swipe-direction attributes. | `DrawerSwipeAreaState` attributes. | Verified |
| `content/DrawerContent.tsx` | Render content surface with Drawer data attribute and Dialog-root presence validation. | `DrawerContent.razor`. | Verified |
| `content/DrawerContentDataAttributes.ts` | Emit `data-drawer-content`. | `DrawerContent` component attributes. | Verified |
| `title/DrawerTitle.tsx` | Render title, register title id for popup `aria-labelledby`. | `DrawerTitle.razor` wrapping `DialogTitle`; id provided by root context. | Verified |
| `description/DrawerDescription.tsx` | Render description, register description id for popup `aria-describedby`. | `DrawerDescription.razor` wrapping `DialogDescription`; id provided by root context. | Verified |
| `close/DrawerClose.tsx` | Close button emits close reason and composes user click handling. | `DrawerClose.razor` wrapping `DialogClose`. | Verified |
| Dialog integration | Drawer is Dialog-derived and must preserve Dialog accessibility while adding Drawer behaviors. | `IsDrawerContext` usage in Drawer components; `DialogOpenChangeReason.Swipe` and `DialogOpenChangeReason.CloseWatcher` added. | Verified |
| CloseWatcher behavior | Android browser back and CloseWatcher close Drawer with correct reason. | `registerRoot` in `blazor-baseui-drawer.js` and `DrawerOpenChangeReason.CloseWatcher`. | Verified |
| Outside press during swipe open | Swipe-open must not immediately close through backdrop or document outside press. | Drawer JS suppression timestamp plus Dialog JS outside-press bridge. | Verified |
| Initial focus | Dialog focus behavior must work with Blazor render timing and callback focus targets. | `DrawerPopup` retry bridge and Dialog JS pending-open callback focus path. | Verified |
| `useRenderElement` equivalence | Components must allow custom render content, tag selection, class/style callbacks, and attribute merging. | `RenderElement<TState>` usage throughout Drawer components. | Verified |
| Demo coverage | Public demo must expose the Drawer usage shown in the source documentation. | Drawer page and `DrawerSection.razor` include hero side drawer, controlled state, position, nested drawers, snap points, indent effect, non-modal drawer, mobile navigation, swipe-to-open, action sheet, and detached triggers. | Verified |
| Automated coverage | Active, disabled, focus, dismissal, swipe, nested, explicit null snap, and snap states must be validated. | 15 bUnit tests and 16 Playwright tests passed. | Verified |
