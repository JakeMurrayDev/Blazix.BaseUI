namespace Blazix.BaseUI.Parity.Tests.Diff;

/// <summary>The category of a parity finding.</summary>
public enum FindingKind
{
    /// <summary>A node exists on one side only, or sibling ordering differs.</summary>
    Structure,

    /// <summary>An attribute value differs, or an attribute is present on one side only.</summary>
    Attribute,

    /// <summary>The ARIA snapshot differs.</summary>
    AriaSnapshot,

    /// <summary>An allowlisted computed style property differs.</summary>
    ComputedStyle,

    /// <summary>A CSS custom property differs.</summary>
    CustomProperty,

    /// <summary>A bounding rectangle differs beyond tolerance.</summary>
    Geometry,

    /// <summary>Focus landed on a different node.</summary>
    Focus,

    /// <summary>A console message appeared on one side only.</summary>
    Console,

    /// <summary>A Blazix marker attribute is unclassified or Blazor-only.</summary>
    Marker,

    /// <summary>The animation event sequence or phase ordering differs.</summary>
    Timeline,

    /// <summary>Screenshot mismatch exceeded the fixture's threshold.</summary>
    Pixel,

    /// <summary>A step selector resolved on one side only.</summary>
    SelectorUnresolved,

    /// <summary>The fixture failed to load or threw.</summary>
    FixtureError
}

/// <summary>How a finding affects the verdict.</summary>
public enum Severity
{
    /// <summary>Fails the test unless waived.</summary>
    Error,

    /// <summary>Reported for context; never fails.</summary>
    Info,

    /// <summary>Differed between retry attempts; reported but never fails.</summary>
    Flaky
}

/// <summary>A single difference between two legs of a fixture.</summary>
public sealed record Finding
{
    /// <summary>Gets the fixture id.</summary>
    public required string Fixture { get; init; }

    /// <summary>Gets the leg being compared against the React reference.</summary>
    public required Capture.ParityLeg Leg { get; init; }

    /// <summary>Gets the manifest step name.</summary>
    public required string Step { get; init; }

    /// <summary>Gets the finding category.</summary>
    public required FindingKind Kind { get; init; }

    /// <summary>Gets the verdict impact.</summary>
    public required Severity Severity { get; init; }

    /// <summary>Gets the node path the finding applies to.</summary>
    public string NodePath { get; init; } = string.Empty;

    /// <summary>Gets the attribute or style property name, when applicable.</summary>
    public string Property { get; init; } = string.Empty;

    /// <summary>Gets the React value.</summary>
    public string? ReferenceValue { get; init; }

    /// <summary>Gets the Blazor value.</summary>
    public string? CandidateValue { get; init; }

    /// <summary>Gets a human-readable summary.</summary>
    public required string Message { get; init; }
}
