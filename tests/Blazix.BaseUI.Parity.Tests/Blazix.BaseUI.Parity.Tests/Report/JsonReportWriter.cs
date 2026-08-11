using System.Text.Json;
using System.Text.Json.Serialization;

namespace Blazix.BaseUI.Parity.Tests.Report;

/// <summary>Renders the canonical deterministic machine-readable parity report.</summary>
public static class JsonReportWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateOptions();

    /// <summary>Serializes an already policy-evaluated report.</summary>
    /// <param name="model">The shared report model.</param>
    /// <returns>UTF-8 JSON bytes.</returns>
    public static byte[] Render(ReportModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var bytes = JsonSerializer.SerializeToUtf8Bytes(model, SerializerOptions);
        using var document = JsonDocument.Parse(bytes);
        ReportOutputSafety.ValidateNoMachinePaths(document.RootElement);
        return bytes;
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Default,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
