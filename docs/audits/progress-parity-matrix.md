# Progress Parity Matrix

Audit target: Blazor Progress port against React Base UI Progress source and docs.

## Source Files

- React Progress: `.base-ui/packages/react/src/progress`
- React Progress docs: `.base-ui/docs/src/app/(docs)/react/components/progress`
- Blazor Progress: `src/BlazorBaseUI/Progress`
- Blazor unit tests: `tests/BlazorBaseUI.Tests/Progress`
- Blazor Playwright tests: `tests/BlazorBaseUI.Playwright.Tests/.../Progress`
- Framework-agnostic spec: `../base-ui-specs/progress`

## Component and Utility Coverage

| React source | React responsibility | Blazor equivalent | Status |
| --- | --- | --- | --- |
| `ProgressRoot.tsx` | Defaults `min=0`, `max=100`; accepts `value`, `format`, `locale`, `getAriaValueText`; renders `progressbar`; cascades context. | `ProgressRoot.razor` exposes `Min`, `Max`, `Value`, `Format`, `Locale`, `GetAriaValueText`; builds root attrs; cascades `ProgressRootContext`. | Verified |
| `ProgressRoot.tsx` default ARIA | Emits `aria-valuenow`, `aria-valuemin`, `aria-valuemax`, `aria-valuetext`, `aria-labelledby`, and `role`. | `ProgressRoot.razor` emits the same defaults and preserves consumer overrides through default-only attribute assignment. | Repaired and verified |
| `ProgressRoot.tsx` state attrs | Emits `data-progressing`, `data-complete`, or `data-indeterminate`. | `ProgressState.ToDataAttributeString()` drives the same root state attributes. | Verified |
| `ProgressRoot.tsx` hidden span | Renders `span role="presentation"` with `visuallyHidden` style and text `x`. | `ProgressRoot.razor` renders the same helper span after child content. | Repaired and verified |
| `ProgressRootContext.ts` | Supplies `formattedValue`, `max`, `min`, `state`, `value`, and `setLabelId`. | `ProgressRootContext` supplies the same data and registration callback. | Verified |
| `useRegisteredLabelId.ts` | Registers the current label id and clears it on cleanup. | `ProgressLabel.razor` registers resolved id in `OnParametersSet` and clears the registered id in `Dispose`. | Repaired and verified |
| `ProgressLabel.tsx` | Renders default `label`; sets `id`; sets `role="presentation"`; emits state data attributes. | `ProgressLabel.razor` renders default `label`, resolves id, sets default role, emits matching state attrs, and registers with root. | Repaired and verified |
| `ProgressTrack.tsx` | Renders default `div`; emits state data attributes; requires context. | `ProgressTrack.razor` renders `div`, emits matching state attrs, and throws missing-context diagnostics outside root. | Repaired and verified |
| `ProgressIndicator.tsx` | Renders default `div`; emits state data attributes; applies inline progress style from `valueToPercent`. | `ProgressIndicator.razor` renders `div`, emits matching state attrs, computes style from `ValueToPercent`, and preserves consumer style precedence. | Repaired and verified |
| `ProgressValue.tsx` | Renders default `span`; sets `aria-hidden="true"`; emits state data attributes; supports function children. | `ProgressValue.razor` renders `span`, sets default `aria-hidden`, emits matching state attrs, and supports `Func<string?, double?, RenderFragment>`. | Repaired and verified |
| `formatNumberValue.ts` | Formats with `Intl.NumberFormatOptions` and `locale`; defaults to percent display. | `ProgressRoot.FormatValue()` formats with `NumberFormatOptions` and `Locale`; `FormatString`/`FormatProvider` provide Blazor-native formatting extension; default percent display remains. | Repaired and verified |
| `getDefaultAriaValueText` | Returns formatted value for determinate values and `undefined` for indeterminate. | `ProgressRoot.GetDefaultAriaValueText()` returns formatted value for determinate values and `null` for indeterminate. | Verified |
| `getAriaValueText` | Consumer callback overrides default aria value text. | `ProgressRoot.GetAriaValueText` callback overrides default aria value text. | Verified |
| `valueToPercent.ts` | Uses raw `((value - min) * 100) / (max - min)` arithmetic. | `ProgressIndicator.ValueToPercent()` mirrors the raw formula. | Repaired and verified |
| `progressStateAttributesMapping` | Maps `progressing`, `complete`, and `indeterminate` to data attributes. | `ProgressState.ToDataAttributeString()` maps states to matching data attributes. | Verified |
| `useRenderElement` | Merges render prop, state, class, style, component props, and element props. | All Progress parts use `RenderElement<TState>` with `Render`, `State`, `ClassValue`, `StyleValue`, `ComponentAttributes`, and `AdditionalAttributes`. | Verified |
| React context guard | Parts throw when rendered outside `Progress.Root`. | All non-root parts throw `ProgressRootContext.MissingContextMessage` when no context exists. | Repaired and verified |

## Attribute Matrix

| Part | React attributes | Blazor verification |
| --- | --- | --- |
| Root | `role`, `aria-valuemin`, `aria-valuemax`, `aria-valuenow`, `aria-valuetext`, `aria-labelledby`, state data attr. | Unit and Playwright tests verify defaults, indeterminate omission, custom value text, label linkage, and consumer overrides. |
| Label | `id`, `role="presentation"`, state data attr. | Unit and Playwright tests verify id generation, explicit id, role default, role override, state attrs, and root linkage updates. |
| Track | State data attr. | Unit and Playwright tests verify state attrs and missing-context guard. |
| Indicator | State data attr, inline progress style. | Unit and Playwright tests verify active/complete/indeterminate styles, custom range, edge arithmetic, style precedence, and missing-context guard. |
| Value | `aria-hidden="true"`, state data attr, formatted text or function child output. | Unit and Playwright tests verify default text, indeterminate blank content, `aria-hidden`, overrides, state attrs, and render function output. |
| Hidden helper span | `role="presentation"`, `style=visuallyHidden`, text `x`. | Unit, Playwright, and in-app Browser DOM checks verify presence and attributes. |

## Final Assessment

All audited React Progress logic branches now have a Blazor equivalent. Lifecycle-sensitive state propagation uses Blazor parameter processing and disposal, not React-style effect loops. No JS interop was required because Progress has no DOM-heavy event, focus, or layout work in the React source.
