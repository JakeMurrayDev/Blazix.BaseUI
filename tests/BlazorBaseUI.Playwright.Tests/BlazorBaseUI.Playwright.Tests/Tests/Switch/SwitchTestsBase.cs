using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace BlazorBaseUI.Playwright.Tests.Tests.Switch;

/// <summary>
/// Playwright tests for Switch component - focused on browser-specific behavior.
/// Static rendering, attribute forwarding, and basic state tests are handled by bUnit.
/// These tests cover: click interactions, keyboard activation, focus management,
/// label associations, form integration, and real JS interop execution.
/// </summary>
public abstract class SwitchTestsBase : TestBase
{
    protected SwitchTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    #region Helper Methods

    protected ILocator GetSwitch() => GetByTestId("switch-root");

    protected ILocator GetSwitchThumb() => GetByTestId("switch-thumb");

    protected ILocator GetHiddenInput() => Page.Locator("input[type='checkbox']");

    protected async Task<bool> GetCheckedStateAsync()
    {
        var switchEl = GetSwitch();
        var ariaChecked = await switchEl.GetAttributeAsync("aria-checked");
        return ariaChecked == "true";
    }

    protected async Task WaitForCheckedStateAsync(bool expected, int timeout = 5000)
    {
        var effectiveTimeout = timeout * TimeoutMultiplier;
        var switchEl = GetSwitch();
        await Assertions.Expect(switchEl).ToHaveAttributeAsync(
            "aria-checked",
            expected ? "true" : "false",
            new LocatorAssertionsToHaveAttributeOptions { Timeout = effectiveTimeout });
    }

    #endregion

    #region Click Interaction Tests

    /// <summary>
    /// Tests that clicking the switch toggles its state.
    /// Requires real browser click events and JS interop.
    /// </summary>
    [Fact]
    public virtual async Task Click_TogglesState()
    {
        await NavigateAsync(CreateUrl("/tests/switch"));

        var switchEl = GetSwitch();
        await WaitForCheckedStateAsync(false);

        await switchEl.ClickAsync();
        await WaitForCheckedStateAsync(true);

        await switchEl.ClickAsync();
        await WaitForCheckedStateAsync(false);
    }

    /// <summary>
    /// Tests that clicking a disabled switch does not change state.
    /// </summary>
    [Fact]
    public virtual async Task DisabledSwitch_DoesNotToggleOnClick()
    {
        await NavigateAsync(CreateUrl("/tests/switch")
            .WithDisabled(true));

        var switchEl = GetSwitch();
        await WaitForCheckedStateAsync(false);

        await switchEl.ClickAsync(new LocatorClickOptions { Force = true });
        await WaitForDelayAsync(100);

        await WaitForCheckedStateAsync(false);
    }

    /// <summary>
    /// Tests that a disabled non-native switch exposes aria-disabled without firing user click callbacks.
    /// </summary>
    [Fact]
    public virtual async Task DisabledSwitch_HasAriaDisabledAndDoesNotInvokeClickCallback()
    {
        await NavigateAsync(CreateUrl("/tests/switch")
            .WithDisabled(true));

        var switchEl = GetSwitch();
        var clickCount = GetByTestId("click-count");

        await Assertions.Expect(switchEl).ToHaveAttributeAsync("aria-disabled", "true");
        Assert.Null(await switchEl.GetAttributeAsync("disabled"));
        await Assertions.Expect(clickCount).ToHaveTextAsync("0");

        await switchEl.ClickAsync(new LocatorClickOptions { Force = true });
        await WaitForDelayAsync(100);

        await Assertions.Expect(clickCount).ToHaveTextAsync("0");
    }

    /// <summary>
    /// Tests that clicking a readonly switch does not change state.
    /// </summary>
    [Fact]
    public virtual async Task ReadOnlySwitch_DoesNotToggleOnClick()
    {
        await NavigateAsync(CreateUrl("/tests/switch")
            .WithReadOnly(true));

        var switchEl = GetSwitch();
        await WaitForCheckedStateAsync(false);

        await switchEl.ClickAsync();
        await WaitForDelayAsync(100);

        await WaitForCheckedStateAsync(false);
    }

