# Accordion Functional Audit

Date: 2026-06-01

## Superseded

This audit is retained as historical evidence only. It was superseded on 2026-06-26 by:

- `docs/audits/accordion-functional-audit.md`
- `docs/audits/accordion-parity-matrix.md`
- `docs/audits/accordion-source-docs-comparison.md`
- `docs/audits/accordion-verification-report.md`

Current React Base UI Accordion behavior no longer includes roving trigger focus, and `Accordion.Root` no longer renders `role="region"`. Use the 2026-06-26 audit set for current parity conclusions.

## Scope

- Component: `Accordion`
- React source audited: `.base-ui/packages/react/src/accordion`
- Shared React behavior audited: `.base-ui/packages/react/src/collapsible`
- Blazor implementation audited: `src/BlazorBaseUI/Accordion`
- Browser test surfaces: Playwright Server, Playwright WASM, in-app browser

`../base-ui-specs/accordion/SPEC.md` was absent at audit start, so the audit fell back to the React source. The adjacent framework-agnostic spec and pitfalls files were created after the audit at `../base-ui-specs/accordion/`.

## Resolved Gaps

| Gap | React behavior | Blazor repair |
| --- | --- | --- |
| Async cancellation | `onValueChange` receives cancelable details before state mutation. | `AccordionRoot.HandleValueChangeAsync` now awaits `OnValueChange` and stops before state/binding updates when canceled. |
| Hidden state | Item state includes `hidden: !open && !mounted`; Item/Header/Trigger/Panel emit `data-hidden` when hidden. | Item now tracks panel mounted state and propagates `Hidden` through Item, Header, Trigger, and Panel state/attributes. |
| Kept closed panels | Kept-mounted and hidden-until-found panels remain rendered but hidden after close completion. | Panel close completion now clears transition-mounted state for all completed closes; render retention is handled by `KeepMounted`/`HiddenUntilFound`. |
| `hidden="until-found"` | Closed hidden-until-found panels remain in DOM with string hidden attribute. | Panel render/hidden logic now keeps the panel present and emits `hidden="until-found"`. |
| `beforematch` | `beforematch` opens hidden-until-found content with reason `none`, including disabled items. | Item `HandleBeforeMatch` no longer rejects disabled items and awaits the root value change. |
| Initial transition state | Initially open panels expose `transitionStatus: idle` and suppress mount keyframe animation. | Item/Panel initialize open state as `Idle`; Panel applies one-shot `animation-name: none` while initially open. |
| Id synchronization | Item owns trigger/panel ids; Panel `id` can override generated panel id. | Item now creates default trigger and panel ids; Panel and Trigger read/write through item context. |
| Trigger state attributes | Trigger emits state-derived `data-index`, `data-orientation`, `data-hidden`, disabled state, and `data-panel-open`. | Trigger state now carries index/hidden and emits the missing attributes. |
| Trigger JS config | Keyboard activation depends on `nativeButton` after parameter changes. | `updateConfig` now carries `isNativeButton`; bUnit JS interop setup was updated. |
| Explicit item value changes | React recomputes explicit `value` props. | Item now refreshes `resolvedValue` when an explicit `Value` parameter changes. |
| Playwright focus coverage | Historical only: this row was superseded by the 2026-06-26 audit after React Base UI removed roving trigger focus. | Current Playwright coverage verifies native trigger activation timing and no longer treats arrow-key focus loops as Accordion behavior. |

## Parity Matrix

