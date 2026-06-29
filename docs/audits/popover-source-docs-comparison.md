# Popover Source Docs Comparison

Date: 2026-06-27

## Scope

Compared React Base UI Popover documentation and generated API metadata against the Blazix.BaseUI Popover docs and API metadata.

Source docs:

- `.base-ui/docs/src/app/(docs)/react/components/popover/page.mdx`
- `.base-ui/docs/src/app/(docs)/react/components/popover/types.md`
- `.base-ui/packages/react/src/popover/root/PopoverRoot.tsx`
- `.base-ui/packages/react/src/popover/positioner/PopoverPositionerCssVars.ts`
- `.base-ui/packages/react/src/popover/viewport/PopoverViewportDataAttributes.ts`

Local docs:

- `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Content/Components/popover.md`
- `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Data/PopoverApi.cs`
- `docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Components/Docs/ApiPartReference.razor`

## Commands

| Command | Result | Log |
| --- | --- | --- |
| `pnpm -C .base-ui docs:validate "(docs)/react/components/popover"` | Passed; one `page.mdx` and one `types.md` processed, no updates required. | `docs/audits/logs/popover-source-docs-validate.log` |
| `pnpm -C .base-ui docs:dev` | Passed; Base UI docs served on `http://localhost:3005` for in-app browser inspection. | Console session; browser facts stored below |
| In-app browser inspection of `http://127.0.0.1:3005/react/components/popover` | Passed; source Popover page rendered with API tables. | `docs/audits/logs/popover-source-docs-browser.log` |
| `dotnet run --project docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.csproj --urls http://127.0.0.1:5216` | Passed; Blazix docs served for comparison. | Console session; browser facts stored below |
| In-app browser inspection of `http://127.0.0.1:5216/components/popover` | Passed; Blazix Popover page rendered updated modal, data-attribute, and CSS-variable docs. | `docs/audits/logs/popover-source-docs-browser.log` |

## Documented Upstream Changes

| Upstream docs/source change | Required local docs behavior | Local result |
| --- | --- | --- |
| `PopoverRoot.modal` JSDoc now states that touch devices block outside taps while leaving page scrollable unless the popup nearly spans viewport width. | `PopoverRoot.Modal` docs must describe `True` scroll lock with the same touch exception. | Updated in markdown docs and `PopoverApi.RootRows`. |
| `PopoverPositionerCssVars.ts` now declares typed CSS variable metadata for available/anchor/positioner dimensions and transform origin. | Blazix API reference must render data/CSS type columns and show number/string types where available. | `ApiPartReference.razor` conditionally renders type columns; `PopoverApi.Positioner` includes number/string types. |
| `PopoverViewportDataAttributes.ts` states `data-activation-direction` contains space-separated horizontal/vertical axis values. | Blazix viewport docs must document space-separated axis tokens and not imply a single direction enum. | Updated markdown docs and `PopoverApi.Viewport` data attribute row. |
| Source Popover docs include viewport popup dimension CSS variables. | Blazix viewport docs must include parent popup width/height variables for transition wrappers. | `PopoverApi.Viewport` includes `--popup-width` and `--popup-height`. |

## API Surface Comparison

| React API | Blazix API | Status |
| --- | --- | --- |
| `Popover.Root.defaultOpen` | `PopoverRoot.DefaultOpen` | Present |
| `Popover.Root.open` | `PopoverRoot.Open` / `OpenChanged` | Present |
| `Popover.Root.onOpenChange` | `PopoverRoot.OnOpenChange` / `PopoverOpenChangeEventArgs` | Present; Blazor event args include cancel, event, trigger ref, trigger id, reason, and interaction type. |
| `Popover.Root.onOpenChangeComplete` | `PopoverRoot.OnOpenChangeComplete` | Present |
| `Popover.Root.defaultTriggerId` | `PopoverRoot.DefaultTriggerId` | Present |
| `Popover.Root.triggerId` | `PopoverRoot.TriggerId` | Present; rendered id ownership fix integrated. |
| `Popover.Root.actionsRef` | `PopoverRoot.ActionsRef` | Present |
| `Popover.Root.handle` | `PopoverRoot.Handle` / `PopoverHandleFactory.CreateHandle()` | Present |
| `Popover.Root.modal` | `PopoverRoot.Modal` / `PopoverModalMode` | Present |
| `Popover.Trigger.handle` | `PopoverTrigger.Handle` | Present |
| `Popover.Trigger.payload` | `PopoverTrigger.Payload` | Present |
| `Popover.Trigger.openOnHover` | `PopoverTrigger.OpenOnHover` | Present |
| `Popover.Trigger.delay` | `PopoverTrigger.Delay` | Present |
| `Popover.Trigger.closeDelay` | `PopoverTrigger.CloseDelay` | Present |
| `Popover.Trigger.id` | `PopoverTrigger.Id` | Present |
| `Popover.Portal.keepMounted` | `PopoverPortal.KeepMounted` | Present |
| `Popover.Positioner` side/align/offset/collision/sticky/method/anchor props | `PopoverPositioner` parameters | Present |
| `Popover.Popup.initialFocus` | `PopoverPopup.InitialFocus` | Present |
| `Popover.Popup.finalFocus` | `PopoverPopup.FinalFocus` | Present; close interaction type verified. |
| `Popover.Close.nativeButton` | `PopoverClose.NativeButton` | Present |
| `render` prop | `RenderFragment<RenderProps<TState>>? Render` via `RenderElement<TState>` | Present |
| `className` / `style` functions | `ClassValue` / `StyleValue` | Present |

## Rendered Docs Comparison

| Rendered signal | Source docs | Blazix docs |
| --- | --- | --- |
| Page rendered | `Popover · Base UI` | `Popover · Blazix.BaseUI` |
| Modal touch-scroll behavior | Source JSDoc/API metadata inspected; local page includes equivalent wording. | Present |
| Positioner CSS variable docs | `--available-width`, `--available-height`, `--anchor-width`, `--anchor-height`, `--positioner-width`, `--positioner-height`, `--transform-origin`. | Present with type column. |
| Popup CSS variable docs | `--popup-width`, `--popup-height`. | Present. |
| Viewport activation direction | Space-separated horizontal/vertical axis values. | Present in type and description. |
| Viewport popup variables | Parent popup width/height variables for transitions. | Present. |

## Final Assessment

The source documentation comparison found no additional Popover implementation gap after the repair. The upstream docs changes from #5094 are represented in Blazix docs and API metadata, and the in-app browser confirmed both source and local pages render the relevant Popover documentation/API signals.
