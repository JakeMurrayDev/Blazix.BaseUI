# Select Functional Audit

Date: 2026-07-21
Component: Select
React baseline: `b6ec388df83d3ef3e01bd0505797b63e489480d4`
React audited head: `bdcb685fadcca9d18b18f013c052795a53b6aa33`

## Scope and Method

- Audited all React Select production/test/docs files and shared dependencies reached by Select.
- Evaluated 255 first-parent upstream commits between the prior-audit baseline and current head.
- Compared the Blazor Razor/C# implementation, component JS, shared floating/portal JS, bUnit tests, Server/WASM Playwright fixtures, and minified assets.
- Used three independent subagents for upstream history, Blazor/interop parity, and test/verification coverage.
- Verified live upstream: local `.base-ui` HEAD, `origin/master`, and `git ls-remote origin refs/heads/master` all resolved to `bdcb685fa`.

## Resolved Gaps

1. Form and validation
   - Multiple mode now serializes the primary input as an empty string and disables every per-value hidden input with the root.
   - Ordered multiple dirty comparison uses the configured item comparer.
   - Disabled controls suppress computed `aria-invalid` while retaining external field-state data attributes.
   - Named and unnamed visually-hidden input styles now match the absolute/fixed source variants.

2. Controlled lifecycle
   - Resolved value changes are observed in `OnParametersSet`; external controlled updates refresh labels, filled/dirty state, errors, and validation.
   - Rejected controlled callbacks no longer run effects for an unchanged value.
   - Programmatic value updates do not force-mount the popup.
   - Dynamic root IDs propagate through trigger, label, popup/list, hidden input, ARIA references, and the JS root registry.

3. Disabled and keyboard behavior
   - Forced-open disabled/read-only roots cannot commit from click, drag, autofill, or boxed-value paths.
   - Initial focus and typeahead skip disabled items; Arrow/Home/End retain upstream focusability of disabled options.
   - Repeated-character typeahead cycles matches; mixed buffered queries retain the current match.
   - Custom-tag Enter/Space activation preserves modifiers and yields to a prevented keydown.
   - Generic virtual clicks remain highlight-gated; assistive virtual clicks carrying pointer metadata can activate an unhighlighted option.
   - Closed/closing options immediately use `tabindex="-1"`.

4. Focus and modality
   - Popup focus management restores the nearest remaining focus target.
   - Programmatic opens return focus to the opener; trigger-driven opens prefer the trigger.
   - Removed the duplicate Select-JS trigger-focus path so the focus manager is the single return-focus owner.
   - Touch modality is retained through exit. Nearly full-width touch popups acquire scroll lock; narrow touch popups do not.

5. Positioning and scrolling
   - Trigger release tolerance is 5px.
   - Align-item placement sets its scroll-ready latch and removes stale transform origin before branching; height-cap removal uses `max-height: none`.
   - Scroll growth uses the current reduced-handler algorithm and normalized fractional edge offsets.
   - Flip uses the all-side precedence inset plus preferred-side bias. Shift, limit-shift, size, and hide use raw collision padding.
   - Select Positioner now supports dynamic side/align offsets, per-side padding, concrete collision boundaries, function anchors, and virtual rectangles.

6. Item registration and transitions
   - Replaced per-item/per-render DOM-index interop with a shared atomic option registry and subtree MutationObserver.
   - Insert, remove, reorder, and grouped-wrapper reparent operations publish authoritative DOM indexes.
   - ItemIndicator and both scroll arrows now wait for actual CSS transition/animation completion and support reversal.
   - Removed resolved `RenderFragment` caches from SelectValue and scroll arrows.

7. Value and portal surfaces
   - Static items can provide rich `RenderFragment` labels; single and multiple selected-value rendering preserves their structure.
   - Conventional object `Label`/`Value` properties are resolved automatically for display and form serialization.
   - Portal accepts either a selector or concrete DOM container.

