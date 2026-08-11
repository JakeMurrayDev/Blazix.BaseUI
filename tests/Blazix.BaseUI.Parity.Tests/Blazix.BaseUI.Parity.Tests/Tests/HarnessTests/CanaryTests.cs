using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Diff;
using Blazix.BaseUI.Parity.Tests.Fixtures;
using Blazix.BaseUI.Parity.Tests.Infrastructure;
using Blazix.BaseUI.Parity.Tests.Waivers;
using Shouldly;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

/// <summary>Proves the deliberately broken reserved fixture reaches the production registry.</summary>
/// <param name="playwright">The production browser fixture.</param>
public sealed class CanaryTests(PlaywrightFixture playwright)
    : IClassFixture<PlaywrightFixture>, IDisposable
{
    private readonly string artifacts = Path.Combine(
        Path.GetTempPath(), "blazix-parity-canary", Guid.NewGuid().ToString("N"));

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(artifacts))
        {
            Directory.Delete(artifacts, recursive: true);
        }
    }

    [Fact]
    public void ReservedCanaryIsNotOrdinaryManifestData()
    {
        FixtureManifest.Load().ShouldNotContain(entry => entry.Id == "harness/canary");
        Client.FixtureRegistry.Ids.ShouldContain("harness/canary");
    }

    [Fact]
    public async Task EmitsExactlyTheKnownAttributeColourAndDurationFindingsInBothModes()
    {
        var fixture = CanaryFixture();
        var options = new ParityOptions { Mode = ParityReferenceMode.Live };
        var first = await new ParityRunner(Path.Combine(artifacts, "attempt-1"))
            .RunBatchAsync(playwright.Browser, fixture, options, attempt: 1);
        var retry = await new ParityRunner(Path.Combine(artifacts, "attempt-2"))
            .RunBatchAsync(playwright.Browser, fixture, options, attempt: 2);
        var scopes = new[]
        {
            new RetryScope("harness/canary@light", ParityLeg.BlazorServer, "initial"),
            new RetryScope("harness/canary@light", ParityLeg.BlazorWasm, "initial")
        };
        var retryVerdict = RetryClassifier.Classify(
            Attempt(first.Results, scopes),
            Attempt(retry.Results, scopes));
        var waiverVerdict = WaiverMatcher.Match(
            retryVerdict.Findings,
            [],
            new DateOnly(2026, 8, 10));
        retryVerdict.Evidence.Count.ShouldBe(6);
        retryVerdict.Evidence.ShouldAllBe(item =>
            item.Classification == RetryFindingClassification.Stable);
        retryVerdict.Failures.ShouldBeEmpty();
        waiverVerdict.BlockingFindings.Count.ShouldBe(6);
        waiverVerdict.Applied.ShouldBeEmpty();
        var results = first.Results;

        results.Select(result => result.Leg)
            .ShouldBe([ParityLeg.BlazorServer, ParityLeg.BlazorWasm]);

        foreach (var result in results)
        {
            foreach (var finding in result.Findings)
            {
                Console.WriteLine(
                    $"[canary:{result.Leg}] {finding.Kind} {finding.NodePath} " +
                    $"{finding.Property}: {finding.ReferenceValue} -> {finding.CandidateValue}");
            }

            result.Findings.Count.ShouldBe(
                3,
                string.Join(
                    Environment.NewLine,
                    result.Findings.Select(finding =>
                        $"{finding.Kind} {finding.NodePath} {finding.Property}: " +
                        $"{finding.ReferenceValue} -> {finding.CandidateValue}")));
            result.Findings.ShouldAllBe(finding => finding.Severity == Severity.Error);
            result.Findings.ShouldAllBe(finding => finding.Leg == result.Leg);
            result.Findings.ShouldAllBe(finding => finding.Fixture == "harness/canary@light");
            result.Findings.ShouldAllBe(finding => finding.Step == "initial");

            var expanded = result.Findings
                .Where(finding => finding.Kind == FindingKind.Attribute)
                .ShouldHaveSingleItem();
            expanded.Property.ShouldBe("aria-expanded");
            expanded.ReferenceValue.ShouldBe("true");
            expanded.CandidateValue.ShouldBeNull();

            var colour = result.Findings
                .Where(finding =>
                    finding.Kind == FindingKind.ComputedStyle &&
                    finding.Property == "background-color")
                .ShouldHaveSingleItem();
            colour.ReferenceValue.ShouldBe("rgb(0, 128, 0)");
            colour.CandidateValue.ShouldBe("rgb(255, 0, 0)");

            var duration = result.Findings
                .Where(finding =>
                    finding.Kind == FindingKind.ComputedStyle &&
                    finding.Property == "transition-duration")
                .ShouldHaveSingleItem();
            duration.ReferenceValue.ShouldBe("0.1s");
            duration.CandidateValue.ShouldBe("0.4s");
        }

        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{ParityServerAssemblyFixture.ServerAddress}/react/");
        (await page.Locator("#fixtures a[href='#/fixture/harness/canary']").CountAsync())
            .ShouldBe(0);
    }

    private static FixtureEntry CanaryFixture() => new()
    {
        Id = "harness/canary",
        Component = "harness",
        React = "internal:canary",
        Blazor = "Harness/Canary",
        PixelThreshold = 0,
        Steps = [new StepEntry { Name = "initial" }]
    };

    private static RetryAttempt Attempt(
        IReadOnlyList<ParityRunResult> results,
        IReadOnlyList<RetryScope> scopes) => new()
        {
            Scopes = scopes,
            Findings = results.SelectMany(item => item.Findings).ToArray()
        };
}
