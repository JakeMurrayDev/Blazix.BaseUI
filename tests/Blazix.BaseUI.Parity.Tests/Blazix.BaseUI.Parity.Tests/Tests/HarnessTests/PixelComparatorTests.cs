using System.Globalization;
using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Diff;
using Blazix.BaseUI.Parity.Tests.Infrastructure;
using Shouldly;
using SkiaSharp;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

/// <summary>
/// Pins the screenshot file-name scheme and the pixel comparison that reads it.
/// </summary>
/// <remarks>
/// The images are generated here rather than committed: a checked-in PNG is a binary blob
/// no reviewer can diff, and the interesting cases are one-pixel and one-channel
/// differences that only a generator can state exactly.
/// </remarks>
public sealed class PixelComparatorTests : IDisposable
{
    /// <summary>
    /// A fixture id with a slash in it, because every real one has: the id is
    /// <c>component/demo</c>, so the naming scheme is exercised on the shape that breaks
    /// a naive <c>{fixture}.png</c> and not on a shape no manifest can produce.
    /// </summary>
    private const string Fixture = "select/grouped";

    private const string Step = "initial";

    private static readonly SKColor White = new(255, 255, 255, 255);

    private readonly string directory = Path.Combine(
        Path.GetTempPath(), "blazix-parity-pixel", Guid.NewGuid().ToString("N"));

    /// <summary>Creates the run's screenshot directory.</summary>
    public PixelComparatorTests() => Directory.CreateDirectory(directory);

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ReportsTheFractionOfPixelsThatDiffer()
    {
        Write(Name(ParityLeg.React, "00"), 4, 4);
        Write(Name(ParityLeg.BlazorServer, "00"), 4, 4, (2, 1, SKColors.Black));

        var finding = Compare(threshold: 0.01).ShouldHaveSingleItem();

        finding.Kind.ShouldBe(FindingKind.Pixel);
        finding.Severity.ShouldBe(Severity.Error);
        finding.Property.ShouldBe("00");
        finding.ReferenceValue.ShouldBe(Name(ParityLeg.React, "00"));
        finding.CandidateValue.ShouldBe(Name(ParityLeg.BlazorServer, "00"));

        // One pixel of sixteen, stated as the exact fraction rather than "some differ":
        // the threshold comparison is only meaningful if the ratio behind it is.
        finding.Message.ShouldContain("6.25%");
    }

    [Fact]
    public void ReportsNothingWhenTheFractionIsWithinTheFixtureThreshold()
    {
        Write(Name(ParityLeg.React, "00"), 4, 4);
        Write(Name(ParityLeg.BlazorServer, "00"), 4, 4, (2, 1, SKColors.Black));

        // 1/16 is 0.0625, under a tenth.
        Compare(threshold: 0.1).ShouldBeEmpty();
    }

    [Fact]
    public void MarksTheDifferingPixelRedOverAFadedReference()
    {
        Write(Name(ParityLeg.React, "00"), 4, 4);
        Write(Name(ParityLeg.BlazorServer, "00"), 4, 4, (2, 1, SKColors.Black));

        Compare(threshold: 0.01).ShouldHaveSingleItem();

        using var diff = Read(ScreenshotSet.DiffName(Name(ParityLeg.BlazorServer, "00")));

        diff.GetPixel(2, 1).ShouldBe(new SKColor(255, 0, 0, 255));

        // The rest is the reference at 30% opacity, which is what makes the red readable
        // as a location rather than as a colour. Asserted alongside the red pixel: a diff
        // that painted every pixel red would satisfy the assertion above on its own.
        var untouched = diff.GetPixel(0, 0);
        untouched.Alpha.ShouldBe((byte)77);
        untouched.Red.ShouldBe((byte)255);
        untouched.Green.ShouldBe((byte)255);
        untouched.Blue.ShouldBe((byte)255);
    }

