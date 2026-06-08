using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Tests.Toast;

public abstract class ToastTestsBase : TestBase
{
    protected ToastTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    [Fact]
    public virtual async Task LowPriorityToast_RendersDialogAndViewportAttributes()
    {
        await NavigateAsync(CreateUrl("/tests/toast"));

        await GetByTestId("add-low").ClickAsync();

        var viewport = GetByTestId("toast-viewport");
        await Assertions.Expect(viewport).ToHaveAttributeAsync("role", "region");
        await Assertions.Expect(viewport).ToHaveAttributeAsync("aria-live", "polite");
        await Assertions.Expect(viewport).ToHaveAttributeAsync("aria-atomic", "false");
        await Assertions.Expect(viewport).ToHaveAttributeAsync("aria-relevant", "additions text");
        await Assertions.Expect(viewport).ToHaveAttributeAsync("aria-label", "Notifications");

        var toast = GetByTestId("toast-low");
        await Assertions.Expect(toast).ToBeVisibleAsync();
        await Assertions.Expect(toast).ToHaveAttributeAsync("role", "dialog");
        await Assertions.Expect(toast).ToHaveAttributeAsync("aria-modal", "false");
        await Assertions.Expect(toast).ToHaveAttributeAsync("data-type", "info");
        await Assertions.Expect(toast).ToContainTextAsync("Low priority");
    }

    [Fact]
    public virtual async Task F6FocusesViewportAndExpandsHighPriorityToast()
    {
        await NavigateAsync(CreateUrl("/tests/toast"));

        await GetByTestId("add-high").ClickAsync();

        var toast = GetByTestId("toast-high");
        await Assertions.Expect(toast).ToHaveAttributeAsync("role", "alertdialog");
        await Assertions.Expect(toast).ToHaveAttributeAsync("aria-hidden", "true");

        await Page.Keyboard.PressAsync("F6");

        await Assertions.Expect(GetByTestId("toast-viewport")).ToBeFocusedAsync();
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('[data-testid=\"toast-high\"]')?.hasAttribute('aria-hidden') === false",
            new PageWaitForFunctionOptions { Timeout = 5000 * TimeoutMultiplier });
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('[data-testid=\"toast-viewport\"]')?.hasAttribute('data-expanded') === true",
            new PageWaitForFunctionOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    [Fact]
    public virtual async Task CloseButtonClosesAndRemovesToast()
    {
        await NavigateAsync(CreateUrl("/tests/toast"));

        await GetByTestId("add-low").ClickAsync();
        await Assertions.Expect(GetByTestId("toast-low")).ToBeVisibleAsync();

        await GetByTestId("toast-close-low").ClickAsync();

        await Assertions.Expect(GetByTestId("closed-count")).ToHaveTextAsync("1");
        await Assertions.Expect(GetByTestId("removed-count")).ToHaveTextAsync("1");
        await Assertions.Expect(GetByTestId("toast-low")).ToBeHiddenAsync();
    }

    [Fact]
    public virtual async Task LimitMarksOldestActiveToastLimited()
    {
        await NavigateAsync(CreateUrl("/tests/toast"));

        await GetByTestId("add-three").ClickAsync();

        await Assertions.Expect(GetByTestId("toast-limit-1")).ToBeVisibleAsync();
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('[data-testid=\"toast-limit-1\"]')?.hasAttribute('data-limited') === true",
            new PageWaitForFunctionOptions { Timeout = 5000 * TimeoutMultiplier });
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('[data-testid=\"toast-limit-3\"]')?.hasAttribute('data-limited') === false",
            new PageWaitForFunctionOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    [Fact]
    public virtual async Task DisabledActionDoesNotInvokeActionCallback()
    {
        await NavigateAsync(CreateUrl("/tests/toast"));

        await GetByTestId("add-disabled-action").ClickAsync();

        var action = GetByTestId("toast-action-disabled-action");
        await Assertions.Expect(action).ToBeDisabledAsync();

        await action.EvaluateAsync("element => element.click()");
        await Assertions.Expect(GetByTestId("action-count")).ToHaveTextAsync("0");
    }

    [Fact]
    public virtual async Task PromiseToastUpdatesFromLoadingToSuccess()
    {
        await NavigateAsync(CreateUrl("/tests/toast"));

        await GetByTestId("add-promise").ClickAsync();

        var loading = Page.Locator("[data-blazix-base-ui-toast-root][data-type='loading']");
        await Assertions.Expect(loading).ToContainTextAsync("Loading promise");

        var success = Page.Locator("[data-blazix-base-ui-toast-root][data-type='success']");
        await Assertions.Expect(success).ToContainTextAsync("Promise resolved 7", new LocatorAssertionsToContainTextOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    [Fact]
    public virtual async Task SwipeDismissesToastAndPublishesSwipeAttributes()
    {
        await NavigateAsync(CreateUrl("/tests/toast"));

        await GetByTestId("add-low").ClickAsync();

        var toast = GetByTestId("toast-low");
        await Assertions.Expect(toast).ToBeVisibleAsync();

        var box = await toast.BoundingBoxAsync();
        Assert.NotNull(box);

        var startX = box.X + 20;
        var startY = box.Y + 20;
        await Page.Mouse.MoveAsync(startX, startY);
        await Page.Mouse.DownAsync();
        try
        {
            await Page.Mouse.MoveAsync(startX + 96, startY, new MouseMoveOptions { Steps = 8 });

            await Page.WaitForFunctionAsync(
                "() => { const toast = document.querySelector('[data-testid=\"toast-low\"]'); return toast?.hasAttribute('data-swiping') === true && toast?.getAttribute('data-swipe-direction') === 'right' && toast?.style.getPropertyValue('--toast-swipe-movement-x') !== '0px'; }",
                new PageWaitForFunctionOptions { Timeout = 5000 * TimeoutMultiplier });
        }
        finally
        {
            await Page.Mouse.UpAsync();
        }

        await Assertions.Expect(GetByTestId("closed-count")).ToHaveTextAsync("1");
        await Assertions.Expect(GetByTestId("removed-count")).ToHaveTextAsync("1");
        await Assertions.Expect(toast).ToBeHiddenAsync();
    }
}
