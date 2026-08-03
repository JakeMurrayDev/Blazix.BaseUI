# Audit Freshness Matrix

Research for wayfinder ticket #147: which components were last audited against which upstream state.

Date: 2026-08-03
Method: primary sources only — `docs/audits/*` contents, `git log` on those files, and the repo's merged PR titles (`gh pr list --state merged`).
Current upstream pin: `.base-ui` at `bdcb685fadcca9d18b18f013c052795a53b6aa33` (origin/master, 2026-07-18). Corroborated in-repo by `docs/audits/drawer-upstream-delta-2026-07.md`, `docs/audits/select-functional-audit.md` (which records `git ls-remote origin refs/heads/master` resolving to `bdcb685fa` on 2026-07-21), and `docs/audits/toast-functional-audit.md`.

## Summary

40 component surfaces tracked (directories in `src/Blazix.BaseUI/`, excluding infrastructure: `Base`, `Portal`, `Utilities`, `wwwroot`).

| Staleness bucket | Count | Components |
| --- | --- | --- |
| **Current** (audited against the `bdcb685fa` pin) | 3 | Drawer, Select, Toast |
| **Audited before current pin** (artifacts exist, pre-2026-07-18 upstream state) | 25 | Accordion, AlertDialog, Autocomplete, Checkbox, CheckboxGroup, Collapsible, Combobox, Dialog, Field, Fieldset, Form, Menu, NumberField, OtpField, Popover, PreviewCard, Progress, Radio, RadioGroup, ScrollArea, Switch, Toggle, ToggleGroup, Toolbar, Tooltip |
| **Never delta-audited** (no audit artifacts in `docs/audits/`) | 12 | Avatar, Button, ContextMenu, Csp, DirectionProvider, Input, MenuBar, Meter, NavigationMenu, Separator, Slider, Tabs |

### Stalest corners

