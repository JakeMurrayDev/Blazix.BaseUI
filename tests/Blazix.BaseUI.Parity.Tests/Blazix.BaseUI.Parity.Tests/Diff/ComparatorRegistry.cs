using System.Collections.Frozen;

namespace Blazix.BaseUI.Parity.Tests.Diff;

/// <summary>
/// Owns the explicit, deterministic production ordering of every capture comparator.
/// </summary>
/// <remarks>
/// Runner-owned findings describe missing or untrustworthy evidence and deliberately have no
/// comparator. Every other <see cref="FindingKind"/> must be owned exactly once so adding an enum
/// value cannot silently exclude a comparison dimension from production runs.
/// </remarks>
/// <param name="comparators">The complete ordered comparator set.</param>
public sealed class ComparatorRegistry(IReadOnlyList<IComparator> comparators)
{
    private static readonly FrozenSet<FindingKind> RunnerKinds =
        new[]
        {
            FindingKind.ActionCompletionUnmet,
            FindingKind.FixtureError
        }.ToFrozenSet();

    private static readonly FrozenSet<FindingKind> NonWaivable =
        new[]
        {
            FindingKind.CorrespondenceUncertain,
            FindingKind.ActionCompletionUnmet,
            FindingKind.FixtureError
        }.ToFrozenSet();

    private readonly IReadOnlyList<IComparator> comparators = Validate(comparators);

    /// <summary>Creates the production comparator composition.</summary>
    public ComparatorRegistry()
        : this(CreateDefault())
    {
    }

    /// <summary>Creates the production composition over a scoped screenshot directory.</summary>
    /// <param name="screenshotDirectory">Where pixel captures and overlays are stored.</param>
    public ComparatorRegistry(string screenshotDirectory)
        : this(CreateDefault(screenshotDirectory))
    {
    }

    /// <summary>Gets the comparator-owned kinds in production execution order.</summary>
    public IReadOnlyList<FindingKind> OrderedKinds => [.. comparators.Select(item => item.Kind)];

    /// <summary>Gets the kinds produced directly by the runner rather than a comparator.</summary>
    public static IReadOnlySet<FindingKind> RunnerOwnedKinds => RunnerKinds;

    /// <summary>Gets evidence kinds policy layers must never accept through a waiver.</summary>
    public static IReadOnlySet<FindingKind> NonWaivableKinds => NonWaivable;

    /// <summary>Runs the complete comparator composition for one paired step.</summary>
    /// <param name="context">The paired capture step.</param>
    /// <returns>The findings in registry and comparator order.</returns>
    public IReadOnlyList<Finding> Compare(ComparisonContext context)
    {
        var findings = new List<Finding>();

        foreach (var comparator in comparators)
        {
            foreach (var finding in comparator.Compare(context))
            {
                if (finding.Kind != comparator.Kind)
                {
                    throw new InvalidOperationException(
                        $"Comparator '{comparator.GetType().Name}' owns '{comparator.Kind}' " +
                        $"but emitted '{finding.Kind}'.");
                }

                findings.Add(finding);
            }
        }

        return findings;
    }

    private static IReadOnlyList<IComparator> CreateDefault()
        => CreateDefault(Infrastructure.ParityPaths.Screenshots);

    private static IReadOnlyList<IComparator> CreateDefault(string screenshotDirectory) =>
    [
        new StructureComparator(),
        new CorrespondenceUncertainComparator(),
        new AttributeComparator(),
        new AriaSnapshotComparator(),
        new ComputedStyleComparator(),
        new CustomPropertyComparator(),
        new GeometryComparator(),
        new FocusComparator(),
        new ConsoleComparator(),
        new MarkerComparator(),
        new TimelineComparator(),
        new PixelComparator(screenshotDirectory),
        new SelectorUnresolvedComparator(),
        new SelectorNonActionableComparator()
    ];

    private static IReadOnlyList<IComparator> Validate(IReadOnlyList<IComparator>? entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var duplicate = entries
            .GroupBy(item => item.Kind)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"Finding kind '{duplicate.Key}' has more than one comparator owner.",
                nameof(entries));
        }

        var illegallyOwned = entries
            .Select(item => item.Kind)
            .Where(RunnerKinds.Contains)
            .OrderBy(kind => kind)
            .ToArray();

        if (illegallyOwned.Length > 0)
        {
            throw new ArgumentException(
                "Runner-owned finding kinds cannot be registered as comparators: " +
                string.Join(", ", illegallyOwned),
                nameof(entries));
        }

        var expected = Enum.GetValues<FindingKind>()
            .Where(kind => !RunnerKinds.Contains(kind))
            .ToHashSet();
        var missing = expected
            .Except(entries.Select(item => item.Kind))
            .OrderBy(kind => kind)
            .ToArray();

        if (missing.Length > 0)
        {
            throw new ArgumentException(
                "Comparator owners are missing for: " + string.Join(", ", missing),
                nameof(entries));
        }

        return [.. entries];
    }
}
