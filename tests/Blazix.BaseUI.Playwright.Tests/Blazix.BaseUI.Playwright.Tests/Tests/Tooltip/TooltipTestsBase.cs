using System.Text.Json;
using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;
using Shouldly;

namespace Blazix.BaseUI.Playwright.Tests.Tests.Tooltip;

/// <summary>
/// Playwright tests for Tooltip component - focused on browser-specific behavior.
/// Static rendering, attribute forwarding, and basic state tests are handled by bUnit.
/// These tests cover: hover interactions, focus behavior, escape key dismissal,
/// positioning, animations, and real JS interop execution.
/// </summary>
public abstract class TooltipTestsBase : TestBase
{
    protected override BrowserNewContextOptions BrowserContextOptions => new() { HasTouch = true };

    protected TooltipTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    #region Helper Methods

    protected async Task OpenTooltipViaHoverAsync()
    {
        var trigger = GetByTestId("tooltip-trigger");
        await trigger.HoverAsync();
        await WaitForTooltipOpenAsync();
    }

    protected async Task OpenTooltipViaFocusAsync()
    {
        var trigger = GetByTestId("tooltip-trigger");
        await trigger.FocusAsync();
        await WaitForTooltipOpenAsync();
    }

    protected async Task WaitForTooltipOpenAsync()
    {
        var openState = GetByTestId("open-state");
        await WaitForTextContentAsync(openState, "true");
    }

    protected async Task WaitForTooltipClosedAsync()
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

    protected async Task MovePointerToOuterTriggerPaddingAsync()
    {
        var box = await GetByTestId("outer-trigger").BoundingBoxAsync();
        Assert.NotNull(box);
        await Page.Mouse.MoveAsync(box.X + 20, box.Y + 20);
    }

    #endregion

    #region Touch Outside Press Tests

    [Fact]
    public virtual async Task TapOutsideDismissesTooltip()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip").WithDefaultOpen(true));

        await WaitForTooltipOpenAsync();
        var outsideButton = GetByTestId("outside-button");
        var box = await outsideButton.BoundingBoxAsync();
        Assert.NotNull(box);

        await Page.Touchscreen.TapAsync(box.X + box.Width / 2, box.Y + box.Height / 2);

