using System.Globalization;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Parity.Tests.Capture;

/// <summary>
/// Names and takes the screenshots one captured step produces.
/// </summary>
/// <remarks>
/// <para>
/// A step is photographed once per capture root: shot <c>00</c> is the fixture root and
/// shot <c>0i</c> is <c>portal(i)</c>, matching the labels <c>shared/capture.js</c> gives
/// the same elements, so a pixel finding on shot <c>02</c> and a geometry finding under
/// <c>portal(2)</c> name the same tree. Photographing the viewport instead would compare
/// the two legs' page shells — which differ by construction, one being a Blazor host and
/// the other a Vite bundle — and report that difference on every fixture.
/// </para>
/// <para>
/// A step whose settle mode is <c>animation</c> is photographed a second time at five
/// points of its animation, each with the page's animations paused and seeked, so the
/// frames are a function of the animation rather than of how fast the machine ran.
/// </para>
/// <para>
/// Every number in a shot id is zero-padded to a fixed width, because the report lists
/// shots in the order an ordinal sort puts their ids in and nothing re-sorts them
/// numerically. Unpadded, <c>frame100</c> falls between <c>frame0</c> and <c>frame25</c>
/// and the animation is presented ending before it has begun; a fixture with ten portals
/// shows shot <c>10</c> ahead of shot <c>2</c>.
/// </para>
/// </remarks>
public static class ScreenshotSet
{
    /// <summary>
    /// What a fixture id's slash becomes in a file name.
    /// </summary>
    /// <remarks>
    /// A hyphen would not do. Ids are lowercase kebab <c>component/demo</c>, so a hyphen
    /// already occurs on both sides of the slash and <c>number-field/scrub-area</c> and
    /// <c>number/field-scrub-area</c> would produce one name between them — the second
    /// fixture's screenshots would overwrite the first's with no error anywhere. No id
    /// contains an underscore, so this pairing is reversible.
    /// </remarks>
    private const string FixtureSeparator = "__";

    private const string Extension = ".png";

    // Long enough for a popup that has just portalled out to become photographable, short
    // enough that an element which never will does not dominate the run.
    private const float ScreenshotTimeoutMs = 5_000;

    private const string Api = "window[Symbol.for('Blazix.Parity.Capture')]";

    /// <summary>
    /// The fractions of its duration an animation step is seeked to. The endpoints are
    /// included because a difference in the start or rest pose is as real as one in the
    /// middle, and cheaper to read.
    /// </summary>
    public static readonly IReadOnlyList<double> Fractions = [0, 0.25, 0.5, 0.75, 1];

    /// <summary>
    /// Renders a fixture id as one file-name segment.
    /// </summary>
    /// <param name="fixtureId">The fixture id, for example <c>select/grouped</c>.</param>
    /// <returns>The segment, for example <c>select__grouped</c>.</returns>
    public static string Slug(string fixtureId)
        => fixtureId.Replace("/", FixtureSeparator, StringComparison.Ordinal);

    /// <summary>
    /// Reads a fixture id back out of the segment <see cref="Slug"/> produced.
    /// </summary>
    /// <param name="slug">The file-name segment.</param>
    /// <returns>The fixture id.</returns>
    public static string FixtureId(string slug)
        => slug.Replace(FixtureSeparator, "/", StringComparison.Ordinal);

    /// <summary>
    /// Names one screenshot.
    /// </summary>
    /// <param name="fixtureId">The fixture id.</param>
    /// <param name="theme">The emulated theme.</param>
    /// <param name="leg">The leg the screenshot was taken on.</param>
    /// <param name="step">The manifest step name.</param>
    /// <param name="shot">The shot id, for example <c>00</c> or <c>frame025.01</c>.</param>
    /// <returns>The file name.</returns>
    public static string Name(
        string fixtureId,
        string theme,
        ParityLeg leg,
        string step,
        string shot)
        => $"{Slug(fixtureId)}.{theme}.{leg}.{step}.{shot}{Extension}";

    /// <summary>
    /// Recovers the shot id from a file name.
    /// </summary>
    /// <remarks>
    /// The fixture, leg, and step are supplied rather than parsed, so a shot id carrying a
    /// dot of its own — every animation frame does — needs no delimiter of its own. A name
    /// that does not match is returned whole: it will then pair with nothing and be
    /// reported as one-sided, which is louder than dropping it.
    /// </remarks>
    /// <param name="name">The file name.</param>
    /// <param name="fixtureId">The fixture id the name should belong to.</param>
    /// <param name="theme">The emulated theme the name should belong to.</param>
    /// <param name="leg">The leg the name should belong to.</param>
    /// <param name="step">The step the name should belong to.</param>
    /// <returns>The shot id.</returns>
    public static string Shot(
        string name,
        string fixtureId,
        string theme,
        ParityLeg leg,
        string step)
    {
        var prefix = $"{Slug(fixtureId)}.{theme}.{leg}.{step}.";

        return name.StartsWith(prefix, StringComparison.Ordinal)
            && name.EndsWith(Extension, StringComparison.Ordinal)
            && name.Length > prefix.Length + Extension.Length
                ? name[prefix.Length..^Extension.Length]
                : name;
    }

    /// <summary>
    /// Names the diff overlay for a screenshot.
    /// </summary>
    /// <param name="name">The candidate leg's screenshot file name.</param>
    /// <returns>The diff file name.</returns>
    public static string DiffName(string name)
        => name.EndsWith(Extension, StringComparison.Ordinal)
            ? $"{name[..^Extension.Length]}.diff{Extension}"
            : $"{name}.diff{Extension}";

