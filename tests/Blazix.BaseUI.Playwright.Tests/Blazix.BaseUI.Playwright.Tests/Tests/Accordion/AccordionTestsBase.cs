using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;
using Blazix.BaseUI.Utilities;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Tests.Accordion;

/// <summary>
/// Playwright tests for Accordion that require real browser interaction.
/// Static rendering, attribute forwarding, CSS class/style application, and context tests
/// are covered by bUnit tests in Blazix.BaseUI.Tests.
/// </summary>
public abstract class AccordionTestsBase : TestBase
{
    protected AccordionTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    #region Interactive State Tests

    [Fact]
    public virtual async Task UncontrolledOpenState()
    {
        await NavigateAsync(CreateUrl("/tests/accordion"));

        var trigger = GetByTestId("accordion-trigger-1");
        var panel = GetByTestId("accordion-panel-1");

        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(panel).Not.ToBeVisibleAsync();

        await trigger.ClickAsync();
        await WaitForAttributeValueAsync(trigger, "aria-expanded", "true");

        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-panel-open", "");
        await Assertions.Expect(panel).ToBeVisibleAsync();
        await Assertions.Expect(panel).ToHaveAttributeAsync("data-open", "");

        await trigger.ClickAsync();
        await WaitForAttributeValueAsync(trigger, "aria-expanded", "false");

        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Fact]
    public virtual async Task ControlledOpenState()
    {
        await NavigateAsync(CreateUrl("/tests/accordion"));

        var trigger = GetByTestId("accordion-trigger-1");
        var panel = GetByTestId("accordion-panel-1");
        var openValues = GetByTestId("open-values");

        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(openValues).ToHaveTextAsync("");

        await trigger.ClickAsync();
        await WaitForAttributeValueAsync(trigger, "aria-expanded", "true");

        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(panel).ToBeVisibleAsync();
    }

    [Fact]
    public virtual async Task MultipleItemsCanBeOpenWhenMultipleTrue()
    {
        await NavigateAsync(CreateUrl("/tests/accordion").WithMultiple(true));

        var trigger1 = GetByTestId("accordion-trigger-1");
        var trigger2 = GetByTestId("accordion-trigger-2");
        var panel1 = GetByTestId("accordion-panel-1");
        var panel2 = GetByTestId("accordion-panel-2");

        await Assertions.Expect(trigger1).Not.ToHaveAttributeAsync("data-panel-open", "");
        await Assertions.Expect(trigger2).Not.ToHaveAttributeAsync("data-panel-open", "");

        await trigger1.ClickAsync();
        await WaitForAttributeValueAsync(trigger1, "aria-expanded", "true");

        await trigger2.ClickAsync();
        await WaitForAttributeValueAsync(trigger2, "aria-expanded", "true");

        await Assertions.Expect(panel1).ToHaveAttributeAsync("data-open", "");
        await Assertions.Expect(panel2).ToHaveAttributeAsync("data-open", "");
        await Assertions.Expect(trigger1).ToHaveAttributeAsync("data-panel-open", "");
        await Assertions.Expect(trigger2).ToHaveAttributeAsync("data-panel-open", "");
    }

    [Fact]
    public virtual async Task OnlyOneItemOpenWhenMultipleFalse()
    {
        await NavigateAsync(CreateUrl("/tests/accordion").WithMultiple(false));

        var trigger1 = GetByTestId("accordion-trigger-1");
        var trigger2 = GetByTestId("accordion-trigger-2");
        var panel1 = GetByTestId("accordion-panel-1");
        var panel2 = GetByTestId("accordion-panel-2");

        await trigger1.ClickAsync();
        await WaitForAttributeValueAsync(trigger1, "aria-expanded", "true");

        await Assertions.Expect(panel1).ToHaveAttributeAsync("data-open", "");
        await Assertions.Expect(trigger1).ToHaveAttributeAsync("data-panel-open", "");

        await trigger2.ClickAsync();
        await WaitForAttributeValueAsync(trigger2, "aria-expanded", "true");

        await Assertions.Expect(panel2).ToHaveAttributeAsync("data-open", "");
        await Assertions.Expect(trigger2).ToHaveAttributeAsync("data-panel-open", "");
        await Assertions.Expect(trigger1).Not.ToHaveAttributeAsync("data-panel-open", "");
    }

