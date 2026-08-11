using System.Text.Json;
using System.Text.Json.Nodes;
using Blazix.BaseUI.Parity.Tests.Baselines;
using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Infrastructure;
using Blazix.BaseUI.Parity.Tests.Diff;
using Blazix.BaseUI.Parity.Tests.Fixtures;
using Shouldly;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

/// <summary>Pins baseline authority, provenance, platform, and atomic storage policy.</summary>
[Collection(ParityTimingCollection.Name)]
public sealed class BaselineStoreTests : IDisposable
{
    private const string Pin = "bdcb685fadcca9d18b18f013c052795a53b6aa33";
    private const string SourceHash =
        "AABBCCDDAABBCCDDAABBCCDDAABBCCDDAABBCCDDAABBCCDDAABBCCDDAABBCCDD";

    private readonly string root = Path.Combine(
        Path.GetTempPath(), "blazix-baselines", Guid.NewGuid().ToString("N"));

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData(null, null, ParityReferenceMode.Baseline)]
    [InlineData("1", null, ParityReferenceMode.Live)]
    [InlineData(null, "1", ParityReferenceMode.WriteBaseline)]
    [InlineData("1", "1", ParityReferenceMode.WriteBaseline)]
    public void ParsesTheExactEnvironmentModeMatrix(
        string? live,
        string? write,
        ParityReferenceMode expected)
    {
        var values = new Dictionary<string, string?>
        {
            ["PARITY_LIVE"] = live,
            ["PARITY_WRITE_BASELINES"] = write,
            ["PARITY_FIXTURES"] = "switch/*"
        };

        var options = ParityOptions.FromEnvironment(name => values.GetValueOrDefault(name));

        options.Mode.ShouldBe(expected);
        options.FixtureFilter.ShouldBe("switch/*");
    }

    [Theory]
    [InlineData("PARITY_LIVE", "true")]
    [InlineData("PARITY_WRITE_BASELINES", "0")]
    [InlineData("PARITY_FIXTURES", " ")]
    public void RejectsAmbiguousEnvironmentValues(string name, string value)
    {
        var exception = Should.Throw<FormatException>(() =>
            ParityOptions.FromEnvironment(candidate => candidate == name ? value : null));
        exception.Message.ShouldContain(name);
    }

    [Fact]
    public void WriteReadRoundTripPreservesCaptureAndMaterializesScreenshots()
    {
        var harness = Arrange();
        var capture = Capture();

        var receipt = harness.Store.Write(
            harness.Fixture,
            capture,
            harness.Platform,
            Provenance());
        File.Delete(Path.Combine(harness.RunScreenshots, Shot()));

        var loaded = harness.Store.Load(harness.Fixture, harness.Platform);

        loaded.Fixture.ShouldBe(capture.Fixture);
        loaded.Leg.ShouldBe(capture.Leg);
        loaded.BaseUiSha.ShouldBe(capture.BaseUiSha);
        loaded.SourceHash.ShouldBe(capture.SourceHash);
        loaded.Theme.ShouldBe("light");
        loaded.Steps.Select(item => item.Step).ShouldBe(["initial", "toggle-off"]);
        loaded.Steps[0].Screenshots.ShouldBe([Shot()]);
        File.ReadAllText(Path.Combine(harness.RunScreenshots, Shot()))
            .ShouldBe("switch/hero@light pixels");
        var metadata = File.ReadAllText(Path.Combine(
            harness.Baselines, harness.Platform.DirectoryName, "metadata.json"));
        metadata.ShouldContain(Pin);
        metadata.ShouldNotContain("provisional");
        metadata.ShouldContain(SourceHash);
        metadata.ShouldContain("\"theme\": \"light\"");
        metadata.ShouldContain("switch/hero");
        metadata.ShouldContain("toggle-off");
        receipt.Fixture.ShouldBe(harness.Fixture.Id);
        receipt.Platform.ShouldBe(harness.Platform);
        receipt.GeneratedAtUtc.ShouldBe(Provenance().GeneratedAtUtc);
        receipt.CaptureSha256.Length.ShouldBe(64);
        receipt.CaptureSha256.All(character =>
                character is >= '0' and <= '9' or >= 'A' and <= 'F')
            .ShouldBeTrue();
    }

    [Fact]
    public void BaselineReadNeedsNeitherBaseUiNorReactDistNorNode()
    {
        var harness = Arrange();
        var capture = Capture();
        harness.Store.Write(harness.Fixture, capture, harness.Platform, Provenance());
        var previous = Environment.GetEnvironmentVariable("PARITY_BASE_UI_PATH");

        try
        {
            Environment.SetEnvironmentVariable(
                "PARITY_BASE_UI_PATH", Path.Combine(root, "definitely-missing-base-ui"));
            Directory.Exists(Path.Combine(root, "react-fixtures", "dist")).ShouldBeFalse();

            var loaded = harness.Store.Load(harness.Fixture, harness.Platform);

            loaded.SourceHash.ShouldBe(SourceHash);
            loaded.BaseUiSha.ShouldBe(Pin);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PARITY_BASE_UI_PATH", previous);
        }
    }

    [Fact]
    public void DescribeReturnsAValidatedReadOnlySnapshotWithoutMaterializingCaptures()
    {
        var harness = Arrange("describe");
        harness.Store.Write(harness.Fixture, Capture(), harness.Platform, Provenance());
        var materializedScreenshot = Path.Combine(harness.RunScreenshots, Shot());
        File.Delete(materializedScreenshot);

        var snapshot = harness.Store.Describe(harness.Platform);

        snapshot.Authority.DeclaredRepositoryPin.ShouldBe(Pin);
        snapshot.Set.UpstreamSha.ShouldBe(Pin);
        snapshot.Set.Platform.ShouldBe(harness.Platform);
        var fixture = snapshot.Set.Fixtures.ShouldHaveSingleItem();
        fixture.Fixture.ShouldBe("switch/hero");
        fixture.Theme.ShouldBe("light");
        fixture.Steps.ShouldBe(["initial", "toggle-off"]);
        fixture.Artifacts.Select(artifact => artifact.Path).ShouldBe(
        [
            "captures/switch__hero.light.json",
            $"screenshots/{Shot()}"
        ]);
        snapshot.Set.Fixtures.ShouldBeAssignableTo<IList<BaselineFixtureMetadata>>()
            .IsReadOnly.ShouldBeTrue();
        fixture.Steps.ShouldBeAssignableTo<IList<string>>().IsReadOnly.ShouldBeTrue();
        fixture.Artifacts.ShouldBeAssignableTo<IList<BaselineArtifact>>().IsReadOnly.ShouldBeTrue();
        typeof(BaselineSnapshot).GetConstructors().ShouldBeEmpty();
        File.Exists(materializedScreenshot).ShouldBeFalse();
    }

    [Theory]
    [InlineData("authority")]
    [InlineData("set")]
    [InlineData("capture")]
    [InlineData("screenshot")]
    public void DescribeRejectsAuthoritySetAndEveryArtifactHashTampering(string target)
    {
        var harness = Arrange($"describe-{target}");
        harness.Store.Write(harness.Fixture, Capture(), harness.Platform, Provenance());
        var setDirectory = Path.Combine(harness.Baselines, harness.Platform.DirectoryName);

        switch (target)
        {
            case "authority":
                var authorityPath = Path.Combine(harness.Baselines, "metadata.json");
                var authority = JsonNode.Parse(File.ReadAllText(authorityPath))!.AsObject();
                authority["schemaVersion"] = BaselineAuthority.CurrentSchemaVersion + 1;
                File.WriteAllText(authorityPath, authority.ToJsonString());
                break;
            case "set":
                var metadataPath = Path.Combine(setDirectory, "metadata.json");
                var metadata = JsonNode.Parse(File.ReadAllText(metadataPath))!.AsObject();
                metadata["upstreamSha"] = new string('a', 40);
                File.WriteAllText(metadataPath, metadata.ToJsonString());
                break;
            case "capture":
                File.AppendAllText(
                    Path.Combine(setDirectory, "captures", "switch__hero.light.json"),
                    "tampered");
                break;
            case "screenshot":
                File.AppendAllText(
                    Path.Combine(setDirectory, "screenshots", Shot()),
                    "tampered");
                break;
        }

        var failure = Should.Throw<InvalidOperationException>(() =>
            harness.Store.Describe(harness.Platform));

        failure.Message.ShouldContain("Baselines stale");
        failure.Message.ShouldContain("pnpm parity:baseline");
    }

    [Fact]
    public void MissingAndCorruptMetadataBlockWithRefreshCommand()
    {
        var harness = Arrange();

        var missing = Should.Throw<InvalidOperationException>(() =>
            harness.Store.Load(harness.Fixture, harness.Platform));
        missing.Message.ShouldContain("missing");
        missing.Message.ShouldContain("pnpm parity:baseline");

        File.WriteAllText(Path.Combine(harness.Baselines, "metadata.json"), "{");
        var corrupt = Should.Throw<InvalidOperationException>(() =>
            harness.Store.Load(harness.Fixture, harness.Platform));
        corrupt.Message.ShouldContain("corrupt");
        corrupt.Message.ShouldContain("pnpm parity:baseline");
    }

    [Fact]
    public void SchemaMismatchAndPinProvenanceInconsistencyBlock()
    {
        var harness = Arrange();
        harness.Store.Write(harness.Fixture, Capture(), harness.Platform, Provenance());
        var authorityPath = Path.Combine(harness.Baselines, "metadata.json");

        File.WriteAllText(authorityPath, AuthorityJson(schemaVersion: 99));
        Should.Throw<InvalidOperationException>(() =>
                harness.Store.Load(harness.Fixture, harness.Platform))
            .Message.ShouldContain("schema");

        File.WriteAllText(authorityPath, AuthorityJson(pin: new string('f', 40)));
        Should.Throw<InvalidOperationException>(() =>
                harness.Store.Load(harness.Fixture, harness.Platform))
            .Message.ShouldContain("declared repository pin");
    }

    [Fact]
    public void StaleSourceHashAndArtifactHashBlock()
    {
        var harness = Arrange();
        harness.Store.Write(harness.Fixture, Capture(), harness.Platform, Provenance());
        var set = Path.Combine(harness.Baselines, harness.Platform.DirectoryName);
        var capturePath = Path.Combine(set, "captures", "switch__hero.light.json");

        File.AppendAllText(capturePath, " ");
        var artifact = Should.Throw<InvalidOperationException>(() =>
            harness.Store.Load(harness.Fixture, harness.Platform));
        artifact.Message.ShouldContain("stale hash");

        harness = Arrange("source-mismatch");
        harness.Store.Write(harness.Fixture, Capture(), harness.Platform, Provenance());
        var metadataPath = Path.Combine(
            harness.Baselines, harness.Platform.DirectoryName, "metadata.json");
        var json = File.ReadAllText(metadataPath).Replace(SourceHash, "DEADBEEF", StringComparison.Ordinal);
        File.WriteAllText(metadataPath, json);
        var mismatch = Should.Throw<InvalidOperationException>(() =>
            harness.Store.Load(harness.Fixture, harness.Platform));
        mismatch.Message.ShouldContain("provenance or capture scope");
    }

    [Fact]
    public void ManifestAliasAndStylesheetHashesMustRemainCurrent()
    {
        var harness = Arrange();
        harness.Store.Write(harness.Fixture, Capture(), harness.Platform, Provenance());

        File.AppendAllText(harness.Manifest, " ");
            Should.Throw<InvalidOperationException>(() =>
                harness.Store.Load(harness.Fixture, harness.Platform))
            .Message.ShouldContain("fixture manifest");

        harness = Arrange("alias-mismatch");
        harness.Store.Write(harness.Fixture, Capture(), harness.Platform, Provenance());
        File.AppendAllText(
            Path.Combine(Path.GetDirectoryName(harness.Manifest)!, "aliases.json"),
            " ");
        Should.Throw<InvalidOperationException>(() =>
                harness.Store.Load(harness.Fixture, harness.Platform))
            .Message.ShouldContain("alias manifest");

        harness = Arrange("style-mismatch");
        harness.Store.Write(harness.Fixture, Capture(), harness.Platform, Provenance());
        File.AppendAllText(harness.Stylesheet, " ");
            Should.Throw<InvalidOperationException>(() =>
                harness.Store.Load(harness.Fixture, harness.Platform))
            .Message.ShouldContain("shared stylesheet");
    }

    [Fact]
    public void ExactPlatformSelectionHasNoOsArchitectureOrBrowserVersionFallback()
    {
        var harness = Arrange();
        harness.Store.Write(harness.Fixture, Capture(), harness.Platform, Provenance());

        foreach (var other in new[]
                 {
                     harness.Platform with { Os = "linux" },
                     harness.Platform with { Architecture = "x64" },
                     harness.Platform with { BrowserVersion = "999.0" }
                 })
        {
            var exception = Should.Throw<InvalidOperationException>(() =>
                harness.Store.Load(harness.Fixture, other));
            exception.Message.ShouldContain("Baselines stale");
            exception.Message.ShouldContain("pnpm parity:baseline");
        }
    }

    [Fact]
    public void FailedReplacementLeavesThePreviousBaselineReadable()
    {
        var harness = Arrange();
        var first = Capture();
        harness.Store.Write(harness.Fixture, first, harness.Platform, Provenance());
        File.Delete(Path.Combine(harness.RunScreenshots, Shot()));

        var replacement = Capture();
        Should.Throw<FileNotFoundException>(() =>
            harness.Store.Write(harness.Fixture, replacement, harness.Platform, Provenance()));

        var loaded = harness.Store.Load(harness.Fixture, harness.Platform);
        loaded.BaseUiSha.ShouldBe(first.BaseUiSha);
        loaded.SourceHash.ShouldBe(first.SourceHash);
        loaded.Steps.Select(item => item.Step).ShouldBe(["initial", "toggle-off"]);
    }

    [Fact]
    public void BackupDeletionFailureRollsBackToTheExactOldSet()
    {
        var failBackupDelete = false;
        var harness = Arrange(
            "backup-delete",
            path =>
            {
                if (failBackupDelete && path.Contains(".bak", StringComparison.Ordinal))
                {
                    throw new IOException("backup delete probe");
                }

                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }
            });
        harness.Store.Write(harness.Fixture, Capture(), harness.Platform, Provenance());
        var capturePath = Path.Combine(
            harness.Baselines,
            harness.Platform.DirectoryName,
            "captures",
            "switch__hero.light.json");
        var before = File.ReadAllText(capturePath);
        failBackupDelete = true;

        var exception = Should.Throw<IOException>(() =>
            harness.Store.Write(
                harness.Fixture,
                Capture() with
                {
                    Steps = [Step("initial"), Step("toggle-off")]
                },
                harness.Platform,
                Provenance()));

        exception.Message.ShouldContain("backup delete probe");
        File.ReadAllText(capturePath).ShouldBe(before);
        harness.Store.Load(harness.Fixture, harness.Platform).Steps[0].Screenshots
            .ShouldBe([Shot()]);
    }

    [Fact]
    public void SchemaMigrationBackupDeletionFailureRestoresTheOldRoot()
    {
        var failBackupDelete = false;
        var harness = Arrange("schema3-root-rollback", path =>
        {
            if (failBackupDelete && path.Contains(".bak", StringComparison.Ordinal))
            {
                throw new IOException("schema3 root backup delete probe");
            }
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        });
        harness.Store.Write(harness.Fixture, Capture(), harness.Platform, Provenance());
        File.WriteAllText(Path.Combine(harness.Baselines, "metadata.json"), AuthorityJson(captureSchemaVersion: 2));
        var before = Snapshot(harness.Baselines);
        var migration = harness.Store.CreateSchemaMigrationStaging();
        migration.Store.Write(harness.Fixture, Capture(), harness.Platform, Provenance());
        failBackupDelete = true;

        var exception = Should.Throw<IOException>(() =>
            harness.Store.ReplaceWithValidatedStaging(migration.Root, harness.Platform));

        exception.Message.ShouldContain("schema3 root backup delete probe");
        Snapshot(harness.Baselines).ShouldBe(before);
        Directory.Exists(migration.Root).ShouldBeTrue();
    }

    [Fact]
    public void SchemaMigrationAuthorityWriteFailureRemovesItsPartialStagingRoot()
    {
        var harness = Arrange("schema3-early-staging-cleanup");
        File.WriteAllText(
            Path.Combine(harness.Baselines, "metadata.json"),
            AuthorityJson(captureSchemaVersion: 2));
        var parent = Path.GetDirectoryName(harness.Baselines)!;
        var before = Directory.EnumerateDirectories(parent).ToHashSet(StringComparer.Ordinal);

        Should.Throw<IOException>(() => harness.Store.CreateSchemaMigrationStaging((path, _) =>
        {
            File.WriteAllText(path, "partial");
            throw new IOException("authority write probe");
        }));

        Directory.EnumerateDirectories(parent).ToHashSet(StringComparer.Ordinal).ShouldBe(before);
    }

    [Fact]
    public async Task PostStagingGeneratorFailureRemovesTheSuccessfulStagingRoot()
    {
        var harness = Arrange("schema3-generator-post-create-cleanup");
        File.WriteAllText(
            Path.Combine(harness.Baselines, "metadata.json"),
            AuthorityJson(captureSchemaVersion: 2));
        var parent = Path.GetDirectoryName(harness.Baselines)!;
        var before = Directory.EnumerateDirectories(parent).ToHashSet(StringComparer.Ordinal);
        var operationReached = false;

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            BaselineGenerator.WithMigrationStoreAsync<int>(
                harness.Store,
                stageCompleteSet: true,
                (_, stagingRoot) =>
                {
                    operationReached = true;
                    Directory.Exists(stagingRoot).ShouldBeTrue();
                    throw new InvalidOperationException("runner construction probe");
                }));

        operationReached.ShouldBeTrue();
        exception.Message.ShouldBe("runner construction probe");
        Directory.EnumerateDirectories(parent).ToHashSet(StringComparer.Ordinal).ShouldBe(before);
    }

    [Fact]
    public void BaselineWriteAndLoadRejectAnimationReplayFailures()
    {
        var harness = Arrange("animation-replay-failure");
        harness.Store.Write(harness.Fixture, Capture(), harness.Platform, Provenance());
        var before = Snapshot(harness.Baselines);
        var invalid = Capture() with
        {
            Steps =
            [
                Capture().Steps[0] with
                {
                    AnimationFrameCaptureFailures =
                    [
                        new AnimationFrameCaptureFailure
                        {
                            Stage = "navigate",
                            ActionIndex = 0,
                            Detail = "probe"
                        }
                    ]
                },
                Capture().Steps[1]
            ]
        };

        ParityRunner.CanWriteReference(invalid).ShouldBeFalse();

        Should.Throw<InvalidOperationException>(() =>
            harness.Store.Write(harness.Fixture, invalid, harness.Platform, Provenance()));
        Snapshot(harness.Baselines).ShouldBe(before);

        var set = Path.Combine(harness.Baselines, harness.Platform.DirectoryName);
        var capturePath = Path.Combine(set, "captures", "switch__hero.light.json");
        File.WriteAllText(capturePath, CaptureSchema.Serialize(invalid));
        var metadataPath = Path.Combine(set, "metadata.json");
        var metadata = JsonNode.Parse(File.ReadAllText(metadataPath))!;
        var captureArtifact = metadata["fixtures"]![0]!["artifacts"]!.AsArray()
            .Single(node => node!["path"]!.GetValue<string>() == "captures/switch__hero.light.json")!;
        captureArtifact["sha256"] = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(capturePath)));
        File.WriteAllText(metadataPath, metadata.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        Should.Throw<InvalidOperationException>(() =>
            harness.Store.Load(harness.Fixture, harness.Platform));
    }

    [Fact]
    public void WriteRejectsLivePinMismatchBeforeMutatingTheSet()
    {
        var harness = Arrange();
        var mismatch = Provenance() with { UpstreamSha = new string('a', 40) };

        var exception = Should.Throw<InvalidOperationException>(() =>
            harness.Store.Write(harness.Fixture, Capture(), harness.Platform, mismatch));

        exception.Message.ShouldContain("declared repository pin");
        Directory.Exists(Path.Combine(harness.Baselines, harness.Platform.DirectoryName))
            .ShouldBeFalse();
    }

    [Fact]
    public void UnsafeSourcePathsAndMalformedSourceHashesBlockBeforeMutation()
    {
        var harness = Arrange();

        var unsafePath = Should.Throw<InvalidOperationException>(() =>
            harness.Store.Write(
                harness.Fixture,
                Capture(),
                harness.Platform,
                Provenance() with { SourcePath = "../machine-secret.tsx" }));
        unsafePath.Message.ShouldContain("source path, hash, or timestamp");

        var malformedHash = Should.Throw<InvalidOperationException>(() =>
            harness.Store.Write(
                harness.Fixture,
                Capture() with { SourceHash = "AABBCCDD" },
                harness.Platform,
                Provenance() with { SourceHash = "AABBCCDD" }));
        malformedHash.Message.ShouldContain("source path, hash, or timestamp");
        Directory.Exists(Path.Combine(harness.Baselines, harness.Platform.DirectoryName))
            .ShouldBeFalse();
    }

    [Fact]
    public void StrictMetadataRejectsDuplicateNamesNonCanonicalVersionsAndNonUtcTimestamps()
    {
        var harness = Arrange();
        File.WriteAllText(
            Path.Combine(harness.Baselines, "metadata.json"),
            AuthorityJson().Replace(
                $"\"schemaVersion\":{BaselineAuthority.CurrentSchemaVersion}",
                $"\"schemaVersion\":{BaselineAuthority.CurrentSchemaVersion}," +
                $"\"schemaVersion\":{BaselineAuthority.CurrentSchemaVersion}",
                StringComparison.Ordinal));
        Should.Throw<InvalidOperationException>(() =>
                harness.Store.Load(harness.Fixture, harness.Platform))
            .Message.ShouldContain("duplicate", Case.Insensitive);

        harness = Arrange("strict-version");
        Should.Throw<InvalidOperationException>(() =>
            harness.Store.Write(
                harness.Fixture,
                Capture(),
                harness.Platform with { BrowserVersion = "140..0" },
                Provenance()));

        harness = Arrange("strict-time");
        Should.Throw<InvalidOperationException>(() =>
            harness.Store.Write(
                harness.Fixture,
                Capture(),
                harness.Platform,
                Provenance() with
                {
                    GeneratedAtUtc = new DateTimeOffset(
                        2026, 8, 9, 8, 0, 0, TimeSpan.FromHours(8))
                }));

        harness = Arrange("strict-source-path");
        harness.Store.Write(harness.Fixture, Capture(), harness.Platform, Provenance());
        var metadataPath = Path.Combine(
            harness.Baselines, harness.Platform.DirectoryName, "metadata.json");
        File.WriteAllText(
            metadataPath,
            File.ReadAllText(metadataPath).Replace(
                LiveBaselineSource.ExpectedSourcePath(harness.Fixture),
                "docs/src/app/(docs)/react/components/switch/demos/other/tailwind/index.tsx",
                StringComparison.Ordinal));
        Should.Throw<InvalidOperationException>(() =>
                harness.Store.Load(harness.Fixture, harness.Platform))
            .Message.ShouldContain("theme or step scope");
    }

    [Fact]
    public void RejectsNonCurrentCaptureSchemaOnWriteAndLoad()
    {
        var harness = Arrange("capture-schema");
        Should.Throw<InvalidOperationException>(() =>
            harness.Store.Write(
                harness.Fixture,
                Capture() with { CaptureSchemaVersion = 1 },
                harness.Platform,
                Provenance()));

        harness.Store.Write(
            harness.Fixture,
            Capture(),
            harness.Platform,
            Provenance());
        var capturePath = Path.Combine(
            harness.Baselines,
            harness.Platform.DirectoryName,
            "captures",
            "switch__hero.light.json");
        File.WriteAllText(
            capturePath,
            File.ReadAllText(capturePath).Replace(
                $"\"captureSchemaVersion\": {CaptureSchema.CurrentVersion}",
                "\"captureSchemaVersion\": 1",
                StringComparison.Ordinal));

        Should.Throw<InvalidOperationException>(() =>
            harness.Store.Load(harness.Fixture, "light", harness.Platform));
    }

    [Theory]
    [InlineData("0143.0.7499.4")]
    [InlineData("143.0.7499")]
    public void RejectsParseableButNoncanonicalBrowserVersions(string browserVersion)
    {
        Version.TryParse(browserVersion, out _).ShouldBeTrue();
        var harness = Arrange($"noncanonical-{browserVersion.Replace('.', '-')}");

        var exception = Should.Throw<InvalidOperationException>(() =>
            harness.Store.Write(
                harness.Fixture,
                Capture(),
                harness.Platform with { BrowserVersion = browserVersion },
                Provenance()));

        exception.Message.ShouldContain("platform selector");
    }

    [Fact]
    public void LoadRejectsACommittedMetadataTimestampWithANonUtcOffset()
    {
        var harness = Arrange("nonutc-load");
        harness.Store.Write(harness.Fixture, Capture(), harness.Platform, Provenance());
        var metadataPath = Path.Combine(
            harness.Baselines, harness.Platform.DirectoryName, "metadata.json");
        var metadata = JsonNode.Parse(File.ReadAllText(metadataPath))!.AsObject();
        metadata["generatedAtUtc"] = "2026-08-09T08:00:00+08:00";
        File.WriteAllText(metadataPath, metadata.ToJsonString());

        var exception = Should.Throw<InvalidOperationException>(() =>
            harness.Store.Load(harness.Fixture, harness.Platform));

        exception.Message.ShouldContain("provenance or fixture identity");
    }

    [Fact]
    public void ReplacementRemovesOnlyObsoleteFixtureArtifacts()
    {
        var otherFixture = Fixture() with
        {
            Id = "popover/hero",
            Component = "popover",
            React = "popover/demos/hero/tailwind/index.tsx",
            Blazor = "Popover/Hero"
        };
        var harness = Arrange(
            "artifact-retention",
            manifestFixtures: [Fixture(), otherFixture]);
        var otherShot = ScreenshotSet.Name(
            otherFixture.Id, "light", ParityLeg.React, "initial", "00");
        File.WriteAllText(Path.Combine(harness.RunScreenshots, otherShot), "other pixels");

        harness.Store.Write(harness.Fixture, Capture(), harness.Platform, Provenance());
        harness.Store.Write(
            otherFixture,
            Capture(otherFixture) with
            {
                Steps = [Step("initial", otherShot), Step("toggle-off")]
            },
            harness.Platform,
            Provenance(otherFixture));

        harness.Store.Write(
            harness.Fixture,
            Capture() with { Steps = [Step("initial"), Step("toggle-off")] },
            harness.Platform,
            Provenance());

        var set = Path.Combine(harness.Baselines, harness.Platform.DirectoryName);
        File.Exists(Path.Combine(set, "screenshots", Shot())).ShouldBeFalse();
        File.Exists(Path.Combine(set, "screenshots", otherShot)).ShouldBeTrue();
        harness.Store.Load(otherFixture, harness.Platform).Fixture.ShouldBe(otherFixture.Id);
    }

    [Fact]
    public void MultiThemeWriteIsOrderedCompleteAndAtomicWhenTheLaterThemeFails()
    {
        var fixture = Fixture() with
        {
            Themes = ["light", "dark"]
        };
        var harness = Arrange("multi-theme", manifestFixtures: [fixture]);
        var light = Capture(fixture, "light");
        var dark = Capture(fixture, "dark");

        var receipt = harness.Store.Write(
            fixture,
            [light, dark],
            harness.Platform,
            Provenance());

        harness.Store.Load(fixture, "light", harness.Platform).Theme.ShouldBe("light");
        harness.Store.Load(fixture, "dark", harness.Platform).Theme.ShouldBe("dark");
        var metadataPath = Path.Combine(
            harness.Baselines, harness.Platform.DirectoryName, "metadata.json");
        var before = File.ReadAllText(metadataPath);
        var metadata = JsonSerializer.Deserialize<BaselineSetMetadata>(before)!;
        metadata.Fixtures.Select(item => $"{item.Fixture}@{item.Theme}")
            .ShouldBe(["switch/hero@light", "switch/hero@dark"]);
        receipt.CaptureSha256.Length.ShouldBe(64);

        File.Delete(Path.Combine(harness.RunScreenshots, Shot(theme: "dark")));
        Should.Throw<FileNotFoundException>(() => harness.Store.Write(
            fixture,
            [light, dark],
            harness.Platform,
            Provenance()));

        File.ReadAllText(metadataPath).ShouldBe(before);
        harness.Store.Load(fixture, "light", harness.Platform).Theme.ShouldBe("light");
        harness.Store.Load(fixture, "dark", harness.Platform).Theme.ShouldBe("dark");
    }

    [Fact]
    public void ReplacingAFixtureMayAtomicallyRemoveOnlyItsStaleDeclaredTheme()
    {
        var twoThemes = Fixture() with { Themes = ["light", "dark"] };
        var harness = Arrange("remove-theme", manifestFixtures: [twoThemes]);
        harness.Store.Write(
            twoThemes,
            [Capture(twoThemes, "light"), Capture(twoThemes, "dark")],
            harness.Platform,
            Provenance());
        var set = Path.Combine(harness.Baselines, harness.Platform.DirectoryName);
        var staleCapture = Path.Combine(set, "captures", "switch__hero.dark.json");
        var staleScreenshot = Path.Combine(set, "screenshots", Shot(theme: "dark"));
        File.Exists(staleCapture).ShouldBeTrue();
        File.Exists(staleScreenshot).ShouldBeTrue();

        var lightOnly = twoThemes with { Themes = ["light"] };
        File.WriteAllText(harness.Manifest, JsonSerializer.Serialize(new[] { lightOnly }));

        harness.Store.Write(
            lightOnly,
            Capture(lightOnly, "light"),
            harness.Platform,
            Provenance());

        File.Exists(staleCapture).ShouldBeFalse();
        File.Exists(staleScreenshot).ShouldBeFalse();
        harness.Store.Load(lightOnly, "light", harness.Platform).Theme.ShouldBe("light");
        var metadata = JsonSerializer.Deserialize<BaselineSetMetadata>(File.ReadAllText(
            Path.Combine(set, "metadata.json")))!;
        metadata.Fixtures.Select(item => $"{item.Fixture}@{item.Theme}")
            .ShouldBe(["switch/hero@light"]);
    }

    [Fact]
    public void FailedThemeRemovalRestoresTheExactPreviousThemeArtifacts()
    {
        var failBackupDelete = false;
        var twoThemes = Fixture() with { Themes = ["light", "dark"] };
        var harness = Arrange(
            "remove-theme-rollback",
            path =>
            {
                if (failBackupDelete && path.Contains(".bak", StringComparison.Ordinal))
                {
                    throw new IOException("theme migration rollback probe");
                }

                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }
            },
            [twoThemes]);
        harness.Store.Write(
            twoThemes,
            [Capture(twoThemes, "light"), Capture(twoThemes, "dark")],
            harness.Platform,
            Provenance());
        var set = Path.Combine(harness.Baselines, harness.Platform.DirectoryName);
        var before = Snapshot(set);
        var lightOnly = twoThemes with { Themes = ["light"] };
        File.WriteAllText(harness.Manifest, JsonSerializer.Serialize(new[] { lightOnly }));
        failBackupDelete = true;

        var exception = Should.Throw<IOException>(() => harness.Store.Write(
            lightOnly,
            Capture(lightOnly, "light"),
            harness.Platform,
            Provenance()));

        exception.Message.ShouldContain("theme migration rollback probe");
        Snapshot(set).ShouldBe(before);
    }

    [Fact]
    public void ThemeReplacementCannotBlessAnUnrelatedFixtureContractChange()
    {
        var target = Fixture();
        var retained = Fixture() with
        {
            Id = "collapsible/hero",
            Component = "collapsible",
            React = "collapsible/demos/hero/tailwind/index.tsx",
            Blazor = "Collapsible/Hero"
        };
        var harness = Arrange("unrelated-contract", manifestFixtures: [target, retained]);
        harness.Store.Write(target, Capture(target), harness.Platform, Provenance(target));
        harness.Store.Write(retained, Capture(retained), harness.Platform, Provenance(retained));
        var metadataPath = Path.Combine(
            harness.Baselines, harness.Platform.DirectoryName, "metadata.json");
        var before = File.ReadAllText(metadataPath);
        File.WriteAllText(
            harness.Manifest,
            JsonSerializer.Serialize(new[]
            {
                target,
                retained with { PixelThreshold = retained.PixelThreshold + 0.01 }
            }));

        var exception = Should.Throw<InvalidOperationException>(() => harness.Store.Write(
            target,
            Capture(target),
            harness.Platform,
            Provenance(target)));

        exception.Message.ShouldContain("retained fixture");
        File.ReadAllText(metadataPath).ShouldBe(before);
    }

    private Harness Arrange(
        string? suffix = null,
        Action<string>? deleteDirectory = null,
        IReadOnlyList<FixtureEntry>? manifestFixtures = null)
    {
        var directory = suffix is null ? root : Path.Combine(root, suffix);
        var baselines = Path.Combine(directory, "baselines");
        var screenshots = Path.Combine(directory, "run-screenshots");
        var manifest = Path.Combine(directory, "fixtures.json");
        var stylesheet = Path.Combine(directory, "parity.css");
        Directory.CreateDirectory(baselines);
        Directory.CreateDirectory(screenshots);
        File.WriteAllText(Path.Combine(baselines, "metadata.json"), AuthorityJson());
        var fixtures = manifestFixtures ?? [Fixture()];
        File.WriteAllText(manifest, JsonSerializer.Serialize(fixtures));
        File.WriteAllText(Path.Combine(directory, "aliases.json"), "{}");
        File.WriteAllText(stylesheet, "stylesheet-v1");
        foreach (var fixture in fixtures)
        {
            foreach (var theme in fixture.Themes)
            {
                File.WriteAllText(
                    Path.Combine(screenshots, Shot(fixture.Id, theme)),
                    $"{fixture.Id}@{theme} pixels");
            }
        }

        return new Harness(
            baselines,
            screenshots,
            manifest,
            stylesheet,
            new BaselineStore(
                baselines,
                screenshots,
                manifest,
                stylesheet,
                deleteDirectory),
            Fixture(),
            Platform());
    }

    private static FixtureEntry Fixture() => new()
    {
        Id = "switch/hero",
        Component = "switch",
        React = "switch/demos/hero/tailwind/index.tsx",
        Blazor = "Switch/Hero",
        Steps =
        [
            new StepEntry { Name = "initial" },
            new StepEntry { Name = "toggle-off" }
        ]
    };

    private static BaselinePlatform Platform() => new()
    {
        Browser = "chromium",
        BrowserVersion = "140.0.0.0",
        Os = "macos",
        Architecture = "arm64"
    };

    private static LiveBaselineProvenance Provenance(FixtureEntry? fixture = null) => new(
        Pin,
        fixture is null
            ? "docs/src/app/(docs)/react/components/switch/demos/hero/tailwind/index.tsx"
            : $"docs/src/app/(docs)/react/components/{fixture.React}",
        SourceHash,
        new DateTimeOffset(2026, 8, 9, 0, 0, 0, TimeSpan.Zero));

    private static CaptureBundle Capture(
        FixtureEntry? fixture = null,
        string theme = "light")
        => new()
        {
            CaptureSchemaVersion = CaptureSchema.CurrentVersion,
            Fixture = fixture?.Id ?? "switch/hero",
            Leg = ParityLeg.React,
            BaseUiSha = Pin,
            SourceHash = SourceHash,
            Theme = theme,
            Steps =
            [
                Step("initial", Shot(fixture?.Id ?? "switch/hero", theme)),
                Step("toggle-off")
            ]
        };

    private static StepCapture Step(string name, params string[] screenshots) => new()
    {
        Step = name,
        Dom = new DomNode
        {
            Tag = "button",
            Path = "root > button[role=switch]",
            Attributes = new Dictionary<string, string> { ["role"] = "switch" },
            Classes = [],
            Text = string.Empty,
            Children = []
        },
        Styles = new Dictionary<string, IReadOnlyDictionary<string, string>>(),
        CustomProps = new Dictionary<string, IReadOnlyDictionary<string, string>>(),
        Geometry = new Dictionary<string, IReadOnlyDictionary<string, double>>(),
        Screenshots = screenshots
    };

    private static string Shot(string fixtureId = "switch/hero", string theme = "light")
        => ScreenshotSet.Name(fixtureId, theme, ParityLeg.React, "initial", "00");

    private static IReadOnlyDictionary<string, string> Snapshot(string directory)
        => Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToDictionary(
                path => Path.GetRelativePath(directory, path),
                path => Convert.ToBase64String(File.ReadAllBytes(path)),
                StringComparer.Ordinal);

    private static string AuthorityJson(
        int schemaVersion = BaselineAuthority.CurrentSchemaVersion,
        int captureSchemaVersion = BaselineAuthority.CurrentCaptureSchemaVersion,
        string pin = Pin)
        => JsonSerializer.Serialize(new BaselineAuthority
        {
            SchemaVersion = schemaVersion,
            CaptureSchemaVersion = captureSchemaVersion,
            DeclaredRepositoryPin = pin
        });

    private sealed record Harness(
        string Baselines,
        string RunScreenshots,
        string Manifest,
        string Stylesheet,
        BaselineStore Store,
        FixtureEntry Fixture,
        BaselinePlatform Platform);
}

