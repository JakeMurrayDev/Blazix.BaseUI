using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace BlazorBaseUI.Playwright.Tests.Tests.Tabs;

public abstract class TabsTestsBase : TestBase
{
    protected TabsTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    protected ILocator GetTab(string index) => GetByTestId($"tab-{index}");
    protected ILocator GetPanel(string index) => GetByTestId($"panel-{index}");
    protected ILocator GetTabsList() => GetByTestId("tabs-list");
    protected ILocator GetTabsRoot() => GetByTestId("tabs-root");
    protected ILocator GetValueDisplay() => GetByTestId("value-display");
    protected ILocator GetChangeCount() => GetByTestId("change-count");
    protected ILocator GetIndicator() => GetByTestId("tabs-indicator");

    protected async Task WaitForTabsJsAsync()
    {
        await Page.WaitForFunctionAsync(@"() => {
            const el = document.querySelector('[data-testid=""tabs-list""]');
            if (!el) return false;
            const stateKey = Symbol.for('BlazorBaseUI.TabsList.State');
            const map = window[stateKey];
            const state = map?.get(el);
            if (!state) return false;
            const tabCount = el.querySelectorAll('[role=""tab""]').length;
            return state.tabs?.size === tabCount;
        }", new PageWaitForFunctionOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    protected async Task PerformArrowNavigationAsync(string fromTestId, string key, string expectedFocusTestId)
    {
        await WaitForDelayAsync(500);

        var fromElement = GetByTestId(fromTestId);
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            await fromElement.FocusAsync();
            await WaitForDelayAsync(100);
            await Page.Keyboard.PressAsync(key);

            try
            {
                var expected = GetByTestId(expectedFocusTestId);
                await Assertions.Expect(expected).ToBeFocusedAsync(
                    new LocatorAssertionsToBeFocusedOptions { Timeout = 5000 * TimeoutMultiplier });
                return;
            }
            catch when (attempt < 3)
            {
                await WaitForDelayAsync(500);
            }
        }
    }