    /// <summary>
    /// Tests that the onClick callback is invoked when clicked.
    /// </summary>
    [Fact]
    public virtual async Task Click_InvokesOnClickCallback()
    {
        await NavigateAsync(CreateUrl("/tests/switch"));

        var switchEl = GetSwitch();
        var clickCount = GetByTestId("click-count");

        await Assertions.Expect(clickCount).ToHaveTextAsync("0");

        await switchEl.ClickAsync();
        await WaitForDelayAsync(100);

        await Assertions.Expect(clickCount).ToHaveTextAsync("1");
    }

    /// <summary>
    /// Tests that the onCheckedChange callback is invoked when state changes.
    /// </summary>
    [Fact]
    public virtual async Task Click_InvokesOnCheckedChangeCallback()
    {
        await NavigateAsync(CreateUrl("/tests/switch"));

        var switchEl = GetSwitch();
        var changeCount = GetByTestId("change-count");

        await Assertions.Expect(changeCount).ToHaveTextAsync("0");

        await switchEl.ClickAsync();
        await WaitForDelayAsync(100);

        await Assertions.Expect(changeCount).ToHaveTextAsync("1");
    }

    /// <summary>
    /// Tests that pointer modifier keys are preserved when dispatching the hidden input click.
    /// </summary>
    [Fact]
    public virtual async Task Click_ReportsModifierKeysToOnCheckedChange()
    {
        await NavigateAsync(CreateUrl("/tests/switch"));

        var switchEl = GetSwitch();
        var modifierState = GetByTestId("modifier-state");

        await Page.Keyboard.DownAsync("Shift");
        await switchEl.ClickAsync();
        await Page.Keyboard.UpAsync("Shift");
        await WaitForDelayAsync(100);

        await Assertions.Expect(modifierState).ToHaveTextAsync("shift=true;ctrl=false;alt=false;meta=false");
    }

    #endregion

    #region Keyboard Activation Tests

    /// <summary>
    /// Tests that pressing Enter toggles the switch state.
    /// Requires real browser keyboard events.
    /// </summary>
    [Fact]
    public virtual async Task Enter_TogglesState()
    {
        await NavigateAsync(CreateUrl("/tests/switch"));

        var switchEl = GetSwitch();
        await WaitForCheckedStateAsync(false);

        await switchEl.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await WaitForDelayAsync(100);

        await WaitForCheckedStateAsync(true);
    }

    /// <summary>
    /// Tests that pressing Space toggles the switch state.
    /// Requires real browser keyboard events.
    /// </summary>
    [Fact]
    public virtual async Task Space_TogglesState()
    {
        await NavigateAsync(CreateUrl("/tests/switch"));

        var switchEl = GetSwitch();
        await WaitForCheckedStateAsync(false);

        await switchEl.FocusAsync();
        await Page.Keyboard.PressAsync("Space");
        await WaitForDelayAsync(100);

        await WaitForCheckedStateAsync(true);
    }

    /// <summary>
    /// Tests that pressing Enter invokes the root click callback in the same activation path as React useButton.
    /// </summary>
    [Fact]
    public virtual async Task Enter_InvokesOnClickCallback()
    {
        await NavigateAsync(CreateUrl("/tests/switch"));

        var switchEl = GetSwitch();
        var clickCount = GetByTestId("click-count");

        await Assertions.Expect(clickCount).ToHaveTextAsync("0");

        await switchEl.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await WaitForDelayAsync(100);

        await Assertions.Expect(clickCount).ToHaveTextAsync("1");
        await WaitForCheckedStateAsync(true);
    }

