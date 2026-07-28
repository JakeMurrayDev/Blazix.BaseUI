using System.Globalization;
using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Infrastructure;
using SkiaSharp;

namespace Blazix.BaseUI.Parity.Tests.Diff;

/// <summary>
/// Reports the screenshots of a step whose pixels differ by more than the fixture allows.
/// </summary>
/// <remarks>
/// <para>
/// The two legs' shots are paired by shot id — <c>00</c> for the fixture root, <c>0i</c>
/// for <c>portal(i)</c>, <c>frame025.01</c> for one seeked animation frame — and never by
/// position, so a popup one leg portals out and the other does not shifts nothing: the
/// unpaired shot is reported rather than silently compared against the wrong image.
/// </para>
/// <para>
/// Every other outcome that is not a ratio is reported too, because each of them would
/// otherwise read as agreement: two images of different sizes, a listed screenshot that is
/// not on disk, and one the decoder cannot read. Only a computed ratio is measured against
/// <see cref="ComparisonContext.PixelThreshold"/>; a threshold of 1 does not wave through
/// a screenshot that was never taken.
/// </para>
/// </remarks>
/// <param name="directory">Where the screenshots live and the diff overlays are written.</param>
public sealed class PixelComparator(string directory) : IComparator
{
    /// <summary>
    /// How far one channel may move before the pixel counts as different.
    /// </summary>
    /// <remarks>
    /// Two engines rasterizing the same glyph with the same font disagree by a few counts
    /// along every edge, and every fixture has a label in it. Applied per channel rather
    /// than to their sum, so this absorbs antialiasing without also absorbing three
    /// channels moving together — which is a colour change and not a rasterizer artifact.
    /// </remarks>
    private const int ChannelTolerance = 8;

    /// <summary>Thirty percent of 255: the reference is a backdrop, not the subject.</summary>
    private const byte ReferenceAlpha = 77;

    private static readonly SKColor MismatchColour = new(255, 0, 0, 255);

    /// <summary>
    /// Reads and writes under the harness's own screenshot directory.
    /// </summary>
    public PixelComparator()
        : this(ParityPaths.Screenshots)
    {
    }

    /// <inheritdoc />
    public FindingKind Kind => FindingKind.Pixel;