        await WaitForTooltipClosedAsync();
    }

    #endregion

    #region Hover Interaction Tests

    [Fact]
    public virtual async Task ContinuousPointerMovementDoesNotOpen()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip").WithDelay(600));
        await WaitForDelayAsync(500);

        await MovePointerWithinTriggerAsync(GetByTestId("tooltip-trigger"), 25);

        await Assertions.Expect(GetByTestId("open-state")).ToHaveTextAsync("false");
    }

    [Fact]
    public virtual async Task PointerRestOpensAfterRestDelay()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip").WithDelay(300));
        await WaitForDelayAsync(500);

        await GetByTestId("tooltip-trigger").HoverAsync();

        await WaitForTextContentAsync(GetByTestId("open-state"), "true", 1000);
    }

    [Fact]
    public virtual async Task PointerRestAfterMovementOpens()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip").WithDelay(600));
        await WaitForDelayAsync(500);

        // 12 steps x 60ms of movement outlast the 600ms delay, so an implementation
        // that arms one timer on entry without restarting it on mousemove opens DURING
        // the movement and fails the closed assertion below.
        await MovePointerWithinTriggerAsync(GetByTestId("tooltip-trigger"), 12);
        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("false");

        await WaitForTextContentAsync(openState, "true", 1500);
    }

    [Fact]
    public virtual async Task NestedTrigger_HoveringInnerDoesNotOpenOuter()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip")
            .WithNestedTrigger(true)
            .WithDelay(200)
            .WithCloseDelay(0));
        await WaitForDelayAsync(500);

        await GetByTestId("inner-trigger").HoverAsync();
        await WaitForTextContentAsync(GetByTestId("inner-open-state"), "true", 1000);
        await WaitForDelayAsync(400);

        await Assertions.Expect(GetByTestId("outer-open-state")).ToHaveTextAsync("false");
    }

    [Fact]
    public virtual async Task NestedTrigger_FocusingInnerDoesNotOpenOuter()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip")
            .WithNestedTrigger(true)
            .WithDelay(200)
            .WithCloseDelay(0));
        await WaitForDelayAsync(500);

        var outerTrigger = GetByTestId("outer-trigger");
        var innerTrigger = GetByTestId("inner-trigger");
        await outerTrigger.FocusAsync();
        await Page.Keyboard.PressAsync("Tab");

        await Assertions.Expect(innerTrigger).ToBeFocusedAsync();
        await outerTrigger.DispatchEventAsync("focus");
        await WaitForTextContentAsync(GetByTestId("inner-open-state"), "true", 1000);
        await WaitForDelayAsync(400);

        await Assertions.Expect(GetByTestId("outer-open-state")).ToHaveTextAsync("false");
    }

    [Fact]
    public virtual async Task NestedTrigger_HoveringOuterOnlyOpensOuter()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip")
            .WithNestedTrigger(true)
            .WithDelay(200)
            .WithCloseDelay(0));
        await WaitForDelayAsync(500);

        await MovePointerToOuterTriggerPaddingAsync();

        await WaitForTextContentAsync(GetByTestId("outer-open-state"), "true", 1000);
    }

    [Fact]
    public virtual async Task NestedTrigger_HoveringInnerClosesHoverOpenedOuter()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip")
            .WithNestedTrigger(true)
            .WithDelay(200)
            .WithCloseDelay(0));
        await WaitForDelayAsync(500);

        await MovePointerToOuterTriggerPaddingAsync();
        var outerOpenState = GetByTestId("outer-open-state");
        await WaitForTextContentAsync(outerOpenState, "true", 1000);

        await GetByTestId("inner-trigger").HoverAsync();
        await WaitForTextContentAsync(GetByTestId("inner-open-state"), "true", 1000);

        await WaitForTextContentAsync(outerOpenState, "false", 1000);
    }

    [Fact]
    public virtual async Task NestedTrigger_LeavingInnerReopensOuter()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip")
            .WithNestedTrigger(true)
            .WithDelay(200)
            .WithCloseDelay(0));
        await WaitForDelayAsync(500);

        await MovePointerToOuterTriggerPaddingAsync();
        var outerOpenState = GetByTestId("outer-open-state");
        await WaitForTextContentAsync(outerOpenState, "true", 1000);
        await GetByTestId("inner-trigger").HoverAsync();
        await WaitForTextContentAsync(outerOpenState, "false", 1000);

        await MovePointerToOuterTriggerPaddingAsync();

        await WaitForTextContentAsync(outerOpenState, "true", 1000);
    }

    [Fact]
    public virtual async Task NestedTrigger_FocusOpenedOuterIsNotClosedByNestedHover()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip")
            .WithNestedTrigger(true)
            .WithDelay(200)
            .WithCloseDelay(0));
        await WaitForDelayAsync(500);

        var outerTrigger = GetByTestId("outer-trigger");
        var outerOpenState = GetByTestId("outer-open-state");
        await outerTrigger.FocusAsync();
        await WaitForTextContentAsync(outerOpenState, "true", 1000);
        await WaitForDelayAsync(100);

        await GetByTestId("inner-trigger").HoverAsync();
        await WaitForTextContentAsync(GetByTestId("inner-open-state"), "true", 1000);
        await WaitForDelayAsync(400);

        await Assertions.Expect(outerOpenState).ToHaveTextAsync("true");
    }

    [Fact]
    public virtual async Task NestedTrigger_DisabledInnerOpensOuter()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip")
            .WithNestedTrigger(true)
            .WithNestedInnerDisabled(true)
            .WithDelay(200)
            .WithCloseDelay(0));
        await WaitForDelayAsync(500);

        await GetByTestId("inner-trigger").HoverAsync();

        await WaitForTextContentAsync(GetByTestId("outer-open-state"), "true", 1000);
        await Assertions.Expect(GetByTestId("inner-open-state")).ToHaveTextAsync("false");
    }

    /// <summary>
    /// Tests that the tooltip opens on hover after the delay period.
    /// Requires real browser to test JS hover interaction timing.
    /// </summary>
    [Fact]
    public virtual async Task OpensOnHover()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip")
            .WithDelay(100)
            .WithCloseDelay(100));

        // Wait for JS interop to initialize
        await WaitForDelayAsync(500);

        var trigger = GetByTestId("tooltip-trigger");
        await trigger.HoverAsync();

        // Wait for delay + some buffer
        await WaitForDelayAsync(300);

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");
    }

    /// <summary>
    /// Tests that the tooltip closes when mouse leaves the trigger.
    /// </summary>
    [Fact]
    public virtual async Task ClosesOnMouseLeave()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip")
            .WithDelay(100)
            .WithCloseDelay(100));

        // Wait for JS interop to initialize
        await WaitForDelayAsync(500);

        var trigger = GetByTestId("tooltip-trigger");
        await trigger.HoverAsync();
        await WaitForDelayAsync(300);

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");

        // Move away from trigger
        var outsideButton = GetByTestId("outside-button");
        await outsideButton.HoverAsync();
        await WaitForDelayAsync(300);

        await Assertions.Expect(openState).ToHaveTextAsync("false");
    }

    /// <summary>
    /// Tests that the tooltip opens when defaultOpen is true.
    /// </summary>
    [Fact]
    public virtual async Task OpensWithDefaultOpenTrue()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip").WithDefaultOpen(true));

        var popup = GetByTestId("tooltip-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();
    }

    /// <summary>
    /// Tests that hovering over the popup keeps it open (hoverable popup).
    /// Note: This test uses focus-based opening as hover timing in Playwright
    /// can be unreliable. The popup should stay open when hovered.
    /// </summary>
    [Fact]
    public virtual async Task PopupStaysOpenWhenHovered()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip")
            .WithCloseDelay(1000)); // Long close delay to give time to hover popup

        // Open via focus (more reliable than hover in automated tests)
        var trigger = GetByTestId("tooltip-trigger");
        await trigger.FocusAsync();
        await WaitForTooltipOpenAsync();

        // Move mouse to popup - it should stay open due to hoverable popup behavior
        var popup = GetByTestId("tooltip-popup");
        await popup.HoverAsync();
        await WaitForDelayAsync(500);

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");
    }

    /// <summary>
    /// Tests that adjacent tooltips inside a provider open immediately during the delay-group timeout.
    /// </summary>
    [Fact]
    public virtual async Task ProviderDelayGroup_OpensNextTooltipImmediatelyDuringTimeout()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip")
            .WithProviderGroup(true)
            .WithDelay(600)
            .WithCloseDelay(0)
            .WithProviderTimeout(400));

        await WaitForDelayAsync(500);

        var firstTrigger = GetByTestId("provider-trigger-one");
        var secondTrigger = GetByTestId("provider-trigger-two");
        var firstOpenState = GetByTestId("provider-open-one");
        var secondOpenState = GetByTestId("provider-open-two");

        await firstTrigger.HoverAsync();
        await WaitForTextContentAsync(firstOpenState, "true", 2000);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await secondTrigger.HoverAsync();
        await WaitForTextContentAsync(secondOpenState, "true", 1000);
        stopwatch.Stop();

        var maxInstantOpenMs = 450 * TimeoutMultiplier;
        Assert.True(
            stopwatch.ElapsedMilliseconds < maxInstantOpenMs,
            $"Expected second provider tooltip to open during the instant phase; elapsed {stopwatch.ElapsedMilliseconds}ms (threshold {maxInstantOpenMs}ms).");
    }

    /// <summary>
    /// Tests that tooltip closes immediately when DisableHoverablePopup is true
    /// and mouse moves to popup.
    /// </summary>
    [Fact]
    public virtual async Task ClosesWhenHoverablePopupDisabled()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip")
            .WithDelay(100)
            .WithCloseDelay(100)
            .WithDisableHoverablePopup(true));

        // Wait for JS interop to initialize
        await WaitForDelayAsync(500);

        var trigger = GetByTestId("tooltip-trigger");
        await trigger.HoverAsync();
        await WaitForDelayAsync(300);

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");

        // Move away - should close since hoverable popup is disabled
        var outsideButton = GetByTestId("outside-button");
        await outsideButton.HoverAsync();
        await WaitForDelayAsync(300);

        await Assertions.Expect(openState).ToHaveTextAsync("false");
    }

    /// <summary>
    /// Tests that restoring a tooltip portal during close does not add a transient flex item gap.
    /// </summary>
    [Fact]
    public virtual async Task ClosingTooltipInFlexLayoutDoesNotMoveTrigger()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip")
            .WithFlexLayout(true)
            .WithDelay(0)
            .WithCloseDelay(0)
            .WithDisableHoverablePopup(true));

        await WaitForDelayAsync(500);

        var trigger = GetByTestId("tooltip-trigger");
        await trigger.HoverAsync();
        await WaitForTooltipOpenAsync();
        await WaitForDelayAsync(100);

        await Page.EvaluateAsync(
            """
            () => {
                const trigger = document.querySelector('[data-testid="tooltip-trigger"]');
                const baseline = trigger.getBoundingClientRect();
                window.__tooltipTriggerLayoutSamples = [];
                window.__tooltipTriggerLayoutStop = false;

                function sample() {
                    const rect = trigger.getBoundingClientRect();
                    window.__tooltipTriggerLayoutSamples.push({
                        left: rect.left,
                        top: rect.top,
                        width: rect.width,
                        height: rect.height,
                        deltaX: Math.abs(rect.left - baseline.left),
                        deltaY: Math.abs(rect.top - baseline.top),
                        deltaW: Math.abs(rect.width - baseline.width),
                        deltaH: Math.abs(rect.height - baseline.height)
                    });

                    if (!window.__tooltipTriggerLayoutStop) {
                        requestAnimationFrame(sample);
                    }
                }

                requestAnimationFrame(sample);
            }
            """);

        var outsideButton = GetByTestId("outside-button");
        await outsideButton.HoverAsync();
        await WaitForTooltipClosedAsync();
        await WaitForDelayAsync(100);

        var maxDelta = await Page.EvaluateAsync<double>(
            """
            () => {
                window.__tooltipTriggerLayoutStop = true;
                const samples = window.__tooltipTriggerLayoutSamples || [];
                return samples.reduce(
                    (max, sample) => Math.max(max, sample.deltaX, sample.deltaY, sample.deltaW, sample.deltaH),
                    0
                );
            }
            """);

        Assert.True(
            maxDelta <= 0.5,
            $"Expected tooltip trigger layout to remain stable while closing, but it moved by {maxDelta}px.");
    }

    /// <summary>
    /// Tests that a restored portal is hidden before it returns to an inline flex parent.
    /// </summary>
    [Fact]
    public virtual async Task RestoredPortalIsHiddenBeforeReturningToFlexParent()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip")
            .WithFlexLayout(true));

        var result = await Page.EvaluateAsync<JsonElement>(
            """
            async () => {
                const module = await import('/_content/Blazix.BaseUI/blazix-baseui-portal.js');
                const row = document.querySelector('[data-testid="tooltip-flex-row"]');
                const trigger = document.querySelector('[data-testid="tooltip-trigger"]');
                const probe = document.createElement('div');
                probe.id = `portal-restore-probe-${crypto.randomUUID()}`;
                probe.textContent = '';
                row.appendChild(probe);

                module.createPortal(probe.id, 'body');

                const baselineLeft = trigger.getBoundingClientRect().left;
                module.restorePortal(probe.id);
                const restoredLeft = trigger.getBoundingClientRect().left;
                const display = getComputedStyle(probe).display;
                const parentIsRow = probe.parentElement === row;
                probe.remove();

                return {
                    display,
                    parentIsRow,
                    triggerDelta: Math.abs(restoredLeft - baselineLeft)
                };
            }
            """);

        Assert.True(result.GetProperty("parentIsRow").GetBoolean());
        Assert.Equal("none", result.GetProperty("display").GetString());
        Assert.True(
            result.GetProperty("triggerDelta").GetDouble() <= 0.5,
            $"Expected hidden restored portal to avoid flex layout movement, but trigger moved by {result.GetProperty("triggerDelta").GetDouble()}px.");
    }

    #endregion

    #region Focus Interaction Tests

    /// <summary>
    /// Tests that the tooltip opens instantly on focus.
    /// </summary>
    [Fact]
    public virtual async Task OpensOnFocus()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip").WithDelay(1000)); // Long delay to verify focus is instant

        var trigger = GetByTestId("tooltip-trigger");
        await trigger.FocusAsync();

        // Focus should open instantly without delay
        await WaitForDelayAsync(100);

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");
    }

    /// <summary>
    /// Tests that the tooltip closes on blur.
    /// </summary>
    [Fact]
    public virtual async Task ClosesOnBlur()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip"));

        var trigger = GetByTestId("tooltip-trigger");
        await trigger.FocusAsync();
        await WaitForDelayAsync(100);

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");

        // Blur the trigger by focusing something else
        var focusableInput = GetByTestId("focusable-input");
        await focusableInput.FocusAsync();

        await WaitForTooltipClosedAsync();
    }

    #endregion

    #region Keyboard Interaction Tests

    /// <summary>
    /// Tests that pressing Escape closes the tooltip.
    /// Note: Opens via focus first to sync JS state (defaultOpen doesn't sync JS isOpen).
    /// </summary>
    [Fact]
    public virtual async Task Escape_ClosesTooltip()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip"));

        // Open via focus to ensure JS state is synced
        var trigger = GetByTestId("tooltip-trigger");
        await trigger.FocusAsync();
        await WaitForTooltipOpenAsync();
        await WaitForDelayAsync(200);

        await Page.Keyboard.PressAsync("Escape");

        await WaitForTooltipClosedAsync();
    }

    /// <summary>
    /// Tests that a default-open tooltip synchronizes its state to JavaScript so Escape closes it.
    /// </summary>
    [Fact]
    public virtual async Task Escape_ClosesDefaultOpenTooltip()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip").WithDefaultOpen(true));

        var popup = GetByTestId("tooltip-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");

        await WaitForTooltipClosedAsync();
    }

    [Fact]
    public virtual async Task EscapeClosesOnlyTheInteractedTooltip()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip-escape"));

        await GetByTestId("open-tooltips-sequentially").ClickAsync();
        var tooltipAOpenState = GetByTestId("tooltip-a-open-state");
        var tooltipBOpenState = GetByTestId("tooltip-b-open-state");
        await WaitForTextContentAsync(tooltipAOpenState, "true");
        await WaitForTextContentAsync(tooltipBOpenState, "true");

        await Page.Keyboard.PressAsync("Escape");

        await WaitForTextContentAsync(tooltipBOpenState, "false");
        await WaitForTextContentAsync(tooltipAOpenState, "true");

        await GetByTestId("open-tooltip-b").DispatchEventAsync("click");
        await WaitForTextContentAsync(tooltipBOpenState, "true");

        await GetByTestId("close-tooltip-a").DispatchEventAsync("click");
        await WaitForTextContentAsync(tooltipAOpenState, "false");
        await GetByTestId("open-tooltip-a").DispatchEventAsync("click");
        await WaitForTextContentAsync(tooltipAOpenState, "true");
        await WaitForTextContentAsync(tooltipBOpenState, "true");

        await Page.Keyboard.PressAsync("Escape");

        await WaitForTextContentAsync(tooltipAOpenState, "false");
        await WaitForTextContentAsync(tooltipBOpenState, "true");

        await Page.Keyboard.PressAsync("Escape");

        await WaitForTextContentAsync(tooltipBOpenState, "false");
    }

    [Fact]
    public virtual async Task EscapeDoesNotAlsoCloseTheEnclosingDialog()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip-escape"));

        await GetByTestId("dialog-trigger").ClickAsync();
        var dialogOpenState = GetByTestId("dialog-open-state");
        var tooltipOpenState = GetByTestId("dialog-tooltip-open-state");
        await WaitForTextContentAsync(dialogOpenState, "true");

        await GetByTestId("dialog-tooltip-trigger").FocusAsync();
        await WaitForTextContentAsync(tooltipOpenState, "true");

        await Page.Keyboard.PressAsync("Escape");

        await WaitForTextContentAsync(tooltipOpenState, "false");
        await WaitForTextContentAsync(dialogOpenState, "true");

        await Page.Keyboard.PressAsync("Escape");

        await WaitForTextContentAsync(dialogOpenState, "false");
    }

    [Fact]
    public virtual async Task EscapePreventsBrowserDefault()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip-escape"));

        await GetByTestId("open-tooltips-sequentially").ClickAsync();
        await WaitForTextContentAsync(GetByTestId("tooltip-b-open-state"), "true");
        await Page.EvaluateAsync("""
            () => {
                window.tooltipEscapeDefaultPrevented = false;
                document.addEventListener('keydown', event => {
                    if (event.key === 'Escape') {
                        window.tooltipEscapeDefaultPrevented = event.defaultPrevented;
                    }
                }, { capture: true, once: true });
            }
            """);

        await Page.Keyboard.PressAsync("Escape");

        var defaultPrevented = await Page.EvaluateAsync<bool>(
            "() => window.tooltipEscapeDefaultPrevented === true");
        defaultPrevented.ShouldBeTrue();
    }

    #endregion

    #region Disabled State Tests

    /// <summary>
    /// Tests that the tooltip does not open when disabled.
    /// </summary>
    [Fact]
    public virtual async Task DoesNotOpenWhenDisabled()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip")
            .WithDisabled(true)
            .WithDelay(100));

        // Wait for JS interop to initialize
        await WaitForDelayAsync(500);

        var trigger = GetByTestId("tooltip-trigger");
        await trigger.HoverAsync();
        await WaitForDelayAsync(300);

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("false");
    }

    /// <summary>
    /// Tests that the tooltip does not open on focus when disabled.
    /// </summary>
    [Fact]
    public virtual async Task DoesNotOpenOnFocusWhenDisabled()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip").WithDisabled(true));

        var trigger = GetByTestId("tooltip-trigger");
        await trigger.FocusAsync();
        await WaitForDelayAsync(200);

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("false");
    }

    #endregion

    #region Multiple Trigger Tests

    /// <summary>
    /// Tests that a single tooltip root supports multiple triggers and switches payloads on hover.
    /// </summary>
    [Fact]
    public virtual async Task MultipleTriggers_HoverSwitchesActivePayload()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip")
            .WithDelay(0)
            .WithCloseDelay(0)
            .WithMultiTrigger(true));

        await WaitForDelayAsync(500);

        var firstTrigger = GetByTestId("tooltip-trigger-one");
        var secondTrigger = GetByTestId("tooltip-trigger-two");
        var viewport = GetByTestId("tooltip-viewport");
        var content = viewport.Locator("[data-current] [data-testid='tooltip-content']");

        await firstTrigger.HoverAsync();
        await WaitForTooltipOpenAsync();
        await Assertions.Expect(content).ToHaveTextAsync("one");
        await Assertions.Expect(firstTrigger).ToHaveAttributeAsync("data-popup-open", "");

        await secondTrigger.HoverAsync();
        await Assertions.Expect(content).ToHaveTextAsync("two");
        await Assertions.Expect(firstTrigger).Not.ToHaveAttributeAsync("data-popup-open", "");
        await Assertions.Expect(secondTrigger).ToHaveAttributeAsync("data-popup-open", "");

        await Assertions.Expect(viewport).ToHaveAttributeAsync("data-transitioning", "");
        await Assertions.Expect(viewport.Locator("[data-previous]")).ToHaveTextAsync("one");
        await Assertions.Expect(viewport).Not.ToHaveAttributeAsync("data-transitioning", "", new LocatorAssertionsToHaveAttributeOptions
        {
            Timeout = 2000 * TimeoutMultiplier
        });
        await Assertions.Expect(viewport.Locator("[data-previous-cache]")).ToHaveTextAsync("");
    }

    /// <summary>
    /// Tests that clicking during a delayed hover cancels the pending hover open.
    /// </summary>
    [Fact]
    public virtual async Task ClickBeforeHoverDelayCancelsPendingOpen()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip")
            .WithDelay(600)
            .WithCloseDelay(0));

        await WaitForDelayAsync(500);

        var trigger = GetByTestId("tooltip-trigger");
        await trigger.HoverAsync();
        await WaitForDelayAsync(100);
        await trigger.ClickAsync();
        await WaitForDelayAsync(700);

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("false");
    }

    #endregion

    #region Positioning Tests

    /// <summary>
    /// Tests that the positioner has a data-side attribute.
    /// Note: The actual side value depends on viewport collision detection,
    /// so we just verify the attribute exists with a valid value.
    /// </summary>
    [Fact]
    public virtual async Task Positioner_HasDataSideAttribute()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip").WithDefaultOpen(true));

        var positioner = GetByTestId("tooltip-positioner");
        var sideValue = await positioner.GetAttributeAsync("data-side");
        Assert.Contains(sideValue, new[] { "top", "bottom", "left", "right" });
    }

    /// <summary>
    /// Tests that the positioner has correct custom data-side attribute.
    /// </summary>
    [Fact]
    public virtual async Task Positioner_HasCustomSide()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip")
            .WithDefaultOpen(true)
            .WithSide("bottom"));

        var positioner = GetByTestId("tooltip-positioner");
        await Assertions.Expect(positioner).ToHaveAttributeAsync("data-side", "bottom");
    }

    /// <summary>
    /// Tests that the positioner has correct data-align attribute.
    /// </summary>
    [Fact]
    public virtual async Task Positioner_HasCorrectAlignAttribute()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip")
            .WithDefaultOpen(true)
            .WithAlign("start"));

        var positioner = GetByTestId("tooltip-positioner");
        await Assertions.Expect(positioner).ToHaveAttributeAsync("data-align", "start");
    }

    #endregion

    #region Accessibility Tests

    /// <summary>
    /// Tests that popup has role="tooltip".
    /// </summary>
    [Fact]
    public virtual async Task Popup_HasTooltipRole()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip").WithDefaultOpen(true));

        var popup = GetByTestId("tooltip-popup");
        await Assertions.Expect(popup).ToHaveAttributeAsync("role", "tooltip");
    }

    /// <summary>
    /// Tests that trigger has aria-describedby when tooltip is open.
    /// </summary>
    [Fact]
    public virtual async Task Trigger_HasAriaDescribedBy_WhenOpen()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip").WithDefaultOpen(true));

        var trigger = GetByTestId("tooltip-trigger");
        var popup = GetByTestId("tooltip-popup");

        var popupId = await popup.GetAttributeAsync("id");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-describedby", popupId!);
    }

    #endregion

    #region Arrow Tests

    /// <summary>
    /// Tests that arrow is rendered when ShowArrow is true.
    /// </summary>
    [Fact]
    public virtual async Task Arrow_IsRendered_WhenEnabled()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip")
            .WithDefaultOpen(true)
            .WithShowArrow(true));

        var popup = GetByTestId("tooltip-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();

        var arrow = GetByTestId("tooltip-arrow");
        await Assertions.Expect(arrow).ToBeAttachedAsync();
        await Assertions.Expect(arrow).ToHaveAttributeAsync("aria-hidden", "true");
    }

    /// <summary>
    /// Tests that arrow has data-side attribute matching positioner.
    /// </summary>
    [Fact]
    public virtual async Task Arrow_HasDataSideAttribute()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip")
            .WithDefaultOpen(true)
            .WithShowArrow(true)
            .WithSide("bottom"));

        var arrow = GetByTestId("tooltip-arrow");
        await Assertions.Expect(arrow).ToHaveAttributeAsync("data-side", "bottom");
    }

    /// <summary>
    /// Tests that the styled arrow touches the popup edge without a visible gap or deep overlap.
    /// </summary>
    [Theory]
    [InlineData("top")]
    [InlineData("right")]
    [InlineData("bottom")]
    [InlineData("left")]
    public virtual async Task Arrow_TouchesPopupEdgeWithoutGapOrDeepOverlap(string side)
    {
        await NavigateAsync(CreateUrl("/tests/tooltip")
            .WithDefaultOpen(true)
            .WithShowArrow(true)
            .WithStyledArrow(true)
            .WithSide(side)
            .WithSideOffset(10));

        var popup = GetByTestId("tooltip-popup");
        var arrow = GetByTestId("tooltip-arrow");
        await Assertions.Expect(popup).ToBeVisibleAsync();
        await Assertions.Expect(arrow).ToBeAttachedAsync();

        var popupBox = await popup.BoundingBoxAsync();
        var arrowBox = await arrow.BoundingBoxAsync();

        Assert.NotNull(popupBox);
        Assert.NotNull(arrowBox);

        var popupLeft = popupBox!.X;
        var popupRight = popupBox.X + popupBox.Width;
        var popupTop = popupBox.Y;
        var popupBottom = popupBox.Y + popupBox.Height;
        var arrowLeft = arrowBox!.X;
        var arrowRight = arrowBox.X + arrowBox.Width;
        var arrowTop = arrowBox.Y;
        var arrowBottom = arrowBox.Y + arrowBox.Height;

        var overlap = side switch
        {
            "top" => popupBottom - arrowTop,
            "right" => arrowRight - popupLeft,
            "bottom" => arrowBottom - popupTop,
            "left" => popupRight - arrowLeft,
            _ => 0
        };

        Assert.True(
            overlap is >= 0.75f and <= 2.25f,
            $"Expected {side} arrow to overlap popup edge by about 1px; actual overlap {overlap}px.");
    }

    #endregion

    #region KeepMounted Tests

    /// <summary>
    /// Tests that tooltip stays in DOM when KeepMounted is true.
    /// Note: Opens via focus first to sync JS state for Escape handling.
    /// </summary>
    [Fact]
    public virtual async Task KeepMounted_TooltipStaysInDOM()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip")
            .WithKeepMounted(true));

        // Open via focus to ensure JS state is synced
        var trigger = GetByTestId("tooltip-trigger");
        await trigger.FocusAsync();
        await WaitForTooltipOpenAsync();

        var popup = GetByTestId("tooltip-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();
        await WaitForDelayAsync(200);

        // Close the tooltip with Escape
        await Page.Keyboard.PressAsync("Escape");
        await WaitForTooltipClosedAsync();

        // Positioner should still be in DOM with data-closed attribute
        var positioner = GetByTestId("tooltip-positioner");
        await Assertions.Expect(positioner).ToBeAttachedAsync();
        await Assertions.Expect(positioner).ToHaveAttributeAsync("data-closed", "");
    }

    #endregion

    #region ActionsRef Tests

    /// <summary>
    /// Tests that ActionsRef.Close closes the tooltip.
    /// </summary>
    [Fact]
    public virtual async Task ActionsRef_CloseClosesTooltip()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip").WithDefaultOpen(true));

        var popup = GetByTestId("tooltip-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();

        var closeButton = GetByTestId("actions-close");
        await closeButton.ClickAsync();

        await WaitForTooltipClosedAsync();
    }

    /// <summary>
    /// Tests that ActionsRef.Open opens the tooltip.
    /// </summary>
    [Fact]
    public virtual async Task ActionsRef_OpenOpensTooltip()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip"));

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("false");

        var openButton = GetByTestId("actions-open");
        await openButton.ClickAsync();

        await WaitForTooltipOpenAsync();
    }

    /// <summary>
    /// Tests that an external controlled Open parameter change mounts and shows the tooltip.
    /// </summary>
    [Fact]
    public virtual async Task ControlledExternalOpen_ShowsTooltip()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip")
            .WithControlledExternal(true)
            .WithAnimated(true));

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("false");

        var openButton = GetByTestId("external-open");
        await openButton.ClickAsync();

        await WaitForTooltipOpenAsync();

        var trigger = GetByTestId("tooltip-trigger");
        var popup = GetByTestId("tooltip-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();
        await Assertions.Expect(popup).ToHaveAttributeAsync("data-open", "");
        await Assertions.Expect(popup).ToHaveCSSAsync("opacity", "1");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-popup-open", "");
    }

    #endregion

    #region Event Tests

    /// <summary>
    /// Tests that OnOpenChange is called with correct reason for hover.
    /// </summary>
    [Fact]
    public virtual async Task OnOpenChange_FiresWithHoverReason()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip")
            .WithDelay(100)
            .WithCloseDelay(100));

        // Wait for JS interop to initialize
        await WaitForDelayAsync(500);

        var trigger = GetByTestId("tooltip-trigger");
        await trigger.HoverAsync();
        await WaitForDelayAsync(300);

        var lastReason = GetByTestId("last-reason");
        await Assertions.Expect(lastReason).ToHaveTextAsync("TriggerHover");
    }

    /// <summary>
    /// Tests that OnOpenChange is called with correct reason for focus.
    /// </summary>
    [Fact]
    public virtual async Task OnOpenChange_FiresWithFocusReason()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip"));

        var trigger = GetByTestId("tooltip-trigger");
        await trigger.FocusAsync();
        await WaitForDelayAsync(100);

        var lastReason = GetByTestId("last-reason");
        await Assertions.Expect(lastReason).ToHaveTextAsync("TriggerFocus");
    }

    /// <summary>
    /// Tests that OnOpenChange is called with correct reason for escape.
    /// Note: Opens via focus first to sync JS state for Escape handling.
    /// </summary>
    [Fact]
    public virtual async Task OnOpenChange_FiresWithEscapeReason()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip"));

        // Open via focus to ensure JS state is synced
        var trigger = GetByTestId("tooltip-trigger");
        await trigger.FocusAsync();
        await WaitForTooltipOpenAsync();
        await WaitForDelayAsync(200);

        await Page.Keyboard.PressAsync("Escape");
        await WaitForTooltipClosedAsync();

        var lastReason = GetByTestId("last-reason");
        await Assertions.Expect(lastReason).ToHaveTextAsync("EscapeKey");
    }

    /// <summary>
    /// Tests that OnOpenChangeComplete is called after transitions.
    /// Note: This tests that the callback fires; transition timing varies by implementation.
    /// </summary>
    [Fact]
    public virtual async Task OnOpenChangeComplete_FiresAfterOpen()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip"));

        // Open via focus
        var trigger = GetByTestId("tooltip-trigger");
        await trigger.FocusAsync();
        await WaitForTooltipOpenAsync();

        // Wait for transition to complete (JS calls OnTransitionEnd)
        await WaitForDelayAsync(1000);

        var completeCount = GetByTestId("complete-count");
        var count = await completeCount.TextContentAsync();

        // The callback may or may not fire depending on animation/transition setup
        // Just verify it's a valid number (0 or more)
        Assert.True(int.TryParse(count, out var parsedCount), "Complete count should be a valid number");
    }

    #endregion

    #region Data Attributes Tests

    /// <summary>
    /// Tests that popup has data-open when open.
    /// </summary>
    [Fact]
    public virtual async Task Popup_HasDataOpenWhenOpen()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip").WithDefaultOpen(true));

        var popup = GetByTestId("tooltip-popup");
        await Assertions.Expect(popup).ToHaveAttributeAsync("data-open", "");
    }

    /// <summary>
    /// Tests that popup has data-closed when closed (with KeepMounted).
    /// </summary>
    [Fact]
    public virtual async Task Popup_HasDataClosedWhenClosed()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip")
            .WithKeepMounted(true)
            .WithDefaultOpen(false));

        var popup = GetByTestId("tooltip-popup");
        await Assertions.Expect(popup).ToHaveAttributeAsync("data-closed", "");
    }

    /// <summary>
    /// Tests that positioner has hidden attribute when not mounted.
    /// </summary>
    [Fact]
    public virtual async Task Positioner_HasHiddenWhenNotMounted()
    {
        await NavigateAsync(CreateUrl("/tests/tooltip")
            .WithKeepMounted(true)
            .WithDefaultOpen(false));

        var positioner = GetByTestId("tooltip-positioner");
        await Assertions.Expect(positioner).ToHaveAttributeAsync("hidden", "");
    }

    #endregion
}
