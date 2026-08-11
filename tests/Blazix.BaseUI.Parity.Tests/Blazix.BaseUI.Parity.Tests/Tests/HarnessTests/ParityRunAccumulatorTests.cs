using Blazix.BaseUI.Parity.Tests.Baselines;
using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Diff;
using Blazix.BaseUI.Parity.Tests.Infrastructure;
using Blazix.BaseUI.Parity.Tests.Report;
using Blazix.BaseUI.Parity.Tests.Waivers;
using Microsoft.Playwright;
using Shouldly;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

/// <summary>Pins the public catalog surface and assembly-wide two-attempt policy.</summary>
public sealed class ParityRunAccumulatorTests
{
    [Fact]
    public void PublicDataEnumeratesTheDurableCatalogAsOrderedLightServerWasmCases()
    {
        var cases = ParityTheoryData.Build(FixtureManifest.Load());

        cases.Count.ShouldBe(58);
        cases.Select(item => item.Fixture).Distinct().ShouldBe(MilestoneFixtureCatalog.Ids);
        foreach (var group in cases.Chunk(2))
        {
            group.Select(item => item.Theme).ShouldBe(["light", "light"]);
            group.Select(item => item.Leg)
                .ShouldBe([ParityLeg.BlazorServer, ParityLeg.BlazorWasm]);
        }

        cases.ShouldAllBe(item => item.Authored);
        cases.Where(item => !item.Timing)
            .ShouldAllBe(item => item.Shard == item.CatalogOrdinal % 4);
    }

    [Fact]
    public void AnimationFixturesMoveBothLegsToTheTimingPartition()
    {
        var fixture = Fixture() with
        {
            Steps = [new StepEntry { Name = "initial", Settle = "animation" }]
        };

        var cases = ParityTheoryData.Build([fixture]);

        cases.Where(item => item.Fixture == fixture.Id)
            .ShouldAllBe(item => item.Timing && item.Shard == -1);
        cases.Where(item => item.Fixture != fixture.Id)
            .ShouldAllBe(item => !item.Timing && item.Shard >= 0 && item.Shard <= 3);
    }

    [Fact]
    public void TimingPartitionContainsEveryAuthoredAnimationFixtureInCatalogOrder()
    {
        ParityTheoryData.Timing.Select(item => (
                Fixture: (string)item[0],
                Theme: (string)item[1],
                Leg: (ParityLeg)item[2]))
            .ShouldBe(
            [
                ("switch/hero", "light", ParityLeg.BlazorServer),
                ("switch/hero", "light", ParityLeg.BlazorWasm),
                ("collapsible/hero", "light", ParityLeg.BlazorServer),
                ("collapsible/hero", "light", ParityLeg.BlazorWasm),
                ("popover/hero", "light", ParityLeg.BlazorServer),
                ("popover/hero", "light", ParityLeg.BlazorWasm),
                ("select/grouped", "light", ParityLeg.BlazorServer),
                ("select/grouped", "light", ParityLeg.BlazorWasm),
                ("accordion/multiple", "light", ParityLeg.BlazorServer),
                ("accordion/multiple", "light", ParityLeg.BlazorWasm),
                ("dialog/hero", "light", ParityLeg.BlazorServer),
                ("dialog/hero", "light", ParityLeg.BlazorWasm),
                ("drawer/hero", "light", ParityLeg.BlazorServer),
                ("drawer/hero", "light", ParityLeg.BlazorWasm),
                ("toast/hero", "light", ParityLeg.BlazorServer),
                ("toast/hero", "light", ParityLeg.BlazorWasm),
                ("tooltip/hero", "light", ParityLeg.BlazorServer),
                ("tooltip/hero", "light", ParityLeg.BlazorWasm),
                ("preview-card/hero", "light", ParityLeg.BlazorServer),
                ("preview-card/hero", "light", ParityLeg.BlazorWasm),
                ("menu/arrow", "light", ParityLeg.BlazorServer),
                ("menu/arrow", "light", ParityLeg.BlazorWasm),
                ("select/hero", "light", ParityLeg.BlazorServer),
                ("select/hero", "light", ParityLeg.BlazorWasm),
                ("menu/checkbox-items", "light", ParityLeg.BlazorServer),
                ("menu/checkbox-items", "light", ParityLeg.BlazorWasm),
                ("menubar/hero", "light", ParityLeg.BlazorServer),
                ("menubar/hero", "light", ParityLeg.BlazorWasm),
                ("tabs/hero", "light", ParityLeg.BlazorServer),
                ("tabs/hero", "light", ParityLeg.BlazorWasm),
                ("toolbar/hero", "light", ParityLeg.BlazorServer),
                ("toolbar/hero", "light", ParityLeg.BlazorWasm),
                ("checkbox/hero", "light", ParityLeg.BlazorServer),
                ("checkbox/hero", "light", ParityLeg.BlazorWasm),
                ("popover/detached-triggers-simple", "light", ParityLeg.BlazorServer),
                ("popover/detached-triggers-simple", "light", ParityLeg.BlazorWasm),
                ("navigation-menu/hero", "light", ParityLeg.BlazorServer),
                ("navigation-menu/hero", "light", ParityLeg.BlazorWasm),
                ("combobox/hero", "light", ParityLeg.BlazorServer),
                ("combobox/hero", "light", ParityLeg.BlazorWasm)
            ]);

        var method = typeof(ParityTimingTests).GetMethod(nameof(ParityTimingTests.MatchesReact));
        method.ShouldNotBeNull();
        method.GetCustomAttributes(typeof(TheoryAttribute), inherit: false)
            .Cast<TheoryAttribute>()
            .ShouldHaveSingleItem()
            .SkipTestWithoutData.ShouldBeTrue();
    }

    [Fact]
    public void StaticAndTimingPartitionsCoverEveryAuthoredRowExactlyOnce()
    {
        var rows = ParityTheoryData.StaticShard0
            .Concat(ParityTheoryData.StaticShard1)
            .Concat(ParityTheoryData.StaticShard2)
            .Concat(ParityTheoryData.StaticShard3)
            .Concat(ParityTheoryData.Timing)
            .Select(item => (
                Fixture: (string)item[0],
                Theme: (string)item[1],
                Leg: (ParityLeg)item[2]))
            .ToArray();

        rows.Length.ShouldBe(58);
        rows.Distinct().Count().ShouldBe(58);

        var expected = ParityTheoryData.Build(FixtureManifest.Load());
        rows.OrderBy(item => item.Fixture, StringComparer.Ordinal)
            .ThenBy(item => item.Theme, StringComparer.Ordinal)
            .ThenBy(item => item.Leg)
            .ShouldBe(expected
                .Select(item => (item.Fixture, item.Theme, item.Leg))
                .OrderBy(item => item.Fixture, StringComparer.Ordinal)
                .ThenBy(item => item.Theme, StringComparer.Ordinal)
                .ThenBy(item => item.Leg));
    }

    [Fact]
    public async Task ConcurrentCaseRowsMemoizeExactlyTwoWholeFixtureAttempts()
    {
        var calls = 0;
        var finalized = 0;
        var accumulator = Accumulator(
            async (_, fixture, options, attempt, root) =>
            {
                Interlocked.Increment(ref calls);
                await Task.Yield();
                return Batch(
                    fixture,
                    options,
                    attempt,
                    root,
                    [Result(ParityLeg.BlazorServer), Result(ParityLeg.BlazorWasm)]);
            },
            _ =>
            {
                Interlocked.Increment(ref finalized);
                return ValueTask.CompletedTask;
            });
        IBrowser browser = null!;

        var verdicts = await Task.WhenAll(
            accumulator.EvaluateAsync(browser, "switch/hero", "light", ParityLeg.BlazorServer),
            accumulator.EvaluateAsync(browser, "switch/hero", "light", ParityLeg.BlazorWasm));

        calls.ShouldBe(2);
        verdicts.ShouldAllBe(item => !item.Blocking);
        await accumulator.DisposeAsync();
        await accumulator.DisposeAsync();
        finalized.ShouldBe(1);
    }

