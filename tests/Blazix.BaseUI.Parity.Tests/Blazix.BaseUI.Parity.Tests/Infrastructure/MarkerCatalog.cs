using System.Text.Json;
using System.Text.Json.Serialization;

namespace Blazix.BaseUI.Parity.Tests.Infrastructure;

/// <summary>
/// Reads <c>manifest/markers.json</c>, the list of marker attributes Blazix renders that
/// base-ui has no counterpart for, each with the reason it exists.
/// </summary>
/// <remarks>
/// A neutral home rather than a member of either comparator that reads it.
/// <see cref="Diff.MarkerComparator"/> classifies a listed name and
/// <see cref="Diff.AttributeComparator"/> cedes one the candidate alone carries, and
/// neither may depend on the other:
/// a comparator has to be reviewable, testable, and rejectable on its own. It sits here
/// rather than beside them for the same reason <see cref="FixtureManifest"/> and
/// <see cref="AliasTable"/> do — it is a manifest reader, and reading a manifest is not
/// a comparison.
/// </remarks>
public static class MarkerCatalog
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        AllowDuplicateProperties = false,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    /// <summary>
    /// Loads the catalogue.
    /// </summary>
    /// <returns>
    /// Each listed attribute name, spelled as a capture holds it, mapped to the reason it
    /// is Blazor-only.
    /// </returns>
    public static IReadOnlyDictionary<string, string> Load()
    {
        var path = Path.Combine(ParityPaths.Manifest, "markers.json");
        var json = File.ReadAllText(path);
        var manifest = JsonSerializer.Deserialize<MarkerManifest>(json, SerializerOptions)
            ?? throw new FormatException($"'{path}' must contain a marker manifest object.");
        return manifest.BlazorOnly;
    }

    private sealed record MarkerManifest
    {
        [JsonPropertyName("blazorOnly")]
        public Dictionary<string, string> BlazorOnly { get; init; } = [];
    }
}