    [Fact]
    public virtual async Task OnValueChangeWithDefaultItemValue()
    {
        await NavigateAsync(CreateUrl("/tests/accordion").WithMultiple(true));

        var trigger1 = GetByTestId("accordion-trigger-1");
        var trigger2 = GetByTestId("accordion-trigger-2");
        var changeCount = GetByTestId("change-count");
        var lastChanged = GetByTestId("last-changed-values");

        await Assertions.Expect(changeCount).ToHaveTextAsync("0");

        // Get the auto-generated values from triggers
        var value1 = await trigger1.GetAttributeAsync("data-value");
        var value2 = await trigger2.GetAttributeAsync("data-value");
        Assert.NotNull(value1);
        Assert.NotNull(value2);

        await trigger1.ClickAsync();
        await WaitForAttributeValueAsync(trigger1, "aria-expanded", "true");

        await Assertions.Expect(changeCount).ToHaveTextAsync("1");
        await Assertions.Expect(lastChanged).ToHaveTextAsync(value1);

        await trigger2.ClickAsync();
        await WaitForAttributeValueAsync(trigger2, "aria-expanded", "true");

        await Assertions.Expect(changeCount).ToHaveTextAsync("2");
        await Assertions.Expect(lastChanged).ToHaveTextAsync($"{value1},{value2}");
    }

    [Fact]
    public virtual async Task OnValueChangeWithCustomItemValue()
    {
        await NavigateAsync(CreateUrl("/tests/accordion")
            .WithMultiple(true)
            .WithUseCustomValues(true));

        var trigger1 = GetByTestId("accordion-trigger-1");
        var trigger2 = GetByTestId("accordion-trigger-2");
        var changeCount = GetByTestId("change-count");
        var lastChanged = GetByTestId("last-changed-values");

        await Assertions.Expect(changeCount).ToHaveTextAsync("0");

        await trigger2.ClickAsync();
        await WaitForAttributeValueAsync(trigger2, "aria-expanded", "true");

        await Assertions.Expect(changeCount).ToHaveTextAsync("1");
        await Assertions.Expect(lastChanged).ToHaveTextAsync("second");

        await trigger1.ClickAsync();
        await WaitForAttributeValueAsync(trigger1, "aria-expanded", "true");

        await Assertions.Expect(changeCount).ToHaveTextAsync("2");
        await Assertions.Expect(lastChanged).ToHaveTextAsync("second,first");
    }

    [Fact]
    public virtual async Task OnValueChangeWhenMultipleFalse()
    {
        await NavigateAsync(CreateUrl("/tests/accordion")
            .WithMultiple(false)
            .WithUseCustomValues(true));

        var trigger1 = GetByTestId("accordion-trigger-1");
        var trigger2 = GetByTestId("accordion-trigger-2");
        var changeCount = GetByTestId("change-count");
        var lastChanged = GetByTestId("last-changed-values");

        await Assertions.Expect(changeCount).ToHaveTextAsync("0");

        await trigger1.ClickAsync();
        await WaitForAttributeValueAsync(trigger1, "aria-expanded", "true");

        await Assertions.Expect(changeCount).ToHaveTextAsync("1");
        await Assertions.Expect(lastChanged).ToHaveTextAsync("first");

        await trigger2.ClickAsync();
        await WaitForAttributeValueAsync(trigger2, "aria-expanded", "true");

        await Assertions.Expect(changeCount).ToHaveTextAsync("2");
        await Assertions.Expect(lastChanged).ToHaveTextAsync("second");
    }

    #endregion

    #region Item State Tests

    [Fact]
    public virtual async Task HasDataOpenWhenOpen()
    {
        await NavigateAsync(CreateUrl("/tests/accordion"));

        var trigger = GetByTestId("accordion-trigger-1");
        await trigger.ClickAsync();
        await WaitForAttributeValueAsync(trigger, "aria-expanded", "true");

        var item = GetByTestId("accordion-item-1");
        await Assertions.Expect(item).ToHaveAttributeAsync("data-open", "");
    }

    [Fact]
    public virtual async Task HasDataClosedWhenClosed()
    {
        await NavigateAsync(CreateUrl("/tests/accordion"));

        var item = GetByTestId("accordion-item-1");
        await Assertions.Expect(item).ToHaveAttributeAsync("data-closed", "");
    }

    #endregion

    #region Header State Tests