    [Fact]
    public async Task MissingCatalogFixtureCreatesBlockingTypedEvidenceWithoutRunningABrowser()
    {
        var calls = 0;
        var accumulator = Accumulator((_, _, _, _, _) =>
        {
            Interlocked.Increment(ref calls);
            throw new InvalidOperationException("missing fixtures must not run");
        });

        var verdict = await accumulator.EvaluateAsync(
            null!,
            "collapsible/hero",
            "light",
            ParityLeg.BlazorServer);

        calls.ShouldBe(0);
        verdict.Blocking.ShouldBeTrue();
        verdict.Findings.ShouldHaveSingleItem().Kind.ShouldBe(FindingKind.FixtureError);
        verdict.Message.ShouldContain("collapsible/hero@light");
        verdict.Message.ShouldContain(nameof(ParityLeg.BlazorServer));
    }

    [Fact]
    public async Task MissingCatalogBothLegsFinalizeAsNonwaivableReportEvidence()
    {
        ParityRunFinalization? finalization = null;
        var accumulator = Accumulator(
            (_, _, _, _, _) => throw new InvalidOperationException("missing fixtures must not run"),
            value =>
            {
                finalization = value;
                return ValueTask.CompletedTask;
            });

        _ = await accumulator.EvaluateAsync(
            null!, "collapsible/hero", "light", ParityLeg.BlazorServer);
        _ = await accumulator.EvaluateAsync(
            null!, "collapsible/hero", "light", ParityLeg.BlazorWasm);
        await accumulator.DisposeAsync();

        finalization.ShouldNotBeNull();
        finalization.RetryVerdict.Evidence.Count.ShouldBe(2);
        finalization.RetryVerdict.Evidence.ShouldAllBe(item =>
            item.Classification == RetryFindingClassification.NonWaivable &&
            item.Effective.Kind == FindingKind.FixtureError);
        finalization.WaiverVerdict.NonWaivableFindings.Count.ShouldBe(2);
        var model = ParityRunAccumulator.BuildReportModel(finalization, Authority);
        model.Findings.Count.ShouldBe(2);
        model.Findings.ShouldAllBe(item => item.Blocking);
        model.Verdict.Kind.ShouldBe(ReportVerdictKind.Incomplete);
    }

    [Fact]
    public async Task MissingCandidateLegRemainsNonwaivableAcrossBothAttempts()
    {
        var accumulator = Accumulator((_, fixture, options, attempt, root) => Task.FromResult(
            Batch(fixture, options, attempt, root, [Result(ParityLeg.BlazorServer)])));

        var verdict = await accumulator.EvaluateAsync(
            null!,
            "switch/hero",
            "light",
            ParityLeg.BlazorWasm);

        verdict.Blocking.ShouldBeTrue();
        verdict.Findings.ShouldContain(item =>
            item.Kind == FindingKind.FixtureError &&
            item.Leg == ParityLeg.BlazorWasm);
        verdict.Message.ShouldContain("missing", Case.Insensitive);
    }

    [Fact]
    public async Task DormantAccumulatorDoesNotFinalizeAReport()
    {
        var finalized = 0;
        var accumulator = Accumulator(
            (_, _, _, _, _) => throw new InvalidOperationException("must stay dormant"),
            _ =>
            {
                Interlocked.Increment(ref finalized);
                return ValueTask.CompletedTask;
            });

        await accumulator.InitializeAsync();
        await accumulator.DisposeAsync();

        finalized.ShouldBe(0);
    }

    [Fact]
    public async Task SharedReferenceNonwaivableEvidenceBlocksBothCandidateRowsAndIsCanonicalizedOnce()
    {
        ParityRunFinalization? finalization = null;
        var referenceFailure = Finding(
            ParityLeg.React,
            FindingKind.ActionCompletionUnmet,
            "completion-0",
            "React completion did not arrive.");
        var accumulator = Accumulator(
            (_, fixture, options, attempt, root) => Task.FromResult(Batch(
                fixture,
                options,
                attempt,
                root,
                [Result(ParityLeg.BlazorServer, referenceFailure), Result(ParityLeg.BlazorWasm, referenceFailure)])),
            value =>
            {
                finalization = value;
                return ValueTask.CompletedTask;
            });

        var server = await accumulator.EvaluateAsync(null!, "switch/hero", "light", ParityLeg.BlazorServer);
        var wasm = await accumulator.EvaluateAsync(null!, "switch/hero", "light", ParityLeg.BlazorWasm);
        await accumulator.DisposeAsync();

        server.Blocking.ShouldBeTrue();
        wasm.Blocking.ShouldBeTrue();
        server.Findings.ShouldHaveSingleItem().Leg.ShouldBe(ParityLeg.React);
        wasm.Findings.ShouldHaveSingleItem().Leg.ShouldBe(ParityLeg.React);
        finalization.ShouldNotBeNull();
        finalization.RetryVerdict.Evidence.ShouldHaveSingleItem()
            .Classification.ShouldBe(RetryFindingClassification.NonWaivable);
    }

    [Fact]
    public async Task DivergentSharedReferenceEvidenceBecomesTypedBlockingExecutionEvidence()
    {
        var left = Finding(ParityLeg.React, FindingKind.Console, "console", "first copy");
        var right = Finding(ParityLeg.React, FindingKind.Console, "console", "divergent copy");
        var accumulator = Accumulator((_, fixture, options, attempt, root) => Task.FromResult(Batch(
            fixture,
            options,
            attempt,
            root,
            [Result(ParityLeg.BlazorServer, left), Result(ParityLeg.BlazorWasm, right)])));

        var verdict = await accumulator.EvaluateAsync(
            null!, "switch/hero", "light", ParityLeg.BlazorServer);

        verdict.Blocking.ShouldBeTrue();
        verdict.Findings.ShouldContain(item =>
            item.Kind == FindingKind.FixtureError &&
            item.Leg == ParityLeg.React &&
            item.Property == "divergent-react-evidence");
    }

    [Fact]
    public async Task FinalizationUsesInjectedUtcClockAndPreservesBatchPlatformAndDuration()
    {
        var instant = new DateTimeOffset(2026, 8, 10, 4, 5, 6, TimeSpan.Zero);
        ParityRunFinalization? finalization = null;
        var accumulator = Accumulator(
            (_, fixture, options, attempt, root) => Task.FromResult(Batch(
                fixture,
                options,
                attempt,
                root,
                [Result(ParityLeg.BlazorServer), Result(ParityLeg.BlazorWasm)])),
            value =>
            {
                finalization = value;
                return ValueTask.CompletedTask;
            },
            new FrozenTimeProvider(instant));

        _ = await accumulator.EvaluateAsync(null!, "switch/hero", "light", ParityLeg.BlazorServer);
        await accumulator.DisposeAsync();

        finalization.ShouldNotBeNull();
        finalization.GeneratedAtUtc.ShouldBe(instant);
        finalization.FirstBatches.ShouldHaveSingleItem().Platform.Browser.ShouldBe("chromium");
        finalization.FirstBatches.ShouldHaveSingleItem().Duration.ShouldBe(TimeSpan.FromMilliseconds(1));
        finalization.RetryBatches.ShouldHaveSingleItem().Duration.ShouldBe(TimeSpan.FromMilliseconds(2));
    }

    [Fact]
    public void SharedReactScreenshotHasUniqueReportPathForEachOwningCandidateLeg()
    {
        using var directory = TestDirectory.Create();
        var reference = ScreenshotSet.Name("switch/hero", "light", ParityLeg.React, "initial", "root");
        var server = ScreenshotSet.Name("switch/hero", "light", ParityLeg.BlazorServer, "initial", "root");
        var wasm = ScreenshotSet.Name("switch/hero", "light", ParityLeg.BlazorWasm, "initial", "root");
        foreach (var name in new[] { reference, server, wasm })
        {
            File.WriteAllBytes(Path.Combine(directory.Path, name), [1, 2, 3]);
        }

        var fixture = Fixture();
        var options = new ParityOptions { Mode = ParityReferenceMode.Baseline };
        var batches = new[]
        {
            Batch(fixture, options, 1, directory.Path,
            [
                Result(ParityLeg.BlazorServer, screenshots: [reference], candidateScreenshots: [server]),
                Result(ParityLeg.BlazorWasm, screenshots: [reference], candidateScreenshots: [wasm])
            ])
        };

        var artifacts = ParityRunAccumulator.BuildArtifactSources(batches);

        artifacts.Select(item => item.Artifact.RelativePath).ShouldBeUnique();
        artifacts.Where(item => item.Artifact.Role == "React").Count().ShouldBe(2);
        artifacts.ShouldContain(item => item.Artifact.RelativePath.Contains("BlazorServer", StringComparison.Ordinal));
        artifacts.ShouldContain(item => item.Artifact.RelativePath.Contains("BlazorWasm", StringComparison.Ordinal));
    }

