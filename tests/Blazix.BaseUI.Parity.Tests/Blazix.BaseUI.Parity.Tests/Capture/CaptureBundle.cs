using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Parity.Tests.Capture;

/// <summary>Identifies which side of the comparison a capture came from.</summary>
public enum ParityLeg
{
    /// <summary>The base-ui React bundle.</summary>
    React,

    /// <summary>Blazor Interactive Server.</summary>
    BlazorServer,

    /// <summary>Blazor Interactive WebAssembly.</summary>
    BlazorWasm
}

/// <summary>A single node in a normalized DOM snapshot.</summary>
public sealed record DomNode
{
    /// <summary>Gets the lowercase tag name.</summary>
    [JsonPropertyName("tag")]
    public required string Tag { get; init; }

    /// <summary>Gets the stable path identifying this node within its root.</summary>
    [JsonPropertyName("path")]
    public required string Path { get; init; }

    /// <summary>Gets the normalized attributes, excluding <c>class</c> and <c>style</c>.</summary>
    [JsonPropertyName("attributes")]
    public required IReadOnlyDictionary<string, string> Attributes { get; init; }

    /// <summary>Gets the sorted class list, reported as informational only.</summary>
    [JsonPropertyName("classes")]
    public required IReadOnlyList<string> Classes { get; init; }

    /// <summary>Gets the whitespace-normalized direct text content.</summary>
    [JsonPropertyName("text")]
    public required string Text { get; init; }

    /// <summary>Gets the child nodes.</summary>
    [JsonPropertyName("children")]
    public required IReadOnlyList<DomNode> Children { get; init; }

    /// <summary>Enumerates this node and every descendant, depth first.</summary>
    /// <returns>The node sequence.</returns>
    public IEnumerable<DomNode> Descendants()
    {
        yield return this;

        foreach (var child in Children)
        {
            foreach (var node in child.Descendants())
            {
                yield return node;
            }
        }
    }
}

/// <summary>A single recorded animation or mutation event.</summary>
public sealed record TimelineEvent
{
    /// <summary>Gets the milliseconds elapsed since the trigger action.</summary>
    [JsonPropertyName("t")]
    public required int T { get; init; }

    /// <summary>Gets the event kind, for example <c>attribute</c> or <c>transitionend</c>.</summary>
    [JsonPropertyName("kind")]
    public required string Kind { get; init; }

    /// <summary>Gets the node path the event applies to.</summary>
    [JsonPropertyName("path")]
    public required string Path { get; init; }

    /// <summary>Gets the attribute, property, or animation name, when applicable.</summary>
    [JsonPropertyName("attr")]
    public string? Attr { get; init; }

    /// <summary>Gets the previous value.</summary>
    [JsonPropertyName("from")]
    public string? From { get; init; }

    /// <summary>Gets the new value.</summary>
    [JsonPropertyName("to")]
    public string? To { get; init; }
}

/// <summary>Everything captured for one manifest step on one leg.</summary>
public sealed record StepCapture
{
    /// <summary>Gets the manifest step name.</summary>
    [JsonPropertyName("step")]
    public required string Step { get; init; }

    /// <summary>Gets the normalized DOM snapshot.</summary>
    [JsonPropertyName("dom")]
    public required DomNode Dom { get; init; }

    /// <summary>Gets the allowlisted computed styles, keyed by node path.</summary>
    [JsonPropertyName("styles")]
    public required IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Styles { get; init; }

    /// <summary>Gets the CSS custom properties, keyed by node path.</summary>
    [JsonPropertyName("customProps")]
    public required IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> CustomProps { get; init; }

    /// <summary>Gets the bounding rectangles, keyed by node path.</summary>
    [JsonPropertyName("geometry")]
    public required IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>> Geometry { get; init; }

    /// <summary>Gets the node path of the focused element, or <see langword="null"/>.</summary>
    [JsonPropertyName("focus")]
    public string? Focus { get; init; }