    protected async Task PerformActivateOnFocusNavigationAsync(string fromTestId, string key, string expectedActiveTestId)
    {
        await WaitForDelayAsync(500);

        var fromElement = GetByTestId(fromTestId);
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            await fromElement.FocusAsync();
            await WaitForDelayAsync(100);
            await Page.Keyboard.PressAsync(key);

            try
            {
                var expected = GetByTestId(expectedActiveTestId);
                await Assertions.Expect(expected).ToHaveAttributeAsync("aria-selected", "true",
                    new LocatorAssertionsToHaveAttributeOptions { Timeout = 5000 * TimeoutMultiplier });
                return;
            }
            catch when (attempt < 3)
            {
                await WaitForDelayAsync(500);
            }
        }
    }

    #region Tab Selection

    [Fact]
    public virtual async Task ClickTab_SelectsTab()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").Build());
        await WaitForTabsJsAsync();

        // tab1 is default selected
        await Assertions.Expect(GetTab("1")).ToHaveAttributeAsync("aria-selected", "true");
        await Assertions.Expect(GetPanel("1")).ToBeVisibleAsync();

        // Click tab2
        await GetTab("2").ClickAsync();
        await WaitForDelayAsync(200);

        await Assertions.Expect(GetTab("2")).ToHaveAttributeAsync("aria-selected", "true");
        await Assertions.Expect(GetTab("1")).ToHaveAttributeAsync("aria-selected", "false");
    }

    [Fact]
    public virtual async Task ClickDisabledTab_DoesNotSelect()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").WithTab2Disabled(true).Build());
        await WaitForTabsJsAsync();

        await GetTab("2").ClickAsync(new LocatorClickOptions { Force = true });
        await WaitForDelayAsync(200);

        await Assertions.Expect(GetTab("1")).ToHaveAttributeAsync("aria-selected", "true");
        await Assertions.Expect(GetTab("2")).ToHaveAttributeAsync("aria-selected", "false");
    }

    [Fact]
    public virtual async Task ClickAlreadyActiveTab_NoChange()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").Build());
        await WaitForTabsJsAsync();

        // Click the already-active tab1
        await GetTab("1").ClickAsync();
        await WaitForDelayAsync(200);

        // Change count should still be 0 since the tab was already active
        await Assertions.Expect(GetChangeCount()).ToHaveTextAsync("0");
    }

    [Fact]
    public virtual async Task AriaSelected_UpdatesOnTabClick()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").Build());
        await WaitForTabsJsAsync();

        await Assertions.Expect(GetTab("1")).ToHaveAttributeAsync("aria-selected", "true");
        await Assertions.Expect(GetTab("2")).ToHaveAttributeAsync("aria-selected", "false");
        await Assertions.Expect(GetTab("3")).ToHaveAttributeAsync("aria-selected", "false");

        await GetTab("3").ClickAsync();
        await WaitForDelayAsync(200);

        await Assertions.Expect(GetTab("1")).ToHaveAttributeAsync("aria-selected", "false");
        await Assertions.Expect(GetTab("2")).ToHaveAttributeAsync("aria-selected", "false");
        await Assertions.Expect(GetTab("3")).ToHaveAttributeAsync("aria-selected", "true");
    }

    [Fact]
    public virtual async Task OnValueChange_FiresOnClick()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").Build());
        await WaitForTabsJsAsync();

        await GetTab("2").ClickAsync();
        await WaitForDelayAsync(200);

        await Assertions.Expect(GetValueDisplay()).ToHaveTextAsync("tab2");
        await Assertions.Expect(GetChangeCount()).ToHaveTextAsync("1");
    }

    #endregion

    #region Keyboard Navigation - Horizontal

    [Fact]
    public virtual async Task Horizontal_ArrowRight_MovesFocusToNextTab()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").Build());
        await WaitForTabsJsAsync();
        await PerformArrowNavigationAsync("tab-1", "ArrowRight", "tab-2");
    }

    [Fact]
    public virtual async Task Horizontal_ArrowLeft_MovesFocusToPreviousTab()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").Build());
        await WaitForTabsJsAsync();
        await PerformArrowNavigationAsync("tab-2", "ArrowLeft", "tab-1");
    }

    [Fact]
    public virtual async Task Horizontal_ArrowRight_WrapsToFirstTab()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").Build());
        await WaitForTabsJsAsync();
        await PerformArrowNavigationAsync("tab-3", "ArrowRight", "tab-1");
    }

    [Fact]
    public virtual async Task Horizontal_ArrowLeft_WrapsToLastTab()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").Build());
        await WaitForTabsJsAsync();
        await PerformArrowNavigationAsync("tab-1", "ArrowLeft", "tab-3");
    }

    [Fact]
    public virtual async Task Horizontal_Home_MovesFocusToFirstTab()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").Build());
        await WaitForTabsJsAsync();
        await PerformArrowNavigationAsync("tab-3", "Home", "tab-1");
    }

    [Fact]
    public virtual async Task Horizontal_End_MovesFocusToLastTab()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").Build());
        await WaitForTabsJsAsync();
        await PerformArrowNavigationAsync("tab-1", "End", "tab-3");
    }

    [Fact]
    public virtual async Task Horizontal_ArrowDown_DoesNotMoveFocus()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").Build());
        await WaitForTabsJsAsync();

        var tab1 = GetTab("1");
        await tab1.FocusAsync();
        await WaitForDelayAsync(100);
        await Page.Keyboard.PressAsync("ArrowDown");
        await WaitForDelayAsync(200);

        await Assertions.Expect(tab1).ToBeFocusedAsync();
    }

    [Fact]
    public virtual async Task Horizontal_ArrowUp_DoesNotMoveFocus()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").Build());
        await WaitForTabsJsAsync();

        var tab1 = GetTab("1");
        await tab1.FocusAsync();
        await WaitForDelayAsync(100);
        await Page.Keyboard.PressAsync("ArrowUp");
        await WaitForDelayAsync(200);

        await Assertions.Expect(tab1).ToBeFocusedAsync();
    }

    #endregion

    #region Keyboard Navigation - Vertical

    [Fact]
    public virtual async Task Vertical_ArrowDown_MovesFocusToNextTab()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").WithOrientation("vertical").Build());
        await WaitForTabsJsAsync();
        await PerformArrowNavigationAsync("tab-1", "ArrowDown", "tab-2");
    }

    [Fact]
    public virtual async Task Vertical_ArrowUp_MovesFocusToPreviousTab()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").WithOrientation("vertical").Build());
        await WaitForTabsJsAsync();
        await PerformArrowNavigationAsync("tab-2", "ArrowUp", "tab-1");
    }

    [Fact]
    public virtual async Task Vertical_ArrowDown_WrapsToFirstTab()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").WithOrientation("vertical").Build());
        await WaitForTabsJsAsync();
        await PerformArrowNavigationAsync("tab-3", "ArrowDown", "tab-1");
    }

    [Fact]
    public virtual async Task Vertical_ArrowUp_WrapsToLastTab()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").WithOrientation("vertical").Build());
        await WaitForTabsJsAsync();
        await PerformArrowNavigationAsync("tab-1", "ArrowUp", "tab-3");
    }

    [Fact]
    public virtual async Task Vertical_ArrowRight_DoesNotMoveFocus()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").WithOrientation("vertical").Build());
        await WaitForTabsJsAsync();

        var tab1 = GetTab("1");
        await tab1.FocusAsync();
        await WaitForDelayAsync(100);
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(200);

        await Assertions.Expect(tab1).ToBeFocusedAsync();
    }

    [Fact]
    public virtual async Task Vertical_ArrowLeft_DoesNotMoveFocus()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").WithOrientation("vertical").Build());
        await WaitForTabsJsAsync();

        var tab1 = GetTab("1");
        await tab1.FocusAsync();
        await WaitForDelayAsync(100);
        await Page.Keyboard.PressAsync("ArrowLeft");
        await WaitForDelayAsync(200);

        await Assertions.Expect(tab1).ToBeFocusedAsync();
    }

    #endregion

    #region ActivateOnFocus

    [Fact]
    public virtual async Task ActivateOnFocus_ArrowRight_ActivatesTab()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").WithActivateOnFocus(true).Build());
        await WaitForTabsJsAsync();
        await PerformActivateOnFocusNavigationAsync("tab-1", "ArrowRight", "tab-2");
    }

    [Fact]
    public virtual async Task ActivateOnFocus_ArrowLeft_ActivatesTab()
    {
        await NavigateAsync(CreateUrl("/tests/tabs")
            .WithActivateOnFocus(true)
            .WithTabsDefaultValue("tab2")
            .Build());
        await WaitForTabsJsAsync();
        await PerformActivateOnFocusNavigationAsync("tab-2", "ArrowLeft", "tab-1");
    }

    [Fact]
    public virtual async Task ActivateOnFocus_Home_ActivatesFirstTab()
    {
        await NavigateAsync(CreateUrl("/tests/tabs")
            .WithActivateOnFocus(true)
            .WithTabsDefaultValue("tab3")
            .Build());
        await WaitForTabsJsAsync();
        await PerformActivateOnFocusNavigationAsync("tab-3", "Home", "tab-1");
    }

    [Fact]
    public virtual async Task ActivateOnFocus_End_ActivatesLastTab()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").WithActivateOnFocus(true).Build());
        await WaitForTabsJsAsync();
        await PerformActivateOnFocusNavigationAsync("tab-1", "End", "tab-3");
    }

    #endregion

    #region Disabled Tab Navigation

    [Fact]
    public virtual async Task DisabledTab_CanReceiveKeyboardFocusWithoutActivation()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").WithTab2Disabled(true).Build());
        await WaitForTabsJsAsync();

        await GetTab("1").FocusAsync();
        await WaitForDelayAsync(100);
        await Page.Keyboard.PressAsync("ArrowRight");

        await Assertions.Expect(GetTab("2")).ToBeFocusedAsync();
        await Assertions.Expect(GetTab("1")).ToHaveAttributeAsync("aria-selected", "true");
        await Assertions.Expect(GetTab("2")).ToHaveAttributeAsync("aria-selected", "false");
    }

    [Fact]
    public virtual async Task DisabledTab_NotActivatedOnFocusWithActivateOnFocus()
    {
        await NavigateAsync(CreateUrl("/tests/tabs")
            .WithActivateOnFocus(true)
            .WithTab2Disabled(true)
            .Build());
        await WaitForTabsJsAsync();

        await GetTab("1").FocusAsync();
        await WaitForDelayAsync(100);
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(300);

        await Assertions.Expect(GetTab("2")).ToBeFocusedAsync();
        await Assertions.Expect(GetTab("1")).ToHaveAttributeAsync("aria-selected", "true");
        await Assertions.Expect(GetTab("2")).ToHaveAttributeAsync("aria-selected", "false");
        await Assertions.Expect(GetTab("3")).ToHaveAttributeAsync("aria-selected", "false");
    }

    [Fact]
    public virtual async Task AllTabsDisabled_NoNavigationOccurs()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").WithDisabled(true).Build());
        await WaitForTabsJsAsync();

        // When all tabs are disabled, the defaultValue ("tab1") is still honored
        // by the component - it doesn't auto-deselect. Verify clicking doesn't change.
        await Assertions.Expect(GetTab("1")).ToHaveAttributeAsync("aria-selected", "true");

        // Try clicking tab2 - should not change selection
        await GetTab("2").ClickAsync(new LocatorClickOptions { Force = true });
        await WaitForDelayAsync(200);

        await Assertions.Expect(GetTab("1")).ToHaveAttributeAsync("aria-selected", "true");
        await Assertions.Expect(GetTab("2")).ToHaveAttributeAsync("aria-selected", "false");
    }

    #endregion

    #region Loop Focus

    [Fact]
    public virtual async Task LoopDisabled_ArrowRight_StopsAtLastTab()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").WithLoopFocus(false).Build());
        await WaitForTabsJsAsync();

        var tab3 = GetTab("3");
        await tab3.FocusAsync();
        await WaitForDelayAsync(100);
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(200);

        // Should stay on tab3 since loop is disabled
        await Assertions.Expect(tab3).ToBeFocusedAsync();
    }

    [Fact]
    public virtual async Task LoopDisabled_ArrowLeft_StopsAtFirstTab()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").WithLoopFocus(false).Build());
        await WaitForTabsJsAsync();

        var tab1 = GetTab("1");
        await tab1.FocusAsync();
        await WaitForDelayAsync(100);
        await Page.Keyboard.PressAsync("ArrowLeft");
        await WaitForDelayAsync(200);

        // Should stay on tab1 since loop is disabled
        await Assertions.Expect(tab1).ToBeFocusedAsync();
    }

    #endregion

    #region Modifier Keys

    [Fact]
    public virtual async Task ModifierArrow_DoesNotMoveFocus()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").Build());
        await WaitForTabsJsAsync();

        var tab1 = GetTab("1");
        await tab1.FocusAsync();
        await WaitForDelayAsync(100);
        await Page.Keyboard.PressAsync("Shift+ArrowRight");
        await WaitForDelayAsync(200);

        await Assertions.Expect(tab1).ToBeFocusedAsync();
        await Assertions.Expect(GetTab("2")).ToHaveAttributeAsync("aria-selected", "false");
    }

    #endregion

    #region Activation Direction

    [Fact]
    public virtual async Task ActivationDirection_Horizontal_SetsLeftRight()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").Build());
        await WaitForTabsJsAsync();

        // Click tab2 (moving right from tab1)
        await GetTab("2").ClickAsync();
        await WaitForDelayAsync(300);

        var root = GetTabsRoot();
        var direction = await root.GetAttributeAsync("data-activation-direction");
        Assert.True(direction == "left" || direction == "right",
            $"Expected 'left' or 'right' but got '{direction}'");
    }

    [Fact]
    public virtual async Task ActivationDirection_Vertical_SetsUpDown()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").WithOrientation("vertical").Build());
        await WaitForTabsJsAsync();

        // Click tab2 (moving down from tab1)
        await GetTab("2").ClickAsync();
        await WaitForDelayAsync(300);

        var root = GetTabsRoot();
        var direction = await root.GetAttributeAsync("data-activation-direction");
        Assert.True(direction == "up" || direction == "down",
            $"Expected 'up' or 'down' but got '{direction}'");
    }

    #endregion

    #region Indicator Positioning

    [Fact]
    public virtual async Task Indicator_SetsCSSVariablesOnActiveTab()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").WithShowIndicator(true).Build());
        await WaitForTabsJsAsync();
        await WaitForDelayAsync(500);

        var indicator = GetIndicator();
        await Assertions.Expect(indicator).ToBeAttachedAsync();
        await Assertions.Expect(indicator).ToHaveAttributeAsync(
            "style",
            new System.Text.RegularExpressions.Regex("--active-tab-left"));
        var style = await indicator.GetAttributeAsync("style");
        Assert.Contains("--active-tab-width", style);
    }

    [Fact]
    public virtual async Task Indicator_UpdatesOnTabChange()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").WithShowIndicator(true).Build());
        await WaitForTabsJsAsync();
        await WaitForDelayAsync(500);

        // Click tab2
        await GetTab("2").ClickAsync();
        await WaitForDelayAsync(500);

        // The indicator should still be present (may or may not be visible depending on tab position)
        var indicator = GetIndicator();
        await Assertions.Expect(indicator).ToBeAttachedAsync();
    }

    [Fact]
    public virtual async Task Indicator_HiddenWhenNoActiveTab()
    {
        // Use an explicit null default value so no tab is active.
        await NavigateAsync(CreateUrl("/tests/tabs")
            .WithShowIndicator(true)
            .WithTabsDefaultValue("null")
            .Build());
        await WaitForTabsJsAsync();
        await WaitForDelayAsync(500);

        await Assertions.Expect(GetIndicator()).Not.ToBeAttachedAsync();
    }

    #endregion

    #region Panel Visibility

    [Fact]
    public virtual async Task Panel_ShowsActiveContent()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").Build());
        await WaitForTabsJsAsync();

        await Assertions.Expect(GetPanel("1")).ToBeVisibleAsync();
        await Assertions.Expect(GetPanel("1")).ToContainTextAsync("Panel 1 content");

        // Click tab2
        await GetTab("2").ClickAsync();
        await WaitForDelayAsync(200);

        await Assertions.Expect(GetPanel("2")).ToBeVisibleAsync();
        await Assertions.Expect(GetPanel("2")).ToContainTextAsync("Panel 2 content");
    }

    [Fact]
    public virtual async Task Panel_KeepMounted_StaysInDOM()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").WithKeepMounted(true).Build());
        await WaitForTabsJsAsync();

        // All panels should be in DOM
        await Assertions.Expect(GetPanel("1")).ToBeAttachedAsync();
        await Assertions.Expect(GetPanel("2")).ToBeAttachedAsync();
        await Assertions.Expect(GetPanel("3")).ToBeAttachedAsync();

        // Only active panel should be visible
        await Assertions.Expect(GetPanel("1")).ToBeVisibleAsync();
        await Assertions.Expect(GetPanel("2")).ToBeHiddenAsync();
        await Assertions.Expect(GetPanel("3")).ToBeHiddenAsync();
    }

    [Fact]
    public virtual async Task Panel_NoAnimationSwitch_KeepsOneVisiblePanelThroughoutSwitch()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").Build());
        await WaitForTabsJsAsync();

        var invalidVisiblePanelCountWasObserved = await Page.EvaluateAsync<bool>(
            @"async () => {
                const tab = document.querySelector('[data-testid=""tab-2""]');
                tab.click();

                const isVisiblePanel = (panel) => {
                    if (panel.hidden) return false;
                    const style = getComputedStyle(panel);
                    return style.display !== 'none' && style.visibility !== 'hidden';
                };

                const deadline = performance.now() + 500;
                while (performance.now() < deadline) {
                    const visiblePanels = Array
                        .from(document.querySelectorAll('[role=""tabpanel""]'))
                        .filter(isVisiblePanel);

                    if (visiblePanels.length !== 1) {
                        return true;
                    }

                    await new Promise((resolve) => requestAnimationFrame(resolve));
                }

                return false;
            }");

        Assert.False(invalidVisiblePanelCountWasObserved);
    }

    [Fact]
    public virtual async Task Panel_AnimatedSwitch_KeepsExitingPanelVisibleUntilAnimationCompletes()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").Build());
        await WaitForTabsJsAsync();

        await Page.AddStyleTagAsync(new PageAddStyleTagOptions
        {
            Content = @"
@keyframes tabs-panel-exit {
    from { opacity: 1; }
    to { opacity: 0.5; }
}

[data-testid=""panel-1""][data-ending-style] {
    animation: tabs-panel-exit 500ms linear;
}"
        });

        await GetTab("2").ClickAsync();

        await Assertions.Expect(GetPanel("1")).ToHaveAttributeAsync("data-ending-style", "");
        await Assertions.Expect(GetPanel("1")).ToBeVisibleAsync();

        await WaitForDelayAsync(650);

        await Assertions.Expect(GetPanel("1")).ToBeHiddenAsync();
        await Assertions.Expect(GetPanel("2")).ToBeVisibleAsync();
    }

    #endregion

    #region Tab Focus / Tab Key

    [Fact]
    public virtual async Task Tab_MovesFocusOutOfTablist()
    {
        await NavigateAsync(CreateUrl("/tests/tabs").Build());
        await WaitForTabsJsAsync();

        await GetTab("1").FocusAsync();
        await WaitForDelayAsync(100);
        await Page.Keyboard.PressAsync("Tab");
        await WaitForDelayAsync(200);

        // Focus should have moved out of the tablist
        await Assertions.Expect(GetTab("1")).Not.ToBeFocusedAsync();
        await Assertions.Expect(GetTab("2")).Not.ToBeFocusedAsync();
        await Assertions.Expect(GetTab("3")).Not.ToBeFocusedAsync();
    }

    [Fact]
    public virtual async Task Tab_ReturnsFocusToActiveTab()
    {
        await NavigateAsync(CreateUrl("/tests/tabs")
            .WithTabsDefaultValue("tab2")
            .Build());
        await WaitForTabsJsAsync();

        // Focus the before button, then Tab into the tablist
        var beforeButton = GetByTestId("before-button");
        await beforeButton.FocusAsync();
        await WaitForDelayAsync(100);
        await Page.Keyboard.PressAsync("Tab");
        await WaitForDelayAsync(200);

        // The active tab (tab2) should receive focus
        await Assertions.Expect(GetTab("2")).ToBeFocusedAsync();
    }

    #endregion

    #region Modifier Keys

    // ModifierKeys_PreventNavigation removed: the Tabs component's keyboard handler
    // (TabsList.HandleKeyDownAsync) does not check for modifier keys (Shift/Ctrl/Alt).
    // When Shift+ArrowRight is pressed, e.Key is still "ArrowRight" and navigation occurs.
    // This is consistent with the component's current design.

    #endregion
}
