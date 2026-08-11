using System.Text.Json;
using System.Text.Json.Serialization;

namespace Blazix.BaseUI.Parity.Tests.Infrastructure;

/// <summary>Ordered diagnostics for catalog entries not yet authored in the corpus.</summary>
/// <param name="MissingManifestIds">Catalog ids absent from the ordinary manifest.</param>
/// <param name="MissingRegistryIds">Catalog ids absent from the ordinary Blazor registry.</param>
public sealed record MilestoneReconciliation(
    IReadOnlyList<string> MissingManifestIds,
    IReadOnlyList<string> MissingRegistryIds);

/// <summary>Strict committed authority for the first parity milestone.</summary>
public static class MilestoneFixtureCatalog
{
    private const int CurrentSchemaVersion = 1;
    private const int ExpectedFixtureCount = 29;
    private const int ExpectedComponentCount = 26;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        AllowDuplicateProperties = false,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    private static readonly Lazy<MilestoneCatalogDocument> Current = new(LoadCurrent);

    /// <summary>Gets the 29 fixture ids in their required milestone order.</summary>
    public static IReadOnlyList<string> Ids => Current.Value.Fixtures;

    /// <summary>Gets the 26 component ids in first-fixture order.</summary>
    public static IReadOnlyList<string> Components =>
        [.. Ids.Select(ComponentOf).Distinct(StringComparer.Ordinal)];

    /// <summary>Parses and strictly validates a milestone catalog document.</summary>
    /// <param name="json">The complete JSON document.</param>
    /// <returns>The ordered fixture ids.</returns>
    internal static IReadOnlyList<string> Parse(string json)
    {
        MilestoneCatalogDocument document;
        try
        {
            document = JsonSerializer.Deserialize<MilestoneCatalogDocument>(
                json, SerializerOptions)
                ?? throw new JsonException("catalog contains null");
        }
        catch (JsonException exception)
        {
            throw new FormatException($"Milestone catalog is corrupt: {exception.Message}", exception);
        }

        if (document.SchemaVersion != CurrentSchemaVersion ||
            document.FixtureCount != ExpectedFixtureCount ||
            document.ComponentCount != ExpectedComponentCount ||
            document.Fixtures is null ||
            document.Fixtures.Count != document.FixtureCount ||
            document.Fixtures.Any(id => !IsFixtureId(id)) ||
            document.Fixtures.Distinct(StringComparer.Ordinal).Count() != document.Fixtures.Count ||
            document.Fixtures.Select(ComponentOf)
                .Distinct(StringComparer.Ordinal).Count() != document.ComponentCount)
        {
            throw new FormatException(
                "Milestone catalog schema, declared counts, fixture ids, or component count is invalid.");
        }

        return document.Fixtures;
    }

    /// <summary>
    /// Reconciles the ordinary manifest and registry while retaining ordered catalog omissions.
    /// </summary>
    /// <param name="manifestIds">Ordinary fixture ids in manifest order.</param>
    /// <param name="registryIds">All reflection-derived Blazor fixture ids.</param>
    /// <returns>Missing catalog ids in catalog order.</returns>
    internal static MilestoneReconciliation Reconcile(
        IEnumerable<string> manifestIds,
        IEnumerable<string> registryIds)
    {
        var manifest = manifestIds.ToArray();
        var registry = registryIds
            .Where(id => !id.StartsWith("harness/", StringComparison.Ordinal))
            .ToArray();

        RejectDuplicates(manifest, "manifest");
        RejectDuplicates(registry, "fixture registry");

        var manifestSet = manifest.ToHashSet(StringComparer.Ordinal);
        var registrySet = registry.ToHashSet(StringComparer.Ordinal);
        var missingRegistryEntries = manifestSet.Except(registrySet, StringComparer.Ordinal).ToArray();
        var unexpectedRegistryEntries = registrySet.Except(manifestSet, StringComparer.Ordinal).ToArray();
        if (missingRegistryEntries.Length != 0 || unexpectedRegistryEntries.Length != 0)
        {
            throw new InvalidOperationException(
                "The fixture manifest and Blazor registry do not reconcile. " +
                $"Missing registry ids: {Describe(missingRegistryEntries)}. " +
                $"Unexpected registry ids: {Describe(unexpectedRegistryEntries)}.");
        }

        var authoredCatalogOrder = manifest.Where(Ids.Contains).ToArray();
        var expectedAuthoredOrder = Ids.Where(manifestSet.Contains).ToArray();
        if (!authoredCatalogOrder.SequenceEqual(expectedAuthoredOrder, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                "Manifest milestone fixtures are not in milestone catalog order.");
        }

        return new MilestoneReconciliation(
            Ids.Where(id => !manifestSet.Contains(id)).ToArray(),
            Ids.Where(id => !registrySet.Contains(id)).ToArray());
    }

    private static string ComponentOf(string fixtureId)
        => fixtureId[..fixtureId.IndexOf('/')];

    private static string Describe(IReadOnlyCollection<string> ids)
        => ids.Count == 0 ? "none" : string.Join(", ", ids.Select(id => $"'{id}'"));

    internal static bool IsFixtureId(string? id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Count(character => character == '/') != 1)
        {
            return false;
        }

        return id.Split('/').All(segment =>
            segment.Length > 0 &&
            segment[0] is >= 'a' and <= 'z' &&
            segment[^1] is >= 'a' and <= 'z' or >= '0' and <= '9' &&
            !segment.Contains("--", StringComparison.Ordinal) &&
            segment.All(character =>
                character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-'));
    }

    private static MilestoneCatalogDocument LoadCurrent()
    {
        var fixtures = Parse(File.ReadAllText(
            Path.Combine(ParityPaths.Manifest, "milestone-1.json")));
        return new MilestoneCatalogDocument
        {
            SchemaVersion = CurrentSchemaVersion,
            FixtureCount = ExpectedFixtureCount,
            ComponentCount = ExpectedComponentCount,
            Fixtures = fixtures
        };
    }

    private static void RejectDuplicates(IEnumerable<string> ids, string source)
    {
        var duplicate = ids
            .GroupBy(id => id, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"The {source} contains duplicate fixture id '{duplicate.Key}'.");
        }
    }

    private sealed record MilestoneCatalogDocument
    {
        [JsonPropertyName("schemaVersion")]
        public required int SchemaVersion { get; init; }

        [JsonPropertyName("fixtureCount")]
        public required int FixtureCount { get; init; }

        [JsonPropertyName("componentCount")]
        public required int ComponentCount { get; init; }

        [JsonPropertyName("fixtures")]
        public required IReadOnlyList<string> Fixtures { get; init; }
    }
}
