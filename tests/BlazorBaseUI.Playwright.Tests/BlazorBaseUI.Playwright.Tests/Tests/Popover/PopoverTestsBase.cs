using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace BlazorBaseUI.Playwright.Tests.Tests.Popover;

/// <summary>
/// Playwright tests for Popover component - focused on browser-specific behavior.
/// Static rendering, attribute forwarding, and basic state tests are handled by bUnit.
/// These tests cover: click interactions, keyboard navigation, focus management,
/// outside click, escape key, hover behavior, and real JS interop execution.
/// </summary>
public abstract class PopoverTestsBase : TestBase
{
    protected PopoverTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    #region Helper Methods

    protected async Task OpenPopoverAsync()
    {
        var trigger = GetByTestId("popover-trigger");
        await trigger.ClickAsync();
        await WaitForPopoverOpenAsync();
    }

    protected async Task WaitForPopoverOpenAsync()
    {
        var popup = GetByTestId("popover-popup");
        await popup.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    protected async Task WaitForPopoverClosedAsync()
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

    protected Task WaitForSemanticDelayAsync(int milliseconds)
    {
        return Page.WaitForTimeoutAsync(milliseconds);
    }

    #endregion

    #region Popover Open/Close Interaction Tests

    /// <summary>
    /// Tests that clicking the trigger toggles the popover open/closed state.
    /// Requires real browser to test JS interop for positioning and state sync.
    /// </summary>
    [Fact]
    public virtual async Task TogglePopoverOnClick()
    {
        await NavigateAsync(CreateUrl("/tests/popover"));

        var trigger = GetByTestId("popover-trigger");
        var openState = GetByTestId("open-state");

        await trigger.ClickAsync();
        await Assertions.Expect(openState).ToHaveTextAsync("true");

        await trigger.ClickAsync();
        await WaitForPopoverClosedAsync();
    }

    /// <summary>
    /// Tests that the popover opens when defaultOpen is true.
    /// </summary>
    [Fact]
    public virtual async Task OpensWithDefaultOpenTrue()
    {
        await NavigateAsync(CreateUrl("/tests/popover").WithDefaultOpen(true));

        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();
    }

    #endregion

    #region Keyboard Interaction Tests

    /// <summary>
    /// Tests that pressing Escape closes the popover.
    /// Requires real browser keyboard events.
    /// </summary>
    [Fact]
    public virtual async Task Escape_ClosesPopover()
    {
        await NavigateAsync(CreateUrl("/tests/popover").WithDefaultOpen(true));

        // Wait for JS interop to initialize and popover to be visible
        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();
        await WaitForDelayAsync(200);

        // Focus the popup for Escape to be captured
        await popup.FocusAsync();
        await Page.Keyboard.PressAsync("Escape");

        await WaitForPopoverClosedAsync();
    }

    /// <summary>
    /// Tests that Enter key on trigger opens the popover.
    /// </summary>
    [Fact]
    public virtual async Task Enter_OpensPopover()
    {
        await NavigateAsync(CreateUrl("/tests/popover"));

        var trigger = GetByTestId("popover-trigger");
        await trigger.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await WaitForPopoverOpenAsync();
    }

    /// <summary>
    /// Tests that Space key on trigger opens the popover.
    /// </summary>
    [Fact]
    public virtual async Task Space_OpensPopover()
    {
        await NavigateAsync(CreateUrl("/tests/popover"));

        var trigger = GetByTestId("popover-trigger");
        await trigger.FocusAsync();
        await Page.Keyboard.PressAsync(" ");

        await WaitForPopoverOpenAsync();
    }

    #endregion

    #region Outside Click Tests

    /// <summary>
    /// Tests that clicking outside the popover closes it.
    /// Requires real browser to test outside click detection.
    /// Note: Uses click to open because defaultOpen doesn't sync JS state properly.
    /// </summary>
    [Fact]
    public virtual async Task OutsideClick_ClosesPopover()
    {
        await NavigateAsync(CreateUrl("/tests/popover"));

        // Open via click to ensure JS state is synced
        await OpenPopoverAsync();
        await WaitForDelayAsync(200);

        var outsideButton = GetByTestId("outside-button");
        await outsideButton.ClickAsync();

        await WaitForPopoverClosedAsync();
    }

    #endregion

    #region Focus Management Tests

    /// <summary>
    /// Tests that the popup can receive focus (has tabindex).
    /// Note: The popup has tabindex="-1" to allow programmatic focus.
    /// </summary>
    [Fact]
    public virtual async Task PopupCanReceiveFocus()
    {
        await NavigateAsync(CreateUrl("/tests/popover"));

        await OpenPopoverAsync();

        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToHaveAttributeAsync("tabindex", "-1");

        // Verify the popup can be focused programmatically
        await popup.FocusAsync();
        await Assertions.Expect(popup).ToBeFocusedAsync();
    }

    /// <summary>
    /// Tests that the trigger can be focused after popover closes.
    /// Note: Current implementation doesn't automatically return focus to trigger.
    /// This test verifies the trigger is still focusable after close.
    /// </summary>
    [Fact]
    public virtual async Task TriggerIsFocusableAfterClose()
    {
        await NavigateAsync(CreateUrl("/tests/popover").WithShowClose(true));

        await OpenPopoverAsync();

        // Close the popover
        var closeButton = GetByTestId("popover-close");
        await closeButton.ClickAsync();
        await WaitForPopoverClosedAsync();

        // Verify trigger can receive focus
        var trigger = GetByTestId("popover-trigger");
        await trigger.FocusAsync();
        await Assertions.Expect(trigger).ToBeFocusedAsync();
    }

    #endregion

    #region Close Button Tests

    /// <summary>
    /// Tests that clicking the close button closes the popover.
    /// </summary>
    [Fact]
    public virtual async Task CloseButton_ClosesPopover()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithDefaultOpen(true)
            .WithShowClose(true));

        var closeButton = GetByTestId("popover-close");
        await closeButton.ClickAsync();