    [Fact]
    public virtual async Task HasDataOpenWhenOpen_Header()
    {
        await NavigateAsync(CreateUrl("/tests/accordion"));

        var trigger = GetByTestId("accordion-trigger-1");
        await trigger.ClickAsync();
        await WaitForAttributeValueAsync(trigger, "aria-expanded", "true");

        var header = GetByTestId("accordion-header-1");
        await Assertions.Expect(header).ToHaveAttributeAsync("data-open", "");
    }

    [Fact]
    public virtual async Task HasDataClosedWhenClosed_Header()
    {
        await NavigateAsync(CreateUrl("/tests/accordion"));

        var header = GetByTestId("accordion-header-1");
        await Assertions.Expect(header).ToHaveAttributeAsync("data-closed", "");
    }

    #endregion

    #region Trigger State Tests

    [Fact]
    public virtual async Task HasAriaExpandedTrueWhenOpen()
    {
        await NavigateAsync(CreateUrl("/tests/accordion"));

        var trigger = GetByTestId("accordion-trigger-1");
        await trigger.ClickAsync();
        await WaitForAttributeValueAsync(trigger, "aria-expanded", "true");

        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
    }

    [Fact]
    public virtual async Task HasAriaControlsWhenOpen()
    {
        await NavigateAsync(CreateUrl("/tests/accordion"));

        var trigger = GetByTestId("accordion-trigger-1");
        await trigger.ClickAsync();
        await WaitForAttributeValueAsync(trigger, "aria-expanded", "true");

        var panel = GetByTestId("accordion-panel-1");

        var panelId = await panel.GetAttributeAsync("id");
        Assert.False(string.IsNullOrEmpty(panelId), "Panel should have an ID");

        var ariaControls = await trigger.GetAttributeAsync("aria-controls");
        Assert.NotNull(ariaControls);
        Assert.Equal(panelId, ariaControls);
    }

    [Fact]
    public virtual async Task HasDataPanelOpenWhenOpen()
    {
        await NavigateAsync(CreateUrl("/tests/accordion"));

        var trigger = GetByTestId("accordion-trigger-1");
        await trigger.ClickAsync();
        await WaitForAttributeValueAsync(trigger, "aria-expanded", "true");

        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-panel-open", "");
    }

    [Fact]
    public virtual async Task TogglesOnClick()
    {
        await NavigateAsync(CreateUrl("/tests/accordion"));

        var trigger = GetByTestId("accordion-trigger-1");
        var panel = GetByTestId("accordion-panel-1");

        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");

        await trigger.ClickAsync();
        await WaitForAttributeValueAsync(trigger, "aria-expanded", "true");

        await Assertions.Expect(panel).ToBeVisibleAsync();
        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-panel-open", "");

        await trigger.ClickAsync();
        await WaitForAttributeValueAsync(trigger, "aria-expanded", "false");

        await Assertions.Expect(trigger).Not.ToHaveAttributeAsync("data-panel-open", "");
    }

    [Fact]
    public virtual async Task DisabledTriggerIgnoresClick()
    {
        await NavigateAsync(CreateUrl("/tests/accordion").WithItem1Disabled(true));

        var trigger = GetByTestId("accordion-trigger-1");
        var panel = GetByTestId("accordion-panel-1");

        await trigger.ClickAsync(new LocatorClickOptions { Force = true });
        await Page.WaitForTimeoutAsync(500);

        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(panel).Not.ToBeVisibleAsync();
    }

    #endregion

    #region Panel State Tests

    [Fact]
    public virtual async Task HasDataOpenWhenOpen_Panel()
    {
        await NavigateAsync(CreateUrl("/tests/accordion"));

        // Click to open the accordion
        var trigger = GetByTestId("accordion-trigger-1");
        await trigger.ClickAsync();
        await WaitForAttributeValueAsync(trigger, "aria-expanded", "true");

        var panel = GetByTestId("accordion-panel-1");
        await Assertions.Expect(panel).ToHaveAttributeAsync("data-open", "");
    }

    [Fact]
    public virtual async Task HasDataClosedWhenClosed_Panel()
    {
        await NavigateAsync(CreateUrl("/tests/accordion").WithKeepMounted(true));

        var panel = GetByTestId("accordion-panel-1");
        await Assertions.Expect(panel).ToHaveAttributeAsync("data-closed", "");
    }