    [Fact]
    public void WritesNoDiffWhenTheFractionIsWithinTheThreshold()
    {
        Write(Name(ParityLeg.React, "00"), 4, 4);
        Write(Name(ParityLeg.BlazorServer, "00"), 4, 4, (2, 1, SKColors.Black));

        Compare(threshold: 0.1).ShouldBeEmpty();

        // A diff overlay exists to explain a finding. Writing one for every screenshot of
        // every step of every fixture would bury the handful that mean something.
        File.Exists(Path.Combine(directory, ScreenshotSet.DiffName(Name(ParityLeg.BlazorServer, "00"))))
            .ShouldBeFalse();
    }

    [Fact]
    public void ReportsNothingWhenEveryPixelAgrees()
    {
        Write(Name(ParityLeg.React, "00"), 4, 4, (1, 1, SKColors.Black));
        Write(Name(ParityLeg.BlazorServer, "00"), 4, 4, (1, 1, SKColors.Black));

        Compare(threshold: 0).ShouldBeEmpty();
    }

    [Fact]
    public void AbsorbsAChannelDifferenceOfEight()
    {
        Write(Name(ParityLeg.React, "00"), 4, 4);
        Write(Name(ParityLeg.BlazorServer, "00"), 4, 4, (0, 0, new SKColor(247, 255, 255, 255)));

        // Text antialiasing moves a channel by a few counts between two engines drawing
        // the same glyph; a comparator without this slack reports every label as a failure.
        Compare(threshold: 0).ShouldBeEmpty();
    }

    [Fact]
    public void ReportsAChannelDifferenceOfNine()
    {
        Write(Name(ParityLeg.React, "00"), 4, 4);
        Write(Name(ParityLeg.BlazorServer, "00"), 4, 4, (0, 0, new SKColor(246, 255, 255, 255)));

        // Asserted next to the row above so the tolerance is pinned to a value rather than
        // to "generous": a tolerance that swallowed everything would pass that test alone.
        Compare(threshold: 0).ShouldHaveSingleItem().Message.ShouldContain("6.25%");
    }

    [Fact]
    public void ReportsEachChannelSeparatelyRatherThanTheirSum()
    {
        Write(Name(ParityLeg.React, "00"), 4, 4);
        Write(Name(ParityLeg.BlazorServer, "00"), 4, 4, (0, 0, new SKColor(249, 249, 249, 255)));

        // Three channels six counts apart sum to eighteen. A comparator that added them up
        // would report this, and would then also miss a single channel eighteen counts out.
        Compare(threshold: 0).ShouldBeEmpty();
    }

    [Fact]
    public void ReportsAnAlphaDifferenceLikeAnyOtherChannel()
    {
        Write(Name(ParityLeg.React, "00"), 4, 4);
        Write(Name(ParityLeg.BlazorServer, "00"), 4, 4, (0, 0, new SKColor(255, 255, 255, 200)));

        // The same colour at a different coverage. A popup caught mid-fade differs from a
        // settled one in nothing but alpha, so a comparison that read only the three colour
        // channels would call the two identical — on exactly the frames that exist to tell
        // them apart.
        Compare(threshold: 0).ShouldHaveSingleItem().Message.ShouldContain("6.25%");
    }

    [Fact]
    public void ReportsNothingAtExactlyTheFixtureThreshold()
    {
        Write(Name(ParityLeg.React, "00"), 4, 4);
        Write(Name(ParityLeg.BlazorServer, "00"), 4, 4, (2, 1, SKColors.Black));

        // The threshold is the fraction a fixture allows, so a fraction that reaches it is
        // allowed and only one that exceeds it is a finding. A fixture calibrated to the
        // exact fraction its own antialiasing produces would otherwise still fail.
        Compare(threshold: 1.0 / 16.0).ShouldBeEmpty();
    }

