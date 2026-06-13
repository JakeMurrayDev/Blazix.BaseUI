# Toggle and ToggleGroup Source Docs Comparison

## Source Docs Run

| Command | Result | Log |
| --- | --- | --- |
| `cd .base-ui && pnpm --version` | Passed. `10.33.4`. | `docs/audits/logs/toggle-toggle-group-pnpm-version.log` |
| `cd .base-ui && pnpm install --frozen-lockfile` | Passed. Lockfile up to date; dependencies already installed. | `docs/audits/logs/toggle-toggle-group-pnpm-install.log` |
| `cd .base-ui && pnpm docs:dev` | Passed. Next.js docs server served on `http://localhost:3005`. | Terminal session |
| `dotnet run --project demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo.csproj --urls http://127.0.0.1:5281` | Passed. Blazor demo served on `http://127.0.0.1:5281`. | Terminal session |
| `dotnet build BlazorBaseUI.slnx` | Passed. 0 warnings, 0 errors. | `docs/audits/logs/toggle-toggle-group-docs-comparison-dotnet-build.log` |
| `curl` GET checks for upstream docs routes | Passed. Public Toggle, public ToggleGroup, and private ToggleGroup experiment returned 200. | `docs/audits/logs/toggle-toggle-group-source-docs-routes.log` |
| `curl` GET checks for Blazor demo routes | Passed. `/toggle` and `/toggle-group` returned 200. | `docs/audits/logs/toggle-toggle-group-blazor-docs-routes.log` |

## Source Files Compared

| Area | React Base UI source | BlazorBaseUI source |
| --- | --- | --- |
| Toggle public docs | `.base-ui/docs/src/app/(docs)/react/components/toggle/page.mdx` | `demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo.Client/Shared/Sections/ToggleSection.razor` |
| Toggle public hero demo | `.base-ui/docs/src/app/(docs)/react/components/toggle/demos/hero/tailwind/index.tsx` | `demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo.Client/Shared/Sections/ToggleSection.razor` |
| ToggleGroup public docs | `.base-ui/docs/src/app/(docs)/react/components/toggle-group/page.mdx` | `demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo.Client/Shared/Sections/ToggleGroupSection.razor` |
| ToggleGroup public demos | `.base-ui/docs/src/app/(docs)/react/components/toggle-group/demos/hero/tailwind/index.tsx`; `.base-ui/docs/src/app/(docs)/react/components/toggle-group/demos/multiple/tailwind/index.tsx` | `demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo.Client/Shared/Sections/ToggleGroupSection.razor` |
| ToggleGroup private experiment | `.base-ui/docs/src/app/(private)/experiments/toggle-group.tsx` | `demo/BlazorBaseUI.Demo/BlazorBaseUI.Demo.Client/Shared/Sections/ToggleGroupSection.razor` |

## Rendered Behavior Comparison

| Check | Upstream rendered docs | Blazor rendered docs |
| --- | --- | --- |
| Toggle hero click | `Favorite` changed `aria-pressed` from `false` to `true`; `data-pressed` became present. | Existing Toggle demos render `aria-pressed`; icon-only buttons now expose `aria-label` values `Favorite`, `Star`, and `Bookmark`. |
| ToggleGroup single-select public demo | `Align center` click made only `Align center` pressed; root had `role="group"` and `data-orientation="horizontal"`. | Basic and alignment demos render matching group and button state attributes. |
| ToggleGroup multiple public demo | `Bold` and `Italic` started pressed; clicking `Underline` made all three pressed; root had `data-multiple`. | Multiple and icon formatting demos expose `data-multiple`; icon formatting buttons now have `Bold`, `Italic`, `Underline`, and `Strikethrough` names. |
| Controlled non-empty private experiment | Clicking the pressed `Align center` button left it pressed because the source ignores empty values. | Added `Required Selection` demo using controlled `Value`, `ValueChanged`, and `OnValueChange.Cancel()`; browser check confirmed `Align center` remained `aria-pressed="true"` after click. |
| RTL keyboard behavior | Canonical React source tests use `DirectionProvider` for RTL. The private experiment renders `dir="rtl"` without `DirectionProvider`, so it is not the canonical RTL keyboard reference. | Added `RTL Direction` demo using Blazor `DirectionProvider` plus `dir="rtl"`; browser check confirmed ArrowLeft moved focus from `Toggle bold` to `Toggle italic`. |
| Icon-only accessibility | React source supplies `aria-label` on every icon-only Toggle in the compared demos. | Fixed all icon-only Toggle and ToggleGroup demo buttons to expose accessible names. Browser check confirmed `namelessPressedButtons: 0`. |

## Resolved Documentation Gaps

- Added accessible names to icon-only Toggle demo buttons.
- Added accessible names and group labels to icon-only ToggleGroup formatting and alignment demos.
- Added a controlled required-selection ToggleGroup demo matching the upstream private experiment pattern.
- Added an RTL ToggleGroup demo using `DirectionProvider` to match the canonical React source tests.
