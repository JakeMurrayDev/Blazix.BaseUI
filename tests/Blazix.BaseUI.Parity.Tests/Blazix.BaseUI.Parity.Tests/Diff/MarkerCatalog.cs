using System.Text.Json;
using System.Text.Json.Serialization;
using Blazix.BaseUI.Parity.Tests.Infrastructure;

namespace Blazix.BaseUI.Parity.Tests.Diff;

/// <summary>
/// Reads <c>manifest/markers.json</c>, the list of marker attributes Blazix renders that
/// base-ui has no counterpart for, each with the reason it exists.
/// </summary>
/// <remarks>
/// A neutral home rather than a member of either comparator that reads it.
/// <see cref="MarkerComparator"/> classifies a listed name and
/// <see cref="AttributeComparator"/> skips one, and neither may depend on the other:
/// a comparator has to be reviewable, testable, and rejectable on its own.
/// </remarks>
public static class MarkerCatalog
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
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
        return JsonSerializer.Deserialize<MarkerManifest>(json, SerializerOptions)!.BlazorOnly;
    }

    private sealed record MarkerManifest
    {
        [JsonPropertyName("blazorOnly")]
        public Dictionary<string, string> BlazorOnly { get; init; } = [];
    }
}
