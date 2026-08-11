using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Fixtures;
using Blazix.BaseUI.Parity.Tests.Infrastructure;

namespace Blazix.BaseUI.Parity.Tests.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ParityTimingCollection
{
    public const string Name = "Parity timing";
}

public abstract class ParityTheoryBase(
    PlaywrightFixture playwright,
    ParityRunAccumulator accumulator)
{
    protected async Task AssertParityAsync(string fixture, string theme, ParityLeg leg)
    {
        var verdict = await accumulator.EvaluateAsync(playwright.Browser, fixture, theme, leg);
        Assert.False(verdict.Blocking, verdict.Message);
    }
}

public sealed class ParityStaticShard0Tests(
    PlaywrightFixture playwright,
    ParityRunAccumulator accumulator)
    : ParityTheoryBase(playwright, accumulator), IClassFixture<PlaywrightFixture>
{
    [Theory]
    [MemberData(nameof(ParityTheoryData.StaticShard0), MemberType = typeof(ParityTheoryData))]
    public Task MatchesReact(string fixture, string theme, ParityLeg leg)
        => AssertParityAsync(fixture, theme, leg);
}

public sealed class ParityStaticShard1Tests(
    PlaywrightFixture playwright,
    ParityRunAccumulator accumulator)
    : ParityTheoryBase(playwright, accumulator), IClassFixture<PlaywrightFixture>
{
    [Theory]
    [MemberData(nameof(ParityTheoryData.StaticShard1), MemberType = typeof(ParityTheoryData))]
    public Task MatchesReact(string fixture, string theme, ParityLeg leg)
        => AssertParityAsync(fixture, theme, leg);
}

public sealed class ParityStaticShard2Tests(
    PlaywrightFixture playwright,
    ParityRunAccumulator accumulator)
    : ParityTheoryBase(playwright, accumulator), IClassFixture<PlaywrightFixture>
{
    [Theory]
    [MemberData(nameof(ParityTheoryData.StaticShard2), MemberType = typeof(ParityTheoryData))]
    public Task MatchesReact(string fixture, string theme, ParityLeg leg)
        => AssertParityAsync(fixture, theme, leg);
}

public sealed class ParityStaticShard3Tests(
    PlaywrightFixture playwright,
    ParityRunAccumulator accumulator)
    : ParityTheoryBase(playwright, accumulator), IClassFixture<PlaywrightFixture>
{
    [Theory]
    [MemberData(nameof(ParityTheoryData.StaticShard3), MemberType = typeof(ParityTheoryData))]
    public Task MatchesReact(string fixture, string theme, ParityLeg leg)
        => AssertParityAsync(fixture, theme, leg);
}

[Collection(ParityTimingCollection.Name)]
public sealed class ParityTimingTests(
    PlaywrightFixture playwright,
    ParityRunAccumulator accumulator)
    : ParityTheoryBase(playwright, accumulator), IClassFixture<PlaywrightFixture>
{
    [Theory(SkipTestWithoutData = true)]
    [MemberData(nameof(ParityTheoryData.Timing), MemberType = typeof(ParityTheoryData))]
    public Task MatchesReact(string fixture, string theme, ParityLeg leg)
        => AssertParityAsync(fixture, theme, leg);
}