    /// <inheritdoc />
    public IEnumerable<Finding> Compare(ComparisonContext context)
    {
        var reference = Index(context.Reference.Screenshots, context.Fixture, ParityLeg.React, context.Step);
        var candidate = Index(context.Candidate.Screenshots, context.Fixture, context.Leg, context.Step);

        // This is the order the findings are reported in, so it is the order a reader meets
        // the step's shots in. Ordinal is the right comparison only because the ids are
        // zero-padded to a fixed width by ScreenshotSet: the roots ascend and the frames
        // run from the start of the animation to its end, which is what a reader expects a
        // list of animation frames to do.
        var shots = reference.Keys
            .Concat(candidate.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(shot => shot, StringComparer.Ordinal);

        foreach (var shot in shots)
        {
            var hasReference = reference.TryGetValue(shot, out var referenceName);
            var hasCandidate = candidate.TryGetValue(shot, out var candidateName);

            var finding = hasReference && hasCandidate
                ? CompareOne(context, shot, referenceName!, candidateName!)
                : Report(
                    context,
                    shot,
                    referenceName,
                    candidateName,
                    $"Screenshot '{shot}' was taken on the {(hasReference ? "React" : "Blazor")} leg only.");

            if (finding is not null)
            {
                yield return finding;
            }
        }
    }

    private static Dictionary<string, string> Index(
        IReadOnlyList<string> names, string fixture, ParityLeg leg, string step)
    {
        var index = new Dictionary<string, string>(names.Count, StringComparer.Ordinal);

        foreach (var name in names)
        {
            // Assigned rather than added: a duplicate shot id is a capturer bug, and
            // throwing here would end the run over it instead of comparing what exists.
            index[ScreenshotSet.Shot(name, fixture, leg, step)] = name;
        }

        return index;
    }

    /// <summary>
    /// Decodes into unpremultiplied RGBA whatever the file's own encoding is, so that the
    /// per-channel comparison is against the written values and not against a
    /// premultiply-then-divide round trip of them.
    /// </summary>
    private static SKBitmap? Decode(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        using var codec = SKCodec.Create(path);

        if (codec is null)
        {
            return null;
        }

        var info = new SKImageInfo(
            codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        var bitmap = new SKBitmap(info);

        if (codec.GetPixels(info, bitmap.GetPixels()) == SKCodecResult.Success)
        {
            return bitmap;
        }

        bitmap.Dispose();
        return null;
    }

    private static Finding Report(
        ComparisonContext context,
        string shot,
        string? referenceName,
        string? candidateName,
        string message) => new()
        {
            Fixture = context.Fixture,
            Leg = context.Leg,
            Step = context.Step,
            Kind = FindingKind.Pixel,
            Severity = Severity.Error,
            Property = shot,
            ReferenceValue = referenceName,
            CandidateValue = candidateName,
            Message = message
        };

    /// <summary>
    /// Renders a fraction as a percentage with an invariant decimal point, so a message
    /// written on one machine reads the same on another whose separator differs.
    /// </summary>
    private static string Percent(double fraction)
        => (fraction * 100).ToString("0.####", CultureInfo.InvariantCulture) + "%";

    private static bool Differs(SKColor reference, SKColor candidate)
        => Math.Abs(reference.Red - candidate.Red) > ChannelTolerance
            || Math.Abs(reference.Green - candidate.Green) > ChannelTolerance
            || Math.Abs(reference.Blue - candidate.Blue) > ChannelTolerance
            || Math.Abs(reference.Alpha - candidate.Alpha) > ChannelTolerance;

    private static void WriteDiff(SKBitmap reference, bool[] mismatched, string path)
    {
        var source = reference.Pixels;
        var pixels = new SKColor[source.Length];

        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = mismatched[i]
                ? MismatchColour
                : new SKColor(source[i].Red, source[i].Green, source[i].Blue, ReferenceAlpha);
        }

        var info = new SKImageInfo(
            reference.Width, reference.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var diff = new SKBitmap(info);
        diff.Pixels = pixels;

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        using var image = SKImage.FromBitmap(diff);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(path);
        data.SaveTo(stream);
    }

    private Finding? CompareOne(
        ComparisonContext context, string shot, string referenceName, string candidateName)
    {
        // Cleared before anything is decided, so the overlay on disk is always this
        // comparison's and never a previous run's. A diff is written only for a shot that
        // failed, so one left behind by a run that has since been fixed sits beside a
        // passing screenshot claiming a difference that no longer exists — and Task 13's
        // report links diffs by name, so it would be published as this run's evidence.
        var diffName = ScreenshotSet.DiffName(candidateName);
        var diffPath = Path.Combine(directory, diffName);

        if (File.Exists(diffPath))
        {
            File.Delete(diffPath);
        }

        using var reference = Decode(Path.Combine(directory, referenceName));
        using var candidate = Decode(Path.Combine(directory, candidateName));

        if (reference is null || candidate is null)
        {
            var missing = reference is null ? referenceName : candidateName;

            return Report(
                context,
                shot,
                referenceName,
                candidateName,
                $"Screenshot '{shot}' could not be read: '{missing}' is missing or is not a " +
                "readable PNG, so the two legs were not compared at all.");
        }

        if (reference.Width != candidate.Width || reference.Height != candidate.Height)
        {
            return Report(
                context,
                shot,
                referenceName,
                candidateName,
                $"Screenshot '{shot}' differs in size: React {reference.Width}x{reference.Height}, " +
                $"Blazor {candidate.Width}x{candidate.Height}. No fraction was computed, so the " +
                "fixture's threshold does not apply.");
        }

        var referencePixels = reference.Pixels;
        var candidatePixels = candidate.Pixels;
        var mismatched = new bool[referencePixels.Length];
        var count = 0;

        for (var i = 0; i < referencePixels.Length; i++)
        {
            if (!Differs(referencePixels[i], candidatePixels[i]))
            {
                continue;
            }

            mismatched[i] = true;
            count++;
        }

        var fraction = (double)count / referencePixels.Length;

        if (fraction <= context.PixelThreshold)
        {
            return null;
        }

        WriteDiff(reference, mismatched, diffPath);

        return Report(
            context,
            shot,
            referenceName,
            candidateName,
            $"Screenshot '{shot}' differs: {Percent(fraction)} of pixels are outside the " +
            $"per-channel tolerance, above the fixture's {Percent(context.PixelThreshold)} " +
            $"threshold. The differing pixels are marked in '{diffName}'.");
    }
}
