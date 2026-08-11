using Blazix.BaseUI.Parity.Tests.Capture;

namespace Blazix.BaseUI.Parity.Tests.Infrastructure;

/// <summary>One primitive public fixture-theme-candidate theory row.</summary>
public sealed record ParityCase(
    string Fixture,
    string Theme,
    ParityLeg Leg,
    int CatalogOrdinal,
    int Shard,
    bool Timing,
    bool Authored);

/// <summary>Durable catalog-backed public parity theory data.</summary>
public static class ParityTheoryData
{
    public static IEnumerable<object[]> StaticShard0 => Rows(shard: 0, timing: false);

    public static IEnumerable<object[]> StaticShard1 => Rows(shard: 1, timing: false);

    public static IEnumerable<object[]> StaticShard2 => Rows(shard: 2, timing: false);

    public static IEnumerable<object[]> StaticShard3 => Rows(shard: 3, timing: false);

    public static IEnumerable<object[]> Timing => Rows(shard: -1, timing: true);

    /// <summary>Builds the deterministic catalog surface over current authored manifest rows.</summary>
    public static IReadOnlyList<ParityCase> Build(IReadOnlyList<FixtureEntry> manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var byId = manifest.ToDictionary(item => item.Id, StringComparer.Ordinal);
        var cases = new List<ParityCase>();

        for (var ordinal = 0; ordinal < MilestoneFixtureCatalog.Ids.Count; ordinal++)
        {
            var id = MilestoneFixtureCatalog.Ids[ordinal];
            byId.TryGetValue(id, out var fixture);
            var timing = fixture?.Steps.Any(step =>
                string.Equals(step.Settle, "animation", StringComparison.Ordinal)) == true;
            var themes = fixture?.Themes ?? ["light"];
            foreach (var theme in themes)
            {
                cases.Add(new ParityCase(
                    id,
                    theme,
                    ParityLeg.BlazorServer,
                    ordinal,
                    timing ? -1 : ordinal % 4,
                    timing,
                    fixture is not null));
                cases.Add(new ParityCase(
                    id,
                    theme,
                    ParityLeg.BlazorWasm,
                    ordinal,
                    timing ? -1 : ordinal % 4,
                    timing,
                    fixture is not null));
            }
        }

        return cases;
    }

    private static IEnumerable<object[]> Rows(int shard, bool timing)
        => Build(LoadManifestForDiscovery(FixtureManifest.Load))
            .Where(item => item.Timing == timing && (timing || item.Shard == shard))
            .Select(item => new object[] { item.Fixture, item.Theme, item.Leg });

    internal static IReadOnlyList<FixtureEntry> LoadManifestForDiscovery(
        Func<IReadOnlyList<FixtureEntry>> load)
        => ParityRunAccumulator.LoadManifest(load).Manifest;
}