    /// <summary>Gets the recorded animation timeline.</summary>
    [JsonPropertyName("timeline")]
    public IReadOnlyList<TimelineEvent> Timeline { get; init; } = [];

    /// <summary>Gets the ARIA snapshot, filled in by the capturer rather than the script.</summary>
    [JsonPropertyName("aria")]
    public string Aria { get; init; } = string.Empty;

    /// <summary>Gets the console messages observed during this step.</summary>
    [JsonPropertyName("console")]
    public IReadOnlyList<string> Console { get; init; } = [];

    /// <summary>Gets the screenshot file names produced for this step.</summary>
    [JsonPropertyName("screenshots")]
    public IReadOnlyList<string> Screenshots { get; init; } = [];

    /// <summary>
    /// Gets the expanded step selectors that matched nothing on this leg.
    /// </summary>
    /// <remarks>
    /// Steps address elements through role-based aliases because roles are the one
    /// contract both implementations must honour, so a selector that resolves on one leg
    /// and not the other is a parity result rather than a harness failure. The capturer
    /// records it here and skips the action instead of throwing; comparing the two legs'
    /// lists is what turns it into a finding.
    /// </remarks>
    [JsonPropertyName("unresolvedSelectors")]
    public IReadOnlyList<string> UnresolvedSelectors { get; init; } = [];

    /// <summary>
    /// Gets the expanded step selectors that resolved to an element the action could not
    /// be driven against on this leg.
    /// </summary>
    /// <remarks>
    /// The element is present and not driveable — zero-size, covered, or
    /// <c>pointer-events: none</c> — which is a different parity result from a selector
    /// that matched nothing, and is kept out of
    /// <see cref="UnresolvedSelectors"/> for that reason. Folding the two together also
    /// lets them cancel: two legs whose elements are non-actionable for unrelated reasons
    /// would report identical lists and no difference at all. The capturer records the
    /// selector here and skips the action instead of throwing.
    /// </remarks>
    [JsonPropertyName("nonActionableSelectors")]
    public IReadOnlyList<string> NonActionableSelectors { get; init; } = [];
}

/// <summary>All steps captured for one fixture on one leg.</summary>
public sealed record CaptureBundle
{
    /// <summary>Gets the fixture id, for example <c>select/grouped</c>.</summary>
    [JsonPropertyName("fixture")]
    public required string Fixture { get; init; }

    /// <summary>Gets the leg this bundle was captured from.</summary>
    [JsonPropertyName("leg")]
    public required ParityLeg Leg { get; init; }

    /// <summary>Gets the base-ui commit the React demo sources came from.</summary>
    [JsonPropertyName("baseUiSha")]
    public string BaseUiSha { get; init; } = string.Empty;

    /// <summary>Gets the content hash of the React demo sources.</summary>
    [JsonPropertyName("sourceHash")]
    public string SourceHash { get; init; } = string.Empty;

    /// <summary>Gets the captured steps.</summary>
    [JsonPropertyName("steps")]
    public required IReadOnlyList<StepCapture> Steps { get; init; }
}

/// <summary>Injects and invokes the shared capture script.</summary>
public static class CaptureScript
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Injects the shared capture script so it runs before any page script.
    /// </summary>
    /// <param name="page">The page to inject into.</param>
    /// <returns>A task that completes when the script is registered.</returns>
    public static Task InjectAsync(IPage page)
        => page.AddInitScriptAsync(scriptPath: Infrastructure.ParityPaths.SharedScript);

    /// <summary>
    /// Captures the current page state for the supplied step.
    /// </summary>
    /// <param name="page">The page to capture.</param>
    /// <param name="step">The manifest step name.</param>
    /// <returns>The captured step.</returns>
    public static async Task<StepCapture> CaptureAsync(IPage page, string step)
    {
        var json = await page.EvaluateAsync<JsonElement>(
            "s => JSON.stringify(window[Symbol.for('Blazix.Parity.Capture')].capture(s))", step);

        return JsonSerializer.Deserialize<StepCapture>(json.GetString()!, SerializerOptions)!;
    }
}
