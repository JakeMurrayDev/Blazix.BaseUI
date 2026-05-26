using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace BlazorBaseUI.Playwright.Tests.Tests.Dialog;

public class DialogTestsServer : DialogTestsBase, IClassFixture<PlaywrightFixture>
{
    protected override TestRenderMode RenderMode => TestRenderMode.Server;

    public DialogTestsServer(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    /// <summary>
    /// Tests that a modal dialog closes when the click target is outside the popup,
    /// even if that target is stacked above the backdrop.
    /// </summary>
    [Fact]
    public async Task OutsideClick_ClosesModalDialogWhenTargetIsAboveBackdrop()
    {
        await NavigateAsync(CreateUrl("/tests/dialog")
            .WithModal(true)
            .WithShowBackdrop(true)
            .WithOutsideAboveBackdrop(true));

        await OpenDialogAsync();

        var outsideButton = GetByTestId("outside-button");
        await Assertions.Expect(outsideButton).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 * TimeoutMultiplier });
        await Assertions.Expect(outsideButton).ToBeEnabledAsync(
            new LocatorAssertionsToBeEnabledOptions { Timeout = 5000 * TimeoutMultiplier });

        await outsideButton.ClickAsync();

        await WaitForDialogClosedAsync();
    }
}
