using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;

namespace BlazorBaseUI.Playwright.Tests.Tests.Select;

public class SelectLabelTestsServer : SelectLabelTestsBase, IClassFixture<PlaywrightFixture>
{
    protected override TestRenderMode RenderMode => TestRenderMode.Server;

    public SelectLabelTestsServer(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }
}
