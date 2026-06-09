using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Tests.Select;

/// <summary>
/// Playwright tests for Select component - focused on browser-specific behavior.
/// Static rendering, attribute forwarding, and basic state tests are handled by bUnit.
/// These tests cover: click to open/close, keyboard navigation, focus management,
/// item selection, outside click, disabled items, typeahead, and real JS interop execution.
/// </summary>
public abstract class SelectTestsBase : TestBase
{
    protected SelectTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    #region Helper Methods

    protected async Task OpenSelectAsync()
    {
        var trigger = GetByTestId("select-trigger");
        await trigger.ClickAsync();
        await WaitForSelectOpenAsync();
    }

    protected async Task WaitForSelectOpenAsync()
    {
        var popup = GetByTestId("select-popup");
        await popup.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    protected async Task WaitForSelectClosedAsync()
    {
        var openState = GetByTestId("open-state");
        await WaitForTextContentAsync(openState, "false");
    }

    protected async Task WaitForTextContentAsync(ILocator element, string expectedText, int timeout = 5000)
    {
        var effectiveTimeout = timeout * TimeoutMultiplier;
        await Assertions.Expect(element).ToHaveTextAsync(expectedText, new LocatorAssertionsToHaveTextOptions
        {
            Timeout = effectiveTimeout
        });
    }

    #endregion

    #region Open/Close Interaction Tests

    /// <summary>
    /// Tests that clicking the trigger toggles the select open/closed state.
    /// </summary>
    [Fact]
    public virtual async Task ToggleSelectOnClick()
    {
        await NavigateAsync(CreateUrl("/tests/select"));

        var trigger = GetByTestId("select-trigger");
        var openState = GetByTestId("open-state");

        await trigger.ClickAsync();
        await Assertions.Expect(openState).ToHaveTextAsync("true");

        await trigger.ClickAsync();
        await Assertions.Expect(openState).ToHaveTextAsync("false");
    }

    /// <summary>
    /// Tests that clicking outside the select closes it.
    /// </summary>
    [Fact]
    public virtual async Task ClosesOnOutsideClick()
    {
        await NavigateAsync(CreateUrl("/tests/select").WithDefaultOpen(true));

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");

        var outsideButton = GetByTestId("outside-button");
        await outsideButton.ClickAsync();

        await WaitForSelectClosedAsync();
    }

    /// <summary>
    /// Tests that pressing Escape closes the select.
    /// </summary>
    [Fact]
    public virtual async Task ClosesOnEscapeKey()
    {
        await NavigateAsync(CreateUrl("/tests/select").WithDefaultOpen(true));

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");

        await Page.Keyboard.PressAsync("Escape");

        await WaitForSelectClosedAsync();
        var lastReason = GetByTestId("last-reason");
        await Assertions.Expect(lastReason).ToHaveTextAsync("EscapeKey");
    }

    #endregion

    #region Item Selection Tests

    /// <summary>
    /// Tests that clicking an item selects it and closes the popup.
    /// </summary>
    [Fact]
    public virtual async Task SelectsItemOnClick()
    {
        await NavigateAsync(CreateUrl("/tests/select").WithDefaultOpen(true));

        var bananaItem = GetByTestId("select-item-banana");
        await bananaItem.ClickAsync();

        var selectedValue = GetByTestId("selected-value");
        await Assertions.Expect(selectedValue).ToHaveTextAsync("banana");

        await WaitForSelectClosedAsync();
    }

    /// <summary>
    /// Tests that a disabled item cannot be selected.
    /// </summary>
    [Fact]
    public virtual async Task DisabledItemCannotBeSelected()
    {
        await NavigateAsync(CreateUrl("/tests/select").WithDefaultOpen(true));

        var disabledItem = GetByTestId("select-item-disabled");
        // Use Force because Playwright refuses to click elements with aria-disabled
        await disabledItem.ClickAsync(new LocatorClickOptions { Force = true });

        // Select should remain open since disabled items don't trigger selection
        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");

        var selectedValue = GetByTestId("selected-value");
        await Assertions.Expect(selectedValue).ToHaveTextAsync("");
    }

    /// <summary>
    /// Tests that a pre-selected item has data-selected attribute when popup opens.
    /// </summary>
    [Fact]
    public virtual async Task SelectedItemHasDataSelected()
    {
        await NavigateAsync(CreateUrl("/tests/select")
            .WithSelectDefaultValue("banana")
            .WithDefaultOpen(true));

        var bananaItem = GetByTestId("select-item-banana");
        await Assertions.Expect(bananaItem).ToHaveAttributeAsync("data-selected", "");
    }

    #endregion

    #region Keyboard Navigation Tests

    /// <summary>
    /// Tests ArrowDown key navigates to the next item.
    /// </summary>
    [Fact]
    public virtual async Task ArrowDownNavigatesToNextItem()
    {
        await NavigateAsync(CreateUrl("/tests/select").WithDefaultOpen(true));

        // Wait for popup to be visible and items to be focusable
        await WaitForSelectOpenAsync();

        // Wait for initial keyboard focus to be established on first item
        var appleItem = GetByTestId("select-item-apple");
        await Assertions.Expect(appleItem).ToHaveAttributeAsync("tabindex", "0",
            new LocatorAssertionsToHaveAttributeOptions { Timeout = 5000 * TimeoutMultiplier });

        // Press ArrowDown to move to banana
        await Page.Keyboard.PressAsync("ArrowDown");

        var bananaItem = GetByTestId("select-item-banana");
        await Assertions.Expect(bananaItem).ToHaveAttributeAsync("tabindex", "0",
            new LocatorAssertionsToHaveAttributeOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    /// <summary>
    /// Tests ArrowUp key navigates to the previous item.
    /// </summary>
    [Fact]
    public virtual async Task ArrowUpNavigatesToPreviousItem()
    {
        await NavigateAsync(CreateUrl("/tests/select").WithDefaultOpen(true));
        await WaitForSelectOpenAsync();

        // Wait for initial keyboard focus
        var appleItem = GetByTestId("select-item-apple");
        await Assertions.Expect(appleItem).ToHaveAttributeAsync("tabindex", "0",
            new LocatorAssertionsToHaveAttributeOptions { Timeout = 5000 * TimeoutMultiplier });

        // Navigate down first
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("ArrowDown");

        // Now cherry should be highlighted, press up to go to banana
        await Page.Keyboard.PressAsync("ArrowUp");

        var bananaItem = GetByTestId("select-item-banana");
        await Assertions.Expect(bananaItem).ToHaveAttributeAsync("tabindex", "0",
            new LocatorAssertionsToHaveAttributeOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    /// <summary>
    /// Tests Enter key selects the highlighted item.
    /// </summary>
    [Fact]
    public virtual async Task EnterSelectsHighlightedItem()
    {
        await NavigateAsync(CreateUrl("/tests/select").WithDefaultOpen(true));
        await WaitForSelectOpenAsync();

        // Wait for initial keyboard focus
        var appleItem = GetByTestId("select-item-apple");
        await Assertions.Expect(appleItem).ToHaveAttributeAsync("tabindex", "0",
            new LocatorAssertionsToHaveAttributeOptions { Timeout = 5000 * TimeoutMultiplier });

        // Navigate to banana
        await Page.Keyboard.PressAsync("ArrowDown");

        // Press Enter to select banana
        await Page.Keyboard.PressAsync("Enter");

        var selectedValue = GetByTestId("selected-value");
        await Assertions.Expect(selectedValue).ToHaveTextAsync("banana");

        await WaitForSelectClosedAsync();
    }

    /// <summary>
    /// Tests Home key navigates to the first item.
    /// </summary>
    [Fact]
    public virtual async Task HomeKeyNavigatesToFirstItem()
    {
        await NavigateAsync(CreateUrl("/tests/select").WithDefaultOpen(true));
        await WaitForSelectOpenAsync();

        // Wait for initial keyboard focus
        var appleItem = GetByTestId("select-item-apple");
        await Assertions.Expect(appleItem).ToHaveAttributeAsync("tabindex", "0",
            new LocatorAssertionsToHaveAttributeOptions { Timeout = 5000 * TimeoutMultiplier });

        // Navigate to a later item
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("ArrowDown");

        // Press Home to go to the first item
        await Page.Keyboard.PressAsync("Home");

        await Assertions.Expect(appleItem).ToHaveAttributeAsync("tabindex", "0",
            new LocatorAssertionsToHaveAttributeOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    /// <summary>
    /// Tests End key navigates to the last item.
    /// </summary>
    [Fact]
    public virtual async Task EndKeyNavigatesToLastItem()
    {
        await NavigateAsync(CreateUrl("/tests/select").WithDefaultOpen(true));
        await WaitForSelectOpenAsync();

        // Wait for initial keyboard focus to be established on first item
        var appleItem = GetByTestId("select-item-apple");
        await Assertions.Expect(appleItem).ToHaveAttributeAsync("tabindex", "0",
            new LocatorAssertionsToHaveAttributeOptions { Timeout = 5000 * TimeoutMultiplier });

        // Press End to go to the last enabled item
        await Page.Keyboard.PressAsync("End");

        var dateItem = GetByTestId("select-item-date");
        await Assertions.Expect(dateItem).ToHaveAttributeAsync("tabindex", "0",
            new LocatorAssertionsToHaveAttributeOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    /// <summary>
    /// Tests typeahead character matching to focus matching items.
    /// </summary>
    [Fact]
    public virtual async Task TypeaheadFocusesMatchingItem()
    {
        await NavigateAsync(CreateUrl("/tests/select").WithDefaultOpen(true));
        await WaitForSelectOpenAsync();

        // Wait for initial keyboard focus
        var appleItem = GetByTestId("select-item-apple");
        await Assertions.Expect(appleItem).ToHaveAttributeAsync("tabindex", "0",
            new LocatorAssertionsToHaveAttributeOptions { Timeout = 5000 * TimeoutMultiplier });

        // Type 'c' to match "Cherry"
        await Page.Keyboard.PressAsync("c");

        var cherryItem = GetByTestId("select-item-cherry");
        await Assertions.Expect(cherryItem).ToHaveAttributeAsync("tabindex", "0",
            new LocatorAssertionsToHaveAttributeOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    #endregion

    #region Focus Management Tests

    /// <summary>
    /// Tests that focus returns to the trigger when the select closes.
    /// </summary>
    [Fact]
    public virtual async Task FocusReturnToTriggerOnClose()
    {
        await NavigateAsync(CreateUrl("/tests/select"));

        var trigger = GetByTestId("select-trigger");

        // Open and close with keyboard
        await trigger.ClickAsync();
        await WaitForSelectOpenAsync();

        await Page.Keyboard.PressAsync("Escape");
        await WaitForSelectClosedAsync();

        // Trigger should be focused
        await Assertions.Expect(trigger).ToBeFocusedAsync();
    }

    /// <summary>
    /// Tests that the selected item is focused when the popup opens with a pre-selected value.
    /// </summary>
    [Fact]
    public virtual async Task FocusesSelectedItemOnOpen()
    {
        await NavigateAsync(CreateUrl("/tests/select").WithSelectDefaultValue("banana"));

        var trigger = GetByTestId("select-trigger");
        await trigger.ClickAsync();
        await WaitForSelectOpenAsync();

        var bananaItem = GetByTestId("select-item-banana");
        await Assertions.Expect(bananaItem).ToHaveAttributeAsync("tabindex", "0",
            new LocatorAssertionsToHaveAttributeOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    #endregion

    #region Disabled Select Tests

    /// <summary>
    /// Tests that a disabled select trigger cannot be clicked to open.
    /// </summary>
    [Fact]
    public virtual async Task DisabledSelectCannotBeOpened()
    {
        await NavigateAsync(CreateUrl("/tests/select").WithDisabled(true));

        var trigger = GetByTestId("select-trigger");
        await Assertions.Expect(trigger).ToBeDisabledAsync();

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("false");
    }

    #endregion

    #region Popup Pipeline Tests

    /// <summary>
    /// Verifies that a window-resize event closes a select that was opened in
    /// align-item-with-trigger mode.
    /// </summary>
    [Fact]
    public virtual async Task WindowResizeClosesOpenSelectWhenAligned()
    {
        await NavigateAsync(CreateUrl("/tests/select"));

        await OpenSelectAsync();

        // Trigger a resize event — the JS resize listener inside SelectPopup
        // calls back to OnWindowResize, which calls SetOpenAsync(false, WindowResize).
        await Page.EvaluateAsync("() => window.dispatchEvent(new Event('resize'))");

        await WaitForSelectClosedAsync();
    }

    /// <summary>
    /// Verifies the SelectPopup emits <c>data-side</c> and <c>data-align</c>
    /// attributes derived from the positioner.
    /// </summary>
    [Fact]
    public virtual async Task PopupExposesDataSideAndDataAlign()
    {
        await NavigateAsync(CreateUrl("/tests/select"));
        await OpenSelectAsync();

        var popup = GetByTestId("select-popup");
        await Assertions.Expect(popup).ToHaveAttributeAsync(
            "data-side",
            new System.Text.RegularExpressions.Regex("^(top|bottom|left|right|inline-start|inline-end|none)$"));
        await Assertions.Expect(popup).ToHaveAttributeAsync(
            "data-align",
            new System.Text.RegularExpressions.Regex("^(start|center|end)$"));
    }

    #endregion
}