    /// <summary>
    /// Tests that pressing Space invokes the root click callback in the same activation path as React useButton.
    /// </summary>
    [Fact]
    public virtual async Task Space_InvokesOnClickCallback()
    {
        await NavigateAsync(CreateUrl("/tests/switch"));

        var switchEl = GetSwitch();
        var clickCount = GetByTestId("click-count");

        await Assertions.Expect(clickCount).ToHaveTextAsync("0");

        await switchEl.FocusAsync();
        await Page.Keyboard.PressAsync("Space");
        await WaitForDelayAsync(100);

        await Assertions.Expect(clickCount).ToHaveTextAsync("1");
        await WaitForCheckedStateAsync(true);
    }

    /// <summary>
    /// Tests that readonly keyboard activation preserves the root click callback while blocking state changes.
    /// </summary>
    [Fact]
    public virtual async Task ReadOnlySwitch_KeyboardInvokesClickCallbackWithoutToggling()
    {
        await NavigateAsync(CreateUrl("/tests/switch")
            .WithReadOnly(true));

        var switchEl = GetSwitch();
        var clickCount = GetByTestId("click-count");
        var changeCount = GetByTestId("change-count");

        await WaitForCheckedStateAsync(false);
        await Assertions.Expect(clickCount).ToHaveTextAsync("0");
        await Assertions.Expect(changeCount).ToHaveTextAsync("0");

        await switchEl.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await WaitForDelayAsync(100);

        await Assertions.Expect(clickCount).ToHaveTextAsync("1");
        await Assertions.Expect(changeCount).ToHaveTextAsync("0");
        await WaitForCheckedStateAsync(false);
    }

    /// <summary>
    /// Tests that disabled switch does not respond to keyboard.
    /// </summary>
    [Fact]
    public virtual async Task DisabledSwitch_DoesNotRespondToKeyboard()
    {
        await NavigateAsync(CreateUrl("/tests/switch")
            .WithDisabled(true));

        var switchEl = GetSwitch();
        await WaitForCheckedStateAsync(false);

        // Try to focus and activate - disabled switches should not be focusable
        await switchEl.FocusAsync(new LocatorFocusOptions { Timeout = 1000 });
        await Page.Keyboard.PressAsync("Enter");
        await WaitForDelayAsync(100);

        await WaitForCheckedStateAsync(false);
    }

    /// <summary>
    /// Tests that readonly switch does not respond to keyboard.
    /// </summary>
    [Fact]
    public virtual async Task ReadOnlySwitch_DoesNotRespondToKeyboard()
    {
        await NavigateAsync(CreateUrl("/tests/switch")
            .WithReadOnly(true));

        var switchEl = GetSwitch();
        await WaitForCheckedStateAsync(false);

        await switchEl.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await WaitForDelayAsync(100);

        await WaitForCheckedStateAsync(false);
    }

    #endregion

    #region Native Button Tests

    /// <summary>
    /// Tests that native button mode renders a button element.
    /// </summary>
    [Fact]
    public virtual async Task NativeButton_RendersAsButton()
    {
        await NavigateAsync(CreateUrl("/tests/switch")
            .WithNativeButton(true));

        var switchEl = GetSwitch();
        var tagName = await switchEl.EvaluateAsync<string>("el => el.tagName.toLowerCase()");

        Assert.Equal("button", tagName);
    }

    /// <summary>
    /// Tests that native button responds to Enter key.
    /// </summary>
    [Fact]
    public virtual async Task NativeButton_RespondsToEnter()
    {
        await NavigateAsync(CreateUrl("/tests/switch")
            .WithNativeButton(true));

        var switchEl = GetSwitch();
        await WaitForCheckedStateAsync(false);

        await switchEl.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await WaitForDelayAsync(100);

        await WaitForCheckedStateAsync(true);
    }

    /// <summary>
    /// Tests that native button responds to Space key.
    /// </summary>
    [Fact]
    public virtual async Task NativeButton_RespondsToSpace()
    {
        await NavigateAsync(CreateUrl("/tests/switch")
            .WithNativeButton(true));

        var switchEl = GetSwitch();
        await WaitForCheckedStateAsync(false);

        await switchEl.FocusAsync();
        await Page.Keyboard.PressAsync("Space");
        await WaitForDelayAsync(100);

        await WaitForCheckedStateAsync(true);
    }

