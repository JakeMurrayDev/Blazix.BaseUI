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

    #region Native Context Menu Suppression Tests

    /// <summary>
    /// Waits until the context-menu JS module reflects an open menu with a registered,
    /// connected positioner. The open-state and positioner registration are two independent
    /// async interops; under WASM either can lag the open render, so the suppression tests
    /// wait for both before asserting (in real usage a second right-click occurs long after).
    /// </summary>
    private async Task WaitForContextMenuJsSyncedOpenAsync()
    {
        await Page.WaitForFunctionAsync("""
            () => {
                const state = window[Symbol.for('Blazix.BaseUI.ContextMenu.State')];
                if (!state) { return false; }
                for (const root of state.roots.values()) {
                    if (root.isOpen && root.positionerElement && root.positionerElement.isConnected) {
                        return true;
                    }
                }
                return false;
            }
            """, new PageWaitForFunctionOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    /// <summary>
    /// While a modal context menu is open, the native browser context menu must be
    /// suppressed everywhere except over the popup — replicating React's no-cutout modal
    /// internal backdrop (ContextMenuRoot.internalBackdropRef).
    /// </summary>
    [Fact]
    public virtual async Task NativeContextMenuSuppressed_OverModalRegion_WhileOpen()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu").WithDefaultOpen(true));

        var item1 = GetByTestId("menu-item-1");
        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "", new LocatorAssertionsToHaveAttributeOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });

        await WaitForContextMenuJsSyncedOpenAsync();

        // Dispatch a contextmenu on an element outside the popup. The component's document
        // handler must cancel it (suppressing the native menu over the modal region). Driving
        // the handler directly removes the real-right-click layout/coverage non-determinism.
        var prevented = await Page.EvaluateAsync<bool>("""
            () => {
                const outside = document.querySelector('[data-testid="outside-button"]');
                const ev = new MouseEvent('contextmenu', { bubbles: true, cancelable: true });
                outside.dispatchEvent(ev);
                return ev.defaultPrevented;
            }
            """);
        Assert.True(prevented);
    }

    /// <summary>
    /// The native browser context menu must NOT be suppressed when right-clicking the popup
    /// itself (React leaves the popup region outside the internal backdrop).
    /// </summary>
    [Fact]
    public virtual async Task NativeContextMenuNotSuppressed_OverPopup_WhileOpen()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu").WithDefaultOpen(true));

        var item1 = GetByTestId("menu-item-1");
        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "", new LocatorAssertionsToHaveAttributeOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });

        await WaitForContextMenuJsSyncedOpenAsync();

        // Dispatch a contextmenu on the popup itself. The component's document handler must
        // NOT cancel it (React leaves the popup region outside the modal internal backdrop).
        var prevented = await Page.EvaluateAsync<bool>("""
            () => {
                const item = document.querySelector('[data-testid="menu-item-1"]');
                const ev = new MouseEvent('contextmenu', { bubbles: true, cancelable: true });
                item.dispatchEvent(ev);
                return ev.defaultPrevented;
            }
            """);
        Assert.False(prevented);
    }

    /// <summary>
    /// If the app cancels a context-menu open, the JS-side open mirror must be reset to
    /// false. The trigger marks it open synchronously to suppress Server outside-press
    /// races, but a canceled open has no normal Blazor open-state echo to clear it.
    /// </summary>
    [Fact]
    public virtual async Task CanceledOpen_ClearsContextMenuJsOpenMirror()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu").WithContextMenuCancelOpen(true));

        var trigger = GetByTestId("context-menu-trigger");
        await trigger.ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("false", new LocatorAssertionsToHaveTextOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });

        var jsOpen = await Page.EvaluateAsync<bool>("""
            () => {
                const state = window[Symbol.for('Blazix.BaseUI.ContextMenu.State')];
                return Array.from(state?.roots.values() ?? []).some(root => root.isOpen);
            }
            """);
        Assert.False(jsOpen);
    }

    /// <summary>
    /// Click-drag-release item activation must require a right-button (button 2) release,
    /// matching React's useMenuItemCommonProps gate: macOS activates on button 2 only; a
    /// left-button release must never activate via the positioner mouseup path on any platform.
    /// </summary>
    [Fact]
    public virtual async Task ClickDragRelease_RequiresRightButton_ForActivation()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu"));

        var result = await Page.EvaluateAsync<string>("""
            async () => {
                const module = await import('/_content/Blazix.BaseUI/blazix-baseui-context-menu.js');
                const rootId = `btn-gate-${Date.now()}`;
                const isMac = /mac/i.test(navigator.userAgent);

                const trigger = document.createElement('div');
                const anchor = document.createElement('div');
                const positioner = document.createElement('div');
                const item = document.createElement('div');
                item.setAttribute('role', 'menuitem');
                positioner.appendChild(item);
                document.body.append(trigger, anchor, positioner);

                let clicks = 0;
                item.addEventListener('click', () => { clicks += 1; });

                module.initializeContextMenu(rootId, trigger, anchor, { invokeMethodAsync: () => Promise.resolve() }, false);
                module.setPositionerElement(rootId, positioner);

                async function gesture(button) {
                    clicks = 0;
                    // Right-click opens: arms allowMouseUp after the 500ms long-press delay.
                    trigger.dispatchEvent(new MouseEvent('contextmenu', {
                        bubbles: true, cancelable: true, clientX: 10, clientY: 10, button: 2
                    }));
                    await new Promise(r => setTimeout(r, 600));
                    // Release over the item, moved >1px from the initial point, with the given button.
                    item.dispatchEvent(new MouseEvent('mouseup', {
                        bubbles: true, cancelable: true, clientX: 200, clientY: 200, button
                    }));
                    await new Promise(r => setTimeout(r, 0));
                    return clicks;
                }

                const rightClicks = await gesture(2);
                const leftClicks = await gesture(0);

                module.disposeContextMenu(rootId);
                trigger.remove(); anchor.remove(); positioner.remove();

                return JSON.stringify({ isMac, rightClicks, leftClicks });
            }
            """);

        var data = System.Text.Json.JsonDocument.Parse(result).RootElement;
        var isMac = data.GetProperty("isMac").GetBoolean();
        var rightClicks = data.GetProperty("rightClicks").GetInt32();
        var leftClicks = data.GetProperty("leftClicks").GetInt32();

        // A left-button release must NEVER activate the item (the positive button===2 gate).
        Assert.Equal(0, leftClicks);

        if (isMac)
        {
            // On macOS, a right-button drag-release activates the item.
            Assert.Equal(1, rightClicks);
        }
        else
        {
            // On non-macOS, the right-button release belongs to the opening gesture and must not activate.
            Assert.Equal(0, rightClicks);
        }
    }

    #endregion

    #region Reposition Tests

    /// <summary>
    /// A second right-click inside the trigger (clear of the popup) must reposition the open
    /// menu to the new cursor and keep it open — matching React Base UI, where the menu
    /// follows the cursor rather than dismissing.
    /// </summary>
    [Fact]
    public virtual async Task SecondRightClickInTrigger_RepositionsMenuAndStaysOpen()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu"));

        var trigger = GetByTestId("context-menu-trigger");
        var box = await trigger.BoundingBoxAsync();
        Assert.NotNull(box);

        // Open at point A (upper-left region of the trigger).
        var ax = box.X + box.Width * 0.3f;
        var ay = box.Y + box.Height * 0.3f;
        await Page.Mouse.ClickAsync(ax, ay, new MouseClickOptions { Button = MouseButton.Right });
        await WaitForContextMenuOpenAsync();

        var popup = GetByTestId("context-menu-popup");
        var rect1 = await popup.BoundingBoxAsync();
        Assert.NotNull(rect1);

        // Past the 500ms grace, right-click point B in the trigger's top-right (clear of the
        // down-right-opening popup).
        await WaitForDelayAsync(600);
        var bx = box.X + box.Width - 12;
        var by = box.Y + 10;
        await Page.Mouse.ClickAsync(bx, by, new MouseClickOptions { Button = MouseButton.Right });

        // Menu must stay open (React parity — no dismiss).
        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true", new LocatorAssertionsToHaveTextOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });

        // ... and the popup must move toward B.
        await WaitForDelayAsync(200);
        var rect2 = await popup.BoundingBoxAsync();
        Assert.NotNull(rect2);
        Assert.True(
            Math.Abs(rect2.X - rect1.X) > 20 || Math.Abs(rect2.Y - rect1.Y) > 20,
            $"Expected popup to reposition; was ({rect1.X},{rect1.Y}), now ({rect2.X},{rect2.Y}).");
    }

    /// <summary>
    /// An immediate second right-click (no delay after opening) must NOT close the menu.
    /// Exercises the synchronous open-state guard: a fast double right-click must not outrun
    /// the dismiss-suppression (which previously depended on an async open-state push).
    /// </summary>
    [Fact]
    public virtual async Task ImmediateSecondRightClickInTrigger_DoesNotCloseMenu()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu"));

        var trigger = GetByTestId("context-menu-trigger");
        var box = await trigger.BoundingBoxAsync();
        Assert.NotNull(box);

        // Open at A, then immediately (no wait) right-click B in the trigger's top-right.
        await Page.Mouse.ClickAsync(box.X + box.Width * 0.3f, box.Y + box.Height * 0.3f, new MouseClickOptions { Button = MouseButton.Right });
        await Page.Mouse.ClickAsync(box.X + box.Width - 12, box.Y + 10, new MouseClickOptions { Button = MouseButton.Right });

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true", new LocatorAssertionsToHaveTextOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    /// <summary>
    /// A second right-click on the trigger that is HELD past the long-press delay (&gt;500ms)
    /// must not dismiss the menu. Regression for a Server-only close: the held release fired
    /// the click-drag-release cancel, whose dismiss round-trip beat the reposition over SignalR.
    /// </summary>
    [Fact]
    public virtual async Task HeldSecondRightClickInTrigger_DoesNotCloseMenu()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu"));

        var trigger = GetByTestId("context-menu-trigger");
        var box = await trigger.BoundingBoxAsync();
        Assert.NotNull(box);

        var x = box.X + box.Width * 0.4f;
        var y = box.Y + box.Height * 0.4f;

        await Page.Mouse.ClickAsync(x, y, new MouseClickOptions { Button = MouseButton.Right });
        await WaitForContextMenuOpenAsync();

        // Second right-click at the same spot, held past the 500ms long-press delay.
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync(new MouseDownOptions { Button = MouseButton.Right });
        await WaitForDelayAsync(700);
        await Page.Mouse.UpAsync(new MouseUpOptions { Button = MouseButton.Right });

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true", new LocatorAssertionsToHaveTextOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    /// <summary>
    /// Repeated right-clicks at the exact same trigger coordinate must keep the context
    /// menu open every time. Regression for the Server race where a visible-trigger
    /// right-click was first treated as a shared Menu outside press, so the close from
    /// pointerdown could win over the contextmenu reopen on alternating clicks.
    /// </summary>
    [Fact]
    public virtual async Task RepeatedRightClicksAtSameTriggerPoint_AlwaysLeaveMenuOpen()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu"));

        var trigger = GetByTestId("context-menu-trigger");
        var box = await trigger.BoundingBoxAsync();
        Assert.NotNull(box);

        var x = box.X + box.Width * 0.35f;
        var y = box.Y + box.Height * 0.35f;
        var openState = GetByTestId("open-state");

        for (var i = 0; i < 4; i++)
        {
            await Page.Mouse.ClickAsync(x, y, new MouseClickOptions { Button = MouseButton.Right });
            await Assertions.Expect(openState).ToHaveTextAsync("true", new LocatorAssertionsToHaveTextOptions
            {
                Timeout = 5000 * TimeoutMultiplier
            });
            await WaitForDelayAsync(150);
        }
    }

    /// <summary>
    /// A right-click OUTSIDE the trigger while open must dismiss the menu (React parity).
    /// </summary>
    [Fact]
    public virtual async Task RightClickOutsideTrigger_ClosesMenu()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu").WithDefaultOpen(true));

        var item1 = GetByTestId("menu-item-1");
        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "", new LocatorAssertionsToHaveAttributeOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });

        var outsideButton = GetByTestId("outside-button");
        await outsideButton.ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });

        await WaitForContextMenuClosedAsync();
    }

    #endregion
}
