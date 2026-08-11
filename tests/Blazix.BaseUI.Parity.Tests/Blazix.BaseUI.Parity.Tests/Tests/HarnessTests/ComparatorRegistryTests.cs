using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Diff;
using Blazix.BaseUI.Parity.Tests.Infrastructure;
using Shouldly;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

/// <summary>Pins the complete ownership and production order of comparison kinds.</summary>
public sealed class ComparatorRegistryTests
{
    [Fact]
    public void ProductionRegistryOwnsEveryComparatorKindExactlyOnceInExplicitOrder()
    {
        var registry = new ComparatorRegistry();

        registry.OrderedKinds.ShouldBe(
        [
            FindingKind.Structure,
            FindingKind.CorrespondenceUncertain,
            FindingKind.Attribute,
            FindingKind.AriaSnapshot,
            FindingKind.ComputedStyle,
            FindingKind.CustomProperty,
            FindingKind.Geometry,
            FindingKind.Focus,
            FindingKind.Console,
            FindingKind.Marker,
            FindingKind.Timeline,
            FindingKind.Pixel,
            FindingKind.SelectorUnresolved,
            FindingKind.SelectorNonActionable
        ]);

        registry.OrderedKinds
            .Concat(ComparatorRegistry.RunnerOwnedKinds)
            .OrderBy(kind => kind)
            .ShouldBe(Enum.GetValues<FindingKind>().OrderBy(kind => kind));
        ComparatorRegistry.RunnerOwnedKinds.ShouldBe(
            new HashSet<FindingKind>
            {
                FindingKind.ActionCompletionUnmet,
                FindingKind.FixtureError
            }, ignoreOrder: true);
        ComparatorRegistry.NonWaivableKinds.ShouldBe(
            new HashSet<FindingKind>
            {
                FindingKind.CorrespondenceUncertain,
                FindingKind.ActionCompletionUnmet,
                FindingKind.FixtureError
            }, ignoreOrder: true);
    }

    [Fact]
    public void RejectsDuplicateMissingAndRunnerOwnedComparatorKinds()
    {
        var complete = ComparatorKinds()
            .Select(kind => (IComparator)new StubComparator(kind))
            .ToList();

        Should.Throw<ArgumentException>(() => new ComparatorRegistry(
                [.. complete, new StubComparator(FindingKind.Attribute)]))
            .Message.ShouldContain("more than one comparator owner");

        Should.Throw<ArgumentException>(() => new ComparatorRegistry(
                [.. complete.Where(item => item.Kind != FindingKind.Geometry)]))
            .Message.ShouldContain("Geometry");

        Should.Throw<ArgumentException>(() => new ComparatorRegistry(
                [.. complete, new StubComparator(FindingKind.FixtureError)]))
            .Message.ShouldContain("Runner-owned");
    }

    [Fact]
    public void RejectsAComparatorThatEmitsAnotherKindsFinding()
    {
        var comparators = ComparatorKinds()
            .Select(kind => (IComparator)new StubComparator(
                kind,
                emittedKind: kind == FindingKind.Structure ? FindingKind.Attribute : null))
            .ToArray();
        var registry = new ComparatorRegistry(comparators);

        Should.Throw<InvalidOperationException>(() => registry.Compare(Context()))
            .Message.ShouldContain("owns 'Structure' but emitted 'Attribute'");
    }

    [Fact]
    public void RunnerExecutesEveryComparatorExactlyOnceInRegistryOrder()
    {
        var calls = new List<FindingKind>();
        var registry = new ComparatorRegistry(
            [.. ComparatorKinds().Select(kind =>
                (IComparator)new RecordingComparator(kind, calls))]);
        var fixture = new FixtureEntry
        {
            Id = "harness/registry",
            Component = "harness",
            React = "internal:none",
            Blazor = "Harness/Registry",
            PixelThreshold = 0.001,
            Steps = [new StepEntry { Name = "initial" }]
        };
        var reference = new CaptureBundle
        {
            CaptureSchemaVersion = CaptureSchema.CurrentVersion,
            Fixture = fixture.Id,
            Theme = "light",
            Leg = ParityLeg.React,
            Steps = [Capture("initial")]
        };
        var candidate = new CaptureBundle
        {
            CaptureSchemaVersion = CaptureSchema.CurrentVersion,
            Fixture = fixture.Id,
            Theme = "light",
            Leg = ParityLeg.BlazorServer,
            Steps = [Capture("initial")]
        };
        var runner = new ParityRunner(registry, "unused", "unused", "unused");

        var result = runner.Compare(
            fixture, ParityLeg.BlazorServer, reference, candidate);

        calls.ShouldBe(ComparatorKinds());
        result.Findings.Select(finding => finding.Kind).ShouldBe(ComparatorKinds());
        result.Findings.ShouldAllBe(finding => finding.Leg == ParityLeg.BlazorServer);
        result.Findings.ShouldAllBe(finding => finding.Step == "initial");
    }