    [Fact]
    public void MissingDeclaredAttemptOneScreenshotsRemainBlockingReportDiagnostics()
    {
        using var directory = TestDirectory.Create();
        var fixture = Fixture();
        var options = new ParityOptions { Mode = ParityReferenceMode.Live };
        var reference = ScreenshotSet.Name(
            fixture.Id, "light", ParityLeg.React, "initial", "00");
        var candidate = ScreenshotSet.Name(
            fixture.Id, "light", ParityLeg.BlazorServer, "initial", "00");
        var first = Batch(
            fixture,
            options,
            1,
            directory.Path,
            [
                Result(
                    ParityLeg.BlazorServer,
                    screenshots: [reference],
                    candidateScreenshots: [candidate]),
                Result(ParityLeg.BlazorWasm)
            ]);
        var retry = Batch(
            fixture,
            options,
            2,
            Path.Combine(directory.Path, "retry"),
            [Result(ParityLeg.BlazorServer), Result(ParityLeg.BlazorWasm)]);

        var model = ParityRunAccumulator.BuildReportModel(
            Finalization(options, fixture, first, retry));

        model.Diagnostics.Count(item => item.Kind == "ArtifactMissing").ShouldBe(2);
        model.Diagnostics.Where(item => item.Kind == "ArtifactMissing")
            .ShouldAllBe(item => item.Blocking && item.Source == ReportDiagnosticSource.Artifact);
        model.Artifacts.ShouldBeEmpty();
        System.Text.Encoding.UTF8.GetString(JsonReportWriter.Render(model))
            .ShouldContain("ArtifactMissing");
        HtmlReportWriter.Render(model).ShouldContain("ArtifactMissing");
    }

    [Fact]
    public void InvalidDeclaredScreenshotNameBecomesBlockingDiagnosticBeforePackaging()
    {
        using var directory = TestDirectory.Create();
        const string name = "not-a-capture-name.png";
        File.WriteAllBytes(Path.Combine(directory.Path, name), [1, 2, 3]);
        var fixture = Fixture();
        var options = new ParityOptions { Mode = ParityReferenceMode.Live };
        var batch = Batch(
            fixture,
            options,
            1,
            directory.Path,
            [Result(ParityLeg.BlazorServer, screenshots: [name])]);

        var mapping = ParityRunAccumulator.BuildArtifactMapping([batch]);

        mapping.Sources.ShouldBeEmpty();
        mapping.Diagnostics.ShouldHaveSingleItem().Kind.ShouldBe("ArtifactInvalid");
        mapping.Diagnostics.ShouldHaveSingleItem().Blocking.ShouldBeTrue();
    }

