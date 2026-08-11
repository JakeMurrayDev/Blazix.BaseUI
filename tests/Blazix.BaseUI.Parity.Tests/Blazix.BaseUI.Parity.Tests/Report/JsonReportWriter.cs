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

/// <summary>Shared path and output safety checks for report renderers and packages.</summary>
internal static class ReportOutputSafety
{
    internal static void ValidateNoMachinePaths(JsonElement value)
    {
        ValidateNoMachinePaths(value, "$");
    }

    private static void ValidateNoMachinePaths(
        JsonElement value,
        string jsonPath)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in value.EnumerateObject())
                {
                    ValidateNoMachinePaths(
                        property.Value,
                        $"{jsonPath}.{property.Name}");
                }

                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in value.EnumerateArray())
                {
                    ValidateNoMachinePaths(item, $"{jsonPath}[{index}]");
                    index++;
                }

                break;
            case JsonValueKind.String:
                var text = value.GetString();
                if (ContainsMachinePath(text))
                {
                    throw new InvalidOperationException(
                        $"Parity report output field '{jsonPath}' contains a machine-local absolute path.");
                }

                break;
        }
    }

    internal static void ValidateRelativeArtifactPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            path.StartsWith("/", StringComparison.Ordinal) ||
            path.StartsWith("//", StringComparison.Ordinal) ||
            path.Contains('\\') ||
            path.Contains(':') ||
            path.Any(char.IsControl))
        {
            throw new InvalidOperationException(
                $"Report artifact path '{path}' is not a safe report-relative POSIX path.");
        }

        var segments = path.Split('/');
        if (segments.Any(segment =>
                segment.Length == 0 ||
                segment is "." or ".." ||
                segment.Any(character =>
                    !char.IsAsciiLetterOrDigit(character) &&
                    character is not ('.' or '_' or '-'))))
        {
            throw new InvalidOperationException(
                $"Report artifact path '{path}' is not a safe report-relative POSIX path.");
        }
    }

    /// <summary>
    /// The complete inventory of first path segments the parity fixture sites serve. A
    /// filesystem path and a root-relative URL are spelled identically —
    /// <c>/segment/segment</c> — so the only absolute token that can be *proved* to be a
    /// URL is one naming something these sites actually publish. Everything else absolute
    /// is treated as a machine path, which is the safe direction for a guard whose job is
    /// to keep machine-local paths out of committed evidence: an unrecognised prefix fails
    /// the render loudly instead of serialising silently into a baseline.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><c>react</c> — the React fixture site's Vite base
    /// (<c>react-fixtures/vite.config.mts</c> <c>base: '/react/'</c>), which also covers
    /// the upstream demo markup's links into the docs site
    /// (<c>/react/overview/quick-start</c>).</item>
    /// <item><c>fixture</c> — the Blazor fixture host pages
    /// (<c>Pages/FixtureHostServer.razor</c>, <c>Pages/FixtureHostWasm.razor</c>:
    /// <c>/fixture/{Component}/{Demo}/{server|wasm}</c>).</item>
    /// <item><c>parity.css</c> — the single stylesheet both legs link
    /// (<c>react-fixtures/index.html</c>, built by the <c>parity:css</c> script into
    /// <c>wwwroot/parity.css</c>).</item>
    /// <item><c>_content</c>, <c>_framework</c> — Blazor static web assets and runtime
    /// served by the fixture host.</item>
    /// </list>
    /// Extend this inventory when a fixture site starts serving a new route root; do not
    /// relax the discriminator itself.
    /// </remarks>
    private static readonly HashSet<string> FixtureSiteRootSegments =
        new(StringComparer.Ordinal)
        {
            "react", "fixture", "parity.css", "_content", "_framework"
        };

    /// <summary>
    /// The absolute roots that belong to the machine this process is running on. Second
    /// line of defence behind <see cref="FixtureSiteRootSegments"/>: it holds even if a
    /// checkout ever lands under a directory that shares a name with a served route root.
    /// </summary>
    private static readonly string[] HostRoots = ResolveHostRoots();

    private static string[] ResolveHostRoots()
        => new[]
            {
                Path.GetFullPath(Path.Combine(Infrastructure.ParityPaths.HarnessRoot, "..", "..")),
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                Path.GetTempPath(),
                AppContext.BaseDirectory,
                Directory.GetCurrentDirectory()
            }
            .Where(root => root.Length > 1)
            .Select(root => root.Replace('\\', '/').TrimEnd('/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static bool ContainsMachinePath(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        if (value.Contains("\\\\", StringComparison.Ordinal) ||
            value.Contains("file:", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (value.StartsWith("/", StringComparison.Ordinal) &&
            !IsRootRelativeUrlReference(value, 0))
        {
            return true;
        }

        for (var index = 0; index < value.Length; index++)
        {
            if (index + 2 < value.Length &&
                (index == 0 || char.IsWhiteSpace(value[index - 1]) ||
                 value[index - 1] is '\'' or '"' or '(' or '[' or '{' or '=') &&
                char.IsAsciiLetter(value[index]) &&
                value[index + 1] == ':' &&
                value[index + 2] is '\\' or '/')
            {
                return true;
            }

            if (value[index] == '/' &&
                (index == 0 || char.IsWhiteSpace(value[index - 1]) ||
                 value[index - 1] is '\'' or '"' or '(' or '[' or '{' or '=') &&
                index + 1 < value.Length &&
                !char.IsWhiteSpace(value[index + 1]) &&
                !IsPlaywrightAriaPropertyKey(value, index) &&
                !IsRootRelativeUrlReference(value, index))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Decides whether the token starting at <paramref name="slashIndex"/> is a
    /// root-relative URL reference — <c>/react/overview/quick-start</c> — rather than a
    /// path into this machine's filesystem.
    /// </summary>
    /// <remarks>
    /// A reference qualifies only when it is spelled entirely in URL path characters, has
    /// no empty or dot segments, does not begin at one of this host's own roots, and leads
    /// with a segment the fixture sites actually serve
    /// (<see cref="FixtureSiteRootSegments"/>). Anything else — a space, a backslash,
    /// <c>//host/share</c>, <c>/Users/…</c>, <c>/var/folders/…</c>, and equally
    /// <c>/builds/group/other-repo/…</c>, <c>/workspace/…</c>, <c>/nix/store/…</c> —
    /// stays a machine path, because nothing proves it is a URL.
    /// </remarks>
    private static bool IsRootRelativeUrlReference(string value, int slashIndex)
    {
        var end = slashIndex;
        while (end < value.Length && !IsTokenTerminator(value[end]))
        {
            end++;
        }

        var token = value[slashIndex..end];

        // "//host/share" is a UNC share or a protocol-relative reference, never a path
        // on the site under test. A bare "/" is the filesystem root.
        if (token.Length < 2 || token[1] == '/')
        {
            return false;
        }

        if (HostRoots.Any(root => token.StartsWith(root, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (!token.All(IsUrlPathCharacter))
        {
            return false;
        }

        var queryIndex = token.IndexOfAny(['?', '#']);
        var path = queryIndex < 0 ? token : token[..queryIndex];
        var segments = path[1..].Split('/');

        return segments.All(segment => segment.Length > 0 && segment is not ("." or "..")) &&
               FixtureSiteRootSegments.Contains(segments[0]);
    }

    /// <summary>Ends a candidate path or URL token in surrounding prose or markup.</summary>
    private static bool IsTokenTerminator(char character)
        => char.IsWhiteSpace(character) ||
           character is '\'' or '"' or '`' or '(' or ')' or '[' or ']' or '{' or '}'
               or '<' or '>' or ',' or ';';

    /// <summary>
    /// Matches the RFC 3986 path characters that survive <see cref="IsTokenTerminator"/>,
    /// plus the query and fragment delimiters.
    /// </summary>
    private static bool IsUrlPathCharacter(char character)
        => char.IsAsciiLetterOrDigit(character) ||
           character is '/' or '-' or '.' or '_' or '~' or '%' or '!' or '$' or '&' or '*'
               or '+' or '=' or ':' or '@' or '?' or '#';

    private static bool IsPlaywrightAriaPropertyKey(string value, int slashIndex)
    {
        if (slashIndex < 2 || value[slashIndex - 2] != '-' || value[slashIndex - 1] != ' ')
        {
            return false;
        }

        var lineStart = value.LastIndexOf('\n', slashIndex - 1) + 1;
        if (value.AsSpan(lineStart, slashIndex - lineStart - 2)
            .ContainsAnyExcept(' ', '\t'))
        {
            return false;
        }

        var key = value.AsSpan(slashIndex);
        return key.StartsWith("/placeholder:", StringComparison.Ordinal) ||
               key.StartsWith("/url:", StringComparison.Ordinal) ||
               key.StartsWith("/children:", StringComparison.Ordinal);
    }
}
