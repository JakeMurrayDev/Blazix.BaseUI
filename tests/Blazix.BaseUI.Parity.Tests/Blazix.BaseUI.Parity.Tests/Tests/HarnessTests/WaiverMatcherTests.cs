using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Diff;
using Blazix.BaseUI.Parity.Tests.Waivers;
using Shouldly;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

/// <summary>Pins strict loading and one-to-one waiver consumption.</summary>
public sealed class WaiverMatcherTests
{
    private static readonly DateOnly ReviewDate = new(2026, 8, 9);
    private static readonly DateOnly FutureDate = new(2026, 9, 1);

    [Fact]
    public void LoadsAnExactAcceptedLimitationAndDefaultsPropertyMatch()
    {
        var waiver = Load(ValidJson()).ShouldHaveSingleItem();

        waiver.Identity.ShouldBe(Identity());
        waiver.PropertyMatch.ShouldBe(WaiverPropertyMatch.Exact);
        waiver.Disposition.ShouldBe(WaiverDisposition.AcceptedLimitation);
        waiver.DocLink.ShouldBe("docs/audits/switch-functional-audit.md");
        waiver.Expires.ShouldBe(FutureDate);
    }

    [Fact]
    public void EmptyRegistryLoadsInDeterministicOrder()
    {
        Load("[]").ShouldBeEmpty();

        var json = $"[{ValidEntry(property: "aria-checked")},{ValidEntry(property: "role")}]";
        Load(json).Select(item => item.Property).ShouldBe(["aria-checked", "role"]);
    }

    [Theory]
    [InlineData("fixture")]
    [InlineData("leg")]
    [InlineData("step")]
    [InlineData("nodePath")]
    [InlineData("kind")]
    [InlineData("property")]
    public void EveryIdentityFieldMustMatchExactly(string changedField)
    {
        var finding = Finding();
        var waiver = changedField switch
        {
            "fixture" => Waiver() with { Fixture = "switch/other@light" },
            "leg" => Waiver() with { Leg = ParityLeg.BlazorWasm },
            "step" => Waiver() with { Step = "toggle-off" },
            "nodePath" => Waiver() with { NodePath = "root > button[role=switch] > span" },
            "kind" => Waiver() with { Kind = FindingKind.ComputedStyle },
            "property" => Waiver() with { Property = "role" },
            _ => throw new ArgumentOutOfRangeException(nameof(changedField))
        };

        var verdict = WaiverMatcher.Match([finding], [waiver], ReviewDate);

        verdict.Applied.ShouldBeEmpty();
        verdict.BlockingFindings.ShouldBe([finding]);
        verdict.Diagnostics.ShouldHaveSingleItem().Kind.ShouldBe(WaiverDiagnosticKind.Unused);
    }

    [Fact]
    public void PathsAndPropertiesMatchOrdinallyWithoutCaseFoldingOrCascading()
    {
        var child = Finding() with
        {
            NodePath = "root > button[role=switch] > span",
            Property = "Aria-Checked"
        };
        var waiver = Waiver() with
        {
            NodePath = "root > button[role=switch]",
            Property = "aria-checked"
        };

        var verdict = WaiverMatcher.Match([child], [waiver], ReviewDate);

        verdict.Applied.ShouldBeEmpty();
        verdict.BlockingFindings.ShouldBe([child]);
        verdict.Diagnostics.ShouldHaveSingleItem().Kind.ShouldBe(WaiverDiagnosticKind.Unused);
    }