    [Fact]
    public void ReportsDifferingDimensionsWithoutAFraction()
    {
        Write(Name(ParityLeg.React, "00"), 4, 4);
        Write(Name(ParityLeg.BlazorServer, "00"), 4, 5);

        var finding = Compare(threshold: 0.9).ShouldHaveSingleItem();

        finding.Kind.ShouldBe(FindingKind.Pixel);
        finding.Message.ShouldContain("4x4");
        finding.Message.ShouldContain("4x5");

        // No ratio exists between images of different sizes, so none is claimed — and the
        // fixture's threshold cannot wave the difference through however generous it is.
        finding.Message.ShouldNotContain("%");
        File.Exists(Path.Combine(directory, ScreenshotSet.DiffName(Name(ParityLeg.BlazorServer, "00"))))
            .ShouldBeFalse();
    }

    [Fact]
    public void ReportsAShotCapturedOnOneLegOnly()
    {
        Write(Name(ParityLeg.React, "00"), 4, 4);
        Write(Name(ParityLeg.React, "01"), 4, 4);
        Write(Name(ParityLeg.BlazorServer, "00"), 4, 4);

        var context = Context(
            threshold: 0.5,
            reference: [Name(ParityLeg.React, "00"), Name(ParityLeg.React, "01")],
            candidate: [Name(ParityLeg.BlazorServer, "00")]);

        // Shot 01 is portal(1): React portalled a popup out and Blazor did not. Dropping
        // the unpaired shot would turn a whole missing popup into silence.
        var finding = new PixelComparator(directory).Compare(context).ShouldHaveSingleItem();

        finding.Property.ShouldBe("01");
        finding.ReferenceValue.ShouldBe(Name(ParityLeg.React, "01"));
        finding.CandidateValue.ShouldBeNull();

        // Which leg has it is the whole content of this finding: a reader told only that
        // the two legs disagree learns nothing about which one is missing the popup.
        finding.Message.ShouldContain("React leg only");
    }

    [Fact]
    public void ReportsAScreenshotThatIsNotOnDisk()
    {
        Write(Name(ParityLeg.React, "00"), 4, 4);

        // The candidate is listed but never written. Reading the absence as "no difference"
        // would make a crashed screenshot indistinguishable from a matching one.
        var finding = Compare(threshold: 0.9).ShouldHaveSingleItem();

        finding.Message.ShouldContain(Name(ParityLeg.BlazorServer, "00"));
    }

    [Fact]
    public void ComparesEverySettledShotAndEveryAnimationFrameInShotOrder()
    {
        // Two roots and the five fractions of one of them, listed here in the order the
        // capturer takes them. The findings must come back in this order too, and the
        // order is asserted rather than ignored: it is the order a reader meets the step's
        // shots in, and an animation whose frames are presented 0, 100, 25, 50, 75 reads as
        // ending before it has begun. Unpadded ids sort exactly that way.
        string[] shots =
        [
            "00", "01",
            "frame000.00", "frame025.00", "frame050.00", "frame075.00", "frame100.00"
        ];

        foreach (var shot in shots)
        {
            Write(Name(ParityLeg.React, shot), 4, 4);
            Write(Name(ParityLeg.BlazorServer, shot), 4, 4, (0, 0, SKColors.Black));
        }

        var context = Context(
            threshold: 0.01,
            // Shuffled on the way in, so what is asserted below is the comparator's
            // ordering and not the order the two lists happened to arrive in.
            reference: [.. shots.Reverse().Select(shot => Name(ParityLeg.React, shot))],
            candidate: [.. shots.Select(shot => Name(ParityLeg.BlazorServer, shot))]);

        var findings = new PixelComparator(directory).Compare(context).ToList();

        findings.Select(f => f.Property).ShouldBe(shots);
    }