/// <summary>Pins the explicit baseline-generation command's manifest selection contract.</summary>
public sealed class BaselineGenerationContractTests
{
    [Fact]
    public void SelectionPreservesManifestOrderAndUsesCaseSensitiveSimpleGlobs()
    {
        var fixtures = new[]
        {
            Fixture("switch/hero"),
            Fixture("Switch/upper"),
            Fixture("popover/hero")
        };

        var selected = BaselineGenerator.SelectFixtures(fixtures, "switch/*");

        selected.Select(fixture => fixture.Id).ShouldBe(["switch/hero"]);
        BaselineGenerator.SelectFixtures(fixtures, null).ShouldBe(fixtures);
    }

    [Fact]
    public void PackageCommandBuildsBeforeInvokingTheDedicatedWriter()
    {
        var package = File.ReadAllText(Path.Combine(
            ParityPaths.HarnessRoot,
            "react-fixtures",
            "package.json"));

        package.ShouldContain(
            "pnpm parity:build && dotnet run --project " +
            "../Blazix.BaseUI.Parity.Tests/Blazix.BaseUI.Parity.Tests.csproj -- --write-baselines");
        package.ShouldContain("node scripts/write-provenance-manifest.mjs");
    }

    [Fact]
    public void RejectsFreshWriteFailureBeforeReadingAnOldReadableBaseline()
    {
        var fixture = Fixture("switch/hero");
        var staleResults = new[]
        {
            Result(fixture, ParityLeg.BlazorServer),
            Result(fixture, ParityLeg.BlazorWasm)
        };
        var oldBaselineWasRead = false;

        var exception = Should.Throw<InvalidOperationException>(() =>
            BaselineGenerator.RequireFreshWriteAndLoad(
                fixture,
                staleResults,
                () =>
                {
                    oldBaselineWasRead = true;
                    return staleResults[0].Reference!;
                }));

        exception.Message.ShouldContain("no positive fresh-write evidence");
        oldBaselineWasRead.ShouldBeFalse();
    }

