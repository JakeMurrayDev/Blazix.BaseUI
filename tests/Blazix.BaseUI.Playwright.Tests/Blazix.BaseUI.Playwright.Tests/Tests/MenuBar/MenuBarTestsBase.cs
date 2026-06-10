using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Tests.MenuBar;

public abstract class MenuBarTestsBase : TestBase
{
    private const string ScrollLockedScript = @"() => {
        const doc = document.documentElement;
        const body = document.body;
        const docOverflow = getComputedStyle(doc).overflow;
        const bodyOverflow = getComputedStyle(body).overflow;
        return doc.hasAttribute('data-base-ui-scroll-locked') ||
               body.hasAttribute('data-base-ui-scroll-locked') ||
               docOverflow === 'hidden' ||
               bodyOverflow === 'hidden';
    }";

    protected MenuBarTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    #region Helper Methods

    protected async Task OpenMenu1Async()
    {
        var trigger = GetByTestId("menu-1-trigger");
        await trigger.ClickAsync();
        await WaitForMenu1OpenAsync();
    }

    protected async Task WaitForMenu1OpenAsync()
    {
        var popup = GetByTestId("menu-1-popup");
        await popup.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    protected async Task WaitForTextContentAsync(ILocator element, string expectedText, int timeout = 5000)
    {
        var effectiveTimeout = timeout * TimeoutMultiplier;
        await Assertions.Expect(element).ToHaveTextAsync(expectedText, new LocatorAssertionsToHaveTextOptions
        {
            Timeout = (float)effectiveTimeout
        });
    }

    protected async Task TouchOpenAsync(ILocator trigger)
    {
        await trigger.DispatchEventAsync("pointerdown", new Dictionary<string, object>
        {
            ["pointerType"] = "touch",
            ["button"] = 0,
            ["buttons"] = 1,
            ["isPrimary"] = true
        });
        await trigger.DispatchEventAsync("mousedown", new Dictionary<string, object>
        {
            ["button"] = 0,
            ["buttons"] = 1
        });
    }

    protected async Task WaitForScrollLockedAsync()
    {
        await Page.WaitForFunctionAsync(ScrollLockedScript, null, new PageWaitForFunctionOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    protected async Task AssertScrollUnlockedAsync()
    {
        await Page.WaitForFunctionAsync($@"() => {{
            const isLocked = ({ScrollLockedScript})();
            return !isLocked;
        }}", null, new PageWaitForFunctionOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    #endregion

    #region Interaction Tests

    [Fact]
    public virtual async Task TracksHasSubmenuOpenState()
    {
        await NavigateAsync(CreateUrl("/tests/menubar"));

        var root = GetByTestId("menubar-root");

        // Initially no submenu is open - attribute should be absent
        await Assertions.Expect(root).Not.ToHaveAttributeAsync("data-has-submenu-open", "");

        // Open a menu
        await OpenMenu1Async();

        // data-has-submenu-open should be present as standalone attribute
        await Assertions.Expect(root).ToHaveAttributeAsync("data-has-submenu-open", "");
    }

    [Fact]
    public virtual async Task ArrowRight_NavigatesToNextMenubarItem()
    {
        await NavigateAsync(CreateUrl("/tests/menubar"));

        var trigger1 = GetByTestId("menu-1-trigger");

        // Focus and click on first trigger
        await trigger1.ClickAsync();
        await WaitForMenu1OpenAsync();

        // Press right to navigate to next menu
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(300);

        // Second menu should now be open
        var menu2State = GetByTestId("menu-2-state");
        await Assertions.Expect(menu2State).ToHaveTextAsync("true");
    }

    [Fact]
    public virtual async Task ArrowLeft_NavigatesToPreviousMenubarItem()
    {
        await NavigateAsync(CreateUrl("/tests/menubar"));

        var trigger2 = GetByTestId("menu-2-trigger");

        // Click on second trigger
        await trigger2.ClickAsync();
        var popup2 = GetByTestId("menu-2-popup");
        await popup2.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5000 * TimeoutMultiplier
        });

        // Press left to navigate to previous menu
        await Page.Keyboard.PressAsync("ArrowLeft");
        await WaitForDelayAsync(300);

        // First menu should now be open
        var menu1State = GetByTestId("menu-1-state");
        await Assertions.Expect(menu1State).ToHaveTextAsync("true");

        var changeCount = GetByTestId("change-count");
        await Assertions.Expect(changeCount).ToHaveTextAsync("1");
    }

    [Fact]
    public virtual async Task HoverOpensSibling_WhenOneIsOpen()
    {
        await NavigateAsync(CreateUrl("/tests/menubar"));

        // Click to open first menu
        await OpenMenu1Async();

        // Hover over second trigger
        var trigger2 = GetByTestId("menu-2-trigger");
        await trigger2.HoverAsync();
        await WaitForDelayAsync(300);

        // Second menu should open
        var menu2State = GetByTestId("menu-2-state");
        await Assertions.Expect(menu2State).ToHaveTextAsync("true");

        // First menu should close
        var menu1State = GetByTestId("menu-1-state");
        await Assertions.Expect(menu1State).ToHaveTextAsync("false");
    }

    [Fact]
    public virtual async Task HoverDoesNotOpen_WhenNoneOpen()
    {
        await NavigateAsync(CreateUrl("/tests/menubar"));

        // Hover over first trigger without clicking
        var trigger1 = GetByTestId("menu-1-trigger");
        await trigger1.HoverAsync();
        await WaitForDelayAsync(300);

        // Menu should NOT open
        var menu1State = GetByTestId("menu-1-state");
        await Assertions.Expect(menu1State).ToHaveTextAsync("false");
    }

    [Fact]
    public virtual async Task MenuBar_ClickTrigger_OpensMenu()
    {
        await NavigateAsync(CreateUrl("/tests/menubar"));

        var trigger1 = GetByTestId("menu-1-trigger");
        await trigger1.ClickAsync();

        await WaitForMenu1OpenAsync();

        var menu1State = GetByTestId("menu-1-state");
        await Assertions.Expect(menu1State).ToHaveTextAsync("true");

        var changeCount = GetByTestId("change-count");
        await Assertions.Expect(changeCount).ToHaveTextAsync("1");
    }

    [Fact]
    public virtual async Task MenuBar_ClickItem_ClosesMenu()
    {
        await NavigateAsync(CreateUrl("/tests/menubar"));

        await OpenMenu1Async();

        var item1 = GetByTestId("menu-1-item-1");
        await item1.ClickAsync();

        // Wait for menu to close with explicit timeout for WASM compatibility
        var menu1State = GetByTestId("menu-1-state");
        await Assertions.Expect(menu1State).ToHaveTextAsync("false", new LocatorAssertionsToHaveTextOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });

        // Wait for click handler to execute with explicit timeout for WASM compatibility
        var lastClicked = GetByTestId("last-item-clicked");
        await Assertions.Expect(lastClicked).ToHaveTextAsync("Menu1-Item1", new LocatorAssertionsToHaveTextOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    [Fact]
    public virtual async Task MenuBar_Escape_ClosesMenu()
    {
        await NavigateAsync(CreateUrl("/tests/menubar"));

        await OpenMenu1Async();

        await Page.Keyboard.PressAsync("Escape");
        await WaitForDelayAsync(300);

        var menu1State = GetByTestId("menu-1-state");
        await Assertions.Expect(menu1State).ToHaveTextAsync("false");
    }

    [Fact]
    public virtual async Task MenuBar_ClickOutside_ClosesMenu()
    {
        await NavigateAsync(CreateUrl("/tests/menubar"));

        await OpenMenu1Async();

        var outsideButton = GetByTestId("outside-button");
        await outsideButton.ClickAsync();

        await WaitForDelayAsync(300);

        var menu1State = GetByTestId("menu-1-state");
        await Assertions.Expect(menu1State).ToHaveTextAsync("false");
    }

    [Fact]
    public virtual async Task MenuBar_ArrowDown_NavigatesInMenu()
    {
        await NavigateAsync(CreateUrl("/tests/menubar"));

        await OpenMenu1Async();

        var item1 = GetByTestId("menu-1-item-1");
        var item2 = GetByTestId("menu-1-item-2");

        // Press ArrowDown to highlight first item (menu opens with no item highlighted)
        await Page.Keyboard.PressAsync("ArrowDown");
        await WaitForDelayAsync(100);

        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "");

        // Press ArrowDown again to navigate to second item
        await Page.Keyboard.PressAsync("ArrowDown");
        await WaitForDelayAsync(100);

        await Assertions.Expect(item2).ToHaveAttributeAsync("data-highlighted", "");
    }

    [Fact]
    public virtual async Task MenuBar_LoopFocus_WrapAround()
    {
        await NavigateAsync(CreateUrl("/tests/menubar").WithLoopFocus(true));

        var trigger1 = GetByTestId("menu-1-trigger");
        var trigger3 = GetByTestId("menu-3-trigger");

        await trigger3.ClickAsync();
        var popup3 = GetByTestId("menu-3-popup");
        await popup3.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5000 * TimeoutMultiplier
        });

        // Navigate right from last menu (should wrap to first)
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(300);

        var menu1State = GetByTestId("menu-1-state");
        await Assertions.Expect(menu1State).ToHaveTextAsync("true");
    }

    [Fact]
    public virtual async Task MenuBar_Disabled_PreventsInteraction()
    {
        await NavigateAsync(CreateUrl("/tests/menubar").WithDisabled(true));

        var trigger1 = GetByTestId("menu-1-trigger");
        await trigger1.ClickAsync(new LocatorClickOptions { Force = true });
        await WaitForDelayAsync(300);

        var menu1State = GetByTestId("menu-1-state");
        await Assertions.Expect(menu1State).ToHaveTextAsync("false");
    }

    [Fact]
    public virtual async Task MenuBar_ClickTriggerAgain_ClosesMenu()
    {
        await NavigateAsync(CreateUrl("/tests/menubar"));

        var trigger1 = GetByTestId("menu-1-trigger");

        // First click opens menu
        await trigger1.ClickAsync();
        await WaitForMenu1OpenAsync();

        var menu1State = GetByTestId("menu-1-state");
        await Assertions.Expect(menu1State).ToHaveTextAsync("true");

        // Second click closes menu
        await trigger1.ClickAsync();
        await WaitForDelayAsync(300);

        await Assertions.Expect(menu1State).ToHaveTextAsync("false");
    }

    [Fact]
    public virtual async Task MenuBar_TabFocus_DoesNotOpenMenu()
    {
        await NavigateAsync(CreateUrl("/tests/menubar"));

        // Tab to focus on first trigger
        await Page.Keyboard.PressAsync("Tab");
        await WaitForDelayAsync(100);

        // Menu should NOT be open
        var menu1State = GetByTestId("menu-1-state");
        await Assertions.Expect(menu1State).ToHaveTextAsync("false");

        // Trigger should be focused
        var trigger1 = GetByTestId("menu-1-trigger");
        await Assertions.Expect(trigger1).ToBeFocusedAsync();
    }

    [Fact]
    public virtual async Task MenuBar_SpaceKey_OpensMenu()
    {
        await NavigateAsync(CreateUrl("/tests/menubar"));

        var trigger1 = GetByTestId("menu-1-trigger");
        await trigger1.FocusAsync();
        await WaitForDelayAsync(100);

        // Press Space to open menu
        await Page.Keyboard.PressAsync("Space");
        await WaitForDelayAsync(300);

        var menu1State = GetByTestId("menu-1-state");
        await Assertions.Expect(menu1State).ToHaveTextAsync("true");
    }

    [Fact]
    public virtual async Task MenuBar_EnterKey_OpensMenu()
    {
        await NavigateAsync(CreateUrl("/tests/menubar"));

        var trigger1 = GetByTestId("menu-1-trigger");
        await trigger1.FocusAsync();
        await WaitForDelayAsync(100);

        // Press Enter to open menu
        await Page.Keyboard.PressAsync("Enter");
        await WaitForDelayAsync(300);

        var menu1State = GetByTestId("menu-1-state");
        await Assertions.Expect(menu1State).ToHaveTextAsync("true");
    }

    [Fact]
    public virtual async Task MenuBar_LoopFocusTrue_WrapsFromFirstToLast()
    {
        await NavigateAsync(CreateUrl("/tests/menubar").WithLoopFocus(true));

        var trigger1 = GetByTestId("menu-1-trigger");

        await trigger1.ClickAsync();
        await WaitForMenu1OpenAsync();

        // Navigate left from first menu (should wrap to last)
        await Page.Keyboard.PressAsync("ArrowLeft");
        await WaitForDelayAsync(300);

        var menu3State = GetByTestId("menu-3-state");
        await Assertions.Expect(menu3State).ToHaveTextAsync("true");
    }

    [Fact]
    public virtual async Task MenuBar_LoopFocusFalse_StaysOnLastItem()
    {
        await NavigateAsync(CreateUrl("/tests/menubar").WithLoopFocus(false));

        var trigger1 = GetByTestId("menu-1-trigger");
        var trigger3 = GetByTestId("menu-3-trigger");

        // Focus first trigger
        await trigger1.FocusAsync();
        await WaitForDelayAsync(100);

        // Navigate right twice to get to last trigger (trigger3)
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(100);
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(100);

        await Assertions.Expect(trigger3).ToBeFocusedAsync();

        // Try to navigate right again (should stay on last)
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(100);

        // Focus should still be on last trigger
        await Assertions.Expect(trigger3).ToBeFocusedAsync();
    }

    [Fact]
    public virtual async Task MenuBar_LoopFocusFalse_DoesNotWrapOpenLastMenu()
    {
        await NavigateAsync(CreateUrl("/tests/menubar").WithLoopFocus(false));

        var trigger3 = GetByTestId("menu-3-trigger");
        await trigger3.ClickAsync();

        var popup3 = GetByTestId("menu-3-popup");
        await popup3.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5000 * TimeoutMultiplier
        });

        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(300);

        await Assertions.Expect(GetByTestId("menu-3-state")).ToHaveTextAsync("true");
        await Assertions.Expect(GetByTestId("menu-1-state")).ToHaveTextAsync("false");
        await Assertions.Expect(trigger3).ToBeFocusedAsync();
    }

    [Fact]
    public virtual async Task MenuBar_LoopFocusFalse_StaysOnFirstItem()
    {
        await NavigateAsync(CreateUrl("/tests/menubar").WithLoopFocus(false));

        var trigger1 = GetByTestId("menu-1-trigger");

        // Focus first trigger
        await trigger1.FocusAsync();
        await WaitForDelayAsync(100);

        await Assertions.Expect(trigger1).ToBeFocusedAsync();

        // Try to navigate left (should stay on first)
        await Page.Keyboard.PressAsync("ArrowLeft");
        await WaitForDelayAsync(100);

        // Focus should still be on first trigger
        await Assertions.Expect(trigger1).ToBeFocusedAsync();
    }

    [Fact]
    public virtual async Task MenuBar_RtlArrowRight_NavigatesToPreviousMenubarItem()
    {
        await NavigateAsync(CreateUrl("/tests/menubar").WithDirection("rtl"));

        var trigger1 = GetByTestId("menu-1-trigger");
        var trigger3 = GetByTestId("menu-3-trigger");

        await trigger1.FocusAsync();
        await WaitForDelayAsync(100);

        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(100);

        await Assertions.Expect(trigger3).ToBeFocusedAsync();
    }

    [Fact]
    public virtual async Task MenuBar_HomeAndEnd_DoNotMoveMenubarFocus()
    {
        await NavigateAsync(CreateUrl("/tests/menubar"));

        var trigger2 = GetByTestId("menu-2-trigger");

        await trigger2.FocusAsync();
        await WaitForDelayAsync(100);

        await Page.Keyboard.PressAsync("End");
        await WaitForDelayAsync(100);
        await Assertions.Expect(trigger2).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Home");
        await WaitForDelayAsync(100);
        await Assertions.Expect(trigger2).ToBeFocusedAsync();
    }

    [Fact]
    public virtual async Task MenuBar_DetachedTriggers_HoverSwitchesOpenMenu()
    {
        await NavigateAsync(CreateUrl("/tests/menubar-parity").WithTestScenario("detached"));

        var root = GetByTestId("menubar-root");
        var fileTrigger = GetByTestId("detached-file-trigger");
        var editTrigger = GetByTestId("detached-edit-trigger");

        await fileTrigger.ClickAsync();

        var popup = GetByTestId("detached-menu-popup");
        await popup.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5000 * TimeoutMultiplier
        });

        await Assertions.Expect(GetByTestId("detached-active-item")).ToHaveTextAsync("File");
        await Assertions.Expect(root).ToHaveAttributeAsync("data-has-submenu-open", "");

        await editTrigger.HoverAsync();
        await Assertions.Expect(GetByTestId("detached-active-item")).ToHaveTextAsync("Edit", new LocatorAssertionsToHaveTextOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    [Fact]
    public virtual async Task MenuBar_MultipleContainedTriggers_HoverSwitchesPayload()
    {
        await NavigateAsync(CreateUrl("/tests/menubar-parity").WithTestScenario("multiple-contained"));

        var root = GetByTestId("menubar-root");
        var fileTrigger = GetByTestId("multi-file-trigger");
        var editTrigger = GetByTestId("multi-edit-trigger");

        await fileTrigger.ClickAsync();

        var popup = GetByTestId("multi-menu-popup");
        await popup.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5000 * TimeoutMultiplier
        });

        await Assertions.Expect(GetByTestId("multi-active-item")).ToHaveTextAsync("File");
        await Assertions.Expect(root).ToHaveAttributeAsync("data-has-submenu-open", "");

        await editTrigger.HoverAsync();
        await Assertions.Expect(GetByTestId("multi-active-item")).ToHaveTextAsync("Edit", new LocatorAssertionsToHaveTextOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    [Fact]
    public virtual async Task MenuBar_TouchOpenedFullWidthMenu_LocksScroll()
    {
        await NavigateAsync(CreateUrl("/tests/menubar").WithTestScenario("touch-scroll-lock-full-width"));

        var trigger1 = GetByTestId("menu-1-trigger");
        await TouchOpenAsync(trigger1);
        await WaitForMenu1OpenAsync();

        await WaitForScrollLockedAsync();
    }

    [Fact]
    public virtual async Task MenuBar_TouchOpenedNarrowMenu_DoesNotLockScroll()
    {
        await NavigateAsync(CreateUrl("/tests/menubar").WithTestScenario("touch-scroll-lock-narrow"));

        var trigger1 = GetByTestId("menu-1-trigger");
        await TouchOpenAsync(trigger1);
        await WaitForMenu1OpenAsync();

        await AssertScrollUnlockedAsync();
    }

    [Fact]
    public virtual async Task MenuBar_TouchHandoffFromFullWidthToNarrowMenu_ReleasesScrollLock()
    {
        await NavigateAsync(CreateUrl("/tests/menubar").WithTestScenario("touch-scroll-lock-handoff"));

        var trigger1 = GetByTestId("menu-1-trigger");
        var trigger2 = GetByTestId("menu-2-trigger");

        await TouchOpenAsync(trigger1);
        await WaitForMenu1OpenAsync();
        await WaitForScrollLockedAsync();

        await TouchOpenAsync(trigger2);
        var popup2 = GetByTestId("menu-2-popup");
        await popup2.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5000 * TimeoutMultiplier
        });

        await AssertScrollUnlockedAsync();
    }

    #endregion

    #region Submenu Tests

    [Fact]
    public virtual async Task MenuBar_SubmenuOpensOnHover()
    {
        await NavigateAsync(CreateUrl("/tests/menubar").WithShowSubmenu(true));

        // Open menu 4
        var trigger4 = GetByTestId("menu-4-trigger");
        await trigger4.ClickAsync();
        var popup4 = GetByTestId("menu-4-popup");
        await popup4.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5000 * TimeoutMultiplier
        });

        // Hover over submenu trigger
        var submenuTrigger = GetByTestId("menu-4-submenu-trigger");
        await submenuTrigger.HoverAsync();
        await WaitForDelayAsync(500);

        var submenuState = GetByTestId("submenu-state");
        await Assertions.Expect(submenuState).ToHaveTextAsync("true");
    }

    [Fact]
    public virtual async Task MenuBar_ArrowRight_OpensSubmenu()
    {
        await NavigateAsync(CreateUrl("/tests/menubar").WithShowSubmenu(true));

        // Open menu 4
        var trigger4 = GetByTestId("menu-4-trigger");
        await trigger4.ClickAsync();
        var popup4 = GetByTestId("menu-4-popup");
        await popup4.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5000 * TimeoutMultiplier
        });

        // Navigate to submenu trigger (menu opens with no item highlighted)
        // First ArrowDown highlights Documentation (index 0)
        await Page.Keyboard.PressAsync("ArrowDown");
        await WaitForDelayAsync(100);
        // Second ArrowDown highlights More Help submenu trigger (index 1)
        await Page.Keyboard.PressAsync("ArrowDown");
        await WaitForDelayAsync(100);

        // Open submenu with arrow right
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(300);

        var submenuState = GetByTestId("submenu-state");
        await Assertions.Expect(submenuState).ToHaveTextAsync("true");
    }

    [Fact]
    public virtual async Task MenuBar_ArrowLeft_ClosesSubmenu()
    {
        await NavigateAsync(CreateUrl("/tests/menubar").WithShowSubmenu(true));

        // Open menu 4
        var trigger4 = GetByTestId("menu-4-trigger");
        await trigger4.ClickAsync();
        var popup4 = GetByTestId("menu-4-popup");
        await popup4.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5000 * TimeoutMultiplier
        });

        // Navigate to submenu trigger and open it
        await Page.Keyboard.PressAsync("ArrowDown");
        await WaitForDelayAsync(100);
        await Page.Keyboard.PressAsync("ArrowDown");
        await WaitForDelayAsync(100);
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(300);

        var submenuState = GetByTestId("submenu-state");
        await Assertions.Expect(submenuState).ToHaveTextAsync("true");

        // Press ArrowLeft to close submenu
        await Page.Keyboard.PressAsync("ArrowLeft");
        await WaitForDelayAsync(300);

        // Submenu should be closed
        await Assertions.Expect(submenuState).ToHaveTextAsync("false");

        // Parent menu should still be open
        var menu4State = GetByTestId("menu-4-state");
        await Assertions.Expect(menu4State).ToHaveTextAsync("true");
    }

    #endregion
}
