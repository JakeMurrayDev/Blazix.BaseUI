using System.Text.Json;
using System.Text.Json.Serialization;
using Blazix.BaseUI.Parity.Tests.Client;

namespace Blazix.BaseUI.Parity.Tests.Infrastructure;

/// <summary>
/// One browser-observable consequence that must hold before the next action starts.
/// </summary>
/// <remarks>
/// The strict JSON vocabulary is one of:
/// <c>{ "selector": "@popup", "state": "visible" }</c>,
/// <c>{ "selector": "@trigger", "attribute": "aria-expanded", "equals": "true" }</c>,
/// <c>{ "selector": "input", "property": "checked", "equals": "true" }</c>,
/// <c>{ "selector": "@input", "inputValue": "text" }</c>, or
/// <c>{ "selector": "@input", "focus": "equals|not-equals" }</c>.
/// Exactly one predicate discriminator is legal in each object.
/// </remarks>
public sealed record CompletionPredicate
{
    /// <summary>Gets the alias-backed or raw selector the predicate observes.</summary>
    [JsonPropertyName("selector")]
    public string? Selector { get; init; }

    /// <summary>Gets the required selector state: attached, detached, visible, or hidden.</summary>
    [JsonPropertyName("state")]
    public string? State { get; init; }

    /// <summary>Gets the DOM attribute whose string value must equal <see cref="Expected"/>.</summary>
    [JsonPropertyName("attribute")]
    public string? Attribute { get; init; }

    /// <summary>
    /// Gets the DOM property whose value must equal <see cref="Expected"/>; boolean and invariant
    /// numeric spellings are compared as their corresponding JavaScript primitive types.
    /// </summary>
    [JsonPropertyName("property")]
    public string? Property { get; init; }

    /// <summary>Gets the expected attribute or property value.</summary>
    [JsonPropertyName("equals")]
    public string? Expected { get; init; }

    /// <summary>Gets the exact value an input, textarea, or select must expose.</summary>
    [JsonPropertyName("inputValue")]
    public string? InputValue { get; init; }

    /// <summary>Gets whether focus must equal or not equal the selected element.</summary>
    [JsonPropertyName("focus")]
    public string? Focus { get; init; }
}

