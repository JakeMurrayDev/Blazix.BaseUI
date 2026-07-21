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

    [Fact]
    public virtual async Task CanonicalSwipeIgnoreTargetDoesNotStartOrDismissSwipe()
    {
        await NavigateAsync(CreateUrl("/tests/toast"));
        await GetByTestId("add-ignore").ClickAsync();

        var target = GetByTestId("swipe-ignore");
        var toast = GetByTestId("toast-ignore");
        var box = await target.BoundingBoxAsync();
        Assert.NotNull(box);

        await Page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(box.X + box.Width + 100, box.Y + box.Height / 2);
        await Page.Mouse.UpAsync();

        await Assertions.Expect(toast).Not.ToHaveAttributeAsync("data-swiping", string.Empty);
        await Assertions.Expect(GetByTestId("closed-count")).ToHaveTextAsync("0");
        await Assertions.Expect(toast).ToBeVisibleAsync();
    }

    [Fact]
    public virtual async Task DocumentPointerReleaseClearsSwipeAndAllowsNextDismissal()
    {
        await NavigateAsync(CreateUrl("/tests/toast"));
        await GetByTestId("add-low").ClickAsync();

        var toast = GetByTestId("toast-low");
        var box = await toast.BoundingBoxAsync();
        Assert.NotNull(box);
        var startX = box.X + 20;
        var startY = box.Y + 20;

        await Page.Mouse.MoveAsync(startX, startY);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(startX + 20, startY);
        await Page.EvaluateAsync("() => document.dispatchEvent(new PointerEvent('pointerup', { bubbles: true, pointerId: 1, pointerType: 'mouse', button: 0 }))");
        await Page.Mouse.UpAsync();

        await Assertions.Expect(toast).Not.ToHaveAttributeAsync("data-swiping", string.Empty);
        await Assertions.Expect(GetByTestId("closed-count")).ToHaveTextAsync("0");

        box = await toast.BoundingBoxAsync();
        Assert.NotNull(box);
        startX = box.X + 20;
        startY = box.Y + 20;
        await Page.Mouse.MoveAsync(startX, startY);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(startX + 96, startY, new MouseMoveOptions { Steps = 8 });
        await Page.Mouse.UpAsync();

        await Assertions.Expect(GetByTestId("closed-count")).ToHaveTextAsync("1");
        await Assertions.Expect(toast).ToBeHiddenAsync();
    }

    [Fact]
    public virtual async Task TouchMoveIsPreventedOnlyDuringAnActiveSwipe()
    {
        await NavigateAsync(CreateUrl("/tests/toast"));
        await GetByTestId("add-low").ClickAsync();
        var toast = GetByTestId("toast-low");

        var beforeSwipe = await toast.EvaluateAsync<bool>("element => { const event = new Event('touchmove', { bubbles: true, cancelable: true }); element.dispatchEvent(event); return event.defaultPrevented; }");
        Assert.False(beforeSwipe);

        var duringSwipe = await toast.EvaluateAsync<bool>("element => { element.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true, pointerId: 77, pointerType: 'touch', button: 0, clientX: 10, clientY: 10 })); const event = new Event('touchmove', { bubbles: true, cancelable: true }); element.dispatchEvent(event); document.dispatchEvent(new PointerEvent('pointercancel', { bubbles: true, pointerId: 77, pointerType: 'touch' })); return event.defaultPrevented; }");
        Assert.True(duringSwipe);
    }

    [Fact]
    public virtual async Task NonNativeActionAndCloseImplementEnterAndSpaceActivation()
    {
        await NavigateAsync(CreateUrl("/tests/toast"));
        await GetByTestId("add-keyboard-action").ClickAsync();

        var action = GetByTestId("toast-action-keyboard-action");
        await Assertions.Expect(action).ToHaveAttributeAsync("role", "button");
        await action.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.PressAsync("Space");
        await Assertions.Expect(GetByTestId("action-count")).ToHaveTextAsync("2");

        await GetByTestId("add-keyboard-close").ClickAsync();
        var close = GetByTestId("toast-close-keyboard-close");
        await Assertions.Expect(close).ToHaveAttributeAsync("role", "button");
        await close.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(GetByTestId("toast-keyboard-close")).ToBeHiddenAsync();
    }

    [Fact]
    public virtual async Task DynamicLimitZeroMarksAllToastsInertAndFocusGuardRestoresPriorFocus()
    {
        await NavigateAsync(CreateUrl("/tests/toast"));
        await GetByTestId("add-three").ClickAsync();
        await GetByTestId("limit-zero").ClickAsync();

        await Page.WaitForFunctionAsync(
            "() => [...document.querySelectorAll('[data-blazix-base-ui-toast-root]')].every(toast => toast.hasAttribute('data-limited') && toast.hasAttribute('inert'))",
            new PageWaitForFunctionOptions { Timeout = 5000 * TimeoutMultiplier });

        await GetByTestId("before-toast").FocusAsync();
        await Page.Keyboard.PressAsync("F6");
        await Assertions.Expect(GetByTestId("toast-viewport")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(GetByTestId("before-toast")).ToBeFocusedAsync();
    }

    [Fact]
    public virtual async Task GlobalListenersRebindAfterStoreReturnsFromEmptyState()
    {
        await NavigateAsync(CreateUrl("/tests/toast"));
        await GetByTestId("add-low").ClickAsync();
        await GetByTestId("toast-close-low").ClickAsync();
        await Assertions.Expect(GetByTestId("toast-low")).ToBeHiddenAsync();

        await GetByTestId("add-high").ClickAsync();
        await Assertions.Expect(GetByTestId("toast-high")).ToBeVisibleAsync();
        await Page.WaitForFunctionAsync(
            """
            () => {
                const state = window[Symbol.for('Blazix.BaseUI.Toast.State')];
                const viewport = document.querySelector('[data-testid="toast-viewport"]');
                return state && viewport && [...state.viewports.values()].some(
                    entry => entry.viewport === viewport && entry.globalCleanups.length > 0);
            }
            """,
            new PageWaitForFunctionOptions { Timeout = 5000 * TimeoutMultiplier });
        await GetByTestId("before-toast").FocusAsync();
        await Page.Keyboard.PressAsync("F6");
        await Assertions.Expect(GetByTestId("toast-viewport")).ToBeFocusedAsync();
    }
}