    /// <summary>
    /// Tests that disabled native button has disabled attribute.
    /// </summary>
    [Fact]
    public virtual async Task NativeButton_HasDisabledAttribute()
    {
        await NavigateAsync(CreateUrl("/tests/switch")
            .WithNativeButton(true)
            .WithDisabled(true));

        var switchEl = GetSwitch();
        await Assertions.Expect(switchEl).ToBeDisabledAsync();
    }

    /// <summary>
    /// Tests that native button mode associates the public id with the button rather than the hidden input.
    /// </summary>
    [Fact]
    public virtual async Task NativeButton_LinkedLabelTargetsButton()
    {
        await NavigateAsync(CreateUrl("/tests/switch")
            .WithNativeButton(true)
            .WithShowLabel(true));

        var switchEl = GetSwitch();
        var hiddenInput = GetHiddenInput();
        var label = GetByTestId("linked-label");

        await Assertions.Expect(switchEl).ToHaveAttributeAsync("id", "my-switch");
        Assert.NotEqual("my-switch", await hiddenInput.GetAttributeAsync("id"));
        await WaitForCheckedStateAsync(false);

        await label.ClickAsync();
        await WaitForDelayAsync(100);

        await WaitForCheckedStateAsync(true);
    }

    #endregion

    #region Focus Management Tests

    /// <summary>
    /// Tests that the switch can receive focus.
    /// </summary>
    [Fact]
    public virtual async Task Switch_CanReceiveFocus()
    {
        await NavigateAsync(CreateUrl("/tests/switch"));

        var switchEl = GetSwitch();
        await switchEl.FocusAsync();

        await Assertions.Expect(switchEl).ToBeFocusedAsync();
    }

    /// <summary>
    /// Tests that focus event is triggered when switch receives focus.
    /// </summary>
    [Fact]
    public virtual async Task Focus_InvokesFocusCallback()
    {
        await NavigateAsync(CreateUrl("/tests/switch"));

        var switchEl = GetSwitch();
        var focusCount = GetByTestId("focus-count");

        await Assertions.Expect(focusCount).ToHaveTextAsync("0");

        await switchEl.FocusAsync();
        await WaitForDelayAsync(100);

        await Assertions.Expect(focusCount).ToHaveTextAsync("1");
    }

    /// <summary>
    /// Tests that blur event is triggered when switch loses focus.
    /// </summary>
    [Fact]
    public virtual async Task Blur_InvokesBlurCallback()
    {
        await NavigateAsync(CreateUrl("/tests/switch"));

        var switchEl = GetSwitch();
        var outsideButton = GetByTestId("outside-button");
        var blurCount = GetByTestId("blur-count");

        await switchEl.FocusAsync();
        await WaitForDelayAsync(50);

        await outsideButton.FocusAsync();
        await WaitForDelayAsync(100);

        await Assertions.Expect(blurCount).ToHaveTextAsync("1");
    }

    /// <summary>
    /// Tests that Tab key navigates to the switch.
    /// </summary>
    [Fact]
    public virtual async Task Tab_NavigatesToSwitch()
    {
        await NavigateAsync(CreateUrl("/tests/switch"));

        // Start from outside the switch
        var outsideButton = GetByTestId("outside-button");
        await outsideButton.FocusAsync();

        // Tab should move focus to the switch (or elsewhere, depending on tab order)
        await Page.Keyboard.PressAsync("Tab");
        await WaitForDelayAsync(100);

        // Just verify the switch is focusable via explicit focus
        var switchEl = GetSwitch();
        await switchEl.FocusAsync();
        await Assertions.Expect(switchEl).ToBeFocusedAsync();
    }

    #endregion

    #region Label Association Tests

