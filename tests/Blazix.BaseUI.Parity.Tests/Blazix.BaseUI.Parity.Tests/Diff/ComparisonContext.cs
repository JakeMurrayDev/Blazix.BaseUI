namespace Blazix.BaseUI.Parity.Tests.Diff;

/// <summary>
/// One step of one fixture, paired across the reference and candidate legs.
/// </summary>
/// <param name="Fixture">The fixture id, for example <c>select/grouped</c>.</param>
/// <param name="Theme">The exact emulated color-scheme theme.</param>
/// <param name="ExecutionId">The fixture-theme identity used by findings and waivers.</param>
/// <param name="Leg">The Blazor leg being compared against React.</param>
/// <param name="Step">The manifest step name.</param>
/// <param name="Reference">The React capture for this step.</param>
/// <param name="Candidate">The Blazor capture for this step.</param>
/// <param name="PixelThreshold">The fixture's screenshot mismatch threshold.</param>
public sealed record ComparisonContext(
    string Fixture,
    string Theme,
    string ExecutionId,
    Capture.ParityLeg Leg,
    string Step,
    Capture.StepCapture Reference,
    Capture.StepCapture Candidate,
    double PixelThreshold);

/// <summary>Compares one dimension of a paired capture step.</summary>
public interface IComparator
{
    /// <summary>Gets the finding category this comparator produces.</summary>
    FindingKind Kind { get; }

    /// <summary>
    /// Compares the reference and candidate captures.
    /// </summary>
    /// <param name="context">The paired step.</param>
    /// <returns>Zero or more findings.</returns>
    IEnumerable<Finding> Compare(ComparisonContext context);
}
