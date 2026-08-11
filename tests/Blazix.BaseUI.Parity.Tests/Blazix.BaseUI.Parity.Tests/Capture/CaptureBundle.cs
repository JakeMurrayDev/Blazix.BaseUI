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

/// <summary>Describes what was observed for one capture root at one shot.</summary>
[JsonConverter(typeof(ScreenshotObservationStateJsonConverter))]
public enum ScreenshotObservationState
{
    /// <summary>The root had photographable pixels and its PNG was written.</summary>
    Captured,

    /// <summary>The root was present but had no photographable viewport pixels.</summary>
    NotVisible,

    /// <summary>The root had photographable pixels but its PNG could not be written.</summary>
    CaptureFailed
}

/// <summary>Reads and writes only the exact screenshot-observation state vocabulary.</summary>
public sealed class ScreenshotObservationStateJsonConverter
    : JsonConverter<ScreenshotObservationState>
{
    /// <inheritdoc />
    public override ScreenshotObservationState Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Screenshot observation state must be a JSON string.");
        }

        return reader.GetString() switch
        {
            nameof(ScreenshotObservationState.Captured) => ScreenshotObservationState.Captured,
            nameof(ScreenshotObservationState.NotVisible) => ScreenshotObservationState.NotVisible,
            nameof(ScreenshotObservationState.CaptureFailed) => ScreenshotObservationState.CaptureFailed,
            var value => throw new JsonException(
                $"Unknown screenshot observation state '{value ?? "null"}'.")
        };
    }

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        ScreenshotObservationState value,
        JsonSerializerOptions options)
    {
        if (!Enum.IsDefined(value))
        {
            throw new JsonException($"Unknown screenshot observation state value '{(int)value}'.");
        }

        writer.WriteStringValue(value.ToString());
    }
}

/// <summary>Canonical screenshot evidence for one shared capture-script root.</summary>
public sealed record ScreenshotObservation
{
    /// <summary>Gets the label assigned by <c>shared/capture.js</c>.</summary>
    [JsonPropertyName("rootLabel")]
    [JsonRequired]
    public required string RootLabel { get; init; }

    /// <summary>Gets the stable shot id, for example <c>00</c> or <c>frame025.01</c>.</summary>
    [JsonPropertyName("shot")]
    [JsonRequired]
    public required string Shot { get; init; }

    /// <summary>Gets the observed capture state.</summary>
    [JsonPropertyName("state")]
    [JsonRequired]
    public required ScreenshotObservationState State { get; init; }

    /// <summary>Gets the PNG file name when <see cref="State"/> is <see cref="ScreenshotObservationState.Captured"/>.</summary>
    [JsonPropertyName("fileName")]
    public string? FileName { get; init; }

    /// <summary>Gets a bounded diagnostic when capture failed.</summary>
    [JsonPropertyName("detail")]
    public string? Detail { get; init; }

    /// <summary>Creates a successfully captured observation.</summary>
    public static ScreenshotObservation Captured(string rootLabel, string shot, string fileName)
        => new() { RootLabel = rootLabel, Shot = shot, State = ScreenshotObservationState.Captured, FileName = fileName };

    /// <summary>Creates an explicit no-photographable-viewport-pixels observation.</summary>
    public static ScreenshotObservation NotVisible(string rootLabel, string shot)
        => new() { RootLabel = rootLabel, Shot = shot, State = ScreenshotObservationState.NotVisible };

    /// <summary>Creates an observation for a photographable root whose capture failed.</summary>
    public static ScreenshotObservation CaptureFailed(string rootLabel, string shot, string detail)
        => new() { RootLabel = rootLabel, Shot = shot, State = ScreenshotObservationState.CaptureFailed, Detail = detail };
}

/// <summary>A typed failure from deterministic animation-frame replay.</summary>
public sealed record AnimationFrameCaptureFailure
{
    /// <summary>Gets the replay stage that failed.</summary>
    [JsonPropertyName("stage")]
    [JsonRequired]
    public required string Stage { get; init; }

    /// <summary>Gets the zero-based manifest action index, when the failure occurred during an action.</summary>
    [JsonPropertyName("actionIndex")]
    public int? ActionIndex { get; init; }

    /// <summary>Gets the primary diagnostic.</summary>
    [JsonPropertyName("detail")]
    [JsonRequired]
    public required string Detail { get; init; }

