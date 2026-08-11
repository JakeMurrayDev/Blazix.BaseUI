using System.Text.Json.Serialization;
using Blazix.BaseUI.Parity.Tests.Capture;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Parity.Tests.Baselines;

/// <summary>The committed repository authority for every baseline set.</summary>
public sealed record BaselineAuthority
{
    /// <summary>The current authority schema.</summary>
    public const int CurrentSchemaVersion = 3;

    /// <summary>The current capture schema.</summary>
    public const int CurrentCaptureSchemaVersion = Capture.CaptureSchema.CurrentVersion;

    /// <summary>Gets the authority schema.</summary>
    [JsonPropertyName("schemaVersion")]
    public required int SchemaVersion { get; init; }

    /// <summary>Gets the capture schema.</summary>
    [JsonPropertyName("captureSchemaVersion")]
    public required int CaptureSchemaVersion { get; init; }

    /// <summary>Gets the repository-declared Base UI commit.</summary>
    [JsonPropertyName("declaredRepositoryPin")]
    public required string DeclaredRepositoryPin { get; init; }
}

/// <summary>The exact browser and operating-system baseline selector.</summary>
public sealed record BaselinePlatform
{
    /// <summary>Gets the browser family.</summary>
    [JsonPropertyName("browser")]
    public required string Browser { get; init; }

    /// <summary>Gets the browser version.</summary>
    [JsonPropertyName("browserVersion")]
    public required string BrowserVersion { get; init; }

    /// <summary>Gets the operating system token.</summary>
    [JsonPropertyName("os")]
    public required string Os { get; init; }

    /// <summary>Gets the process architecture token.</summary>
    [JsonPropertyName("architecture")]
    public required string Architecture { get; init; }

    /// <summary>Gets the set directory, excluding browser version.</summary>
    [JsonIgnore]
    public string DirectoryName => $"{Browser}-{Os}-{Architecture}";

    /// <summary>Creates the exact platform for a running browser.</summary>
    /// <param name="browser">The Playwright browser.</param>
    /// <returns>The exact platform selector.</returns>
    public static BaselinePlatform Current(IBrowser browser)
    {
        ArgumentNullException.ThrowIfNull(browser);

        var os = OperatingSystem.IsLinux()
            ? "linux"
            : OperatingSystem.IsMacOS()
                ? "macos"
                : OperatingSystem.IsWindows()
                    ? "windows"
                    : throw new PlatformNotSupportedException("Parity baselines require Linux, macOS, or Windows.");
        var architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture switch
        {
            System.Runtime.InteropServices.Architecture.X64 => "x64",
            System.Runtime.InteropServices.Architecture.Arm64 => "arm64",
            var value => throw new PlatformNotSupportedException(
                $"Parity baselines do not support architecture '{value}'.")
        };

        return new BaselinePlatform
        {
            Browser = browser.BrowserType.Name,
            BrowserVersion = browser.Version,
            Os = os,
            Architecture = architecture
        };
    }
}

/// <summary>One hashed file inside a baseline fixture.</summary>
public sealed record BaselineArtifact
{
    /// <summary>Gets the set-relative POSIX path.</summary>
    [JsonPropertyName("path")]
    public required string Path { get; init; }

    /// <summary>Gets the uppercase SHA-256.</summary>
    [JsonPropertyName("sha256")]
    public required string Sha256 { get; init; }
}

/// <summary>Fixture-level baseline scope and provenance.</summary>
public sealed record BaselineFixtureMetadata
{
    /// <summary>Gets the fixture id.</summary>
    [JsonPropertyName("fixture")]
    public required string Fixture { get; init; }

    /// <summary>Gets the source path relative to its declared source root.</summary>
    [JsonPropertyName("sourcePath")]
    public required string SourcePath { get; init; }

    /// <summary>Gets the source content SHA-256.</summary>
    [JsonPropertyName("sourceHash")]
    public required string SourceHash { get; init; }

    /// <summary>Gets the full fixture contract plus alias-authority SHA-256.</summary>
    [JsonPropertyName("contractHash")]
    public required string ContractHash { get; init; }

    /// <summary>Gets the single color-scheme theme actually emulated for this capture.</summary>
    [JsonPropertyName("theme")]
    public required string Theme { get; init; }

