using System.Text.Json;
using Blazix.BaseUI.Parity.Tests.Fixtures;
using Blazix.BaseUI.Parity.Tests.Infrastructure;
using Microsoft.Playwright;
using Shouldly;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

/// <summary>
/// Pins the harness's central premise: every leg renders under one stylesheet, produced
/// by a single Tailwind build that scans both source trees. If the legs resolved
/// utilities from separate builds, a Tailwind configuration difference would land in the
/// computed-style, geometry, and pixel comparisons as a fake component discrepancy.
/// </summary>
/// <param name="playwright">The browser fixture.</param>
public sealed class SharedStylesheetTests(PlaywrightFixture playwright)
    : IClassFixture<PlaywrightFixture>
{
    // Reads what the page actually resolved rather than the href it declares: the two
    // sides spell the link differently — the Blazor host relative against <base href="/">,
    // the React bundle rooted because it is mounted under /react/ — and only the fetched
    // bytes settle whether both spellings land on the same file.
    private const string ReadStylesheets =
        """
        async () => {
          const links = [...document.querySelectorAll('link[rel=stylesheet]')];
          const out = [];
          for (const link of links) {
            const text = await (await fetch(link.href)).text();
            const digest = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(text));
            out.push([...new Uint8Array(digest)].map((b) => b.toString(16).padStart(2, '0')).join(''));
          }
          return out;
        }
        """;

    [Theory]
    [InlineData("switch/hero")]
    [InlineData("collapsible/hero")]
    [InlineData("popover/hero")]
    [InlineData("select/grouped")]
    [InlineData("field/hero")]
    [InlineData("avatar/hero")]
    [InlineData("separator/hero")]
    [InlineData("progress/hero")]
    [InlineData("meter/hero")]
    [InlineData("accordion/multiple")]
    [InlineData("dialog/hero")]
    [InlineData("drawer/hero")]
    [InlineData("toast/hero")]
    [InlineData("tooltip/hero")]
    [InlineData("preview-card/hero")]
    [InlineData("menu/arrow")]
    [InlineData("select/hero")]
    [InlineData("menu/checkbox-items")]
    [InlineData("menubar/hero")]
    [InlineData("tabs/hero")]
    [InlineData("toolbar/hero")]
    [InlineData("form/hero")]
    [InlineData("number-field/hero")]
    [InlineData("checkbox/hero")]
    [InlineData("otp-field/hero")]
    [InlineData("popover/detached-triggers-simple")]
    [InlineData("navigation-menu/hero")]
    [InlineData("scroll-area/hero")]
    [InlineData("combobox/hero")]
    public async Task EveryPublishedFixtureLegLoadsTheSameSingleStylesheet(string fixture)
    {
        var server = ParityServerAssemblyFixture.ServerAddress;

        await using var context = await playwright.Browser.NewContextAsync();

        var react = await DigestsAsync(context, $"{server}/react/#/fixture/{fixture}");
        var blazorServer = await DigestsAsync(context, $"{server}/fixture/{fixture}/server");
        var blazorWasm = await DigestsAsync(context, $"{server}/fixture/{fixture}/wasm");

        // One each: a second sheet on any leg is a second Tailwind build, which is the
        // shape the defect took — the React bundle emitted its own uncompiled copy.
        react.Count.ShouldBe(1);
        blazorServer.Count.ShouldBe(1);
        blazorWasm.Count.ShouldBe(1);

        blazorServer.ShouldBe(react);
        blazorWasm.ShouldBe(react);
    }

    [Fact]
    public void ReactBuildProvenanceContainsThePublishedManifestAndCanaryInOrder()
    {
        var provenance = JsonSerializer.Deserialize<ReactBundleProvenanceManifest>(File.ReadAllText(
            Path.Combine(ParityPaths.ReactDist, ReactBundleProvenanceManifest.FileName)));

        provenance.ShouldNotBeNull();
        provenance.SchemaVersion.ShouldBe(ReactBundleProvenanceManifest.CurrentSchemaVersion);
        provenance.Sources.Select(source => source.Fixture)
            .ShouldBe(FixtureManifest.Load().Select(fixture => fixture.Id).Append("harness/canary"));
        provenance.Sources.Select(source => source.SourcePath).Distinct().Count()
            .ShouldBe(provenance.Sources.Count);
    }

    [Theory]
    [InlineData("switch/hero", "[role=switch]", "element => getComputedStyle(element).display", "flex")]
    [InlineData("collapsible/hero", "button[aria-expanded]", "element => getComputedStyle(element.parentElement).minHeight", "144px")]
    [InlineData("popover/hero", "button[aria-haspopup=dialog]", "element => getComputedStyle(element).height", "32px")]
    [InlineData("select/grouped", "[role=combobox]", "element => getComputedStyle(element).minWidth", "176px")]
    [InlineData("field/hero", "input", "element => getComputedStyle(element).alignSelf", "stretch")]
    public async Task TheSharedStylesheetCarriesEachCalibrationFixturesCompiledUtilities(
        string fixture,
        string selector,
        string readStyle,
        string expected)
    {
        // Guards the test above from passing vacuously. Equal digests only prove the legs
        // agree; they would still agree if every leg loaded the same *uncompiled* source,
        // which is a stylesheet of preflight and theme variables and no utilities at all —
        // exactly what the React bundle shipped. Asserted through the rendered result
        // rather than by grepping the file, because a utility that is present but not
        // applied leaves the comparison just as empty.
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{ParityServerAssemblyFixture.ServerAddress}/react/#/fixture/{fixture}");
        var target = page.Locator(selector).First;
        await target.WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Attached });

        var value = await target.EvaluateAsync<string>(readStyle);

        // The demo's `flex h-5 w-9`. Unstyled the element is an inline span at its
        // content width, so this reads `inline` when no utility compiled.
        value.ShouldBe(expected);
    }

    [Theory]
    [InlineData("avatar/hero", "img", "element => getComputedStyle(element).objectFit", "cover")]
    [InlineData("separator/hero", "[role=separator]", "element => getComputedStyle(element).width", "1px")]
    [InlineData("progress/hero", "[role=progressbar]", "element => getComputedStyle(element).display", "grid")]
    [InlineData("meter/hero", "[role=meter]", "element => getComputedStyle(element).display", "grid")]
    [InlineData("accordion/multiple", "button[aria-expanded]", "element => getComputedStyle(element).display", "flex")]
    [InlineData("dialog/hero", "button[aria-haspopup=dialog]", "element => getComputedStyle(element).height", "32px")]
    [InlineData("drawer/hero", "button[aria-haspopup=dialog]", "element => getComputedStyle(element).paddingLeft", "12px")]
    [InlineData("toast/hero", "button[type=button]", "element => getComputedStyle(element).height", "32px")]
    [InlineData("tooltip/hero", "button[aria-label=Bold]", "element => getComputedStyle(element).width", "32px")]
    [InlineData("preview-card/hero", "p", "element => getComputedStyle(element).textWrap", "balance")]
    [InlineData("menu/arrow", "button[aria-haspopup=menu]", "element => getComputedStyle(element).columnGap", "6px")]
    [InlineData("select/hero", "[role=combobox]", "element => getComputedStyle(element).minWidth", "160px")]
    [InlineData("menu/checkbox-items", "button[aria-haspopup=menu]", "element => getComputedStyle(element).paddingRight", "8px")]
    [InlineData("menubar/hero", "button[aria-haspopup=menu]", "element => getComputedStyle(element).height", "32px")]
    [InlineData("tabs/hero", "[role=tablist]", "element => getComputedStyle(element).columnGap", "4px")]
    [InlineData("toolbar/hero", "[role=toolbar]", "element => getComputedStyle(element).width", "600px")]
    [InlineData("form/hero", "form", "element => getComputedStyle(element).maxWidth", "256px")]
    [InlineData("number-field/hero", "input", "element => getComputedStyle(element.parentElement).height", "32px")]
    [InlineData("checkbox/hero", "[role=checkbox]", "element => getComputedStyle(element).width", "16px")]
    [InlineData("otp-field/hero", "input", "element => getComputedStyle(element).height", "40px")]
    [InlineData("popover/detached-triggers-simple", "button[aria-haspopup=dialog]", "element => getComputedStyle(element).height", "32px")]
    [InlineData("navigation-menu/hero", "button", "element => getComputedStyle(element).paddingLeft", "12px")]
    [InlineData("scroll-area/hero", "[class~='h-[8.5rem]']", "element => getComputedStyle(element).height", "136px")]
    [InlineData("combobox/hero", "input[role=combobox]", "element => getComputedStyle(element.parentElement).height", "32px")]
    public async Task TheSharedStylesheetCarriesEachTask16FixturesSourceSpecificCompiledUtility(
        string fixture,
        string selector,
        string readStyle,
        string expected)
    {
        // The equal-digest theory proves that all three legs fetch the same bytes. These
        // rendered probes prove that every newly published source contributed at least one
        // fixture-specific utility to those bytes and that the browser actually applies it.
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{ParityServerAssemblyFixture.ServerAddress}/react/#/fixture/{fixture}");
        var target = page.Locator(selector).First;
        await target.WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Attached });

        var value = await target.EvaluateAsync<string>(readStyle);
        value.ShouldBe(expected);
    }

    private static async Task<List<string>> DigestsAsync(IBrowserContext context, string url)
    {
        var page = await context.NewPageAsync();
        try
        {
            // The links live in the served HTML, so a parsed document is enough. Waiting
            // for the network to idle would make the WASM leg pay for the whole runtime
            // download to answer a question already settled by the <head>.
            await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            return [.. await page.EvaluateAsync<string[]>(ReadStylesheets)];
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
