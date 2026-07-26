# Focused regression-test status

## Progress

- [x] Research completed.
- [x] Test plan completed.
- [x] Analyzer tests implemented.
- [x] Menu assertion strengthened.
- [x] Focused analyzer tests passed.
- [x] Focused menu tests passed.
- [x] Assertion-quality and test-gap reviews completed.
- [x] Scope and diff checks completed.

## Scope guard

Only `.testagent/` Markdown artifacts and the two requested test files may be
changed by this task. Production files, JavaScript fixtures, and browser fixtures
remain out of scope.

## Validation

- `dotnet test tests/Blazix.BaseUI.Analyzers.Tests/Blazix.BaseUI.Analyzers.Tests.csproj --filter "FullyQualifiedName~ConventionAnalyzerTests" --no-restore -v minimal`
  - Passed: 14, failed: 0, skipped: 0.
- `dotnet test tests/Blazix.BaseUI.Tests/Blazix.BaseUI.Tests.csproj --filter "FullyQualifiedName~Blazix.BaseUI.Tests.Menu.MenuRootTests." --no-build --no-restore -v minimal`
  - Passed: 21, failed: 0, skipped: 0.
- The initial menu test invocation built the full dependency graph and also passed
  repository textual lint plus validation of all 37 JavaScript source/minified pairs.
- `git diff --check`
  - Passed with no whitespace errors.

## Assertion-quality review

- The two positive analyzer tests each contain a meaningful diagnostic-presence
  assertion. Only the outer object parameter can produce `BBUI0019`, so the assertions
  specifically exercise symbol identity rather than merely analyzer execution.
- The two exemption tests contain meaningful negative collection assertions proving
  that no `BBUI0019` diagnostic is emitted.
- The strengthened menu checkpoint combines one Boolean state assertion with three
  state/equality assertions across the cascade and imperative handle.
- No generated test is assertion-free, trivial-only, self-referential, skipped, or
  dependent on timing, files, network, or environment state.

## Pseudo-mutation gap review

- Removing semantic symbol equality and reverting to identifier-text matching would
  be killed by `UtilityObjectParameter_IgnoresUnrelatedMemberWithSameIdentifier` and
  `UtilityObjectParameter_IgnoresShadowedLocalFunctionParameter`.
- Reverting dictionary detection to parameter syntax text would be killed by
  `UtilityObjectParameter_ExemptsAliasedAttributeDictionary` and
  `UtilityObjectParameter_ExemptsFullyQualifiedAttributeDictionary`. The qualified
  case also fully qualifies `System.String` and `System.Object`, avoiding accidental
  coverage by the old `Dictionary<string, object>` substring.
- Omitting any initial handle synchronization (`IsOpen`, active trigger ID, or payload)
  would be killed by the first controlled-open checkpoint in
  `ControlledTriggerLifecyclePreservesAssociationAndPayload`.
- No behavior gap remains within the requested scope. The optional identical-payload
  tooltip browser case was intentionally not added because the existing fixture has no
  same-payload-then-mutation seam; creating one would expand this test-only task.
