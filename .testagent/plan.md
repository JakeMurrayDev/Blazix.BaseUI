# Focused regression-test plan

## Phase 1: Analyzer symbol-resolution regressions

- Add `UtilityObjectParameter_IgnoresUnrelatedMemberWithSameIdentifier`.
  - Include one cast-only object parameter use and an unrelated `holder.value`
    member access.
  - Assert `BBUI0019` remains present.
- Add `UtilityObjectParameter_IgnoresShadowedLocalFunctionParameter`.
  - Include a static local function whose parameter has the same identifier as the
    analyzed method parameter.
  - Assert `BBUI0019` remains present for the outer parameter.

## Phase 2: Analyzer semantic dictionary regressions

- Add `UtilityObjectParameter_ExemptsAliasedAttributeDictionary`.
  - Declare a type alias for `Dictionary<string, object>`.
  - Assert `BBUI0019` is absent.
- Add `UtilityObjectParameter_ExemptsFullyQualifiedAttributeDictionary`.
  - Use `global::System.Collections.Generic.Dictionary<string, object>`.
  - Assert `BBUI0019` is absent.

## Phase 3: Controlled-menu state assertion

- Strengthen the first controlled-open assertion in
  `ControlledTriggerLifecyclePreservesAssociationAndPayload`.
  - Assert the cascaded payload is `"one"`.
  - Assert `IsOpen` is `true`, `ActiveTriggerId` is `"trigger-1"`, and `Payload`
    is `"one"`.

## Phase 4: Focused validation and quality review

- Build and run only `ConventionAnalyzerTests`.
- Build and run only `MenuRootTests`.
- Re-open the modified test blocks and perform assertion-quality and
  mutation-oriented gap reviews.
- Run formatting/diff checks appropriate to the edited C# and Markdown files.
- Skip the optional identical-payload tooltip browser test: the current fixture does
  not expose a same-payload-then-mutation seam, so adding it would expand the bounded
  task beyond a minimal test-only change.