8. First-visible-frame popup sizing
   - Frame extraction from the reported WASM recording proved that the popup was fully populated before the resize: frame 180 displayed every option at approximately 258 source pixels, frame 181 was hidden, and frame 182 reappeared at approximately 504 source pixels.
   - Align-item placement now waits for the current open revision's Floating UI callback rather than nonzero intrinsic geometry or the existence of public CSS variables.
   - Every open invalidates `data-positioned`, resets the Floating UI first-position latch, rejects obsolete async generations, and executes one fresh size-middleware pass before visibility.
   - Default-open Server/WASM render-order races retain only a first-open result produced before root/positioner registration; callbacks from replaced elements and prior open revisions are rejected.
   - Popup JS registration now waits for the first render where `RenderElement` has published its `ElementReference`; a delayed WASM reference can no longer permanently skip initialization.
   - Externally controlled opens can publish a fresh pre-open result for exactly the next revision, closing the Blazor parameter-render versus root-interop race. An explicit reset token rejects mounted close-transition auto-updates as next-open readiness.
   - Runtime root-ID changes update the positioner registration used by later callbacks; renamed roots retain current-revision readiness.
   - The placement watchdog only retries. It cannot publish `data-positioned`; visibility is released exclusively by a completed align-item commit or standard floating fallback.
   - Standard fallback immediately starts a Floating UI pass without waiting for Blazor interop, cancels superseded queued align commits, restores the consumer's anchor-tracking setting, and releases visibility only after standard coordinates and sizing variables are committed.
   - Floating UI no longer overwrites the fixed align-item position while computing sizing, and fallback positioning publishes equivalent anchor/viewport variables before visibility.
   - Device-pixel snapping now resolves DPR from the positioner's owning window for iframe and shadow-root correctness.

## Upstream Delta and Impact Report

| Commit | Upstream change | Blazor implementation/accounting |
| --- | --- | --- |
| `c779f6bc1` | Disabled form registration/hidden inputs/invalid props | Disabled all hidden inputs; gated computed invalid ARIA. |
| `f70b3160e` | Programmatic-open focus returns to opener | Passed open interaction type to the focus manager; removed duplicate focus path. |
| `3dcfa3ec8` | Explicit accepted/canceled value flow | Existing cancelable value event retained; effects now run only after resolved change. |
| `992c52b78` | Kept-mounted closed items are not tabbable | Item tabindex now requires `open && highlighted`. |
| `54d73cc21` | Controlled selection, autofill, serialization, disabled/read-only guards, arrow transitions | Repaired lifecycle observer, forced-open guards, serialization, and DOM transition ownership. |
| `bf831b754` | Realm-safe platform cleanup | Existing owner-document/owner-window behavior retained. |
| `900210afa` | Ordered multiple dirty comparison | Implemented ordered comparison with `IsItemEqualToValue`. |
| `86c467bd4` | Documentation demo cleanup | No runtime delta. |
| `a47b1df37` | `actionsRef` documentation | Existing imperative unmount surface accounted. |
| `ee3c13a0e` | Disabled/repeated typeahead, empty multi serialization, `maxHeight: none` | Implemented all four. |
| `4530d87c9` | Modal touch documentation | Implemented width-dependent touch scroll lock and retained touch modality. |
| `cfa8df0be` | Repository-link update | No runtime delta. |
| `73f65d984` | Programmatic value changes do not force mount | Lifecycle observer updates mounted value content without `ForceMount`. |
| `4cc8e31ca` | Starting-style wording | Emitted attributes already compatible. |
| `5a50be524` | Test timing stabilization | Test-only; no runtime delta. |
| `849a05613` | Trigger slip tolerance 2px to 5px | Updated constant and added 5px/6px browser regressions. |
| `823e1f46c` | Popup state mapping cleanup | Output-equivalent. |
| `9798cd1e8` | Scroll/placement/store synchronization performance | Reduced scroll handler, early placement latch, native parameter synchronization, atomic registry. |
| `c9c90dce2` | Custom option keyboard and virtual-click activation | Preserved modifiers, single activation, prevented-keydown cancellation, and pointer-metadata virtual-click semantics. |
| `7a0fd2f84` | Published type stripping | Packaging-only. |
| `bdcb685fa` | Atomic composite registration | Shared option registry plus MutationObserver replaced per-render calls. |
| `a68d387d6a` | Reset size/positioning state after the close transition | Added per-open readiness revisions, close/open invalidation, controlled pre-open handoff, suspended stale generations, and a mandatory fresh middleware pass before reopen visibility. |
| `d1eb968ed` | Resolve DPR through the floating element's owner window | Replaced global `window.devicePixelRatio` with `positionerElement.ownerDocument.defaultView?.devicePixelRatio`. |
| `ec4218156` | Defer align-item measurement until Floating UI is positioned | Added the C# `IsPositioned` lifecycle gate and a same-frame JS callback/commit guarded by the current element and open revision. |

