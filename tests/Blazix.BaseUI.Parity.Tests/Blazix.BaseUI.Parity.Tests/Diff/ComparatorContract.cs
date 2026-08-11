namespace Blazix.BaseUI.Parity.Tests.Diff;

/// <summary>One reportable numeric comparator threshold.</summary>
/// <param name="Name">The stable threshold token.</param>
/// <param name="Value">The invariant numeric value.</param>
/// <param name="Unit">The stable unit token.</param>
public sealed record ComparatorThreshold(string Name, double Value, string Unit);

/// <summary>Reportable numeric policy for one comparator kind.</summary>
/// <param name="Kind">The finding kind that consumes the thresholds.</param>
/// <param name="Thresholds">The thresholds in deterministic presentation order.</param>
public sealed record ComparatorDescriptor(
    FindingKind Kind,
    IReadOnlyList<ComparatorThreshold> Thresholds);

/// <summary>Single reportable authority for comparator numeric thresholds.</summary>
public static class ComparatorContract
{
    internal const string NumericTolerance = "numericTolerance";
    internal const string DurationToleranceFloor = "durationToleranceFloor";
    internal const string DurationRelativeTolerance = "durationRelativeTolerance";
    internal const string MaximumDiffLines = "maximumDiffLines";
    internal const string ChannelTolerance = "channelTolerance";

    private static readonly IReadOnlyList<ComparatorDescriptor> CurrentDescriptors =
        Array.AsReadOnly<ComparatorDescriptor>(
        [
            Descriptor(FindingKind.Structure),
            Descriptor(FindingKind.CorrespondenceUncertain),
            Descriptor(FindingKind.Attribute),
            Descriptor(FindingKind.AriaSnapshot),
            Descriptor(
                FindingKind.ComputedStyle,
                new ComparatorThreshold(NumericTolerance, 0.5, "css-unit")),
            Descriptor(
                FindingKind.CustomProperty,
                new ComparatorThreshold(NumericTolerance, 0.5, "css-unit")),
            Descriptor(
                FindingKind.Geometry,
                new ComparatorThreshold(NumericTolerance, 1.0, "css-pixel")),
            Descriptor(FindingKind.Focus),
            Descriptor(FindingKind.Console),
            Descriptor(FindingKind.Marker),
            Descriptor(
                FindingKind.Timeline,
                new ComparatorThreshold(DurationToleranceFloor, 50, "millisecond"),
                new ComparatorThreshold(DurationRelativeTolerance, 0.5, "ratio"),
                new ComparatorThreshold(MaximumDiffLines, 40, "line")),
            Descriptor(
                FindingKind.Pixel,
                new ComparatorThreshold(ChannelTolerance, 8, "channel-level")),
            Descriptor(FindingKind.SelectorUnresolved),
            Descriptor(FindingKind.SelectorNonActionable)
        ]);

    /// <summary>Gets numeric comparator policy in deterministic presentation order.</summary>
    public static IReadOnlyList<ComparatorDescriptor> Descriptors => CurrentDescriptors;

    internal static double Value(FindingKind kind, string name)
        => CurrentDescriptors
            .Single(descriptor => descriptor.Kind == kind)
            .Thresholds
            .Single(threshold => string.Equals(threshold.Name, name, StringComparison.Ordinal))
            .Value;

    private static ComparatorDescriptor Descriptor(
        FindingKind kind,
        params ComparatorThreshold[] thresholds)
        => new(kind, Array.AsReadOnly(thresholds));
}