    /// <summary>
    /// Photographs one naturally settled canonical step.
    /// </summary>
    /// <param name="page">The page the step was captured on.</param>
    /// <param name="directory">Where the files are written.</param>
    /// <param name="fixtureId">The fixture id.</param>
    /// <param name="theme">The emulated theme.</param>
    /// <param name="leg">The leg being captured.</param>
    /// <param name="step">The manifest step name.</param>
    /// <returns>The file names written, in shot order.</returns>
    public static async Task<IReadOnlyList<ScreenshotObservation>> CaptureCanonicalAsync(
        IPage page,
        string directory,
        string fixtureId,
        string theme,
        ParityLeg leg,
        string step)
    {
        // The directory is not created here: Playwright's screenshot path option creates
        // the parent directory it is given, so a run on a fresh checkout needs nothing to
        // exist beforehand.
        return await ShootAsync(
            page, directory, fixtureId, theme, leg, step, string.Empty);
    }

    /// <summary>
    /// Photographs deterministic fractions on a disposable replay page.
    /// </summary>
    /// <remarks>
    /// Seeking can resolve a component's <c>animation.finished</c> callbacks and therefore
    /// advance lifecycle state. The caller must discard <paramref name="page"/> after this
    /// method returns; it must never be the authoritative page or be reused for another step.
    /// </remarks>
    /// <param name="page">The isolated replay page.</param>
    /// <param name="directory">Where the files are written.</param>
    /// <param name="fixtureId">The fixture id.</param>
    /// <param name="theme">The emulated theme.</param>
    /// <param name="leg">The leg being captured.</param>
    /// <param name="step">The manifest step name.</param>
    /// <returns>The frame file names written, in fraction order.</returns>
    public static async Task<IReadOnlyList<ScreenshotObservation>> CaptureFramesAsync(
        IPage page,
        string directory,
        string fixtureId,
        string theme,
        ParityLeg leg,
        string step)
    {
        var observations = new List<ScreenshotObservation>();

        // Seeking is what makes the frames comparable, so a leg with nothing to seek has
        // no frames rather than five copies of the canonical shot. The asymmetry is the
        // point: if one leg animates and the other does not, the frames exist on one side
        // only and the pixel comparator reports each of them, which is exactly the finding
        // a run of five identical images would have hidden.
        //
        // The replay page is discarded after this call. Finding nothing therefore needs no
        // cleanup and correctly produces no frame files.
        if (await SeekAsync(page, Fractions[0]) == 0)
        {
            return observations;
        }

        observations.AddRange(
            await ShootAsync(
                page, directory, fixtureId, theme, leg, step, FramePrefix(Fractions[0])));

        foreach (var fraction in Fractions.Skip(1))
        {
            await SeekAsync(page, fraction);
            observations.AddRange(
                await ShootAsync(
                    page, directory, fixtureId, theme, leg, step, FramePrefix(fraction)));
        }

        return observations;
    }

    private static Task<int> SeekAsync(IPage page, double fraction)
        => page.EvaluateAsync<int>($"f => {Api}.seekAnimations(f)", fraction);

    /// <summary>
    /// The percentage, padded to three digits so <c>frame025</c> sorts before
    /// <c>frame100</c> under the ordinal comparison the report lists shots by.
    /// </summary>
    private static string FramePrefix(double fraction)
        => $"frame{Math.Round(fraction * 100).ToString("000", CultureInfo.InvariantCulture)}";

    private static async Task<List<ScreenshotObservation>> ShootAsync(
        IPage page,
        string directory,
        string fixtureId,
        string theme,
        ParityLeg leg,
        string step,
        string framePrefix)
    {
        var roots = await page.EvaluateAsync<ScreenshotRoot[]>(
            $"() => {Api}.screenshotRoots()");
        var observations = new List<ScreenshotObservation>(roots.Length);

        for (var index = 0; index < roots.Length; index++)
        {
            // Padded for the same reason the percentage is: a fixture with ten capture
            // roots would otherwise list shot 10 between shots 1 and 2.
            var root = index.ToString("00", CultureInfo.InvariantCulture);
            var shot = framePrefix.Length == 0 ? root : $"{framePrefix}.{root}";
            var name = Name(fixtureId, theme, leg, step, shot);

            if (roots[index].State == nameof(ScreenshotObservationState.NotVisible) ||
                roots[index].Clip is null)
            {
                observations.Add(ScreenshotObservation.NotVisible(roots[index].Label, shot));
                continue;
            }

            try
            {
                var clip = roots[index].Clip!;
                await page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = Path.Combine(directory, name),
                    Timeout = ScreenshotTimeoutMs,
                    Clip = new Clip
                    {
                        X = (float)clip.X,
                        Y = (float)clip.Y,
                        Width = (float)clip.Width,
                        Height = (float)clip.Height
                    }
                });
            }
            // Both, because they are unrelated types: a zero-size or invisible container
            // exhausts the actionability wait and surfaces as System.TimeoutException,
            // which does not derive from PlaywrightException, while a detached or
            // non-element target surfaces as PlaywrightException, which is not a timeout.
            catch (Exception ex) when (ex is PlaywrightException or TimeoutException)
            {
                observations.Add(ScreenshotObservation.CaptureFailed(
                    roots[index].Label,
                    shot,
                    ex.Message.Length <= 500 ? ex.Message : ex.Message[..500]));
                continue;
            }

            observations.Add(ScreenshotObservation.Captured(roots[index].Label, shot, name));
        }

        return observations;
    }

    private sealed record ScreenshotRoot
    {
        public required string Label { get; init; }

        public required string State { get; init; }

        public ScreenshotClip? Clip { get; init; }
    }

    private sealed record ScreenshotClip
    {
        public double X { get; init; }

        public double Y { get; init; }

        public double Width { get; init; }

        public double Height { get; init; }
    }
}
