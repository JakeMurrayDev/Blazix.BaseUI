using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Diff;
using Shouldly;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

/// <summary>
/// Pins the distinct ordinal-multiset contracts for unresolved and non-actionable selectors.
/// </summary>
public sealed class SelectorComparatorTests
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> NoText
        = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>> NoNumbers
        = new Dictionary<string, IReadOnlyDictionary<string, double>>(StringComparer.Ordinal);

    [Fact]
    public void EqualSelectorMultisetsProduceNoFindings()
    {
        var context = Context(
            Capture(
                unresolved: ["[role=dialog]", "[role=dialog]", "[role=switch]"],
                nonActionable: ["button", "button"]),
            Capture(
                unresolved: ["[role=switch]", "[role=dialog]", "[role=dialog]"],
                nonActionable: ["button", "button"]));

        new SelectorUnresolvedComparator().Compare(context).ShouldBeEmpty();
        new SelectorNonActionableComparator().Compare(context).ShouldBeEmpty();
    }

    [Fact]
    public void ReportsReferenceOnlyUnresolvedSelectorAsAnError()
    {
        var finding = new SelectorUnresolvedComparator()
            .Compare(Context(Capture(unresolved: ["[role=dialog]"]), Capture()))
            .ShouldHaveSingleItem();

        AssertFinding(
            finding,
            FindingKind.SelectorUnresolved,
            "[role=dialog]",
            referenceCount: "1",
            candidateCount: "0");
    }

    [Fact]
    public void ReportsCandidateOnlyUnresolvedSelectorAsAnError()
    {
        var finding = new SelectorUnresolvedComparator()
            .Compare(Context(Capture(), Capture(unresolved: ["[role=dialog]"])))
            .ShouldHaveSingleItem();

        AssertFinding(
            finding,
            FindingKind.SelectorUnresolved,
            "[role=dialog]",
            referenceCount: "0",
            candidateCount: "1");
    }

    [Fact]
    public void PreservesRepeatedUnresolvedSelectorOccurrences()
    {
        var finding = new SelectorUnresolvedComparator()
            .Compare(Context(
                Capture(unresolved: ["[role=dialog]", "[role=dialog]", "[role=dialog]"]),
                Capture(unresolved: ["[role=dialog]"])))
            .ShouldHaveSingleItem();

        AssertFinding(
            finding,
            FindingKind.SelectorUnresolved,
            "[role=dialog]",
            referenceCount: "3",
            candidateCount: "1");
    }

    [Fact]
    public void ReportsReferenceOnlyNonActionableSelectorAsAnError()
    {
        var finding = new SelectorNonActionableComparator()
            .Compare(Context(Capture(nonActionable: ["[role=switch]"]), Capture()))
            .ShouldHaveSingleItem();

        AssertFinding(
            finding,
            FindingKind.SelectorNonActionable,
            "[role=switch]",
            referenceCount: "1",
            candidateCount: "0");
    }

    [Fact]
    public void ReportsCandidateOnlyNonActionableSelectorAsAnError()
    {
        var finding = new SelectorNonActionableComparator()
            .Compare(Context(Capture(), Capture(nonActionable: ["[role=switch]"])))
            .ShouldHaveSingleItem();

        AssertFinding(
            finding,
            FindingKind.SelectorNonActionable,
            "[role=switch]",
            referenceCount: "0",
            candidateCount: "1");
    }

    [Fact]
    public void PreservesRepeatedNonActionableSelectorOccurrences()
    {
        var finding = new SelectorNonActionableComparator()
            .Compare(Context(
                Capture(nonActionable: ["[role=switch]", "[role=switch]", "[role=switch]"]),
                Capture(nonActionable: ["[role=switch]"])))
            .ShouldHaveSingleItem();

        AssertFinding(
            finding,
            FindingKind.SelectorNonActionable,
            "[role=switch]",
            referenceCount: "3",
            candidateCount: "1");
    }

    [Fact]
    public void CrossCategoryFailuresEmitBothTypedFindings()
    {
        const string selector = "[aria-haspopup],[aria-expanded]";
        var context = Context(
            Capture(unresolved: [selector]),
            Capture(nonActionable: [selector]));

        var findings = new SelectorUnresolvedComparator().Compare(context)
            .Concat(new SelectorNonActionableComparator().Compare(context))
            .ToList();

        findings.Count.ShouldBe(2);
        AssertFinding(
            findings.Single(finding => finding.Kind == FindingKind.SelectorUnresolved),
            FindingKind.SelectorUnresolved,
            selector,
            referenceCount: "1",
            candidateCount: "0");
        AssertFinding(
            findings.Single(finding => finding.Kind == FindingKind.SelectorNonActionable),
            FindingKind.SelectorNonActionable,
            selector,
            referenceCount: "0",
            candidateCount: "1");
    }

    [Fact]
    public void OrdersSelectorsOrdinallyWithoutFoldingCase()
    {
        var context = Context(
            Capture(),
            Capture(unresolved: ["[role=switch]", "[Role=switch]", "button"]));

        new SelectorUnresolvedComparator().Compare(context).Select(finding => finding.Property)
            .ShouldBe(["[Role=switch]", "[role=switch]", "button"]);
    }

    [Fact]
    public void EachSelectorComparatorOwnsExactlyItsTypedKind()
    {
        new SelectorUnresolvedComparator().Kind.ShouldBe(FindingKind.SelectorUnresolved);
        new SelectorNonActionableComparator().Kind.ShouldBe(FindingKind.SelectorNonActionable);
    }

    private static void AssertFinding(
        Finding finding,
        FindingKind kind,
        string selector,
        string referenceCount,
        string candidateCount)
    {
        finding.Fixture.ShouldBe("switch/hero@light");
        finding.Leg.ShouldBe(ParityLeg.BlazorWasm);
        finding.Step.ShouldBe("toggle-off");
        finding.Kind.ShouldBe(kind);
        finding.Severity.ShouldBe(Severity.Error);
        finding.NodePath.ShouldBeEmpty();
        finding.Property.ShouldBe(selector);
        finding.ReferenceValue.ShouldBe(referenceCount);
        finding.CandidateValue.ShouldBe(candidateCount);
    }

    private static ComparisonContext Context(StepCapture reference, StepCapture candidate)
        => new(
            "switch/hero",
            "light",
            "switch/hero@light",
            ParityLeg.BlazorWasm,
            "toggle-off",
            reference,
            candidate,
            0.001);

    private static StepCapture Capture(
        IReadOnlyList<string>? unresolved = null,
        IReadOnlyList<string>? nonActionable = null) => new()
        {
            Step = "toggle-off",
            Dom = new DomNode
            {
                Tag = "span",
                Path = "root > span[role=switch]",
                Attributes = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["role"] = "switch"
                },
                Classes = [],
                Text = string.Empty,
                Children = []
            },
            Styles = NoText,
            CustomProps = NoText,
            Geometry = NoNumbers,
            Actions =
            [
                .. (unresolved ?? []).Select((selector, index) => new ActionExecution
                {
                    ActionIndex = index,
                    Verb = "click",
                    ExpandedSelector = selector,
                    Status = ActionExecutionStatus.Unresolved
                }),
                .. (nonActionable ?? []).Select((selector, index) => new ActionExecution
                {
                    ActionIndex = (unresolved?.Count ?? 0) + index,
                    Verb = "click",
                    ExpandedSelector = selector,
                    Status = ActionExecutionStatus.NonActionable
                })
            ]
        };
}