    [Fact]
    public virtual async Task IsHiddenWhenClosed()
    {
        await NavigateAsync(CreateUrl("/tests/accordion"));

        var panel = GetByTestId("accordion-panel-1");
        await Assertions.Expect(panel).Not.ToBeVisibleAsync();
    }

    [Fact]
    public virtual async Task IsVisibleWhenOpen()
    {
        await NavigateAsync(CreateUrl("/tests/accordion"));

        // Click to open the accordion
        var trigger = GetByTestId("accordion-trigger-1");
        await trigger.ClickAsync();
        await WaitForAttributeValueAsync(trigger, "aria-expanded", "true");

        var panel = GetByTestId("accordion-panel-1");
        await Assertions.Expect(panel).ToBeVisibleAsync();
    }

    [Fact]
    public virtual async Task AnimatedPanel_ShouldApplyStartingStyle_OnOpen_WhenNotKeepMounted()
    {
        await NavigateAsync(CreateUrl("/tests/accordion")
            .WithAnimated(true)
            .WithAnimationDuration(1000));

        var trigger = GetByTestId("accordion-trigger-1");
        var panel = GetByTestId("accordion-panel-1");

        await Page.EvaluateAsync(@"
            () => {
                const container = document.querySelector('[data-testid=""test-container""]');
                window.__accordionOpenRecords = [];

                const record = (panel, label) => {
                    window.__accordionOpenRecords.push({
                        label,
                        hasStartingStyle: panel.hasAttribute('data-starting-style'),
                        hasEndingStyle: panel.hasAttribute('data-ending-style'),
                        hasOpen: panel.hasAttribute('data-open'),
                        height: panel.getBoundingClientRect().height,
                        scrollHeight: panel.scrollHeight,
                        style: panel.getAttribute('style'),
                    });
                };

                const observePanel = (panel) => {
                    record(panel, 'attached');

                    const panelObserver = new MutationObserver((mutations) => {
                        record(panel, mutations
                            .map((mutation) => mutation.type === 'attributes' ? mutation.attributeName : mutation.type)
                            .join(','));
                    });

                    panelObserver.observe(panel, {
                        attributes: true,
                        attributeFilter: ['style', 'data-open', 'data-closed', 'data-starting-style', 'data-ending-style'],
                    });

                    window.__accordionOpenPanelObserver = panelObserver;
                    requestAnimationFrame(() => record(panel, 'raf-after-attach'));
                };

                window.__accordionOpenContainerObserver = new MutationObserver((mutations) => {
                    for (const mutation of mutations) {
                        for (const node of mutation.addedNodes) {
                            if (node.nodeType !== Node.ELEMENT_NODE) {
                                continue;
                            }

                            const panel = node.matches('[data-testid=""accordion-panel-1""]')
                                ? node
                                : node.querySelector('[data-testid=""accordion-panel-1""]');

                            if (panel) {
                                observePanel(panel);
                            }
                        }
                    }
                });

                window.__accordionOpenContainerObserver.observe(container, { childList: true, subtree: true });
            }
        ");

        await trigger.ClickAsync();
        await Page.WaitForFunctionAsync(
            "() => window.__accordionOpenRecords?.some((record) => record.label === 'attached') === true",
            new PageWaitForFunctionOptions { Timeout = 1000 * TimeoutMultiplier });

        var firstAttachedStartedCollapsed = await Page.EvaluateAsync<bool>(
            @"() => {
                const record = window.__accordionOpenRecords.find((item) => item.label === 'attached');
                return record?.hasStartingStyle === true && record.height < record.scrollHeight;
            }");
        var recordsJson = await Page.EvaluateAsync<string>("() => JSON.stringify(window.__accordionOpenRecords)");

        await WaitForDelayAsync(100);
        var currentHeight = await panel.GetHeightAsync();
        var scrollHeight = await panel.EvaluateAsync<double>("el => el.scrollHeight");

        await Page.EvaluateAsync(@"
            () => {
                window.__accordionOpenPanelObserver?.disconnect();
                window.__accordionOpenContainerObserver?.disconnect();
            }
        ");

        Assert.True(firstAttachedStartedCollapsed, $"Animated accordion panel must attach collapsed with data-starting-style. Records: {recordsJson}");
        Assert.True(currentHeight > 0, $"Panel should begin the open transition. Current: {currentHeight}");
        Assert.True(
            currentHeight < scrollHeight,
            $"Panel should still be transitioning during open. Current: {currentHeight}, ScrollHeight: {scrollHeight}");
    }

    [Fact]
    public virtual async Task KeepsMountedWhenKeepMountedTrue()
    {
        await NavigateAsync(CreateUrl("/tests/accordion").WithKeepMounted(true));

        var panel = GetByTestId("accordion-panel-1");
        await Assertions.Expect(panel).ToBeAttachedAsync();
        await Assertions.Expect(panel).ToHaveAttributeAsync("data-closed", "");

        var trigger = GetByTestId("accordion-trigger-1");
        await trigger.ClickAsync();
        await WaitForAttributeValueAsync(panel, "data-open", "");

        await Assertions.Expect(panel).ToBeVisibleAsync();

        await trigger.ClickAsync();
        await WaitForAttributeValueAsync(panel, "data-closed", "");

        await Assertions.Expect(panel).ToBeAttachedAsync();
    }

    [Fact]
    public virtual async Task HiddenUntilFoundKeepsClosedPanelMountedWithUntilFound()
    {
        await NavigateAsync(CreateUrl("/tests/accordion").WithHiddenUntilFound(true));

        var panel = GetByTestId("accordion-panel-1");
        var trigger = GetByTestId("accordion-trigger-1");

        await Assertions.Expect(panel).ToBeAttachedAsync();
        await Assertions.Expect(panel).ToHaveAttributeAsync("hidden", "until-found");
        await Assertions.Expect(panel).ToHaveAttributeAsync("data-hidden", "");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-hidden", "");

        await trigger.ClickAsync();
        await WaitForAttributeValueAsync(trigger, "aria-expanded", "true");

        await Assertions.Expect(panel).ToBeVisibleAsync();
        await Assertions.Expect(panel).Not.ToHaveAttributeAsync("hidden", "until-found");
        await Assertions.Expect(trigger).Not.ToHaveAttributeAsync("data-hidden", "");
    }

    [Fact]
    public virtual async Task HiddenUntilFoundBeforeMatchOpensDisabledItem()
    {
        await NavigateAsync(CreateUrl("/tests/accordion")
            .WithHiddenUntilFound(true)
            .WithItem1Disabled(true));

        var panel = GetByTestId("accordion-panel-1");
        var trigger = GetByTestId("accordion-trigger-1");

        await Assertions.Expect(panel).ToHaveAttributeAsync("hidden", "until-found");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");

        await panel.EvaluateAsync(@"(el) => {
            const event = new Event('beforematch', { bubbles: true, cancelable: false });
            el.dispatchEvent(event);
        }");

        await WaitForAttributeValueAsync(trigger, "aria-expanded", "true");
        await Assertions.Expect(panel).ToBeVisibleAsync();
        await Assertions.Expect(panel).Not.ToHaveAttributeAsync("hidden", "until-found");
    }

    #endregion

    #region Keyboard Interaction Tests

    [Theory]
    [InlineData("Enter")]
    [InlineData(" ")]
    public virtual async Task KeyToggleAccordionOpenState(string key)
    {
        await NavigateAsync(CreateUrl("/tests/accordion"));

        var trigger = GetByTestId("accordion-trigger-1");
        var panel = GetByTestId("accordion-panel-1");

        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");

        await Page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(trigger).ToBeFocusedAsync();

        await trigger.PressAsync(key);
        await WaitForAttributeValueAsync(trigger, "aria-expanded", "true");

        await Assertions.Expect(panel).ToBeVisibleAsync();
        await Assertions.Expect(panel).ToHaveAttributeAsync("data-open", "");

        await trigger.PressAsync(key);
        await WaitForAttributeValueAsync(trigger, "aria-expanded", "false");
    }

    [Fact]
    public virtual async Task SpaceKeyTogglesOnKeyUp()
    {
        await NavigateAsync(CreateUrl("/tests/accordion"));

        var trigger = GetByTestId("accordion-trigger-1");

        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        await trigger.FocusAsync();

        await Page.Keyboard.DownAsync(" ");
        await Page.WaitForTimeoutAsync(100);

        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");

        await Page.Keyboard.UpAsync(" ");

        await WaitForAttributeValueAsync(trigger, "aria-expanded", "true");
    }

    [Fact]
    public virtual async Task ArrowUpDownDoNotMoveFocusBetweenTriggers()
    {
        await NavigateAsync(CreateUrl("/tests/accordion").WithShowThirdItem(true));

        var trigger1 = GetByTestId("accordion-trigger-1");

        await Page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(trigger1).ToBeFocusedAsync();

        await trigger1.PressAsync("ArrowDown");
        await Page.WaitForTimeoutAsync(100);
        await Assertions.Expect(trigger1).ToBeFocusedAsync();

        await trigger1.PressAsync("ArrowUp");
        await Page.WaitForTimeoutAsync(100);
        await Assertions.Expect(trigger1).ToBeFocusedAsync();
    }

    [Fact]
    public virtual async Task ArrowKeysDoNotMoveFocusAcrossDisabledItems()
    {
        await NavigateAsync(CreateUrl("/tests/accordion")
            .WithShowThirdItem(true)
            .WithItem2Disabled(true));

        var trigger1 = GetByTestId("accordion-trigger-1");
        var trigger3 = GetByTestId("accordion-trigger-3");

        await Page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(trigger1).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(trigger1).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowUp");
        await Assertions.Expect(trigger1).ToBeFocusedAsync();

        await Assertions.Expect(trigger3).Not.ToBeFocusedAsync();
    }

    [Fact]
    public virtual async Task EndKeyDoesNotMoveFocusToLastTrigger()
    {
        await NavigateAsync(CreateUrl("/tests/accordion")
            .WithShowThirdItem(true)
            .WithShowFourthItem(true)
            .WithItem2Disabled(true));

        var trigger1 = GetByTestId("accordion-trigger-1");
        var trigger4 = GetByTestId("accordion-trigger-4");

        await Page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(trigger1).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("End");
        await Assertions.Expect(trigger1).ToBeFocusedAsync();
        await Assertions.Expect(trigger4).Not.ToBeFocusedAsync();
    }

    [Fact]
    public virtual async Task HomeKeyDoesNotMoveFocusToFirstTrigger()
    {
        await NavigateAsync(CreateUrl("/tests/accordion")
            .WithShowThirdItem(true)
            .WithShowFourthItem(true)
            .WithItem2Disabled(true));

        var trigger1 = GetByTestId("accordion-trigger-1");
        var trigger4 = GetByTestId("accordion-trigger-4");

        await trigger4.ClickAsync();
        await Assertions.Expect(trigger4).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Home");
        await Assertions.Expect(trigger4).ToBeFocusedAsync();
        await Assertions.Expect(trigger1).Not.ToBeFocusedAsync();
    }

    [Fact]
    public virtual async Task LoopFocusFalseDoesNotAffectArrowKeys()
    {
        await NavigateAsync(CreateUrl("/tests/accordion").WithLoopFocus(false));

        var trigger1 = GetByTestId("accordion-trigger-1");

        await Page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(trigger1).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(trigger1).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(trigger1).ToBeFocusedAsync();
    }

    [Fact]
    public virtual async Task DoesNotAffectCompositeKeysOnInteractiveElementsInPanel()
    {
        await NavigateAsync(CreateUrl("/tests/accordion")
            .WithShowInput(true));

        // Click to open the accordion
        var trigger = GetByTestId("accordion-trigger-1");
        await trigger.ClickAsync();
        await WaitForAttributeValueAsync(trigger, "aria-expanded", "true");

        var input = GetByTestId("panel-input-1");

        await input.FocusAsync();
        await Assertions.Expect(input).ToBeFocusedAsync();

        // The input should handle arrow keys, not the accordion
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Assertions.Expect(input).ToBeFocusedAsync();
    }

    #endregion

    #region Horizontal Orientation Tests

    [Fact]
    public virtual async Task ArrowLeftRightDoNotMoveFocusInHorizontalOrientation()
    {
        await NavigateAsync(CreateUrl("/tests/accordion")
            .WithHorizontal(true)
            .WithShowThirdItem(true));

        var trigger1 = GetByTestId("accordion-trigger-1");

        await Page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(trigger1).ToBeFocusedAsync();

        await trigger1.PressAsync("ArrowRight");
        await Page.WaitForTimeoutAsync(100);
        await Assertions.Expect(trigger1).ToBeFocusedAsync();

        await trigger1.PressAsync("ArrowLeft");
        await Page.WaitForTimeoutAsync(100);
        await Assertions.Expect(trigger1).ToBeFocusedAsync();
    }

    #endregion
}
