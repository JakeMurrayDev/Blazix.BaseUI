using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Tests.Dialog;

/// <summary>
/// Playwright tests for Dialog component - focused on browser-specific behavior.
/// Static rendering, attribute forwarding, and basic state tests are handled by bUnit.
/// These tests cover: click interactions, keyboard navigation, focus management,
/// outside click, escape key, modal behavior, nested dialogs, and real JS interop execution.
/// </summary>
public abstract class DialogTestsBase : TestBase
{
    protected DialogTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    #region Helper Methods

    protected async Task OpenDialogAsync()
    {
        var trigger = GetByTestId("dialog-trigger");
        await trigger.ClickAsync();
        await WaitForDialogOpenAsync();
    }

    protected async Task WaitForDialogOpenAsync()
    {
        var popup = GetByTestId("dialog-popup");
        await popup.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    protected async Task WaitForDialogClosedAsync()
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

    #region Dialog Open/Close Interaction Tests

    /// <summary>
    /// Tests that clicking the trigger toggles the dialog open/closed state.
    /// Requires real browser to test JS interop for state sync.
    /// </summary>
    [Fact]
    public virtual async Task ToggleDialogOnClick()
    {
        await NavigateAsync(CreateUrl("/tests/dialog"));

        var trigger = GetByTestId("dialog-trigger");
        var openState = GetByTestId("open-state");

        await trigger.ClickAsync();
        await Assertions.Expect(openState).ToHaveTextAsync("true");

        // Click trigger again to close (toggle behavior)
        await trigger.ClickAsync();
        await WaitForDialogClosedAsync();
    }

    /// <summary>
    /// Tests that the dialog opens when defaultOpen is true.
    /// </summary>
    [Fact]
    public virtual async Task OpensWithDefaultOpenTrue()
    {
        await NavigateAsync(CreateUrl("/tests/dialog").WithDefaultOpen(true));

        var popup = GetByTestId("dialog-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();
    }

    #endregion

    #region Keyboard Interaction Tests

    /// <summary>
    /// Tests that pressing Escape closes the dialog.
    /// Requires real browser keyboard events.
    /// </summary>
    [Fact]
    public virtual async Task Escape_ClosesDialog()
    {
        await NavigateAsync(CreateUrl("/tests/dialog").WithDefaultOpen(true));

        // Wait for JS interop to initialize and dialog to be visible
        var popup = GetByTestId("dialog-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();
        await WaitForDelayAsync(200);

        // Focus the popup for Escape to be captured
        await popup.FocusAsync();
        await Page.Keyboard.PressAsync("Escape");

        await WaitForDialogClosedAsync();
    }

    /// <summary>
    /// Tests that Enter key on trigger opens the dialog.
    /// </summary>
    [Fact]
    public virtual async Task Enter_OpensDialog()
    {
        await NavigateAsync(CreateUrl("/tests/dialog"));

        var trigger = GetByTestId("dialog-trigger");
        await trigger.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await WaitForDialogOpenAsync();
    }

    /// <summary>
    /// Tests that Space key on trigger opens the dialog.
    /// </summary>
    [Fact]
    public virtual async Task Space_OpensDialog()
    {
        await NavigateAsync(CreateUrl("/tests/dialog"));

        var trigger = GetByTestId("dialog-trigger");
        await trigger.FocusAsync();
        await Page.Keyboard.PressAsync(" ");

        await WaitForDialogOpenAsync();
    }

    #endregion

    #region Outside Click Tests

    /// <summary>
    /// Tests that clicking outside the dialog closes it when not modal.
    /// Requires real browser to test outside click detection.
    /// </summary>
    [Fact]
    public virtual async Task OutsideClick_ClosesNonModalDialog()
    {
        await NavigateAsync(CreateUrl("/tests/dialog").WithModal(false));

        // Open via click to ensure JS state is synced
        await OpenDialogAsync();
        await WaitForDelayAsync(200);

        var outsideButton = GetByTestId("outside-button");
        await outsideButton.ClickAsync();

        await WaitForDialogClosedAsync();
    }

    /// <summary>
    /// Tests that clicking backdrop closes modal dialog.
    /// </summary>
    [Fact]
    public virtual async Task BackdropClick_ClosesModalDialog()
    {
        await NavigateAsync(CreateUrl("/tests/dialog")
            .WithModal(true)
            .WithShowBackdrop(true));

        await OpenDialogAsync();
        await WaitForDelayAsync(200);

        var backdrop = GetByTestId("dialog-backdrop");
        await backdrop.ClickAsync(new LocatorClickOptions { Force = true });

        await WaitForDialogClosedAsync();
    }

    #endregion

    #region Focus Management Tests

    /// <summary>
    /// Tests that the popup can receive focus (has tabindex).
    /// </summary>
    [Fact]
    public virtual async Task PopupCanReceiveFocus()
    {
        await NavigateAsync(CreateUrl("/tests/dialog"));

        await OpenDialogAsync();

        var popup = GetByTestId("dialog-popup");
        await Assertions.Expect(popup).ToHaveAttributeAsync("tabindex", "-1");

        // Verify the popup can be focused programmatically
        await popup.FocusAsync();
        await Assertions.Expect(popup).ToBeFocusedAsync();
    }

    /// <summary>
    /// Tests that the trigger can be focused after dialog closes.
    /// </summary>
    [Fact]
    public virtual async Task TriggerIsFocusableAfterClose()
    {
        await NavigateAsync(CreateUrl("/tests/dialog").WithShowClose(true));

        await OpenDialogAsync();

        // Close the dialog
        var closeButton = GetByTestId("dialog-close");
        await closeButton.ClickAsync();
        await WaitForDialogClosedAsync();

        // Wait for returnFocus rAF to complete
        await WaitForDelayAsync(200);

        // Verify trigger can receive focus
        var trigger = GetByTestId("dialog-trigger");
        await trigger.FocusAsync();
        await Assertions.Expect(trigger).ToBeFocusedAsync();
    }

    /// <summary>
    /// Tests that focus moves to initial focus element when specified.
    /// </summary>
    [Fact]
    public virtual async Task InitialFocus_FocusesSpecifiedElement()
    {
        await NavigateAsync(CreateUrl("/tests/dialog")
            .WithShowClose(true)
            .WithUseInitialFocus(true));

        await OpenDialogAsync();
        await WaitForDelayAsync(300);

        // The initial focus element should be focused
        var closeButton = GetByTestId("dialog-close");
        await Assertions.Expect(closeButton).ToBeFocusedAsync();
    }

    #endregion

    #region Close Button Tests

    /// <summary>
    /// Tests that clicking the close button closes the dialog.
    /// </summary>
    [Fact]
    public virtual async Task CloseButton_ClosesDialog()
    {
        await NavigateAsync(CreateUrl("/tests/dialog")
            .WithDefaultOpen(true)
            .WithShowClose(true));

        var closeButton = GetByTestId("dialog-close");
        await closeButton.ClickAsync();

        await WaitForDialogClosedAsync();
    }

    #endregion

    #region Modal Tests

    /// <summary>
    /// Tests that modal dialog renders with backdrop.
    /// </summary>
    [Fact]
    public virtual async Task Modal_RendersBackdrop()
    {
        await NavigateAsync(CreateUrl("/tests/dialog")
            .WithModal(true)
            .WithShowBackdrop(true));

        await OpenDialogAsync();
        await WaitForDelayAsync(100);

        var backdrop = GetByTestId("dialog-backdrop");
        await Assertions.Expect(backdrop).ToBeAttachedAsync();
        await Assertions.Expect(backdrop).ToHaveAttributeAsync("role", "presentation");
        await Assertions.Expect(backdrop).ToHaveAttributeAsync("data-open", "");
    }

    /// <summary>
    /// Tests that modal dialog has aria-modal attribute.
    /// </summary>
    [Fact]
    public virtual async Task Modal_HasAriaModal()
    {
        await NavigateAsync(CreateUrl("/tests/dialog")
            .WithModal(true)
            .WithDefaultOpen(true));

        var popup = GetByTestId("dialog-popup");
        await Assertions.Expect(popup).ToHaveAttributeAsync("aria-modal", "true");
    }

    /// <summary>
    /// Tests that non-modal dialog does not have aria-modal attribute.
    /// </summary>
    [Fact]
    public virtual async Task NonModal_NoAriaModal()
    {
        await NavigateAsync(CreateUrl("/tests/dialog")
            .WithModal(false)
            .WithDefaultOpen(true));

        var popup = GetByTestId("dialog-popup");
        var ariaModal = await popup.GetAttributeAsync("aria-modal");
        Assert.Null(ariaModal);
    }

    #endregion

    #region Accessibility Tests

    /// <summary>
    /// Tests that trigger has correct aria-expanded attribute.
    /// </summary>
    [Fact]
    public virtual async Task Trigger_HasAriaExpandedFalse_WhenClosed()
    {
        await NavigateAsync(CreateUrl("/tests/dialog"));

        var trigger = GetByTestId("dialog-trigger");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
    }

    /// <summary>
    /// Tests that trigger has correct aria-expanded attribute when open.
    /// </summary>
    [Fact]
    public virtual async Task Trigger_HasAriaExpandedTrue_WhenOpen()
    {
        await NavigateAsync(CreateUrl("/tests/dialog").WithDefaultOpen(true));

        var trigger = GetByTestId("dialog-trigger");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
    }

    /// <summary>
    /// Tests that trigger has aria-haspopup="dialog".
    /// </summary>
    [Fact]
    public virtual async Task Trigger_HasAriaHasPopupDialog()
    {
        await NavigateAsync(CreateUrl("/tests/dialog"));

        var trigger = GetByTestId("dialog-trigger");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-haspopup", "dialog");
    }

    /// <summary>
    /// Tests that popup has role="dialog".
    /// </summary>
    [Fact]
    public virtual async Task Popup_HasDialogRole()
    {
        await NavigateAsync(CreateUrl("/tests/dialog").WithDefaultOpen(true));

        var popup = GetByTestId("dialog-popup");
        await Assertions.Expect(popup).ToHaveAttributeAsync("role", "dialog");
    }

    /// <summary>
    /// Tests that popup is labeled by title when present.
    /// </summary>
    [Fact]
    public virtual async Task Popup_HasAriaLabelledBy_WhenTitlePresent()
    {
        await NavigateAsync(CreateUrl("/tests/dialog")
            .WithDefaultOpen(true)
            .WithShowTitle(true));

        var popup = GetByTestId("dialog-popup");
        var title = GetByTestId("dialog-title");

        var titleId = await title.GetAttributeAsync("id");
        await Assertions.Expect(popup).ToHaveAttributeAsync("aria-labelledby", titleId!);
    }

    /// <summary>
    /// Tests that popup is described by description when present.
    /// </summary>
    [Fact]
    public virtual async Task Popup_HasAriaDescribedBy_WhenDescriptionPresent()
    {
        await NavigateAsync(CreateUrl("/tests/dialog")
            .WithDefaultOpen(true)
            .WithShowDescription(true));

        var popup = GetByTestId("dialog-popup");
        var description = GetByTestId("dialog-description");

        var descriptionId = await description.GetAttributeAsync("id");
        await Assertions.Expect(popup).ToHaveAttributeAsync("aria-describedby", descriptionId!);
    }

    #endregion

    #region KeepMounted Tests

    /// <summary>
    /// Tests that dialog stays in DOM when KeepMounted is true.
    /// </summary>
    [Fact]
    public virtual async Task KeepMounted_DialogStaysInDOM()
    {
        await NavigateAsync(CreateUrl("/tests/dialog")
            .WithKeepMounted(true)
            .WithShowClose(true));

        // Open the dialog
        await OpenDialogAsync();

        var popup = GetByTestId("dialog-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();

        // Close the dialog using the close button
        var closeButton = GetByTestId("dialog-close");
        await closeButton.ClickAsync();
        await WaitForDialogClosedAsync();

        // Popup should still be in DOM with data-closed attribute
        await Assertions.Expect(popup).ToBeAttachedAsync();
        await Assertions.Expect(popup).ToHaveAttributeAsync("data-closed", "");
    }

    #endregion

    #region Event Tests

    /// <summary>
    /// Tests that OnOpenChange is called with correct reason when opening via trigger.
    /// </summary>
    [Fact]
    public virtual async Task OnOpenChange_FiresWithTriggerPressReason()
    {
        await NavigateAsync(CreateUrl("/tests/dialog"));

        var trigger = GetByTestId("dialog-trigger");
        await trigger.ClickAsync();
        await WaitForDialogOpenAsync();

        var lastReason = GetByTestId("last-reason");
        await Assertions.Expect(lastReason).ToHaveTextAsync("TriggerPress");
    }

    /// <summary>
    /// Tests that OnOpenChange is called when closing with Escape.
    /// </summary>
    [Fact]
    public virtual async Task OnOpenChange_FiresOnEscapeClose()
    {
        await NavigateAsync(CreateUrl("/tests/dialog").WithDefaultOpen(true));

        // Wait for JS interop to initialize and dialog to be visible
        var popup = GetByTestId("dialog-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();
        await WaitForDelayAsync(200);

        // Focus the popup and press Escape
        await popup.FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await WaitForDialogClosedAsync();

        var lastReason = GetByTestId("last-reason");
        await Assertions.Expect(lastReason).ToHaveTextAsync("EscapeKey");
    }

    /// <summary>
    /// Tests that OnOpenChange is called when closing with close button.
    /// </summary>
    [Fact]
    public virtual async Task OnOpenChange_FiresOnClosePress()
    {
        await NavigateAsync(CreateUrl("/tests/dialog")
            .WithDefaultOpen(true)
            .WithShowClose(true));

        var closeButton = GetByTestId("dialog-close");
        await closeButton.ClickAsync();
        await WaitForDialogClosedAsync();

        var lastReason = GetByTestId("last-reason");
        await Assertions.Expect(lastReason).ToHaveTextAsync("ClosePress");
    }

    /// <summary>
    /// Tests that OnOpenChange is called when closing with outside click.
    /// </summary>
    [Fact]
    public virtual async Task OnOpenChange_FiresOnOutsidePress()
    {
        await NavigateAsync(CreateUrl("/tests/dialog").WithModal(false));

        // Open via click to ensure JS state is synced
        await OpenDialogAsync();
        await WaitForDelayAsync(200);

        var outsideButton = GetByTestId("outside-button");
        await outsideButton.ClickAsync();
        await WaitForDialogClosedAsync();

        var lastReason = GetByTestId("last-reason");
        await Assertions.Expect(lastReason).ToHaveTextAsync("OutsidePress");
    }

    #endregion

    #region Nested Dialog Tests

    /// <summary>
    /// Tests that nested dialogs can be opened.
    /// </summary>
    [Fact]
    public virtual async Task NestedDialog_CanBeOpened()
    {
        await NavigateAsync(CreateUrl("/tests/dialog").WithShowNestedDialog(true));

        // Open parent dialog
        await OpenDialogAsync();
        await WaitForDelayAsync(100);

        // Open nested dialog
        var nestedTrigger = GetByTestId("nested-dialog-trigger");
        await nestedTrigger.ClickAsync();

        var nestedPopup = GetByTestId("nested-dialog-popup");
        await Assertions.Expect(nestedPopup).ToBeVisibleAsync();
    }

    /// <summary>
    /// Tests that Escape only closes the innermost nested dialog.
    /// </summary>
    [Fact]
    public virtual async Task NestedDialog_EscapeClosesOnlyInnermost()
    {
        await NavigateAsync(CreateUrl("/tests/dialog").WithShowNestedDialog(true));

        // Open parent dialog
        await OpenDialogAsync();
        await WaitForDelayAsync(100);

        // Open nested dialog
        var nestedTrigger = GetByTestId("nested-dialog-trigger");
        await nestedTrigger.ClickAsync();

        var nestedPopup = GetByTestId("nested-dialog-popup");
        await Assertions.Expect(nestedPopup).ToBeVisibleAsync();
        await WaitForDelayAsync(200);

        // Press Escape
        await nestedPopup.FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await WaitForDelayAsync(100);

        // Nested dialog should be closed, parent should still be open
        await Assertions.Expect(nestedPopup).Not.ToBeVisibleAsync();
        var parentPopup = GetByTestId("dialog-popup");
        await Assertions.Expect(parentPopup).ToBeVisibleAsync();
    }

    /// <summary>
    /// Tests that parent dialog has data-nested-dialog-open when nested is open.
    /// </summary>
    [Fact]
    public virtual async Task NestedDialog_ParentHasDataAttribute()
    {
        await NavigateAsync(CreateUrl("/tests/dialog").WithShowNestedDialog(true));

        // Open parent dialog
        await OpenDialogAsync();
        await WaitForDelayAsync(100);

        // Open nested dialog
        var nestedTrigger = GetByTestId("nested-dialog-trigger");
        await nestedTrigger.ClickAsync();

        var nestedPopup = GetByTestId("nested-dialog-popup");
        await Assertions.Expect(nestedPopup).ToBeVisibleAsync();

        var parentPopup = GetByTestId("dialog-popup");
        await Assertions.Expect(parentPopup).ToHaveAttributeAsync("data-nested-dialog-open", "");
    }

    #endregion

    #region Viewport Tests

    /// <summary>
    /// Tests that viewport is rendered when present.
    /// </summary>
    [Fact]
    public virtual async Task Viewport_IsRendered()
    {
        await NavigateAsync(CreateUrl("/tests/dialog")
            .WithDefaultOpen(true)
            .WithShowViewport(true));

        var viewport = GetByTestId("dialog-viewport");
        await Assertions.Expect(viewport).ToBeAttachedAsync();
        await Assertions.Expect(viewport).ToHaveAttributeAsync("role", "presentation");
    }

    #endregion

    #region ActionsRef Tests

    /// <summary>
    /// Tests that ActionsRef.Close closes the dialog.
    /// </summary>
    [Fact]
    public virtual async Task ActionsRef_Close_ClosesDialog()
    {
        await NavigateAsync(CreateUrl("/tests/dialog")
            .WithDefaultOpen(true)
            .WithShowActionsButtons(true));

        var popup = GetByTestId("dialog-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();

        var closeActionsButton = GetByTestId("actions-close");
        await closeActionsButton.ClickAsync();

        await WaitForDialogClosedAsync();
    }

    #endregion
}
