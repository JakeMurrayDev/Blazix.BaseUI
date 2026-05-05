using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;

namespace BlazorBaseUI.Playwright.Tests.Tests.Select;

public class SelectTriggerCancelTestsServer : SelectTriggerCancelTestsBase, IClassFixture<PlaywrightFixture>
{
    protected override TestRenderMode RenderMode => TestRenderMode.Server;

    public SelectTriggerCancelTestsServer(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }
}
