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
    /// The fractions of its duration an animation step is seeked to. The endpoints are
    /// included because a difference in the start or rest pose is as real as one in the
    /// middle, and cheaper to read.
    /// </summary>
    public static readonly IReadOnlyList<double> Fractions = [0, 0.25, 0.5, 0.75, 1];

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

    private const string RootSelector = "[data-parity-root]";

    /// <summary>
    /// The portal containers, spelled to match <c>roots()</c> in
    /// <c>shared/capture.js</c>: every direct child of <c>body</c> that is neither the
    /// fixture root, nor framework chrome a fixture marked as ignorable, nor a script tag.
    /// The one divergence is a page carrying two <c>[data-parity-root]</c> elements, which
    /// <c>roots()</c> would treat the second of as a portal and this selector excludes;
    /// both fixture hosts render exactly one.
    /// </summary>
    private const string PortalSelector =
        "body > *:not([data-parity-root]):not([data-parity-ignore]):not(script)";

    // Long enough for a popup that has just portalled out to become photographable, short
    // enough that an element which never will does not dominate the run.
    private const float ScreenshotTimeoutMs = 5_000;

    private const string Api = "window[Symbol.for('Blazix.Parity.Capture')]";

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
    /// <param name="leg">The leg the screenshot was taken on.</param>
    /// <param name="step">The manifest step name.</param>
    /// <param name="shot">The shot id, for example <c>00</c> or <c>frame025.01</c>.</param>
    /// <returns>The file name.</returns>
    public static string Name(string fixtureId, ParityLeg leg, string step, string shot)
        => $"{Slug(fixtureId)}.{leg}.{step}.{shot}{Extension}";

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
    /// <param name="leg">The leg the name should belong to.</param>
    /// <param name="step">The step the name should belong to.</param>
    /// <returns>The shot id.</returns>
    public static string Shot(string name, string fixtureId, ParityLeg leg, string step)
    {
        var prefix = $"{Slug(fixtureId)}.{leg}.{step}.";

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
    /// Photographs one settled step, and its animation frames when it has any.
    /// </summary>
    /// <param name="page">The page the step was captured on.</param>
    /// <param name="directory">Where the files are written.</param>
    /// <param name="fixtureId">The fixture id.</param>
    /// <param name="leg">The leg being captured.</param>
    /// <param name="step">The manifest step name.</param>
    /// <param name="animation">Whether the step's settle mode is <c>animation</c>.</param>
    /// <returns>The file names written, in shot order.</returns>
    public static async Task<IReadOnlyList<string>> CaptureAsync(
        IPage page,
        string directory,
        string fixtureId,
        ParityLeg leg,
        string step,
        bool animation)
    {
        // The directory is not created here: Playwright's screenshot path option creates
        // the parent directory it is given, so a run on a fresh checkout needs nothing to
        // exist beforehand.
        var names = await ShootAsync(page, directory, fixtureId, leg, step, string.Empty);

        if (!animation)
        {
            return names;
        }

        // Seeking is what makes the frames comparable, so a leg with nothing to seek has
        // no frames rather than five copies of the settled shot. The asymmetry is the
        // point: if one leg animates and the other does not, the frames exist on one side
        // only and the pixel comparator reports each of them, which is exactly the finding
        // a run of five identical images would have hidden.
        //
        // Returning here without resuming is safe only because seekAnimations() arms
        // nothing when it finds nothing to seek. If it armed its resume state regardless,
        // this return would strand it — and the next animation step's seek would believe
        // the recording had already been torn down and record its own seeking into that
        // step's timeline.
        if (await SeekAsync(page, Fractions[0]) == 0)
        {
            return names;
        }

        try
        {
            names.AddRange(
                await ShootAsync(page, directory, fixtureId, leg, step, FramePrefix(Fractions[0])));

            foreach (var fraction in Fractions.Skip(1))
            {
                await SeekAsync(page, fraction);
                names.AddRange(
                    await ShootAsync(page, directory, fixtureId, leg, step, FramePrefix(fraction)));
            }
        }
        finally
        {
            // In a finally because the page outlives the step: every remaining step of the
            // fixture runs on it, and one left with its animations paused would capture a
            // frozen popup and record no transition at all.
            try
            {
                await page.EvaluateAsync($"() => {Api}.resumeAnimations()");
            }
            // Guarded because a finally that throws replaces whatever was already in
            // flight. The case is precisely the one that matters: a screenshot fails with
            // something this method does not filter, and the reason it failed — a closed
            // page, a crashed browser — is also why the resume cannot run. Unguarded, the
            // resume's exception is what the runner would report, and the original cause
            // would be gone. There is nothing to recover here anyway: a page that cannot
            // be evaluated has no animations left to put back.
            catch (Exception ex) when (ex is PlaywrightException or TimeoutException)
            {
            }
        }

        return names;
    }

    private static Task<int> SeekAsync(IPage page, double fraction)
        => page.EvaluateAsync<int>($"f => {Api}.seekAnimations(f)", fraction);

    /// <summary>
    /// The percentage, padded to three digits so <c>frame025</c> sorts before
    /// <c>frame100</c> under the ordinal comparison the report lists shots by.
    /// </summary>
    private static string FramePrefix(double fraction)
        => $"frame{Math.Round(fraction * 100).ToString("000", CultureInfo.InvariantCulture)}";

    private static async Task<List<string>> ShootAsync(
        IPage page,
        string directory,
        string fixtureId,
        ParityLeg leg,
        string step,
        string framePrefix)
    {
        var targets = new List<ILocator> { page.Locator(RootSelector) };
        var portals = page.Locator(PortalSelector);
        var portalCount = await portals.CountAsync();

        for (var i = 0; i < portalCount; i++)
        {
            targets.Add(portals.Nth(i));
        }

        var names = new List<string>(targets.Count);

        for (var index = 0; index < targets.Count; index++)
        {
            // Padded for the same reason the percentage is: a fixture with ten capture
            // roots would otherwise list shot 10 between shots 1 and 2.
            var root = index.ToString("00", CultureInfo.InvariantCulture);
            var shot = framePrefix.Length == 0 ? root : $"{framePrefix}.{root}";
            var name = Name(fixtureId, leg, step, shot);

            try
            {
                await targets[index].ScreenshotAsync(new LocatorScreenshotOptions
                {
                    Path = Path.Combine(directory, name),
                    Timeout = ScreenshotTimeoutMs
                });
            }
            // Both, because they are unrelated types: a zero-size or invisible container
            // exhausts the actionability wait and surfaces as System.TimeoutException,
            // which does not derive from PlaywrightException, while a detached or
            // non-element target surfaces as PlaywrightException, which is not a timeout.
            catch (Exception ex) when (ex is PlaywrightException or TimeoutException)
            {
                // A container that is empty, zero-size, or still detaching cannot be
                // photographed. That is a parity result, not a harness failure: the shot
                // is simply absent from this leg's list, which the pixel comparator reports
                // as one-sided when the other leg produced it and passes over in silence
                // when neither did. Throwing here would end the run over one popup.
                continue;
            }

            names.Add(name);
        }

        return names;
    }
}
