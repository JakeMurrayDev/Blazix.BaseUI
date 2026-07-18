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
    public async Task SwipeBelowThresholdDoesNotDismissDrawer()
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
        await Page.Mouse.MoveAsync(startX, startY + 60, new MouseMoveOptions { Steps = 6 });
        await Page.WaitForTimeoutAsync(200);
        await Page.Mouse.UpAsync();

        await Assertions.Expect(GetByTestId("open-state")).ToHaveTextAsync("true");
        await Assertions.Expect(popup).Not.ToHaveAttributeAsync("data-swipe-dismiss", string.Empty);
    }

    [Fact]
    public async Task FastShortSwipeDismissesDrawerByVelocity()
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
        await Page.Mouse.MoveAsync(startX, startY + 1);
        await Page.Mouse.MoveAsync(startX, startY + 120);
        await Page.Mouse.UpAsync();

        await WaitForDrawerClosedAsync();
    }

    [Fact]
    public async Task ReverseSwipeCancelsDismissal()
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
        await Page.Mouse.MoveAsync(startX, startY + 240, new MouseMoveOptions { Steps = 8 });
        await Page.Mouse.MoveAsync(startX, startY + 4, new MouseMoveOptions { Steps = 8 });
        await Page.Mouse.UpAsync();

        await Assertions.Expect(GetByTestId("open-state")).ToHaveTextAsync("true");
        await Assertions.Expect(popup).Not.ToHaveAttributeAsync("data-swipe-dismiss", string.Empty);
    }

    [Fact]
    public async Task ControlledSwipeDismissRejectionRestoresPopup()
    {
        await NavigateAsync(CreateUrl("/tests/drawer")
            .WithDefaultOpen(true)
            .WithRejectSwipeDismiss(true));
        await WaitForDrawerOpenAsync();

        var popup = GetByTestId("drawer-popup");
        var box = await popup.BoundingBoxAsync();
        Assert.NotNull(box);

        var startX = box!.X + box.Width / 2;
        var startY = box.Y + 40;
        await Page.Mouse.MoveAsync(startX, startY);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(startX, startY + 260, new MouseMoveOptions { Steps = 8 });
        await Page.Mouse.UpAsync();

        await Assertions.Expect(GetByTestId("open-state")).ToHaveTextAsync("true");
        await Assertions.Expect(popup).Not.ToHaveAttributeAsync("data-swipe-dismiss", string.Empty);
        await Assertions.Expect(popup).Not.ToHaveAttributeAsync("data-ending-style", string.Empty);
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

        var startX = box!.X + box.Width / 2;
        var startY = box.Y + 40;
        await Page.Mouse.MoveAsync(startX, startY);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(startX, startY - 220, new MouseMoveOptions { Steps = 10 });
        await Assertions.Expect(popup).ToHaveAttributeAsync("data-swiping", string.Empty, new LocatorAssertionsToHaveAttributeOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
        await Page.Mouse.UpAsync();

        await Assertions.Expect(GetByTestId("snap-point")).ToHaveTextAsync("0.75", new LocatorAssertionsToHaveTextOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
        await Assertions.Expect(GetByTestId("snap-change-count")).Not.ToHaveTextAsync("0");
    }

    [Fact]
    public async Task SnapPointOvershootUsesSquareRootDamping()
    {
        await NavigateAsync(CreateUrl("/tests/drawer")
            .WithDefaultOpen(true)
            .WithSnapPoints(true));
        await WaitForDrawerOpenAsync();

        var popup = GetByTestId("drawer-popup");
        var box = await popup.BoundingBoxAsync();
        Assert.NotNull(box);
        var snapPointOffset = await popup.EvaluateAsync<double>(
            "element => parseFloat(getComputedStyle(element).getPropertyValue('--drawer-snap-point-offset'))");
        var titleBox = await GetByTestId("drawer-title").BoundingBoxAsync();
        Assert.NotNull(titleBox);

        var startX = titleBox!.X + titleBox.Width / 2;
        var startY = titleBox.Y + titleBox.Height / 2;
        await Page.Mouse.MoveAsync(startX, startY);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(startX, startY - 500, new MouseMoveOptions { Steps = 10 });

        var movement = await popup.EvaluateAsync<string>(
            "element => element.style.getPropertyValue('--drawer-swipe-movement-y')");
        await Page.Mouse.UpAsync();

        Assert.EndsWith("px", movement);
        var movementValue = double.Parse(movement[..^2], System.Globalization.CultureInfo.InvariantCulture);
        Assert.True(movementValue < -snapPointOffset);
        Assert.True(movementValue > -500);
    }

    [Fact]
    public async Task SwipeAreaCanRegrabDuringDismissTransition()
    {
        await NavigateAsync(CreateUrl("/tests/drawer").WithDefaultOpen(true));
        await WaitForDrawerOpenAsync();

        var popup = GetByTestId("drawer-popup");
        var popupBox = await popup.BoundingBoxAsync();
        Assert.NotNull(popupBox);
        var popupX = popupBox!.X + popupBox.Width / 2;
        var popupY = popupBox.Y + 40;
        await Page.Mouse.MoveAsync(popupX, popupY);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(popupX, popupY + 260, new MouseMoveOptions { Steps = 8 });
        await Page.Mouse.UpAsync();
        await WaitForDrawerClosedAsync();

        var swipeArea = GetByTestId("drawer-swipe-area");
        var areaBox = await swipeArea.BoundingBoxAsync();
        Assert.NotNull(areaBox);
        var areaX = areaBox!.X + areaBox.Width / 2;
        var areaY = areaBox.Y + areaBox.Height / 2;
        await Page.Mouse.MoveAsync(areaX, areaY);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(areaX, areaY - 180, new MouseMoveOptions { Steps = 8 });
        await Page.Mouse.UpAsync();

        await WaitForDrawerOpenAsync();
    }

    [Fact]
    public async Task VirtualKeyboardProviderSetsViewportInsetVariable()
    {
        await NavigateAsync(CreateUrl("/tests/drawer").WithDefaultOpen(true));
        await WaitForDrawerOpenAsync();

        await GetByTestId("drawer-input").FocusAsync();
        var viewport = GetByTestId("drawer-viewport");

        await Assertions.Expect(viewport).ToHaveCSSAsync("--drawer-keyboard-inset", "0px");
    }

    [Fact]
    public async Task CompositeKeysDoNotBubblePastPopup()
    {
        await NavigateAsync(CreateUrl("/tests/drawer").WithDefaultOpen(true));
        await WaitForDrawerOpenAsync();

        await GetByTestId("drawer-popup").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");

        await Assertions.Expect(GetByTestId("composite-key-count")).ToHaveTextAsync("0");
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
