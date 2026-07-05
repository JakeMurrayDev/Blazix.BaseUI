using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;

namespace Blazix.BaseUI.Playwright.Tests.Tests.Combobox;

public class ComboboxTestsWasm : ComboboxTestsBase, IClassFixture<PlaywrightFixture>
{
    protected override TestRenderMode RenderMode => TestRenderMode.Wasm;

    public ComboboxTestsWasm(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }
}