1. **NavigationMenu** — parity work was PR #72 (merged 2026-05-11), the oldest parity PR of any component, and it left no artifact in `docs/audits/`. One of the largest component surfaces in the library with zero recorded upstream reference.
2. **Meter and Tabs** — both were audited (PR #76 merged 2026-05-24, PR #77 merged 2026-05-25) and their audit docs were then deleted two days later in commit `7a8f9560` ("Remove audit docs", 2026-05-26). No audit evidence survives in the tree.
3. **Tooltip** — oldest surviving artifact (`tooltip-functional-audit.md`, dated 2026-05-27), a single doc with no parity matrix, no verification report, and no stated upstream commit. PR #133 (merged 2026-07-26) fixed tooltip transitions without refreshing the audit.
4. **ContextMenu, MenuBar, Slider** — parity-fix PRs only (#74 2026-05-16, #75 2026-05-24, #89 2026-05-31), no artifacts. ContextMenu inherits some coverage from the Menu audit's shared floating internals (2026-06-30) but has no audit doc of its own.
5. **The late-May cluster** — Radio/RadioGroup, Field/Fieldset/Form, ScrollArea (2026-05-28), Checkbox/CheckboxGroup (2026-05-29/30), Switch (2026-05-30): artifacts exist but state no upstream commit, and per the July delta audits, 233–255 first-parent upstream commits landed between late-May baselines and the current pin.

The three "current" audits are also the only ones performed with the repo's mature delta methodology (baseline SHA -> head SHA, every intervening commit dispositioned). Every earlier audit either predates that methodology or references an intermediate mirror state.

### Upstream states encountered, oldest to newest

| Upstream state | Date | Audits pinned to it |
| --- | --- | --- |
| unstated (mirror state at audit time, no SHA recorded) | 2026-05-27 … 2026-06-26 | Tooltip, Radio, Field group, ScrollArea, Checkbox, Switch, Toolbar, Toggle group, PreviewCard, Progress, OtpField, Accordion, Collapsible |
| `748f4228d` | mirror state as of 2026-06-27..30 | Popover, Dialog/AlertDialog, Menu |
| `95cf9e0339567518ccdf82628c8ef4f3d67cad07` | mirror fast-forwarded 2026-07-03 | Autocomplete, Combobox, NumberField |
| `ca246a6068d98f8fa27fa1c382743184550a0360` | origin/master fetched 2026-07-05 | Combobox and NumberField verified no component-relevant remote deltas beyond `95cf9e033` |
| `bdcb685fadcca9d18b18f013c052795a53b6aa33` | origin/master 2026-07-18 (current pin) | Drawer, Select, Toast |

## Full matrix

Audit dates are the `Date:` headers inside the docs; git commit dates corroborate (note several docs authored late May were batch-committed 2026-06-11 in `623ce0bb` "docs: add audit artifacts").

| Component | Audit artifacts present | Last audit date | Upstream state audited against | Staleness | Evidence |
| --- | --- | --- | --- | --- | --- |
| Accordion | functional-audit (+ superseded 2026-06-01 copy), parity-matrix, source-docs-comparison, verification-report, logs | 2026-06-26 | unknown SHA; dispositioned named upstream commits `3980d3576`, `be47a6214`, `a3cfc4f98`, `9069ba886`, `4133d56f7` (+ Collapsible fixes) against the mirror as of 2026-06-26 | pre-pin | `accordion-functional-audit.md`; commit `7ea6c5f6` (PR #121) |
| AlertDialog | covered by Dialog audit set (dialog docs explicitly audit `alert-dialog/**`) | 2026-06-29 | `.base-ui` @ `748f4228d` | pre-pin | `dialog-parity-matrix.md` header; `dialog-functional-audit.md` |
| Autocomplete | functional-audit, parity-matrix, verification-report | 2026-07-03 (review fixes 2026-07-04) | `95cf9e0339567518ccdf82628c8ef4f3d67cad07` (mirror fast-forwarded during audit) | pre-pin | `autocomplete-functional-audit.md`, `autocomplete-verification-report.md`; commits `a7473b7c`, `f6caba3a` (PR #126) |
| Avatar | none | never | unknown | never delta-audited | no `docs/audits/avatar*`; no audit PR in merged PR list |
| Button | none | never | unknown | never delta-audited | no `docs/audits/button*`; PR #105 is docs-only |
| Checkbox | functional-audit, parity-matrix (both cover CheckboxGroup) | 2026-05-29 (follow-up 2026-05-30) | unknown — no upstream commit stated | pre-pin | `checkbox-functional-audit.md`; PR #87 (2026-05-30) |
| CheckboxGroup | shared with Checkbox audit set | 2026-05-29/30 | unknown | pre-pin | `checkbox-functional-audit.md` (title covers both) |
| Collapsible | functional-audit, parity-matrix, verification-report, logs | 2026-06-26 | unknown SHA; dispositioned upstream commits `e18d78832`, `d33150322`, `4133d56f7`, `3d0be4e37` | pre-pin | `collapsible-functional-audit.md`; commit `a646d083` |
| Combobox | functional-audit, parity-matrix, verification-report | 2026-07-05 | local mirror `95cf9e033`; remote `origin/master` `ca246a606` fetched and verified to contain no further Combobox source/docs commits | pre-pin | `combobox-functional-audit.md` "Upstream mirror state", `combobox-verification-report.md`; PR #127 |
| ContextMenu | none (shared floating internals partially covered by Menu audit 2026-06-30) | never (parity PRs only) | unknown | never delta-audited | PR #74 "Improve ContextMenu parity" (2026-05-16), PR #125 fix (2026-07-03); no `docs/audits/context-menu*` |
| Csp (utility provider) | none | never | unknown | never delta-audited | PR #118 refactor only |
| Dialog | functional-audit, parity-matrix, source-docs-comparison, verification-report | 2026-06-29 | `.base-ui` @ `748f4228d` | pre-pin | `dialog-parity-matrix.md`, `dialog-functional-audit.md`; commit `5e2b4046` (PR #123) |
| DirectionProvider (utility provider) | none | never | unknown | never delta-audited | PR #118 refactor only |
| Drawer | functional-audit, parity-matrix, verification-report, **upstream-delta doc** | 2026-07-18 | `.base-ui` @ `bdcb685fa` (origin/master 2026-07-18); baseline `7c25be77` (2026-05-27), 233 first-parent commits dispositioned | **current** | `drawer-upstream-delta-2026-07.md`, `drawer-verification-report.md`; commit `252b838d` (PR #130) |
| Field | functional-audit, parity-matrix (cover Fieldset + Form) | 2026-05-28 | unknown — no upstream commit stated | pre-pin | `field-fieldset-form-functional-audit.md`; PR #86 |
| Fieldset | shared with Field audit set | 2026-05-28 | unknown | pre-pin | `field-fieldset-form-functional-audit.md` |
| Form | shared with Field audit set | 2026-05-28 | unknown | pre-pin | `field-fieldset-form-functional-audit.md` |
| Input | none | never | unknown | never delta-audited | PR #109 is docs-only; no `docs/audits/input*` |
| Menu | functional-audit, parity-matrix, source-docs-comparison, verification-report | 2026-06-30 | `.base-ui` @ `748f4228d` | pre-pin | `menu-parity-matrix.md` / `menu-verification-report.md` headers; commit `b23597be` (PR #124) |
| MenuBar | none | never (parity-fix PR only) | unknown | never delta-audited | PR #75 "Fix menubar parity gaps" (2026-05-24); no `docs/audits/menubar*` |
| Meter | none surviving — `meter-functional-audit.md` added in `1dd6b4cc` (2026-05-24), deleted in `7a8f9560` "Remove audit docs" (2026-05-26) | 2026-05-24 (artifacts deleted) | unknown | never delta-audited (no surviving artifacts) | git history of `docs/audits/meter-functional-audit.md`; PR #76 |
| NavigationMenu | none | never (parity PR only, oldest of all) | unknown | never delta-audited | PR #72 "Audit NavigationMenu component parity" (2026-05-11); no `docs/audits/navigation-menu*` |
| NumberField | functional-audit, parity-matrix, verification-report | 2026-07-05 | mirror HEAD `95cf9e033`; `origin/master` fetched at `ca246a606` | pre-pin | `number-field-functional-audit.md`; commit `be47e631` (PR #129) |
| OtpField | functional-audit, parity-matrix, source-docs-comparison, verification-report | 2026-06-21 | unknown — no upstream commit stated | pre-pin | `otp-field-functional-audit.md`; commit `2dee8bc1` (PR #116) |
| Popover | functional-audit (2 passes), parity-matrix, source-docs-comparison | 2026-06-28 (second-pass upstream-delta re-verification) | `.base-ui` @ `748f4228d` | pre-pin | `popover-functional-audit.md`; commit `cc8ad0cf` (PR #122) |
| PreviewCard | functional-audit, parity-matrix, source-docs-comparison | 2026-06-07 | unknown — no upstream commit stated | pre-pin | `preview-card-functional-audit.md`; PR #98 |
| Progress | functional-audit, parity-matrix, source-docs-comparison | 2026-06-07 | unknown — no upstream commit stated | pre-pin | `progress-functional-audit.md`; PR #97 |
| Radio | functional-audit, parity-matrix (cover RadioGroup) | 2026-05-28 | unknown — no upstream commit stated | pre-pin | `radio-functional-audit.md`; PR #85 |
| RadioGroup | shared with Radio audit set | 2026-05-28 | unknown | pre-pin | `radio-functional-audit.md` (title covers both) |
| ScrollArea | functional-audit only (+ logs) | 2026-05-28 | unknown — no upstream commit stated | pre-pin | `scroll-area-functional-audit.md`; commit `c9cb607a` (PR #84) |
| Select | functional-audit, parity-matrix, verification-report | 2026-07-21 | `bdcb685fadcca9d18b18f013c052795a53b6aa33` (baseline `b6ec388df`, 255 intervening commits screened; live `ls-remote` check confirmed head) | **current** | `select-functional-audit.md`; commit `4269ebfc` (PR #131) |
| Separator | none | never | unknown | never delta-audited | no `docs/audits/separator*`; no audit PR |
| Slider | none | never (parity-fix PRs only) | unknown | never delta-audited | PR #89 "slider parity fix" (2026-05-31), PR #114 (2026-06-17); no `docs/audits/slider*` |
| Switch | functional-audit, parity-matrix | 2026-05-30 | unknown — no upstream commit stated | pre-pin | `switch-functional-audit.md`; PR #88 |
| Tabs | none surviving — `tabs-functional-audit.md` added in `d68abffc` (2026-05-25), deleted in `7a8f9560` "Remove audit docs" (2026-05-26) | 2026-05-25 (artifacts deleted) | unknown | never delta-audited (no surviving artifacts) | git history of `docs/audits/tabs-functional-audit.md`; PR #77 |
| Toast | functional-audit, parity-matrix, source-docs-comparison, verification-report | 2026-07-21 | `bdcb685fadcca9d18b18f013c052795a53b6aa33` (baseline `b6ec388df`, all 255 intervening commits screened) | **current** | `toast-functional-audit.md`; commit `2eb7119c` (PR #132) |
| Toggle | functional-audit, parity-matrix, source-docs-comparison (cover ToggleGroup; no Date header in docs) | ~2026-06-06 (PR merge date) | unknown — no upstream commit stated | pre-pin | `toggle-toggle-group-functional-audit.md`; PR #94 "Repair toggle parity" (2026-06-06) |
| ToggleGroup | shared with Toggle audit set | ~2026-06-06 | unknown | pre-pin | `toggle-toggle-group-functional-audit.md` |
| Toolbar | functional-audit, parity-matrix | 2026-06-06 | unknown — no upstream commit stated | pre-pin | `toolbar-functional-audit.md`; PR #95 |
| Tooltip | functional-audit only — no parity matrix, no verification report | 2026-05-27 | unknown — no upstream commit stated | pre-pin (oldest surviving artifact) | `tooltip-functional-audit.md`; PR #82. Note: PR #133 (2026-07-26) changed tooltip transitions with no audit refresh |

## Caveats

- The vendored `.base-ui` clone could not be inspected directly from this research worktree (git isolation); the `bdcb685fadcc` / 2026-07-18 pin is taken from the ticket and independently corroborated by three audit docs listed above.
- "Never delta-audited" means no artifact exists in `docs/audits/` today. Several of those components did receive parity work via PRs (noted per row), but with no recorded upstream reference, that work cannot be dated against upstream.
- `AlertDialog`, `CheckboxGroup`, `RadioGroup`, `Fieldset`, `Form`, and `ToggleGroup` have no dedicated docs but are explicitly in-scope of a sibling component's audit set; they inherit that audit's date and upstream state.
- The Select audit (255 commits from `b6ec388df`) and Drawer audit (233 commits from `7c25be77`, 2026-05-27) report their own baselines; the docs do not date `b6ec388df`, so it is left undated here rather than guessed.
- Infrastructure directories (`Base`, `Portal`, `Utilities`, `wwwroot`) are excluded from the matrix; shared floating/focus-manager JS is audited transitively through the popup components (most recently by the Drawer, Select, and Toast audits at the current pin).