Shared fixes evaluated and mapped: queued-focus cancellation (`66738e638`), required validation (`34f542357`), pseudo-element bounds (`08e9ba68a`), non-modal pointer reset (`4292cfaa6`), nested focus return (`d4ee8ae78`), initial disabled focus (`1631d37ad`), keyboard-visible focus (`e6dc73dfa`), first-pass available-size seeding (`3fde28905`), reset-after-close sizing (`a68d387d6a`), owner-window DPR (`d1eb968ed`), align-item readiness (`ec4218156`), disabled external invalid state (`5504cedda`), collision bias isolation (`d6f51ca8a`), grouped reorder observation (`2437d817e`), native disabled navigation (`70e03c2f1`), and popup bundle/performance refactors (`37cf15f43`, `43d11ebcf`, `560eda0b3`, `af2aadb1a`). No unaccounted Select-observable delta remains in the audited range.

## Post-Audit Opening and Grouped-Scroll Repair

- Frame-by-frame follow-up identified repeated align-item commits after the popup was already visible. General root notifications caused `SelectPopup` to call placement again, and fixed-delay probes at 0, 50, 250, and 1000 ms multiplied the same synchronous geometry reads and style writes.
- Each replay rewrote the list endpoint `scrollTop`. In a grouped popup opened near its final option, upward scrolling changed arrow visibility, caused another root render, and placement immediately returned the list to its maximum scroll position.
- Placement now records the committed open/input revision, rejects duplicate in-progress and committed calls, and invalidates only for geometry-bearing element/item/value/readiness changes. The fixed-delay probe fan-out was removed; the existing requestAnimationFrame readiness scheduler and watchdog remain.
- A same-input Floating update can transiently clear `data-positioned` after align-item geometry has committed. The committed revision now reasserts that visibility token and rejects watchdog fallback until a real placement input changes, preventing the grouped popup from collapsing from aligned height to standard constrained height after opening.
- The Blazor lifecycle bridge invokes placement only when open/align/readiness transitions to ready. Active-index and scroll-arrow renders no longer execute placement.
- Floating UI now retains `data-side="none"` on every align-item pass, including unchanged physical-side updates that intentionally omit the .NET callback.
- Initial focus waits for a rendered open popup and nonzero list layout. Selected-index state is supplied with root-open interop, and nested option scrolling uses list-relative rectangles instead of group-local `offsetTop`.
- React parity impact: this maps React's dependency-scoped layout effect and composite nearest-scroll behavior without reproducing React's internal hook structure. DOM measurement, focus, and scroll remain JS-owned.

## Attribute and Accessibility Result

- Trigger: `role=combobox`, `aria-haspopup`, `aria-expanded`, controls/label/description/required/read-only/invalid state, popup side, placeholder/filled/open state verified.
- Popup/List: source role ownership, IDs, multiselect state, open/closed/side/align/transition attributes verified.
- Item: option role, selected/highlighted/disabled state, string ARIA values, closed tab order, label metadata verified.
- Group/label, indicator, arrow, backdrop, portal, icon, and scroll-arrow structural/state attributes verified.
- Hidden input name/id/form/autocomplete/required/read-only/disabled/value/description/invalid behavior verified.

## Result

The audited Blazor Select port has observable source parity for the current local React source and the complete audited upstream range. All identified production gaps were repaired, covered by tests or direct browser checks, documented, and staged without commit. Raw generated logs remain local under `docs/audits/logs/` and are intentionally not staged.
