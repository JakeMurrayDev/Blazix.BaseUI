using System.Text.Json.Serialization;

namespace Blazix.BaseUI.Parity.Tests.Capture;

/// <summary>
/// A dispatched action whose declared browser-observable consequence missed its deadline.
/// </summary>
public sealed record ActionCompletionFailure
{
    /// <summary>Gets the fixture id.</summary>
    [JsonPropertyName("fixture")]
    public required string Fixture { get; init; }

    /// <summary>Gets the actual leg on which completion failed.</summary>
    [JsonPropertyName("leg")]
    public required ParityLeg Leg { get; init; }

    /// <summary>Gets the manifest step name.</summary>
    [JsonPropertyName("step")]
    public required string Step { get; init; }

    /// <summary>Gets the zero-based action index within the step.</summary>
    [JsonPropertyName("actionIndex")]
    public required int ActionIndex { get; init; }

    /// <summary>Gets the dispatched action verb.</summary>
    [JsonPropertyName("verb")]
    public required string Verb { get; init; }

    /// <summary>Gets the exact expanded selector observed by the unmet predicate.</summary>
    [JsonPropertyName("selector")]
    public required string Selector { get; init; }

    /// <summary>Gets the unmet predicate kind and property name, when applicable.</summary>
    [JsonPropertyName("predicate")]
    public required string Predicate { get; init; }

    /// <summary>Gets the expected state or value.</summary>
    [JsonPropertyName("expectedValue")]
    public required string ExpectedValue { get; init; }

    /// <summary>Gets a bounded snapshot of the state observed at the deadline.</summary>
    [JsonPropertyName("observed")]
    public required string Observed { get; init; }
}
