using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;

namespace Blazix.BaseUI.Playwright.Tests.Tests.Combobox;

public class ComboboxTestsServer : ComboboxTestsBase, IClassFixture<PlaywrightFixture>
{
    protected override TestRenderMode RenderMode => TestRenderMode.Server;

    public ComboboxTestsServer(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }
}