    /// <summary>Gets exact manifest step order.</summary>
    [JsonPropertyName("steps")]
    public required IReadOnlyList<string> Steps { get; init; }

    /// <summary>Gets the set-relative capture path.</summary>
    [JsonPropertyName("capture")]
    public required string Capture { get; init; }

    /// <summary>Gets every required capture and screenshot hash.</summary>
    [JsonPropertyName("artifacts")]
    public required IReadOnlyList<BaselineArtifact> Artifacts { get; init; }
}

/// <summary>Metadata for one platform-exact committed baseline set.</summary>
public sealed record BaselineSetMetadata
{
    /// <summary>Gets the metadata schema.</summary>
    [JsonPropertyName("schemaVersion")]
    public required int SchemaVersion { get; init; }

    /// <summary>Gets the capture schema.</summary>
    [JsonPropertyName("captureSchemaVersion")]
    public required int CaptureSchemaVersion { get; init; }

    /// <summary>Gets the exact upstream commit captured.</summary>
    [JsonPropertyName("upstreamSha")]
    public required string UpstreamSha { get; init; }

    /// <summary>Gets the exact browser/OS provenance.</summary>
    [JsonPropertyName("platform")]
    public required BaselinePlatform Platform { get; init; }

    /// <summary>Gets the UTC generation instant.</summary>
    [JsonPropertyName("generatedAtUtc")]
    public required DateTimeOffset GeneratedAtUtc { get; init; }

    /// <summary>Gets the committed fixture manifest hash.</summary>
    [JsonPropertyName("fixtureManifestHash")]
    public required string FixtureManifestHash { get; init; }

    /// <summary>Gets the committed alias manifest SHA-256.</summary>
    [JsonPropertyName("aliasManifestHash")]
    public required string AliasManifestHash { get; init; }

    /// <summary>Gets the committed shared stylesheet hash.</summary>
    [JsonPropertyName("stylesheetHash")]
    public required string StylesheetHash { get; init; }

    /// <summary>Gets fixture baselines in manifest order.</summary>
    [JsonPropertyName("fixtures")]
    public required IReadOnlyList<BaselineFixtureMetadata> Fixtures { get; init; }
}

/// <summary>Validated read-only authority and metadata for one committed baseline set.</summary>
public sealed record BaselineSnapshot
{
    internal BaselineSnapshot(BaselineAuthority authority, BaselineSetMetadata set)
    {
        Authority = authority;
        Set = set;
    }

    /// <summary>Gets the repository baseline authority.</summary>
    public BaselineAuthority Authority { get; }

    /// <summary>Gets the platform-exact baseline-set metadata.</summary>
    public BaselineSetMetadata Set { get; }
}

/// <summary>Validated read-only repository authority independent of any platform set.</summary>
public sealed record BaselineAuthoritySnapshot
{
    internal BaselineAuthoritySnapshot(BaselineAuthority authority)
    {
        Authority = authority;
    }

    /// <summary>Gets the validated repository authority.</summary>
    public BaselineAuthority Authority { get; }
}

/// <summary>Opaque validated live fixture-theme provenance and computed contract hash.</summary>
public sealed record LiveFixtureProvenanceSnapshot
{
    internal LiveFixtureProvenanceSnapshot(
        string fixture,
        string theme,
        string contractHash,
        LiveBaselineProvenance provenance)
    {
        Fixture = fixture;
        Theme = theme;
        ContractHash = contractHash;
        Provenance = provenance;
    }

    public string Fixture { get; }

    public string Theme { get; }

    public string ContractHash { get; }

    public LiveBaselineProvenance Provenance { get; }
}

/// <summary>Live source provenance supplied to an atomic baseline write.</summary>
public sealed record LiveBaselineProvenance(
    string UpstreamSha,
    string SourcePath,
    string SourceHash,
    DateTimeOffset GeneratedAtUtc);

/// <summary>Positive evidence that one atomic baseline replacement completed.</summary>
public sealed record BaselineWriteReceipt(
    string Fixture,
    BaselinePlatform Platform,
    DateTimeOffset GeneratedAtUtc,
    string CaptureSha256);