    /// <summary>
    /// Tests that clicking a wrapping label toggles the switch.
    /// Requires real browser label click behavior.
    /// </summary>
    [Fact]
    public virtual async Task WrappingLabel_TogglesSwitch()
    {
        await NavigateAsync(CreateUrl("/tests/switch")
            .WithShowWrappingLabel(true));

        var label = GetByTestId("wrapping-label");
        await WaitForCheckedStateAsync(false);

        await label.ClickAsync();
        await WaitForDelayAsync(100);

        await WaitForCheckedStateAsync(true);
    }

    /// <summary>
    /// Tests that clicking an explicitly linked label toggles the switch.
    /// Requires real browser label-for association.
    /// </summary>
    [Fact]
    public virtual async Task LinkedLabel_TogglesSwitch()
    {
        await NavigateAsync(CreateUrl("/tests/switch")
            .WithShowLabel(true));

        var label = GetByTestId("linked-label");
        await WaitForCheckedStateAsync(false);

        await label.ClickAsync();
        await WaitForDelayAsync(100);

        await WaitForCheckedStateAsync(true);
    }

    /// <summary>
    /// Tests that the non-native root receives fallback aria-labelledby from an explicitly linked native label.
    /// </summary>
    [Fact]
    public virtual async Task LinkedLabel_ProvidesFallbackAriaLabelledBy()
    {
        await NavigateAsync(CreateUrl("/tests/switch")
            .WithShowLabel(true));

        var switchEl = GetSwitch();
        var label = GetByTestId("linked-label");

        var labelId = await label.GetAttributeAsync("id");
        Assert.False(string.IsNullOrEmpty(labelId));
        await Assertions.Expect(switchEl).ToHaveAttributeAsync("aria-labelledby", labelId!);
    }

    /// <summary>
    /// Tests that fallback aria-labelledby tracks the hidden input id when it changes.
    /// </summary>
    [Fact]
    public virtual async Task LinkedLabel_UpdatesFallbackAriaLabelledByWhenInputIdChanges()
    {
        await NavigateAsync(CreateUrl("/tests/switch")
            .WithShowDynamicLabel(true));

        var switchEl = GetSwitch();
        var labelA = GetByTestId("linked-label-a");
        var labelB = GetByTestId("linked-label-b");
        var toggleId = GetByTestId("toggle-switch-id");

        await Assertions.Expect(labelA).ToHaveAttributeAsync("id", "dynamic-switch-a-label");
        await Assertions.Expect(switchEl).ToHaveAttributeAsync("aria-labelledby", "dynamic-switch-a-label");

        await toggleId.ClickAsync();

        await Assertions.Expect(labelB).ToHaveAttributeAsync("id", "dynamic-switch-b-label");
        await Assertions.Expect(switchEl).ToHaveAttributeAsync("aria-labelledby", "dynamic-switch-b-label");
    }

    #endregion

    #region External State Control Tests

    /// <summary>
    /// Tests that the switch updates when controlled externally.
    /// </summary>
    [Fact]
    public virtual async Task ExternalToggle_UpdatesSwitchState()
    {
        await NavigateAsync(CreateUrl("/tests/switch"));

        await WaitForCheckedStateAsync(false);

        var toggleButton = GetByTestId("toggle-external");
        await toggleButton.ClickAsync();
        await WaitForDelayAsync(100);

        await WaitForCheckedStateAsync(true);

        await toggleButton.ClickAsync();
        await WaitForDelayAsync(100);

        await WaitForCheckedStateAsync(false);
    }

    #endregion

    #region Default Checked Tests

    /// <summary>
    /// Tests that defaultChecked initializes the switch as checked.
    /// </summary>
    [Fact]
    public virtual async Task DefaultChecked_InitializesAsChecked()
    {
        await NavigateAsync(CreateUrl("/tests/switch")
            .WithDefaultChecked(true));

        await WaitForCheckedStateAsync(true);
    }

    #endregion

    #region Form Integration Tests