    [Fact]
    public async Task CandidateFindingNeverLeaksAcrossThemesIntoAnotherCaseOrMessage()
    {
        var fixture = Fixture() with { Themes = ["light", "dark"] };
        var darkFinding = Finding(
            ParityLeg.BlazorWasm,
            FindingKind.Attribute,
            "aria-checked",
            "dark-only evidence") with { Fixture = "switch/hero@dark" };
        var accumulator = Accumulator(
            [fixture],
            (_, entry, options, attempt, root) => Task.FromResult(Batch(
                entry,
                options,
                attempt,
                root,
                [
                    Result(ParityLeg.BlazorServer, theme: "light"),
                    Result(ParityLeg.BlazorWasm, theme: "light"),
                    Result(ParityLeg.BlazorServer, theme: "dark"),
                    Result(ParityLeg.BlazorWasm, darkFinding, theme: "dark")
                ])));

        var light = await accumulator.EvaluateAsync(
            null!, "switch/hero", "light", ParityLeg.BlazorWasm);
        var dark = await accumulator.EvaluateAsync(
            null!, "switch/hero", "dark", ParityLeg.BlazorWasm);

        light.Blocking.ShouldBeFalse();
        light.Findings.ShouldBeEmpty();
        light.Message.ShouldNotContain("dark-only evidence");
        dark.Blocking.ShouldBeTrue();
        dark.Findings.ShouldHaveSingleItem().Message.ShouldBe("dark-only evidence");
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task AttemptExceptionsBecomeBoundedTypedEvidenceAndStillFinalizeOnce(
        bool throwFirst,
        bool throwRetry)
    {
        var finalized = 0;
        ParityRunFinalization? finalization = null;
        var accumulator = Accumulator(
            (_, fixture, options, attempt, root) =>
            {
                if (attempt == 1 && throwFirst || attempt == 2 && throwRetry)
                {
                    throw new IOException(new string('x', 700) + " /Users/private/file.json");
                }

                return Task.FromResult(Batch(
                    fixture,
                    options,
                    attempt,
                    root,
                    [Result(ParityLeg.BlazorServer), Result(ParityLeg.BlazorWasm)]));
            },
            value =>
            {
                Interlocked.Increment(ref finalized);
                finalization = value;
                return ValueTask.CompletedTask;
            });

        var verdict = await accumulator.EvaluateAsync(
            null!, "switch/hero", "light", ParityLeg.BlazorServer);
        await accumulator.DisposeAsync();

        verdict.Blocking.ShouldBeTrue();
        verdict.Findings.ShouldContain(item =>
            item.Kind == FindingKind.FixtureError && item.Property == "attempt-exception");
        verdict.Findings.Where(item => item.Property == "attempt-exception")
            .ShouldAllBe(item => item.Message.Length <= 550);
        verdict.Message.Length.ShouldBeLessThan(4000);
        verdict.Message.ShouldNotContain("/Users/private");
        finalized.ShouldBe(1);
        finalization.ShouldNotBeNull();
        finalization.RetryVerdict.Failures.ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData("attempt-root")]
    [InlineData("fixture")]
    [InlineData("fixture-contract")]
    [InlineData("options")]
    [InlineData("platform")]
    public async Task WrongBatchEnvelopeBecomesScopedTypedEvidenceAndCannotOwnArtifactsOrTiming(
        string defect)
    {
        ParityRunFinalization? finalization = null;
        var accumulator = Accumulator(
            (_, fixture, options, attempt, root) =>
            {
                var batch = Batch(
                    fixture,
                    options,
                    attempt,
                    root,
                    [Result(ParityLeg.BlazorServer), Result(ParityLeg.BlazorWasm)]);
                return Task.FromResult(defect switch
                {
                    "attempt-root" => batch with { Attempt = attempt + 10, ArtifactRoot = root + "-wrong" },
                    "fixture" => batch with { Fixture = fixture with { Id = "collapsible/hero" } },
                    "fixture-contract" => batch with
                    {
                        Fixture = fixture with { Themes = ["light", "dark"] }
                    },
                    "platform" => batch with { Platform = null! },
                    _ => batch with { Options = options with { Mode = ParityReferenceMode.Live } }
                });
            },
            value =>
            {
                finalization = value;
                return ValueTask.CompletedTask;
            });

        var verdict = await accumulator.EvaluateAsync(
            null!, "switch/hero", "light", ParityLeg.BlazorServer);
        await accumulator.DisposeAsync();

        verdict.Blocking.ShouldBeTrue();
        verdict.Findings.ShouldContain(item => item.Property == "invalid-batch-envelope");
        finalization.ShouldNotBeNull();
        finalization.FirstBatches.ShouldHaveSingleItem().Attempt.ShouldBe(1);
        finalization.RetryBatches.ShouldHaveSingleItem().Attempt.ShouldBe(2);
        finalization.FirstBatches.ShouldHaveSingleItem().ArtifactRoot
            .ShouldContain("attempt-1");
    }

    [Fact]
    public async Task NegativeDurationAndCrossAttemptPlatformBecomeTypedEvidenceBeforeReporting()
    {
        ParityRunFinalization? finalization = null;
        var accumulator = Accumulator(
            (_, fixture, options, attempt, root) =>
            {
                var batch = Batch(
                    fixture,
                    options,
                    attempt,
                    root,
                    [Result(ParityLeg.BlazorServer), Result(ParityLeg.BlazorWasm)]);
                return Task.FromResult(attempt == 1
                    ? batch with { Duration = TimeSpan.FromMilliseconds(-1) }
                    : batch with
                    {
                        Platform = batch.Platform with { BrowserVersion = "141.0.0.0" }
                    });
            },
            value =>
            {
                finalization = value;
                return ValueTask.CompletedTask;
            });

        var verdict = await accumulator.EvaluateAsync(
            null!, "switch/hero", "light", ParityLeg.BlazorServer);
        await accumulator.DisposeAsync();

        verdict.Blocking.ShouldBeTrue();
        verdict.Findings.ShouldContain(item => item.Property == "invalid-batch-envelope");
        finalization.ShouldNotBeNull();
        finalization.FirstBatches.ShouldHaveSingleItem().Duration.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
        finalization.FirstBatches.ShouldHaveSingleItem().Platform
            .ShouldBe(finalization.RetryBatches.ShouldHaveSingleItem().Platform);
    }

    [Fact]
    public async Task NullRunnerResultRowBecomesEnvelopeFailureAndStillFinalizes()
    {
        ParityRunFinalization? finalization = null;
        var accumulator = Accumulator(
            (_, fixture, options, attempt, root) => Task.FromResult(Batch(
                fixture,
                options,
                attempt,
                root,
                [Result(ParityLeg.BlazorServer), null!, Result(ParityLeg.BlazorWasm)])),
            value =>
            {
                finalization = value;
                return ValueTask.CompletedTask;
            });

        var verdict = await accumulator.EvaluateAsync(
            null!, "switch/hero", "light", ParityLeg.BlazorServer);
        await accumulator.DisposeAsync();

        verdict.Blocking.ShouldBeTrue();
        verdict.Findings.ShouldContain(item => item.Property == "invalid-batch-envelope");
        finalization.ShouldNotBeNull();
        finalization.RetryVerdict.Evidence.ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WrongResultOrEmbeddedCaptureIdentityBlocksImmediatelyAndStillFinalizes(bool bundleMismatch)
    {
        ParityRunFinalization? finalization = null;
        var accumulator = Accumulator(
            (_, fixture, options, attempt, root) =>
            {
                var server = Result(ParityLeg.BlazorServer);
                server = bundleMismatch
                    ? server with
                    {
                        Reference = server.Reference! with { Theme = "dark" },
                        Candidate = server.Candidate! with { Leg = ParityLeg.React }
                    }
                    : server with { ExecutionId = "switch/hero@dark" };
                return Task.FromResult(Batch(
                    fixture,
                    options,
                    attempt,
                    root,
                    [server, Result(ParityLeg.BlazorWasm)]));
            },
            value =>
            {
                finalization = value;
                return ValueTask.CompletedTask;
            });

        var verdict = await accumulator.EvaluateAsync(
            null!, "switch/hero", "light", ParityLeg.BlazorServer);
        await accumulator.DisposeAsync();

        verdict.Blocking.ShouldBeTrue();
        verdict.Findings.ShouldContain(item => item.Property == "invalid-result-identity");
        finalization.ShouldNotBeNull();
        var model = ParityRunAccumulator.BuildReportModel(finalization, Authority);
        model.Findings.ShouldContain(item => item.Effective.Property == "invalid-result-identity");
    }

    [Theory]
    [InlineData("")]
    [InlineData("unexpected-capture-step")]
    public async Task RunnerExecutionFailureOutsideManifestStepsRemainsNonwaivableThroughFinalization(
        string step)
    {
        ParityRunFinalization? finalization = null;
        var failure = Finding(
            ParityLeg.BlazorServer,
            FindingKind.FixtureError,
            "browser-context",
            "Browser context creation failed.") with { Step = step };
        var accumulator = Accumulator(
            (_, fixture, options, attempt, root) => Task.FromResult(Batch(
                fixture,
                options,
                attempt,
                root,
                [Result(ParityLeg.BlazorServer, failure), Result(ParityLeg.BlazorWasm)])),
            value =>
            {
                finalization = value;
                return ValueTask.CompletedTask;
            });

        var verdict = await accumulator.EvaluateAsync(
            null!, "switch/hero", "light", ParityLeg.BlazorServer);
        await accumulator.DisposeAsync();

        verdict.Blocking.ShouldBeTrue();
        verdict.Findings.ShouldHaveSingleItem().Step.ShouldBe(step);
        finalization.ShouldNotBeNull();
        finalization.RetryVerdict.Evidence.ShouldHaveSingleItem()
            .Classification.ShouldBe(RetryFindingClassification.NonWaivable);
        var model = ParityRunAccumulator.BuildReportModel(finalization, Authority);
        model.Findings.ShouldHaveSingleItem().Blocking.ShouldBeTrue();
    }

    [Theory]
    [InlineData("fingerprint")]
    [InlineData("snapshot")]
    [InlineData("authority")]
    [InlineData("duplicate-theme")]
    public void CrossAttemptLiveProvenanceDriftPublishesIncompleteTypedEvidence(string defect)
    {
        var fixture = Fixture() with { Themes = defect == "duplicate-theme" ? ["light", "dark"] : ["light"] };
        var options = new ParityOptions { Mode = ParityReferenceMode.Live };
        var firstLive = fixture.Themes.Select(theme => LiveSnapshot(fixture, theme)).ToArray();
        var first = Batch(fixture, options, 1, "attempt-1",
            fixture.Themes.SelectMany(theme => new[]
            {
                Result(ParityLeg.BlazorServer, theme: theme),
                Result(ParityLeg.BlazorWasm, theme: theme)
            }).ToArray()) with
        {
            LiveDistFingerprint = new string('9', 64),
            LiveProvenance = firstLive
        };
        var retry = Batch(fixture, options, 2, "attempt-2", first.Results) with
        {
            LiveDistFingerprint = defect == "fingerprint" ? new string('8', 64) : first.LiveDistFingerprint,
            LiveProvenance = defect switch
            {
                "snapshot" => [LiveSnapshot(fixture, "light", sourceHash: new string('E', 64))],
                "duplicate-theme" => [firstLive[0], firstLive[0]],
                _ => firstLive
            },
            Authority = defect == "authority"
                ? new BaselineAuthoritySnapshot(Authority().Authority with
                {
                    DeclaredRepositoryPin = new string('b', 40)
                })
                : first.Authority
        };

        var model = ParityRunAccumulator.BuildReportModel(Finalization(options, fixture, first, retry), Authority);

        model.Provenance.Complete.ShouldBeFalse();
        model.Provenance.UpstreamSha.ShouldBeNull();
        model.Diagnostics.ShouldContain(item =>
            item.Source == ReportDiagnosticSource.Provenance && item.Blocking);
        JsonReportWriter.Render(model).ShouldNotBeEmpty();
        HtmlReportWriter.Render(model).ShouldContain("Provenance");
    }

    [Fact]
    public void CrossAttemptBaselineSnapshotDriftPublishesIncompleteTypedEvidence()
    {
        var fixture = Fixture();
        var options = new ParityOptions { Mode = ParityReferenceMode.Baseline };
        var first = Batch(fixture, options, 1, "attempt-1",
            [Result(ParityLeg.BlazorServer), Result(ParityLeg.BlazorWasm)]) with
        {
            Baseline = BaselineSnapshot(new string('B', 64))
        };
        var retry = Batch(fixture, options, 2, "attempt-2", first.Results) with
        {
            Baseline = BaselineSnapshot(new string('C', 64))
        };

        var model = ParityRunAccumulator.BuildReportModel(Finalization(options, fixture, first, retry), Authority);

        model.Provenance.Complete.ShouldBeFalse();
        model.Diagnostics.ShouldContain(item => item.Kind == "BaselineSnapshotDrift" && item.Blocking);
        JsonReportWriter.Render(model).ShouldNotBeEmpty();
    }

    [Fact]
    public async Task FinalizerPublishesBeforeFailingReportOnlyBlockersAndAllowsCleanFilteredDiagnostics()
    {
        var fixture = Fixture();
        var baseline = new ParityOptions { Mode = ParityReferenceMode.Baseline };
        var first = Batch(fixture, baseline, 1, "attempt-1",
            [Result(ParityLeg.BlazorServer), Result(ParityLeg.BlazorWasm)]);
        var retry = Batch(fixture, baseline, 2, "attempt-2", first.Results);
        var published = false;

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await ParityRunAccumulator.FinalizeProductionAsync(
                Finalization(baseline, fixture, first, retry),
                _ =>
                {
                    published = true;
                    return ValueTask.CompletedTask;
                }));
        published.ShouldBeTrue();

        var filtered = baseline with { FixtureFilter = "switch/*" };
        var snapshot = BaselineSnapshot(new string('B', 64));
        var filteredFinalization = Finalization(
            filtered,
            fixture,
            first with { Options = filtered, Baseline = snapshot },
            retry with { Options = filtered, Baseline = snapshot });
        var filteredPublished = false;
        await ParityRunAccumulator.FinalizeProductionAsync(
            filteredFinalization,
            _ =>
            {
                filteredPublished = true;
                return ValueTask.CompletedTask;
            });
        filteredPublished.ShouldBeTrue();
    }

    [Fact]
    public async Task ArtifactOnlyFailurePublishesBlockingReportBeforeFinalizerFails()
    {
        using var directory = TestDirectory.Create();
        var fixture = Fixture();
        var options = new ParityOptions { Mode = ParityReferenceMode.Live };
        var missing = ScreenshotSet.Name(fixture.Id, "light", ParityLeg.React, "initial", "00");
        var first = LiveBatch(
            fixture,
            options,
            1,
            directory.Path,
            [Result(ParityLeg.BlazorServer, screenshots: [missing]), Result(ParityLeg.BlazorWasm)]);
        var retry = LiveBatch(fixture, options, 2, Path.Combine(directory.Path, "retry"),
            [Result(ParityLeg.BlazorServer), Result(ParityLeg.BlazorWasm)]);
        ReportModel? published = null;

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await ParityRunAccumulator.FinalizeProductionAsync(
                Finalization(options, fixture, first, retry),
                model =>
                {
                    published = model;
                    return ValueTask.CompletedTask;
                }));

        published.ShouldNotBeNull();
        published.Diagnostics.ShouldContain(item => item.Kind == "ArtifactMissing" && item.Blocking);
    }

