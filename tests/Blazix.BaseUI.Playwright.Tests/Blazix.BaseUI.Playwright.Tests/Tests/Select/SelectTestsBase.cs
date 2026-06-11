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
        try
        {
            await popup.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 5000 * TimeoutMultiplier
            });
        }
        catch (TimeoutException ex)
        {
            var diagnostics = await Page.EvaluateAsync<string>(
                "() => { const selectState = window[Symbol.for('Blazix.BaseUI.Select.State')]; const roots = selectState ? Array.from(selectState.roots.entries()).map(([id, root]) => ({ id, isOpen: root.isOpen, hasTrigger: !!root.triggerElement, hasPopup: !!root.popupElement, hasPositioner: !!root.positionerElement, hasList: !!root.listElement, popupState: !!root.popup, alignItemWithTriggerActive: !!root.popup?.alignItemWithTriggerActive, initialPlaced: !!root.popup?.initialPlaced, watchdog: !!root.alignItemPlacementWatchdog, popupRect: root.popupElement?.getBoundingClientRect().toJSON(), positionerRect: root.positionerElement?.getBoundingClientRect().toJSON(), positionerSide: root.positionerElement?.getAttribute('data-side'), positionerPositioned: !!root.positionerElement?.hasAttribute('data-positioned') })) : []; const popup = document.querySelector('[data-testid=\"select-popup\"]'); const positioner = document.querySelector('[data-testid=\"select-positioner\"]'); return JSON.stringify({ roots, dom: { popupExists: !!popup, popupHidden: popup ? getComputedStyle(popup).visibility : null, popupRect: popup?.getBoundingClientRect().toJSON(), popupScrollHeight: popup?.scrollHeight, positionerExists: !!positioner, positionerRect: positioner?.getBoundingClientRect().toJSON(), positionerSide: positioner?.getAttribute('data-side'), positionerPositioned: !!positioner?.hasAttribute('data-positioned') } }); }");
            throw new TimeoutException($"Select popup did not become visible. Diagnostics: {diagnostics}", ex);
        }
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
        await Assertions.Expect(outsideButton).ToBeVisibleAsync();
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
        await WaitForSelectOpenAsync();

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
    /// Tests that explicit FinalFocus directs focus to the provided element after item selection.
    /// </summary>
    [Fact]
    public virtual async Task Focus_FinalFocusDirectsToSpecifiedElementAfterItemSelection()
    {
        await NavigateAsync(CreateUrl("/tests/select").WithSelectUseFinalFocus(true));

        await OpenSelectAsync();
        var bananaItem = GetByTestId("select-item-banana");
        await bananaItem.ClickAsync();

        await WaitForSelectClosedAsync();
        var finalFocusTarget = GetByTestId("select-final-focus-target");
        try
        {
            await Assertions.Expect(finalFocusTarget).ToBeFocusedAsync(new LocatorAssertionsToBeFocusedOptions
            {
                Timeout = 5000 * TimeoutMultiplier
            });
        }
        catch (Exception ex)
        {
            var diagnostics = await Page.EvaluateAsync<string>(
                "() => JSON.stringify({ active: document.activeElement ? { tag: document.activeElement.tagName, testId: document.activeElement.getAttribute('data-testid'), text: document.activeElement.textContent, id: document.activeElement.id, role: document.activeElement.getAttribute('role') } : null, select: Array.from((window[Symbol.for('Blazix.BaseUI.Select.State')]?.roots || new Map()).values()).map(root => ({ isOpen: root.isOpen, finalFocusManaged: root.finalFocusManaged, hasTrigger: !!root.triggerElement, hasPopup: !!root.popupElement, hasPopupState: !!root.popup })) })");
            throw new InvalidOperationException($"FinalFocus target was not focused after item selection. Diagnostics: {diagnostics}", ex);
        }
    }

    /// <summary>
    /// Tests that FinalFocus receives mouse as the close interaction type after mouse item selection.
    /// </summary>
    [Fact]
    public virtual async Task Focus_FinalFocusReceivesMouseCloseTypeAfterMouseItemSelection()
    {
        await NavigateAsync(CreateUrl("/tests/select")
            .WithSelectUseFinalFocus(true)
            .WithSelectFinalFocusMode("mouse-target"));

        await OpenSelectAsync();
        var bananaItem = GetByTestId("select-item-banana");
        await bananaItem.ClickAsync();

        await WaitForSelectClosedAsync();
        await Assertions.Expect(GetByTestId("last-final-focus-type")).ToHaveTextAsync("Mouse");
        await Assertions.Expect(GetByTestId("select-final-focus-target")).ToBeFocusedAsync(new LocatorAssertionsToBeFocusedOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    /// <summary>
    /// Tests that FinalFocus receives touch as the close interaction type after touch item selection.
    /// </summary>
    [Fact]
    public virtual async Task Focus_FinalFocusReceivesTouchCloseTypeAfterTouchItemSelection()
    {
        await NavigateAsync(CreateUrl("/tests/select")
            .WithSelectUseFinalFocus(true)
            .WithSelectFinalFocusMode("touch-target"));

        await OpenSelectAsync();
        var bananaItem = GetByTestId("select-item-banana");
        await bananaItem.DispatchEventAsync("pointerdown", new
        {
            pointerId = 1,
            pointerType = "touch",
            isPrimary = true,
            button = 0,
            buttons = 1,
            bubbles = true,
            cancelable = true
        });
        await bananaItem.DispatchEventAsync("touchstart", new
        {
            bubbles = true,
            cancelable = true
        });
        await bananaItem.DispatchEventAsync("click", new
        {
            button = 0,
            bubbles = true,
            cancelable = true
        });

        await WaitForSelectClosedAsync();
        await Assertions.Expect(GetByTestId("last-final-focus-type")).ToHaveTextAsync("Touch");
        await Assertions.Expect(GetByTestId("select-final-focus-target")).ToBeFocusedAsync(new LocatorAssertionsToBeFocusedOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    /// <summary>
    /// Tests that FinalFocus receives touch as the close interaction type after touch outside press.
    /// </summary>
    [Fact]
    public virtual async Task Focus_FinalFocusReceivesTouchCloseTypeAfterTouchOutsidePress()
    {
        await NavigateAsync(CreateUrl("/tests/select")
            .WithSelectUseFinalFocus(true)
            .WithSelectFinalFocusMode("touch-target"));

        await OpenSelectAsync();
        var outsideButton = GetByTestId("outside-button");
        await outsideButton.DispatchEventAsync("pointerdown", new
        {
            pointerId = 1,
            pointerType = "touch",
            isPrimary = true,
            button = 0,
            buttons = 1,
            bubbles = true,
            cancelable = true
        });
        await outsideButton.DispatchEventAsync("mousedown", new
        {
            button = 0,
            bubbles = true,
            cancelable = true
        });

        await WaitForSelectClosedAsync();
        await Assertions.Expect(GetByTestId("last-final-focus-type")).ToHaveTextAsync("Touch");
        await Assertions.Expect(GetByTestId("select-final-focus-target")).ToBeFocusedAsync(new LocatorAssertionsToBeFocusedOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    /// <summary>
    /// Tests that FinalFocus receives pen as the close interaction type after pen item selection.
    /// </summary>
    [Fact]
    public virtual async Task Focus_FinalFocusReceivesPenCloseTypeAfterPenItemSelection()
    {
        await NavigateAsync(CreateUrl("/tests/select")
            .WithSelectUseFinalFocus(true)
            .WithSelectFinalFocusMode("pen-target"));

        await OpenSelectAsync();
        var appleItem = GetByTestId("select-item-apple");
        await appleItem.DispatchEventAsync("pointerdown", new
        {
            pointerId = 1,
            pointerType = "pen",
            isPrimary = true,
            button = 0,
            buttons = 1,
            bubbles = true,
            cancelable = true
        });
        await appleItem.DispatchEventAsync("click", new
        {
            button = 0,
            bubbles = true,
            cancelable = true
        });

        await WaitForSelectClosedAsync();
        await Assertions.Expect(GetByTestId("last-final-focus-type")).ToHaveTextAsync("Pen");
        await Assertions.Expect(GetByTestId("select-final-focus-target")).ToBeFocusedAsync(new LocatorAssertionsToBeFocusedOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    /// <summary>
    /// Tests that FinalFocus receives keyboard as the close interaction type after keyboard item selection.
    /// </summary>
    [Fact]
    public virtual async Task Focus_FinalFocusReceivesKeyboardCloseTypeAfterKeyboardItemSelection()
    {
        await NavigateAsync(CreateUrl("/tests/select").WithSelectUseFinalFocus(true));

        await OpenSelectAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("Enter");

        await WaitForSelectClosedAsync();
        await Assertions.Expect(GetByTestId("last-final-focus-type")).ToHaveTextAsync("Keyboard");
        await Assertions.Expect(GetByTestId("select-trigger")).ToBeFocusedAsync(new LocatorAssertionsToBeFocusedOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
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
        await Page.EvaluateAsync(
            "() => { document.querySelector('[data-testid=\"test-container\"]').style.paddingTop = '120px'; }");

        await OpenSelectAsync();

        var positioner = GetByTestId("select-positioner");
        await Assertions.Expect(positioner).ToHaveAttributeAsync("data-side", "none");

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

    #region Select JS Regression Tests

    /// <summary>
    /// Tests that root-trigger registration rebinds JS listeners when the DOM element changes.
    /// </summary>
    [Fact]
    public virtual async Task Js_RebindsTriggerListenersWhenTriggerElementChanges()
    {
        await NavigateAsync(CreateUrl("/tests/select"));

        var rebound = await Page.EvaluateAsync<bool>(
            """
            async () => {
                const select = await import('/_content/Blazix.BaseUI/blazix-baseui-select.min.js');
                const rootId = `trigger-rebind-${crypto.randomUUID()}`;
                const calls = [];
                const dotNetRef = {
                    invokeMethodAsync: async (name) => {
                        calls.push(name);
                    },
                };
                const first = document.createElement('button');
                const second = document.createElement('button');
                first.type = 'button';
                second.type = 'button';
                document.body.append(first, second);

                try {
                    select.initializeRoot(rootId, dotNetRef, false, false, 'ltr', false, false);
                    select.setTriggerElement(rootId, first);
                    select.setTriggerElement(rootId, second);

                    second.dispatchEvent(new PointerEvent('pointerdown', {
                        pointerId: 1,
                        pointerType: 'touch',
                        isPrimary: true,
                        button: 0,
                        buttons: 1,
                        bubbles: true,
                        cancelable: true,
                    }));
                    await new Promise((resolve) => setTimeout(resolve, 0));

                    return calls.includes('NotifyTouchOpen');
                } finally {
                    select.disposeRoot(rootId);
                    first.remove();
                    second.remove();
                }
            }
            """);

        Assert.True(rebound);
    }

    /// <summary>
    /// Tests that normal floating opens do not keep the align-item placement probe alive.
    /// </summary>
    [Fact]
    public virtual async Task Js_NormalFloatingOpenDoesNotContinueAlignItemPlacementProbe()
    {
        await NavigateAsync(CreateUrl("/tests/select"));

        var probeRafCount = await Page.EvaluateAsync<int>(
            """
            async () => {
                const select = await import('/_content/Blazix.BaseUI/blazix-baseui-select.min.js');
                const rootId = `normal-floating-${crypto.randomUUID()}`;
                const calls = [];
                const dotNetRef = {
                    invokeMethodAsync: async (name) => {
                        calls.push(name);
                    },
                };
                const trigger = document.createElement('button');
                const positioner = document.createElement('div');
                const popup = document.createElement('div');
                trigger.type = 'button';
                trigger.style.cssText = 'position:absolute;left:20px;top:20px;width:100px;height:30px;';
                positioner.style.cssText = 'position:absolute;left:20px;top:60px;width:120px;height:80px;';
                positioner.setAttribute('data-side', 'bottom');
                popup.style.cssText = 'width:120px;height:80px;';
                popup.innerHTML = '<div role="option">Apple</div>';
                positioner.append(popup);
                document.body.append(trigger, positioner);

                const originalRaf = window.requestAnimationFrame;
                let probeRafCount = 0;
                let counting = true;
                window.requestAnimationFrame = (callback) => {
                    if (counting && Function.prototype.toString.call(callback).includes('scheduleOpenAlignItemPlacement')) {
                        probeRafCount += 1;
                    }
                    return originalRaf.call(window, callback);
                };

                try {
                    select.initializeRoot(rootId, dotNetRef, false, false, 'ltr', false, false);
                    select.setTriggerElement(rootId, trigger);
                    select.initializePopup(rootId, popup, dotNetRef, false);
                    select.setPopupElement(rootId, popup);
                    select.registerPositioner(rootId, positioner);
                    select.setRootOpen(rootId, true, null);

                    await new Promise((resolve) => setTimeout(resolve, 120));
                    counting = false;
                    return probeRafCount;
                } finally {
                    counting = false;
                    window.requestAnimationFrame = originalRaf;
                    select.disposeRoot(rootId);
                    positioner.remove();
                    trigger.remove();
                }
            }
            """);

        Assert.Equal(0, probeRafCount);
    }

    #endregion
}