    [Fact]
    public void OneExactWaiverConsumesOneErrorAndPreservesFindingOrder()
    {
        var first = Finding(property: "role");
        var second = Finding(property: "aria-checked");
        var info = Finding(property: "class") with { Severity = Severity.Info };

        var verdict = WaiverMatcher.Match([first, second, info], [Waiver()], ReviewDate);

        verdict.Findings.ShouldBe([first, second, info]);
        var applied = verdict.Applied.ShouldHaveSingleItem();
        applied.WaiverIndex.ShouldBe(0);
        applied.FindingIndex.ShouldBe(1);
        applied.Finding.ShouldBe(second);
        verdict.BlockingFindings.ShouldBe([first]);
        verdict.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void DuplicateFindingIdentityMakesAnExactWaiverAmbiguous()
    {
        var first = Finding(candidateValue: "true");
        var duplicate = Finding(candidateValue: "mixed") with { Message = "different presentation" };

        var verdict = WaiverMatcher.Match([first, duplicate], [Waiver()], ReviewDate);

        verdict.Applied.ShouldBeEmpty();
        verdict.BlockingFindings.ShouldBe([first, duplicate]);
        var diagnostic = verdict.Diagnostics.ShouldHaveSingleItem();
        diagnostic.Kind.ShouldBe(WaiverDiagnosticKind.Ambiguous);
        diagnostic.Message.ShouldContain("2 Error findings");
    }

    [Fact]
    public void OverlappingExactAndPrefixWaiversAreBothAmbiguous()
    {
        var finding = Finding(
            kind: FindingKind.Console,
            nodePath: string.Empty,
            property: "error: reconnect token=abc");
        var exact = Load(
            ValidJson(
                kind: "Console",
                nodePath: string.Empty,
                property: finding.Property,
                disposition: "deferred-defect",
                docLink: "https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/999999"),
            ValidIssueValidator()).ShouldHaveSingleItem();
        var prefix = PrefixWaiver("error: reconnect token=");

        var verdict = WaiverMatcher.Match([finding], [exact, prefix], ReviewDate);

        verdict.Applied.ShouldBeEmpty();
        verdict.BlockingFindings.ShouldBe([finding]);
        verdict.Diagnostics.Select(item => item.Kind)
            .ShouldBe([WaiverDiagnosticKind.Ambiguous, WaiverDiagnosticKind.Ambiguous]);
    }

    [Fact]
    public void ZeroMatchesAreUnusedAndExpiredEntriesBlockWithoutConsuming()
    {
        var finding = Finding();
        var unused = Waiver() with { Property = "role" };
        var expired = Waiver() with { Expires = ReviewDate };

        var verdict = WaiverMatcher.Match([finding], [unused, expired], ReviewDate);

        verdict.Applied.ShouldBeEmpty();
        verdict.BlockingFindings.ShouldBe([finding]);
        verdict.Diagnostics.Select(item => item.Kind)
            .ShouldBe([WaiverDiagnosticKind.Unused, WaiverDiagnosticKind.Expired]);
        verdict.HasBlockingEvidence.ShouldBeTrue();
    }

    [Fact]
    public void InfoAndFlakyFindingsNeverConsumeWaivers()
    {
        var info = Finding() with { Severity = Severity.Info };
        var flaky = Finding() with { Severity = Severity.Flaky };

        foreach (var finding in new[] { info, flaky })
        {
            var verdict = WaiverMatcher.Match([finding], [Waiver()], ReviewDate);
            verdict.Applied.ShouldBeEmpty();
            verdict.BlockingFindings.ShouldBeEmpty();
            verdict.Diagnostics.ShouldHaveSingleItem().Kind.ShouldBe(WaiverDiagnosticKind.Unused);
        }
    }

    [Theory]
    [InlineData(FindingKind.CorrespondenceUncertain)]
    [InlineData(FindingKind.ActionCompletionUnmet)]
    [InlineData(FindingKind.FixtureError)]
    public void NonWaivableKindsRemainBlockingAndRejectManualWaivers(FindingKind kind)
    {
        var finding = Finding(kind: kind, property: "identity");
        var waiver = Waiver() with { Kind = kind, Property = "identity" };

        var verdict = WaiverMatcher.Match([finding], [waiver], ReviewDate);

        verdict.Applied.ShouldBeEmpty();
        verdict.BlockingFindings.ShouldBe([finding]);
        verdict.NonWaivableFindings.ShouldBe([finding]);
        verdict.Diagnostics.ShouldHaveSingleItem().Kind.ShouldBe(WaiverDiagnosticKind.NonWaivable);
    }

    [Theory]
    [InlineData("error: request timestamp=", "2026-08-09T10:00:00Z", "2026-08-09T10:01:00Z", "timestamp")]
    [InlineData("error: circuit id=", "f05f3b4a-a5de-4c38-96f3-3877e3694f36", "884f6de9-c91c-44ab-bf00-a14933617e73", "GUID")]
    [InlineData("warning: request query-token=", "a1b2", "c3d4", "query token")]
    [InlineData("error: Vite cache token=", "111", "222", "Vite cache token")]
    public void NarrowConsolePrefixMatchesExactlyOneFindingAcrossTwoAttempts(
        string prefix,
        string firstSuffix,
        string retrySuffix,
        string suffixName)
    {
        var waiver = PrefixWaiver(prefix) with
        {
            Reason = $"Observed values differ per attempt. Volatile suffix: {suffixName}."
        };
        var first = Finding(
            kind: FindingKind.Console,
            nodePath: string.Empty,
            property: prefix + firstSuffix);
        var retry = Finding(
            kind: FindingKind.Console,
            nodePath: string.Empty,
            property: prefix + retrySuffix);

        var firstVerdict = WaiverMatcher.Match([first], [waiver], ReviewDate);
        var retryVerdict = WaiverMatcher.Match([retry], [waiver], ReviewDate);

        firstVerdict.Applied.ShouldHaveSingleItem().Finding.ShouldBe(first);
        retryVerdict.Applied.ShouldHaveSingleItem().Finding.ShouldBe(retry);
        firstVerdict.HasBlockingEvidence.ShouldBeFalse();
        retryVerdict.HasBlockingEvidence.ShouldBeFalse();
    }

    [Fact]
    public void ConsolePrefixMatchingMultipleFindingsIsAmbiguous()
    {
        var waiver = PrefixWaiver("error: reconnect token=");
        var first = Finding(
            kind: FindingKind.Console,
            nodePath: string.Empty,
            property: "error: reconnect token=one");
        var second = first with { Property = "error: reconnect token=two" };

        var verdict = WaiverMatcher.Match([first, second], [waiver], ReviewDate);

        verdict.Applied.ShouldBeEmpty();
        verdict.BlockingFindings.ShouldBe([first, second]);
        verdict.Diagnostics.ShouldHaveSingleItem().Kind.ShouldBe(WaiverDiagnosticKind.Ambiguous);
    }

    [Fact]
    public void MatcherRejectsAnInMemoryNonConsolePrefixThatBypassesTheLoader()
    {
        var finding = Finding();
        var waiver = Waiver() with
        {
            Property = "aria-",
            PropertyMatch = WaiverPropertyMatch.Prefix,
            Disposition = WaiverDisposition.DeferredDefect,
            DocLink = "https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/999999",
            Reason = "Volatile suffix: attribute name."
        };

        var verdict = WaiverMatcher.Match([finding], [waiver], ReviewDate);

        verdict.Applied.ShouldBeEmpty();
        verdict.BlockingFindings.ShouldBe([finding]);
        verdict.Diagnostics.ShouldHaveSingleItem().Kind.ShouldBe(WaiverDiagnosticKind.Invalid);
    }

    [Fact]
    public void MatcherDoesNotTrustADeferredWaiverWithoutOpaqueValidatorProvenance()
    {
        var finding = Finding(
            kind: FindingKind.Console,
            nodePath: string.Empty,
            property: "error: reconnect token=abc");
        var unverified = new Waiver
        {
            Fixture = "switch/hero@light",
            Leg = ParityLeg.BlazorServer,
            Step = "toggle-on",
            NodePath = string.Empty,
            Kind = FindingKind.Console,
            Property = "error: reconnect token=",
            PropertyMatch = WaiverPropertyMatch.Prefix,
            Reason = "Captured twice. Volatile suffix: token.",
            Disposition = WaiverDisposition.DeferredDefect,
            DocLink = "https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/999999",
            Expires = FutureDate
        };

        var statusProperty = typeof(Waiver).GetProperty("IssuePolicyStatus")!;
        var verdict = WaiverMatcher.Match([finding], [unverified], ReviewDate);

        statusProperty.SetMethod.ShouldBeNull();
        unverified.IssuePolicyStatus.ShouldBe(WaiverIssuePolicyStatus.Unverified);
        verdict.Applied.ShouldBeEmpty();
        verdict.BlockingFindings.ShouldBe([finding]);
        verdict.Diagnostics.ShouldHaveSingleItem().Kind
            .ShouldBe(WaiverDiagnosticKind.IssuePolicyUnverified);
    }

    [Fact]
    public void OfflineLoaderMarksDeferredConsolePrefixIssuePolicyUnverifiedAndBlocking()
    {
        var json = ValidJson(
            kind: "Console",
            nodePath: string.Empty,
            property: "error: reconnect token=",
            propertyMatch: "prefix",
            reason: "Captured twice. Volatile suffix: circuit GUID.",
            disposition: "deferred-defect",
            docLink: "https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/999999");

        var waiver = Load(json).ShouldHaveSingleItem();
        var finding = Finding(
            kind: FindingKind.Console,
            nodePath: string.Empty,
            property: "error: reconnect token=abc");
        var verdict = WaiverMatcher.Match([finding], [waiver], ReviewDate);

        waiver.PropertyMatch.ShouldBe(WaiverPropertyMatch.Prefix);
        waiver.Kind.ShouldBe(FindingKind.Console);
        waiver.IssuePolicyStatus.ShouldBe(WaiverIssuePolicyStatus.Unverified);
        verdict.Applied.ShouldBeEmpty();
        verdict.BlockingFindings.ShouldBe([finding]);
        verdict.Diagnostics.ShouldHaveSingleItem().Kind
            .ShouldBe(WaiverDiagnosticKind.IssuePolicyUnverified);
    }

    [Fact]
    public void InjectedIssueValidatorPermitsOnlyVerifiedDeferredConsolePrefix()
    {
        var validator = new StubIssueValidator(new WaiverIssuePolicyValidation(
            IsOpen: true,
            IsOwned: true,
            CapturedAttemptCount: 2,
            HasAcceptanceCriteria: true));
        var json = ValidJson(
            kind: "Console",
            nodePath: string.Empty,
            property: "error: reconnect token=",
            propertyMatch: "prefix",
            reason: "Captured twice. Volatile suffix: circuit GUID.",
            disposition: "deferred-defect",
            docLink: "https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/999999");
        var waiver = Load(json, validator).ShouldHaveSingleItem();
        var finding = Finding(
            kind: FindingKind.Console,
            nodePath: string.Empty,
            property: "error: reconnect token=abc");

        var verdict = WaiverMatcher.Match([finding], [waiver], ReviewDate);

        validator.Urls.ShouldBe([
            "https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/999999"
        ]);
        waiver.IssuePolicyStatus.ShouldBe(WaiverIssuePolicyStatus.Verified);
        verdict.Applied.ShouldHaveSingleItem().Finding.ShouldBe(finding);
        verdict.HasBlockingEvidence.ShouldBeFalse();
    }

    [Theory]
    [InlineData(false, true, 2, true, "open issue")]
    [InlineData(true, false, 2, true, "owned issue")]
    [InlineData(true, true, 1, true, "two captured attempts")]
    [InlineData(true, true, 2, false, "acceptance criteria")]
    public void InjectedIssueValidatorRejectsIncompleteLivePolicy(
        bool isOpen,
        bool isOwned,
        int capturedAttempts,
        bool hasAcceptanceCriteria,
        string expected)
    {
        var validator = new StubIssueValidator(new WaiverIssuePolicyValidation(
            isOpen,
            isOwned,
            capturedAttempts,
            hasAcceptanceCriteria));
        var json = ValidJson(
            disposition: "deferred-defect",
            docLink: "https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/999999");

        var exception = Should.Throw<FormatException>(() => Load(json, validator));

        exception.Message.ShouldContain("entry 0");
        exception.Message.ShouldContain("'docLink'");
        exception.Message.ShouldContain(expected);
    }

    [Fact]
    public void OfflineLoaderDoesNotInventAnUndefinedNumericShortExpiryCap()
    {
        var waiver = Load(ValidJson(
            kind: "Console",
            nodePath: string.Empty,
            property: "error: reconnect token=",
            propertyMatch: "prefix",
            reason: "Captured twice. Volatile suffix: circuit GUID.",
            disposition: "deferred-defect",
            docLink: "https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/999999",
            expires: "2027-08-09"))
            .ShouldHaveSingleItem();

        waiver.Expires.ShouldBe(new DateOnly(2027, 8, 9));
    }

    [Theory]
    [InlineData("Attribute", "aria-checked", "deferred-defect", "https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/999999", "Volatile suffix: token.", "propertyMatch")]
    [InlineData("Console", "error:", "deferred-defect", "https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/999999", "Volatile suffix: token.", "property")]
    [InlineData("Console", "warning:   ", "deferred-defect", "https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/999999", "Volatile suffix: token.", "property")]
    [InlineData("Console", "error: reconnect token=", "accepted-limitation", "docs/audits/switch-functional-audit.md", "Volatile suffix: token.", "disposition")]
    [InlineData("Console", "error: reconnect token=", "deferred-defect", "https://example.com/issues/139", "Volatile suffix: token.", "docLink")]
    [InlineData("Console", "error: reconnect token=", "deferred-defect", "https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/999999", "Token varies.", "reason")]
    public void StrictLoaderRejectsForbiddenPrefixShapes(
        string kind,
        string property,
        string disposition,
        string docLink,
        string reason,
        string expectedField)
    {
        var json = ValidJson(
            kind: kind,
            nodePath: kind == "Console" ? string.Empty : "root",
            property: property,
            propertyMatch: "prefix",
            reason: reason,
            disposition: disposition,
            docLink: docLink);

        var exception = Should.Throw<FormatException>(() => Load(json));

        exception.Message.ShouldContain("entry 0");
        exception.Message.ShouldContain($"'{expectedField}'");
    }

    [Theory]
    [InlineData("unknown", "\"value\"", "unknown")]
    [InlineData("fixture", "null", "fixture")]
    [InlineData("fixture", "[]", "fixture")]
    [InlineData("leg", "\"blazorServer\"", "leg")]
    [InlineData("kind", "\"NotAKind\"", "kind")]
    [InlineData("propertyMatch", "\"regex\"", "propertyMatch")]
    [InlineData("disposition", "\"quarantined\"", "disposition")]
    [InlineData("expires", "\"09/01/2026\"", "expires")]
    public void StrictLoaderRejectsUnknownMalformedAndInvalidFields(
        string field,
        string rawValue,
        string expectedField)
    {
        var json = field == "unknown"
            ? ValidJson(extra: $",\"unknown\":{rawValue}")
            : field == "propertyMatch"
                ? ValidJson(propertyMatch: "regex")
            : ReplaceField(ValidJson(), field, rawValue);

        var exception = Should.Throw<FormatException>(() => Load(json));

        exception.Message.ShouldContain("entry 0");
        exception.Message.ShouldContain($"'{expectedField}'");
    }

    [Theory]
    [InlineData("fixture")]
    [InlineData("leg")]
    [InlineData("step")]
    [InlineData("nodePath")]
    [InlineData("kind")]
    [InlineData("property")]
    [InlineData("reason")]
    [InlineData("disposition")]
    [InlineData("docLink")]
    [InlineData("expires")]
    public void StrictLoaderRejectsEveryMissingRequiredField(string field)
    {
        var json = RemoveField(ValidJson(), field);

        var exception = Should.Throw<FormatException>(() => Load(json));

        exception.Message.ShouldContain("entry 0");
        exception.Message.ShouldContain($"'{field}'");
    }

    [Theory]
    [InlineData("fixture")]
    [InlineData("nodePath")]
    [InlineData("property")]
    public void StrictLoaderRejectsWildcardIdentityValues(string field)
    {
        var exception = Should.Throw<FormatException>(() =>
            Load(ReplaceField(ValidJson(), field, "\"*\"")));

        exception.Message.ShouldContain($"'{field}'");
        exception.Message.ShouldContain("wildcard");
    }

    [Fact]
    public void StrictLoaderPermitsRequiredEmptyConsoleNodePath()
    {
        var waiver = Load(ValidJson(
            kind: "Console",
            nodePath: string.Empty,
            property: "error: boom",
            disposition: "deferred-defect",
            docLink: "https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/999999"))
            .ShouldHaveSingleItem();

        waiver.NodePath.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("docs/audits/../secret.md")]
    [InlineData("docs/audits/accepted.txt")]
    [InlineData("https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/999999")]
    public void AcceptedLimitationsRequireRepositoryRelativeAuditOrSpec(string docLink)
    {
        var exception = Should.Throw<FormatException>(() => Load(ValidJson(docLink: docLink)));
        exception.Message.ShouldContain("'docLink'");
    }

    [Theory]
    [InlineData("https://github.com/JakeMurrayDev/Blazix.BaseUI/pull/139")]
    [InlineData("https://github.com/other/Blazix.BaseUI/issues/139")]
    [InlineData("https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/0")]
    [InlineData("https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/139?state=open")]
    public void DeferredDefectsRequireAnExactRepositoryIssueUrl(string docLink)
    {
        var exception = Should.Throw<FormatException>(() => Load(ValidJson(
            disposition: "deferred-defect",
            docLink: docLink)));
        exception.Message.ShouldContain("'docLink'");
    }

    [Theory]
    [InlineData("2026-08-09")]
    [InlineData("2026-08-08")]
    public void StrictLoaderRejectsNonFutureExpiryWithoutGrace(string expires)
    {
        var exception = Should.Throw<FormatException>(() => Load(ValidJson(expires: expires)));
        exception.Message.ShouldContain("'expires'");
        exception.Message.ShouldContain("later than 2026-08-09");
    }

    [Theory]
    [InlineData("CorrespondenceUncertain")]
    [InlineData("ActionCompletionUnmet")]
    [InlineData("FixtureError")]
    public void StrictLoaderRejectsEveryNonWaivableKind(string kind)
    {
        var exception = Should.Throw<FormatException>(() => Load(ValidJson(kind: kind)));
        exception.Message.ShouldContain("'kind'");
        exception.Message.ShouldContain("nonwaivable");
    }

    [Fact]
    public void StrictLoaderRejectsDuplicateEntriesEvenWhenReasonsDiffer()
    {
        var first = ValidEntry();
        var second = ValidEntry(reason: "A different explanation.");

        var exception = Should.Throw<FormatException>(() => Load($"[{first},{second}]"));

        exception.Message.ShouldContain("entry 1");
        exception.Message.ShouldContain("duplicates");
    }

    [Fact]
    public void StrictLoaderRejectsExactAndPrefixEntriesWithTheSameSixFieldIdentity()
    {
        var exact = ValidEntry(
            kind: "Console",
            nodePath: string.Empty,
            property: "error: reconnect token=",
            reason: "Exact captured console error.",
            disposition: "deferred-defect",
            docLink: "https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/999999");
        var prefix = ValidEntry(
            kind: "Console",
            nodePath: string.Empty,
            property: "error: reconnect token=",
            propertyMatch: "prefix",
            reason: "Captured twice. Volatile suffix: circuit token.",
            disposition: "deferred-defect",
            docLink: "https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/999999");

        var exception = Should.Throw<FormatException>(() => Load($"[{exact},{prefix}]"));

        exception.Message.ShouldContain("entry 1");
        exception.Message.ShouldContain("duplicates");
    }

    [Fact]
    public void StrictLoaderRejectsDuplicateJsonPropertiesAndMalformedRoot()
    {
        var duplicate = ValidJson(extra: ",\"fixture\":\"again\"");
        Should.Throw<FormatException>(() => Load(duplicate))
            .Message.ShouldContain("appears more than once");
        Should.Throw<FormatException>(() => Load("{}"))
            .Message.ShouldContain("JSON array");
        Should.Throw<FormatException>(() => Load("["))
            .Message.ShouldContain("invalid JSON");
    }

    [Theory]
    [InlineData("switch/hero")]
    [InlineData("switch/hero@blue")]
    [InlineData("switch/hero@light@dark")]
    [InlineData("switch/hero@Light")]
    [InlineData("Switch/hero@light")]
    [InlineData("switch/hero-@light")]
    public void StrictLoaderRejectsMissingUnknownDuplicateOrCaseVariantThemeSuffix(string fixture)
    {
        var json = ValidJson().Replace(
            "\"fixture\":\"switch/hero@light\"",
            $"\"fixture\":{Json(fixture)}",
            StringComparison.Ordinal);

        var exception = Should.Throw<FormatException>(() => Load(json));

        exception.Message.ShouldContain("fixture");
        exception.Message.ShouldContain("@light");
    }

    [Fact]
    public void ExactWaiverDoesNotCrossThemeExecutionIdentity()
    {
        var waiver = Load(ValidJson()).ShouldHaveSingleItem();
        var darkFinding = Finding() with { Fixture = "switch/hero@dark" };

        var verdict = WaiverMatcher.Match(
            [darkFinding],
            [waiver],
            ReviewDate);

        verdict.Applied.ShouldBeEmpty();
        verdict.BlockingFindings.ShouldBe([darkFinding]);
        verdict.Diagnostics.ShouldHaveSingleItem().Kind.ShouldBe(WaiverDiagnosticKind.Unused);
    }

    private static FindingIdentity Identity()
        => FindingIdentity.From(Finding());

    private static Finding Finding(
        FindingKind kind = FindingKind.Attribute,
        string nodePath = "root > button[role=switch]",
        string property = "aria-checked",
        string? candidateValue = "false") => new()
        {
            Fixture = "switch/hero@light",
            Leg = ParityLeg.BlazorServer,
            Step = "toggle-on",
            Kind = kind,
            Severity = Severity.Error,
            NodePath = nodePath,
            Property = property,
            ReferenceValue = "true",
            CandidateValue = candidateValue,
            Message = "presentation"
        };

    private static Waiver Waiver() => new()
    {
        Fixture = "switch/hero@light",
        Leg = ParityLeg.BlazorServer,
        Step = "toggle-on",
        NodePath = "root > button[role=switch]",
        Kind = FindingKind.Attribute,
        Property = "aria-checked",
        Reason = "Documented component limitation.",
        Disposition = WaiverDisposition.AcceptedLimitation,
        DocLink = "docs/audits/switch-functional-audit.md",
        Expires = FutureDate
    };

    private static Waiver PrefixWaiver(string property)
        => Load(
            ValidJson(
                kind: "Console",
                nodePath: string.Empty,
                property: property,
                propertyMatch: "prefix",
                reason: "Captured twice. Volatile suffix: token.",
                disposition: "deferred-defect",
                docLink: "https://github.com/JakeMurrayDev/Blazix.BaseUI/issues/999999"),
            ValidIssueValidator()).ShouldHaveSingleItem();

    private static StubIssueValidator ValidIssueValidator()
        => new(new WaiverIssuePolicyValidation(
            IsOpen: true,
            IsOwned: true,
            CapturedAttemptCount: 2,
            HasAcceptanceCriteria: true));

    private static string ValidJson(
        string kind = "Attribute",
        string nodePath = "root > button[role=switch]",
        string property = "aria-checked",
        string? propertyMatch = null,
        string reason = "Documented component limitation.",
        string disposition = "accepted-limitation",
        string docLink = "docs/audits/switch-functional-audit.md",
        string expires = "2026-09-01",
        string extra = "")
        => $"[{ValidEntry(kind, nodePath, property, propertyMatch, reason, disposition, docLink, expires, extra)}]";

    private static string ValidEntry(
        string kind = "Attribute",
        string nodePath = "root > button[role=switch]",
        string property = "aria-checked",
        string? propertyMatch = null,
        string reason = "Documented component limitation.",
        string disposition = "accepted-limitation",
        string docLink = "docs/audits/switch-functional-audit.md",
        string expires = "2026-09-01",
        string extra = "")
        => $$"""
             {
               "fixture":"switch/hero@light",
               "leg":"BlazorServer",
               "step":"toggle-on",
               "nodePath":{{Json(nodePath)}},
               "kind":{{Json(kind)}},
               "property":{{Json(property)}},
               {{(propertyMatch is null ? string.Empty : $"\"propertyMatch\":{Json(propertyMatch)},")}}
               "reason":{{Json(reason)}},
               "disposition":{{Json(disposition)}},
               "docLink":{{Json(docLink)}},
               "expires":{{Json(expires)}}{{extra}}
             }
             """;

    private static IReadOnlyList<Waiver> Load(string json)
        => Load(json, issuePolicyValidator: null);

    private static IReadOnlyList<Waiver> Load(
        string json,
        IWaiverIssuePolicyValidator? issuePolicyValidator)
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, json);
            return WaiverFile.Load(path, ReviewDate, issuePolicyValidator);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string Json(string value)
        => System.Text.Json.JsonSerializer.Serialize(value);

    private static string ReplaceField(string json, string field, string rawValue)
    {
        var marker = $"\"{field}\":";
        var start = json.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var end = json.IndexOf(',', start);
        if (end < 0)
        {
            end = json.IndexOf('}', start);
        }

        return json[..start] + rawValue + json[end..];
    }

    private static string RemoveField(string json, string field)
    {
        var marker = $"\"{field}\":";
        var start = json.IndexOf(marker, StringComparison.Ordinal);
        var end = json.IndexOf(',', start);

        if (end >= 0)
        {
            return json.Remove(start, end - start + 1);
        }

        end = json.IndexOf('}', start);
        var comma = json.LastIndexOf(',', start);
        return json.Remove(comma, end - comma);
    }

    private sealed class StubIssueValidator(WaiverIssuePolicyValidation validation)
        : IWaiverIssuePolicyValidator
    {
        internal List<string> Urls { get; } = [];

        public WaiverIssuePolicyValidation Validate(string issueUrl)
        {
            Urls.Add(issueUrl);
            return validation;
        }
    }
}
