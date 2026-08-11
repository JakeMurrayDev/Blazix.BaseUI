using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Infrastructure;

namespace Blazix.BaseUI.Parity.Tests.Diff;

/// <summary>
/// Classifies the marker attributes Blazix renders that base-ui has no counterpart for.
/// </summary>
/// <remarks>
/// <para>
/// <c>capture.js</c> renames every <c>data-blazix-base-ui-*</c> attribute to its upstream
/// <c>data-base-ui-*</c> spelling before the snapshot is written, so most Blazix markers
/// reach this comparator already wearing an upstream name and the Blazix prefix is not what
/// identifies them. <c>manifest/markers.json</c> is therefore keyed on the spelling a
/// capture holds, not the one the Razor source writes.
/// </para>
/// <para>
/// Two families are classified here. A name listed in the manifest is reported
/// <see cref="Severity.Info"/> carrying its written reason — listing it is the assertion
/// that base-ui has no counterpart for it, which is what makes a Blazix-invented name safe
/// to key on its normalized spelling. A name still carrying the <c>data-blazix-</c> prefix
/// and not listed is unclassified: normalization left it alone, so no upstream name is
/// even in play, and it fails the run rather than being quietly tolerated.
/// </para>
/// <para>
/// An unlisted name wearing an upstream spelling is not claimed here at all. Nothing here
/// distinguishes it from an attribute Blazix renders and React does not, and that is a
/// parity defect rather than a marker, so it falls through to
/// <see cref="AttributeComparator"/> and is reported one-sided.
/// </para>
/// <para>
/// That bounds the advice the unclassified message gives, and the bound is worth stating.
/// <c>capture.js</c> renames the <c>data-blazix-base-ui-</c> family only, so a marker newly
/// invented in that family arrives here unlisted and upstream-spelled, is not claimed, and
/// fails the run as a plain attribute error — never as the unclassified marker whose
/// message asks for a manifest entry. What keying on the normalized spelling bought is that
/// adding that name to <c>manifest/markers.json</c> now classifies it; the message itself is
/// only ever printed for a name normalization left alone, which today is the
/// <c>data-blazix-otp-</c> family.
/// </para>
/// </remarks>
public sealed class MarkerComparator : IComparator
{
    private readonly IReadOnlyDictionary<string, string> blazorOnly = MarkerCatalog.Load();

    /// <inheritdoc />
    public FindingKind Kind => FindingKind.Marker;

    /// <inheritdoc />
    public IEnumerable<Finding> Compare(ComparisonContext context)
    {
        // Descendants() is self-inclusive, so a multi-root capture starts at the synthetic
        // '#roots' wrapper. Its path is the empty string, so a finding from there would
        // name an element in neither page.
        var nodes = context.Candidate.Dom.Descendants()
            .Where(node => node.Tag != CaptureNames.RootsWrapper);

        foreach (var node in nodes)
        {
            var markers = node.Attributes.Keys
                .Where(name => blazorOnly.ContainsKey(name)
                    || name.StartsWith(CaptureNames.MarkerPrefix, StringComparison.Ordinal))
                .OrderBy(name => name, StringComparer.Ordinal);

            foreach (var name in markers)
            {
                var listed = blazorOnly.TryGetValue(name, out var reason);

                yield return new Finding
                {
                    Fixture = context.ExecutionId,
                    Leg = context.Leg,
                    Step = context.Step,
                    Kind = FindingKind.Marker,
                    Severity = listed ? Severity.Info : Severity.Error,
                    NodePath = node.Path,
                    Property = name,
                    CandidateValue = node.Attributes[name],
                    Message = listed
                        ? $"Blazor-only marker '{name}' at '{node.Path}'. {reason}"
                        : $"Unclassified Blazix marker '{name}'. Add it to " +
                          "manifest/markers.json with a reason, or rename it to its " +
                          "data-base-ui-* counterpart."
                };
            }
        }
    }
}
