using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace BlazorBaseUI.Playwright.Tests.Tests.Toolbar;

public abstract class ToolbarTestsBase : TestBase
{
    protected ToolbarTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    #region Helper Methods

    protected ILocator GetToolbar() => GetByTestId("toolbar");
    protected ILocator GetButton(string name) => GetByTestId($"toolbar-button-{name}");
    protected ILocator GetGroupButton(string name) => GetByTestId($"toolbar-group-button-{name}");
    protected ILocator GetInput() => GetByTestId("toolbar-input");
    protected ILocator GetLink() => GetByTestId("toolbar-link");
    protected ILocator GetGroup() => GetByTestId("toolbar-group");

    protected async Task WaitForToolbarJsAsync()
    {
        await Page.WaitForFunctionAsync(@"() => {
            const el = document.querySelector('[data-testid=""toolbar""]');
            if (!el) return false;
            const stateKey = Symbol.for('BlazorBaseUI.Toolbar.State');
            const map = window[stateKey];
            if (!map || !map.has(el)) return false;
            const state = map.get(el);
            return state && state.items && state.items.size > 0;
        }", new PageWaitForFunctionOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    private async Task PerformArrowNavigationAsync(string fromTestId, string key, string expectedFocusTestId)
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

    #endregion

    #region Keyboard Navigation - Horizontal

    [Fact]
    public virtual async Task Horizontal_ArrowRight_MovesFocusToNextItem()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar"));
        await WaitForToolbarJsAsync();

        await PerformArrowNavigationAsync("toolbar-button-1", "ArrowRight", "toolbar-button-2");
    }

    [Fact]
    public virtual async Task Horizontal_ArrowLeft_MovesFocusToPreviousItem()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar"));
        await WaitForToolbarJsAsync();

        // Focus button-2 first, then arrow left
        var button2 = GetButton("2");
        await button2.FocusAsync();
        await WaitForDelayAsync(100);

