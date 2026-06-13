# PreviewCard Source Docs Comparison

## Source Inputs

- React source: `.base-ui/packages/react/src/preview-card`
- React docs page: `.base-ui/docs/src/app/(docs)/react/components/preview-card/page.mdx`
- Live source docs URL: `http://127.0.0.1:3005/react/components/preview-card`
- Blazor implementation: `src/BlazorBaseUI/PreviewCard` and `src/BlazorBaseUI/wwwroot/blazor-baseui-preview-card.js`

## PNPM Evidence

- `pnpm --version`: `10.33.4`
- `pnpm --filter docs run typescript`: passed.
- `pnpm --filter docs dev`: served the docs app at `http://localhost:3005`.
- In-app browser source-doc check confirmed:
  - page title `Preview Card · Base UI`
  - heading `Preview Card`
  - anatomy includes `PreviewCard.Root` and `PreviewCard.Viewport`
  - docs include detached triggers and multiple triggers
  - docs include viewport `data-current`, `data-previous`, and `data-activation-direction`
- `pnpm vitest run --project @base-ui/react packages/react/src/preview-card`: passed source tests with 160 passed and 78 upstream-skipped.

## Source Docs Requirements Matched

| Source docs behavior | Blazor result |
|---|---|
| Preview Card is a link-triggered popup for visual preview content. | `PreviewCardTrigger` renders `<a>` and JS hover/focus opens the card. |
| Anatomy includes Root, Trigger, Portal, Backdrop, Positioner, Popup, Arrow, Viewport. | All parts exist and are wired through contexts/interop. |
| Detached triggers use a handle from `createHandle()`. | `PreviewCardHandle<TPayload>` supports detached triggers and now syncs root JS ID to detached triggers. |
| Multiple triggers can share one preview card. | Root no longer returns early when open; active trigger and payload can change while open. |
| Payload renders different content by active trigger. | `ChildContentWithPayload` updates on active trigger switch. |
| Controlled mode uses `open`, `onOpenChange`, `triggerId`, trigger `id`; event details include trigger. | `Open`, `OpenChanged`, `OnOpenChange`, `TriggerId`, trigger `Id`, and event args with `TriggerId`/`TriggerElement`. |
| Position/size animation uses Positioner and Popup. | Positioner gets collision updates and closed/start styles; Popup retains transition state. |
| Content animation uses Viewport with `data-current`, `data-previous`, `data-activation-direction`. | Viewport implementation now wraps current content and calls shared JS viewport transition module. |

## Differences Accounted

- React stores inline rect cursor coordinates in `inlineRectCoordsRef`; Blazor keeps pointer/DOM-heavy behavior in JS and syncs active trigger IDs to C#.
- React uses hook composition (`useDismiss`, `useHoverReferenceInteraction`, `useFocus`, `usePopupViewport`); Blazor uses component lifecycle methods plus component-specific JS interop and shared floating/viewport JS modules.
- Source `.spec.tsx` files are excluded by upstream Vitest config. They were used as static source references; runnable source evidence comes from `.test.tsx` files.
