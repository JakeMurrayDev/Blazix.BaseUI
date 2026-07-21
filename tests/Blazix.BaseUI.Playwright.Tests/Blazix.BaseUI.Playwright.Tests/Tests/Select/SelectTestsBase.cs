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

    protected async Task<double[][]> CaptureVisiblePopupSizingFramesAsync(
        ILocator trigger,
        ILocator? opener = null)
    {
        var samplingTask = Page.EvaluateAsync<double[][]>(
            """
            async () => {
                window.__selectSizingSamplerReady = false;
                const frames = [];
                let frameNumber = 0;
                let firstOpenFrame = -1;
                window.__selectSizingSamplerReady = true;
                const startedAt = performance.now();

                try {
                    while (performance.now() - startedAt < 900) {
                        await new Promise((resolve) => requestAnimationFrame(resolve));
                        frameNumber += 1;
                        const popup = document.querySelector('[data-testid="select-popup"]');
                        const positioner = document.querySelector('[data-testid="select-positioner"]');
                        if (!popup || !positioner || !popup.hasAttribute('data-open')) {
                            continue;
                        }
                        if (firstOpenFrame < 0) firstOpenFrame = frameNumber;

                        const popupStyles = getComputedStyle(popup);
                        const popupRect = popup.getBoundingClientRect();
                        if (popupStyles.visibility === 'hidden' || popupStyles.display === 'none' ||
                            Number.parseFloat(popupStyles.opacity) === 0 ||
                            popupRect.width === 0 || popupRect.height === 0) {
                            continue;
                        }

                        const positionerStyles = getComputedStyle(positioner);
                        const selectState = window[Symbol.for('Blazix.BaseUI.Select.State')];
                        const root = selectState ? Array.from(selectState.roots.values())[0] : null;
                        frames.push([
                            popupRect.width,
                            popup.querySelectorAll('[role="option"]').length,
                            Number.parseFloat(positionerStyles.getPropertyValue('--anchor-width')) || 0,
                            positioner.hasAttribute('data-positioned') ? 1 : 0,
                            popupRect.height,
                            popupRect.top,
                            positioner.getBoundingClientRect().height,
                            positioner.getBoundingClientRect().top,
                            (popup.querySelector('[role="listbox"]') || popup).scrollTop,
                            frameNumber - firstOpenFrame,
                            root?.placementInputRevision ?? -1,
                            root?.popup?.placementCommittedInputRevision ?? -1,
                            root?.popup?.alignItemWithTriggerActive ? 1 : 0,
                            root?.popup?.standardFallbackPending ? 1 : 0,
                        ]);
                    }
                } finally {
                    delete window.__selectSizingSamplerReady;
                }

                return frames;
            }
            """);

        await Page.WaitForFunctionAsync("() => window.__selectSizingSamplerReady === true");
        await (opener ?? trigger).ClickAsync();
        return await samplingTask;
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
    /// Verifies that align-item popups are never exposed at their intrinsic
    /// width before Floating UI resolves anchor sizing, including on reopen.
    /// </summary>
    [Theory]
    [InlineData(false, 5)]
    [InlineData(true, 16)]
    public virtual async Task AlignItemPopupUsesFinalAnchorWidthFromFirstVisibleFrameOnRepeatedOpen(
        bool showGroups,
        int expectedOptionCount)
    {
        await NavigateAsync(CreateUrl("/tests/select").WithSelectGroups(showGroups));
        await Page.EvaluateAsync(
            "() => { document.querySelector('[data-testid=\"test-container\"]').style.paddingTop = '120px'; }");

        await Page.EvaluateAsync(
            """
            () => {
                const style = document.createElement('style');
                style.textContent = `
                    [data-testid="select-trigger"] { width: 224px; }
                    [data-testid="select-popup"] {
                        min-width: calc(var(--anchor-width) + 28px);
                        max-height: var(--available-height);
                    }
                `;
                document.head.append(style);
            }
            """);

        var trigger = GetByTestId("select-trigger");

        for (var openIndex = 0; openIndex < 2; openIndex++)
        {
            var expectedAnchorWidth = openIndex == 0 ? 224d : 280d;
            var expectedPopupWidth = expectedAnchorWidth + 28d;
            var visibleFrames = await CaptureVisiblePopupSizingFramesAsync(trigger);

            Assert.NotEmpty(visibleFrames);
            var firstFrame = visibleFrames[0];
            Assert.InRange(firstFrame[9], 0, 16);
            var resizedFrame = visibleFrames.FirstOrDefault(frame =>
                Math.Abs(frame[4] - firstFrame[4]) > 0.5 ||
                Math.Abs(frame[6] - firstFrame[6]) > 0.5);
            Assert.True(resizedFrame is null,
                $"Popup resized after visibility. First: [{string.Join(", ", firstFrame)}]. Changed: [{string.Join(", ", resizedFrame ?? [])}].");
            Assert.All(visibleFrames, frame =>
            {
                Assert.Equal(expectedOptionCount, (int)frame[1]);
                Assert.Equal(1, (int)frame[3]);
                Assert.InRange(frame[2], expectedAnchorWidth - 0.5, expectedAnchorWidth + 0.5);
                Assert.InRange(frame[0], expectedPopupWidth - 0.5, expectedPopupWidth + 0.5);
                Assert.InRange(frame[4], firstFrame[4] - 0.5, firstFrame[4] + 0.5);
                Assert.InRange(frame[6], firstFrame[6] - 0.5, firstFrame[6] + 0.5);
                Assert.InRange(frame[8], firstFrame[8] - 0.5, firstFrame[8] + 0.5);
            });

            if (openIndex == 0)
            {
                await Page.Keyboard.PressAsync("Escape");
                await WaitForSelectClosedAsync();
                await Assertions.Expect(GetByTestId("select-popup")).ToHaveAttributeAsync("data-closed", "");
                await Page.EvaluateAsync(
                    "() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
                await trigger.EvaluateAsync("element => { element.style.width = '280px'; }");
            }
        }
    }

    /// <summary>
    /// Verifies that an externally controlled reopen cannot lose the Floating UI
    /// readiness result when the parameter render precedes root-open JS interop.
    /// </summary>
    [Fact]
    public virtual async Task ControlledReopenUsesFreshSizingFromFirstVisibleFrame()
    {
        await NavigateAsync(CreateUrl("/tests/select"));

        await Page.EvaluateAsync(
            """
            () => {
                const style = document.createElement('style');
                style.textContent = `
                    [data-testid="select-trigger"] { width: 224px; }
                    [data-testid="select-popup"] {
                        min-width: calc(var(--anchor-width) + 28px);
                        max-height: var(--available-height);
                    }
                `;
                document.head.append(style);
            }
            """);

        var trigger = GetByTestId("select-trigger");
        var externalOpenButton = GetByTestId("external-open-button");

        var initialFrames = await CaptureVisiblePopupSizingFramesAsync(trigger, externalOpenButton);
        Assert.NotEmpty(initialFrames);
        Assert.All(initialFrames, frame =>
        {
            Assert.Equal(5, (int)frame[1]);
            Assert.Equal(1, (int)frame[3]);
            Assert.InRange(frame[2], 223.5, 224.5);
            Assert.InRange(frame[0], 251.5, 252.5);
        });

        await externalOpenButton.EvaluateAsync("element => element.click()");
        await WaitForSelectClosedAsync();
        await Page.EvaluateAsync(
            "() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
        await trigger.EvaluateAsync("element => { element.style.width = '280px'; }");

        var reopenedFrames = await CaptureVisiblePopupSizingFramesAsync(trigger, externalOpenButton);
        Assert.NotEmpty(reopenedFrames);
        Assert.All(reopenedFrames, frame =>
        {
            Assert.Equal(5, (int)frame[1]);
            Assert.Equal(1, (int)frame[3]);
            Assert.InRange(frame[2], 279.5, 280.5);
            Assert.InRange(frame[0], 307.5, 308.5);
        });
    }

    /// <summary>
    /// Verifies that selecting the final option in a grouped, overflowing list
    /// does not let a later placement replay reset upward scrolling.
    /// </summary>
    [Fact]
    public virtual async Task GroupedBottomSelectionCanScrollBackToTopWithoutPlacementReplay()
    {
        await NavigateAsync(CreateUrl("/tests/select").WithSelectGroups(true));

        await OpenSelectAsync();
        await GetByTestId("select-item-thyme").ClickAsync();
        await WaitForSelectClosedAsync();

        await OpenSelectAsync();
        var list = GetByTestId("select-list");
        await list.EvaluateAsync("element => { element.scrollTop = element.scrollHeight; }");
        var bottomScrollTop = await list.EvaluateAsync<double>("element => element.scrollTop");
        Assert.True(bottomScrollTop > 0, $"Expected an overflowing grouped list, but got scrollTop {bottomScrollTop}.");

        await list.EvaluateAsync("element => element.scrollTo({ top: 0, behavior: 'auto' })");
        await Page.WaitForTimeoutAsync(1200);

        var finalScrollTop = await list.EvaluateAsync<double>("element => element.scrollTop");
        Assert.InRange(finalScrollTop, 0, 1);
        await Assertions.Expect(GetByTestId("select-popup")).ToBeVisibleAsync();
        await Assertions.Expect(GetByTestId("select-item-thyme")).ToHaveAttributeAsync("aria-selected", "true");
        await Assertions.Expect(GetByTestId("select-item-apple")).ToBeInViewportAsync();
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
    /// Tests that a generic detail-zero MouseEvent cannot select an unhighlighted item.
    /// </summary>
    [Fact]
    public virtual async Task GenericVirtualClickCannotSelectUnhighlightedItem()
    {
        await NavigateAsync(CreateUrl("/tests/select").WithDefaultOpen(true));
        await WaitForSelectOpenAsync();

        var bananaItem = GetByTestId("select-item-banana");
        await Assertions.Expect(bananaItem).Not.ToHaveAttributeAsync("data-highlighted", "");

        await bananaItem.EvaluateAsync("element => element.dispatchEvent(new MouseEvent('click', { bubbles: true, detail: 0 }))");

        await Assertions.Expect(GetByTestId("selected-value")).ToHaveTextAsync("");
        await Assertions.Expect(GetByTestId("open-state")).ToHaveTextAsync("true");
    }

    /// <summary>
    /// Tests that a virtual PointerEvent carrying pointer metadata can select an unhighlighted item.
    /// </summary>
    [Fact]
    public virtual async Task AssistiveVirtualPointerClickCanSelectUnhighlightedItem()
    {
        await NavigateAsync(CreateUrl("/tests/select").WithDefaultOpen(true));
        await WaitForSelectOpenAsync();

        var bananaItem = GetByTestId("select-item-banana");
        await Assertions.Expect(bananaItem).Not.ToHaveAttributeAsync("data-highlighted", "");

        await bananaItem.EvaluateAsync("element => element.dispatchEvent(new PointerEvent('click', { bubbles: true, detail: 0, pointerType: '' }))");

        await Assertions.Expect(GetByTestId("selected-value")).ToHaveTextAsync("banana");
        await WaitForSelectClosedAsync();
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
    /// Tests that positioner-owned DOM outside the popup is treated as inside the select for outside press dismissal.
    /// </summary>
    [Fact]
    public virtual async Task Js_PositionerOwnedDomDoesNotDismissAsOutsidePress()
    {
        await NavigateAsync(CreateUrl("/tests/select"));

        var outsidePressCalled = await Page.EvaluateAsync<bool>(
            """
            async () => {
                const select = await import('/_content/Blazix.BaseUI/blazix-baseui-select.min.js');
                const rootId = `positioner-containment-${crypto.randomUUID()}`;
                const calls = [];
                const dotNetRef = {
                    invokeMethodAsync: async (name) => {
                        calls.push(name);
                    },
                };
                const trigger = document.createElement('button');
                const positioner = document.createElement('div');
                const popup = document.createElement('div');
                const positionerOwned = document.createElement('div');
                trigger.type = 'button';
                popup.textContent = 'Popup';
                positionerOwned.textContent = 'Positioner-owned';
                positioner.append(popup, positionerOwned);
                document.body.append(trigger, positioner);

                try {
                    select.initializeRoot(rootId, dotNetRef, false, false, 'ltr', false, false);
                    select.setTriggerElement(rootId, trigger);
                    select.initializePopup(rootId, popup, dotNetRef, false);
                    select.setPopupElement(rootId, popup);
                    select.registerPositioner(rootId, positioner);
                    select.setRootOpen(rootId, true, null);

                    positionerOwned.dispatchEvent(new PointerEvent('pointerdown', {
                        pointerId: 1,
                        pointerType: 'mouse',
                        isPrimary: true,
                        button: 0,
                        buttons: 1,
                        bubbles: true,
                        cancelable: true,
                    }));
                    await new Promise((resolve) => setTimeout(resolve, 0));

                    return calls.includes('OnOutsidePress');
                } finally {
                    select.disposeRoot(rootId);
                    positioner.remove();
                    trigger.remove();
                }
            }
            """);

        Assert.False(outsidePressCalled);
    }

    /// <summary>
    /// Tests that inside compatibility mousedown does not overwrite the preceding touch or pen pointer modality.
    /// </summary>
    [Fact]
    public virtual async Task Js_InsideCompatibilityMouseDownPreservesPointerInteractionType()
    {
        await NavigateAsync(CreateUrl("/tests/select"));

        var interactionTypes = await Page.EvaluateAsync<string[]>(
            """
            async () => {
                const select = await import('/_content/Blazix.BaseUI/blazix-baseui-select.min.js');

                async function run(pointerType, includeTouchStart) {
                    const rootId = `inside-modality-${pointerType}-${crypto.randomUUID()}`;
                    const dotNetRef = {
                        invokeMethodAsync: async () => {},
                    };
                    const trigger = document.createElement('button');
                    const positioner = document.createElement('div');
                    const popup = document.createElement('div');
                    const item = document.createElement('div');
                    trigger.type = 'button';
                    item.setAttribute('role', 'option');
                    item.textContent = pointerType;
                    popup.append(item);
                    positioner.append(popup);
                    document.body.append(trigger, positioner);

                    try {
                        select.initializeRoot(rootId, dotNetRef, false, false, 'ltr', false, false);
                        select.setTriggerElement(rootId, trigger);
                        select.initializePopup(rootId, popup, dotNetRef, false);
                        select.setPopupElement(rootId, popup);
                        select.registerPositioner(rootId, positioner);
                        select.setRootOpen(rootId, true, null);

                        item.dispatchEvent(new PointerEvent('pointerdown', {
                            pointerId: 1,
                            pointerType,
                            isPrimary: true,
                            button: 0,
                            buttons: 1,
                            bubbles: true,
                            cancelable: true,
                        }));
                        if (includeTouchStart) {
                            item.dispatchEvent(new TouchEvent('touchstart', {
                                bubbles: true,
                                cancelable: true,
                            }));
                        }
                        item.dispatchEvent(new MouseEvent('mousedown', {
                            button: 0,
                            bubbles: true,
                            cancelable: true,
                        }));
                        await new Promise((resolve) => setTimeout(resolve, 0));

                        return select.getLastInteractionType(rootId);
                    } finally {
                        select.disposeRoot(rootId);
                        positioner.remove();
                        trigger.remove();
                    }
                }

                return [
                    await run('touch', true),
                    await run('pen', false),
                ];
            }
            """);

        Assert.Equal(["touch", "pen"], interactionTypes);
    }

    /// <summary>
    /// Verifies that a renamed root receives the controlled pre-open positioning
    /// result on the next open revision.
    /// </summary>
    [Fact]
    public virtual async Task Js_RenamePreservesControlledReopenReadiness()
    {
        await NavigateAsync(CreateUrl("/tests/select"));

        var result = await Page.EvaluateAsync<bool[]>(
            """
            async () => {
                const select = await import('/_content/Blazix.BaseUI/blazix-baseui-select.js');
                const roots = window[Symbol.for('Blazix.BaseUI.Select.State')].roots;
                const previousRootId = `rename-before-${crypto.randomUUID()}`;
                const nextRootId = `rename-after-${crypto.randomUUID()}`;
                const dotNetRef = { invokeMethodAsync: async () => {} };
                const trigger = document.createElement('button');
                const positioner = document.createElement('div');
                const popup = document.createElement('div');
                const option = document.createElement('div');
                trigger.type = 'button';
                trigger.style.cssText = 'position:absolute;left:20px;top:20px;width:140px;height:32px;';
                positioner.setAttribute('role', 'presentation');
                positioner.setAttribute('data-side', 'bottom');
                positioner.style.cssText = 'width:168px;height:100px;';
                popup.style.cssText = 'width:168px;height:100px;';
                option.setAttribute('role', 'option');
                option.textContent = 'Apple';
                popup.append(option);
                positioner.append(popup);
                document.body.append(trigger, positioner);

                let positionerId = null;
                try {
                    select.initializeRoot(previousRootId, dotNetRef, false, false, 'ltr', false, false);
                    select.setTriggerElement(previousRootId, trigger);
                    select.initializePopup(previousRootId, popup, dotNetRef, false);
                    select.setPopupElement(previousRootId, popup);
                    select.registerPositioner(previousRootId, positioner);

                    positionerId = await select.initializePositioner(
                        positioner, trigger, 'bottom', 'center', 0, 0, 5,
                        'clipping-ancestors', 5, null, false, 'absolute', false,
                        null, true, dotNetRef, previousRootId, false, false, null);
                    select.setRootOpen(previousRootId, true, null);
                    await new Promise((resolve) => requestAnimationFrame(() => requestAnimationFrame(resolve)));

                    const initialRoot = roots.get(previousRootId);
                    const initialReady = initialRoot.positionerReadyElement === positioner &&
                        initialRoot.positionerReadyRevision === initialRoot.openRevision;

                    select.setRootOpen(previousRootId, false, null);
                    trigger.style.top = '300px';
                    await select.updatePosition(
                        positionerId, trigger, 'top', 'center', 0, 0, 5,
                        'clipping-ancestors', 5, null, false, 'absolute', null,
                        true, false, false, null);
                    const closedTransitionRejected = initialRoot.pendingPositionerReadyElement === null &&
                        initialRoot.pendingPositionerReadyRevision === -1;

                    select.renameRoot(previousRootId, nextRootId);
                    select.resetPositionerForOpen(positionerId, nextRootId);
                    await select.updatePosition(
                        positionerId, trigger, 'bottom', 'center', 0, 0, 5,
                        'clipping-ancestors', 5, null, false, 'absolute', null,
                        true, false, false, null);

                    const renamedRoot = roots.get(nextRootId);
                    const pendingReady = renamedRoot.pendingPositionerReadyElement === positioner &&
                        renamedRoot.pendingPositionerReadyRevision === renamedRoot.openRevision + 1;

                    select.setRootOpen(nextRootId, true, null);
                    await new Promise((resolve) => requestAnimationFrame(() => requestAnimationFrame(resolve)));
                    const reopenedReady = renamedRoot.positionerReadyElement === positioner &&
                        renamedRoot.positionerReadyRevision === renamedRoot.openRevision;

                    return [initialReady, closedTransitionRejected, pendingReady, reopenedReady];
                } finally {
                    if (positionerId) select.disposePositioner(positionerId);
                    select.disposeRoot(nextRootId);
                    select.disposeRoot(previousRootId);
                    positioner.remove();
                    trigger.remove();
                }
            }
            """);

        Assert.Equal([true, true, true, true], result);
    }

    /// <summary>
    /// Verifies that the placement watchdog cannot expose uncommitted intrinsic
    /// geometry when no option exists for the align-item commit.
    /// </summary>
    [Fact]
    public virtual async Task Js_WatchdogDoesNotReleaseVisibilityWithoutPlacementCommit()
    {
        await NavigateAsync(CreateUrl("/tests/select"));

        var positioned = await Page.EvaluateAsync<bool>(
            """
            async () => {
                const select = await import('/_content/Blazix.BaseUI/blazix-baseui-select.js');
                const roots = window[Symbol.for('Blazix.BaseUI.Select.State')].roots;
                const rootId = `watchdog-${crypto.randomUUID()}`;
                const dotNetRef = { invokeMethodAsync: async () => {} };
                const trigger = document.createElement('button');
                const positioner = document.createElement('div');
                const popup = document.createElement('div');
                trigger.type = 'button';
                trigger.style.cssText = 'position:absolute;left:20px;top:0;width:140px;height:32px;';
                positioner.setAttribute('data-side', 'none');
                positioner.style.cssText = 'position:fixed;left:20px;top:60px;width:168px;height:100px;';
                popup.style.cssText = 'width:168px;height:100px;';
                positioner.append(popup);
                document.body.append(trigger, positioner);

                try {
                    select.initializeRoot(rootId, dotNetRef, false, false, 'ltr', false, false);
                    select.setTriggerElement(rootId, trigger);
                    select.initializePopup(rootId, popup, dotNetRef, false);
                    select.setPopupElement(rootId, popup);
                    select.registerPositioner(rootId, positioner);
                    select.setRootOpen(rootId, true, null);

                    const root = roots.get(rootId);
                    root.popup.alignItemWithTriggerActive = true;
                    root.popup.dotNetRef = null;
                    root.positionerReadyElement = positioner;
                    root.positionerReadyRevision = root.openRevision;

                    await new Promise((resolve) => setTimeout(resolve, 400));
                    return positioner.hasAttribute('data-positioned');
                } finally {
                    select.disposeRoot(rootId);
                    positioner.remove();
                    trigger.remove();
                }
            }
            """);

        Assert.False(positioned);
    }

    /// <summary>
    /// Verifies that an align-item collision fallback becomes visible only
    /// after the standard Floating UI pass has committed final sizing.
    /// </summary>
    [Fact]
    public virtual async Task Js_AlignItemFallbackCommitsStandardFloatingPlacement()
    {
        await NavigateAsync(CreateUrl("/tests/select"));

        var result = await Page.EvaluateAsync<bool[]>(
            """
            async () => {
                const select = await import('/_content/Blazix.BaseUI/blazix-baseui-select.js');
                const rootId = `fallback-commit-${crypto.randomUUID()}`;
                const calls = [];
                const dotNetRef = {
                    invokeMethodAsync: async (name) => {
                        calls.push(name);
                    },
                };
                const trigger = document.createElement('button');
                const positioner = document.createElement('div');
                const popup = document.createElement('div');
                const option = document.createElement('div');
                trigger.type = 'button';
                trigger.style.cssText = 'position:fixed;left:20px;top:0;width:140px;height:32px;';
                positioner.setAttribute('role', 'presentation');
                positioner.setAttribute('data-side', 'bottom');
                popup.style.cssText = 'width:168px;height:100px;';
                option.setAttribute('role', 'option');
                option.textContent = 'Apple';
                popup.append(option);
                positioner.append(popup);
                document.body.append(trigger, positioner);

                let positionerId = null;
                try {
                    select.initializeRoot(rootId, dotNetRef, false, false, 'ltr', false, false);
                    select.setTriggerElement(rootId, trigger);
                    select.initializePopup(rootId, popup, dotNetRef, false);
                    select.setPopupElement(rootId, popup);
                    select.registerPositioner(rootId, positioner);

                    positionerId = await select.initializePositioner(
                        positioner, trigger, 'bottom', 'center', 0, 0, 5,
                        'clipping-ancestors', 5, null, false, 'absolute', true,
                        null, true, dotNetRef, rootId, false, false, null, false);
                    select.setRootOpen(rootId, true, null);
                    select.beginAlignItemWithTriggerPlacement(rootId, true);

                    const deadline = performance.now() + 2000;
                    while (!positioner.hasAttribute('data-positioned') && performance.now() < deadline) {
                        await new Promise((resolve) => requestAnimationFrame(resolve));
                    }

                    const rect = positioner.getBoundingClientRect();
                    return [
                        calls.includes('OnFallbackToAlignPopupToTrigger'),
                        positioner.hasAttribute('data-positioned'),
                        positioner.getAttribute('data-side') !== 'none',
                        positioner.style.getPropertyValue('--anchor-width') === '140px',
                        positioner.style.getPropertyValue('--available-height').length > 0,
                        rect.width > 0 && rect.height > 0,
                    ];
                } finally {
                    if (positionerId) select.disposePositioner(positionerId);
                    select.disposeRoot(rootId);
                    positioner.remove();
                    trigger.remove();
                }
            }
            """);

        Assert.Equal([true, true, true, true, true, true], result);
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
                const select = await import('/_content/Blazix.BaseUI/blazix-baseui-select.js');
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
