using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Blazix.BaseUI.Parity.Tests.Baselines;

namespace Blazix.BaseUI.Parity.Tests.Infrastructure;

/// <summary>One source file bound into a generated React fixture bundle.</summary>
internal sealed record ReactBundleSource
{
    [JsonPropertyName("fixture")]
    public required string Fixture { get; init; }

    [JsonPropertyName("sourcePath")]
    public required string SourcePath { get; init; }

    [JsonPropertyName("sourceHash")]
    public required string SourceHash { get; init; }
}

/// <summary>Build-generated binding between live source provenance and exact Vite output.</summary>
internal sealed record ReactBundleProvenanceManifest
{
    internal const string FileName = "parity-provenance.json";
    internal const int CurrentSchemaVersion = 2;

    [JsonPropertyName("schemaVersion")]
    public required int SchemaVersion { get; init; }

    [JsonPropertyName("upstreamSha")]
    public required string UpstreamSha { get; init; }

    [JsonPropertyName("distFingerprint")]
    public required string DistFingerprint { get; init; }

    [JsonPropertyName("generatedAtUtc")]
    public required DateTimeOffset GeneratedAtUtc { get; init; }

    [JsonPropertyName("sources")]
    public required IReadOnlyList<ReactBundleSource> Sources { get; init; }
}

/// <summary>Reads and validates the generated live React bundle provenance manifest.</summary>
internal static class ReactBundleProvenance
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        AllowDuplicateProperties = false,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    private static readonly ConcurrentDictionary<string, string> FingerprintCache =
        new(StringComparer.Ordinal);

    internal static void Validate(
        string directory,
        FixtureEntry fixture,
        LiveBaselineProvenance provenance)
    {
        var manifest = Load(directory);
        ValidateManifest(directory, manifest);

        var matches = manifest.Sources
            .Where(source => string.Equals(source.Fixture, fixture.Id, StringComparison.Ordinal))
            .ToArray();
        if (matches.Length != 1 ||
            !string.Equals(manifest.UpstreamSha, provenance.UpstreamSha, StringComparison.Ordinal) ||
            !string.Equals(matches[0].SourcePath, provenance.SourcePath, StringComparison.Ordinal) ||
            !string.Equals(matches[0].SourceHash, provenance.SourceHash, StringComparison.Ordinal) ||
            manifest.GeneratedAtUtc != provenance.GeneratedAtUtc)
        {
            throw new InvalidOperationException(
                $"The React bundle was not built from the current provenance for '{fixture.Id}'.");
        }
    }

    internal static DateTimeOffset GeneratedAtUtc(string directory)
    {
        var manifest = Load(directory);
        ValidateManifest(directory, manifest);

        return manifest.GeneratedAtUtc;
    }

    internal static string Fingerprint(string directory)
        => FingerprintCache.GetOrAdd(Path.GetFullPath(directory), ComputeFingerprint);

    private static string ComputeFingerprint(string directory)
    {
        var entries = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Select(path => new
            {
                Path = path,
                RelativePath = Path.GetRelativePath(directory, path)
                    .Replace(Path.DirectorySeparatorChar, '/')
            })
            .Where(item => !string.Equals(
                item.RelativePath,
                ReactBundleProvenanceManifest.FileName,
                StringComparison.Ordinal))
            .Select(item =>
                item.RelativePath + ":" +
                Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(item.Path))))
            .OrderBy(value => value, StringComparer.Ordinal);
        var canonical = string.Join('\n', entries) + "\n";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static bool IsUpperHex(string? value, int length)
        => value is not null && value.Length == length && value.All(character =>
            character is >= '0' and <= '9' or >= 'A' and <= 'F');

    private static bool IsHex(string? value, int length)
        => value is not null && value.Length == length && value.All(Uri.IsHexDigit);

    private static ReactBundleProvenanceManifest Load(string directory)
    {
        var path = Path.Combine(directory, ReactBundleProvenanceManifest.FileName);
        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                $"The source React bundle provenance manifest '{path}' is missing.");
        }

        try
        {
            return JsonSerializer.Deserialize<ReactBundleProvenanceManifest>(
                File.ReadAllText(path), SerializerOptions)
                ?? throw new JsonException("manifest contains null");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"The source React bundle provenance manifest is corrupt: {exception.Message}",
                exception);
        }
    }

    private static void ValidateManifest(
        string directory,
        ReactBundleProvenanceManifest manifest)
    {
        if (manifest.SchemaVersion != ReactBundleProvenanceManifest.CurrentSchemaVersion ||
            !IsHex(manifest.UpstreamSha, 40) ||
            !IsUpperHex(manifest.DistFingerprint, 64) ||
            manifest.GeneratedAtUtc == default ||
            manifest.GeneratedAtUtc.Offset != TimeSpan.Zero ||
            manifest.Sources is null ||
            manifest.Sources.Count == 0 ||
            manifest.Sources.Any(source => source is null) ||
            manifest.Sources.Select(source => source.Fixture)
                .Distinct(StringComparer.Ordinal).Count() != manifest.Sources.Count ||
            manifest.Sources.Select(source => source.SourcePath)
                .Distinct(StringComparer.Ordinal).Count() != manifest.Sources.Count ||
            manifest.Sources.Any(source =>
                string.IsNullOrWhiteSpace(source.Fixture) ||
                string.IsNullOrWhiteSpace(source.SourcePath) ||
                !IsUpperHex(source.SourceHash, 64)))
        {
            throw new InvalidOperationException(
                "The source React bundle provenance manifest has invalid or duplicate data.");
        }

        if (!string.Equals(manifest.DistFingerprint, Fingerprint(directory), StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The source React bundle fingerprint differs from its build provenance manifest.");
        }
    }
}
