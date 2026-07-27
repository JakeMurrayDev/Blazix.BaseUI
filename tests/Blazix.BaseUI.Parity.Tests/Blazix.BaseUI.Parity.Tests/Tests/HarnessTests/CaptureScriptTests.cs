using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Fixtures;
using Blazix.BaseUI.Parity.Tests.Infrastructure;
using Microsoft.Playwright;
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
        var page = await OpenProbeAsync(context);

        var capture = await CaptureScript.CaptureAsync(page, "initial");

        var button = capture.Dom.Descendants().Single(n => n.Tag == "button");
        var panel = capture.Dom.Descendants().Single(n => n.Tag == "section");

        // The panel is portalled to <body>, so the trigger and its target land in
        // different capture roots. One id table per root cannot see across that
        // boundary and leaves the reference as a raw framework-generated id.
        panel.Attributes["id"].ShouldStartWith("#id");

        // The raw ids differ between React and Blazor; the RELATIONSHIP must survive.
        button.Attributes["aria-controls"].ShouldBe(panel.Attributes["id"]);
    }

    [Fact]
    public async Task RenamesBlazixMarkersToUpstreamForm()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await OpenProbeAsync(context);

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
        var page = await OpenProbeAsync(context);

        var capture = await CaptureScript.CaptureAsync(page, "initial");
        var button = capture.Dom.Descendants().Single(n => n.Tag == "button");

        button.Attributes.ShouldNotContainKey("class");
        button.Attributes.ShouldNotContainKey("style");
        button.Classes.ShouldContain("px-3");
    }

    [Fact]
    public async Task ExcludesTailwindInternalsButKeepsOtherCustomProperties()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await OpenProbeAsync(context);

        var capture = await CaptureScript.CaptureAsync(page, "initial");
        var button = capture.Dom.Descendants().Single(n => n.Tag == "button");
        var props = capture.CustomProps[button.Path];

        // Asserted first: without a property that must survive, a readCustomProps
        // returning nothing at all would satisfy the exclusion assertion below.
        props["--parity-probe"].ShouldBe("7px");

        props.Keys.ShouldNotContain(name => name.StartsWith("--tw-", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExcludesBlazorElementReferenceAttributes()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await OpenProbeAsync(context);

        // Asserted first so the exclusion below cannot pass vacuously: `_bl_` is applied
        // by Blazor's browser renderer, never by the server, so a probe without a
        // RenderElement-backed component would carry none of them to begin with.
        var live = await page.EvaluateAsync<int>(
            """
            () => [...document.querySelectorAll('*')]
              .filter((e) => e.getAttributeNames().some((n) => n.startsWith('_bl_')))
              .length
            """);
        live.ShouldBeGreaterThan(0);

        var capture = await CaptureScript.CaptureAsync(page, "initial");
        var leaked = capture.Dom.Descendants()
            .SelectMany(n => n.Attributes.Keys)
            .Where(name => name.StartsWith("_bl_", StringComparison.Ordinal))
            .ToList();

        // The name embeds a per-run reference-capture id, so leaking even one makes the
        // element diff against React and differ from itself on the next run.
        leaked.ShouldBeEmpty();
    }

    [Fact]
    public async Task NamespacesNodePathsByRoot()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await OpenProbeAsync(context);

        var capture = await CaptureScript.CaptureAsync(page, "initial");

        // #roots is a synthetic wrapper, not a DOM node, and holds no keyed data.
        var nodes = capture.Dom.Descendants().Where(n => n.Tag != "#roots").ToList();

        var inRoot = nodes.Single(n => n.Attributes.GetValueOrDefault("data-probe") == "in-root");
        var inPortal = nodes.Single(n => n.Attributes.GetValueOrDefault("data-probe") == "in-portal");

        inRoot.Path.ShouldBe("root > p");
        inPortal.Path.ShouldBe("portal(1) > p");

        // Both root elements are captured too, and un-namespaced they share the path "".
        nodes.Select(n => n.Path).ShouldBeUnique();

        // The keyed side-tables are what the comparators read. A collision does not throw,
        // it overwrites — so assert the counts line up rather than just spot-checking keys.
        capture.Geometry.Keys.ShouldContain("root");
        capture.Geometry.Keys.ShouldContain("portal(1)");
        capture.Geometry.Count.ShouldBe(nodes.Count);
        capture.Styles.Count.ShouldBe(nodes.Count);
        capture.CustomProps.Count.ShouldBe(nodes.Count);
    }

    [Fact]
    public async Task StartTimelineDiscardsThePreviousRegistration()
    {
        await using var context = await playwright.Browser.NewContextAsync();
        var page = await OpenProbeAsync(context);

        // A per-step runner naturally does start -> capture -> start -> capture, because
        // capture() returns the timeline without stopping it.
        var recorded = await page.EvaluateAsync<int>(
            """
            async () => {
              const api = window[Symbol.for('Blazix.Parity.Capture')];
              api.startTimeline();
              api.startTimeline();
              document.querySelector('[data-parity-root]').setAttribute('data-probe-tick', '1');
              await new Promise((resolve) => setTimeout(resolve, 100));
              return api.stopTimeline().filter((e) => e.attr === 'data-probe-tick').length;
            }
            """);

        recorded.ShouldBe(1);
    }

    private static async Task<IPage> OpenProbeAsync(IBrowserContext context)
    {
        var page = await context.NewPageAsync();
        await CaptureScript.InjectAsync(page);
        await page.GotoAsync($"{ParityServerAssemblyFixture.ServerAddress}/fixture/harness/capture-probe/server");

        // The Portal container is appended to <body> from OnAfterRenderAsync, after a
        // module import and an interop round trip, which can land later than the settle
        // protocol's two quiet frames. SettleProtocol now gates on the container's own
        // mid-mount flag, so no local wait is needed here.
        await SettleProtocol.WaitAsync(page);

        return page;
    }
}
