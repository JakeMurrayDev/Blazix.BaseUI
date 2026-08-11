using System.Text.Json;
using System.Text.Json.Serialization;
using Blazix.BaseUI.Parity.Tests.Infrastructure;

namespace Blazix.BaseUI.Parity.Tests.Capture;

/// <summary>The terminal execution status of one manifest action.</summary>
[JsonConverter(typeof(ActionExecutionStatusJsonConverter))]
public enum ActionExecutionStatus
{
    /// <summary>The action dispatched and its completion contract succeeded.</summary>
    Completed,

    /// <summary>The action selector matched no element.</summary>
    Unresolved,

    /// <summary>The action selector matched an element that could not be driven.</summary>
    NonActionable,

    /// <summary>The action dispatched but its completion contract missed its deadline.</summary>
    CompletionUnmet,

    /// <summary>The action was not attempted because an earlier action failed.</summary>
    Skipped
}

/// <summary>One canonical, typed manifest-action execution row.</summary>
public sealed record ActionExecution
{
    /// <summary>Gets the zero-based manifest action index.</summary>
    [JsonPropertyName("actionIndex")]
    [JsonPropertyOrder(0)]
    public required int ActionIndex { get; init; }

    /// <summary>Gets the canonical lowercase manifest action verb.</summary>
    [JsonPropertyName("verb")]
    [JsonPropertyOrder(1)]
    public required string Verb { get; init; }

    /// <summary>Gets the expanded action target, or <see langword="null"/> for a key action.</summary>
    [JsonPropertyName("expandedSelector")]
    [JsonPropertyOrder(2)]
    public string? ExpandedSelector { get; init; }

    /// <summary>Gets the action's terminal execution status.</summary>
    [JsonPropertyName("status")]
    [JsonPropertyOrder(3)]
    public required ActionExecutionStatus Status { get; init; }
}

/// <summary>Reads and writes only the exact string vocabulary of action statuses.</summary>
public sealed class ActionExecutionStatusJsonConverter : JsonConverter<ActionExecutionStatus>
{
    public override ActionExecutionStatus Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Action execution status must be a JSON string.");
        }

        return reader.GetString() switch
        {
            nameof(ActionExecutionStatus.Completed) => ActionExecutionStatus.Completed,
            nameof(ActionExecutionStatus.Unresolved) => ActionExecutionStatus.Unresolved,
            nameof(ActionExecutionStatus.NonActionable) => ActionExecutionStatus.NonActionable,
            nameof(ActionExecutionStatus.CompletionUnmet) => ActionExecutionStatus.CompletionUnmet,
            nameof(ActionExecutionStatus.Skipped) => ActionExecutionStatus.Skipped,
            var value => throw new JsonException(
                $"Unknown action execution status '{value ?? "null"}'.")
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        ActionExecutionStatus value,
        JsonSerializerOptions options)
    {
        if (!Enum.IsDefined(value))
        {
            throw new JsonException($"Unknown action execution status value '{(int)value}'.");
        }

        writer.WriteStringValue(value.ToString());
    }
}

internal static class ActionExecutionContract
{
    internal static ActionExecution Expected(
        AliasTable aliases,
        string component,
        StepAction action,
        int actionIndex,
        ActionExecutionStatus status)
        => new()
        {
            ActionIndex = actionIndex,
            Verb = Verb(action),
            ExpandedSelector = Selector(action) is { } selector
                ? aliases.Expand(component, selector)
                : null,
            Status = status
        };

    internal static string Verb(StepAction action) => action switch
    {
        { Click: not null } => "click",
        { Hover: not null } => "hover",
        { Key: not null } => "key",
        { Type: not null } => "type",
        { Focus: not null } => "focus",
        { Blur: not null } => "blur",
        { Scroll: not null } => "scroll",
        { Wait: not null } => "wait",
        _ => throw InvalidAction()
    };

    private static string? Selector(StepAction action) => action switch
    {
        { Click: { } selector } => selector,
        { Hover: { } selector } => selector,
        { Key: not null } => null,
        { Type: not null, Into: { } selector } => selector,
        { Focus: { } selector } => selector,
        { Blur: { } selector } => selector,
        { Scroll: { } selector } => selector,
        { Wait: { } selector } => selector,
        _ => throw InvalidAction()
    };

    private static FormatException InvalidAction()
        => new(
            "A manifest step action must carry exactly one of click, hover, key, focus, " +
            "blur, scroll, wait, or type with into.");
}
