using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Diff;
using Blazix.BaseUI.Parity.Tests.Infrastructure;
using Shouldly;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

/// <summary>Pins conservative, six-field retry correlation.</summary>
public sealed class RetryClassifierTests
{
    private static readonly RetryScope ServerScope =
        new("switch/hero@light", ParityLeg.BlazorServer, "toggle-on");

    [Fact]
    public void SameIdentityIsStableWhenMessageAndValuesChange()
    {
        var first = Finding(reference: "true", candidate: "false", message: "first presentation");
        var retry = Finding(reference: "mixed", candidate: "true", message: "retry presentation");

        var verdict = RetryClassifier.Classify(Attempt(first), Attempt(retry));

        var evidence = verdict.Evidence.ShouldHaveSingleItem();
        evidence.Classification.ShouldBe(RetryFindingClassification.Stable);
        evidence.FirstAttempt.ShouldBe(first);
        evidence.RetryAttempt.ShouldBe(retry);
        evidence.Effective.ShouldBe(retry);
        evidence.Effective.Severity.ShouldBe(Severity.Error);
        verdict.HasBlockingEvidence.ShouldBeTrue();
    }

    [Fact]
    public void FirstAttemptErrorAndCleanRetryBecomeFlakyInSameCompleteScope()
    {
        var first = Finding();

        var verdict = RetryClassifier.Classify(Attempt(first), Attempt());

        var evidence = verdict.Evidence.ShouldHaveSingleItem();
        evidence.Classification.ShouldBe(RetryFindingClassification.Flaky);
        evidence.FirstAttempt.ShouldBe(first);
        evidence.RetryAttempt.ShouldBeNull();
        evidence.Effective.Severity.ShouldBe(Severity.Flaky);
        evidence.Effective.Message.ShouldBe(first.Message);
        verdict.HasBlockingEvidence.ShouldBeFalse();
    }

    [Fact]
    public void RetryOnlyErrorBecomesFlakyWhenFirstAttemptWasCleanInTheSameScope()
    {
        var retry = Finding();

        var verdict = RetryClassifier.Classify(Attempt(), Attempt(retry));

        var evidence = verdict.Evidence.ShouldHaveSingleItem();
        evidence.Classification.ShouldBe(RetryFindingClassification.Flaky);
        evidence.FirstAttempt.ShouldBeNull();
        evidence.RetryAttempt.ShouldBe(retry);
        evidence.Effective.Severity.ShouldBe(Severity.Flaky);
        verdict.HasBlockingEvidence.ShouldBeFalse();
    }

    [Fact]
    public void DifferentErrorIdentitiesInTheSameScopeRemainBlockingInFirstSeenOrder()
    {
        var first = Finding(property: "aria-checked");
        var retry = Finding(property: "role");

        var verdict = RetryClassifier.Classify(Attempt(first), Attempt(retry));

        verdict.Findings.Select(item => item.Property).ShouldBe(["aria-checked", "role"]);
        verdict.Findings.ShouldAllBe(item => item.Severity == Severity.Error);
        verdict.Evidence.Select(item => item.Classification)
            .ShouldBe([
                RetryFindingClassification.IdentityChanged,
                RetryFindingClassification.IdentityChanged
            ]);
        verdict.HasBlockingEvidence.ShouldBeTrue();
    }

    [Fact]
    public void ErrorsInIndependentCleanScopesMayEachBecomeFlaky()
    {
        var wasmScope = new RetryScope("switch/hero@light", ParityLeg.BlazorWasm, "toggle-on");
        var first = Finding();
        var retry = Finding(leg: ParityLeg.BlazorWasm, property: "role");
        var firstAttempt = new RetryAttempt
        {
            Scopes = [ServerScope, wasmScope],
            Findings = [first]
        };
        var retryAttempt = new RetryAttempt
        {
            Scopes = [ServerScope, wasmScope],
            Findings = [retry]
        };

        var verdict = RetryClassifier.Classify(firstAttempt, retryAttempt);

        verdict.Findings.ShouldAllBe(item => item.Severity == Severity.Flaky);
        verdict.HasBlockingEvidence.ShouldBeFalse();
    }

