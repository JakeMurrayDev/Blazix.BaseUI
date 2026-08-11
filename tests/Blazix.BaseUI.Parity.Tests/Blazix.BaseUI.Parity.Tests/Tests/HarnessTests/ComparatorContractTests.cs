using Blazix.BaseUI.Parity.Tests.Diff;
using Blazix.BaseUI.Parity.Tests.Infrastructure;
using Shouldly;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

public sealed class ComparatorContractTests
{
    [Fact]
    public void ExposesDeterministicReportableNumericThresholds()
    {
        ComparatorContract.Descriptors
            .SelectMany(descriptor => descriptor.Thresholds.Select(threshold =>
                (descriptor.Kind, threshold.Name, threshold.Value, threshold.Unit)))
            .ShouldBe(
        [
            (FindingKind.ComputedStyle, "numericTolerance", 0.5, "css-unit"),
            (FindingKind.CustomProperty, "numericTolerance", 0.5, "css-unit"),
            (FindingKind.Geometry, "numericTolerance", 1.0, "css-pixel"),
            (FindingKind.Timeline, "durationToleranceFloor", 50.0, "millisecond"),
            (FindingKind.Timeline, "durationRelativeTolerance", 0.5, "ratio"),
            (FindingKind.Timeline, "maximumDiffLines", 40.0, "line"),
            (FindingKind.Pixel, "channelTolerance", 8.0, "channel-level")
        ]);
    }

    [Fact]
    public void DescriptorCollectionsAreReadOnlyAndPixelThresholdRemainsFixtureOwned()
    {
        ComparatorContract.Descriptors
            .ShouldBeAssignableTo<IList<ComparatorDescriptor>>()
            .IsReadOnly.ShouldBeTrue();
        ComparatorContract.Descriptors.ShouldAllBe(descriptor =>
            descriptor.Thresholds.ShouldBeAssignableTo<IList<ComparatorThreshold>>().IsReadOnly);
        ComparatorContract.Descriptors
            .SelectMany(descriptor => descriptor.Thresholds)
            .ShouldNotContain(threshold =>
                string.Equals(threshold.Name, "pixelThreshold", StringComparison.Ordinal));
        typeof(FixtureEntry).GetProperty(nameof(FixtureEntry.PixelThreshold))
            .ShouldNotBeNull().PropertyType.ShouldBe(typeof(double));
    }
}
