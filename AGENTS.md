# Blazix.BaseUI

## Project Overview

Blazix.BaseUI is a Blazor component library that ports [Base UI](https://base-ui.com/) (React) components to Blazor. The library provides unstyled, accessible UI primitives for building design systems.

### Repository Links

- **GitHub**: <https://github.com/JakeMurrayDev/Blazix.BaseUI/>
- **GitHub API**: <https://api.github.com/repos/JakeMurrayDev/Blazix.BaseUI>

### Technology Stack

- **.NET Version**: .NET 10
- **Blazor**: Server and WebAssembly (Auto render mode)
- **Testing**: xUnit v3, bUnit, Playwright, Shouldly, NSubstitute

### Project Structure

```
Blazix.BaseUI/
├── src/
│   ├── Blazix.BaseUI/              # Main component library
│   │   ├── [Component]/           # Component folders
│   │   ├── Utilities/             # Shared utilities
│   │   └── wwwroot/               # JavaScript modules
│   └── Blazix.BaseUI.Analyzers/    # Roslyn analyzers
├── demo/
│   └── Blazix.BaseUI.Demo/         # Demo application (Server + Client)
└── tests/
    ├── Blazix.BaseUI.Tests/        # Unit tests (bUnit)
    ├── Blazix.BaseUI.Tests.Contracts/  # Test interface contracts
    └── Blazix.BaseUI.Playwright.Tests/ # E2E tests (Playwright)
```

### Tool Preferences

- Use relevant Codex skills when their trigger applies.
- Use `rg` for repository file and text search unless a relevant skill provides a better workflow.
- For current library, framework, SDK, API, CLI, or cloud-service documentation, use the Context7 CLI workflow (`npx ctx7@latest library ...`, then `npx ctx7@latest docs ...`).
- Use `/dev/null` in Git Bash, not `nul`
- Put all `tmpclaude-*-cwd` files in `/.claude/tmp-files`
- **`gh api` on Windows**: Omit the leading `/` from endpoint paths. Windows shells rewrite `/repos/...` as filesystem paths (e.g., `F:/Git/repos/...`). Use `gh api repos/owner/repo/...` instead of `gh api /repos/owner/repo/...`

### Documentation and Audit Artifact Placement

- Project documentation belongs in `docs/` and may be committed when it is durable source material for the repository, including documentation-site content, design guidance, plans, and component documentation.
- Place durable audit reports in `docs/audits/` using the component name, for example `docs/audits/toolbar-functional-audit.md`. These files may be committed when they are intended as reviewable audit evidence or source documentation.
- Store durable parity matrices in `docs/audits/` using the component name, for example `docs/audits/toolbar-parity-matrix.md`. These files may be committed when they describe component behavior or source parity.
- Source documentation comparison reports belong in `docs/audits/`, for example `docs/audits/toggle-toggle-group-source-docs-comparison.md`, and may be committed when they are part of the audit record.
- Raw verification evidence such as `.txt` transcripts, `.log` files, JSON browser summaries, screenshots, traces, and other generated run logs may live under `docs/audits/logs/` with the component name as a prefix for local review only. Do not commit these files. Commit only the durable Markdown audit artifacts in `docs/audits/`, such as functional audits, parity matrices, source comparison reports, and verification reports.
- Before committing documentation or audit artifacts, review them for sensitive information. Do not commit secrets, tokens, credentials, absolute local paths, user names, personal emails, machine-specific paths, or other personal information.
- Temporary/generated run output belongs in `output/` only while work is in progress and should not be committed unless explicitly requested.
- Framework-agnostic behavioral specs belong in `../base-ui-specs/<component>/`. Keep those specs updated when an audit discovers reusable behavior or pitfalls, while repository-specific documentation and audits may live in `docs/`.
- Do not commit `.DS_Store` files or other local filesystem metadata.

---

## Build and Test Commands

### Build

```bash
dotnet build Blazix.BaseUI.slnx                    # Build entire solution
dotnet build src/Blazix.BaseUI/Blazix.BaseUI.csproj # Build specific project
dotnet build Blazix.BaseUI.slnx -c Release         # Build in release mode
```

### Run Demo

```bash
dotnet run --project demo/Blazix.BaseUI.Demo/Blazix.BaseUI.Demo/Blazix.BaseUI.Demo.csproj
```

### Unit Tests (bUnit)

```bash
dotnet test tests/Blazix.BaseUI.Tests/Blazix.BaseUI.Tests.csproj
dotnet test --filter "FullyQualifiedName~CheckboxRootTests"  # Specific class
dotnet test --filter "TestName"                               # Specific test
dotnet test -v detailed                                       # Verbose output
```

### E2E Tests (Playwright)

```bash
dotnet test tests/Blazix.BaseUI.Playwright.Tests/Blazix.BaseUI.Playwright.Tests/Blazix.BaseUI.Playwright.Tests.csproj
dotnet test --filter "FullyQualifiedName~CollapsibleServerTests"  # Specific class
```

See [Testing Instructions](.claude/rules/testing-instructions.md) for debugging, traces, and advanced configuration.

### Lint Rules

```bash
bash scripts/lint-rules.sh              # Run all lint rules
bash scripts/lint-rules.sh --rule 5     # Run specific rule only
```

Enforces coding standards from this document. Suppress a violation with `// lint-ignore:RULE-NN` on the same or preceding line.

---

## Code Style Guidelines

### 1. Pre-Generation Validation Rule (Mandatory)

1. **Consult the framework-agnostic spec** at `../base-ui-specs/<component>/SPEC.md` first — it captures the component's full behavioral surface and pre-flagged Blazor pitfalls (`pitfalls.md`). If the spec is missing for the target component, fall back to the React source.
2. Check relevant files in `/.base-ui`
3. List all relevant components found
4. Create an implementation plan (Outline files, structure, and approach)
5. When you discover a behavior the spec doesn't capture, **update the spec** in `../base-ui-specs/<component>/` in the same change — scar tissue belongs in the spec, not only in the Blazor port.

### 2. Code Ordering (Strict)

Generate members **only** in the exact order below.

1. Constants (PascalCase)
2. Fields (no underscore prefix: read-only, private, backing)
3. Private properties
4. Parameter properties
5. Public properties
6. Internal properties
7. Lifecycle methods
8. Dispose method (only if needed)
9. Public methods
10. Internal methods
11. Private methods

No reordering is allowed.

### 3. Fidelity to Source

- Replicate **structure and behavior exactly**
- **Do not add business logic**
- Do not simplify or "improve" behavior unless explicitly requested
- Do not create Class or Style parameter property unless needed (like passing context/state). Use the `Func<TState, string>?` types for these, and name these as `ClassValue` and `StyleValue` accordingly.
- Use the shared `RenderElement<TState>` component to simulate `useRender`. Components expose a `Render` parameter of type `RenderFragment<RenderProps<TState>>?` and pass it to `<RenderElement>` along with a `Tag` (default HTML element), `State`, `ClassValue`, `StyleValue`, `ComponentAttributes`, and `ChildContent`.

#### Attribute Handling

- Follow attribute ordering from the source as closely as possible
- aria attributes with boolean values should be converted to string `isTrue ? "true" : "false"`
- Check `./AttributeUtilities.cs` for attribute helpers

### 4. JavaScript Interop Rules

See [JS Interop Rules](.claude/rules/js-interop-rules.md) for imports, script states, exception handling, and responsibility split.

### 4a. JS Interop File Architecture

- Every component requiring JS interop **must** have its own component-specific JS file (e.g., `blazix-baseui-dialog.js`)
- Component JS files import shared behavior from functional JS modules (e.g., `blazix-baseui-floating.js`, `blazix-baseui-composite.js`)
- Shared/functional JS modules must **never** contain component-specific logic
- **Exception:** When the interaction is trivial (a single function call with no component-specific wiring), the C# component may import the shared JS module directly
- When adding new behavior to a component, modify the component's JS file — not the shared module

### 5. Logging

- Use `ILogger`
- If recommending alternatives, explain **before** code generation

### 6. Element Reference Capture

See [Element Reference Capture](.claude/rules/element-reference-capture.md) for the `@ref` / `RenderElement` pattern.

### 7. Cascading Parameters Rules

- Never cascade the parent component directly

### 8. Code Placement and Rendering Rules

- Code-behind files (`.razor.cs`) must contain only the namespace, XML doc comment, and `public partial class ComponentName;` declaration — no logic
- All components use `.razor` files with `<RenderElement>` in Razor markup and logic inside `@code { }`
- The `.cs` stub file contains only the namespace, XML doc comment, and `public partial class ComponentName;` declaration

### 9. RenderTreeBuilder Sequencing (Strict)

See `.blazor-docs/advanced-scenarios.md` for detailed guidance on sequence numbers.

Key rules:
- Use **hardcoded integer literals** for sequence numbers (no variables or expressions)
- Sequence numbers must increase in source code order, not runtime order
- For complex `BuildRenderTree`, use `OpenRegion`/`CloseRegion` for isolated sequence spaces

### 10. Component files guide

1. If there are enums specific to the component, create an `Enumerations.cs`
2. Interface and implementation in a single file
3. Extension method should be in `./Extensions.cs`, if component-scoped only, create its own.

### 11. Async Lifecycle and Exception Handling

See [Async Lifecycle](.claude/rules/async-lifecycle.md) for non-blocking patterns, thread-blocking methods, re-entrancy, and fire-and-forget exception handling.

### 12. Dispose Method Implementation

See [Dispose Implementation](.claude/rules/dispose-implementation.md) for GC.SuppressFinalize rules and CancellationToken patterns.

### 13. ElementReference and Module Guard Checks

See [Element & Module Guards](.claude/rules/element-module-guards.md) for ElementReference guards and lazy module dispose patterns.

### 14. Event Handler Override Prevention

See [Event Handler Override Prevention](.claude/rules/event-handler-override.md) for EventUtilities patterns and when to apply them.

---

## Commit and PR Style

- Do NOT commit until I say that I have personally tested your changes
- Do NOT add "Generated with Claude Code" or co-author footers to commits or PRs
- Keep commit messages concise and descriptive
- PR descriptions should focus on what changed and why
- Do NOT mark PRs as "ready for review" (`gh pr ready`) - leave PRs in draft mode and let the user decide when to mark them ready

### PR Review Comment Workflow

See [PR Review Workflow](.claude/rules/pr-review-workflow.md) for the full 7-step process.

---

## Testing Instructions

See [Testing Instructions](.claude/rules/testing-instructions.md) for full details on test configuration, unit test structure, contracts, JS interop setup, Playwright tests, debugging, traces, codegen, cross-browser testing, environment variables, and assertions.
