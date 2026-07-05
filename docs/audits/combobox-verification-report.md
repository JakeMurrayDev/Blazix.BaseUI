# Combobox Verification Report

## Commands

| Command | Result | Evidence |
| --- | --- | --- |
| `dotnet test tests/Blazix.BaseUI.Tests/Blazix.BaseUI.Tests.csproj --filter "FullyQualifiedName~ComboboxRootTests"` | Passed: 11, Failed: 0 | `docs/audits/logs/combobox-bunit.txt` |
| `dotnet test tests/Blazix.BaseUI.Playwright.Tests/Blazix.BaseUI.Playwright.Tests/Blazix.BaseUI.Playwright.Tests.csproj --filter "FullyQualifiedName~Combobox"` | Passed: 16, Failed: 0 | `docs/audits/logs/combobox-playwright.txt` |
| `dotnet build Blazix.BaseUI.slnx` | Succeeded: 0 warnings, 0 errors | `docs/audits/logs/combobox-build.txt` |
| `bash scripts/lint-rules.sh` | Completed with 0 textual violations | `docs/audits/logs/combobox-lint.txt` |
| `node --check src/Blazix.BaseUI/wwwroot/blazix-baseui-combobox.js` | Passed | `docs/audits/logs/combobox-node-check.txt` |
| `node --check src/Blazix.BaseUI/wwwroot/blazix-baseui-combobox.min.js` | Passed | `docs/audits/logs/combobox-node-check.txt` |
| `dotnet test tests/Blazix.BaseUI.Tests/Blazix.BaseUI.Tests.csproj --filter "FullyQualifiedName~Docs"` | Passed: 12, Failed: 0 | `docs/audits/logs/combobox-docs-bunit.txt` |
| `dotnet build docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.csproj` | Succeeded: 0 warnings, 0 errors | `docs/audits/logs/combobox-docs-build.txt` |
| `pnpm --dir .base-ui --filter docs dev` | Source docs served on port 3005 | `docs/audits/logs/combobox-source-docs.txt` |
| In-app browser against Blazor test host | Passed targeted DOM state checks | `docs/audits/logs/combobox-inapp-browser.txt` |
| In-app browser against Blazix docs route `/components/combobox` | Passed route, heading, all visible example popup checks, and CSS/Tailwind variant switch | `docs/audits/logs/combobox-docs-inapp-browser.txt` |
| `git -C .base-ui fetch origin master` then `git -C .base-ui log HEAD..origin/master -- packages/react/src/combobox docs/src/app/(docs)/react/components/combobox` | No remote Combobox source/docs commits after local mirror | Recorded below |

## Automated Browser Coverage

The Playwright suite covers Server and WASM render modes for:

- single item press selection
- hidden single-value form serialization
- multiple item press toggle behavior
- repeated hidden inputs for multiple selection
- selected item indicators and `aria-selected`
- chip removal and `ChipRemovePress`
- clear press and focus retention
- input click opens popup by default in Server and WASM render modes
- keyboard highlight and Enter commit
- Enter with no active item submitting the form
- disabled, readonly, required ARIA/native/data attributes
- input rendered inside popup
- popup pointer-down focus retention

## In-App Browser Check

The in-app browser was used against the component test host:

```text
http://127.0.0.1:5123/tests/combobox/server?defaultOpen=true&multiple=true&defaultValues=Apple
```

Observed results:

- initial state: interactive true; popup open true; selected values `Apple`; hidden values `[Apple]`
- after selecting Banana: popup open true; selected values `Apple,Banana`; hidden values `[Apple, Banana]`
- after removing Apple chip: selected values `Banana`; hidden values `[Banana]`; reason `ChipRemovePress`; active element `combobox-input`

The in-app browser was also used against the docs route:

```text
http://127.0.0.1:5124/components/combobox
http://127.0.0.1:5227/components/combobox
```

Observed results:

- route title `Combobox · Blazix.BaseUI`, H1 `Combobox`, and no Blazor error boundary
- hydrated docs state with `document.documentElement.dataset.blazixInteractive = "true"`
- all upstream source docs example headings rendered: typed wrapper component, multiple select, input inside popup, grouped, async search (single), async search (multiple), creatable, virtualized, memoizing items
- hero demo trigger opened one listbox with `Apple` and related fruit options; selecting `Apple` closed the popup, updated the input value, and showed the clear button
- multiple-select demo filtered to `TypeScript`; selecting it rendered a chip with `aria-label="TypeScript"` and a remove button with `tabindex="-1"`
- follow-up visual popup pass opened every visible docs example in the in-app browser: hero, multiple select, input inside popup, grouped, async search (single), async search (multiple), creatable, and virtualized
- creatable visual pass verified typing `security` renders `Create "security"` in the popup
- styling-variant combobox was switched from CSS to Tailwind, then the hero Combobox popup was opened again and rendered fruit options

The first attempted docs host used `--no-launch-profile`; that was rejected as verification evidence because static web assets and hydration did not load under that invalid launch configuration. The docs host was restarted with the normal launch profile before the recorded checks.

## Source Docs Check

The React docs were served with PNPM:

```bash
pnpm --dir .base-ui --filter docs dev
```

Observed route:

```text
http://127.0.0.1:3005/react/components/combobox
```

Observed results:

- title: `Combobox · Base UI`
- H1: `Combobox`
- route contains multiple select, input-inside-popup, grouped, and virtualized sections
- multiple docs demo exposes input `role="combobox"`, `aria-haspopup="listbox"`, `aria-autocomplete="list"`, and controlled listbox state when open
- selecting `TypeScript` renders a chip with `aria-label="TypeScript"` and a remove button with `aria-label="Remove TypeScript"` and `tabindex="-1"`

## Upstream Freshness Check

Fetched remote metadata without changing the `.base-ui` working tree:

```bash
git -C .base-ui fetch origin master
```

Remote result:

```text
95cf9e033..ca246a606  master -> origin/master
```

Combobox path delta:

```bash
git -C .base-ui log HEAD..origin/master -- packages/react/src/combobox docs/src/app/\(docs\)/react/components/combobox
git -C .base-ui diff --stat HEAD..origin/master -- packages/react/src/combobox docs/src/app/\(docs\)/react/components/combobox
```

Both commands produced no Combobox source or docs changes. The relevant Combobox upstream fixes are therefore the commits already present in local `.base-ui` and cataloged in `docs/audits/combobox-functional-audit.md`.

## Notes

- The lint script emitted `declare: -A: invalid option` under the local shell, then completed the textual subset with 0 violations. Structural rules are enforced by `src/Blazix.BaseUI.Analyzers` and were covered by `dotnet build Blazix.BaseUI.slnx`.
- The in-app browser DOM snapshot API failed in this runtime, so verification used targeted DOM evaluation and stable `data-testid` locators after the in-app browser successfully navigated to the local pages.