    [Fact]
    public void StableStructureDoesNotDemoteWhenPresentationLabelsVary()
    {
        var first = Finding(
            kind: FindingKind.Structure,
            property: "identity",
            reference: "span[role=switch]",
            candidate: "button[role=switch]",
            message: "old labels");
        var retry = first with
        {
            ReferenceValue = "span[data-key=hero]",
            CandidateValue = "button[data-key=hero]",
            Message = "new labels"
        };

        var evidence = RetryClassifier.Classify(Attempt(first), Attempt(retry))
            .Evidence.ShouldHaveSingleItem();

        evidence.Classification.ShouldBe(RetryFindingClassification.Stable);
        evidence.Effective.Severity.ShouldBe(Severity.Error);
    }

    [Fact]
    public void StableTimelineL1DoesNotDemoteWhenSequenceTextVaries()
    {
        var first = Finding(
            kind: FindingKind.Timeline,
            nodePath: string.Empty,
            property: string.Empty,
            reference: "transitionstart",
            candidate: "transitionend",
            message: "first diff rendering");
        var retry = first with
        {
            ReferenceValue = "transitionstart\ntransitionend",
            CandidateValue = "transitioncancel",
            Message = "retry diff rendering"
        };

        var evidence = RetryClassifier.Classify(Attempt(first), Attempt(retry))
            .Evidence.ShouldHaveSingleItem();

        evidence.Classification.ShouldBe(RetryFindingClassification.Stable);
        evidence.Effective.Severity.ShouldBe(Severity.Error);
    }

    [Theory]
    [InlineData("error: request id=111", "error: request id=222")]
    [InlineData("error: circuit f05f3b4a-a5de-4c38-96f3-3877e3694f36", "error: circuit 884f6de9-c91c-44ab-bf00-a14933617e73")]
    [InlineData("error: /api?token=abc", "error: /api?token=def")]
    [InlineData("error: /src/App.jsx?t=111", "error: /src/App.jsx?t=222")]
    public void ChangedVolatileConsolePropertyIsIdentityDriftNotFlaky(
        string firstProperty,
        string retryProperty)
    {
        var first = Finding(
            kind: FindingKind.Console,
            nodePath: string.Empty,
            property: firstProperty);
        var retry = Finding(
            kind: FindingKind.Console,
            nodePath: string.Empty,
            property: retryProperty);

        var verdict = RetryClassifier.Classify(Attempt(first), Attempt(retry));

        verdict.Findings.ShouldAllBe(item => item.Severity == Severity.Error);
        verdict.Evidence.ShouldAllBe(item =>
            item.Classification == RetryFindingClassification.IdentityChanged);
        verdict.HasBlockingEvidence.ShouldBeTrue();
    }

    [Theory]
    [InlineData(RetryAttemptState.IncompleteCapture, "candidate step missing")]
    [InlineData(RetryAttemptState.ExecutionFailure, "browser disconnected")]
    public void IncompleteOrFailedRetryNeverDemotesAnError(
        RetryAttemptState state,
        string failure)
    {
        var first = Finding();
        var retry = new RetryAttempt
        {
            Scopes = [ServerScope],
            State = state,
            Failure = failure
        };

        var verdict = RetryClassifier.Classify(Attempt(first), retry);

        var evidence = verdict.Evidence.ShouldHaveSingleItem();
        evidence.Classification.ShouldBe(RetryFindingClassification.ExecutionFailure);
        evidence.Effective.Severity.ShouldBe(Severity.Error);
        var retryFailure = verdict.Failures.ShouldHaveSingleItem();
        retryFailure.Attempt.ShouldBe(2);
        retryFailure.State.ShouldBe(state);
        retryFailure.Message.ShouldBe(failure);
        verdict.HasBlockingEvidence.ShouldBeTrue();
    }

    [Fact]
    public void MissingCorrespondingScopeNeverCountsAsACleanRetry()
    {
        var first = Finding();
        var wasmScope = new RetryScope("switch/hero@light", ParityLeg.BlazorWasm, "toggle-on");
        var retry = new RetryAttempt { Scopes = [wasmScope] };

        var verdict = RetryClassifier.Classify(Attempt(first), retry);

        var evidence = verdict.Evidence.ShouldHaveSingleItem();
        evidence.Classification.ShouldBe(RetryFindingClassification.ExecutionFailure);
        evidence.Effective.Severity.ShouldBe(Severity.Error);
        verdict.Failures.Select(item => item.Attempt).ShouldBe([2, 1]);
        verdict.HasBlockingEvidence.ShouldBeTrue();
    }