    /// <summary>Gets a cleanup diagnostic without replacing the primary failure.</summary>
    [JsonPropertyName("cleanupDetail")]
    public string? CleanupDetail { get; init; }
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
    private IReadOnlyList<string>? screenshots;

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

    /// <summary>Gets one explicit observation per shared capture root and shot.</summary>
    [JsonPropertyName("screenshotObservations")]
    [JsonRequired]
    public IReadOnlyList<ScreenshotObservation> ScreenshotObservations { get; init; } = [];

    /// <summary>Projects captured PNG names for in-process compatibility.</summary>
    [JsonIgnore]
    public IReadOnlyList<string> Screenshots
    {
        get => screenshots ??
            [.. ScreenshotObservations
                .Where(observation => observation.State == ScreenshotObservationState.Captured)
                .Select(observation => observation.FileName!)];
        init => screenshots = value;
    }

    /// <summary>Gets exactly one canonical execution row per manifest action.</summary>
    [JsonPropertyName("actions")]
    [JsonRequired]
    public IReadOnlyList<ActionExecution> Actions { get; init; } = [];

    /// <summary>Projects unresolved selector occurrences from <see cref="Actions"/>.</summary>
    [JsonIgnore]
    public IReadOnlyList<string> UnresolvedSelectors =>
    [
        .. Actions
            .Where(action => action.Status == ActionExecutionStatus.Unresolved)
            .Select(action => action.ExpandedSelector!)
    ];

    /// <summary>Projects non-actionable selector occurrences from <see cref="Actions"/>.</summary>
    [JsonIgnore]
    public IReadOnlyList<string> NonActionableSelectors =>
    [
        .. Actions
            .Where(action => action.Status == ActionExecutionStatus.NonActionable)
            .Select(action => action.ExpandedSelector!)
    ];

    /// <summary>Gets the declared action consequences that missed their deadlines.</summary>
    [JsonPropertyName("actionCompletionFailures")]
    public IReadOnlyList<ActionCompletionFailure> ActionCompletionFailures { get; init; } = [];

    /// <summary>Gets typed deterministic-frame replay failures without discarding canonical evidence.</summary>
    [JsonPropertyName("animationFrameCaptureFailures")]
    [JsonRequired]
    public IReadOnlyList<AnimationFrameCaptureFailure> AnimationFrameCaptureFailures { get; init; } = [];
}

/// <summary>All steps captured for one fixture on one leg.</summary>
public sealed record CaptureBundle
{
    /// <summary>Gets the capture document schema.</summary>
    [JsonPropertyName("captureSchemaVersion")]
    [JsonRequired]
    public int CaptureSchemaVersion { get; init; }

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

    /// <summary>Gets the color-scheme theme emulated while this bundle was captured.</summary>
    [JsonPropertyName("theme")]
    public string Theme { get; init; } = string.Empty;

    /// <summary>Gets the captured steps.</summary>
    [JsonPropertyName("steps")]
    public required IReadOnlyList<StepCapture> Steps { get; init; }
}

/// <summary>Injects and invokes the shared capture script.</summary>
public static class CaptureScript
{
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

        return CaptureSchema.DeserializeStep(json.GetString()!);
    }

    internal static Task<JsonElement> WaitForCompletionAsync(
        IPage page,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> predicates,
        float timeoutMs)
        => page.EvaluateAsync<JsonElement>(
            "args => window[Symbol.for('Blazix.Parity.Capture')].awaitCompletion(" +
            "args.predicates, args.timeoutMs)",
            new { predicates, timeoutMs = (double)timeoutMs });

    internal static Task<int> WaitForAnimationRegistrationAsync(
        IPage page,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> predicates,
        float timeoutMs)
        => page.EvaluateAsync<int>(
            "args => window[Symbol.for('Blazix.Parity.Capture')].awaitAnimationRegistration(" +
            "args.predicates, args.timeoutMs)",
            new { predicates, timeoutMs = (double)timeoutMs });

    internal static Task BeginAnimationProbeAsync(IPage page)
        => page.EvaluateAsync(
            "() => window[Symbol.for('Blazix.Parity.Capture')].beginAnimationProbe()");

    internal static Task<int> SelectCurrentAnimationsAsync(IPage page)
        => page.EvaluateAsync<int>(
            "() => window[Symbol.for('Blazix.Parity.Capture')].selectCurrentAnimations()");
}