    [Fact]
    public async Task ConfigurationFailurePreventsBrowserWorkPublishesThenFails()
    {
        var calls = 0;
        var loaded = ParityRunAccumulator.LoadOptions(
            () => throw new FormatException("PARITY_LIVE must be unset or exactly 1"));
        ParityRunFinalization? captured = null;
        var accumulator = Accumulator(
            (_, _, _, _, _) =>
            {
                calls++;
                throw new InvalidOperationException("configuration failures must not run");
            },
            value =>
            {
                captured = value;
                return ValueTask.CompletedTask;
            },
            policyDiagnostics: loaded.Diagnostics);

        var verdict = await accumulator.EvaluateAsync(
            null!, "switch/hero", "light", ParityLeg.BlazorServer);
        await accumulator.DisposeAsync();
        calls.ShouldBe(0);
        verdict.Blocking.ShouldBeTrue();
        captured.ShouldNotBeNull();

        var published = false;
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await ParityRunAccumulator.FinalizeProductionAsync(
                captured,
                _ =>
                {
                    published = true;
                    return ValueTask.CompletedTask;
                }));
        published.ShouldBeTrue();
    }

    [Fact]
    public void WriteBaselineReceiptDriftBecomesBlockingProvenanceDiagnostic()
    {
        var fixture = Fixture();
        var options = new ParityOptions { Mode = ParityReferenceMode.WriteBaseline };
        var receipt = new BaselineWriteReceipt(
            fixture.Id,
            Platform(),
            new DateTimeOffset(2026, 8, 10, 1, 0, 0, TimeSpan.Zero),
            new string('A', 64));
        var firstResults = new[]
        {
            Result(ParityLeg.BlazorServer) with { BaselineWrite = receipt },
            Result(ParityLeg.BlazorWasm) with { BaselineWrite = receipt }
        };
        var first = LiveBatch(fixture, options, 1, "attempt-1", firstResults);
        var retry = LiveBatch(fixture, options, 2, "attempt-2",
            firstResults.Select(item => item with
            {
                BaselineWrite = receipt with { CaptureSha256 = new string('B', 64) }
            }).ToArray());

        var model = ParityRunAccumulator.BuildReportModel(Finalization(options, fixture, first, retry), Authority);

        model.Diagnostics.ShouldContain(item => item.Kind == "BaselineWriteReceiptDrift" && item.Blocking);
    }

    [Theory]
    [InlineData("duplicate-step")]
    [InlineData("null-step")]
    [InlineData("null-steps")]
    [InlineData("null-screenshots")]
    [InlineData("null-result")]
    public void MalformedCaptureStepGraphsBecomeArtifactInvalidInsteadOfThrowing(string defect)
    {
        var result = Result(ParityLeg.BlazorServer);
        var step = result.Candidate!.Steps[0];
        IReadOnlyList<StepCapture>? steps = defect switch
        {
            "duplicate-step" => [step, step],
            "null-step" => [null!],
            "null-steps" => null,
            "null-screenshots" => [step with { ScreenshotObservations = null! }],
            _ => result.Candidate.Steps
        };
        result = result with { Candidate = result.Candidate with { Steps = steps! } };
        var batch = Batch(Fixture(), new ParityOptions { Mode = ParityReferenceMode.Live },
            1, "attempt-1", defect == "null-result" ? [null!] : [result]);

        var mapping = ParityRunAccumulator.BuildArtifactMapping([batch]);

        mapping.Sources.ShouldBeEmpty();
        mapping.Diagnostics.ShouldHaveSingleItem().Kind.ShouldBe("ArtifactInvalid");
    }

    [Fact]
    public void UnreadableDeclaredArtifactBecomesArtifactInvalid()
    {
        using var directory = TestDirectory.Create();
        var name = ScreenshotSet.Name("switch/hero", "light", ParityLeg.React, "initial", "00");
        File.WriteAllBytes(Path.Combine(directory.Path, name), [1, 2, 3]);
        var batch = Batch(
            Fixture(),
            new ParityOptions { Mode = ParityReferenceMode.Live },
            1,
            directory.Path,
            [Result(ParityLeg.BlazorServer, screenshots: [name])]);

        var mapping = ParityRunAccumulator.BuildArtifactMapping(
            [batch],
            _ => throw new UnauthorizedAccessException("denied /private/artifact.png"));

        mapping.Sources.ShouldBeEmpty();
        mapping.Diagnostics.ShouldHaveSingleItem().Kind.ShouldBe("ArtifactInvalid");
        mapping.Diagnostics.ShouldHaveSingleItem().Message.ShouldNotContain("/private/");
    }

    [Theory]
    [InlineData("duplicate-key")]
    [InlineData("illegal-first-receipt")]
    [InlineData("write-missing-receipt")]
    public async Task ReceiptValidationIsTotalAndPublishesBeforeFailure(string defect)
    {
        var fixture = Fixture();
        var options = new ParityOptions
        {
            Mode = defect == "write-missing-receipt"
                ? ParityReferenceMode.WriteBaseline
                : ParityReferenceMode.Live
        };
        var server = Result(ParityLeg.BlazorServer);
        var wasm = Result(ParityLeg.BlazorWasm);
        var receipt = new BaselineWriteReceipt(
            fixture.Id,
            Platform(),
            new DateTimeOffset(2026, 8, 10, 1, 0, 0, TimeSpan.Zero),
            new string('A', 64));
        var firstRows = defect == "duplicate-key"
            ? new[] { server, server, wasm }
            : defect == "illegal-first-receipt"
                ? new[] { server with { BaselineWrite = receipt }, wasm }
                : [server, wasm];
        var first = LiveBatch(fixture, options, 1, "attempt-1", firstRows);
        var retry = LiveBatch(fixture, options, 2, "attempt-2", [server, wasm]);
        var finalization = Finalization(options, fixture, first, retry) with
        {
            FirstResults = defect == "duplicate-key" ? [server, wasm] : firstRows
        };

        var model = ParityRunAccumulator.BuildReportModel(finalization, Authority);

        model.Diagnostics.ShouldContain(item => item.Kind == "BaselineWriteReceiptDrift" && item.Blocking);
        model.Executions.ShouldAllBe(item => item.BaselineWrite == null);
        JsonReportWriter.Render(model).ShouldNotBeEmpty();

        ReportModel? published = null;
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await ParityRunAccumulator.FinalizeProductionAsync(
                finalization,
                report =>
                {
                    published = report;
                    return ValueTask.CompletedTask;
                }));
        published.ShouldNotBeNull();
        published.Diagnostics.ShouldContain(item => item.Kind == "BaselineWriteReceiptDrift");
    }

    [Theory]
    [InlineData("manifest")]
    [InlineData("options")]
    [InlineData("waiver-io")]
    [InlineData("waiver-access")]
    public void BoundedConfigurationFailuresBecomeSanitizedBlockingDiagnostics(string defect)
    {
        IReadOnlyList<ReportDiagnostic> diagnostics = defect switch
        {
            "manifest" => ParityRunAccumulator.LoadManifest(
                () => throw new FileNotFoundException("missing /private/fixtures.json")).Diagnostics,
            "options" => ParityRunAccumulator.LoadOptions(
                () => throw new FormatException("PARITY_LIVE must be 1 at /private/env")).Diagnostics,
            "waiver-io" => ParityRunAccumulator.LoadWaivers(
                () => throw new IOException("cannot read /private/waivers.json")).Diagnostics,
            _ => ParityRunAccumulator.LoadWaivers(
                () => throw new UnauthorizedAccessException("denied /private/waivers.json")).Diagnostics
        };

        diagnostics.ShouldHaveSingleItem().Blocking.ShouldBeTrue();
        diagnostics.ShouldHaveSingleItem().Message.ShouldNotContain("/private/");
        Should.Throw<InvalidOperationException>(() => ParityRunAccumulator.LoadManifest(
            () => throw new InvalidOperationException("programmer bug")));
    }

    [Fact]
    public void DiscoveryKeepsTheDurableCatalogWhenManifestLoadingFails()
    {
        var manifest = ParityTheoryData.LoadManifestForDiscovery(
            () => throw new FileNotFoundException("fixtures.json is absent"));
        var cases = ParityTheoryData.Build(manifest);

        cases.Count.ShouldBe(58);
        cases.ShouldAllBe(item => !item.Authored && item.Theme == "light");
        cases.Select(item => item.Fixture).Distinct().ShouldBe(MilestoneFixtureCatalog.Ids);
    }

    [Theory]
    [InlineData(ParityLeg.BlazorServer)]
    [InlineData(ParityLeg.BlazorWasm)]
    public async Task DuplicateOneLegAndMissingOtherLegNeverCountsAsComplete(ParityLeg duplicatedLeg)
    {
        var missingLeg = duplicatedLeg == ParityLeg.BlazorServer
            ? ParityLeg.BlazorWasm
            : ParityLeg.BlazorServer;
        var accumulator = Accumulator((_, fixture, options, attempt, root) => Task.FromResult(Batch(
            fixture,
            options,
            attempt,
            root,
            [Result(duplicatedLeg), Result(duplicatedLeg)])));

        var duplicate = await accumulator.EvaluateAsync(
            null!, "switch/hero", "light", duplicatedLeg);
        var missing = await accumulator.EvaluateAsync(
            null!, "switch/hero", "light", missingLeg);

        duplicate.Blocking.ShouldBeTrue();
        duplicate.Findings.ShouldContain(item => item.Property == "duplicate-result");
        missing.Blocking.ShouldBeTrue();
        missing.Findings.ShouldContain(item => item.Property == "missing-result");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RetryClassificationPrecedesImmediateCaseVerdict(bool stable)
    {
        var finding = Finding(
            ParityLeg.BlazorServer,
            FindingKind.Attribute,
            "aria-checked",
            "candidate differs");
        var accumulator = Accumulator((_, fixture, options, attempt, root) => Task.FromResult(Batch(
            fixture,
            options,
            attempt,
            root,
            [
                Result(ParityLeg.BlazorServer, attempt == 1 || stable ? finding : null),
                Result(ParityLeg.BlazorWasm)
            ])));

        var verdict = await accumulator.EvaluateAsync(
            null!, "switch/hero", "light", ParityLeg.BlazorServer);

        verdict.Blocking.ShouldBe(stable);
        verdict.Findings.ShouldHaveSingleItem().Severity.ShouldBe(
            stable ? Severity.Error : Severity.Flaky);
    }

    [Fact]
    public async Task ChangedIdentityInSameCompleteScopeKeepsBothErrorsBlocking()
    {
        var first = Finding(
            ParityLeg.BlazorServer,
            FindingKind.Attribute,
            "aria-checked",
            "first identity");
        var retry = Finding(
            ParityLeg.BlazorServer,
            FindingKind.Attribute,
            "aria-expanded",
            "changed identity");
        ParityRunFinalization? finalization = null;
        var accumulator = Accumulator(
            (_, fixture, options, attempt, root) => Task.FromResult(Batch(
                fixture,
                options,
                attempt,
                root,
                [
                    Result(ParityLeg.BlazorServer, attempt == 1 ? first : retry),
                    Result(ParityLeg.BlazorWasm)
                ])),
            value =>
            {
                finalization = value;
                return ValueTask.CompletedTask;
            });

        var verdict = await accumulator.EvaluateAsync(
            null!, "switch/hero", "light", ParityLeg.BlazorServer);
        await accumulator.DisposeAsync();

        verdict.Blocking.ShouldBeTrue();
        verdict.Findings.Count.ShouldBe(2);
        verdict.Findings.ShouldAllBe(item => item.Severity == Severity.Error);
        finalization.ShouldNotBeNull();
        finalization.RetryVerdict.Evidence.ShouldAllBe(item =>
            item.Classification == RetryFindingClassification.IdentityChanged);
    }

    [Fact]
    public async Task StableFindingIsWaivedOnlyAfterRetryAndGlobalFinalizationPreservesApplication()
    {
        ParityRunFinalization? finalization = null;
        var finding = Finding(
            ParityLeg.BlazorServer,
            FindingKind.Attribute,
            "aria-checked",
            "candidate differs");
        var accumulator = Accumulator(
            (_, fixture, options, attempt, root) => Task.FromResult(Batch(
                fixture,
                options,
                attempt,
                root,
                [Result(ParityLeg.BlazorServer, finding), Result(ParityLeg.BlazorWasm)])),
            value =>
            {
                finalization = value;
                return ValueTask.CompletedTask;
            },
            new FrozenTimeProvider(new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero)),
            [Waiver(finding)]);

        var verdict = await accumulator.EvaluateAsync(
            null!, "switch/hero", "light", ParityLeg.BlazorServer);
        await accumulator.DisposeAsync();

        verdict.Blocking.ShouldBeFalse();
        finalization.ShouldNotBeNull();
        finalization.RetryVerdict.Evidence.ShouldHaveSingleItem()
            .Classification.ShouldBe(RetryFindingClassification.Stable);
        finalization.WaiverVerdict.Applied.ShouldHaveSingleItem()
            .Finding.ShouldBe(finding);
        finalization.WaiverVerdict.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task GloballyUnusedSelectedWaiverDoesNotPolluteImmediatePreviewButBlocksFinalPolicy()
    {
        ParityRunFinalization? finalization = null;
        var target = Finding(
            ParityLeg.BlazorServer,
            FindingKind.Attribute,
            "aria-checked",
            "not emitted");
        var accumulator = Accumulator(
            (_, fixture, options, attempt, root) => Task.FromResult(Batch(
                fixture,
                options,
                attempt,
                root,
                [Result(ParityLeg.BlazorServer), Result(ParityLeg.BlazorWasm)])),
            value =>
            {
                finalization = value;
                return ValueTask.CompletedTask;
            },
            new FrozenTimeProvider(new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero)),
            [Waiver(target)]);

        var verdict = await accumulator.EvaluateAsync(
            null!, "switch/hero", "light", ParityLeg.BlazorServer);
        await accumulator.DisposeAsync();

        verdict.Blocking.ShouldBeFalse();
        verdict.Message.ShouldNotContain("Unused");
        finalization.ShouldNotBeNull();
        finalization.WaiverVerdict.Diagnostics.ShouldHaveSingleItem()
            .Kind.ShouldBe(WaiverDiagnosticKind.Unused);
    }

    [Fact]
    public async Task FilteredSelectedWaiverPreservesItsOriginalRegistryIndexInReport()
    {
        ParityRunFinalization? finalization = null;
        var switchFinding = Finding(
            ParityLeg.BlazorServer,
            FindingKind.Attribute,
            "aria-checked",
            "stable selected finding");
        var excludedFinding = switchFinding with
        {
            Fixture = "collapsible/hero@light",
            Property = "aria-expanded"
        };
        var accumulator = Accumulator(
            (_, fixture, options, attempt, root) => Task.FromResult(Batch(
                fixture,
                options,
                attempt,
                root,
                [Result(ParityLeg.BlazorServer, switchFinding), Result(ParityLeg.BlazorWasm)])),
            value =>
            {
                finalization = value;
                return ValueTask.CompletedTask;
            },
            new FrozenTimeProvider(new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero)),
            [Waiver(excludedFinding), Waiver(switchFinding)],
            new ParityOptions
            {
                Mode = ParityReferenceMode.Baseline,
                FixtureFilter = "switch/*"
            });

        var verdict = await accumulator.EvaluateAsync(
            null!, "switch/hero", "light", ParityLeg.BlazorServer);
        await accumulator.DisposeAsync();

        verdict.Blocking.ShouldBeFalse();
        finalization.ShouldNotBeNull();
        finalization.WaiverVerdict.Applied.ShouldHaveSingleItem().WaiverIndex.ShouldBe(1);
        finalization.WaiverVerdict.Diagnostics.ShouldBeEmpty();
        var model = ParityRunAccumulator.BuildReportModel(finalization, Authority);
        model.AppliedWaivers.ShouldHaveSingleItem().WaiverIndex.ShouldBe(1);
    }

    [Theory]
    [InlineData(ParityReferenceMode.Baseline, "BaselineSnapshotUnavailable")]
    [InlineData(ParityReferenceMode.Live, "LiveProvenanceUnavailable")]
    public void FailedProvenanceStillBuildsRenderableBlockingReport(
        ParityReferenceMode mode,
        string diagnosticKind)
    {
        var fixture = Fixture();
        var options = new ParityOptions { Mode = mode };
        var first = Batch(
            fixture,
            options,
            1,
            "attempt-1",
            [Result(ParityLeg.BlazorServer), Result(ParityLeg.BlazorWasm)]);
        var retry = Batch(
            fixture,
            options,
            2,
            "attempt-2",
            [Result(ParityLeg.BlazorServer), Result(ParityLeg.BlazorWasm)]);
        var finalization = Finalization(options, fixture, first, retry);

        var model = ParityRunAccumulator.BuildReportModel(finalization);
        var json = System.Text.Encoding.UTF8.GetString(JsonReportWriter.Render(model));
        var html = HtmlReportWriter.Render(model);

        model.Provenance.Complete.ShouldBeFalse();
        model.Provenance.Platform.ShouldBeNull();
        model.Provenance.UpstreamSha.ShouldBeNull();
        model.Diagnostics.ShouldContain(item =>
            item.Kind == diagnosticKind && item.Blocking);
        json.ShouldContain(diagnosticKind);
        json.ShouldContain("\"upstreamSha\": null");
        html.ShouldContain(diagnosticKind);
    }

    [Fact]
    public void MissingAuthorityPublishesNullAuthorityFieldsWithBlockingDiagnostic()
    {
        var fixture = Fixture();
        var options = new ParityOptions { Mode = ParityReferenceMode.Live };
        var results = new[] { Result(ParityLeg.BlazorServer), Result(ParityLeg.BlazorWasm) };
        var first = Batch(fixture, options, 1, "attempt-1", results) with { Authority = null };
        var retry = Batch(fixture, options, 2, "attempt-2", results) with { Authority = null };
        var model = ParityRunAccumulator.BuildReportModel(
            Finalization(options, fixture, first, retry),
            () => throw new FormatException("authority metadata corrupt at /private/root"));

        model.Provenance.Complete.ShouldBeFalse();
        model.Provenance.DeclaredRepositoryPin.ShouldBeNull();
        model.Provenance.UpstreamSha.ShouldBeNull();
        model.Provenance.AuthoritySchemaVersion.ShouldBeNull();
        model.Provenance.CaptureSchemaVersion.ShouldBeNull();
        model.Diagnostics.ShouldContain(item => item.Kind == "AuthorityUnavailable" && item.Blocking);
        var json = System.Text.Encoding.UTF8.GetString(JsonReportWriter.Render(model));
        var html = HtmlReportWriter.Render(model);
        json.ShouldContain("\"declaredRepositoryPin\": null");
        json.ShouldContain("\"authoritySchemaVersion\": null");
        html.ShouldContain("AuthorityUnavailable");
        html.ShouldContain("Not recorded");
    }

    [Fact]
    public async Task MalformedWaiverLoaderBecomesTypedBlockingDiagnosticAndFinalizesWithoutCases()
    {
        var loaded = ParityRunAccumulator.LoadWaivers(
            () => throw new FormatException("waiver entry 0 is malformed"));
        ParityRunFinalization? finalization = null;
        var accumulator = Accumulator(
            (_, _, _, _, _) => throw new InvalidOperationException("must remain dormant"),
            value =>
            {
                finalization = value;
                return ValueTask.CompletedTask;
            },
            policyDiagnostics: loaded.Diagnostics);

        await accumulator.DisposeAsync();

        loaded.Waivers.ShouldBeEmpty();
        loaded.Diagnostics.ShouldHaveSingleItem().Source.ShouldBe(ReportDiagnosticSource.WaiverLoader);
        finalization.ShouldNotBeNull();
        finalization.PolicyDiagnostics.ShouldHaveSingleItem().Kind.ShouldBe("WaiverFileInvalid");
    }

    [Fact]
    public void FilteredDiagnosticIdIsBoundedDeterministicAndCollisionResistant()
    {
        var slash = ParityRunAccumulator.DiagnosticId("a/b");
        var dash = ParityRunAccumulator.DiagnosticId("a-b");
        var longValue = ParityRunAccumulator.DiagnosticId(new string('x', 500));

        slash.ShouldNotBe(dash);
        slash.ShouldBe(ParityRunAccumulator.DiagnosticId("a/b"));
        slash.ShouldStartWith("a-b-");
        longValue.Length.ShouldBeLessThanOrEqualTo(53);
        longValue.ShouldMatch("^[a-z0-9-]+$");
    }

    [Fact]
    public async Task UnexpectedResultIdentityBecomesInScopeTypedEvidenceInsteadOfBreakingRetry()
    {
        var unexpected = Result(ParityLeg.BlazorServer) with
        {
            Fixture = "other/fixture",
            ExecutionId = "other/fixture@light"
        };
        var accumulator = Accumulator((_, fixture, options, attempt, root) => Task.FromResult(Batch(
            fixture,
            options,
            attempt,
            root,
            [Result(ParityLeg.BlazorServer), Result(ParityLeg.BlazorWasm), unexpected])));

        var verdict = await accumulator.EvaluateAsync(
            null!, "switch/hero", "light", ParityLeg.BlazorServer);

        verdict.Blocking.ShouldBeTrue();
        verdict.Findings.ShouldContain(item =>
            item.Kind == FindingKind.FixtureError &&
            item.Property == "unexpected-result" &&
            item.Fixture == "switch/hero@light");
    }

    [Fact]
    public async Task FilterExcludesUnrelatedUnusedWaiverFromImmediateAndGlobalPolicy()
    {
        ParityRunFinalization? finalization = null;
        var unrelatedFinding = Finding(
            ParityLeg.BlazorServer,
            FindingKind.Attribute,
            "aria-expanded",
            "not emitted") with { Fixture = "collapsible/hero@light" };
        var accumulator = Accumulator(
            (_, fixture, options, attempt, root) => Task.FromResult(Batch(
                fixture,
                options,
                attempt,
                root,
                [Result(ParityLeg.BlazorServer), Result(ParityLeg.BlazorWasm)])),
            value =>
            {
                finalization = value;
                return ValueTask.CompletedTask;
            },
            new FrozenTimeProvider(new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero)),
            [Waiver(unrelatedFinding)],
            new ParityOptions
            {
                Mode = ParityReferenceMode.Baseline,
                FixtureFilter = "switch/*"
            });

        var verdict = await accumulator.EvaluateAsync(
            null!, "switch/hero", "light", ParityLeg.BlazorServer);
        await accumulator.DisposeAsync();

        verdict.Blocking.ShouldBeFalse();
        finalization.ShouldNotBeNull();
        finalization.WaiverVerdict.Diagnostics.ShouldBeEmpty();
        finalization.WaiverVerdict.Applied.ShouldBeEmpty();
    }

    [Fact]
    public async Task PublicGuardRejectsReferenceLegAndNonCatalogFixture()
    {
        var accumulator = Accumulator((_, _, _, _, _) =>
            throw new InvalidOperationException("guarded requests must not run"));

        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => accumulator.EvaluateAsync(
            null!, "switch/hero", "light", ParityLeg.React));
        await Should.ThrowAsync<ArgumentException>(() => accumulator.EvaluateAsync(
            null!, "harness/canary", "light", ParityLeg.BlazorServer));
    }

    private static ParityRunAccumulator Accumulator(
        Func<IBrowser, FixtureEntry, ParityOptions, int, string, Task<ParityRunBatch>> run,
        Func<ParityRunFinalization, ValueTask>? finalize = null,
        TimeProvider? timeProvider = null,
        IReadOnlyList<Waiver>? waivers = null,
        ParityOptions? options = null,
        IReadOnlyList<ReportDiagnostic>? policyDiagnostics = null)
        => Accumulator(
            [Fixture()], run, finalize, timeProvider, waivers, options, policyDiagnostics);

    private static ParityRunAccumulator Accumulator(
        IReadOnlyList<FixtureEntry> fixtures,
        Func<IBrowser, FixtureEntry, ParityOptions, int, string, Task<ParityRunBatch>> run,
        Func<ParityRunFinalization, ValueTask>? finalize = null,
        TimeProvider? timeProvider = null,
        IReadOnlyList<Waiver>? waivers = null,
        ParityOptions? options = null,
        IReadOnlyList<ReportDiagnostic>? policyDiagnostics = null)
        => new(
            fixtures,
            options ?? new ParityOptions { Mode = ParityReferenceMode.Baseline },
            waivers ?? [],
            run,
            finalize ?? (_ => ValueTask.CompletedTask),
            Path.Combine(Path.GetTempPath(), "blazix-parity-accumulator-tests", Guid.NewGuid().ToString("N")),
            timeProvider,
            _ => new BaselinePlatform
            {
                Browser = "chromium",
                BrowserVersion = "140.0.0.0",
                Os = "linux",
                Architecture = "x64"
            },
            policyDiagnostics);

    private static ParityRunBatch Batch(
        FixtureEntry fixture,
        ParityOptions options,
        int attempt,
        string root,
        IReadOnlyList<ParityRunResult> results) => new()
        {
            Attempt = attempt,
            Fixture = fixture,
            Options = options,
            ArtifactRoot = root,
            Platform = Platform(),
            Duration = TimeSpan.FromMilliseconds(attempt),
            Results = results,
            Authority = Authority()
        };

    private static ParityRunBatch LiveBatch(
        FixtureEntry fixture,
        ParityOptions options,
        int attempt,
        string root,
        IReadOnlyList<ParityRunResult> results)
        => Batch(fixture, options, attempt, root, results) with
        {
            LiveDistFingerprint = new string('9', 64),
            LiveProvenance = fixture.Themes.Select(theme => LiveSnapshot(fixture, theme)).ToArray()
        };

    private static ParityRunFinalization Finalization(
        ParityOptions options,
        FixtureEntry fixture,
        ParityRunBatch first,
        ParityRunBatch retry) => new()
        {
            GeneratedAtUtc = new DateTimeOffset(2026, 8, 10, 1, 0, 0, TimeSpan.Zero),
            Options = options,
            Fixtures = [fixture],
            FirstBatches = [first],
            RetryBatches = [retry],
            FirstResults = first.Results,
            RetryVerdict = new RetryVerdict
            {
                Evidence = [],
                Findings = [],
                Failures = []
            },
            WaiverVerdict = new WaiverVerdict
            {
                Findings = [],
                Applied = [],
                BlockingFindings = [],
                NonWaivableFindings = [],
                Diagnostics = []
            }
        };

    private static BaselineAuthoritySnapshot Authority() => new(new BaselineAuthority
    {
        SchemaVersion = BaselineAuthority.CurrentSchemaVersion,
        CaptureSchemaVersion = BaselineAuthority.CurrentCaptureSchemaVersion,
        DeclaredRepositoryPin = new string('a', 40)
    });

    private static BaselinePlatform Platform() => new()
    {
        Browser = "chromium",
        BrowserVersion = "140.0.0.0",
        Os = "linux",
        Architecture = "x64"
    };

    private static LiveFixtureProvenanceSnapshot LiveSnapshot(
        FixtureEntry fixture,
        string theme,
        string? sourceHash = null)
        => new(
            fixture.Id,
            theme,
            new string('D', 64),
            new LiveBaselineProvenance(
                new string('a', 40),
                "docs/src/app/(docs)/react/components/" + fixture.React,
                sourceHash ?? new string('A', 64),
                new DateTimeOffset(2026, 8, 10, 1, 0, 0, TimeSpan.Zero)));

    private static BaselineSnapshot BaselineSnapshot(string fixtureManifestHash)
        => new(
            Authority().Authority,
            new BaselineSetMetadata
            {
                SchemaVersion = BaselineAuthority.CurrentSchemaVersion,
                CaptureSchemaVersion = BaselineAuthority.CurrentCaptureSchemaVersion,
                UpstreamSha = new string('a', 40),
                Platform = Platform(),
                GeneratedAtUtc = new DateTimeOffset(2026, 8, 10, 1, 0, 0, TimeSpan.Zero),
                FixtureManifestHash = fixtureManifestHash,
                AliasManifestHash = new string('C', 64),
                StylesheetHash = new string('D', 64),
                Fixtures =
                [
                    new BaselineFixtureMetadata
                    {
                        Fixture = "switch/hero",
                        SourcePath = "switch/demos/hero/tailwind/index.tsx",
                        SourceHash = new string('A', 64),
                        ContractHash = new string('E', 64),
                        Theme = "light",
                        Steps = ["initial"],
                        Capture = "captures/switch__hero.light.json",
                        Artifacts = []
                    }
                ]
            });

    private static FixtureEntry Fixture() => new()
    {
        Id = "switch/hero",
        Component = "switch",
        React = "switch/demos/hero/tailwind/index.tsx",
        Blazor = "Switch/Hero",
        Themes = ["light"],
        Steps = [new StepEntry { Name = "initial" }]
    };

    private static ParityRunResult Result(
        ParityLeg leg,
        Finding? finding = null,
        IReadOnlyList<string>? screenshots = null,
        IReadOnlyList<string>? candidateScreenshots = null,
        string theme = "light")
    {
        var reference = Bundle(ParityLeg.React, screenshots, theme);
        var candidate = Bundle(leg, candidateScreenshots, theme);
        return new ParityRunResult
        {
            Fixture = "switch/hero",
            Theme = theme,
            ExecutionId = $"switch/hero@{theme}",
            Leg = leg,
            Reference = reference,
            Candidate = candidate,
            Findings = finding is null ? [] : [finding]
        };
    }

    private static CaptureBundle Bundle(
        ParityLeg leg,
        IReadOnlyList<string>? screenshots = null,
        string theme = "light") => new()
    {
        CaptureSchemaVersion = CaptureSchema.CurrentVersion,
        Fixture = "switch/hero",
        Theme = theme,
        Leg = leg,
        BaseUiSha = new string('a', 40),
        SourceHash = new string('A', 64),
        Steps =
        [
            new StepCapture
            {
                Step = "initial",
                Dom = new DomNode
                {
                    Tag = "button",
                    Path = "root > button",
                    Attributes = new Dictionary<string, string>(),
                    Classes = [],
                    Text = string.Empty,
                    Children = []
                },
                Styles = new Dictionary<string, IReadOnlyDictionary<string, string>>(),
                CustomProps = new Dictionary<string, IReadOnlyDictionary<string, string>>(),
                Geometry = new Dictionary<string, IReadOnlyDictionary<string, double>>(),
                Actions = [],
                ActionCompletionFailures = [],
                Screenshots = screenshots ?? []
            }
        ]
    };

    private static Finding Finding(
        ParityLeg leg,
        FindingKind kind,
        string property,
        string message) => new()
        {
            Fixture = "switch/hero@light",
            Leg = leg,
            Step = "initial",
            Kind = kind,
            Severity = Severity.Error,
            Property = property,
            Message = message
        };

    private static Waiver Waiver(Finding finding) => new()
    {
        Fixture = finding.Fixture,
        Leg = finding.Leg,
        Step = finding.Step,
        NodePath = finding.NodePath,
        Kind = finding.Kind,
        Property = finding.Property,
        Reason = "Documented parity limitation retained for explicit review.",
        Disposition = WaiverDisposition.AcceptedLimitation,
        DocLink = "docs/audits/switch-functional-audit.md",
        Expires = new DateOnly(2027, 8, 10)
    };

    private sealed class FrozenTimeProvider(DateTimeOffset instant) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => instant;
    }

    private sealed class TestDirectory : IDisposable
    {
        private TestDirectory(string path) => Path = path;

        public string Path { get; }

        public static TestDirectory Create()
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "blazix-parity-artifacts",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return new TestDirectory(path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