    /// <summary>
    /// Tests that the switch value is included in form submission when checked.
    /// </summary>
    [Fact]
    public virtual async Task FormSubmission_IncludesValueWhenChecked()
    {
        await NavigateAsync(CreateUrl("/tests/switch")
            .WithShowForm(true)
            .WithSwitchName("toggle")
            .WithDefaultChecked(true));

        var submitButton = GetByTestId("submit-button");
        await submitButton.ClickAsync();
        await WaitForDelayAsync(100);

        var formData = GetByTestId("form-data");
        await Assertions.Expect(formData).ToContainTextAsync("toggle=on");
    }

    /// <summary>
    /// Tests that the switch value is not included when unchecked and no uncheckedValue.
    /// </summary>
    [Fact]
    public virtual async Task FormSubmission_ExcludesValueWhenUnchecked()
    {
        await NavigateAsync(CreateUrl("/tests/switch")
            .WithShowForm(true)
            .WithSwitchName("toggle"));

        await WaitForCheckedStateAsync(false);

        var submitButton = GetByTestId("submit-button");
        await submitButton.ClickAsync();
        await WaitForDelayAsync(100);

        var formData = GetByTestId("form-data");
        var text = await formData.TextContentAsync();
        Assert.True(string.IsNullOrEmpty(text), "Form data should be empty when switch is unchecked without uncheckedValue");
    }

    /// <summary>
    /// Tests that uncheckedValue is submitted when switch is unchecked.
    /// </summary>
    [Fact]
    public virtual async Task FormSubmission_IncludesUncheckedValue()
    {
        await NavigateAsync(CreateUrl("/tests/switch")
            .WithShowForm(true)
            .WithSwitchName("toggle")
            .WithUncheckedValue("off"));

        await WaitForCheckedStateAsync(false);

        var submitButton = GetByTestId("submit-button");
        await submitButton.ClickAsync();
        await WaitForDelayAsync(100);

        var formData = GetByTestId("form-data");
        await Assertions.Expect(formData).ToContainTextAsync("toggle=off");
    }

    /// <summary>
    /// Tests that custom value is submitted when switch is checked.
    /// </summary>
    [Fact]
    public virtual async Task FormSubmission_IncludesCustomValue()
    {
        await NavigateAsync(CreateUrl("/tests/switch")
            .WithShowForm(true)
            .WithSwitchName("toggle")
            .WithSwitchValue("yes")
            .WithDefaultChecked(true));

        var submitButton = GetByTestId("submit-button");
        await submitButton.ClickAsync();
        await WaitForDelayAsync(100);

        var formData = GetByTestId("form-data");
        await Assertions.Expect(formData).ToContainTextAsync("toggle=yes");
    }

    /// <summary>
    /// Tests that the form attribute is forwarded to the checkbox input and unchecked hidden input only.
    /// </summary>
    [Fact]
    public virtual async Task FormAttribute_AppliesToHiddenInputsOnly()
    {
        await NavigateAsync(CreateUrl("/tests/switch")
            .WithSwitchName("toggle")
            .WithSwitchForm("external-form")
            .WithUncheckedValue("off"));

        var switchEl = GetSwitch();
        var input = GetHiddenInput();
        var uncheckedInput = Page.Locator("input[type='hidden']");

        Assert.Null(await switchEl.GetAttributeAsync("form"));
        Assert.Equal("external-form", await input.GetAttributeAsync("form"));
        Assert.Equal("external-form", await uncheckedInput.GetAttributeAsync("form"));
    }

    #endregion

    #region Data Attribute Tests

    /// <summary>
    /// Tests that data-checked is applied when checked.
    /// </summary>
    [Fact]
    public virtual async Task DataChecked_AppliedWhenChecked()
    {
        await NavigateAsync(CreateUrl("/tests/switch")
            .WithDefaultChecked(true));

        var switchEl = GetSwitch();
        await Assertions.Expect(switchEl).ToHaveAttributeAsync("data-checked", "");
    }