    [Fact]
    public void AcceptsOnlyTwoLegsCarryingTheCompletedWriteReceipt()
    {
        var fixture = Fixture("switch/hero");
        var platform = new BaselinePlatform
        {
            Browser = "chromium",
            BrowserVersion = "143.0.7499.4",
            Os = "macos",
            Architecture = "arm64"
        };
        var receipt = new BaselineWriteReceipt(
            fixture.Id,
            platform,
            new DateTimeOffset(2026, 8, 9, 0, 0, 0, TimeSpan.Zero),
            new string('A', 64));
        var results = new[]
        {
            Result(fixture, ParityLeg.BlazorServer) with { BaselineWrite = receipt },
            Result(fixture, ParityLeg.BlazorWasm) with { BaselineWrite = receipt }
        };

        Should.NotThrow(() => BaselineGenerator.RequireFreshWrite(fixture, results));
    }

    [Fact]
    public void AcceptsEveryThemeInThemeMajorServerWasmOrder()
    {
        var fixture = Fixture("switch/hero") with { Themes = ["light", "dark"] };
        var receipt = Receipt(fixture);
        var results = new[]
        {
            Result(fixture, "light", ParityLeg.BlazorServer) with { BaselineWrite = receipt },
            Result(fixture, "light", ParityLeg.BlazorWasm) with { BaselineWrite = receipt },
            Result(fixture, "dark", ParityLeg.BlazorServer) with { BaselineWrite = receipt },
            Result(fixture, "dark", ParityLeg.BlazorWasm) with { BaselineWrite = receipt }
        };

        Should.NotThrow(() => BaselineGenerator.RequireFreshWrite(fixture, results));

        var reordered = results.ToArray();
        (reordered[1], reordered[2]) = (reordered[2], reordered[1]);
        Should.Throw<InvalidOperationException>(() =>
            BaselineGenerator.RequireFreshWrite(fixture, reordered));
    }

