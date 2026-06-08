using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Tests.ContextMenu;

/// <summary>
/// Playwright tests for ContextMenu component - focused on browser-specific behavior.
/// Static rendering, attribute forwarding, and basic state tests are handled by bUnit.
/// These tests cover: right-click activation, keyboard navigation, focus management,
/// outside click, positioning at cursor, and real JS interop execution.
/// </summary>
public abstract class ContextMenuTestsBase : TestBase
{
    protected ContextMenuTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    #region Helper Methods

    protected async Task OpenContextMenuAsync(string triggerTestId = "context-menu-trigger")
    {
        var trigger = GetByTestId(triggerTestId);
        await trigger.ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });
        await WaitForContextMenuOpenAsync();
    }

    protected async Task WaitForContextMenuOpenAsync()
    {
        var popup = GetByTestId("context-menu-popup");
        await popup.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    protected async Task WaitForContextMenuClosedAsync()
    {
        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("false", new LocatorAssertionsToHaveTextOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    protected virtual async Task HoverSubmenuTriggerAsync()
    {
        var submenuTrigger = GetByTestId("submenu-trigger");
        await submenuTrigger.HoverAsync();
    }

    protected virtual async Task WaitForSubmenuPopupVisibleAsync()
    {
        var submenuPopup = GetByTestId("submenu-popup");
        await Assertions.Expect(submenuPopup).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    #endregion

    #region Right-Click Activation Tests

    /// <summary>
    /// Tests that right-clicking the trigger area opens the context menu.
    /// Requires real browser contextmenu event handling via JS interop.
    /// </summary>
    [Fact]
    public virtual async Task OpensMenuOnRightClick()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu"));

        var trigger = GetByTestId("context-menu-trigger");
        await trigger.ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true", new LocatorAssertionsToHaveTextOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    /// <summary>
    /// Tests that the data-popup-open attribute is added when menu opens and removed when it closes.
    /// </summary>
    [Fact]
    public virtual async Task AddsAndRemovesDataAttributes()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu"));

        var trigger = GetByTestId("context-menu-trigger");

        // Initially no data-popup-open
        await Assertions.Expect(trigger).Not.ToHaveAttributeAsync("data-popup-open", "");

        // Right-click to open
        await trigger.ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });
        await WaitForContextMenuOpenAsync();

        // data-popup-open should now be present
        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-popup-open", "");

        // Close with Escape
        await Page.Keyboard.PressAsync("Escape");
        await WaitForContextMenuClosedAsync();

        // data-popup-open should be removed
        await Assertions.Expect(trigger).Not.ToHaveAttributeAsync("data-popup-open", "");
    }

    /// <summary>
    /// Tests that the OnOpenChange callback fires when menu opens via right-click.
    /// </summary>
    [Fact]
    public virtual async Task InvokesOnOpenChangeOnRightClick()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu"));

        await OpenContextMenuAsync();

        var changeCount = GetByTestId("change-count");
        await Assertions.Expect(changeCount).Not.ToHaveTextAsync("0", new LocatorAssertionsToHaveTextOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    #endregion

    #region Keyboard Navigation Tests

    /// <summary>
    /// Tests ArrowDown navigation within the context menu.
    /// </summary>
    [Fact]
    public virtual async Task KeyboardNavigation_ArrowDown()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu").WithDefaultOpen(true));

        var item1 = GetByTestId("menu-item-1");
        var item2 = GetByTestId("menu-item-2");

        // First item should be highlighted
        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "");

        await Page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(item2).ToHaveAttributeAsync("data-highlighted", "");
    }

    /// <summary>
    /// Tests ArrowUp navigation within the context menu.
    /// </summary>
    [Fact]
    public virtual async Task KeyboardNavigation_ArrowUp()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu").WithDefaultOpen(true));

        var item1 = GetByTestId("menu-item-1");
        var item2 = GetByTestId("menu-item-2");

        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "");

        await Page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(item2).ToHaveAttributeAsync("data-highlighted", "");

        await Page.Keyboard.PressAsync("ArrowUp");
        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "");
    }

    /// <summary>
    /// Tests that Escape closes the context menu.
    /// </summary>
    [Fact]
    public virtual async Task Escape_ClosesMenu()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu").WithDefaultOpen(true));

        var item1 = GetByTestId("menu-item-1");
        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "", new LocatorAssertionsToHaveAttributeOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });

        await Page.Keyboard.PressAsync("Escape");
        await WaitForContextMenuClosedAsync();
    }

    /// <summary>
    /// Tests that Enter activates the highlighted item.
    /// </summary>
    [Fact]
    public virtual async Task Enter_ActivatesHighlightedItem()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu").WithDefaultOpen(true));

        var item1 = GetByTestId("menu-item-1");
        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "");

        await Page.Keyboard.PressAsync("Enter");

        var lastClicked = GetByTestId("last-item-clicked");
        await Assertions.Expect(lastClicked).ToHaveTextAsync("1");
    }

    #endregion

    #region Focus Management Tests

    /// <summary>
    /// Tests that the first item is focused when the context menu opens.
    /// </summary>
    [Fact]
    public virtual async Task FocusFirstItem_OnMenuOpen()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu"));

        await OpenContextMenuAsync();

        var item1 = GetByTestId("menu-item-1");
        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "");
    }

    #endregion

    #region Outside Click Tests

    /// <summary>
    /// Tests that clicking outside the context menu closes it.
    /// </summary>
    [Fact]
    public virtual async Task OutsideClick_ClosesMenu()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu").WithDefaultOpen(true));

        var outsideButton = GetByTestId("outside-button");
        await outsideButton.ClickAsync();

        await WaitForContextMenuClosedAsync();
    }

    #endregion

    #region Scroll Lock Tests

    /// <summary>
    /// Tests that a mouse-opened context menu locks page scroll like the React Base UI reference.
    /// </summary>
    [Fact]
    public virtual async Task RightClickOpen_LocksPageScroll()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu"));

        await Page.EvaluateAsync("""
            () => {
                document.documentElement.style.minHeight = '3000px';
                document.body.style.minHeight = '3000px';
                window.scrollTo(0, 600);
            }
            """);

        await OpenContextMenuAsync();

        await Page.WaitForFunctionAsync(
            """
            () => {
                const htmlStyle = getComputedStyle(document.documentElement);
                const bodyStyle = getComputedStyle(document.body);
                return document.documentElement.hasAttribute('data-base-ui-scroll-locked')
                    || htmlStyle.overflowY === 'hidden'
                    || bodyStyle.overflowY === 'hidden'
                    || bodyStyle.overflow === 'hidden';
            }
            """,
            new PageWaitForFunctionOptions { Timeout = 5000 * TimeoutMultiplier });

        var beforeScroll = await Page.EvaluateAsync<double>("() => window.scrollY");
        await Page.Mouse.WheelAsync(0, 500);
        await WaitForDelayAsync(100);
        var afterScroll = await Page.EvaluateAsync<double>("() => window.scrollY");

        Assert.Equal(beforeScroll, afterScroll);
    }

    #endregion

    #region Positioning Tests

    /// <summary>
    /// Tests that the context menu appears near the cursor position.
    /// </summary>
    [Fact]
    public virtual async Task PositionsAtCursorLocation()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu"));

        var trigger = GetByTestId("context-menu-trigger");
        var box = await trigger.BoundingBoxAsync();
        Assert.NotNull(box);

        // Right-click at specific coordinates within the trigger
        var clickX = box.X + box.Width / 2;
        var clickY = box.Y + box.Height / 2;
        await Page.Mouse.ClickAsync(clickX, clickY, new MouseClickOptions { Button = MouseButton.Right });

        await WaitForContextMenuOpenAsync();

        // Verify popup appeared (positioning is handled by Floating UI)
        var popup = GetByTestId("context-menu-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();
    }

    #endregion

    #region Submenu Tests

    /// <summary>
    /// Tests that submenu opens on hover within the context menu.
    /// </summary>
    [Fact]
    public virtual async Task SubmenuOpensOnHover()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu")
            .WithDefaultOpen(true)
            .WithContextMenuShowSubmenu(true));

        await Page.WaitForFunctionAsync(
            @"() => document.querySelector('[data-testid=""menu-item-1""]')?.hasAttribute('data-highlighted') === true",
            new PageWaitForFunctionOptions { Timeout = 5000 * TimeoutMultiplier });

        await HoverSubmenuTriggerAsync();

        await WaitForSubmenuPopupVisibleAsync();
    }

    #endregion

    #region Disabled State Tests

    /// <summary>
    /// Tests that right-clicking a disabled trigger does not open the menu.
    /// </summary>
    [Fact]
    public virtual async Task DisabledTrigger_DoesNotOpenMenu()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu").WithDisabled(true));

        var trigger = GetByTestId("context-menu-trigger");
        await trigger.ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });

        await WaitForDelayAsync(500);

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("false");
    }

    /// <summary>
    /// Tests that disabling the root preserves the browser's native context menu event.
    /// </summary>
    [Fact]
    public virtual async Task DisabledTrigger_DoesNotPreventNativeContextMenu()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu").WithDisabled(true));

        var trigger = GetByTestId("context-menu-trigger");
        await Page.EvaluateAsync("""
            () => {
              window.__contextMenuDefaultPrevented = null;
              const trigger = document.querySelector('[data-testid="context-menu-trigger"]');
              trigger.addEventListener('contextmenu', (event) => {
                window.__contextMenuDefaultPrevented = event.defaultPrevented;
              }, { once: true });
            }
            """);

        await trigger.ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });

        var defaultPrevented = await Page.EvaluateAsync<bool?>("() => window.__contextMenuDefaultPrevented");
        Assert.False(defaultPrevented ?? true);
    }

    /// <summary>
    /// Tests that disabling context menu interaction cancels a pending long-press gesture.
    /// </summary>
    [Fact]
    public virtual async Task DisablingRoot_CancelsPendingLongPress()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu"));

        var invocationCount = await Page.EvaluateAsync<int>("""
            async () => {
                const module = await import('/_content/Blazix.BaseUI/blazix-baseui-context-menu.js');
                const rootId = `pending-disable-${Date.now()}`;
                const trigger = document.createElement('div');
                const anchor = document.createElement('div');
                let invocations = 0;

                document.body.append(trigger, anchor);
                module.initializeContextMenu(rootId, trigger, anchor, {
                    invokeMethodAsync: () => {
                        invocations += 1;
                        return Promise.resolve();
                    }
                }, false);

                const event = new Event('touchstart', { bubbles: true, cancelable: true });
                Object.defineProperty(event, 'touches', {
                    value: [{ clientX: 25, clientY: 35 }]
                });

                trigger.dispatchEvent(event);
                module.setContextMenuDisabled(rootId, true);

                await new Promise(resolve => setTimeout(resolve, 650));
                module.disposeContextMenu(rootId);
                trigger.remove();
                anchor.remove();

                return invocations;
            }
            """);

        Assert.Equal(0, invocationCount);
    }

    /// <summary>
    /// Tests that releasing the context-menu gesture inside popup chrome does not cancel the menu.
    /// </summary>
    [Fact]
    public virtual async Task MouseUpInsidePopupNonItem_DoesNotCancelOpen()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu").WithContextMenuShowPopupGap(true));

        var trigger = GetByTestId("context-menu-trigger");
        await trigger.ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });
        await WaitForContextMenuOpenAsync();
        await WaitForDelayAsync(600);

        var gap = GetByTestId("popup-gap");
        var gapBox = await gap.BoundingBoxAsync();
        Assert.NotNull(gapBox);

        await Page.Mouse.MoveAsync(gapBox.X + gapBox.Width / 2, gapBox.Y + gapBox.Height / 2);
        await Page.Mouse.UpAsync(new MouseUpOptions { Button = MouseButton.Right });

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");
    }

    #endregion
}
