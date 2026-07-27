using System.Text.Json;
using System.Text.Json.Serialization;

namespace Blazix.BaseUI.Parity.Tests.Infrastructure;

/// <summary>
/// One interaction inside a manifest step.
/// </summary>
/// <remarks>
/// Each entry carries exactly one verb, spelled as the JSON key, whose value is the
/// selector it applies to — <c>{ "click": "@trigger" }</c>. <c>key</c> is the exception:
/// its value is a key name and it is dispatched to the page rather than to an element.
/// <c>type</c> takes the text and names its target with <c>into</c>.
/// </remarks>
public sealed record StepAction
{
    /// <summary>Gets the selector to click.</summary>
    [JsonPropertyName("click")]
    public string? Click { get; init; }

    /// <summary>Gets the selector to hover.</summary>
    [JsonPropertyName("hover")]
    public string? Hover { get; init; }

    /// <summary>Gets the key to press, for example <c>Escape</c>.</summary>
    [JsonPropertyName("key")]
    public string? Key { get; init; }

    /// <summary>Gets the text to type into <see cref="Into"/>.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    /// <summary>Gets the selector <see cref="Type"/> types into.</summary>
    [JsonPropertyName("into")]
    public string? Into { get; init; }

    /// <summary>Gets the selector to focus.</summary>
    [JsonPropertyName("focus")]
    public string? Focus { get; init; }

    /// <summary>Gets the selector to blur.</summary>
    [JsonPropertyName("blur")]
    public string? Blur { get; init; }

    /// <summary>Gets the selector to scroll into view.</summary>
    [JsonPropertyName("scroll")]
    public string? Scroll { get; init; }

    /// <summary>Gets a selector to wait for, without otherwise interacting with it.</summary>
    [JsonPropertyName("wait")]
    public string? Wait { get; init; }
}

/// <summary>
/// One capture point in a fixture's interaction sequence.
/// </summary>
public sealed record StepEntry
{
    /// <summary>Gets the step name, which keys the capture and every finding raised against it.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>Gets the actions performed before this step is captured.</summary>
    [JsonPropertyName("do")]
    public IReadOnlyList<StepAction> Do { get; init; } = [];

    /// <summary>
    /// Gets the settle mode: <c>render</c> (quiescence only) or <c>animation</c>
    /// (quiescence plus a recorded timeline).
    /// </summary>
    [JsonPropertyName("settle")]
    public string Settle { get; init; } = "render";
}

/// <summary>
/// One fixture: a base-ui demo paired with its Blazor port, plus how to drive both.
/// </summary>
public sealed record FixtureEntry
{
    /// <summary>Gets the fixture id, for example <c>select/grouped</c>.</summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Gets the component segment of <see cref="Id"/>, which also keys
    /// <c>manifest/aliases.json</c>.
    /// </summary>
    [JsonPropertyName("component")]
    public required string Component { get; init; }

    /// <summary>Gets the demo's path within the base-ui checkout.</summary>
    [JsonPropertyName("react")]
    public required string React { get; init; }

    /// <summary>Gets the fixture component's path under <c>Client/Fixtures</c>.</summary>
    [JsonPropertyName("blazor")]
    public required string Blazor { get; init; }

    /// <summary>Gets the colour schemes this fixture is captured under.</summary>
    [JsonPropertyName("themes")]
    public IReadOnlyList<string> Themes { get; init; } = ["light"];

    /// <summary>Gets the fraction of pixels allowed to differ before a screenshot fails.</summary>
    [JsonPropertyName("pixelThreshold")]
    public double PixelThreshold { get; init; } = 0.001;

    /// <summary>Gets the steps to capture, defaulting to the initial render alone.</summary>
    [JsonPropertyName("steps")]
    public IReadOnlyList<StepEntry> Steps { get; init; } = [new StepEntry { Name = "initial" }];

    /// <summary>
    /// Gets the demo segment of <see cref="Id"/>, which is the second segment of the
    /// Blazor fixture route.
    /// </summary>
    [JsonIgnore]
    public string Demo => Id[(Id.IndexOf('/') + 1)..];
}

/// <summary>
/// Reads <c>manifest/fixtures.json</c>.
/// </summary>
public static class FixtureManifest
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Loads every fixture entry.
    /// </summary>
    /// <returns>The manifest entries, in file order.</returns>
    public static IReadOnlyList<FixtureEntry> Load()
    {
        var path = Path.Combine(ParityPaths.Manifest, "fixtures.json");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<FixtureEntry>>(json, SerializerOptions)!;
    }
}