    [Theory]
    [InlineData(ParityLeg.BlazorServer, ParityLeg.BlazorServer)]
    [InlineData(ParityLeg.BlazorWasm, ParityLeg.BlazorWasm)]
    [InlineData(ParityLeg.React, ParityLeg.BlazorServer)]
    public void RejectsDuplicateOrMissingServerWasmLegIdentities(
        ParityLeg first,
        ParityLeg second)
    {
        var fixture = Fixture("switch/hero");
        var receipt = Receipt(fixture);
        var results = new[]
        {
            Result(fixture, first) with { BaselineWrite = receipt },
            Result(fixture, second) with { BaselineWrite = receipt }
        };

        var exception = Should.Throw<InvalidOperationException>(() =>
            BaselineGenerator.RequireFreshWrite(fixture, results));

        exception.Message.ShouldContain("no positive fresh-write evidence");
    }

    [Fact]
    public void RejectsReactFixtureErrorEvenWhenBothFreshWriteReceiptsExist()
    {
        var fixture = Fixture("switch/hero");
        var receipt = Receipt(fixture);
        var results = new[]
        {
            Result(fixture, ParityLeg.BlazorServer) with
            {
                BaselineWrite = receipt,
                Findings =
                [
                    new Finding
                    {
                        Fixture = fixture.Id,
                        Leg = ParityLeg.React,
                        Step = string.Empty,
                        Kind = FindingKind.FixtureError,
                        Severity = Severity.Error,
                        Message = "reference write probe"
                    }
                ]
            },
            Result(fixture, ParityLeg.BlazorWasm) with { BaselineWrite = receipt }
        };

        var exception = Should.Throw<InvalidOperationException>(() =>
            BaselineGenerator.RequireFreshWrite(fixture, results));

        exception.Message.ShouldContain("no positive fresh-write evidence");
    }

