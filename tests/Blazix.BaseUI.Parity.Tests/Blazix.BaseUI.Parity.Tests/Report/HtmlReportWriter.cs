using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Blazix.BaseUI.Parity.Tests.Report;

/// <summary>
/// Renders a self-contained, offline-readable parity report shell whose only external
/// resource is the report-local stylesheet.
/// </summary>
public static class HtmlReportWriter
{
    private const string EmptyValue = "Not recorded";
    private const string LocalPathOmitted = "Local path omitted";
    private const string UnsafeAssetOmitted = "Unsafe asset path omitted";

    private static readonly HtmlEncoder Encoder = HtmlEncoder.Default;
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    /// <summary>
    /// Renders <paramref name="model"/> without scripts, remote resources, or raw model text.
    /// </summary>
    /// <param name="model">The already ordered and policy-evaluated report model.</param>
    /// <returns>A complete HTML document.</returns>
    public static string Render(ReportModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        using var document = JsonSerializer.SerializeToDocument(model, JsonOptions);
        ReportOutputSafety.ValidateNoMachinePaths(document.RootElement);
        var builder = new StringBuilder(64 * 1024);

        builder.Append("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\">")
            .Append("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">")
            .Append("<meta http-equiv=\"Content-Security-Policy\" content=\"default-src 'none'; img-src 'self' data:; style-src 'self'; base-uri 'none'; form-action 'none'\">")
            .Append("<title>React versus Blazor parity report</title>")
            .Append("<link rel=\"stylesheet\" href=\"report.css\"></head><body>")
            .Append("<header><p class=\"eyebrow\">Observable parity evidence</p>")
            .Append("<h1>React versus Blazor parity report</h1>")
            .Append("<p class=\"muted\">Evidence is shown in the canonical order recorded by the runner. The absence of a finding is not evidence of equality.</p>")
            .Append("</header><main id=\"main\">");

        RenderRoot(builder, document.RootElement);

        builder.Append("</main><footer>Offline parity evidence. No script or network resource is required.</footer></body></html>");
        return builder.ToString();
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static void RenderRoot(StringBuilder builder, JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("The parity report model must serialize as an object.");
        }

        foreach (var property in root.EnumerateObject())
        {
            RenderSection(builder, property.Name, property.Value);
        }
    }

    private static void RenderSection(StringBuilder builder, string name, JsonElement value)
    {
        builder.Append("<section class=\"panel section-");
        AppendCssToken(builder, name);
        builder.Append("\" aria-labelledby=\"section-");
        AppendCssToken(builder, name);
        builder.Append("\"><h2 id=\"section-");
        AppendCssToken(builder, name);
        builder.Append("\">");
        AppendText(builder, Humanize(name));
        builder.Append("</h2>");

        switch (name)
        {
            case "findings":
                RenderEvidenceArray(builder, value);
                break;
            case "artifacts":
                RenderArtifactArray(builder, value);
                break;
            case "counts":
                RenderCountValue(builder, value);
                break;
            default:
                RenderValue(builder, name, value, 0);
                break;
        }

        builder.Append("</section>");
    }

    private static void RenderEvidenceArray(StringBuilder builder, JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() == 0)
        {
            RenderEmpty(builder);
            return;
        }

        foreach (var finding in value.EnumerateArray())
        {
            var tier = GetString(finding, "tier") ?? "Diagnostic";
            var disposition = GetString(finding, "disposition") ?? "Informational";

            builder.Append("<article class=\"evidence evidence-");
            AppendCssToken(builder, tier);
            builder.Append("\"><h3>");
            AppendText(builder, GetFindingHeading(finding));
            builder.Append("</h3><p><span class=\"status status-");
            AppendCssToken(builder, disposition);
            builder.Append("\">");
            AppendText(builder, disposition);
            builder.Append("</span> <span class=\"status\">");
            AppendText(builder, tier);
            builder.Append(" evidence</span></p>");
            RenderObject(builder, finding, 1, "Finding evidence");
            builder.Append("</article>");
        }
    }

    private static void RenderArtifactArray(StringBuilder builder, JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() == 0)
        {
            RenderEmpty(builder);
            return;
        }

        builder.Append("<div class=\"three-up\">");
        foreach (var artifact in value.EnumerateArray())
        {
            builder.Append("<article class=\"shot\">");
            RenderArtifactImages(builder, artifact);
            RenderObject(builder, artifact, 1, "Artifact");
            builder.Append("</article>");
        }

