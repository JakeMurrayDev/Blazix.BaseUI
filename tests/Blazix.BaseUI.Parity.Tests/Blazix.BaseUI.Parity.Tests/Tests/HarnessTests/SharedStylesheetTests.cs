using Blazix.BaseUI.Parity.Tests.Fixtures;
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

    [Fact]
    public async Task EveryLegLoadsTheSameSingleStylesheet()
    {
        var server = ParityServerAssemblyFixture.ServerAddress;

        await using var context = await playwright.Browser.NewContextAsync();

        var react = await DigestsAsync(context, $"{server}/react/#/fixture/switch/hero");
        var blazorServer = await DigestsAsync(context, $"{server}/fixture/switch/hero/server");
        var blazorWasm = await DigestsAsync(context, $"{server}/fixture/switch/hero/wasm");

        // One each: a second sheet on any leg is a second Tailwind build, which is the
        // shape the defect took — the React bundle emitted its own uncompiled copy.
        react.Count.ShouldBe(1);
        blazorServer.Count.ShouldBe(1);
        blazorWasm.Count.ShouldBe(1);

        blazorServer.ShouldBe(react);
        blazorWasm.ShouldBe(react);
    }

    [Fact]
    public async Task TheSharedStylesheetCarriesCompiledUtilities()
    {
        // Guards the test above from passing vacuously. Equal digests only prove the legs
        // agree; they would still agree if every leg loaded the same *uncompiled* source,
        // which is a stylesheet of preflight and theme variables and no utilities at all —
        // exactly what the React bundle shipped. Asserted through the rendered result
        // rather than by grepping the file, because a utility that is present but not
        // applied leaves the comparison just as empty.
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{ParityServerAssemblyFixture.ServerAddress}/react/#/fixture/switch/hero");
        await page.Locator("[role=switch]").First.WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Attached });

        var display = await page.EvaluateAsync<string>(
            "() => getComputedStyle(document.querySelector('[role=switch]')).display");

        // The demo's `flex h-5 w-9`. Unstyled the element is an inline span at its
        // content width, so this reads `inline` when no utility compiled.
        display.ShouldBe("flex");
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
