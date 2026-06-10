using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Tests.Avatar;

/// <summary>
/// Integration tests for Avatar components requiring real browser/JS execution.
/// Unit-level tests (rendering, attributes, styles) are covered by bUnit tests.
/// </summary>
public abstract class AvatarTestsBase : TestBase
{
    protected AvatarTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    #region Image Loading Tests (Real JS Interop)

    [Fact]
    public virtual async Task RendersWhenLoaded()
    {
        await NavigateAsync(CreateUrl("/tests/avatar"));

        var loadingStatus = GetByTestId("loading-status");
        await WaitForTextContentAsync(loadingStatus, "loaded", 15000);

        var image = GetByTestId("avatar-image");

        await Assertions.Expect(image).ToBeVisibleAsync();

        var tagName = await image.EvaluateAsync<string>("el => el.tagName.toLowerCase()");
        Assert.Equal("img", tagName);
    }

    [Fact]
    public virtual async Task DoesNotRenderWhenNotLoaded()
    {
        await NavigateAsync(CreateUrl("/tests/avatar").WithForceError(true));

        var loadingStatus = GetByTestId("loading-status");
        await WaitForTextContentAsync(loadingStatus, "error", 15000);

        var image = GetByTestId("avatar-image");

        await Assertions.Expect(image).Not.ToBeVisibleAsync();
    }

    [Fact]
    public virtual async Task UpdatesStatusOnLoad()
    {
        await NavigateAsync(CreateUrl("/tests/avatar"));

        var loadingStatus = GetByTestId("loading-status");

        await WaitForTextContentAsync(loadingStatus, "loaded", 15000);

        await Assertions.Expect(loadingStatus).ToHaveTextAsync("loaded");
    }

    [Fact]
    public virtual async Task UpdatesStatusOnError()
    {
        await NavigateAsync(CreateUrl("/tests/avatar").WithForceError(true));

        var loadingStatus = GetByTestId("loading-status");

        await WaitForTextContentAsync(loadingStatus, "error", 15000);

        await Assertions.Expect(loadingStatus).ToHaveTextAsync("error");
    }

    [Fact]
    public virtual async Task InvokesOnLoadingStatusChange()
    {
        await NavigateAsync(CreateUrl("/tests/avatar"));

        var statusChangeCount = GetByTestId("status-change-count");

        var loadingStatus = GetByTestId("loading-status");
        await WaitForTextContentAsync(loadingStatus, "loaded", 15000);

        var count = await statusChangeCount.TextContentAsync();
        var countValue = int.Parse(count ?? "0");

        Assert.True(countValue >= 1, $"Expected status change count >= 1, got {countValue}");
    }

    #endregion

    #region Fallback Tests (Real JS Image Error Handling)

    [Fact]
    public virtual async Task RendersWhenImageFails()
    {
        await NavigateAsync(CreateUrl("/tests/avatar").WithForceError(true));

        var loadingStatus = GetByTestId("loading-status");
        await WaitForTextContentAsync(loadingStatus, "error", 15000);

        var fallback = GetByTestId("avatar-fallback");

        await Assertions.Expect(fallback).ToBeVisibleAsync();
    }

    [Fact]
    public virtual async Task DoesNotRenderWhenImageLoaded()
    {
        await NavigateAsync(CreateUrl("/tests/avatar"));

        var loadingStatus = GetByTestId("loading-status");
        await WaitForTextContentAsync(loadingStatus, "loaded", 15000);

        var fallback = GetByTestId("avatar-fallback");

        await Assertions.Expect(fallback).Not.ToBeVisibleAsync();
    }

    #endregion

    #region Helper Methods

    protected async Task WaitForTextContentAsync(ILocator element, string expectedText, int timeout = 5000)
    {
        var startTime = DateTime.UtcNow;
        while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeout)
        {
            var text = await element.TextContentAsync();
            if (text == expectedText)
            {
                Console.WriteLine($"[Debug] Found expected text '{expectedText}' at {(DateTime.UtcNow - startTime).TotalMilliseconds}ms");
                await Page.WaitForTimeoutAsync(500);
                return;
            }
            await Task.Delay(100);
        }

        var finalText = await element.TextContentAsync();
        throw new TimeoutException($"Text content did not reach '{expectedText}' within {timeout}ms. Current: '{finalText}'");
    }

    #endregion
}
