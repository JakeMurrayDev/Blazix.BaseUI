using Blazix.BaseUI.Parity.Tests.Fixtures;
using Blazix.BaseUI.Parity.Tests.Infrastructure;

namespace Blazix.BaseUI.Parity.Tests.Baselines;

/// <summary>Runs the explicit local command that refreshes committed React baselines.</summary>
internal static class BaselineGenerator
{
    /// <summary>Captures and writes every selected manifest fixture.</summary>
    /// <returns>Zero only when every selected baseline can be loaded back successfully.</returns>
    internal static async Task<int> RunAsync()
    {
        var options = ParityOptions.FromEnvironment() with
        {
            Mode = ParityReferenceMode.WriteBaseline
        };
        var fixtures = SelectFixtures(FixtureManifest.Load(), options.FixtureFilter);
        if (fixtures.Count == 0)
        {
            Console.Error.WriteLine("No parity fixtures matched PARITY_FIXTURES.");
            return 1;
        }

        var server = new ParityServerAssemblyFixture();
        var playwright = new PlaywrightFixture();
        var serverStarted = false;
        var browserStarted = false;

        try
        {
            serverStarted = true;
            await server.InitializeAsync();
            browserStarted = true;
            await playwright.InitializeAsync();

            var store = new BaselineStore();
            var migrating = store.RequiresCaptureSchemaMigration();
            var manifestFixtureCount = FixtureManifest.Load().Count;
            if (migrating && fixtures.Count != manifestFixtureCount)
            {
                Console.Error.WriteLine(
                    "A capture-schema migration must refresh the complete fixture manifest.");
                return 1;
            }
            var stageCompleteSet = fixtures.Count == manifestFixtureCount;
            return await WithMigrationStoreAsync(store, stageCompleteSet, async (targetStore, stagingRoot) =>
            {
                var runner = new ParityRunner(targetStore);
                var platform = BaselinePlatform.Current(playwright.Browser);
                var refreshed = new List<FixtureEntry>(fixtures.Count);
                foreach (var fixture in fixtures)
                {
                    Console.WriteLine($"Refreshing React baseline for {fixture.Id}...");
                    var results = await runner.RunAsync(playwright.Browser, fixture, options);

                    try
                    {
                        RequireFreshWrite(fixture, results);
                    }
                    catch (Exception exception)
                    {
                        Console.Error.WriteLine(
                            $"Baseline refresh failed for {fixture.Id}: {exception.Message}");
                        return 1;
                    }

                    var blockingCandidates = results.Count(result => result.HasBlockingEvidence);
                    Console.WriteLine(
                        $"Refreshed {fixture.Id}; candidate parity still has " +
                        $"{blockingCandidates} blocking leg(s).");
                    refreshed.Add(fixture);
                }

                // BaselineStore.Load deliberately requires a complete manifest set. Readback
                // therefore happens only after every replacement has succeeded.
                foreach (var fixture in refreshed)
                {
                    foreach (var theme in fixture.Themes)
                    {
                        _ = targetStore.Load(fixture, theme, platform);
                    }
                }

                if (stageCompleteSet)
                {
                    store.ReplaceWithValidatedStaging(stagingRoot, platform);
                }
                return 0;
            });
        }
        finally
        {
            try
            {
                if (browserStarted)
                {
                    await playwright.DisposeAsync();
                }
            }
            finally
            {
                if (serverStarted)
                {
                    await server.DisposeAsync();
                }
            }
        }
    }

    internal static async Task<T> WithMigrationStoreAsync<T>(
        BaselineStore store,
        bool stageCompleteSet,
        Func<BaselineStore, string, Task<T>> operation)
    {
        (BaselineStore Store, string Root) migration = (store, string.Empty);
        try
        {
            if (stageCompleteSet)
            {
                migration = store.CreateSchemaMigrationStaging();
            }

            return await operation(migration.Store, migration.Root);
        }
        finally
        {
            if (stageCompleteSet &&
                !string.IsNullOrEmpty(migration.Root) &&
                Directory.Exists(migration.Root))
            {
                Directory.Delete(migration.Root, recursive: true);
            }
        }
    }

    internal static void RequireFreshWrite(
        FixtureEntry fixture,
        IReadOnlyList<ParityRunResult> results)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentNullException.ThrowIfNull(results);

        var expected = FixtureExecution.Expand(fixture)
            .SelectMany(execution => new[]
            {
                (execution.ExecutionId, execution.Theme, Capture.ParityLeg.BlazorServer),
                (execution.ExecutionId, execution.Theme, Capture.ParityLeg.BlazorWasm)
            })
            .ToArray();
        var actual = results
            .Select(result => (result.ExecutionId, result.Theme, result.Leg))
            .ToArray();

        if (!actual.SequenceEqual(expected) ||
            results.Any(result =>
                !string.Equals(result.Fixture, fixture.Id, StringComparison.Ordinal) ||
                result.Reference is null ||
                result.Reference.CaptureSchemaVersion != Capture.CaptureSchema.CurrentVersion ||
                !string.Equals(result.Reference.Fixture, fixture.Id, StringComparison.Ordinal) ||
                !string.Equals(result.Reference.Theme, result.Theme, StringComparison.Ordinal) ||
                result.Reference.Leg != Capture.ParityLeg.React ||
                result.BaselineWrite is null ||
                !string.Equals(result.BaselineWrite.Fixture, fixture.Id, StringComparison.Ordinal) ||
                result.Findings.Any(finding =>
                    finding.Kind == Diff.FindingKind.FixtureError &&
                    finding.Leg == Capture.ParityLeg.React)))
        {
            throw new InvalidOperationException(
                $"Baseline refresh for '{fixture.Id}' produced no positive fresh-write evidence.");
        }
    }

    internal static Capture.CaptureBundle RequireFreshWriteAndLoad(
        FixtureEntry fixture,
        IReadOnlyList<ParityRunResult> results,
        Func<Capture.CaptureBundle> load)
    {
        ArgumentNullException.ThrowIfNull(load);
        RequireFreshWrite(fixture, results);
        return load();
    }

    internal static IReadOnlyList<FixtureEntry> SelectFixtures(
        IReadOnlyList<FixtureEntry> fixtures,
        string? filter)
        => string.IsNullOrEmpty(filter)
            ? fixtures
            : fixtures
                .Where(fixture => ParityOptions.MatchesFixture(fixture.Id, filter))
                .ToArray();
}
