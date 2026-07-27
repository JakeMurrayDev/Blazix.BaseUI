using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Fixtures;
using Blazix.BaseUI.Parity.Tests.Infrastructure;
using Shouldly;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

/// <summary>
/// Pins the normalization rules of the shared capture script against a probe fixture
/// whose markup is known, so the assertions can be exact.
/// </summary>
/// <param name="playwright">The browser fixture.</param>
public sealed class CaptureScriptTests(PlaywrightFixture playwright)
    : IClassFixture<PlaywrightFixture>
{
    [Fact]
    public async Task SymbolizesGeneratedIdsAndPreservesReferences()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await CaptureScript.InjectAsync(page);
        await page.GotoAsync($"{ParityServerAssemblyFixture.ServerAddress}/fixture/harness/capture-probe/server");
        await SettleProtocol.WaitAsync(page);

        var capture = await CaptureScript.CaptureAsync(page, "initial");

        var button = capture.Dom.Descendants().Single(n => n.Tag == "button");
        var panel = capture.Dom.Descendants().Single(n => n.Tag == "section");

        // The raw ids differ between React and Blazor; the RELATIONSHIP must survive.
        button.Attributes["aria-controls"].ShouldBe(panel.Attributes["id"]);
        panel.Attributes["id"].ShouldStartWith("#id");
    }

    [Fact]
    public async Task RenamesBlazixMarkersToUpstreamForm()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await CaptureScript.InjectAsync(page);
        await page.GotoAsync($"{ParityServerAssemblyFixture.ServerAddress}/fixture/harness/capture-probe/server");
        await SettleProtocol.WaitAsync(page);

        var capture = await CaptureScript.CaptureAsync(page, "initial");
        var marked = capture.Dom.Descendants().Single(n => n.Tag == "aside");

        marked.Attributes.ShouldContainKey("data-base-ui-portal");
        marked.Attributes.ShouldNotContainKey("data-blazix-base-ui-portal");

        // Idempotent: an already-unprefixed marker passes through untouched.
        marked.Attributes.ShouldContainKey("data-base-ui-focusable");
    }

    [Fact]
    public async Task ExcludesClassAndStyleFromAttributesButRecordsClassSeparately()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await CaptureScript.InjectAsync(page);
        await page.GotoAsync($"{ParityServerAssemblyFixture.ServerAddress}/fixture/harness/capture-probe/server");
        await SettleProtocol.WaitAsync(page);

        var capture = await CaptureScript.CaptureAsync(page, "initial");
        var button = capture.Dom.Descendants().Single(n => n.Tag == "button");

        button.Attributes.ShouldNotContainKey("class");
        button.Attributes.ShouldNotContainKey("style");
        button.Classes.ShouldContain("px-3");
    }
}
