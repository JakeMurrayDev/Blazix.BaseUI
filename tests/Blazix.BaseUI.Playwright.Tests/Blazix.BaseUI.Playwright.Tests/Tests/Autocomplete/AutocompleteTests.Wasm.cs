using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;

namespace Blazix.BaseUI.Playwright.Tests.Tests.Autocomplete;

public class AutocompleteTestsWasm : AutocompleteTestsBase, IClassFixture<PlaywrightFixture>
{
    protected override TestRenderMode RenderMode => TestRenderMode.Wasm;

    public AutocompleteTestsWasm(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }
}