        await PerformArrowNavigationAsync("toolbar-button-2", "ArrowLeft", "toolbar-button-1");
    }

    [Fact]
    public virtual async Task Horizontal_ArrowRight_LoopsToFirstItem()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar"));
        await WaitForToolbarJsAsync();

        // Focus last button, then arrow right to loop
        await PerformArrowNavigationAsync("toolbar-button-3", "ArrowRight", "toolbar-button-1");
    }

    [Fact]
    public virtual async Task Horizontal_ArrowLeft_LoopsToLastItem()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar"));
        await WaitForToolbarJsAsync();

        await PerformArrowNavigationAsync("toolbar-button-1", "ArrowLeft", "toolbar-button-3");
    }

    [Fact]
    public virtual async Task Horizontal_ArrowDown_DoesNotMoveFocus()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar"));
        await WaitForToolbarJsAsync();

        var button1 = GetButton("1");
        await button1.FocusAsync();
        await WaitForDelayAsync(200);
        await Page.Keyboard.PressAsync("ArrowDown");
        await WaitForDelayAsync(200);

        // Focus should remain on button-1
        await Assertions.Expect(button1).ToBeFocusedAsync(
            new LocatorAssertionsToBeFocusedOptions { Timeout = 3000 * TimeoutMultiplier });
    }

    [Fact]
    public virtual async Task Horizontal_Home_DoesNotMoveFocus()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar"));
        await WaitForToolbarJsAsync();

        var button3 = GetButton("3");
        await button3.FocusAsync();
        await WaitForDelayAsync(200);
        await Page.Keyboard.PressAsync("Home");
        await WaitForDelayAsync(200);

        await Assertions.Expect(button3).ToBeFocusedAsync(
            new LocatorAssertionsToBeFocusedOptions { Timeout = 3000 * TimeoutMultiplier });
    }

    [Fact]
    public virtual async Task Horizontal_End_DoesNotMoveFocus()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar"));
        await WaitForToolbarJsAsync();

        var button1 = GetButton("1");
        await button1.FocusAsync();
        await WaitForDelayAsync(200);
        await Page.Keyboard.PressAsync("End");
        await WaitForDelayAsync(200);

        await Assertions.Expect(button1).ToBeFocusedAsync(
            new LocatorAssertionsToBeFocusedOptions { Timeout = 3000 * TimeoutMultiplier });
    }

    [Fact]
    public virtual async Task Horizontal_Rtl_ArrowLeft_MovesFocusToNextItem()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar")
            .WithDirection("rtl"));
        await WaitForToolbarJsAsync();

        await PerformArrowNavigationAsync("toolbar-button-1", "ArrowLeft", "toolbar-button-2");
    }

    [Fact]
    public virtual async Task Horizontal_Rtl_ArrowRight_MovesFocusToPreviousItem()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar")
            .WithDirection("rtl"));
        await WaitForToolbarJsAsync();

        await PerformArrowNavigationAsync("toolbar-button-2", "ArrowRight", "toolbar-button-1");
    }

    #endregion

    #region Keyboard Navigation - Vertical

    [Fact]
    public virtual async Task Vertical_ArrowDown_MovesFocusToNextItem()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar")
            .WithOrientation("vertical"));
        await WaitForToolbarJsAsync();

        await PerformArrowNavigationAsync("toolbar-button-1", "ArrowDown", "toolbar-button-2");
    }

    [Fact]
    public virtual async Task Vertical_ArrowUp_MovesFocusToPreviousItem()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar")
            .WithOrientation("vertical"));
        await WaitForToolbarJsAsync();

        var button2 = GetButton("2");
        await button2.FocusAsync();
        await WaitForDelayAsync(100);

        await PerformArrowNavigationAsync("toolbar-button-2", "ArrowUp", "toolbar-button-1");
    }

    [Fact]
    public virtual async Task Vertical_ArrowDown_LoopsToFirst()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar")
            .WithOrientation("vertical"));
        await WaitForToolbarJsAsync();

        await PerformArrowNavigationAsync("toolbar-button-3", "ArrowDown", "toolbar-button-1");
    }

    [Fact]
    public virtual async Task Vertical_ArrowRight_DoesNotMoveFocus()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar")
            .WithOrientation("vertical"));
        await WaitForToolbarJsAsync();

        var button1 = GetButton("1");
        await button1.FocusAsync();
        await WaitForDelayAsync(200);
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(200);

        await Assertions.Expect(button1).ToBeFocusedAsync(
            new LocatorAssertionsToBeFocusedOptions { Timeout = 3000 * TimeoutMultiplier });
    }

    #endregion

    #region Tab Navigation

    [Fact]
    public virtual async Task Tab_MovesFocusOutOfToolbar()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar"));
        await WaitForToolbarJsAsync();

        var button1 = GetButton("1");
        await button1.FocusAsync();
        await WaitForDelayAsync(200);
        await Page.Keyboard.PressAsync("Tab");
        await WaitForDelayAsync(200);

        var afterButton = GetByTestId("after-button");
        await Assertions.Expect(afterButton).ToBeFocusedAsync(
            new LocatorAssertionsToBeFocusedOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    [Fact]
    public virtual async Task Tab_MovesFocusIntoToolbar()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar"));
        await WaitForToolbarJsAsync();

        var beforeButton = GetByTestId("before-button");
        await beforeButton.FocusAsync();
        await WaitForDelayAsync(200);
        await Page.Keyboard.PressAsync("Tab");
        await WaitForDelayAsync(200);

        var button1 = GetButton("1");
        await Assertions.Expect(button1).ToBeFocusedAsync(
            new LocatorAssertionsToBeFocusedOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    [Fact]
    public virtual async Task Tab_SkipsFirstDisabledNonFocusableItem()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar")
            .WithToolbarButton1Disabled(true)
            .WithToolbarFocusableWhenDisabled(false));
        await WaitForToolbarJsAsync();

        var beforeButton = GetByTestId("before-button");
        await beforeButton.FocusAsync();
        await WaitForDelayAsync(200);
        await Page.Keyboard.PressAsync("Tab");
        await WaitForDelayAsync(200);

        await Assertions.Expect(GetButton("2")).ToBeFocusedAsync(
            new LocatorAssertionsToBeFocusedOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    #endregion

    #region Loop Focus Control

    [Fact]
    public virtual async Task LoopDisabled_ArrowRight_StopsAtLastItem()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar")
            .WithLoopFocus(false));
        await WaitForToolbarJsAsync();

        var button3 = GetButton("3");
        await button3.FocusAsync();
        await WaitForDelayAsync(200);
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(200);

        // Should stay on button-3
        await Assertions.Expect(button3).ToBeFocusedAsync(
            new LocatorAssertionsToBeFocusedOptions { Timeout = 3000 * TimeoutMultiplier });
    }

    [Fact]
    public virtual async Task LoopDisabled_ArrowLeft_StopsAtFirstItem()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar")
            .WithLoopFocus(false));
        await WaitForToolbarJsAsync();

        var button1 = GetButton("1");
        await button1.FocusAsync();
        await WaitForDelayAsync(200);
        await Page.Keyboard.PressAsync("ArrowLeft");
        await WaitForDelayAsync(200);

        // Should stay on button-1
        await Assertions.Expect(button1).ToBeFocusedAsync(
            new LocatorAssertionsToBeFocusedOptions { Timeout = 3000 * TimeoutMultiplier });
    }

    #endregion

    #region Disabled Toolbar

    [Fact]
    public virtual async Task Disabled_ButtonsHaveDataDisabled()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar")
            .WithDisabled(true));

        await Assertions.Expect(GetButton("1")).ToHaveAttributeAsync("data-disabled", "");
        await Assertions.Expect(GetButton("2")).ToHaveAttributeAsync("data-disabled", "");
        await Assertions.Expect(GetButton("3")).ToHaveAttributeAsync("data-disabled", "");
    }

    [Fact]
    public virtual async Task Disabled_InputsHaveDataDisabled()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar")
            .WithDisabled(true)
            .WithShowInput(true));

        await Assertions.Expect(GetInput()).ToHaveAttributeAsync("data-disabled", "");
    }

    [Fact]
    public virtual async Task Disabled_LinksRemainActive()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar")
            .WithDisabled(true)
            .WithShowLink(true));

        var link = GetLink();
        // Links should NOT get data-disabled even when toolbar is disabled
        await Assertions.Expect(link).Not.ToHaveAttributeAsync("data-disabled", "");
    }

    [Fact]
    public virtual async Task Disabled_GroupHasDataDisabled()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar")
            .WithDisabled(true)
            .WithShowGroup(true));

        await Assertions.Expect(GetGroup()).ToHaveAttributeAsync("data-disabled", "");
    }

    #endregion

    #region FocusableWhenDisabled Navigation

    [Fact]
    public virtual async Task SkipsDisabledNonFocusableItems()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar")
            .WithToolbarButton2Disabled(true)
            .WithToolbarFocusableWhenDisabled(false));
        await WaitForToolbarJsAsync();

        // ArrowRight from button-1 should skip disabled button-2 and go to button-3
        await PerformArrowNavigationAsync("toolbar-button-1", "ArrowRight", "toolbar-button-3");
    }

    [Fact]
    public virtual async Task FocusesDisabledFocusableItems()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar")
            .WithToolbarButton2Disabled(true)
            .WithToolbarFocusableWhenDisabled(true));
        await WaitForToolbarJsAsync();

        // ArrowRight from button-1 should still land on disabled button-2 because focusableWhenDisabled=true
        await PerformArrowNavigationAsync("toolbar-button-1", "ArrowRight", "toolbar-button-2");
    }

    [Fact]
    public virtual async Task DisabledFocusableButton_DoesNotInvokeActivationHandlers()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar")
            .WithToolbarButton2Disabled(true)
            .WithToolbarFocusableWhenDisabled(true));
        await WaitForToolbarJsAsync();

        var button2 = GetButton("2");
        var clickCount = GetByTestId("toolbar-button-2-click-count");

        await Assertions.Expect(clickCount).ToHaveTextAsync("0");

        await button2.EvaluateAsync("el => el.click()");
        await button2.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.PressAsync("Space");
        await WaitForDelayAsync(200);

        await Assertions.Expect(clickCount).ToHaveTextAsync("0");
    }

    [Fact]
    public virtual async Task Vertical_DisabledFocusableInput_SuppressesArrowUpDown()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar")
            .WithOrientation("vertical")
            .WithShowInput(true)
            .WithToolbarInputDisabled(true)
            .WithToolbarFocusableWhenDisabled(true));
        await WaitForToolbarJsAsync();

        await PerformArrowNavigationAsync("toolbar-button-3", "ArrowDown", "toolbar-input");

        await Page.Keyboard.PressAsync("ArrowDown");
        await WaitForDelayAsync(200);
        await Assertions.Expect(GetInput()).ToBeFocusedAsync(
            new LocatorAssertionsToBeFocusedOptions { Timeout = 3000 * TimeoutMultiplier });

        await Page.Keyboard.PressAsync("ArrowUp");
        await WaitForDelayAsync(200);
        await Assertions.Expect(GetInput()).ToBeFocusedAsync(
            new LocatorAssertionsToBeFocusedOptions { Timeout = 3000 * TimeoutMultiplier });
    }

    #endregion

    #region Mixed Item Types

    [Fact]
    public virtual async Task NavigatesThroughButtonsLinksAndInputs()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar")
            .WithShowLink(true)
            .WithShowInput(true));
        await WaitForToolbarJsAsync();

        // Navigate through all item types: button-1 -> button-2 -> button-3 -> link -> input
        await PerformArrowNavigationAsync("toolbar-button-1", "ArrowRight", "toolbar-button-2");
        await PerformArrowNavigationAsync("toolbar-button-2", "ArrowRight", "toolbar-button-3");
        await PerformArrowNavigationAsync("toolbar-button-3", "ArrowRight", "toolbar-link");
        await PerformArrowNavigationAsync("toolbar-link", "ArrowRight", "toolbar-input");
    }

    [Fact]
    public virtual async Task Horizontal_InputArrowRight_UsesTextCursorUntilEnd()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar")
            .WithShowInput(true));
        await WaitForToolbarJsAsync();

        await PerformArrowNavigationAsync("toolbar-button-3", "ArrowRight", "toolbar-input");

        await Assertions.Expect(GetInput()).ToBeFocusedAsync(
            new LocatorAssertionsToBeFocusedOptions { Timeout = 5000 * TimeoutMultiplier });

        var selectedText = await GetInput().EvaluateAsync<string>(
            "el => el.value.substring(el.selectionStart, el.selectionEnd)");
        Assert.Equal("abcd", selectedText);

        await Page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(GetInput()).ToBeFocusedAsync(
            new LocatorAssertionsToBeFocusedOptions { Timeout = 3000 * TimeoutMultiplier });

        await Page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(GetButton("1")).ToBeFocusedAsync(
            new LocatorAssertionsToBeFocusedOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    [Fact]
    public virtual async Task Horizontal_InputArrowLeft_UsesTextCursorUntilStart()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar")
            .WithShowInput(true));
        await WaitForToolbarJsAsync();

        await PerformArrowNavigationAsync("toolbar-button-3", "ArrowRight", "toolbar-input");

        await Page.Keyboard.PressAsync("ArrowLeft");
        await Assertions.Expect(GetInput()).ToBeFocusedAsync(
            new LocatorAssertionsToBeFocusedOptions { Timeout = 3000 * TimeoutMultiplier });

        await Page.Keyboard.PressAsync("ArrowLeft");
        await Assertions.Expect(GetButton("3")).ToBeFocusedAsync(
            new LocatorAssertionsToBeFocusedOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    [Fact]
    public virtual async Task Vertical_InputArrowKeys_UseTextCursorBoundary()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar")
            .WithOrientation("vertical")
            .WithShowInput(true));
        await WaitForToolbarJsAsync();

        await PerformArrowNavigationAsync("toolbar-button-3", "ArrowDown", "toolbar-input");

        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(GetButton("1")).ToBeFocusedAsync(
            new LocatorAssertionsToBeFocusedOptions { Timeout = 5000 * TimeoutMultiplier });

        await PerformArrowNavigationAsync("toolbar-button-3", "ArrowDown", "toolbar-input");

        await Page.Keyboard.PressAsync("ArrowLeft");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Assertions.Expect(GetButton("3")).ToBeFocusedAsync(
            new LocatorAssertionsToBeFocusedOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    [Fact]
    public virtual async Task NavigatesThroughGroups()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar")
            .WithShowGroup(true));
        await WaitForToolbarJsAsync();

        // button-1 -> group-button-1 -> group-button-2 -> button-2
        await PerformArrowNavigationAsync("toolbar-button-1", "ArrowRight", "toolbar-group-button-1");
        await PerformArrowNavigationAsync("toolbar-group-button-1", "ArrowRight", "toolbar-group-button-2");
        await PerformArrowNavigationAsync("toolbar-group-button-2", "ArrowRight", "toolbar-button-2");
    }

    #endregion

    #region Dynamic State

    [Fact]
    public virtual async Task DynamicDisable_TogglesToolbarDisabledState()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar"));

        // Initially not disabled
        await Assertions.Expect(GetButton("1")).Not.ToHaveAttributeAsync("data-disabled", "");

        // Click toggle disabled button
        var toggleBtn = GetByTestId("toggle-disabled");
        await toggleBtn.ClickAsync();
        await WaitForDelayAsync(200);

        // Now should be disabled
        await Assertions.Expect(GetButton("1")).ToHaveAttributeAsync("data-disabled", "");
        await Assertions.Expect(GetButton("2")).ToHaveAttributeAsync("data-disabled", "");
        await Assertions.Expect(GetButton("3")).ToHaveAttributeAsync("data-disabled", "");
    }

    [Fact]
    public virtual async Task DynamicOrientationChange_UpdatesKeyboardNavigation()
    {
        await NavigateAsync(CreateUrl("/tests/toolbar"));
        await WaitForToolbarJsAsync();

        // Toggle to vertical
        var toggleBtn = GetByTestId("toggle-orientation");
        await toggleBtn.ClickAsync();
        await WaitForDelayAsync(500);

        // Verify orientation data attribute changed
        await Assertions.Expect(GetToolbar()).ToHaveAttributeAsync("data-orientation", "vertical");

        // ArrowDown should now work (vertical orientation)
        await PerformArrowNavigationAsync("toolbar-button-1", "ArrowDown", "toolbar-button-2");
    }

    #endregion
}