    [Fact]
    public void RunnerNormalizesComparatorExceptionsAsBlockingEvidence()
    {
        var comparators = ComparatorKinds()
            .Select(kind => kind == FindingKind.Geometry
                ? (IComparator)new ThrowingComparator(kind)
                : new StubComparator(kind))
            .ToArray();
        var registry = new ComparatorRegistry(comparators);
        var fixture = new FixtureEntry
        {
            Id = "harness/comparator-failure",
            Component = "harness",
            React = "internal:none",
            Blazor = "Harness/ComparatorFailure",
            Steps = [new StepEntry { Name = "initial" }]
        };
        var reference = new CaptureBundle
        {
            CaptureSchemaVersion = CaptureSchema.CurrentVersion,
            Fixture = fixture.Id,
            Theme = "light",
            Leg = ParityLeg.React,
            Steps = [Capture("initial")]
        };
        var candidate = new CaptureBundle
        {
            CaptureSchemaVersion = CaptureSchema.CurrentVersion,
            Fixture = fixture.Id,
            Theme = "light",
            Leg = ParityLeg.BlazorServer,
            Steps = [Capture("initial")]
        };

        var result = new ParityRunner(registry, "unused", "unused", "unused")
            .Compare(fixture, ParityLeg.BlazorServer, reference, candidate);

        var failure = result.Findings.ShouldHaveSingleItem();
        failure.Kind.ShouldBe(FindingKind.FixtureError);
        failure.Severity.ShouldBe(Severity.Error);
        failure.Leg.ShouldBe(ParityLeg.BlazorServer);
        failure.Step.ShouldBe("initial");
        failure.Property.ShouldBe("comparator");
        failure.Message.ShouldContain("Geometry comparator probe");
        failure.Message.ShouldNotContain("/Users/private");
        result.HasBlockingEvidence.ShouldBeTrue();
    }

    private static IReadOnlyList<FindingKind> ComparatorKinds()
        => Enum.GetValues<FindingKind>()
            .Where(kind => !ComparatorRegistry.RunnerOwnedKinds.Contains(kind))
            .ToArray();

    private static ComparisonContext Context()
    {
        var capture = Capture("initial");
        return new ComparisonContext(
            "harness/registry",
            "light",
            "harness/registry@light",
            ParityLeg.BlazorServer,
            "initial",
            capture,
            capture,
            0.001);
    }

    private static StepCapture Capture(string step) => new()
    {
        Step = step,
        Dom = new DomNode
        {
            Tag = "div",
            Path = "root",
            Attributes = new Dictionary<string, string>(),
            Classes = [],
            Text = string.Empty,
            Children = []
        },
        Styles = new Dictionary<string, IReadOnlyDictionary<string, string>>(),
        CustomProps = new Dictionary<string, IReadOnlyDictionary<string, string>>(),
        Geometry = new Dictionary<string, IReadOnlyDictionary<string, double>>()
    };

    private sealed class StubComparator(
        FindingKind kind,
        FindingKind? emittedKind = null) : IComparator
    {
        public FindingKind Kind => kind;

        public IEnumerable<Finding> Compare(ComparisonContext context)
        {
            if (emittedKind is null)
            {
                return [];
            }

            return
            [
                new Finding
                {
                    Fixture = context.Fixture,
                    Leg = context.Leg,
                    Step = context.Step,
                    Kind = emittedKind.Value,
                    Severity = Severity.Error,
                    Message = "wrong kind"
                }
            ];
        }
    }

    private sealed class RecordingComparator(
        FindingKind kind,
        ICollection<FindingKind> calls) : IComparator
    {
        public FindingKind Kind => kind;

        public IEnumerable<Finding> Compare(ComparisonContext context)
        {
            calls.Add(kind);

            return
            [
                new Finding
                {
                    Fixture = context.Fixture,
                    Leg = context.Leg,
                    Step = context.Step,
                    Kind = kind,
                    Severity = Severity.Error,
                    Message = $"{kind} executed"
                }
            ];
        }
    }

    private sealed class ThrowingComparator(FindingKind kind) : IComparator
    {
        public FindingKind Kind => kind;

        public IEnumerable<Finding> Compare(ComparisonContext context)
            => throw new InvalidOperationException(
                $"{kind} comparator probe at /Users/private/task15-live-field/attempt-1");
    }
}
