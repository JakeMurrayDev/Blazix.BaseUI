# Progress Source Docs Comparison

## Source Docs Run

| Command | Result | Log |
| --- | --- | --- |
| `pnpm --dir .base-ui docs:validate "(docs)/react/components/progress"` | Passed. No files needed updating. | `docs/audits/logs/progress-source-docs-validate.log` |
| `pnpm --dir .base-ui exec vitest run --project @base-ui/react packages/react/src/progress` | Passed. 5 files passed; 83 passed; 3 skipped by upstream source suite. | `docs/audits/logs/progress-source-react-tests.log` |
| `dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --filter "FullyQualifiedName~Progress" -v minimal` | Passed. 74 Progress unit tests passed. | `docs/audits/logs/progress-bunit.log` |
| `dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~Progress" -v minimal` | Passed. 26 Progress Playwright tests passed. | `docs/audits/logs/progress-playwright-dotnet-pass.log` |
| In-app Browser inspection of Server Progress test page | Passed. DOM and accessibility snapshot matched expected active and complete states. | `docs/audits/logs/progress-in-app-browser.json` |

## Source Files Compared

| Area | React Base UI source | BlazorBaseUI source |
| --- | --- | --- |
| Public docs | `.base-ui/docs/src/app/(docs)/react/components/progress/page.mdx` | `demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo.Client/Shared/Sections/ProgressSection.razor` |
| API docs | `.base-ui/docs/src/app/(docs)/react/components/progress/types.md` | `src/BlazorBaseUI/Progress/*.razor` |
| Root | `.base-ui/packages/react/src/progress/root/ProgressRoot.tsx` | `src/BlazorBaseUI/Progress/ProgressRoot.razor` |
| Label | `.base-ui/packages/react/src/progress/label/ProgressLabel.tsx` | `src/BlazorBaseUI/Progress/ProgressLabel.razor` |
| Track | `.base-ui/packages/react/src/progress/track/ProgressTrack.tsx` | `src/BlazorBaseUI/Progress/ProgressTrack.razor` |
| Indicator | `.base-ui/packages/react/src/progress/indicator/ProgressIndicator.tsx` | `src/BlazorBaseUI/Progress/ProgressIndicator.razor` |
| Value | `.base-ui/packages/react/src/progress/value/ProgressValue.tsx` | `src/BlazorBaseUI/Progress/ProgressValue.razor` |
| Utilities | `.base-ui/packages/react/src/progress/utils/*`, `.base-ui/packages/react/src/utils/formatNumberValue.ts`, `.base-ui/packages/react/src/utils/useRegisteredLabelId.ts`, `.base-ui/packages/react/src/utils/visuallyHidden.ts` | `src/BlazorBaseUI/Progress/ProgressRootContext.cs`, `src/BlazorBaseUI/Progress/*.razor` |

## API Comparison

| React docs prop | React behavior | Blazor equivalent | Result |
| --- | --- | --- | --- |
| `Root.value` | Determinate value; `null` means indeterminate. | `ProgressRoot.Value`. | Matched |
| `Root.min` | Minimum range value, default `0`. | `ProgressRoot.Min`. | Matched |
| `Root.max` | Maximum range value, default `100`. | `ProgressRoot.Max`. | Matched |
| `Root.format` | `Intl.NumberFormatOptions`; default percent. | `ProgressRoot.Format` using `NumberFormatOptions`; `FormatString` retained as Blazor extension. | Repaired and matched |
| `Root.locale` | Locale for formatted value. | `ProgressRoot.Locale`. | Repaired and matched |
| `Root.getAriaValueText` | Callback for custom `aria-valuetext`. | `ProgressRoot.GetAriaValueText`. | Matched |
| `Root.render` | Custom render element. | `ProgressRoot.Render`. | Matched |
| `className` / `style` | Static or function of state. | `ClassValue` / `StyleValue`. | Matched |
| Part render props | Custom element rendering for each part. | `RenderElement<TState>` on every part. | Matched |
| `Value.children` function | Receives formatted value and raw value. | `Func<string?, double?, RenderFragment>` child callback. | Repaired and matched |

## Rendered Behavior Comparison

| Check | React Base UI source/docs | Blazor result |
| --- | --- | --- |
| Active root | Root has progressbar role, value ARIA, `aria-valuetext`, label linkage, and `data-progressing`. | Unit, Playwright, and in-app Browser verified matching DOM. |
| Complete state | Root, track, indicator, label, and value expose `data-complete`. | Unit and Playwright verified all parts. |
| Indeterminate state | Value-dependent ARIA is omitted; state attr is `data-indeterminate`. | Unit and Playwright verified root, track, indicator, label, and value. |
| Label association | Label id registers into root `aria-labelledby`. | Unit and in-app Browser verified generated and explicit id paths, including id changes. |
| Presentation semantics | Label and hidden helper use `role="presentation"`. | Unit, Playwright, and in-app Browser verified. |
| Hidden NVDA helper | Root renders visually hidden helper span with text `x`. | Unit, Playwright, and in-app Browser verified. |
| Indicator style | Width is derived from raw percent calculation and respects merge order. | Unit and Playwright verified default, custom range, edge arithmetic, and override precedence. |
| Value content | Default renders formatted value; function child can render custom markup. | Unit and Playwright verified text and render function paths. |
| Missing context | Parts throw exact Base UI missing-context diagnostic. | Unit and Playwright verified all non-root parts. |

## Documentation Delta

No public Progress demo behavior required adjustment beyond changing demo calls from the removed string `Format` usage to `FormatString`. The upstream source docs remain valid for the repaired Blazor API with the following naming translation:

- React `format` maps to Blazor `Format` for `NumberFormatOptions`.
- Blazor `FormatString` and `FormatProvider` are retained as .NET-specific extensions.
- React `className` and `style` function props map to `ClassValue` and `StyleValue`.

## Conclusion

The repaired Blazor Progress component matches the audited React source docs and tested source behavior. Source docs validation, React source tests, targeted Blazor unit tests, targeted Blazor Playwright tests, and in-app Browser DOM inspection all passed for Progress.