        builder.Append("</div>");
    }

    private static void RenderArtifactImages(StringBuilder builder, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                if (value.TryGetProperty("relativePath", out var relativePath)
                    && relativePath.ValueKind == JsonValueKind.String)
                {
                    RenderImage(builder, value, relativePath.GetString());
                }

                foreach (var property in value.EnumerateObject())
                {
                    if (property.Name != "relativePath")
                    {
                        RenderArtifactImages(builder, property.Value);
                    }
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in value.EnumerateArray())
                {
                    RenderArtifactImages(builder, item);
                }

                break;
        }
    }

    private static void RenderImage(StringBuilder builder, JsonElement artifact, string? relativePath)
    {
        builder.Append("<figure>");
        if (IsSafeRelativeAssetPath(relativePath))
        {
            var role = GetString(artifact, "role") ?? GetString(artifact, "kind") ?? "Parity artifact";
            var fixture = GetString(artifact, "fixture") ?? GetString(artifact, "executionId");
            var caption = fixture is null ? role : $"{role}: {fixture}";

            builder.Append("<img loading=\"lazy\" src=\"");
            AppendAttribute(builder, relativePath!);
            builder.Append("\" alt=\"");
            AppendAttribute(builder, caption);
            builder.Append("\"><figcaption>");
            AppendText(builder, caption);
            builder.Append("</figcaption>");
        }
        else
        {
            builder.Append("<figcaption class=\"empty\">");
            AppendText(builder, UnsafeAssetOmitted);
            builder.Append("</figcaption>");
        }

        builder.Append("</figure>");
    }

    private static void RenderCountValue(StringBuilder builder, JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            RenderValue(builder, "counts", value, 0);
            return;
        }

        builder.Append("<dl class=\"count-grid\">");
        foreach (var property in value.EnumerateObject())
        {
            if (IsScalar(property.Value))
            {
                builder.Append("<div class=\"count-card\"><dt>");
                AppendText(builder, Humanize(property.Name));
                builder.Append("</dt><dd>");
                RenderScalar(builder, property.Name, property.Value);
                builder.Append("</dd></div>");
            }
        }

        builder.Append("</dl>");

        foreach (var property in value.EnumerateObject())
        {
            if (!IsScalar(property.Value))
            {
                RenderNamedValue(builder, property.Name, property.Value, 1);
            }
        }
    }

    private static void RenderValue(StringBuilder builder, string name, JsonElement value, int depth)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                RenderObject(builder, value, depth, Humanize(name));
                break;
            case JsonValueKind.Array:
                RenderArray(builder, name, value, depth);
                break;
            default:
                RenderScalar(builder, name, value);
                break;
        }
    }

    private static void RenderObject(StringBuilder builder, JsonElement value, int depth, string label)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            RenderValue(builder, label, value, depth);
            return;
        }

        if (!value.EnumerateObject().Any())
        {
            RenderEmpty(builder);
            return;
        }

        builder.Append("<dl class=\"metadata\">");
        foreach (var property in value.EnumerateObject())
        {
            if (IsScalar(property.Value))
            {
                builder.Append("<dt>");
                AppendText(builder, Humanize(property.Name));
                builder.Append("</dt><dd>");
                RenderScalar(builder, property.Name, property.Value);
                builder.Append("</dd>");
            }
        }

        builder.Append("</dl>");

        foreach (var property in value.EnumerateObject())
        {
            if (!IsScalar(property.Value))
            {
                RenderNamedValue(builder, property.Name, property.Value, depth + 1);
            }
        }
    }

    private static void RenderNamedValue(StringBuilder builder, string name, JsonElement value, int depth)
    {
        var heading = Math.Min(depth + 2, 4);
        builder.Append('<').Append('h').Append(heading).Append('>');
        AppendText(builder, Humanize(name));
        builder.Append("</h").Append(heading).Append('>');
        RenderValue(builder, name, value, depth);
    }

    private static void RenderArray(StringBuilder builder, string name, JsonElement value, int depth)
    {
        if (value.GetArrayLength() == 0)
        {
            RenderEmpty(builder);
            return;
        }

        if (value.EnumerateArray().All(IsScalar))
        {
            builder.Append("<ul>");
            foreach (var item in value.EnumerateArray())
            {
                builder.Append("<li>");
                RenderScalar(builder, name, item);
                builder.Append("</li>");
            }

            builder.Append("</ul>");
            return;
        }

        var index = 0;
        foreach (var item in value.EnumerateArray())
        {
            builder.Append("<details><summary>");
            AppendText(builder, GetItemHeading(name, item, index));
            builder.Append("</summary>");
            RenderValue(builder, name, item, depth + 1);
            builder.Append("</details>");
            index++;
        }
    }

    private static void RenderScalar(StringBuilder builder, string name, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                RenderString(builder, name, value.GetString());
                break;
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                AppendText(builder, value.GetRawText());
                break;
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                builder.Append("<span class=\"empty\">");
                AppendText(builder, EmptyValue);
                builder.Append("</span>");
                break;
        }
    }

    private static void RenderString(StringBuilder builder, string name, string? value)
    {
        if (IsMachinePathField(name) && LooksLikeMachinePath(value))
        {
            builder.Append("<span class=\"empty\">");
            AppendText(builder, LocalPathOmitted);
            builder.Append("</span>");
            return;
        }

        if (name.Equals("relativePath", StringComparison.OrdinalIgnoreCase)
            && !IsSafeRelativeAssetPath(value))
        {
            builder.Append("<span class=\"empty\">");
            AppendText(builder, UnsafeAssetOmitted);
            builder.Append("</span>");
            return;
        }

        AppendText(builder, string.IsNullOrEmpty(value) ? EmptyValue : value);
    }

    private static void RenderEmpty(StringBuilder builder)
    {
        builder.Append("<p class=\"empty\">None recorded</p>");
    }

    private static string GetFindingHeading(JsonElement finding)
    {
        var kind = GetString(finding, "kind")
            ?? GetNestedString(finding, "finding", "kind")
            ?? "Finding";
        var fixture = GetString(finding, "executionId")
            ?? GetString(finding, "fixture")
            ?? GetNestedString(finding, "finding", "fixture");
        var step = GetString(finding, "step")
            ?? GetNestedString(finding, "finding", "step");

        return string.Join(" · ", new[] { kind, fixture, step }.Where(part => !string.IsNullOrEmpty(part)));
    }

    private static string GetItemHeading(string name, JsonElement item, int index)
    {
        if (item.ValueKind != JsonValueKind.Object)
        {
            return $"{Humanize(name)} {index + 1}";
        }

        var identity = GetString(item, "executionId")
            ?? GetString(item, "fixture")
            ?? GetString(item, "step")
            ?? GetString(item, "kind")
            ?? GetString(item, "code")
            ?? GetString(item, "id");

        return identity is null
            ? $"{Humanize(Singularize(name))} {index + 1}"
            : $"{Humanize(Singularize(name))}: {identity}";
    }

    private static string? GetString(JsonElement value, string propertyName)
    {
        if (value.ValueKind != JsonValueKind.Object
            || !value.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return property.GetString();
    }

    private static string? GetNestedString(JsonElement value, string objectName, string propertyName)
    {
        if (value.ValueKind != JsonValueKind.Object
            || !value.TryGetProperty(objectName, out var nested))
        {
            return null;
        }

        return GetString(nested, propertyName);
    }

    private static bool IsScalar(JsonElement value)
        => value.ValueKind is not JsonValueKind.Object and not JsonValueKind.Array;

    private static bool IsSafeRelativeAssetPath(string? value)
        => ReportOutputSafety.IsSafeRelativeArtifactPath(value);

    private static bool IsMachinePathField(string name)
        => name.Equals("sourcePath", StringComparison.OrdinalIgnoreCase)
            || name.Equals("localPath", StringComparison.OrdinalIgnoreCase)
            || name.Equals("artifactSourceDirectory", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikeMachinePath(string? value)
        => !string.IsNullOrWhiteSpace(value)
            && (value.StartsWith("/", StringComparison.Ordinal)
                || value.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
                || (value.Length > 2 && char.IsLetter(value[0]) && value[1] == ':' && value[2] is '\\' or '/'));

    private static string Singularize(string name)
        => name.EndsWith("ies", StringComparison.OrdinalIgnoreCase) && name.Length > 3
            ? $"{name[..^3]}y"
            : name.EndsWith('s') && name.Length > 1
                ? name[..^1]
                : name;

    private static string Humanize(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var builder = new StringBuilder(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (index > 0 && char.IsUpper(character) && !char.IsUpper(value[index - 1]))
            {
                builder.Append(' ');
            }

            builder.Append(index == 0 ? char.ToUpperInvariant(character) : character);
        }

        return builder.ToString();
    }

    private static void AppendCssToken(StringBuilder builder, string value)
    {
        foreach (var character in value)
        {
            if (char.IsAsciiLetterOrDigit(character) || character == '-')
            {
                builder.Append(char.ToLowerInvariant(character));
            }
            else
            {
                builder.Append('-');
            }
        }
    }

    private static void AppendText(StringBuilder builder, string value)
        => builder.Append(Encoder.Encode(value));

    private static void AppendAttribute(StringBuilder builder, string value)
        => builder.Append(Encoder.Encode(value));
}
