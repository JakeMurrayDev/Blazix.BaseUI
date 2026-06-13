# PreviewCard Functional Audit

Date: 2026-06-07

## Result

Status: repaired and verified.

The Blazor PreviewCard port now accounts for the React Base UI source behavior in `.base-ui/packages/react/src/preview-card`. Repairs were applied to root state transitions, detached/multiple trigger handling, JS hover/dismiss interop, popup/positioner/arrow/backdrop attributes, viewport content transitions, and bUnit/JS interop coverage.

## Resolved Gaps

- Root no longer ignores active trigger changes while already open. Payload and active trigger switch now update while `open == true`.
- `PreviewCardOpenChangeEventArgs` now carries `TriggerId` and `TriggerElement` parity with React event details.
- `PreventUnmountOnClose` is now honored through transition completion.
- Close instant mapping now matches React: `TriggerPress` and `EscapeKey` use dismiss instant; outside press does not.
- JS hover is default and trigger-aware. It supports multiple triggers, detached triggers through handles, safe polygon hover, delay updates, and trigger marker synchronization.
- JS outside-press dismiss is implemented through shared floating dismiss interaction.
- JS root open state now receives the active trigger ID and syncs initial/default-open state.
- Popup now renders `tabindex="-1"` and `data-floating-ui-focusable=""`.
- Popup, Positioner, and Arrow no longer render non-source `data-instant` attributes.
- Positioner now receives computed placement callbacks and updates side/align/anchor-hidden/arrow-uncentered state after collision.
- Positioner now applies source-equivalent `pointer-events: none` when closed and `transition: none` during starting style.
- Backdrop now forces `pointer-events: none; user-select: none; -webkit-user-select: none`.
- Viewport now wraps current content with `data-current`, manages trigger-change content keys, exposes `data-activation-direction`, `data-transitioning`, and `data-instant`, and registers JS viewport transition/auto-resize hooks.
- Test JS interop setup now stubs both `.js` and `.min.js` PreviewCard modules and all new exports.

## Verification

| Command | Result | Log |
|---|---:|---|
| `dotnet test tests/BlazorBaseUI.Tests/BlazorBaseUI.Tests.csproj --filter "FullyQualifiedName~PreviewCard" -v minimal` | Passed, 87/87 | `docs/audits/logs/preview-card-repair-bunit-3.log` |
| `dotnet build BlazorBaseUI.slnx -v minimal` | Passed, 0 warnings/errors | `docs/audits/logs/preview-card-build-2.log` |
| `bash scripts/lint-rules.sh` | Passed, 0 violations | `docs/audits/logs/preview-card-lint-3.log` |
| `dotnet test tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests/BlazorBaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~PreviewCard" -v minimal` | Passed, 48/48 | `docs/audits/logs/preview-card-playwright-2.log` |
| `.base-ui/node_modules/.bin/terser src/BlazorBaseUI/wwwroot/blazor-baseui-preview-card.js --compress --mangle --module -o src/BlazorBaseUI/wwwroot/blazor-baseui-preview-card.min.js` | Passed | no output |
| `pnpm --filter docs run typescript` from `.base-ui` | Passed | `docs/audits/logs/preview-card-source-docs-typescript.log` |
| `pnpm vitest run --project @base-ui/react packages/react/src/preview-card` from `.base-ui` | Passed, 160 passed, 78 upstream-skipped | `docs/audits/logs/preview-card-source-react-tests.log` |
| In-app browser against `http://127.0.0.1:5261/tests/preview-card/server?...` | Passed live open/close attribute check | `docs/audits/logs/preview-card-in-app-browser-check.json` |
| In-app browser against `http://127.0.0.1:3005/react/components/preview-card` | Passed source-doc section check | `docs/audits/logs/preview-card-source-docs-browser-check.json` |

## Notes

- `pnpm --filter docs docs:validate` was attempted and failed because the `docs` package does not define a package-level `docs:validate` script. The available source-doc check used for this audit was `pnpm --filter docs run typescript`, and the live docs page was run with `pnpm --filter docs dev`.
- React `.spec.tsx` files are present for source reference but excluded by the upstream Vitest project config (`exclude: **/*.spec.*`). Direct execution returned "No test files found" by design; see `docs/audits/logs/preview-card-source-react-specs.log`.