/// <summary>An intentional action with no stronger browser-observable consequence.</summary>
public sealed record ActionOnlyEntry
{
    /// <summary>Gets why this action legitimately has no completion predicate.</summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; init; }
}

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

    /// <summary>Gets the non-empty all-of completion predicate list.</summary>
    [JsonPropertyName("complete")]
    public IReadOnlyList<CompletionPredicate>? Complete { get; init; }

    /// <summary>Gets the reasoned declaration for an intentional action-only operation.</summary>
    [JsonPropertyName("actionOnly")]
    public ActionOnlyEntry? ActionOnly { get; init; }
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
        AllowDuplicateProperties = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    /// <summary>
    /// Loads every fixture entry.
    /// </summary>
    /// <returns>The manifest entries, in file order.</returns>
    public static IReadOnlyList<FixtureEntry> Load()
    {
        var path = Path.Combine(ParityPaths.Manifest, "fixtures.json");
        var json = File.ReadAllText(path);
        var entries = Parse(json);
        ReconcileRegistry(entries);
        MilestoneFixtureCatalog.Reconcile(
            entries.Select(entry => entry.Id),
            FixtureRegistry.Ids);
        return entries;
    }

    /// <summary>Parses and validates manifest JSON.</summary>
    /// <param name="json">The complete manifest document.</param>
    /// <returns>The validated entries, in document order.</returns>
    internal static IReadOnlyList<FixtureEntry> Parse(string json)
    {
        var entries = JsonSerializer.Deserialize<List<FixtureEntry>>(json, SerializerOptions)
            ?? throw new FormatException("The fixture manifest must contain a JSON array.");
        var fixtureIds = new HashSet<string>(StringComparer.Ordinal);

        for (var entryIndex = 0; entryIndex < entries.Count; entryIndex++)
        {
            var entry = entries[entryIndex];

            if (entry is null)
            {
                throw new FormatException(
                    $"Manifest entry {entryIndex}: entry must be an object.");
            }

            if (entry.Steps is null)
            {
                throw new FormatException(
                    $"Manifest entry {entryIndex} ('{entry.Id}'): steps must be an array.");
            }

            if (!MilestoneFixtureCatalog.IsFixtureId(entry.Id))
            {
                throw new FormatException(
                    $"Manifest entry {entryIndex}: id must be a safe lowercase fixture id.");
            }

            if (!fixtureIds.Add(entry.Id))
            {
                throw new FormatException(
                    $"Manifest entry {entryIndex} ('{entry.Id}'): duplicate fixture id.");
            }

            var separatorIndex = entry.Id.IndexOf('/');
            var component = entry.Id[..separatorIndex];
            var demo = entry.Id[(separatorIndex + 1)..];
            if (!string.Equals(entry.Component, component, StringComparison.Ordinal))
            {
                throw new FormatException(
                    $"Manifest entry {entryIndex} ('{entry.Id}'): component must be exactly " +
                    $"'{component}'.");
            }

            var expectedReact = $"{component}/demos/{demo}/tailwind/index.tsx";
            if (!string.Equals(entry.React, expectedReact, StringComparison.Ordinal))
            {
                throw new FormatException(
                    $"Manifest entry {entryIndex} ('{entry.Id}'): react must be exactly " +
                    $"'{expectedReact}'.");
            }

            var expectedBlazor = $"{ToPascalToken(component)}/{ToPascalToken(demo)}";
            if (!string.Equals(entry.Blazor, expectedBlazor, StringComparison.Ordinal))
            {
                throw new FormatException(
                    $"Manifest entry {entryIndex} ('{entry.Id}'): blazor must be exactly " +
                    $"'{expectedBlazor}'.");
            }

            if (entry.Themes is null)
            {
                throw new FormatException(
                    $"Manifest entry {entryIndex} ('{entry.Id}'): themes must be an array.");
            }

            if (entry.Themes.Count == 0)
            {
                throw new FormatException(
                    $"Manifest entry {entryIndex} ('{entry.Id}'): themes must contain at least one theme.");
            }

            var themes = new HashSet<string>(StringComparer.Ordinal);
            for (var themeIndex = 0; themeIndex < entry.Themes.Count; themeIndex++)
            {
                var theme = entry.Themes[themeIndex];
                if (theme is not ("light" or "dark"))
                {
                    throw new FormatException(
                        $"Manifest entry {entryIndex} ('{entry.Id}'), theme {themeIndex}: " +
                        "theme must be exactly light or dark.");
                }

                if (!themes.Add(theme))
                {
                    throw new FormatException(
                        $"Manifest entry {entryIndex} ('{entry.Id}'), theme {themeIndex}: " +
                        $"duplicate theme '{theme}'.");
                }
            }

            var stepNames = new HashSet<string>(StringComparer.Ordinal);
            for (var stepIndex = 0; stepIndex < entry.Steps.Count; stepIndex++)
            {
                var step = entry.Steps[stepIndex];

                if (step is null)
                {
                    throw new FormatException(
                        $"Manifest entry {entryIndex} ('{entry.Id}'), step {stepIndex}: " +
                        "step must be an object.");
                }

                if (string.IsNullOrWhiteSpace(step.Name))
                {
                    throw new FormatException(
                        $"Manifest entry {entryIndex} ('{entry.Id}'), step {stepIndex}: " +
                        "step token must not be blank.");
                }

                if (!IsSafeToken(step.Name))
                {
                    throw new FormatException(
                        $"Manifest entry {entryIndex} ('{entry.Id}'), step {stepIndex} " +
                        $"('{step.Name}'): name must be a safe lowercase step token.");
                }

                if (!stepNames.Add(step.Name))
                {
                    throw new FormatException(
                        $"Manifest entry {entryIndex} ('{entry.Id}'), step {stepIndex}: " +
                        $"duplicate step token '{step.Name}'.");
                }

                if (step.Do is null)
                {
                    throw new FormatException(
                        $"Manifest entry {entryIndex} ('{entry.Id}'), step {stepIndex} " +
                        $"('{step.Name}'): do must be an array.");
                }

                for (var actionIndex = 0; actionIndex < step.Do.Count; actionIndex++)
                {
                    var action = step.Do[actionIndex];

                    if (action is null)
                    {
                        throw new FormatException(
                            $"Manifest entry {entryIndex} ('{entry.Id}'), step {stepIndex} " +
                            $"('{step.Name}'), action {actionIndex}: action must be an object.");
                    }

                    ValidateAction(entry, entryIndex, step, stepIndex, action, actionIndex);
                }
            }
        }

        return entries;
    }

    /// <summary>Verifies exact declared Blazor paths against the reflection registry.</summary>
    /// <param name="entries">Parsed fixture entries.</param>
    internal static void ReconcileRegistry(IEnumerable<FixtureEntry> entries)
    {
        var prefix = $"{typeof(FixtureRegistry).Namespace}.Fixtures.";

        foreach (var entry in entries)
        {
            var separatorIndex = entry.Id.IndexOf('/');
            var component = entry.Id[..separatorIndex];
            var demo = entry.Id[(separatorIndex + 1)..];
            var type = FixtureRegistry.Resolve(component, demo)
                ?? throw new InvalidOperationException(
                    $"Manifest fixture '{entry.Id}' has no registered Blazor port.");
            var fullName = type.FullName;
            if (fullName is null || !fullName.StartsWith(prefix, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Registered Blazor port for '{entry.Id}' has an invalid type name.");
            }

            var registeredPath = fullName[prefix.Length..].Replace('.', '/');
            if (!string.Equals(entry.Blazor, registeredPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Manifest fixture '{entry.Id}' declares Blazor port '{entry.Blazor}', " +
                    $"but the registry resolves it to '{registeredPath}'.");
            }
        }
    }

    private static void ValidateAction(
        FixtureEntry entry,
        int entryIndex,
        StepEntry step,
        int stepIndex,
        StepAction action,
        int actionIndex)
    {
        var coordinates =
            $"Manifest entry {entryIndex} ('{entry.Id}'), step {stepIndex} ('{step.Name}'), " +
            $"action {actionIndex}";

        var verbs = new[]
        {
            action.Click,
            action.Hover,
            action.Key,
            action.Type,
            action.Focus,
            action.Blur,
            action.Scroll,
            action.Wait
        }.Count(value => value is not null);

        if (verbs != 1 || (action.Type is null) != (action.Into is null))
        {
            throw new FormatException(
                $"{coordinates}: exactly one action verb is required; type also requires into.");
        }

        if (action.Type is null && ActionSelector(action) is { } selector &&
            string.IsNullOrWhiteSpace(selector))
        {
            throw new FormatException($"{coordinates}: the action selector or key must not be blank.");
        }

        if (action.Type is not null && string.IsNullOrWhiteSpace(action.Into))
        {
            throw new FormatException($"{coordinates}: type requires a non-blank into selector.");
        }

        if ((action.Complete is null) == (action.ActionOnly is null))
        {
            throw new FormatException(
                $"{coordinates}: exactly one completion contract is required: complete or actionOnly.");
        }

        if (action.ActionOnly is not null)
        {
            if (string.IsNullOrWhiteSpace(action.ActionOnly.Reason))
            {
                throw new FormatException($"{coordinates}: actionOnly.reason must not be blank.");
            }

            return;
        }

        if (action.Complete!.Count == 0)
        {
            throw new FormatException($"{coordinates}: complete must contain at least one predicate.");
        }

        for (var predicateIndex = 0; predicateIndex < action.Complete.Count; predicateIndex++)
        {
            var predicate = action.Complete[predicateIndex];

            if (predicate is null)
            {
                throw new FormatException(
                    $"{coordinates}, completion predicate {predicateIndex}: predicate must be an object.");
            }

            ValidatePredicate(predicate, coordinates, predicateIndex);
        }
    }

    private static void ValidatePredicate(
        CompletionPredicate predicate,
        string coordinates,
        int predicateIndex)
    {
        var location = $"{coordinates}, completion predicate {predicateIndex}";

        if (string.IsNullOrWhiteSpace(predicate.Selector))
        {
            throw new FormatException($"{location}: selector must not be blank.");
        }

        var discriminators = new[]
        {
            predicate.State,
            predicate.Attribute,
            predicate.Property,
            predicate.InputValue,
            predicate.Focus
        }.Count(value => value is not null);

        if (discriminators != 1)
        {
            throw new FormatException($"{location}: exactly one predicate discriminator is required.");
        }

        if (predicate.State is not null)
        {
            if (predicate.State is not ("attached" or "detached" or "visible" or "hidden"))
            {
                throw new FormatException(
                    $"{location}: state must be attached, detached, visible, or hidden.");
            }

            RejectEquals(predicate, location);
            return;
        }

        if (predicate.Attribute is not null || predicate.Property is not null)
        {
            var name = predicate.Attribute ?? predicate.Property;

            if (string.IsNullOrWhiteSpace(name) || predicate.Expected is null)
            {
                throw new FormatException(
                    $"{location}: attribute and property predicates require a non-blank name and equals value.");
            }

            return;
        }

        if (predicate.InputValue is not null)
        {
            RejectEquals(predicate, location);
            return;
        }

        if (predicate.Focus is not ("equals" or "not-equals"))
        {
            throw new FormatException($"{location}: focus must be equals or not-equals.");
        }

        RejectEquals(predicate, location);
    }

    private static string? ActionSelector(StepAction action) => action switch
    {
        { Click: { } value } => value,
        { Hover: { } value } => value,
        { Key: { } value } => value,
        { Focus: { } value } => value,
        { Blur: { } value } => value,
        { Scroll: { } value } => value,
        { Wait: { } value } => value,
        _ => null
    };

    private static bool IsSafeToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) ||
            token[0] is not (>= 'a' and <= 'z') ||
            token[^1] is not (>= 'a' and <= 'z' or >= '0' and <= '9') ||
            token.Contains("--", StringComparison.Ordinal))
        {
            return false;
        }

        return token.All(character =>
            character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');
    }

    private static string ToPascalToken(string token)
        => string.Concat(token.Split('-').Select(segment =>
            char.ToUpperInvariant(segment[0]) + segment[1..]));

    private static void RejectEquals(CompletionPredicate predicate, string location)
    {
        if (predicate.Expected is not null)
        {
            throw new FormatException(
                $"{location}: equals is legal only with attribute or property predicates.");
        }
    }
}
