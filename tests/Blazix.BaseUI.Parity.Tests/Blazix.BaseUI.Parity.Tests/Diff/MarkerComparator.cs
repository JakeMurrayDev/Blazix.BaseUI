using System.Text.Json;
using System.Text.Json.Serialization;
using Blazix.BaseUI.Parity.Tests.Infrastructure;

namespace Blazix.BaseUI.Parity.Tests.Diff;

/// <summary>
/// Classifies the Blazix marker attributes that survive capture normalization.
/// </summary>
/// <remarks>
/// <c>capture.js</c> renames every <c>data-blazix-base-ui-*</c> attribute to its
/// upstream <c>data-base-ui-*</c> spelling, so a name still carrying the Blazix prefix
/// is one base-ui has no counterpart for. Each such name has to be listed in
/// <c>manifest/markers.json</c> with a reason; an unlisted one fails the run rather
/// than being quietly tolerated.
/// </remarks>
public sealed class MarkerComparator : IComparator
{
    /// <summary>The prefix an unnormalized Blazix marker attribute carries.</summary>
    public const string MarkerPrefix = "data-blazix-";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly IReadOnlyDictionary<string, string> BlazorOnly = LoadBlazorOnly();

    /// <inheritdoc />
    public FindingKind Kind => FindingKind.Marker;

    /// <inheritdoc />
    public IEnumerable<Finding> Compare(ComparisonContext context)
    {
        foreach (var node in context.Candidate.Dom.Descendants())
        {
            var markers = node.Attributes.Keys
                .Where(name => name.StartsWith(MarkerPrefix, StringComparison.Ordinal))
                .OrderBy(name => name, StringComparer.Ordinal);

            foreach (var name in markers)
            {
                var listed = BlazorOnly.TryGetValue(name, out var reason);

                yield return new Finding
                {
                    Fixture = context.Fixture,
                    Leg = context.Leg,
                    Step = context.Step,
                    Kind = FindingKind.Marker,
                    Severity = listed ? Severity.Info : Severity.Error,
                    NodePath = node.Path,
                    Property = name,
                    CandidateValue = node.Attributes[name],
                    Message = listed
                        ? $"Blazor-only marker '{name}' at '{node.Path}'. {reason}"
                        : $"Unclassified Blazix marker '{name}'. Add it to " +
                          "manifest/markers.json with a reason, or rename it to its " +
                          "data-base-ui-* counterpart."
                };
            }
        }
    }

    private static IReadOnlyDictionary<string, string> LoadBlazorOnly()
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
