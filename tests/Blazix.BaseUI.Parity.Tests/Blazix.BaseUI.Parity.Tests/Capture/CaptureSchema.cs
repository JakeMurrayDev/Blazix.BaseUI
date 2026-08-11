using System.Text.Json;
using System.Text.Json.Serialization;

namespace Blazix.BaseUI.Parity.Tests.Capture;

/// <summary>Owns the strict serialization grammar for persisted parity captures.</summary>
public static class CaptureSchema
{
    /// <summary>The current capture document schema.</summary>
    public const int CurrentVersion = 3;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        AllowDuplicateProperties = false,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    /// <summary>Serializes a capture with the schema's canonical field and enum spellings.</summary>
    public static string Serialize(CaptureBundle capture)
        => JsonSerializer.Serialize(capture, SerializerOptions);

    /// <summary>Deserializes a capture using the strict schema grammar.</summary>
    public static CaptureBundle Deserialize(string json)
    {
        var capture = JsonSerializer.Deserialize<CaptureBundle>(json, SerializerOptions)
            ?? throw new JsonException("A capture document must be a JSON object.");

        foreach (var step in capture.Steps)
        {
            ValidateObservations(step);
        }

        return capture;
    }

    internal static StepCapture DeserializeStep(string json)
    {
        var step = JsonSerializer.Deserialize<StepCapture>(json, SerializerOptions)
            ?? throw new JsonException("A step capture must be a JSON object.");

        ValidateObservations(step);

        return step;
    }

    private static void ValidateObservations(StepCapture step)
    {
        foreach (var observation in step.ScreenshotObservations)
        {
            switch (observation.State)
            {
                case ScreenshotObservationState.Captured
                    when string.IsNullOrEmpty(observation.FileName):
                    throw new JsonException(
                        "A captured screenshot observation must carry a fileName.");
                case ScreenshotObservationState.CaptureFailed
                    when string.IsNullOrEmpty(observation.Detail):
                    throw new JsonException(
                        "A failed screenshot observation must carry a detail diagnostic.");
            }
        }
    }
}