    [Fact]
    public void SortsTenCaptureRootsNumerically()
    {
        // Ten is not hypothetical for a nested popup corpus, and it is where an ordinal
        // sort of unpadded ids puts shot 10 between 1 and 2.
        var shots = Enumerable.Range(0, 11).Select(i => i.ToString("00", CultureInfo.InvariantCulture)).ToList();

        foreach (var shot in shots)
        {
            Write(Name(ParityLeg.React, shot), 4, 4);
            Write(Name(ParityLeg.BlazorServer, shot), 4, 4, (0, 0, SKColors.Black));
        }

        var context = Context(
            threshold: 0.01,
            reference: [.. shots.Select(shot => Name(ParityLeg.React, shot))],
            candidate: [.. shots.Select(shot => Name(ParityLeg.BlazorServer, shot))]);

        new PixelComparator(directory).Compare(context)
            .Select(f => f.Property)
            .ShouldBe(shots);
    }

    [Fact]
    public void RemovesTheDiffOfAShotThatNoLongerDiffers()
    {
        Write(Name(ParityLeg.React, "00"), 4, 4);
        Write(Name(ParityLeg.BlazorServer, "00"), 4, 4, (2, 1, SKColors.Black));

        Compare(threshold: 0.01).ShouldHaveSingleItem();

        var diff = Path.Combine(directory, ScreenshotSet.DiffName(Name(ParityLeg.BlazorServer, "00")));
        File.Exists(diff).ShouldBeTrue();

        // The screenshot is retaken and now matches, as it would after the difference the
        // first run reported was fixed.
        Write(Name(ParityLeg.BlazorServer, "00"), 4, 4);

        Compare(threshold: 0.01).ShouldBeEmpty();

        // A diff overlay is written only for a shot that failed, so one sitting beside a
        // passing screenshot claims a difference that is no longer there — and the report
        // links diffs by name, so the stale image would be published as this run's evidence.
        File.Exists(diff).ShouldBeFalse();
    }

    [Fact]
    public void RemovesTheDiffOfAShotThatIsNoLongerCapturedOnBothLegs()
    {
        Write(Name(ParityLeg.React, "00"), 4, 4);
        Write(Name(ParityLeg.BlazorServer, "00"), 4, 4, (2, 1, SKColors.Black));

        Compare(threshold: 0.01).ShouldHaveSingleItem();

        var diff = Path.Combine(directory, ScreenshotSet.DiffName(Name(ParityLeg.BlazorServer, "00")));
        File.Exists(diff).ShouldBeTrue();

        // The next run captures the shot on the Blazor leg only, as it would once React
        // stopped portalling the popup out. A one-sided shot is reported without ever being
        // decoded, so nothing on that path rewrites the overlay — and one left on disk
        // claims a pixel difference measured against an image this run does not contain.
        var context = Context(
            threshold: 0.01,
            reference: [],
            candidate: [Name(ParityLeg.BlazorServer, "00")]);

        new PixelComparator(directory).Compare(context)
            .ShouldHaveSingleItem()
            .Message.ShouldContain("Blazor leg only");

        File.Exists(diff).ShouldBeFalse();
    }

    [Fact]
    public void OwnsOnlyThePixelKind()
    {
        new PixelComparator(directory).Kind.ShouldBe(FindingKind.Pixel);
    }

    [Fact]
    public void CarriesTheFixtureLegAndStep()
    {
        Write(Name(ParityLeg.React, "00"), 4, 4);
        Write(Name(ParityLeg.BlazorServer, "00"), 4, 4, (2, 1, SKColors.Black));

        var finding = Compare(threshold: 0.01).ShouldHaveSingleItem();

        finding.Fixture.ShouldBe(Fixture);
        finding.Leg.ShouldBe(ParityLeg.BlazorServer);
        finding.Step.ShouldBe(Step);
    }

    [Theory]
    [InlineData("select/grouped")]
    [InlineData("number-field/scrub-area")]
    [InlineData("switch/hero")]
    public void RoundTripsAFixtureIdThroughItsFileNameSegment(string id)
    {
        var slug = ScreenshotSet.Slug(id);

        // A slug that is still a path would name a file inside a `select/` directory that
        // no one creates, and the screenshot would fail to write at all.
        slug.ShouldBe(Path.GetFileName(slug));
        slug.ShouldNotContain("/");

        ScreenshotSet.FixtureId(slug).ShouldBe(id);
    }