    [Fact]
    public void MissingScopeBlocksEvenWhenNeitherAttemptEmittedAComparatorFinding()
    {
        var wasmScope = new RetryScope("switch/hero@light", ParityLeg.BlazorWasm, "toggle-on");
        var first = new RetryAttempt { Scopes = [ServerScope] };
        var retry = new RetryAttempt { Scopes = [wasmScope] };

        var verdict = RetryClassifier.Classify(first, retry);

        verdict.Findings.ShouldBeEmpty();
        verdict.Failures.Select(item => item.Attempt).ShouldBe([2, 1]);
        verdict.Failures.ShouldAllBe(item => item.State == RetryAttemptState.IncompleteCapture);
        verdict.HasBlockingEvidence.ShouldBeTrue();
    }

    [Fact]
    public void ExecutionFailureBlocksEvenWithoutAComparatorFinding()
    {
        var retry = new RetryAttempt
        {
            Scopes = [ServerScope],
            State = RetryAttemptState.ExecutionFailure,
            Failure = "browser disconnected"
        };

        var verdict = RetryClassifier.Classify(Attempt(), retry);

        verdict.Findings.ShouldBeEmpty();
        verdict.Failures.ShouldHaveSingleItem().Message.ShouldBe("browser disconnected");
        verdict.HasBlockingEvidence.ShouldBeTrue();
    }

    [Theory]
    [InlineData(FindingKind.CorrespondenceUncertain)]
    [InlineData(FindingKind.ActionCompletionUnmet)]
    [InlineData(FindingKind.FixtureError)]
    public void NonWaivableEvidenceNeverDemotesOnACleanRetry(FindingKind kind)
    {
        var first = Finding(kind: kind, property: "identity");

        var verdict = RetryClassifier.Classify(Attempt(first), Attempt());

        var evidence = verdict.Evidence.ShouldHaveSingleItem();
        evidence.Classification.ShouldBe(RetryFindingClassification.NonWaivable);
        evidence.Effective.Severity.ShouldBe(Severity.Error);
        verdict.HasBlockingEvidence.ShouldBeTrue();
    }

    [Fact]
    public void RetryInfoCannotHideAFirstAttemptNonWaivableError()
    {
        var first = Finding(
            kind: FindingKind.CorrespondenceUncertain,
            property: "identity");
        var retry = first with
        {
            Severity = Severity.Info,
            CandidateValue = "retry presentation",
            Message = "retry informational presentation"
        };

        var verdict = RetryClassifier.Classify(Attempt(first), Attempt(retry));

        var evidence = verdict.Evidence.ShouldHaveSingleItem();
        evidence.Classification.ShouldBe(RetryFindingClassification.NonWaivable);
        evidence.Effective.Severity.ShouldBe(Severity.Error);
        evidence.Effective.ShouldBe(first);
        verdict.HasBlockingEvidence.ShouldBeTrue();
    }

    [Fact]
    public void DuplicateIdentityIsAnExecutionFailureAndNeverFlaky()
    {
        var first = Finding();
        var duplicate = first with
        {
            CandidateValue = "mixed",
            Message = "same identity, different presentation"
        };

        var verdict = RetryClassifier.Classify(Attempt(first, duplicate), Attempt());

        var evidence = verdict.Evidence.ShouldHaveSingleItem();
        evidence.Classification.ShouldBe(RetryFindingClassification.ExecutionFailure);
        evidence.Effective.Severity.ShouldBe(Severity.Error);
        var failure = verdict.Failures.ShouldHaveSingleItem();
        failure.Attempt.ShouldBe(1);
        failure.Message.ShouldContain("occurs more than once");
    }

    [Fact]
    public void DuplicateIdentityOnRetryIsAttributedToTheRetryAttempt()
    {
        var retry = Finding();

        var verdict = RetryClassifier.Classify(Attempt(), Attempt(retry, retry));

        verdict.Failures.ShouldHaveSingleItem().Attempt.ShouldBe(2);
        verdict.Findings.ShouldHaveSingleItem().Severity.ShouldBe(Severity.Error);
    }

    [Fact]
    public void InformationalEvidenceNeverBecomesBlockingOrRequiresRetry()
    {
        var info = Finding() with { Severity = Severity.Info };
        var flaky = Finding(property: "role") with { Severity = Severity.Flaky };

        var verdict = RetryClassifier.Classify(Attempt(info, flaky), Attempt());

        verdict.Evidence.ShouldAllBe(item =>
            item.Classification == RetryFindingClassification.Informational);
        verdict.Findings.Select(item => item.Severity).ShouldBe([Severity.Info, Severity.Flaky]);
        verdict.HasBlockingEvidence.ShouldBeFalse();
    }