        await WaitForPopoverClosedAsync();
    }

    #endregion

    #region Hover Behavior Tests

    /// <summary>
    /// Tests that popover opens on hover when OpenOnHover is true.
    /// </summary>
    [Fact]
    public virtual async Task OpenOnHover_OpensPopover()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithOpenOnHover(true)
            .WithOpenDelay(100));

        // Allow JS hover interaction to initialize
        await WaitForDelayAsync(500);

        var trigger = GetByTestId("popover-trigger");
        await trigger.HoverAsync();
        await WaitForDelayAsync(300);

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");
    }

    /// <summary>
    /// Tests that popover closes when mouse leaves when OpenOnHover is true.
    /// </summary>
    [Fact]
    public virtual async Task OpenOnHover_ClosesOnMouseLeave()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithOpenOnHover(true)
            .WithOpenDelay(100)
            .WithCloseDelay(100));

        // Allow JS hover interaction to initialize
        await WaitForDelayAsync(500);

        var trigger = GetByTestId("popover-trigger");
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

    #endregion

    #region Modal Tests

    /// <summary>
    /// Tests that modal popover renders with backdrop.
    /// </summary>
    [Fact]
    public virtual async Task Modal_RendersBackdrop()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithModal(true)
            .WithShowBackdrop(true));

        await OpenPopoverAsync();
        await WaitForDelayAsync(100);

        var backdrop = GetByTestId("popover-backdrop");
        // Check that backdrop is attached and has the expected attributes
        await Assertions.Expect(backdrop).ToBeAttachedAsync();
        await Assertions.Expect(backdrop).ToHaveAttributeAsync("role", "presentation");
        await Assertions.Expect(backdrop).ToHaveAttributeAsync("data-open", "");
    }

    #endregion

    #region Positioning Tests

    /// <summary>
    /// Tests that the positioner has correct default data-side attribute (bottom).
    /// Note: Testing custom side values requires further investigation of query param binding.
    /// </summary>
    [Fact]
    public virtual async Task Positioner_HasDataSideAttribute()
    {
        await NavigateAsync(CreateUrl("/tests/popover").WithDefaultOpen(true));

        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();

        // Default side is "bottom"
        var positioner = GetByTestId("popover-positioner");
        await Assertions.Expect(positioner).ToHaveAttributeAsync("data-side", "bottom");
    }

    /// <summary>
    /// Tests that the positioner has correct data-align attribute.
    /// </summary>
    [Fact]
    public virtual async Task Positioner_HasCorrectAlignAttribute()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithDefaultOpen(true)
            .WithAlign("start"));

        var positioner = GetByTestId("popover-positioner");
        await Assertions.Expect(positioner).ToHaveAttributeAsync("data-align", "start");
    }

    #endregion

    #region Accessibility Tests

    /// <summary>
    /// Tests that trigger has correct aria-expanded attribute.
    /// </summary>
    [Fact]
    public virtual async Task Trigger_HasAriaExpandedFalse_WhenClosed()
    {
        await NavigateAsync(CreateUrl("/tests/popover"));

        var trigger = GetByTestId("popover-trigger");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
    }

    /// <summary>
    /// Tests that trigger has correct aria-expanded attribute when open.
    /// </summary>
    [Fact]
    public virtual async Task Trigger_HasAriaExpandedTrue_WhenOpen()
    {
        await NavigateAsync(CreateUrl("/tests/popover").WithDefaultOpen(true));

        var trigger = GetByTestId("popover-trigger");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
    }

    /// <summary>
    /// Tests that popup has role="dialog".
    /// </summary>
    [Fact]
    public virtual async Task Popup_HasDialogRole()
    {
        await NavigateAsync(CreateUrl("/tests/popover").WithDefaultOpen(true));

        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToHaveAttributeAsync("role", "dialog");
    }

    /// <summary>
    /// Tests that popup is labeled by title when present.
    /// </summary>
    [Fact]
    public virtual async Task Popup_HasAriaLabelledBy_WhenTitlePresent()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithDefaultOpen(true)
            .WithShowTitle(true));

        var popup = GetByTestId("popover-popup");
        var title = GetByTestId("popover-title");

        var titleId = await title.GetAttributeAsync("id");
        await Assertions.Expect(popup).ToHaveAttributeAsync("aria-labelledby", titleId!);
    }

    /// <summary>
    /// Tests that popup is described by description when present.
    /// </summary>
    [Fact]
    public virtual async Task Popup_HasAriaDescribedBy_WhenDescriptionPresent()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithDefaultOpen(true)
            .WithShowDescription(true));

        var popup = GetByTestId("popover-popup");
        var description = GetByTestId("popover-description");

        var descriptionId = await description.GetAttributeAsync("id");
        await Assertions.Expect(popup).ToHaveAttributeAsync("aria-describedby", descriptionId!);
    }

    #endregion

    #region Arrow Tests

    /// <summary>
    /// Tests that arrow is rendered when ShowArrow is true.
    /// </summary>
    [Fact]
    public virtual async Task Arrow_IsRendered_WhenEnabled()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithDefaultOpen(true)
            .WithShowArrow(true));

        // Wait for popup to be visible first
        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();

        // Check arrow is attached and has expected attributes
        var arrow = GetByTestId("popover-arrow");
        await Assertions.Expect(arrow).ToBeAttachedAsync();
        await Assertions.Expect(arrow).ToHaveAttributeAsync("aria-hidden", "true");
        await Assertions.Expect(arrow).ToHaveAttributeAsync("data-open", "");
    }

    /// <summary>
    /// Tests that arrow has data-side attribute.
    /// </summary>
    [Fact]
    public virtual async Task Arrow_HasDataSideAttribute()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithDefaultOpen(true)
            .WithShowArrow(true));

        var arrow = GetByTestId("popover-arrow");
        await Assertions.Expect(arrow).ToHaveAttributeAsync("data-side", "bottom");
    }

    #endregion

    #region KeepMounted Tests

    /// <summary>
    /// Tests that popover stays in DOM when KeepMounted is true.
    /// </summary>
    [Fact]
    public virtual async Task KeepMounted_PopoverStaysInDOM()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithKeepMounted(true)
            .WithShowClose(true));

        // Open the popover
        await OpenPopoverAsync();

        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();

        // Close the popover using the close button (more reliable than Escape)
        var closeButton = GetByTestId("popover-close");
        await closeButton.ClickAsync();
        await WaitForPopoverClosedAsync();

        // Positioner should still be in DOM with data-closed attribute
        var positioner = GetByTestId("popover-positioner");
        await Assertions.Expect(positioner).ToBeAttachedAsync();
        await Assertions.Expect(positioner).ToHaveAttributeAsync("data-closed", "");
    }

    /// <summary>
    /// Tests that SideOffset creates visual gap between trigger and positioner.
    /// </summary>
    [Fact]
    public virtual async Task SideOffset_CreatesGapBetweenTriggerAndPositioner()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithDefaultOpen(true)
            .WithSideOffset(20));

        var trigger = GetByTestId("popover-trigger");
        var positioner = GetByTestId("popover-positioner");

        await Assertions.Expect(positioner).ToBeVisibleAsync();

        var triggerBox = await trigger.BoundingBoxAsync();
        var positionerBox = await positioner.BoundingBoxAsync();

        Assert.NotNull(triggerBox);
        Assert.NotNull(positionerBox);

        // Default side is bottom, so gap is between trigger bottom and positioner top
        var gap = positionerBox!.Y - (triggerBox!.Y + triggerBox.Height);
        Assert.True(gap >= 15, $"Expected gap >= 15px but got {gap}px"); // Allow some tolerance (~20px offset)
    }

    /// <summary>
    /// Tests that AlignOffset shifts popup along the alignment axis.
    /// </summary>
    [Fact]
    public virtual async Task AlignOffset_ShiftsPopupAlongAlignmentAxis()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithDefaultOpen(true)
            .WithAlign("start")
            .WithAlignOffset(15));

        var trigger = GetByTestId("popover-trigger");
        var positioner = GetByTestId("popover-positioner");

        await Assertions.Expect(positioner).ToBeVisibleAsync();

        var triggerBox = await trigger.BoundingBoxAsync();
        var positionerBox = await positioner.BoundingBoxAsync();

        Assert.NotNull(triggerBox);
        Assert.NotNull(positionerBox);

        // With align=start and alignOffset=15, positioner left should be ~15px right of trigger left
        var offset = positionerBox!.X - triggerBox!.X;
        Assert.True(offset >= 10, $"Expected offset >= 10px but got {offset}px"); // Allow tolerance
    }

    /// <summary>
    /// Tests that position is consistent across open/close cycles when KeepMounted is false.
    /// </summary>
    [Fact]
    public virtual async Task Position_ConsistentAcrossCycles_KeepMountedFalse()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithKeepMounted(false)
            .WithShowClose(true));

        // First open
        await OpenPopoverAsync();
        var positioner = GetByTestId("popover-positioner");
        await Assertions.Expect(positioner).ToBeVisibleAsync();
        var firstBox = await positioner.BoundingBoxAsync();

        // Close
        var closeButton = GetByTestId("popover-close");
        await closeButton.ClickAsync();
        await WaitForPopoverClosedAsync();

        // Re-open
        await OpenPopoverAsync();
        positioner = GetByTestId("popover-positioner");
        await Assertions.Expect(positioner).ToBeVisibleAsync();
        var secondBox = await positioner.BoundingBoxAsync();

        Assert.NotNull(firstBox);
        Assert.NotNull(secondBox);
        Assert.True(Math.Abs(firstBox!.X - secondBox!.X) < 2, "X position should be consistent");
        Assert.True(Math.Abs(firstBox.Y - secondBox.Y) < 2, "Y position should be consistent");
    }

    /// <summary>
    /// Tests that position is consistent across open/close cycles when KeepMounted is true.
    /// </summary>
    [Fact]
    public virtual async Task Position_ConsistentAcrossCycles_KeepMountedTrue()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithKeepMounted(true)
            .WithShowClose(true));

        // First open
        await OpenPopoverAsync();
        var positioner = GetByTestId("popover-positioner");
        await Assertions.Expect(positioner).ToBeVisibleAsync();
        var firstBox = await positioner.BoundingBoxAsync();

        // Close
        var closeButton = GetByTestId("popover-close");
        await closeButton.ClickAsync();
        await WaitForPopoverClosedAsync();

        // Re-open
        await OpenPopoverAsync();
        await Assertions.Expect(positioner).ToBeVisibleAsync();
        var secondBox = await positioner.BoundingBoxAsync();

        Assert.NotNull(firstBox);
        Assert.NotNull(secondBox);
        Assert.True(Math.Abs(firstBox!.X - secondBox!.X) < 2, "X position should be consistent");
        Assert.True(Math.Abs(firstBox.Y - secondBox.Y) < 2, "Y position should be consistent");
    }

    #endregion

    #region Trigger Data Attribute Tests

    /// <summary>
    /// Tests that trigger has both data-popup-open and data-pressed when opened via click.
    /// </summary>
    [Fact]
    public virtual async Task Trigger_HasDataPressedWhenOpenViaClick()
    {
        await NavigateAsync(CreateUrl("/tests/popover"));

        var trigger = GetByTestId("popover-trigger");
        await trigger.ClickAsync();
        await WaitForPopoverOpenAsync();

        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-popup-open", "");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-pressed", "");
    }

    /// <summary>
    /// Tests that trigger has data-popup-open but NOT data-pressed when opened via hover.
    /// </summary>
    [Fact]
    public virtual async Task Trigger_NoDataPressedWhenOpenViaHover()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithOpenOnHover(true)
            .WithOpenDelay(100));

        await WaitForDelayAsync(500);

        var trigger = GetByTestId("popover-trigger");
        await trigger.HoverAsync();
        await WaitForDelayAsync(300);

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");

        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-popup-open", "");
        await Assertions.Expect(trigger).Not.ToHaveAttributeAsync("data-pressed", "");
    }

    /// <summary>
    /// Tests that hovering then clicking shows both data-popup-open and data-pressed.
    /// </summary>
    [Fact]
    public virtual async Task Trigger_DataPressedAfterHoverThenClick()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithOpenOnHover(true)
            .WithOpenDelay(0));

        await WaitForDelayAsync(500);

        var trigger = GetByTestId("popover-trigger");
        await trigger.HoverAsync();
        await WaitForSemanticDelayAsync(200);

        // Now click the trigger - should set data-pressed
        await trigger.ClickAsync();
        await WaitForDelayAsync(100);

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-popup-open", "");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-pressed", "");
    }

    #endregion

    #region OnOpenChangeComplete Tests

    /// <summary>
    /// Tests that OnOpenChangeComplete fires after Escape close with defaultOpen=true.
    /// </summary>
    [Fact]
    public virtual async Task OnOpenChangeComplete_FiresOnEscapeClose()
    {
        await NavigateAsync(CreateUrl("/tests/popover").WithDefaultOpen(true));

        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();
        await WaitForDelayAsync(200);

        await popup.FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await WaitForPopoverClosedAsync();

        var completeCount = GetByTestId("complete-count");
        await Assertions.Expect(completeCount).Not.ToHaveTextAsync("0");
    }

    /// <summary>
    /// Tests that OnOpenChangeComplete fires after click to open.
    /// </summary>
    [Fact]
    public virtual async Task OnOpenChangeComplete_FiresOnClickOpen()
    {
        await NavigateAsync(CreateUrl("/tests/popover"));

        var trigger = GetByTestId("popover-trigger");
        await trigger.ClickAsync();
        await WaitForPopoverOpenAsync();

        // Wait for transition to complete
        await WaitForDelayAsync(500);

        var completeCount = GetByTestId("complete-count");
        await Assertions.Expect(completeCount).Not.ToHaveTextAsync("0");
    }

    /// <summary>
    /// Tests that OnOpenChangeComplete is NOT called on initial mount with defaultOpen=true.
    /// </summary>
    [Fact]
    public virtual async Task OnOpenChangeComplete_NotCalledOnMount()
    {
        await NavigateAsync(CreateUrl("/tests/popover").WithDefaultOpen(true));

        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();

        // Immediately check - should be 0 (not called on mount)
        var completeCount = GetByTestId("complete-count");
        await Assertions.Expect(completeCount).ToHaveTextAsync("0");
    }

    #endregion

    #region Multi-Trigger Contained Tests

    /// <summary>
    /// Tests that clicking different contained triggers opens the popover.
    /// </summary>
    [Fact]
    public virtual async Task MultiTrigger_Contained_OpensWithEachTrigger()
    {
        await NavigateAsync(CreateUrl("/tests/popover-multi-trigger"));

        var triggerA = GetByTestId("trigger-a");
        await triggerA.ClickAsync();
        await WaitForDelayAsync(200);

        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();

        // Close
        var closeBtn = GetByTestId("popover-close");
        await closeBtn.ClickAsync();
        var openState = GetByTestId("open-state");
        await WaitForTextContentAsync(openState, "false");

        // Click trigger B
        var triggerB = GetByTestId("trigger-b");
        await triggerB.ClickAsync();
        await WaitForDelayAsync(200);

        await Assertions.Expect(popup).ToBeVisibleAsync();
    }

    /// <summary>
    /// Tests that contained triggers pass correct payload.
    /// </summary>
    [Fact]
    public virtual async Task MultiTrigger_Contained_ShowsCorrectPayload()
    {
        await NavigateAsync(CreateUrl("/tests/popover-multi-trigger")
            .WithShowPayload(true));

        // Note: contained triggers use ChildContent's payload context which displays payload in popup-content
        var triggerA = GetByTestId("trigger-a");
        await triggerA.ClickAsync();
        await WaitForDelayAsync(200);

        var content = GetByTestId("popup-content");
        await Assertions.Expect(content).ToHaveTextAsync("Payload A");
    }

    /// <summary>
    /// Tests that defaultOpen with defaultTriggerId opens the correct trigger.
    /// </summary>
    [Fact]
    public virtual async Task MultiTrigger_Contained_DefaultOpenWithTriggerId()
    {
        await NavigateAsync(CreateUrl("/tests/popover-multi-trigger")
            .WithDefaultOpen(true)
            .WithDefaultTriggerId("trigger-b"));

        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();

        var triggerB = GetByTestId("trigger-b");
        await Assertions.Expect(triggerB).ToHaveAttributeAsync("data-popup-open", "");
    }

    #endregion

    #region Multi-Trigger Detached (Handle) Tests

    /// <summary>
    /// Tests that detached triggers open the popover.
    /// </summary>
    [Fact]
    public virtual async Task MultiTrigger_Handle_OpensWithEachTrigger()
    {
        await NavigateAsync(CreateUrl("/tests/popover-multi-trigger")
            .WithUseHandle(true));

        var triggerA = GetByTestId("trigger-a");
        await triggerA.ClickAsync();
        await WaitForDelayAsync(200);

        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();

        var content = GetByTestId("popup-content");
        await Assertions.Expect(content).ToHaveTextAsync("Content A");

        // Close
        var closeBtn = GetByTestId("popover-close");
        await closeBtn.ClickAsync();
        var openState = GetByTestId("open-state");
        await WaitForTextContentAsync(openState, "false");

        // Click trigger B
        var triggerB = GetByTestId("trigger-b");
        await triggerB.ClickAsync();
        await WaitForDelayAsync(200);

        await Assertions.Expect(popup).ToBeVisibleAsync();
        await Assertions.Expect(content).ToHaveTextAsync("Content B");
    }

    /// <summary>
    /// Tests that detached triggers show correct payload.
    /// </summary>
    [Fact]
    public virtual async Task MultiTrigger_Handle_ShowsCorrectPayload()
    {
        await NavigateAsync(CreateUrl("/tests/popover-multi-trigger")
            .WithUseHandle(true)
            .WithShowPayload(true));

        var triggerA = GetByTestId("trigger-a");
        await triggerA.ClickAsync();
        await WaitForDelayAsync(200);

        var payloadDisplay = GetByTestId("payload-display");
        await Assertions.Expect(payloadDisplay).ToHaveTextAsync("Payload A");
    }

    /// <summary>
    /// Tests that clicking second trigger while open switches content.
    /// </summary>
    [Fact]
    public virtual async Task MultiTrigger_Handle_SwitchesOnSecondTriggerClick()
    {
        await NavigateAsync(CreateUrl("/tests/popover-multi-trigger")
            .WithUseHandle(true));

        var triggerA = GetByTestId("trigger-a");
        await triggerA.ClickAsync();
        await WaitForDelayAsync(200);

        var content = GetByTestId("popup-content");
        await Assertions.Expect(content).ToHaveTextAsync("Content A");

        // Click trigger B while popup is open
        var triggerB = GetByTestId("trigger-b");
        await triggerB.ClickAsync();
        await WaitForDelayAsync(200);

        // Popup should still be open with Content B
        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();
        await Assertions.Expect(content).ToHaveTextAsync("Content B");
    }

    /// <summary>
    /// Tests programmatic open/close via handle buttons.
    /// </summary>
    [Fact]
    public virtual async Task MultiTrigger_Handle_ProgrammaticOpenClose()
    {
        await NavigateAsync(CreateUrl("/tests/popover-multi-trigger")
            .WithUseHandle(true)
            .WithShowProgrammaticButtons(true));

        var openAButton = GetByTestId("open-a-button");
        await openAButton.ClickAsync();
        await WaitForDelayAsync(200);

        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();

        var content = GetByTestId("popup-content");
        await Assertions.Expect(content).ToHaveTextAsync("Content A");

        // Close
        var closeButton = GetByTestId("close-button");
        await closeButton.ClickAsync();

        var openState = GetByTestId("open-state");
        await WaitForTextContentAsync(openState, "false");
    }

    /// <summary>
    /// Tests defaultOpen with detached triggers.
    /// </summary>
    [Fact]
    public virtual async Task MultiTrigger_Handle_DefaultOpenWithTriggerId()
    {
        await NavigateAsync(CreateUrl("/tests/popover-multi-trigger")
            .WithUseHandle(true)
            .WithDefaultOpen(true)
            .WithDefaultTriggerId("trigger-b"));

        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();

        var content = GetByTestId("popup-content");
        await Assertions.Expect(content).ToHaveTextAsync("Content B");
    }

    /// <summary>
    /// Tests that position changes when switching between triggers.
    /// </summary>
    [Fact]
    public virtual async Task MultiTrigger_Handle_PositionChangesOnSwitch()
    {
        await NavigateAsync(CreateUrl("/tests/popover-multi-trigger")
            .WithUseHandle(true));

        var triggerA = GetByTestId("trigger-a");
        await triggerA.ClickAsync();
        await WaitForDelayAsync(300);

        var positioner = GetByTestId("popover-positioner");
        await Assertions.Expect(positioner).ToBeVisibleAsync();
        var posA = await positioner.BoundingBoxAsync();

        // Click trigger B (positioned to the right)
        var triggerB = GetByTestId("trigger-b");
        await triggerB.ClickAsync();
        await WaitForDelayAsync(300);

        await Assertions.Expect(positioner).ToBeVisibleAsync();
        var posB = await positioner.BoundingBoxAsync();

        Assert.NotNull(posA);
        Assert.NotNull(posB);
        // Trigger B is at left:300px vs trigger A at left:50px, so positioner should shift right
        Assert.True(posB!.X > posA!.X, $"Expected positioner to shift right, but posA.X={posA.X} posB.X={posB.X}");
    }

    #endregion

    #region Tab Navigation Tests

    /// <summary>
    /// Tests tab from trigger goes to first focusable element in popup (non-modal, after-trigger layout).
    /// </summary>
    [Fact]
    public virtual async Task Tab_FromTrigger_FocusesPopupFirst()
    {
        await NavigateAsync(CreateUrl("/tests/popover-tab")
            .WithDefaultOpen(true)
            .WithLayout("after-trigger"));

        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();
        await WaitForDelayAsync(200);

        var trigger = GetByTestId("popover-trigger");
        await trigger.FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await WaitForDelayAsync(100);

        var popupFirst = GetByTestId("popup-first");
        await Assertions.Expect(popupFirst).ToBeFocusedAsync();
    }

    /// <summary>
    /// Tests tab from last popup element goes to next element outside popup (non-modal).
    /// </summary>
    [Fact]
    public virtual async Task Tab_FromPopupLast_GoesToAfterButton()
    {
        await NavigateAsync(CreateUrl("/tests/popover-tab")
            .WithDefaultOpen(true)
            .WithLayout("after-trigger"));

        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();
        await WaitForDelayAsync(200);

        var popupLast = GetByTestId("popup-last");
        await popupLast.FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await WaitForDelayAsync(100);

        var afterButton = GetByTestId("after-button");
        await Assertions.Expect(afterButton).ToBeFocusedAsync();
    }

    /// <summary>
    /// Tests tab from trigger goes to between button first (between layout).
    /// </summary>
    [Fact]
    public virtual async Task Tab_BetweenLayout_GoesToBetweenButton()
    {
        await NavigateAsync(CreateUrl("/tests/popover-tab")
            .WithDefaultOpen(true)
            .WithLayout("between"));

        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();
        await WaitForDelayAsync(200);

        var trigger = GetByTestId("popover-trigger");
        await trigger.FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await WaitForDelayAsync(100);

        var betweenButton = GetByTestId("between-button");
        await Assertions.Expect(betweenButton).ToBeFocusedAsync();
    }

    /// <summary>
    /// Tests shift-tab from trigger goes to last popup element (before-trigger layout).
    /// </summary>
    [Fact]
    public virtual async Task ShiftTab_BeforeTriggerLayout_GoesToPopupLast()
    {
        await NavigateAsync(CreateUrl("/tests/popover-tab")
            .WithDefaultOpen(true)
            .WithLayout("before-trigger"));

        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();
        await WaitForDelayAsync(200);

        var trigger = GetByTestId("popover-trigger");
        await trigger.FocusAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        await WaitForDelayAsync(100);

        var popupLast = GetByTestId("popup-last");
        await Assertions.Expect(popupLast).ToBeFocusedAsync();
    }

    #endregion

    #region Nested Popover Tests

    /// <summary>
    /// Tests that closing child popover keeps parent open.
    /// </summary>
    [Fact]
    public virtual async Task Nested_CloseChild_ParentStaysOpen()
    {
        await NavigateAsync(CreateUrl("/tests/popover-nested"));

        // Open parent
        var parentTrigger = GetByTestId("parent-trigger");
        await parentTrigger.ClickAsync();

        var parentPopup = GetByTestId("parent-popup");
        await Assertions.Expect(parentPopup).ToBeVisibleAsync();
        await WaitForDelayAsync(200);

        // Open child
        var childTrigger = GetByTestId("child-trigger");
        await childTrigger.ClickAsync();

        var childPopup = GetByTestId("child-popup");
        await Assertions.Expect(childPopup).ToBeVisibleAsync();
        await WaitForDelayAsync(200);

        // Close child
        var childClose = GetByTestId("child-close");
        await childClose.ClickAsync();

        var childOpenState = GetByTestId("child-open-state");
        await WaitForTextContentAsync(childOpenState, "false");

        // Parent should still be open
        var parentOpenState = GetByTestId("parent-open-state");
        await Assertions.Expect(parentOpenState).ToHaveTextAsync("true");
    }

    /// <summary>
    /// Tests that clicking child trigger keeps parent open.
    /// </summary>
    [Fact]
    public virtual async Task Nested_ClickChildTrigger_ParentStaysOpen()
    {
        await NavigateAsync(CreateUrl("/tests/popover-nested"));

        // Open parent
        var parentTrigger = GetByTestId("parent-trigger");
        await parentTrigger.ClickAsync();

        var parentPopup = GetByTestId("parent-popup");
        await Assertions.Expect(parentPopup).ToBeVisibleAsync();
        await WaitForDelayAsync(200);

        // Click child trigger
        var childTrigger = GetByTestId("child-trigger");
        await childTrigger.ClickAsync();
        await WaitForDelayAsync(200);

        // Parent should still be open
        var parentOpenState = GetByTestId("parent-open-state");
        await Assertions.Expect(parentOpenState).ToHaveTextAsync("true");
    }

    /// <summary>
    /// Tests that closing parent closes both parent and child.
    /// </summary>
    [Fact]
    public virtual async Task Nested_CloseParent_BothClose()
    {
        await NavigateAsync(CreateUrl("/tests/popover-nested"));

        // Open parent
        var parentTrigger = GetByTestId("parent-trigger");
        await parentTrigger.ClickAsync();

        var parentPopup = GetByTestId("parent-popup");
        await Assertions.Expect(parentPopup).ToBeVisibleAsync();
        await WaitForDelayAsync(200);

        // Open child
        var childTrigger = GetByTestId("child-trigger");
        await childTrigger.ClickAsync();

        var childPopup = GetByTestId("child-popup");
        await Assertions.Expect(childPopup).ToBeVisibleAsync();
        await WaitForDelayAsync(200);

        // Close parent
        var parentClose = GetByTestId("parent-close");
        await parentClose.ClickAsync();

        var parentOpenState = GetByTestId("parent-open-state");
        await WaitForTextContentAsync(parentOpenState, "false");

        // Child should also be closed
        await Assertions.Expect(childPopup).Not.ToBeVisibleAsync();
        var childOpenState = GetByTestId("child-open-state");
        await WaitForTextContentAsync(childOpenState, "false");
    }

    #endregion

    #region Impatient Click Tests

    /// <summary>
    /// Tests that an impatient click (within 500ms of hover open) keeps popover open.
    /// </summary>
    [Fact]
    public virtual async Task ImpatientClick_KeepsPopoverOpen()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithOpenOnHover(true)
            .WithOpenDelay(50));

        await WaitForDelayAsync(500);

        var trigger = GetByTestId("popover-trigger");
        await trigger.HoverAsync();
        await WaitForSemanticDelayAsync(200);

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");

        // Click immediately (impatient) - should stick open
        await trigger.ClickAsync();
        await WaitForDelayAsync(200);

        await Assertions.Expect(openState).ToHaveTextAsync("true");
    }

    /// <summary>
    /// Tests that a patient click (after 500ms of hover open) closes popover.
    /// </summary>
    [Fact]
    public virtual async Task PatientClick_ClosesPopover()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithOpenOnHover(true)
            .WithOpenDelay(50));

        await WaitForDelayAsync(500);

        var trigger = GetByTestId("popover-trigger");
        await trigger.HoverAsync();
        await WaitForSemanticDelayAsync(200);

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");

        // Wait >500ms (patient click threshold)
        await WaitForSemanticDelayAsync(600);

        // Patient click - should close
        await trigger.ClickAsync();
        await WaitForPopoverClosedAsync();
    }

    /// <summary>
    /// Tests that impatient click then mouse leave keeps popover open (stuck).
    /// </summary>
    [Fact]
    public virtual async Task ImpatientClick_ThenMouseLeave_StaysOpen()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithOpenOnHover(true)
            .WithOpenDelay(50)
            .WithCloseDelay(100));

        await WaitForDelayAsync(500);

        var trigger = GetByTestId("popover-trigger");
        await trigger.HoverAsync();
        await WaitForSemanticDelayAsync(200);

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");

        // Impatient click - sticks open
        await trigger.ClickAsync();
        await WaitForDelayAsync(100);

        // Move mouse away
        var outsideButton = GetByTestId("outside-button");
        await outsideButton.HoverAsync();
        await WaitForDelayAsync(300);

        // Should still be open (stuck)
        await Assertions.Expect(openState).ToHaveTextAsync("true");
    }

    /// <summary>
    /// Tests that patient click closes popover without sticking.
    /// </summary>
    [Fact]
    public virtual async Task PatientClick_ClosesWithoutSticking()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithOpenOnHover(true)
            .WithOpenDelay(50)
            .WithCloseDelay(100));

        await WaitForDelayAsync(500);

        var trigger = GetByTestId("popover-trigger");
        await trigger.HoverAsync();
        await WaitForSemanticDelayAsync(200);

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");

        // Wait for patient threshold
        await WaitForSemanticDelayAsync(600);

        // Patient click - should close
        await trigger.ClickAsync();
        await WaitForPopoverClosedAsync();
    }

    /// <summary>
    /// Tests that clicking before hover delay fires opens via click and sticks.
    /// </summary>
    [Fact]
    public virtual async Task ClickBeforeHoverDelay_OpensViaClick()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithOpenOnHover(true)
            .WithOpenDelay(500));

        await WaitForDelayAsync(500);

        var trigger = GetByTestId("popover-trigger");
        await trigger.HoverAsync();

        // Click before hover delay (500ms) fires
        await WaitForSemanticDelayAsync(100);
        await trigger.ClickAsync();

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");

        // Should have data-pressed since opened via click
        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-pressed", "");
    }

    /// <summary>
    /// Tests hover open, impatient click (sticks), mouse leave, re-hover, click stays open.
    /// </summary>
    [Fact]
    public virtual async Task ImpatientClick_ReHoverClick_StaysOpen()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithOpenOnHover(true)
            .WithOpenDelay(50)
            .WithCloseDelay(100));

        await WaitForDelayAsync(500);

        var trigger = GetByTestId("popover-trigger");
        var openState = GetByTestId("open-state");

        // Hover to open
        await trigger.HoverAsync();
        await WaitForSemanticDelayAsync(200);
        await Assertions.Expect(openState).ToHaveTextAsync("true");

        // Impatient click (sticks)
        await trigger.ClickAsync();
        await WaitForDelayAsync(100);

        // Mouse leave
        var outsideButton = GetByTestId("outside-button");
        await outsideButton.HoverAsync();
        await WaitForDelayAsync(200);

        // Still open (stuck)
        await Assertions.Expect(openState).ToHaveTextAsync("true");

        // Re-hover trigger
        await trigger.HoverAsync();
        await WaitForSemanticDelayAsync(200);

        // Click - should still keep open
        await trigger.ClickAsync();
        await WaitForDelayAsync(200);

        await Assertions.Expect(openState).ToHaveTextAsync("true");
    }

    /// <summary>
    /// Tests that hover then click shows both data-popup-open and data-pressed.
    /// </summary>
    [Fact]
    public virtual async Task HoverThenClick_ShowsDataPressed()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithOpenOnHover(true)
            .WithOpenDelay(100));

        await WaitForDelayAsync(500);

        var trigger = GetByTestId("popover-trigger");
        await trigger.HoverAsync();
        await WaitForSemanticDelayAsync(300);

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");

        // Click while open
        await trigger.ClickAsync();
        await WaitForDelayAsync(100);

        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-popup-open", "");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-pressed", "");
    }

    #endregion

    #region Focus Management Tests (Focus Page)

    /// <summary>
    /// Tests that default focus goes to first focusable element in popup.
    /// </summary>
    [Fact]
    public virtual async Task Focus_DefaultGoesToFirstFocusableElement()
    {
        await NavigateAsync(CreateUrl("/tests/popover-focus"));

        var trigger = GetByTestId("popover-trigger");
        await trigger.ClickAsync();

        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();
        await WaitForDelayAsync(200);

        var firstButton = GetByTestId("first-button");
        await Assertions.Expect(firstButton).ToBeFocusedAsync();
    }

    /// <summary>
    /// Tests that InitialFocus directs focus to the specified element.
    /// </summary>
    [Fact]
    public virtual async Task Focus_InitialFocusDirectsToSpecifiedElement()
    {
        await NavigateAsync(CreateUrl("/tests/popover-focus")
            .WithPopoverUseInitialFocus(true));

        var trigger = GetByTestId("popover-trigger");
        await trigger.ClickAsync();

        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();
        await WaitForDelayAsync(200);

        var specificButton = GetByTestId("specific-button");
        await Assertions.Expect(specificButton).ToBeFocusedAsync();
    }

    /// <summary>
    /// Tests that default final focus returns to trigger on close.
    /// </summary>
    [Fact]
    public virtual async Task Focus_DefaultFinalFocusReturnToTrigger()
    {
        await NavigateAsync(CreateUrl("/tests/popover-focus"));

        var trigger = GetByTestId("popover-trigger");
        await trigger.ClickAsync();

        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();
        await WaitForDelayAsync(200);

        // Close via close button
        var closeButton = GetByTestId("popover-close");
        await closeButton.ClickAsync();

        var openState = GetByTestId("open-state");
        await WaitForTextContentAsync(openState, "false");
        await WaitForDelayAsync(200);

        await Assertions.Expect(trigger).ToBeFocusedAsync();
    }

    /// <summary>
    /// Tests that FinalFocus directs focus to the specified element on close.
    /// </summary>
    [Fact]
    public virtual async Task Focus_FinalFocusDirectsToSpecifiedElement()
    {
        await NavigateAsync(CreateUrl("/tests/popover-focus")
            .WithPopoverUseFinalFocus(true));

        var trigger = GetByTestId("popover-trigger");
        await trigger.ClickAsync();

        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();
        await WaitForDelayAsync(200);

        // Close via close button
        var closeButton = GetByTestId("popover-close");
        await closeButton.ClickAsync();

        var openState = GetByTestId("open-state");
        await WaitForTextContentAsync(openState, "false");
        await WaitForDelayAsync(200);

        var finalFocusTarget = GetByTestId("final-focus-target");
        await Assertions.Expect(finalFocusTarget).ToBeFocusedAsync();
    }

    /// <summary>
    /// Tests that hover-opened popover does not steal focus.
    /// </summary>
    [Fact]
    public virtual async Task Focus_HoverOpenDoesNotMoveFocus()
    {
        await NavigateAsync(CreateUrl("/tests/popover-focus")
            .WithOpenOnHover(true)
            .WithOpenDelay(100));

        await WaitForDelayAsync(500);

        // Focus something specific first
        var outsideButton = GetByTestId("outside-button");
        await outsideButton.FocusAsync();
        await Assertions.Expect(outsideButton).ToBeFocusedAsync();

        // Hover the trigger
        var trigger = GetByTestId("popover-trigger");
        await trigger.HoverAsync();
        await WaitForSemanticDelayAsync(300);

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");

        // Focus should NOT have moved to popup
        await Assertions.Expect(outsideButton).ToBeFocusedAsync();
    }

    /// <summary>
    /// Tests that hover-open, then click close, returns focus to trigger.
    /// </summary>
    [Fact]
    public virtual async Task Focus_HoverOpenClickCloseReturnsFocusToTrigger()
    {
        await NavigateAsync(CreateUrl("/tests/popover-focus")
            .WithOpenOnHover(true)
            .WithOpenDelay(100));

        await WaitForDelayAsync(500);

        var trigger = GetByTestId("popover-trigger");
        await trigger.HoverAsync();
        await WaitForSemanticDelayAsync(300);

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");

        // Click trigger to engage click mode, then click close
        await trigger.ClickAsync();
        await WaitForDelayAsync(200);

        var closeButton = GetByTestId("popover-close");
        await closeButton.ClickAsync();
        await WaitForTextContentAsync(openState, "false");
        await WaitForDelayAsync(200);

        await Assertions.Expect(trigger).ToBeFocusedAsync();
    }

    #endregion

    #region Viewport Morphing Tests

    /// <summary>
    /// Tests that switching triggers with viewport shows transitioning state.
    /// </summary>
    [Fact]
    public virtual async Task Viewport_SwitchTrigger_TransitionsContent()
    {
        await NavigateAsync(CreateUrl("/tests/popover-multi-trigger")
            .WithUseHandle(true)
            .WithUseViewport(true));

        var triggerA = GetByTestId("trigger-a");
        await triggerA.ClickAsync();
        await WaitForDelayAsync(300);

        var viewport = GetByTestId("popover-viewport");
        await Assertions.Expect(viewport).ToBeVisibleAsync();

        var content = GetByTestId("popup-content");
        await Assertions.Expect(content).ToHaveTextAsync("Content A");

        // Switch to trigger B
        var triggerB = GetByTestId("trigger-b");
        await triggerB.ClickAsync();
        await WaitForDelayAsync(500);

        await Assertions.Expect(content).ToHaveTextAsync("Content B");
    }

    /// <summary>
    /// Tests rapid trigger switches settle on final trigger's content.
    /// </summary>
    [Fact]
    public virtual async Task Viewport_RapidSwitches_SettlesOnFinal()
    {
        await NavigateAsync(CreateUrl("/tests/popover-multi-trigger")
            .WithUseHandle(true)
            .WithUseViewport(true));

        var triggerA = GetByTestId("trigger-a");
        var triggerB = GetByTestId("trigger-b");

        await triggerA.ClickAsync();
        await WaitForDelayAsync(200);

        // Rapid switches
        await triggerB.ClickAsync();
        await WaitForDelayAsync(50);
        await triggerA.ClickAsync();
        await WaitForDelayAsync(500);

        var content = GetByTestId("popup-content");
        await Assertions.Expect(content).ToHaveTextAsync("Content A");
    }

    #endregion

    #region Remaining Focus/Dismiss Tests

    /// <summary>
    /// Tests that Escape close with KeepMounted returns focus to trigger.
    /// </summary>
    [Fact]
    public virtual async Task EscapeClose_KeepMounted_FocusesTrigger()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithKeepMounted(true)
            .WithShowClose(true));

        await OpenPopoverAsync();
        await WaitForDelayAsync(200);

        var popup = GetByTestId("popover-popup");
        await popup.FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await WaitForPopoverClosedAsync();
        await WaitForDelayAsync(200);

        var trigger = GetByTestId("popover-trigger");
        await Assertions.Expect(trigger).ToBeFocusedAsync();
    }

    /// <summary>
    /// Tests that hover-opened popover does not move focus to popup.
    /// </summary>
    [Fact]
    public virtual async Task HoverOpen_DoesNotMoveFocusToPopup()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithOpenOnHover(true)
            .WithOpenDelay(100)
            .WithShowFocusableContent(true));

        await WaitForDelayAsync(500);

        var outsideButton = GetByTestId("outside-button");
        await outsideButton.FocusAsync();

        var trigger = GetByTestId("popover-trigger");
        await trigger.HoverAsync();
        await WaitForDelayAsync(300);

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");

        // Focus should not have moved to popup content
        await Assertions.Expect(outsideButton).ToBeFocusedAsync();
    }

    /// <summary>
    /// Tests non-modal with backdrop closes on backdrop click.
    /// </summary>
    [Fact]
    public virtual async Task NonModal_BackdropClick_Closes()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithShowBackdrop(true));

        await OpenPopoverAsync();
        await WaitForDelayAsync(200);

        var backdrop = GetByTestId("popover-backdrop");
        await backdrop.ClickAsync(new LocatorClickOptions { Force = true });

        await WaitForPopoverClosedAsync();
    }

    /// <summary>
    /// Tests modal with backdrop closes on backdrop click.
    /// </summary>
    [Fact]
    public virtual async Task Modal_BackdropClick_Closes()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithModal(true)
            .WithShowBackdrop(true));

        await OpenPopoverAsync();
        await WaitForDelayAsync(200);

        var backdrop = GetByTestId("popover-backdrop");
        await backdrop.ClickAsync(new LocatorClickOptions { Force = true });

        await WaitForPopoverClosedAsync();
    }

    /// <summary>
    /// Tests non-modal popover closes on outside click.
    /// </summary>
    [Fact]
    public virtual async Task NonModal_OutsideClick_ClosesWithReason()
    {
        await NavigateAsync(CreateUrl("/tests/popover"));

        await OpenPopoverAsync();
        await WaitForDelayAsync(200);

        var outsideButton = GetByTestId("outside-button");
        await outsideButton.ClickAsync();

        await WaitForPopoverClosedAsync();

        var lastReason = GetByTestId("last-reason");
        await Assertions.Expect(lastReason).ToHaveTextAsync("OutsidePress");
    }

    /// <summary>
    /// Tests modal hover popover: hover opens without backdrop, click engages modal.
    /// </summary>
    [Fact]
    public virtual async Task Modal_HoverOpen_ClickEngagesModal()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithModal(true)
            .WithOpenOnHover(true)
            .WithOpenDelay(100)
            .WithShowBackdrop(true));

        await WaitForDelayAsync(500);

        // Hover to open
        var trigger = GetByTestId("popover-trigger");
        await trigger.HoverAsync();
        await WaitForSemanticDelayAsync(300);

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");

        // Backdrop should not be visible during hover-only open
        var backdrop = GetByTestId("popover-backdrop");
        await Assertions.Expect(backdrop).Not.ToBeVisibleAsync();

        // Click trigger to engage modal mode
        await trigger.ClickAsync();
        await WaitForDelayAsync(200);

        // Popover should still be open
        await Assertions.Expect(openState).ToHaveTextAsync("true");

        // Backdrop should now be visible in modal mode
        await Assertions.Expect(backdrop).ToBeVisibleAsync();
    }

    #endregion

    #region Event Tests

    /// <summary>
    /// Tests that OnOpenChange is called with correct reason.
    /// </summary>
    [Fact]
    public virtual async Task OnOpenChange_FiresWithCorrectReason()
    {
        await NavigateAsync(CreateUrl("/tests/popover"));

        var trigger = GetByTestId("popover-trigger");
        await trigger.ClickAsync();
        await WaitForPopoverOpenAsync();

        var lastReason = GetByTestId("last-reason");
        await Assertions.Expect(lastReason).ToHaveTextAsync("TriggerPress");
    }

    /// <summary>
    /// Tests that OnOpenChange is called when closing with Escape.
    /// </summary>
    [Fact]
    public virtual async Task OnOpenChange_FiresOnEscapeClose()
    {
        await NavigateAsync(CreateUrl("/tests/popover").WithDefaultOpen(true));

        // Wait for JS interop to initialize and popover to be visible
        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();
        await WaitForDelayAsync(200);

        // Focus the popup and press Escape
        await popup.FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await WaitForPopoverClosedAsync();

        var lastReason = GetByTestId("last-reason");
        await Assertions.Expect(lastReason).ToHaveTextAsync("EscapeKey");
    }

    /// <summary>
    /// Tests that OnOpenChange is called when closing with outside click.
    /// Note: Uses click to open because defaultOpen doesn't sync JS state properly.
    /// </summary>
    [Fact]
    public virtual async Task OnOpenChange_FiresOnOutsideClick()
    {
        await NavigateAsync(CreateUrl("/tests/popover"));

        // Open via click to ensure JS state is synced
        await OpenPopoverAsync();
        await WaitForDelayAsync(200);

        var outsideButton = GetByTestId("outside-button");
        await outsideButton.ClickAsync();
        await WaitForPopoverClosedAsync();

        var lastReason = GetByTestId("last-reason");
        await Assertions.Expect(lastReason).ToHaveTextAsync("OutsidePress");
    }

    #endregion

    #region Missing Coverage Tests

    /// <summary>
    /// Tests that moving the mouse away before the hover delay fires does not open the popover.
    /// React source: PopoverRoot.test.tsx — hover delay cancellation.
    /// </summary>
    [Fact]
    public virtual async Task HoverOpen_MouseLeaveBeforeDelay_DoesNotOpen()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithOpenOnHover(true)
            .WithOpenDelay(500));

        // Allow JS hover interaction to initialize
        await WaitForDelayAsync(500);

        var trigger = GetByTestId("popover-trigger");
        await trigger.HoverAsync();

        // Move away quickly, before the 500ms delay fires
        await WaitForDelayAsync(100);
        var outsideButton = GetByTestId("outside-button");
        await outsideButton.HoverAsync();

        // Wait past the original delay to confirm it was cancelled
        await WaitForDelayAsync(600);

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("false");
    }

    /// <summary>
    /// Tests that a non-modal popover closes when focus moves outside of it.
    /// React source: PopoverRoot.test.tsx — focusOut close reason.
    /// </summary>
    [Fact]
    public virtual async Task FocusOut_ClosesNonModalPopover()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithShowFocusableContent(true));

        await OpenPopoverAsync();
        await WaitForDelayAsync(200);

        // Tab through the popup content until focus leaves
        var outsideButton = GetByTestId("outside-button");
        await outsideButton.FocusAsync();
        await WaitForDelayAsync(300);

        await WaitForPopoverClosedAsync();
    }

    /// <summary>
    /// Tests that when InitialFocus is disabled, focus does not move to the popup on open.
    /// React source: PopoverPopup.test.tsx — initialFocus={false}.
    /// </summary>
    [Fact]
    public virtual async Task Focus_InitialFocusNone_DoesNotMoveFocus()
    {
        await NavigateAsync(CreateUrl("/tests/popover-focus")
            .WithDisableInitialFocus(true));

        var trigger = GetByTestId("popover-trigger");
        await trigger.ClickAsync();

        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();
        await WaitForDelayAsync(200);

        // Focus should remain on trigger, not move to popup content
        await Assertions.Expect(trigger).ToBeFocusedAsync();
    }

    /// <summary>
    /// Tests that outside elements are marked inert when a modal popover is open.
    /// React source: PopoverClose.test.tsx — modal inertness semantics.
    /// </summary>
    [Fact]
    public virtual async Task Modal_OutsideElements_AreInert()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithModal(true)
            .WithShowClose(true));

        await OpenPopoverAsync();
        await WaitForDelayAsync(200);

        var outsideButton = GetByTestId("outside-button");
        var isInert = await outsideButton.GetAttributeAsync("inert");
        var ariaHidden = await outsideButton.GetAttributeAsync("aria-hidden");

        // Outside elements should be either inert or aria-hidden when modal is open
        Assert.True(isInert is not null || ariaHidden == "true",
            "Outside element should be inert or aria-hidden when modal popover is open");
    }

    #endregion
}
