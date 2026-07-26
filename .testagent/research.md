# Focused regression-test research

## Bounded target inventory

- `src/Blazix.BaseUI.Analyzers/AvoidableObjectBoxingAnalyzer.cs`
  - The analyzer now resolves each candidate parameter to an `IParameterSymbol`.
  - Identifier uses are filtered with `SemanticModel.GetSymbolInfo` and `SymbolEqualityComparer.Default`.
  - Attribute-dictionary exemptions compare the semantic `INamedTypeSymbol` against
    `System.Collections.Generic.Dictionary<TKey, TValue>`.
  - Static source-to-test pairing identifies
    `tests/Blazix.BaseUI.Analyzers.Tests/ConventionAnalyzerTests.cs` as the existing covering test.
- `tests/Blazix.BaseUI.Tests/Menu/MenuRootTests.cs`
  - `ControlledTriggerLifecyclePreservesAssociationAndPayload` already exercises the
    controlled closed-to-open lifecycle through `MenuHandle<string>`.
  - Its first controlled-open assertion checks only the cascaded payload, while later
    assertions verify the handle's complete state.

## Existing conventions

- Both projects use xUnit v3 with Shouldly assertions.
- Analyzer fixtures are compiled in-memory by `AnalyzeSourceAsync`.
- Analyzer assertions select diagnostics by the analyzer diagnostic ID.
- Component tests use bUnit `WaitForAssertion` when render-time registration affects
  cascaded or handle state.
- The repository uses .NET SDK 10.0.301. There is no `global.json` MTP runner override,
  and both test projects reference `Microsoft.NET.Test.Sdk` plus xUnit v3, so focused
  validation uses VSTest `--filter` syntax.

## Static pairing note

The repository is polyglot, but the requested scope is C#/.NET only. The polyglot
pairing tool could not run because `tree-sitter-language-pack` is not installed.
The Roslyn pairing engine completed instead: 867 source files, 313 test files,
281 paired source files, and 586 statically unpaired source files. This is
identifier/reference pairing, not line or branch coverage.

## Acceptance checklist

- [x] An unrelated member access whose identifier matches the object parameter does
  not prevent the cast-only parameter diagnostic.
- [x] A local-function parameter that shadows the method parameter does not prevent
  the cast-only method parameter diagnostic.
- [x] An alias for `Dictionary<string, object>` receives the attribute-dictionary
  exemption.
- [x] A fully qualified `Dictionary<string, object>` receives the
  attribute-dictionary exemption.
- [x] The first controlled-open menu assertion verifies cascaded payload plus
  `MenuHandle.IsOpen`, `MenuHandle.ActiveTriggerId`, and `MenuHandle.Payload`.
- [x] No production source is edited.
- [x] The optional identical-payload tooltip browser test was skipped because the
  existing fixture has no observable same-payload-then-mutation seam; adding one
  would require a new fixture.
