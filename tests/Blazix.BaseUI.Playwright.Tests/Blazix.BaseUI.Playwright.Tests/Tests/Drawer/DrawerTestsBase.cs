using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Tests.Drawer;

public abstract class DrawerTestsBase : TestBase
{
    protected DrawerTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    protected async Task OpenDrawerAsync()
    {
        await GetByTestId("drawer-trigger").ClickAsync();
        await WaitForDrawerOpenAsync();
    }

    protected async Task WaitForDrawerOpenAsync()
    {
        await Assertions.Expect(GetByTestId("open-state")).ToHaveTextAsync("true", new LocatorAssertionsToHaveTextOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
        await Assertions.Expect(GetByTestId("drawer-popup")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
        await Assertions.Expect(GetByTestId("drawer-popup")).Not.ToHaveAttributeAsync("data-starting-style", string.Empty, new LocatorAssertionsToHaveAttributeOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    protected async Task WaitForDrawerClosedAsync()
    {
        await Assertions.Expect(GetByTestId("open-state")).ToHaveTextAsync("false", new LocatorAssertionsToHaveTextOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    [Fact]
    public async Task TriggerClickTogglesDrawer()
    {
        await NavigateAsync(CreateUrl("/tests/drawer").WithShowBackdrop(false));

        await OpenDrawerAsync();
        await GetByTestId("drawer-trigger").ClickAsync();

        await WaitForDrawerClosedAsync();
    }

    [Fact]
    public async Task EscapeClosesDrawer()
    {
        await NavigateAsync(CreateUrl("/tests/drawer").WithDefaultOpen(true));

        await WaitForDrawerOpenAsync();
        await GetByTestId("drawer-popup").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");

        await WaitForDrawerClosedAsync();
        await Assertions.Expect(GetByTestId("last-reason")).ToHaveTextAsync("EscapeKey");
    }

    [Fact]
    public async Task BackdropClickClosesModalDrawer()
    {
        await NavigateAsync(CreateUrl("/tests/drawer").WithShowBackdrop(true));

        await OpenDrawerAsync();
        await Page.Mouse.ClickAsync(20, 20);

        await WaitForDrawerClosedAsync();
        await Assertions.Expect(GetByTestId("last-reason")).ToHaveTextAsync("OutsidePress");
    }

    [Fact]
    public async Task SwipeAreaDragOpensDrawer()
    {
        await NavigateAsync(CreateUrl("/tests/drawer"));

        var swipeArea = GetByTestId("drawer-swipe-area");
        var box = await swipeArea.BoundingBoxAsync();
        Assert.NotNull(box);

        var startX = box!.X + box.Width / 2;
        var startY = box.Y + box.Height / 2;
        await Page.Mouse.MoveAsync(startX, startY);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(startX, startY - 180, new MouseMoveOptions { Steps = 8 });
        await Page.Mouse.UpAsync();

        await WaitForDrawerOpenAsync();
        await Assertions.Expect(GetByTestId("last-reason")).ToHaveTextAsync("Swipe");
    }

    [Fact]
    public async Task DisabledSwipeAreaDoesNotOpenDrawer()
    {
        await NavigateAsync(CreateUrl("/tests/drawer").WithSwipeAreaDisabled(true));

        var swipeArea = GetByTestId("drawer-swipe-area");
        var box = await swipeArea.BoundingBoxAsync();
        Assert.NotNull(box);

        var startX = box!.X + box.Width / 2;
        var startY = box.Y + box.Height / 2;
        await Page.Mouse.MoveAsync(startX, startY);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(startX, startY - 180, new MouseMoveOptions { Steps = 8 });
        await Page.Mouse.UpAsync();

        await Assertions.Expect(GetByTestId("open-state")).ToHaveTextAsync("false");
    }

    [Fact]
    public async Task PopupSwipeDismissClosesDrawer()
    {
        await NavigateAsync(CreateUrl("/tests/drawer").WithDefaultOpen(true));

        await WaitForDrawerOpenAsync();
        var popup = GetByTestId("drawer-popup");
        var box = await popup.BoundingBoxAsync();
        Assert.NotNull(box);

        var startX = box!.X + box.Width / 2;
        var startY = box.Y + 40;
        await Page.Mouse.MoveAsync(startX, startY);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(startX, startY + 260, new MouseMoveOptions { Steps = 10 });
        await Page.Mouse.UpAsync();

        await WaitForDrawerClosedAsync();
        await Assertions.Expect(GetByTestId("last-reason")).ToHaveTextAsync("Swipe");
    }

    [Fact]
    public async Task SnapPointDragUpdatesActiveSnapPoint()
    {
        await NavigateAsync(CreateUrl("/tests/drawer")
            .WithDefaultOpen(true)
            .WithSnapPoints(true)
            .WithSnapToSequentialPoints(true));

        await WaitForDrawerOpenAsync();
        await Assertions.Expect(GetByTestId("snap-point")).ToHaveTextAsync("0.35");

        var popup = GetByTestId("drawer-popup");
        var box = await popup.BoundingBoxAsync();
        Assert.NotNull(box);

        var startX = (double)box!.X + (double)box.Width / 2;
        var startY = (double)box.Y + 40;
        var body = Page.Locator("body");
        await popup.DispatchEventAsync("pointerdown", new
        {
            button = 0,
            buttons = 1,
            clientX = startX,
            clientY = startY,
            pointerId = 1,
            pointerType = "mouse"
        });
        await body.DispatchEventAsync("pointermove", new
        {
            buttons = 1,
            clientX = startX,
            clientY = startY - 220,
            pointerId = 1,
            pointerType = "mouse"
        });
        await body.DispatchEventAsync("pointerup", new
        {
            button = 0,
            clientX = startX,
            clientY = startY - 220,
            pointerId = 1,
            pointerType = "mouse"
        });

        await Assertions.Expect(GetByTestId("snap-point")).ToHaveTextAsync("0.75", new LocatorAssertionsToHaveTextOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
        await Assertions.Expect(GetByTestId("snap-change-count")).Not.ToHaveTextAsync("0");
    }

    [Fact]
    public async Task InitialFocusMovesToCloseButton()
    {
        await NavigateAsync(CreateUrl("/tests/drawer").WithUseInitialFocus(true));

        await OpenDrawerAsync();

        await Assertions.Expect(GetByTestId("drawer-close")).ToBeFocusedAsync(new LocatorAssertionsToBeFocusedOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
    }
}