    [Fact]
    public void KeepsFixtureIdsApartThatDifferOnlyInWhereTheSlashFalls()
    {
        // Component names are kebab-case, so a hyphen is already a legal character on both
        // sides of the slash: replacing the slash with a hyphen maps these two onto one
        // name, and one fixture's screenshots would silently overwrite the other's.
        ScreenshotSet.Slug("number-field/scrub-area")
            .ShouldNotBe(ScreenshotSet.Slug("number/field-scrub-area"));
    }

    [Fact]
    public void RecoversTheShotFromAFileName()
    {
        var name = ScreenshotSet.Name(Fixture, ParityLeg.BlazorWasm, "open", "frame025.01");

        name.ShouldBe("select__grouped.BlazorWasm.open.frame025.01.png");

        // Recovered from the leg and step the comparison already knows, so a shot id that
        // contains a dot of its own does not have to be parsed out of the name.
        ScreenshotSet.Shot(name, Fixture, ParityLeg.BlazorWasm, "open").ShouldBe("frame025.01");
    }

    [Fact]
    public void PutsScreenshotsUnderTheReportDirectory()
    {
        // Under the report directory rather than beside the committed baselines: these are
        // run output, and living here gives them the PARITY_REPORT_DIR override and the
        // parity-report/ gitignore entry that already exists, so a fresh checkout needs
        // neither a new directory nor a new ignore rule.
        ParityPaths.Screenshots.ShouldBe(Path.Combine(ParityPaths.ReportDir, "screenshots"));
    }

    [Fact]
    public void NamesTheDiffAfterTheCandidateScreenshot()
    {
        ScreenshotSet.DiffName("select__grouped.BlazorServer.initial.00.png")
            .ShouldBe("select__grouped.BlazorServer.initial.00.diff.png");
    }

    private IEnumerable<Finding> Compare(double threshold)
        => new PixelComparator(directory).Compare(Context(
            threshold,
            [Name(ParityLeg.React, "00")],
            [Name(ParityLeg.BlazorServer, "00")]));

    private static ComparisonContext Context(
        double threshold, IReadOnlyList<string> reference, IReadOnlyList<string> candidate)
        => new(
            Fixture,
            ParityLeg.BlazorServer,
            Step,
            Capture(reference),
            Capture(candidate),
            threshold);

    private static StepCapture Capture(IReadOnlyList<string> screenshots) => new()
    {
        Step = Step,
        Dom = new DomNode
        {
            Tag = "div",
            Path = "root",
            Attributes = new Dictionary<string, string>(StringComparer.Ordinal),
            Classes = [],
            Text = string.Empty,
            Children = []
        },
        Styles = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal),
        CustomProps = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal),
        Geometry = new Dictionary<string, IReadOnlyDictionary<string, double>>(StringComparer.Ordinal),
        Screenshots = screenshots
    };

    private static string Name(ParityLeg leg, string shot)
        => ScreenshotSet.Name(Fixture, leg, Step, shot);

    /// <summary>Writes a white PNG with the listed pixels overpainted.</summary>
    private void Write(string name, int width, int height, params (int X, int Y, SKColor Colour)[] pixels)
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var bitmap = new SKBitmap(info);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                bitmap.SetPixel(x, y, White);
            }
        }

        foreach (var (x, y, colour) in pixels)
        {
            bitmap.SetPixel(x, y, colour);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(Path.Combine(directory, name));
        data.SaveTo(stream);
    }

    /// <summary>
    /// Decodes into unpremultiplied RGBA so that a faded pixel reads back as it was
    /// written, rather than through a premultiply-then-divide round trip.
    /// </summary>
    private SKBitmap Read(string name)
    {
        using var codec = SKCodec.Create(Path.Combine(directory, name));
        var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        var bitmap = new SKBitmap(info);
        codec.GetPixels(info, bitmap.GetPixels());
        return bitmap;
    }
}
