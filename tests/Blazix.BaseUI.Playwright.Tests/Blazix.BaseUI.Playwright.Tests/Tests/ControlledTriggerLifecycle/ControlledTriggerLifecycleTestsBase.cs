using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Tests.ControlledTriggerLifecycle;

public abstract class ControlledTriggerLifecycleTestsBase : TestBase
{
    protected ControlledTriggerLifecycleTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    [Theory]
    [InlineData("menu")]
    [InlineData("popover")]
    [InlineData("tooltip")]
    [InlineData("preview-card")]
    [InlineData("dialog")]
    public virtual async Task ControlledLifecyclePreservesAssociationAndPayload(string componentName)
    {
        await NavigateAsync(CreateUrl("/tests/controlled-trigger-lifecycle")
            .WithControlledTriggerComponent(componentName));

        await GetByTestId("open-a-button").ClickAsync();
        await AssertOpenStateAsync("trigger-a", "Payload A");

        await GetByTestId("open-b-button").ClickAsync();
        await AssertOpenStateAsync("trigger-b", "Payload B");

        await GetByTestId("close-button").ClickAsync();
        await Assertions.Expect(GetByTestId("open-state")).ToHaveTextAsync("false");
        await Assertions.Expect(GetByTestId("handle-open")).ToHaveTextAsync("false");

        await GetByTestId("reopen-button").ClickAsync();
        await AssertOpenStateAsync("trigger-b", "Payload B");
    }

    private async Task AssertOpenStateAsync(string triggerId, string payload)
    {
        var timeout = 5000 * TimeoutMultiplier;
        await Assertions.Expect(GetByTestId("open-state")).ToHaveTextAsync("true", new LocatorAssertionsToHaveTextOptions { Timeout = timeout });
        await Assertions.Expect(GetByTestId("payload-display")).ToHaveTextAsync(payload, new LocatorAssertionsToHaveTextOptions { Timeout = timeout });
        await Assertions.Expect(GetByTestId("handle-open")).ToHaveTextAsync("true", new LocatorAssertionsToHaveTextOptions { Timeout = timeout });
        await Assertions.Expect(GetByTestId("handle-trigger")).ToHaveTextAsync(triggerId, new LocatorAssertionsToHaveTextOptions { Timeout = timeout });
        await Assertions.Expect(GetByTestId("handle-payload")).ToHaveTextAsync(payload, new LocatorAssertionsToHaveTextOptions { Timeout = timeout });
    }
}
