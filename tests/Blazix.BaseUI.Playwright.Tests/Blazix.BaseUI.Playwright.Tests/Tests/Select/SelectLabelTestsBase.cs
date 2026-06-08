using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Tests.Select;

public abstract class SelectLabelTestsBase : TestBase
{
    protected SelectLabelTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    [Fact]
    public virtual async Task LabelHasRootIdMinusLabelId()
    {
        await NavigateAsync(CreateUrl("/tests/select-label"));

        var label = GetByTestId("select-label");
        await Assertions.Expect(label).ToHaveAttributeAsync("id", "fruit-select-label");
    }

    [Fact]
    public virtual async Task TriggerIsLabelledByLabelId()
    {
        await NavigateAsync(CreateUrl("/tests/select-label"));

        var trigger = GetByTestId("select-trigger");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-labelledby", "fruit-select-label");
    }

    [Fact]
    public virtual async Task ClickingLabelFocusesTrigger()
    {
        await NavigateAsync(CreateUrl("/tests/select-label"));

        var label = GetByTestId("select-label");
        await label.ClickAsync();

        var trigger = GetByTestId("select-trigger");
        await Assertions.Expect(trigger).ToBeFocusedAsync();
    }
}