    [Fact]
    public void AttemptsRejectContradictoryStateAndUndeclaredFindingScopes()
    {
        var completeWithFailure = new RetryAttempt
        {
            Scopes = [ServerScope],
            Failure = "impossible"
        };
        var incompleteWithoutFailure = new RetryAttempt
        {
            Scopes = [ServerScope],
            State = RetryAttemptState.IncompleteCapture
        };
        var wrongScope = new RetryAttempt
        {
            Scopes = [new RetryScope("switch/hero@light", ParityLeg.BlazorWasm, "toggle-on")],
            Findings = [Finding()]
        };

        Should.Throw<ArgumentException>(() =>
                RetryClassifier.Classify(completeWithFailure, Attempt()))
            .Message.ShouldContain("complete but contains failure detail");
        Should.Throw<ArgumentException>(() =>
                RetryClassifier.Classify(incompleteWithoutFailure, Attempt()))
            .Message.ShouldContain("has no failure detail");
        Should.Throw<ArgumentException>(() => RetryClassifier.Classify(wrongScope, Attempt()))
            .Message.ShouldContain("outside its declared scopes");
    }

    [Fact]
    public void AttemptRejectsDuplicateScopes()
    {
        var attempt = new RetryAttempt { Scopes = [ServerScope, ServerScope] };

        Should.Throw<ArgumentException>(() => RetryClassifier.Classify(attempt, Attempt()))
            .Message.ShouldContain("duplicate scopes");
    }

    [Theory]
    [InlineData("switch/hero")]
    [InlineData("switch/hero@blue")]
    [InlineData("switch/hero@light@dark")]
    [InlineData("switch/hero@Dark")]
    [InlineData("Switch/hero@light")]
    [InlineData("switch/hero-@light")]
    public void AttemptRejectsMissingUnknownDuplicateOrCaseVariantThemeSuffix(string fixture)
    {
        var attempt = new RetryAttempt
        {
            Scopes = [new RetryScope(fixture, ParityLeg.BlazorServer, "toggle-on")]
        };

        var exception = Should.Throw<ArgumentException>(() =>
            RetryClassifier.Classify(attempt, attempt));

        exception.Message.ShouldContain("fixture-theme identity");
    }

    [Fact]
    public void AttemptRejectsAnInvalidFindingIdentityEvenWhenItsDeclaredScopeIsValid()
    {
        var invalidFinding = Finding() with { Fixture = "switch/hero" };
        var attempt = new RetryAttempt
        {
            Scopes = [ServerScope],
            Findings = [invalidFinding]
        };

        var exception = Should.Throw<ArgumentException>(() =>
            RetryClassifier.Classify(attempt, Attempt()));

        exception.Message.ShouldContain("fixture-theme identity");
    }

    [Fact]
    public void IdenticalFindingFieldsAcrossThemesNeverCorrelateOrDemote()
    {
        var light = Finding();
        var dark = light with { Fixture = "switch/hero@dark" };
        var darkScope = new RetryScope(
            "switch/hero@dark", ParityLeg.BlazorServer, "toggle-on");

        var verdict = RetryClassifier.Classify(
            Attempt(light),
            new RetryAttempt { Scopes = [darkScope], Findings = [dark] });

        verdict.Evidence.ShouldAllBe(item =>
            item.Classification == RetryFindingClassification.ExecutionFailure);
        verdict.Findings.ShouldAllBe(item => item.Severity == Severity.Error);
        verdict.Failures.ShouldNotBeEmpty();
        verdict.HasBlockingEvidence.ShouldBeTrue();
    }

    private static RetryAttempt Attempt(params Finding[] findings) => new()
    {
        Scopes = [ServerScope],
        Findings = findings
    };

    private static Finding Finding(
        ParityLeg leg = ParityLeg.BlazorServer,
        FindingKind kind = FindingKind.Attribute,
        string nodePath = "root > button[role=switch]",
        string property = "aria-checked",
        string? reference = "true",
        string? candidate = "false",
        string message = "presentation") => new()
        {
            Fixture = "switch/hero@light",
            Leg = leg,
            Step = "toggle-on",
            Kind = kind,
            Severity = Severity.Error,
            NodePath = nodePath,
            Property = property,
            ReferenceValue = reference,
            CandidateValue = candidate,
            Message = message
        };
}
