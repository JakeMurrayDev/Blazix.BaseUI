using System.Security.Cryptography;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Infrastructure;

namespace Blazix.BaseUI.Parity.Tests.Baselines;

/// <summary>Reads and atomically replaces committed platform baseline sets.</summary>
public sealed class BaselineStore
{
    private const string RefreshCommand = "pnpm parity:baseline";
    private const string MetadataFileName = "metadata.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        AllowDuplicateProperties = false,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true
    };

    private readonly string root;
    private readonly string screenshotDirectory;
    private readonly string manifestPath;
    private readonly string aliasManifestPath;
    private readonly string stylesheetPath;
    private readonly Action<string> deleteDirectory;

    /// <summary>Creates the production store over committed and run-output paths.</summary>
    public BaselineStore()
        : this(
            ParityPaths.Baselines,
            ParityPaths.Screenshots,
            Path.Combine(ParityPaths.Manifest, "fixtures.json"),
            Path.Combine(ParityPaths.HarnessRoot, "Blazix.BaseUI.Parity.Tests", "wwwroot", "parity.css"),
            DeleteDirectory)
    {
    }

    internal BaselineStore(string screenshotDirectory)
        : this(
            ParityPaths.Baselines,
            screenshotDirectory,
            Path.Combine(ParityPaths.Manifest, "fixtures.json"),
            Path.Combine(ParityPaths.HarnessRoot, "Blazix.BaseUI.Parity.Tests", "wwwroot", "parity.css"),
            DeleteDirectory)
    {
    }

    internal BaselineStore(
        string root,
        string screenshotDirectory,
        string manifestPath,
        string stylesheetPath,
        Action<string>? deleteDirectory = null,
        string? aliasManifestPath = null)
    {
        this.root = root;
        this.screenshotDirectory = screenshotDirectory;
        this.manifestPath = manifestPath;
        this.aliasManifestPath = aliasManifestPath ??
            Path.Combine(Path.GetDirectoryName(manifestPath)!, "aliases.json");
        this.stylesheetPath = stylesheetPath;
        this.deleteDirectory = deleteDirectory ?? DeleteDirectory;
    }

    /// <summary>Creates an empty sibling store with upgraded authority for an all-fixture migration.</summary>
    internal (BaselineStore Store, string Root) CreateSchemaMigrationStaging(
        Action<string, BaselineAuthority>? writeAuthority = null)
    {
        var authority = Read<BaselineAuthority>(Path.Combine(root, MetadataFileName));
        if (authority.SchemaVersion != BaselineAuthority.CurrentSchemaVersion ||
            authority.CaptureSchemaVersion is not (CaptureSchema.CurrentVersion or 2) ||
            !IsHex(authority.DeclaredRepositoryPin, 40))
        {
            throw Stale("baseline authority cannot be migrated to the current capture schema");
        }

        var stagingRoot = Path.Combine(
            Path.GetDirectoryName(root)!, $".{Path.GetFileName(root)}.schema3.{Guid.NewGuid():N}.tmp");
        try
        {
            Directory.CreateDirectory(stagingRoot);
            var upgraded = authority with { CaptureSchemaVersion = CaptureSchema.CurrentVersion };
            if (writeAuthority is null)
            {
                WriteJson(Path.Combine(stagingRoot, MetadataFileName), upgraded);
            }
            else
            {
                writeAuthority(Path.Combine(stagingRoot, MetadataFileName), upgraded);
            }
            return (new BaselineStore(
                stagingRoot,
                screenshotDirectory,
                manifestPath,
                stylesheetPath,
                deleteDirectory,
                aliasManifestPath), stagingRoot);
        }
        catch
        {
            deleteDirectory(stagingRoot);
            throw;
        }
    }

    internal bool RequiresCaptureSchemaMigration()
    {
        var authority = Read<BaselineAuthority>(Path.Combine(root, MetadataFileName));
        return authority.CaptureSchemaVersion != CaptureSchema.CurrentVersion;
    }

    /// <summary>Atomically installs a completely validated sibling store.</summary>
    internal void ReplaceWithValidatedStaging(string stagingRoot, BaselinePlatform platform)
    {
        var staged = new BaselineStore(
            stagingRoot,
            screenshotDirectory,
            manifestPath,
            stylesheetPath,
            deleteDirectory,
            aliasManifestPath);
        _ = staged.Describe(platform);

        var backup = Path.Combine(
            Path.GetDirectoryName(root)!, $".{Path.GetFileName(root)}.{Guid.NewGuid():N}.bak");
        Directory.Move(root, backup);
        try
        {
            Directory.Move(stagingRoot, root);
            deleteDirectory(backup);
        }
        catch
        {
            if (Directory.Exists(backup))
            {
                if (Directory.Exists(root))
                {
                    Directory.Move(root, stagingRoot);
                }
                Directory.Move(backup, root);
            }
            throw;
        }
    }

    /// <summary>Loads a single-theme fixture through the explicit theme-aware path.</summary>
    /// <param name="fixture">The current manifest fixture.</param>
    /// <param name="platform">The exact runtime platform.</param>
    /// <returns>The verified React capture.</returns>
    public CaptureBundle Load(FixtureEntry fixture, BaselinePlatform platform)
        => Load(fixture, RequireSingleTheme(fixture), platform);

    /// <summary>Loads, verifies, and materializes one React fixture-theme capture.</summary>
    /// <param name="fixture">The current manifest fixture.</param>
    /// <param name="theme">The exact declared theme.</param>
    /// <param name="platform">The exact runtime platform.</param>
    /// <returns>The verified React capture.</returns>
    public CaptureBundle Load(FixtureEntry fixture, string theme, BaselinePlatform platform)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentException.ThrowIfNullOrWhiteSpace(theme);
        ArgumentNullException.ThrowIfNull(platform);
        ValidateDeclaredTheme(fixture, theme);
        ValidatePlatform(platform);

        var authority = LoadAuthority();
        var setDirectory = SetDirectory(platform);
        var metadata = Read<BaselineSetMetadata>(Path.Combine(setDirectory, MetadataFileName));

        ValidateSet(authority, metadata, platform, requireComplete: true);

        var matches = metadata.Fixtures
            .Where(item =>
                string.Equals(item.Fixture, fixture.Id, StringComparison.Ordinal) &&
                string.Equals(item.Theme, theme, StringComparison.Ordinal))
            .ToArray();
        if (matches.Length != 1)
        {
            throw Stale(
                $"expected exactly one '{fixture.Id}@{theme}' entry but found {matches.Length}");
        }

        var entry = matches[0];
        var expectedSteps = fixture.Steps.Select(step => step.Name).ToArray();
        var expectedCapture =
            $"captures/{Capture.ScreenshotSet.Slug(fixture.Id)}.{theme}.json";
        if (!string.Equals(
                entry.SourcePath,
                LiveBaselineSource.ExpectedSourcePath(fixture),
                StringComparison.Ordinal) ||
            !string.Equals(entry.Theme, theme, StringComparison.Ordinal) ||
            !string.Equals(entry.Capture, expectedCapture, StringComparison.Ordinal) ||
            !entry.Steps.SequenceEqual(expectedSteps, StringComparer.Ordinal))
        {
            throw Stale($"fixture '{fixture.Id}' theme or step scope differs from the manifest");
        }

        ValidateArtifacts(setDirectory, entry.Artifacts);
        var capturePath = ResolveArtifact(setDirectory, entry.Capture);
        var capture = Read<CaptureBundle>(capturePath);

        if (!string.Equals(capture.Fixture, fixture.Id, StringComparison.Ordinal) ||
            capture.CaptureSchemaVersion != CaptureSchema.CurrentVersion ||
            capture.Leg != ParityLeg.React ||
            !string.Equals(capture.Theme, theme, StringComparison.Ordinal) ||
            !string.Equals(capture.BaseUiSha, metadata.UpstreamSha, StringComparison.Ordinal) ||
            !string.Equals(capture.SourceHash, entry.SourceHash, StringComparison.Ordinal) ||
            capture.Steps is null ||
            capture.Steps.Any(step => step is null || step.ScreenshotObservations is null) ||
            !capture.Steps.Select(step => step.Step).SequenceEqual(expectedSteps, StringComparer.Ordinal))
        {
            throw Stale($"fixture '{fixture.Id}' capture provenance or scope is inconsistent");
        }

        var expectedArtifacts = new HashSet<string>(StringComparer.Ordinal) { entry.Capture };
        ValidateObservationSets(capture, allowCaptureFailures: false);
        foreach (var screenshot in capture.Steps
                     .SelectMany(step => step.ScreenshotObservations)
                     .Where(item => item.State == ScreenshotObservationState.Captured)
                     .Select(item => item.FileName!))
        {
            if (!IsSafeFileName(screenshot))
            {
                throw Stale($"fixture '{fixture.Id}' has unsafe screenshot name '{screenshot}'");
            }

            var relative = $"screenshots/{screenshot}";
            expectedArtifacts.Add(relative);
            var source = ResolveArtifact(setDirectory, relative);
            Directory.CreateDirectory(screenshotDirectory);
            File.Copy(source, Path.Combine(screenshotDirectory, screenshot), overwrite: true);
        }

        if (!entry.Artifacts.Select(item => item.Path).ToHashSet(StringComparer.Ordinal)
                .SetEquals(expectedArtifacts))
        {
            throw Stale($"fixture '{fixture.Id}' artifact inventory differs from its capture");
        }

        return capture;
    }

    /// <summary>
    /// Validates and describes one complete platform set without materializing capture data.
    /// </summary>
    /// <param name="platform">The exact committed platform selector.</param>
    /// <returns>Read-only validated authority and set metadata.</returns>
    public BaselineSnapshot Describe(BaselinePlatform platform)
    {
        ArgumentNullException.ThrowIfNull(platform);
        ValidatePlatform(platform);

        var authority = LoadAuthority();
        var setDirectory = SetDirectory(platform);
        var metadata = Read<BaselineSetMetadata>(Path.Combine(setDirectory, MetadataFileName));
        ValidateSet(authority, metadata, platform, requireComplete: true);
        foreach (var fixture in metadata.Fixtures)
        {
            ValidateArtifacts(setDirectory, fixture.Artifacts);
        }

        return Snapshot(authority, metadata);
    }

    /// <summary>Validates repository authority without requiring a platform baseline set.</summary>
    /// <returns>The opaque validated authority snapshot.</returns>
    public BaselineAuthoritySnapshot DescribeAuthority()
    {
        var authority = LoadAuthority();
        return new BaselineAuthoritySnapshot(authority with { });
    }

    /// <summary>Validates and describes one live fixture-theme without a platform set.</summary>
    public LiveFixtureProvenanceSnapshot DescribeLiveFixture(
        FixtureEntry fixture,
        string theme,
        LiveBaselineProvenance provenance)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentException.ThrowIfNullOrWhiteSpace(theme);
        ValidateDeclaredTheme(fixture, theme);
        _ = ValidateLiveProvenance(fixture, provenance);
        return new LiveFixtureProvenanceSnapshot(
            fixture.Id,
            theme,
            ContractHash(fixture),
            provenance with { });
    }

    /// <summary>Atomically replaces a single-theme fixture through the multi-theme path.</summary>
    /// <param name="fixture">The current manifest fixture.</param>
    /// <param name="capture">The live React capture.</param>
    /// <param name="platform">The exact capture platform.</param>
    /// <param name="provenance">The live upstream and source provenance.</param>
    public BaselineWriteReceipt Write(
        FixtureEntry fixture,
        CaptureBundle capture,
        BaselinePlatform platform,
        LiveBaselineProvenance provenance)
        => Write(fixture, [capture], platform, provenance);

    /// <summary>Atomically replaces every declared theme of one fixture.</summary>
    /// <param name="fixture">The current manifest fixture.</param>
    /// <param name="captures">One React capture per declared theme, in manifest order.</param>
    /// <param name="platform">The exact capture platform.</param>
    /// <param name="provenance">The live upstream and source provenance.</param>
    public BaselineWriteReceipt Write(
        FixtureEntry fixture,
        IReadOnlyList<CaptureBundle> captures,
        BaselinePlatform platform,
        LiveBaselineProvenance provenance)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentNullException.ThrowIfNull(captures);
        ArgumentNullException.ThrowIfNull(platform);
        ArgumentNullException.ThrowIfNull(provenance);
        ValidatePlatform(platform);

        captures = captures.Select(NormalizeScreenshotObservations).ToArray();

        var authority = ValidateLiveProvenance(fixture, provenance);
        var expectedSteps = fixture.Steps.Select(step => step.Name).ToArray();
        if (captures.Count != fixture.Themes.Count ||
            !captures.Select(capture => capture.Theme)
                .SequenceEqual(fixture.Themes, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"Live captures for '{fixture.Id}' must contain every declared theme once " +
                "in manifest order.");
        }

        if (captures.Any(capture =>
                capture is null ||
                capture.CaptureSchemaVersion != CaptureSchema.CurrentVersion ||
                capture.Leg != ParityLeg.React ||
                !string.Equals(capture.Fixture, fixture.Id, StringComparison.Ordinal) ||
                !capture.Steps.Select(step => step.Step)
                    .SequenceEqual(expectedSteps, StringComparer.Ordinal) ||
                !string.Equals(capture.BaseUiSha, provenance.UpstreamSha, StringComparison.Ordinal) ||
                !string.Equals(capture.SourceHash, provenance.SourceHash, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                $"Live capture for '{fixture.Id}' does not match its write provenance or manifest scope.");
        }

        foreach (var capture in captures)
        {
            ValidateObservationSets(capture, allowCaptureFailures: false);
        }

        Directory.CreateDirectory(root);
        var target = SetDirectory(platform);
        var staging = Path.Combine(root, $".{platform.DirectoryName}.{Guid.NewGuid():N}.tmp");
        var backup = Path.Combine(root, $".{platform.DirectoryName}.{Guid.NewGuid():N}.bak");

        try
        {
            if (Directory.Exists(target))
            {
                CopyDirectory(target, staging);
            }
            else
            {
                Directory.CreateDirectory(staging);
            }

            var fixtures = ReadExistingFixtures(staging, authority, platform, fixture.Id);
            var replacedArtifacts = fixtures
                .Where(item => string.Equals(item.Fixture, fixture.Id, StringComparison.Ordinal))
                .SelectMany(item => item.Artifacts)
                .Select(item => item.Path)
                .ToHashSet(StringComparer.Ordinal);
            var retainedArtifacts = fixtures
                .Where(item => !string.Equals(item.Fixture, fixture.Id, StringComparison.Ordinal))
                .SelectMany(item => item.Artifacts)
                .Select(item => item.Path)
                .ToHashSet(StringComparer.Ordinal);
            foreach (var obsolete in replacedArtifacts.Except(retainedArtifacts, StringComparer.Ordinal))
            {
                var obsoletePath = ResolveArtifact(staging, obsolete);
                if (File.Exists(obsoletePath))
                {
                    File.Delete(obsoletePath);
                }
            }

            fixtures.RemoveAll(item => string.Equals(item.Fixture, fixture.Id, StringComparison.Ordinal));
            var captureHashes = new List<string>(captures.Count);

            foreach (var capture in captures)
            {
                var captureRelative =
                    $"captures/{Capture.ScreenshotSet.Slug(fixture.Id)}.{capture.Theme}.json";
                var capturePath = ResolveArtifact(staging, captureRelative);
                Directory.CreateDirectory(Path.GetDirectoryName(capturePath)!);
                WriteJson(capturePath, capture);

                var artifacts = new List<BaselineArtifact>
                {
                    Artifact(staging, captureRelative)
                };
                foreach (var screenshot in capture.Steps
                             .SelectMany(step => step.ScreenshotObservations)
                             .Where(item => item.State == ScreenshotObservationState.Captured)
                             .Select(item => item.FileName!))
                {
                    if (!IsSafeFileName(screenshot))
                    {
                        throw new InvalidOperationException(
                            $"Live capture for '{fixture.Id}@{capture.Theme}' has unsafe " +
                            $"screenshot name '{screenshot}'.");
                    }

                    var source = Path.Combine(screenshotDirectory, screenshot);
                    if (!File.Exists(source))
                    {
                        throw new FileNotFoundException(
                            $"Live baseline screenshot '{screenshot}' is missing.", source);
                    }

                    var relative = $"screenshots/{screenshot}";
                    var destination = ResolveArtifact(staging, relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    File.Copy(source, destination, overwrite: true);
                    artifacts.Add(Artifact(staging, relative));
                }

                captureHashes.Add(artifacts.Single(item => string.Equals(
                    item.Path, captureRelative, StringComparison.Ordinal)).Sha256);
                fixtures.Add(new BaselineFixtureMetadata
                {
                    Fixture = fixture.Id,
                    SourcePath = provenance.SourcePath,
                    SourceHash = provenance.SourceHash,
                    ContractHash = ContractHash(fixture),
                    Theme = capture.Theme,
                    Steps = expectedSteps,
                    Capture = captureRelative,
                    Artifacts = artifacts.OrderBy(item => item.Path, StringComparer.Ordinal).ToArray()
                });
            }

            var metadata = new BaselineSetMetadata
            {
                SchemaVersion = BaselineAuthority.CurrentSchemaVersion,
                CaptureSchemaVersion = BaselineAuthority.CurrentCaptureSchemaVersion,
                UpstreamSha = provenance.UpstreamSha,
                Platform = platform,
                GeneratedAtUtc = provenance.GeneratedAtUtc,
                FixtureManifestHash = HashFile(manifestPath),
                AliasManifestHash = HashFile(aliasManifestPath),
                StylesheetHash = HashFile(stylesheetPath),
                Fixtures = OrderFixtures(fixtures)
            };
            WriteJson(Path.Combine(staging, MetadataFileName), metadata);
            ReplaceDirectory(staging, target, backup);
            return new BaselineWriteReceipt(
                fixture.Id,
                platform,
                provenance.GeneratedAtUtc,
                HashText(string.Join('\n', captureHashes)));
        }
        finally
        {
            deleteDirectory(staging);
        }
    }

    /// <summary>Validates live source provenance without reading or mutating a platform set.</summary>
    /// <param name="provenance">The live provenance to check against repository authority.</param>
    /// <returns>The validated baseline authority.</returns>
    internal BaselineAuthority ValidateLiveProvenance(
        FixtureEntry fixture,
        LiveBaselineProvenance provenance)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentNullException.ThrowIfNull(provenance);

        var authority = LoadAuthority();
        if (!string.Equals(
                authority.DeclaredRepositoryPin,
                provenance.UpstreamSha,
                StringComparison.Ordinal))
        {
            throw Stale(
                $"declared repository pin '{authority.DeclaredRepositoryPin}' differs from " +
                $"live upstream '{provenance.UpstreamSha}'");
        }

        if (!string.Equals(
                provenance.SourcePath,
                LiveBaselineSource.ExpectedSourcePath(fixture),
                StringComparison.Ordinal) ||
            !IsUpperHex(provenance.SourceHash, 64) ||
            provenance.GeneratedAtUtc == default ||
            provenance.GeneratedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "Live provenance has an invalid source path, hash, or timestamp.");
        }

        return authority;
    }

    private static string RequireSingleTheme(FixtureEntry fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        if (fixture.Themes.Count != 1)
        {
            throw new InvalidOperationException(
                $"Fixture '{fixture.Id}' declares {fixture.Themes.Count} themes; call the " +
                "theme-aware baseline API.");
        }

        return fixture.Themes[0];
    }

    private static CaptureBundle NormalizeScreenshotObservations(CaptureBundle capture)
        => capture with
        {
            Steps = [.. capture.Steps.Select(step => step.ScreenshotObservations.Count > 0
                ? step
                : step with
                {
                    ScreenshotObservations = [.. step.Screenshots.Select(name =>
                    {
                        var shot = ScreenshotSet.Shot(
                            name, capture.Fixture, capture.Theme, capture.Leg, step.Step);
                        var root = shot[(shot.LastIndexOf('.') + 1)..];
                        var label = int.TryParse(root, out var index) && index > 0
                            ? $"portal({index})"
                            : "root";
                        return ScreenshotObservation.Captured(label, shot, name);
                    })]
                })]
        };

    private static void ValidateObservationSets(
        CaptureBundle capture,
        bool allowCaptureFailures)
    {
        foreach (var step in capture.Steps)
        {
            if (step.ScreenshotObservations is null ||
                step.ScreenshotObservations.Any(item => item is null) ||
                step.ScreenshotObservations.GroupBy(item => item.Shot, StringComparer.Ordinal)
                    .Any(group => group.Count() > 1) ||
                (!allowCaptureFailures && step.ScreenshotObservations.Any(
                    item => item.State == ScreenshotObservationState.CaptureFailed)) ||
                step.ScreenshotObservations.Any(item => item.State switch
                {
                    ScreenshotObservationState.Captured =>
                        string.IsNullOrWhiteSpace(item.FileName) || item.Detail is not null,
                    ScreenshotObservationState.NotVisible =>
                        item.FileName is not null || item.Detail is not null,
                    ScreenshotObservationState.CaptureFailed =>
                        item.FileName is not null || string.IsNullOrWhiteSpace(item.Detail),
                    _ => true
                }) ||
                step.AnimationFrameCaptureFailures is null ||
                step.AnimationFrameCaptureFailures.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Capture '{capture.Fixture}@{capture.Theme}' has invalid screenshot evidence " +
                    $"for step '{step.Step}'.");
            }
        }
    }

    private static BaselineSnapshot Snapshot(
        BaselineAuthority authority,
        BaselineSetMetadata metadata)
    {
        var fixtures = metadata.Fixtures.Select(fixture => fixture with
        {
            Steps = Array.AsReadOnly(fixture.Steps.ToArray()),
            Artifacts = Array.AsReadOnly(
                fixture.Artifacts.Select(artifact => artifact with { }).ToArray())
        }).ToArray();

        return new BaselineSnapshot(
            authority with { },
            metadata with
            {
                Platform = metadata.Platform with { },
                Fixtures = Array.AsReadOnly(fixtures)
            });
    }

    private static void ValidateDeclaredTheme(FixtureEntry fixture, string theme)
    {
        if (!fixture.Themes.Contains(theme, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"Theme '{theme}' is not declared by fixture '{fixture.Id}'.");
        }
    }

    private BaselineAuthority LoadAuthority()
    {
        var authority = Read<BaselineAuthority>(Path.Combine(root, MetadataFileName));
        if (authority.SchemaVersion != BaselineAuthority.CurrentSchemaVersion ||
            authority.CaptureSchemaVersion != BaselineAuthority.CurrentCaptureSchemaVersion ||
            !IsHex(authority.DeclaredRepositoryPin, 40))
        {
            throw Stale("baseline authority schema or declared repository pin is invalid");
        }

        return authority;
    }

    private void ValidateSet(
        BaselineAuthority authority,
        BaselineSetMetadata metadata,
        BaselinePlatform platform,
        bool requireComplete,
        string? replacementFixture = null)
    {
        if (metadata.Platform is null || metadata.Fixtures is null ||
            metadata.SchemaVersion != authority.SchemaVersion ||
            metadata.CaptureSchemaVersion != authority.CaptureSchemaVersion)
        {
            throw Stale("baseline-set schema does not match authority");
        }

        if (!string.Equals(metadata.UpstreamSha, authority.DeclaredRepositoryPin, StringComparison.Ordinal))
        {
            throw Stale("baseline provenance does not match the declared repository pin");
        }

        if (metadata.Platform != platform)
        {
            throw Stale(
                $"baseline platform '{metadata.Platform}' does not exactly match '{platform}'");
        }

        var manifestHashMatches = string.Equals(
            metadata.FixtureManifestHash,
            HashFile(manifestPath),
            StringComparison.Ordinal);
        if ((!manifestHashMatches && replacementFixture is null) ||
            !string.Equals(metadata.AliasManifestHash, HashFile(aliasManifestPath), StringComparison.Ordinal) ||
            !string.Equals(metadata.StylesheetHash, HashFile(stylesheetPath), StringComparison.Ordinal))
        {
            throw Stale("fixture manifest, alias manifest, or shared stylesheet changed");
        }

        if (metadata.GeneratedAtUtc == default ||
            metadata.GeneratedAtUtc.Offset != TimeSpan.Zero ||
            !IsUpperHex(metadata.FixtureManifestHash, 64) ||
            !IsUpperHex(metadata.AliasManifestHash, 64) ||
            !IsUpperHex(metadata.StylesheetHash, 64) ||
            metadata.Fixtures.Any(item => item is null) ||
            metadata.Fixtures.Select(item => $"{item.Fixture}@{item.Theme}")
                .Distinct(StringComparer.Ordinal).Count() != metadata.Fixtures.Count)
        {
            throw Stale("baseline-set provenance or fixture identity is invalid");
        }

        foreach (var fixture in metadata.Fixtures)
        {
            if (fixture is null ||
                string.IsNullOrWhiteSpace(fixture.Fixture) ||
                !IsSafeRelativePosixPath(fixture.SourcePath) ||
                !IsUpperHex(fixture.SourceHash, 64) ||
                !IsUpperHex(fixture.ContractHash, 64) ||
                fixture.Theme is not ("light" or "dark") ||
                fixture.Steps is null ||
                fixture.Artifacts is null ||
                fixture.Steps.Count == 0 ||
                fixture.Steps.Any(string.IsNullOrWhiteSpace))
            {
                throw Stale(
                    $"fixture '{fixture?.Fixture ?? "<null>"}' provenance or capture scope is invalid");
            }
        }

        var retained = replacementFixture is null
            ? metadata.Fixtures
            : metadata.Fixtures
                .Where(item => !string.Equals(
                    item.Fixture, replacementFixture, StringComparison.Ordinal))
                .ToArray();
        var ordered = OrderFixtures(retained);
        if (!retained.SequenceEqual(ordered))
        {
            throw Stale("baseline fixture-theme entries are not in manifest order");
        }

        var currentFixtures = FixtureManifest.Parse(File.ReadAllText(manifestPath))
            .ToDictionary(item => item.Id, StringComparer.Ordinal);
        foreach (var retainedFixture in retained)
        {
            if (!currentFixtures.TryGetValue(retainedFixture.Fixture, out var current) ||
                !current.Themes.Contains(retainedFixture.Theme, StringComparer.Ordinal) ||
                !string.Equals(
                    retainedFixture.ContractHash,
                    ContractHash(current),
                    StringComparison.Ordinal))
            {
                throw Stale(
                    $"retained fixture '{retainedFixture.Fixture}@{retainedFixture.Theme}' " +
                    "contract differs from the manifest");
            }
        }

        if (requireComplete)
        {
            var expected = FixtureManifest.Parse(File.ReadAllText(manifestPath))
                .SelectMany(entry => entry.Themes.Select(theme => $"{entry.Id}@{theme}"));
            var actual = metadata.Fixtures.Select(entry => $"{entry.Fixture}@{entry.Theme}");
            if (!actual.SequenceEqual(expected, StringComparer.Ordinal))
            {
                throw Stale("baseline set does not cover every manifest fixture-theme execution");
            }
        }
    }

    private List<BaselineFixtureMetadata> ReadExistingFixtures(
        string staging,
        BaselineAuthority authority,
        BaselinePlatform platform,
        string replacementFixture)
    {
        var metadataPath = Path.Combine(staging, MetadataFileName);
        if (!File.Exists(metadataPath))
        {
            return [];
        }

        var metadata = Read<BaselineSetMetadata>(metadataPath);
        ValidateSet(
            authority,
            metadata,
            platform,
            requireComplete: false,
            replacementFixture: replacementFixture);
        foreach (var fixture in metadata.Fixtures)
        {
            ValidateArtifacts(staging, fixture.Artifacts);
        }

        return [.. metadata.Fixtures];
    }

    private IReadOnlyList<BaselineFixtureMetadata> OrderFixtures(
        IEnumerable<BaselineFixtureMetadata> fixtures)
    {
        var manifestOrder = FixtureManifest.Parse(File.ReadAllText(manifestPath))
            .SelectMany(entry => entry.Themes.Select(theme => $"{entry.Id}@{theme}"))
            .Select((executionId, index) => (executionId, index))
            .ToDictionary(item => item.executionId, item => item.index, StringComparer.Ordinal);
        var materialized = fixtures.ToArray();
        var unknown = materialized
            .Select(item => $"{item.Fixture}@{item.Theme}")
            .Where(executionId => !manifestOrder.ContainsKey(executionId))
            .ToArray();
        if (unknown.Length > 0)
        {
            throw Stale($"unknown fixture-theme entries: {string.Join(", ", unknown)}");
        }

        return materialized
            .OrderBy(item => manifestOrder[$"{item.Fixture}@{item.Theme}"])
            .ToArray();
    }

    private static T Read<T>(string path)
    {
        if (!File.Exists(path))
        {
            throw Stale($"required file '{path}' is missing");
        }

        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), SerializerOptions)
                ?? throw Stale($"file '{path}' contains null");
        }
        catch (JsonException exception)
        {
            throw Stale($"file '{path}' is corrupt: {exception.Message}");
        }
    }

    private static void WriteJson<T>(string path, T value)
    {
        var temp = path + ".tmp-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(value, SerializerOptions) + Environment.NewLine);
            File.Move(temp, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temp))
            {
                File.Delete(temp);
            }
        }
    }

    private static void ValidateArtifacts(
        string setDirectory,
        IReadOnlyList<BaselineArtifact>? artifacts)
    {
        if (artifacts is null || artifacts.Count == 0 ||
            artifacts.Any(item => item is null) ||
            artifacts.Select(item => item.Path).Distinct(StringComparer.Ordinal).Count() != artifacts.Count)
        {
            throw Stale("artifact inventory is empty or duplicated");
        }

        foreach (var artifact in artifacts)
        {
            if (!IsUpperHex(artifact.Sha256, 64))
            {
                throw Stale($"artifact '{artifact.Path}' has an invalid SHA-256");
            }

            var path = ResolveArtifact(setDirectory, artifact.Path);
            if (!File.Exists(path) || !string.Equals(HashFile(path), artifact.Sha256, StringComparison.Ordinal))
            {
                throw Stale($"artifact '{artifact.Path}' is missing or has a stale hash");
            }
        }
    }

    private static BaselineArtifact Artifact(string setDirectory, string relative)
        => new() { Path = relative, Sha256 = HashFile(ResolveArtifact(setDirectory, relative)) };

    private static string ResolveArtifact(string setDirectory, string relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative) || relative.Contains('\\'))
        {
            throw Stale($"artifact path '{relative}' is not a relative POSIX path");
        }

        var full = Path.GetFullPath(Path.Combine(setDirectory, relative));
        var prefix = Path.GetFullPath(setDirectory) + Path.DirectorySeparatorChar;
        if (!full.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw Stale($"artifact path '{relative}' escapes its baseline set");
        }

        return full;
    }

    private static bool IsSafeFileName(string? value)
        => !string.IsNullOrWhiteSpace(value) &&
           string.Equals(Path.GetFileName(value), value, StringComparison.Ordinal) &&
           !value.Contains('\\');

    private static bool IsSafeRelativePosixPath(string? value)
        => !string.IsNullOrWhiteSpace(value) &&
           !Path.IsPathRooted(value) &&
           !value.Contains('\\') &&
           value.Split('/').All(segment => segment is not ("" or "." or ".."));

    private static bool IsUpperHex(string? value, int length)
        => value is not null && value.Length == length && value.All(character =>
            character is >= '0' and <= '9' or >= 'A' and <= 'F');

    private static bool IsHex(string? value, int length)
        => value is not null && value.Length == length && value.All(Uri.IsHexDigit);

    private static void ValidatePlatform(BaselinePlatform platform)
    {
        var hasCanonicalVersion = Version.TryParse(platform.BrowserVersion, out var browserVersion) &&
                                  browserVersion.Build >= 0 &&
                                  browserVersion.Revision >= 0 &&
                                  string.Equals(
                                      browserVersion.ToString(),
                                      platform.BrowserVersion,
                                      StringComparison.Ordinal);
        if (platform.Browser is not ("chromium" or "firefox" or "webkit") ||
            platform.Os is not ("linux" or "macos" or "windows") ||
            platform.Architecture is not ("x64" or "arm64") ||
            !hasCanonicalVersion)
        {
            throw Stale("baseline platform selector is invalid");
        }
    }

    private static string HashFile(string path)
    {
        if (!File.Exists(path))
        {
            throw Stale($"required provenance file '{path}' is missing");
        }

        return Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    }

    private static string HashText(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private string ContractHash(FixtureEntry fixture)
        => HashText(
            JsonSerializer.Serialize(fixture, SerializerOptions) + "\n" +
            HashFile(aliasManifestPath));

    private static InvalidOperationException Stale(string reason)
        => new($"Baselines stale: {reason}. Run `{RefreshCommand}` locally.");

    private string SetDirectory(BaselinePlatform platform)
        => Path.Combine(root, platform.DirectoryName);

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    private void ReplaceDirectory(string staging, string target, string backup)
    {
        if (Directory.Exists(target))
        {
            Directory.Move(target, backup);
        }

        try
        {
            Directory.Move(staging, target);
            deleteDirectory(backup);
        }
        catch
        {
            if (Directory.Exists(backup))
            {
                if (Directory.Exists(target))
                {
                    Directory.Move(target, staging);
                }

                Directory.Move(backup, target);
            }

            throw;
        }
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}

/// <summary>Reads live source provenance only for explicit live/write invocations.</summary>
internal static class LiveBaselineSource
{
    internal static string ExpectedSourcePath(FixtureEntry fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        var relative = fixture.React.StartsWith("internal:", StringComparison.Ordinal)
            ? $"react-fixtures/src/{fixture.React["internal:".Length..]}.tsx"
            : $"docs/src/app/(docs)/react/components/{fixture.React}";

        if (!IsSafeRelativePosixPath(relative))
        {
            throw new InvalidOperationException(
                $"React source path for '{fixture.Id}' is not deterministic: '{relative}'.");
        }

        return relative;
    }

    internal static LiveBaselineProvenance Read(FixtureEntry fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        var baseUi = BaseUiLocator.Locate();
        var relative = ExpectedSourcePath(fixture);
        var root = fixture.React.StartsWith("internal:", StringComparison.Ordinal)
            ? ParityPaths.HarnessRoot
            : baseUi;
        var sourcePath = Path.GetFullPath(Path.Combine(
            root,
            relative.Replace('/', Path.DirectorySeparatorChar)));
        var rootPrefix = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        if (!sourcePath.StartsWith(rootPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"React source for '{fixture.Id}' escapes its declared source root.");
        }

        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException(
                $"React source for '{fixture.Id}' is missing: '{sourcePath}'.",
                sourcePath);
        }

        var start = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = baseUi,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        start.ArgumentList.Add("rev-parse");
        start.ArgumentList.Add("HEAD");

        using var process = Process.Start(start)
            ?? throw new InvalidOperationException("Could not start git for Base UI provenance.");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
        {
            throw new InvalidOperationException(
                $"Could not read the Base UI pin: {error.Trim()}".TrimEnd());
        }

        return new LiveBaselineProvenance(
            output.Trim(),
            relative,
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(sourcePath))),
            ReactBundleProvenance.GeneratedAtUtc(ParityPaths.ReactDist));
    }

    private static bool IsSafeRelativePosixPath(string value)
        => !string.IsNullOrWhiteSpace(value) &&
           !Path.IsPathRooted(value) &&
           !value.Contains('\\') &&
           value.Split('/').All(segment => segment is not ("" or "." or ".."));
}