| React hook/utility/branch | Blazor equivalent | Verification |
| --- | --- | --- |
| `useControlled` in Root | `Value`, `DefaultValue`, `currentValue`, `IsControlled` | Unit and Playwright open-state tests |
| `useStableCallback(handleValueChange)` | `HandleValueChangeAsync` on root context | `AsyncOnValueChangeCancellationPreventsStateChange` |
| `createChangeEventDetails(REASONS.none)` | `AccordionValueChangeEventArgs<TValue>` cancellation | Unit cancellation test |
| `useDirection` | `DirectionProviderContext` cascade | Existing direction/orientation attrs and focus tests |
| `CompositeList` / `useCompositeListItem` | `RegisterItem`, `GetItemIndex`, `UnregisterItem` | `data-index` unit coverage and focus navigation |
| `useBaseUiId` item fallback/id defaults | `Guid.NewGuid().ToIdString()` fallback value and item-owned ids | Id synchronization unit coverage |
| `useCollapsibleRoot` | `CollapsibleRootContext` built by `AccordionItem` | Trigger, beforematch, transition tests |
| `useCollapsiblePanel` | `AccordionPanel` plus `blazor-baseui-collapsible.min.js` | Hidden, transition, beforematch, Playwright coverage |
| `useRenderElement` | `RenderElement<TState>` in all Accordion parts | Build/analyzer pass |
| `accordionStateAttributesMapping` | Item/Header/Panel attribute builders | Data attribute unit coverage |
| `triggerOpenStateMapping` | Trigger attribute builder and JS handlers | Trigger unit coverage and Playwright focus tests |
| `useButton` | `AccessibilityUtilities.ApplyNativeButtonAttributes` / `ApplyButtonAttributes` | Trigger unit coverage |
| Composite keyboard helpers | Historical only: superseded by current APG-aligned trigger key handling. | Current trigger JS prevents Space scroll on keydown and activates on keyup. |
| `beforematch` event listener | Shared Collapsible JS module invoking `OnBeforeMatch` | Unit and Playwright hidden-until-found tests |
| Animation finish utilities | Shared Collapsible JS animation measurement/waiting | Panel transition tests and Playwright runs |
| Development-only React warning branch | Behavior implemented; warning branch documented in adjacent spec. Root cannot distinguish explicit `keepMounted={false}` from default `false` with current bool API. | No functional runtime effect; hidden-until-found override verified |

## Attribute Matrix

| Part | React attributes | Verified Blazor attributes |
| --- | --- | --- |
| Root | Current React source: `dir`, no `role`, `data-orientation`, `data-disabled` when disabled | Current root role behavior is verified in `docs/audits/accordion-functional-audit.md`. |
| Item | `data-open`/`data-closed`, `data-disabled`, `data-hidden`, `data-index`, `data-orientation` | Unit tests cover open, closed, disabled, hidden, index, orientation |
| Header | Item state attributes | Unit coverage through state propagation and added hidden state |
| Trigger | `id`, `type`/`role`, `tabindex`, `aria-expanded`, conditional `aria-controls`, `data-panel-open`, `data-disabled`, `data-hidden`, `data-index`, `data-orientation`, `data-value` | Unit tests and in-app browser check |
| Panel | `id`, `role="region"`, `aria-labelledby`, conditional `hidden`, transition attrs, item state attrs, CSS vars | Unit, Playwright, and in-app browser checks |

## Verification Commands

| Command | Result | Log |
| --- | --- | --- |
| `dotnet build BlazorBaseUI.slnx` | Passed, 0 warnings, 0 errors | `docs/audits/logs/accordion-dotnet-build.log` |
| `dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --filter "FullyQualifiedName~Accordion" -v minimal` | Passed: 81, Failed: 0, Skipped: 0 | `docs/audits/logs/accordion-unit-tests.log` |
| `dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~AccordionTestsServer" -v minimal` | Passed: 32, Failed: 0, Skipped: 0 | `docs/audits/logs/accordion-playwright-server.log` |
| `dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~AccordionTestsWasm" -v minimal` | Passed: 32, Failed: 0, Skipped: 0 | `docs/audits/logs/accordion-playwright-wasm.log` |
| `bash scripts/lint-rules.sh` | 0 violations | `docs/audits/logs/accordion-lint-rules.log` |

## In-App Browser Check

Log: `docs/audits/logs/accordion-in-app-browser-check.json`

Historical check against `http://127.0.0.1:5110/tests/accordion/server`. This evidence is superseded by `docs/audits/logs/accordion-in-app-browser-comparison-2026-06-26.json`.

- Closed hidden-until-found panel has `hidden="until-found"`, `data-hidden`, trigger `data-hidden`, trigger `data-index="0"`, and trigger `data-orientation="vertical"`.
- Opening removes `hidden` and `data-hidden`, sets `aria-expanded="true"`, and makes the panel visible.
- Historical vertical focus loop sequence: trigger 1 ArrowDown -> trigger 2, ArrowDown -> trigger 3, ArrowDown -> trigger 1, ArrowUp -> trigger 3. Current React Base UI no longer treats this as Accordion behavior.

## Notes

- No RenderFragment output caching was introduced.
- DOM-heavy measurement, animation, `beforematch`, key prevention, and focus movement remain in JavaScript.
- Serena and Context7 were not exposed as callable tools in this session; local source was searched with `rg`, and framework behavior was checked against the local React Base UI source and existing project guidance.