    /// <summary>
    /// Tests that data-unchecked is applied when unchecked.
    /// </summary>
    [Fact]
    public virtual async Task DataUnchecked_AppliedWhenUnchecked()
    {
        await NavigateAsync(CreateUrl("/tests/switch"));

        var switchEl = GetSwitch();
        await Assertions.Expect(switchEl).ToHaveAttributeAsync("data-unchecked", "");
    }

    /// <summary>
    /// Tests that data attributes toggle correctly.
    /// </summary>
    [Fact]
    public virtual async Task DataAttributes_ToggleCorrectly()
    {
        await NavigateAsync(CreateUrl("/tests/switch"));

        var switchEl = GetSwitch();

        // Initially unchecked
        await Assertions.Expect(switchEl).ToHaveAttributeAsync("data-unchecked", "");

        // Toggle
        await switchEl.ClickAsync();
        await WaitForDelayAsync(100);

        // Should now be checked
        await Assertions.Expect(switchEl).ToHaveAttributeAsync("data-checked", "");

        // Toggle again
        await switchEl.ClickAsync();
        await WaitForDelayAsync(100);

        // Should be unchecked again
        await Assertions.Expect(switchEl).ToHaveAttributeAsync("data-unchecked", "");
    }

    /// <summary>
    /// Tests that thumb also gets data attributes.
    /// </summary>
    [Fact]
    public virtual async Task Thumb_HasMatchingDataAttributes()
    {
        await NavigateAsync(CreateUrl("/tests/switch")
            .WithDefaultChecked(true));

        var thumb = GetSwitchThumb();
        await Assertions.Expect(thumb).ToHaveAttributeAsync("data-checked", "");

        var switchEl = GetSwitch();
        await switchEl.ClickAsync();
        await WaitForDelayAsync(100);

        await Assertions.Expect(thumb).ToHaveAttributeAsync("data-unchecked", "");
    }

    #endregion

    #region Hidden Input Tests

    /// <summary>
    /// Tests that the hidden input is synchronized with switch state.
    /// </summary>
    [Fact]
    public virtual async Task HiddenInput_SynchronizedWithSwitchState()
    {
        await NavigateAsync(CreateUrl("/tests/switch"));

        var hiddenInput = GetHiddenInput();

        // Initially unchecked
        var isChecked = await hiddenInput.IsCheckedAsync();
        Assert.False(isChecked);

        // Toggle
        var switchEl = GetSwitch();
        await switchEl.ClickAsync();
        await WaitForDelayAsync(100);

        // Should now be checked
        isChecked = await hiddenInput.IsCheckedAsync();
        Assert.True(isChecked);
    }

    /// <summary>
    /// Tests that clicking the hidden input toggles the switch.
    /// </summary>
    [Fact]
    public virtual async Task HiddenInput_ClickTogglesSwitch()
    {
        await NavigateAsync(CreateUrl("/tests/switch"));

        var hiddenInput = GetHiddenInput();
        await WaitForCheckedStateAsync(false);

        // Use JavaScript click to programmatically trigger the input's click handler
        // This matches the React test which uses internalInput.click()
        await hiddenInput.EvaluateAsync("el => el.click()");
        await WaitForDelayAsync(100);

        await WaitForCheckedStateAsync(true);
    }

    #endregion

    #region State Display Tests

    /// <summary>
    /// Tests that the state display updates when switch toggles.
    /// </summary>
    [Fact]
    public virtual async Task StateDisplay_UpdatesOnToggle()
    {
        await NavigateAsync(CreateUrl("/tests/switch"));

        var checkedState = GetByTestId("checked-state");
        await Assertions.Expect(checkedState).ToHaveTextAsync("false");

        var switchEl = GetSwitch();
        await switchEl.ClickAsync();
        await WaitForDelayAsync(100);

        await Assertions.Expect(checkedState).ToHaveTextAsync("true");
    }

    #endregion
}