    private static FixtureEntry Fixture(string id) => new()
    {
        Id = id,
        Component = id[..id.IndexOf('/')],
        React = "internal:canary",
        Blazor = "Harness/Canary"
    };

    private static BaselineWriteReceipt Receipt(FixtureEntry fixture) => new(
        fixture.Id,
        new BaselinePlatform
        {
            Browser = "chromium",
            BrowserVersion = "143.0.7499.4",
            Os = "macos",
            Architecture = "arm64"
        },
        new DateTimeOffset(2026, 8, 9, 0, 0, 0, TimeSpan.Zero),
        new string('A', 64));

    private static ParityRunResult Result(FixtureEntry fixture, ParityLeg leg)
        => Result(fixture, "light", leg);

    private static ParityRunResult Result(
        FixtureEntry fixture,
        string theme,
        ParityLeg leg) => new()
    {
        Fixture = fixture.Id,
        Theme = theme,
        ExecutionId = $"{fixture.Id}@{theme}",
        Leg = leg,
        Reference = new CaptureBundle
        {
            CaptureSchemaVersion = CaptureSchema.CurrentVersion,
            Fixture = fixture.Id,
            Theme = theme,
            Leg = ParityLeg.React,
            Steps = []
        }
    };
}

/// <summary>Proves production runner mode orchestration against real browser captures.</summary>
/// <param name="playwright">The production browser fixture.</param>
public sealed class BaselineRunnerModeTests(PlaywrightFixture playwright)
    : IClassFixture<PlaywrightFixture>, IDisposable
{
    private const string Pin = "bdcb685fadcca9d18b18f013c052795a53b6aa33";
    private const string SourceHash =
        "AABBCCDDAABBCCDDAABBCCDDAABBCCDDAABBCCDDAABBCCDDAABBCCDDAABBCCDD";

    private readonly string artifacts = Path.Combine(
        Path.GetTempPath(), "blazix-baseline-runner", Guid.NewGuid().ToString("N"));

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(artifacts))
        {
            Directory.Delete(artifacts, recursive: true);
        }
    }

    [Fact]
    public async Task BaselineRunnerUsesCommittedReferenceWithBothReactDirectoriesMissing()
    {
        var fixture = CanaryFixture();
        var screenshots = Path.Combine(artifacts, "screenshots");
        var sourceStore = Store(screenshots, "live-source");
        var live = await new ParityRunner(
            new ComparatorRegistry(screenshots),
            screenshots,
            ParityPaths.ReactDist,
            Path.Combine(AppContext.BaseDirectory, "react-dist"),
            baselineStore: sourceStore,
            readLiveProvenance: _ => Provenance(),
            validateLiveBundle: (_, _, _, _) => { })
            .RunLiveAsync(playwright.Browser, fixture);
        var reference = live[0].Reference! with { BaseUiSha = Pin, SourceHash = SourceHash };
        var store = Store(screenshots);
        var platform = BaselinePlatform.Current(playwright.Browser);
        store.Write(fixture, reference, platform, Provenance());
        var missing = Path.Combine(artifacts, "missing-react-dist");
        var runner = new ParityRunner(
            new ComparatorRegistry(screenshots),
            screenshots,
            missing,
            missing,
            baselineStore: store,
            readLiveProvenance: _ => throw new InvalidOperationException("must not read live source"));

        var results = await runner.RunAsync(
            playwright.Browser,
            fixture,
            new ParityOptions { Mode = ParityReferenceMode.Baseline });

        results.Select(item => item.Leg).ShouldBe([
            ParityLeg.BlazorServer,
            ParityLeg.BlazorWasm
        ]);
        results.All(item => item.Reference is not null && item.Reference.BaseUiSha == Pin)
            .ShouldBeTrue();
        results.ShouldAllBe(item => item.Findings.Count == 3);
        results.SelectMany(item => item.Findings).All(item =>
                item.Kind == FindingKind.Attribute || item.Kind == FindingKind.ComputedStyle)
            .ShouldBeTrue();
    }

    [Fact]
    public async Task WriteModeCapturesLiveOnceAndPersistsTheReferenceSet()
    {
        var fixture = CanaryFixture();
        var screenshots = Path.Combine(artifacts, "write-screenshots");
        var store = Store(screenshots, "write-store");
        var runner = new ParityRunner(
            new ComparatorRegistry(screenshots),
            screenshots,
            ParityPaths.ReactDist,
            Path.Combine(AppContext.BaseDirectory, "react-dist"),
            baselineStore: store,
            readLiveProvenance: _ => Provenance(),
            validateLiveBundle: (_, _, _, _) => { });

        var results = await runner.RunAsync(
            playwright.Browser,
            fixture,
            new ParityOptions { Mode = ParityReferenceMode.WriteBaseline });

        results.All(item => item.Reference is not null && item.Reference.BaseUiSha == Pin)
            .ShouldBeTrue();
        results.All(item => item.Reference is not null && item.Reference.SourceHash == SourceHash)
            .ShouldBeTrue();
        results.All(item => item.BaselineWrite is not null).ShouldBeTrue();
        var loaded = store.Load(fixture, BaselinePlatform.Current(playwright.Browser));
        loaded.BaseUiSha.ShouldBe(Pin);
        loaded.SourceHash.ShouldBe(SourceHash);
        loaded.Steps.Select(item => item.Step).ShouldBe(["initial"]);
    }

    [Fact]
    public async Task WriteModeExpandsThemesInThemeMajorLegOrderAndPersistsBothAtomically()
    {
        var fixture = CanaryFixture() with { Themes = ["light", "dark"] };
        var screenshots = Path.Combine(artifacts, "multi-theme-screenshots");
        var store = Store(screenshots, "multi-theme-store", fixture);
        var runner = new ParityRunner(
            new ComparatorRegistry(screenshots),
            screenshots,
            ParityPaths.ReactDist,
            Path.Combine(AppContext.BaseDirectory, "react-dist"),
            baselineStore: store,
            readLiveProvenance: _ => Provenance(),
            validateLiveBundle: (_, _, _, _) => { });

        var results = await runner.RunAsync(
            playwright.Browser,
            fixture,
            new ParityOptions { Mode = ParityReferenceMode.WriteBaseline });

        results.Select(item => (item.ExecutionId, item.Leg)).ShouldBe([
            ("harness/canary@light", ParityLeg.BlazorServer),
            ("harness/canary@light", ParityLeg.BlazorWasm),
            ("harness/canary@dark", ParityLeg.BlazorServer),
            ("harness/canary@dark", ParityLeg.BlazorWasm)
        ]);
        results.ShouldAllBe(item => item.BaselineWrite != null);
        results.ShouldAllBe(item =>
            item.Findings.All(finding => finding.Fixture == item.ExecutionId));

        var platform = BaselinePlatform.Current(playwright.Browser);
        store.Load(fixture, "light", platform).Theme.ShouldBe("light");
        store.Load(fixture, "dark", platform).Theme.ShouldBe("dark");
    }

    [Fact]
    public async Task LiveModeRejectsAReactCheckoutOutsideTheDeclaredRepositoryPin()
    {
        var fixture = CanaryFixture();
        var screenshots = Path.Combine(artifacts, "live-pin-screenshots");
        var store = Store(screenshots, "live-pin-store");
        var runner = new ParityRunner(
            new ComparatorRegistry(screenshots),
            screenshots,
            ParityPaths.ReactDist,
            Path.Combine(AppContext.BaseDirectory, "react-dist"),
            baselineStore: store,
            readLiveProvenance: _ => Provenance() with { UpstreamSha = new string('a', 40) },
            validateLiveBundle: (_, _, _, _) => { });

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            runner.RunLiveAsync(playwright.Browser, fixture));

        exception.Message.ShouldContain("declared repository pin");
        Directory.Exists(Path.Combine(
            artifacts,
            "live-pin-store",
            "baselines",
            BaselinePlatform.Current(playwright.Browser).DirectoryName)).ShouldBeFalse();
    }

    [Fact]
    public async Task PublicLiveModeValidatesBundleProvenanceBeforeOpeningABrowserContext()
    {
        var fixture = CanaryFixture();
        var screenshots = Path.Combine(artifacts, "precondition-screenshots");
        var contextAttempts = 0;
        var runner = new ParityRunner(
            new ComparatorRegistry(screenshots),
            screenshots,
            ParityPaths.ReactDist,
            Path.Combine(AppContext.BaseDirectory, "react-dist"),
            createContext: _ =>
            {
                contextAttempts++;
                throw new InvalidOperationException("browser context must not be created");
            },
            baselineStore: Store(screenshots, "precondition-store"),
            readLiveProvenance: _ => Provenance(),
            validateLiveBundle: (_, _, _, _) =>
                throw new InvalidOperationException("bundle provenance probe"));

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            runner.RunLiveAsync(playwright.Browser, fixture));

        exception.Message.ShouldContain("bundle provenance probe");
        contextAttempts.ShouldBe(0);
    }

    private BaselineStore Store(
        string screenshots,
        string name = "store",
        FixtureEntry? fixture = null)
    {
        var directory = Path.Combine(artifacts, name);
        var baselines = Path.Combine(directory, "baselines");
        var manifest = Path.Combine(directory, "fixtures.json");
        var stylesheet = Path.Combine(directory, "parity.css");
        Directory.CreateDirectory(baselines);
        File.WriteAllText(
            Path.Combine(baselines, "metadata.json"),
            JsonSerializer.Serialize(new BaselineAuthority
            {
                SchemaVersion = BaselineAuthority.CurrentSchemaVersion,
                CaptureSchemaVersion = BaselineAuthority.CurrentCaptureSchemaVersion,
                DeclaredRepositoryPin = Pin
            }));
        File.WriteAllText(
            manifest,
            JsonSerializer.Serialize(new[] { fixture ?? CanaryFixture() }));
        File.WriteAllText(Path.Combine(directory, "aliases.json"), "{}");
        File.WriteAllText(stylesheet, "canary stylesheet");
        return new BaselineStore(baselines, screenshots, manifest, stylesheet);
    }

    private static FixtureEntry CanaryFixture() => new()
    {
        Id = "harness/canary",
        Component = "harness",
        React = "harness/demos/canary/tailwind/index.tsx",
        Blazor = "Harness/Canary",
        PixelThreshold = 0,
        Steps = [new StepEntry { Name = "initial" }]
    };

    private static LiveBaselineProvenance Provenance() => new(
        Pin,
        "docs/src/app/(docs)/react/components/harness/demos/canary/tailwind/index.tsx",
        SourceHash,
        new DateTimeOffset(2026, 8, 9, 0, 0, 0, TimeSpan.Zero));
}
