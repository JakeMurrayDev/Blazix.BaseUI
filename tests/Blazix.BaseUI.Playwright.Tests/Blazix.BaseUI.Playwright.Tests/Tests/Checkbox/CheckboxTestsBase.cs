using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Tests.Checkbox;

/// <summary>
/// Playwright tests for Checkbox component - focused on browser-specific behavior.
/// Static rendering, attribute forwarding, and basic state tests are handled by bUnit.
/// These tests cover: click interactions, keyboard activation, focus management,
/// label associations, form integration, indeterminate state, and real JS interop execution.
/// </summary>
public abstract class CheckboxTestsBase : TestBase
{
    protected CheckboxTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    #region Helper Methods

    protected ILocator GetCheckbox() => GetByTestId("checkbox-root");

    protected ILocator GetCheckboxIndicator() => GetByTestId("checkbox-indicator");

    protected ILocator GetHiddenInput() => Page.Locator("input[type='checkbox']");

    protected async Task<bool> GetCheckedStateAsync()
    {
        var checkbox = GetCheckbox();
        var ariaChecked = await checkbox.GetAttributeAsync("aria-checked");
        return ariaChecked == "true";
    }

    protected async Task<string?> GetAriaCheckedStateAsync()
    {
        var checkbox = GetCheckbox();
        return await checkbox.GetAttributeAsync("aria-checked");
    }

    protected async Task WaitForCheckedStateAsync(bool expected, int timeout = 5000)
    {
        var effectiveTimeout = timeout * TimeoutMultiplier;
        var checkbox = GetCheckbox();
        await Assertions.Expect(checkbox).ToHaveAttributeAsync(
            "aria-checked",
            expected ? "true" : "false",
            new LocatorAssertionsToHaveAttributeOptions { Timeout = effectiveTimeout });
    }

    protected async Task WaitForIndeterminateStateAsync(int timeout = 5000)
    {
        var effectiveTimeout = timeout * TimeoutMultiplier;
        var checkbox = GetCheckbox();
        await Assertions.Expect(checkbox).ToHaveAttributeAsync(
            "aria-checked",
            "mixed",
            new LocatorAssertionsToHaveAttributeOptions { Timeout = effectiveTimeout });
    }

    #endregion

    #region Click Interaction Tests

    /// <summary>
    /// Tests that clicking the checkbox toggles its state.
    /// Requires real browser click events and JS interop.
    /// </summary>
    [Fact]
    public virtual async Task Click_TogglesState()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox"));

        var checkbox = GetCheckbox();
        await WaitForCheckedStateAsync(false);

        await checkbox.ClickAsync();
        await WaitForCheckedStateAsync(true);

        await checkbox.ClickAsync();
        await WaitForCheckedStateAsync(false);
    }

    /// <summary>
    /// Tests that clicking a disabled checkbox does not change state.
    /// </summary>
    [Fact]
    public virtual async Task DisabledCheckbox_DoesNotToggleOnClick()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox")
            .WithDisabled(true));

        var checkbox = GetCheckbox();
        await WaitForCheckedStateAsync(false);

        await checkbox.ClickAsync(new LocatorClickOptions { Force = true });
        await WaitForDelayAsync(100);

        await WaitForCheckedStateAsync(false);
    }

    /// <summary>
    /// Tests that clicking a readonly checkbox does not change state.
    /// </summary>
    [Fact]
    public virtual async Task ReadOnlyCheckbox_DoesNotToggleOnClick()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox")
            .WithReadOnly(true));

        var checkbox = GetCheckbox();
        await WaitForCheckedStateAsync(false);

        await checkbox.ClickAsync();
        await WaitForDelayAsync(100);

        await WaitForCheckedStateAsync(false);
    }

    /// <summary>
    /// Tests that the onClick callback is invoked when clicked.
    /// </summary>
    [Fact]
    public virtual async Task Click_InvokesOnClickCallback()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox"));

        var checkbox = GetCheckbox();
        var clickCount = GetByTestId("click-count");

        await Assertions.Expect(clickCount).ToHaveTextAsync("0");

        await checkbox.ClickAsync();
        await WaitForDelayAsync(100);

        await Assertions.Expect(clickCount).ToHaveTextAsync("1");
    }

    /// <summary>
    /// Tests that the onCheckedChange callback is invoked when state changes.
    /// </summary>
    [Fact]
    public virtual async Task Click_InvokesOnCheckedChangeCallback()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox"));

        var checkbox = GetCheckbox();
        var changeCount = GetByTestId("change-count");

        await Assertions.Expect(changeCount).ToHaveTextAsync("0");

        await checkbox.ClickAsync();
        await WaitForDelayAsync(100);

        await Assertions.Expect(changeCount).ToHaveTextAsync("1");
    }

    #endregion

    #region Keyboard Activation Tests

    /// <summary>
    /// Tests that pressing Space toggles the checkbox state.
    /// Requires real browser keyboard events.
    /// </summary>
    [Fact]
    public virtual async Task Space_TogglesState()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox"));

        var checkbox = GetCheckbox();
        await WaitForCheckedStateAsync(false);

        await checkbox.FocusAsync();
        await Page.Keyboard.PressAsync("Space");
        await WaitForDelayAsync(100);

        await WaitForCheckedStateAsync(true);
    }

    /// <summary>
    /// Tests that pressing Enter does not toggle the checkbox state.
    /// </summary>
    [Fact]
    public virtual async Task Enter_DoesNotToggleState()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox"));

        var checkbox = GetCheckbox();
        await WaitForCheckedStateAsync(false);

        await checkbox.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await WaitForDelayAsync(100);

        await WaitForCheckedStateAsync(false);
    }

    /// <summary>
    /// Tests that pressing Enter submits the default form submitter without toggling.
    /// </summary>
    [Fact]
    public virtual async Task Enter_SubmitsFormWithoutToggling()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox")
            .WithShowForm(true)
            .WithSwitchName("agree"));

        var checkbox = GetCheckbox();
        var formData = GetByTestId("form-data");
        var submitCount = GetByTestId("submit-count");

        await WaitForCheckedStateAsync(false);
        await Assertions.Expect(submitCount).ToHaveTextAsync("0");
        await checkbox.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await WaitForDelayAsync(100);

        await WaitForCheckedStateAsync(false);
        await Assertions.Expect(submitCount).ToHaveTextAsync("1");
        await Assertions.Expect(formData).ToHaveTextAsync("");
    }

    /// <summary>
    /// Tests that pressing Enter on a native button checkbox submits the default form submitter without toggling.
    /// </summary>
    [Fact]
    public virtual async Task NativeButton_EnterSubmitsFormWithoutToggling()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox")
            .WithShowForm(true)
            .WithSwitchName("agree")
            .WithNativeButton(true));

        var checkbox = GetCheckbox();
        var formData = GetByTestId("form-data");
        var submitCount = GetByTestId("submit-count");

        await Assertions.Expect(checkbox).ToHaveJSPropertyAsync("tagName", "BUTTON");
        await Assertions.Expect(checkbox).ToHaveAttributeAsync("type", "button");
        await WaitForCheckedStateAsync(false);
        await Assertions.Expect(submitCount).ToHaveTextAsync("0");
        await checkbox.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await WaitForDelayAsync(100);

        await WaitForCheckedStateAsync(false);
        await Assertions.Expect(submitCount).ToHaveTextAsync("1");
        await Assertions.Expect(formData).ToHaveTextAsync("");
    }

    /// <summary>
    /// Tests that disabled checkbox does not respond to keyboard.
    /// </summary>
    [Fact]
    public virtual async Task DisabledCheckbox_DoesNotRespondToKeyboard()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox")
            .WithDisabled(true));

        var checkbox = GetCheckbox();
        await WaitForCheckedStateAsync(false);

        await checkbox.FocusAsync(new LocatorFocusOptions { Timeout = 1000 });
        await Page.Keyboard.PressAsync("Space");
        await WaitForDelayAsync(100);

        await WaitForCheckedStateAsync(false);
    }

    /// <summary>
    /// Tests that readonly checkbox does not respond to keyboard.
    /// </summary>
    [Fact]
    public virtual async Task ReadOnlyCheckbox_DoesNotRespondToKeyboard()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox")
            .WithReadOnly(true));

        var checkbox = GetCheckbox();
        await WaitForCheckedStateAsync(false);

        await checkbox.FocusAsync();
        await Page.Keyboard.PressAsync("Space");
        await WaitForDelayAsync(100);

        await WaitForCheckedStateAsync(false);
    }

    #endregion

    #region Focus Management Tests

    /// <summary>
    /// Tests that the checkbox can receive focus.
    /// </summary>
    [Fact]
    public virtual async Task Checkbox_CanReceiveFocus()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox"));

        var checkbox = GetCheckbox();
        await checkbox.FocusAsync();

        await Assertions.Expect(checkbox).ToBeFocusedAsync();
    }

    /// <summary>
    /// Tests that focus event is triggered when checkbox receives focus.
    /// </summary>
    [Fact]
    public virtual async Task Focus_InvokesFocusCallback()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox"));

        var checkbox = GetCheckbox();
        var focusCount = GetByTestId("focus-count");

        await Assertions.Expect(focusCount).ToHaveTextAsync("0");

        await checkbox.FocusAsync();
        await WaitForDelayAsync(100);

        await Assertions.Expect(focusCount).ToHaveTextAsync("1");
    }

    /// <summary>
    /// Tests that blur event is triggered when checkbox loses focus.
    /// </summary>
    [Fact]
    public virtual async Task Blur_InvokesBlurCallback()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox"));

        var checkbox = GetCheckbox();
        var outsideButton = GetByTestId("outside-button");
        var blurCount = GetByTestId("blur-count");

        await checkbox.FocusAsync();
        await WaitForDelayAsync(50);

        await outsideButton.FocusAsync();
        await WaitForDelayAsync(100);

        await Assertions.Expect(blurCount).ToHaveTextAsync("1");
    }

    /// <summary>
    /// Tests that Tab key navigates to the checkbox.
    /// </summary>
    [Fact]
    public virtual async Task Tab_NavigatesToCheckbox()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox"));

        var outsideButton = GetByTestId("outside-button");
        await outsideButton.FocusAsync();

        await Page.Keyboard.PressAsync("Tab");
        await WaitForDelayAsync(100);

        var checkbox = GetCheckbox();
        await Assertions.Expect(checkbox).ToBeFocusedAsync();
    }

    #endregion

    #region Label Association Tests

    /// <summary>
    /// Tests that clicking a wrapping label toggles the checkbox.
    /// Requires real browser label click behavior.
    /// </summary>
    [Fact]
    public virtual async Task WrappingLabel_TogglesCheckbox()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox")
            .WithShowWrappingLabel(true));

        var label = GetByTestId("wrapping-label");
        await WaitForCheckedStateAsync(false);

        await label.ClickAsync();
        await WaitForDelayAsync(100);

        await WaitForCheckedStateAsync(true);
    }

    /// <summary>
    /// Tests that clicking an explicitly linked label toggles the checkbox.
    /// Requires real browser label-for association.
    /// </summary>
    [Fact]
    public virtual async Task LinkedLabel_TogglesCheckbox()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox")
            .WithShowLabel(true));

        var label = GetByTestId("linked-label");
        await WaitForCheckedStateAsync(false);

        await label.ClickAsync();
        await WaitForDelayAsync(100);

        await WaitForCheckedStateAsync(true);
    }

    #endregion

    #region External State Control Tests

    /// <summary>
    /// Tests that the checkbox updates when controlled externally.
    /// </summary>
    [Fact]
    public virtual async Task ExternalToggle_UpdatesCheckboxState()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox"));

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
    /// Tests that defaultChecked initializes the checkbox as checked.
    /// </summary>
    [Fact]
    public virtual async Task DefaultChecked_InitializesAsChecked()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox")
            .WithDefaultChecked(true));

        await WaitForCheckedStateAsync(true);
    }

    /// <summary>
    /// Tests that native button mode renders a button root and keeps Enter from toggling.
    /// </summary>
    [Fact]
    public virtual async Task NativeButton_RendersButtonAndEnterDoesNotToggle()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox")
            .WithNativeButton(true));

        var checkbox = GetCheckbox();
        await Assertions.Expect(checkbox).ToHaveJSPropertyAsync("tagName", "BUTTON");
        await Assertions.Expect(checkbox).ToHaveAttributeAsync("type", "button");

        await WaitForCheckedStateAsync(false);
        await checkbox.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await WaitForDelayAsync(100);

        await WaitForCheckedStateAsync(false);

        await Page.Keyboard.PressAsync("Space");
        await WaitForDelayAsync(100);
        await WaitForCheckedStateAsync(true);
    }

    #endregion

    #region Indeterminate State Tests

    /// <summary>
    /// Tests that indeterminate checkbox has aria-checked="mixed".
    /// </summary>
    [Fact]
    public virtual async Task Indeterminate_HasAriaCheckedMixed()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox")
            .WithIndeterminate(true));

        await WaitForIndeterminateStateAsync();
    }

    /// <summary>
    /// Tests that indeterminate checkbox shows the indicator.
    /// </summary>
    [Fact]
    public virtual async Task Indeterminate_ShowsIndicator()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox")
            .WithIndeterminate(true));

        var indicator = GetCheckboxIndicator();
        await Assertions.Expect(indicator).ToBeVisibleAsync();
    }

    /// <summary>
    /// Tests that clicking indeterminate checkbox invokes change callback.
    /// Note: The indeterminate state is externally controlled via the Indeterminate parameter.
    /// The component does not auto-clear indeterminate on click - that's consumer responsibility.
    /// </summary>
    [Fact]
    public virtual async Task Indeterminate_ClickInvokesChangeCallback()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox")
            .WithIndeterminate(true));

        await WaitForIndeterminateStateAsync();

        var changeCount = GetByTestId("change-count");
        await Assertions.Expect(changeCount).ToHaveTextAsync("0");

        var checkbox = GetCheckbox();
        await checkbox.ClickAsync();
        await WaitForDelayAsync(100);

        await Assertions.Expect(changeCount).ToHaveTextAsync("1");
    }

    /// <summary>
    /// Tests that data-indeterminate attribute is present when indeterminate.
    /// </summary>
    [Fact]
    public virtual async Task Indeterminate_HasDataIndeterminateAttribute()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox")
            .WithIndeterminate(true));

        var checkbox = GetCheckbox();
        await Assertions.Expect(checkbox).ToHaveAttributeAsync("data-indeterminate", "");
    }

    #endregion

    #region Form Integration Tests

    /// <summary>
    /// Tests that the checkbox value is included in form submission when checked.
    /// </summary>
    [Fact]
    public virtual async Task FormSubmission_IncludesValueWhenChecked()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox")
            .WithShowForm(true)
            .WithSwitchName("agree")
            .WithDefaultChecked(true));

        var submitButton = GetByTestId("submit-button");
        await submitButton.ClickAsync();
        await WaitForDelayAsync(100);

        var formData = GetByTestId("form-data");
        await Assertions.Expect(formData).ToContainTextAsync("agree=on");
    }

    /// <summary>
    /// Tests that the checkbox value is not included when unchecked and no uncheckedValue.
    /// </summary>
    [Fact]
    public virtual async Task FormSubmission_ExcludesValueWhenUnchecked()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox")
            .WithShowForm(true)
            .WithSwitchName("agree"));

        await WaitForCheckedStateAsync(false);

        var submitButton = GetByTestId("submit-button");
        await submitButton.ClickAsync();
        await WaitForDelayAsync(100);

        var formData = GetByTestId("form-data");
        var text = await formData.TextContentAsync();
        Assert.True(string.IsNullOrEmpty(text), "Form data should be empty when checkbox is unchecked without uncheckedValue");
    }

    /// <summary>
    /// Tests that uncheckedValue is submitted when checkbox is unchecked.
    /// </summary>
    [Fact]
    public virtual async Task FormSubmission_IncludesUncheckedValue()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox")
            .WithShowForm(true)
            .WithSwitchName("agree")
            .WithUncheckedValue("no"));

        await WaitForCheckedStateAsync(false);

        var submitButton = GetByTestId("submit-button");
        await submitButton.ClickAsync();
        await WaitForDelayAsync(100);

        var formData = GetByTestId("form-data");
        await Assertions.Expect(formData).ToContainTextAsync("agree=no");
    }

    /// <summary>
    /// Tests that custom value is submitted when checkbox is checked.
    /// </summary>
    [Fact]
    public virtual async Task FormSubmission_IncludesCustomValue()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox")
            .WithShowForm(true)
            .WithSwitchName("agree")
            .WithSwitchValue("yes")
            .WithDefaultChecked(true));

        var submitButton = GetByTestId("submit-button");
        await submitButton.ClickAsync();
        await WaitForDelayAsync(100);

        var formData = GetByTestId("form-data");
        await Assertions.Expect(formData).ToContainTextAsync("agree=yes");
    }

    #endregion

    #region Data Attribute Tests

    /// <summary>
    /// Tests that data-checked is applied when checked.
    /// </summary>
    [Fact]
    public virtual async Task DataChecked_AppliedWhenChecked()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox")
            .WithDefaultChecked(true));

        var checkbox = GetCheckbox();
        await Assertions.Expect(checkbox).ToHaveAttributeAsync("data-checked", "");
    }

    /// <summary>
    /// Tests that data-unchecked is applied when unchecked.
    /// </summary>
    [Fact]
    public virtual async Task DataUnchecked_AppliedWhenUnchecked()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox"));

        var checkbox = GetCheckbox();
        await Assertions.Expect(checkbox).ToHaveAttributeAsync("data-unchecked", "");
    }

    /// <summary>
    /// Tests that data attributes toggle correctly.
    /// </summary>
    [Fact]
    public virtual async Task DataAttributes_ToggleCorrectly()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox"));

        var checkbox = GetCheckbox();

        // Initially unchecked
        await Assertions.Expect(checkbox).ToHaveAttributeAsync("data-unchecked", "");

        // Toggle
        await checkbox.ClickAsync();
        await WaitForDelayAsync(100);

        // Should now be checked
        await Assertions.Expect(checkbox).ToHaveAttributeAsync("data-checked", "");

        // Toggle again
        await checkbox.ClickAsync();
        await WaitForDelayAsync(100);

        // Should be unchecked again
        await Assertions.Expect(checkbox).ToHaveAttributeAsync("data-unchecked", "");
    }

    /// <summary>
    /// Tests that data-disabled is applied when disabled.
    /// </summary>
    [Fact]
    public virtual async Task DataDisabled_AppliedWhenDisabled()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox")
            .WithDisabled(true));

        var checkbox = GetCheckbox();
        await Assertions.Expect(checkbox).ToHaveAttributeAsync("data-disabled", "");
    }

    /// <summary>
    /// Tests that data-readonly is applied when readonly.
    /// </summary>
    [Fact]
    public virtual async Task DataReadonly_AppliedWhenReadonly()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox")
            .WithReadOnly(true));

        var checkbox = GetCheckbox();
        await Assertions.Expect(checkbox).ToHaveAttributeAsync("data-readonly", "");
    }

    /// <summary>
    /// Tests that data-required is applied when required.
    /// </summary>
    [Fact]
    public virtual async Task DataRequired_AppliedWhenRequired()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox")
            .WithRequired(true));

        var checkbox = GetCheckbox();
        await Assertions.Expect(checkbox).ToHaveAttributeAsync("data-required", "");
    }

    /// <summary>
    /// Tests that indicator also gets data attributes.
    /// </summary>
    [Fact]
    public virtual async Task Indicator_HasMatchingDataAttributes()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox")
            .WithDefaultChecked(true));

        var indicator = GetCheckboxIndicator();
        await Assertions.Expect(indicator).ToHaveAttributeAsync("data-checked", "");

        var checkbox = GetCheckbox();
        await checkbox.ClickAsync();

        // Wait for the indicator's 150ms transition delay plus buffer before unmounting
        await WaitForDelayAsync(300);

        // Indicator should be hidden when unchecked (unless keepMounted)
        var indicatorCount = await Page.Locator("[data-testid='checkbox-indicator']").CountAsync();
        Assert.Equal(0, indicatorCount);
    }

    #endregion

    #region Hidden Input Tests

    /// <summary>
    /// Tests that the hidden input is synchronized with checkbox state.
    /// </summary>
    [Fact]
    public virtual async Task HiddenInput_SynchronizedWithCheckboxState()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox"));

        var hiddenInput = GetHiddenInput();

        // Initially unchecked
        var isChecked = await hiddenInput.IsCheckedAsync();
        Assert.False(isChecked);

        // Toggle
        var checkbox = GetCheckbox();
        await checkbox.ClickAsync();
        await WaitForDelayAsync(100);

        // Should now be checked
        isChecked = await hiddenInput.IsCheckedAsync();
        Assert.True(isChecked);
    }

    /// <summary>
    /// Tests that clicking the hidden input toggles the checkbox.
    /// </summary>
    [Fact]
    public virtual async Task HiddenInput_ClickTogglesCheckbox()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox"));

        var hiddenInput = GetHiddenInput();
        await WaitForCheckedStateAsync(false);

        // Use JavaScript click to programmatically trigger the input's click handler
        await hiddenInput.EvaluateAsync("el => el.click()");
        await WaitForDelayAsync(100);

        await WaitForCheckedStateAsync(true);
    }

    #endregion

    #region State Display Tests

    /// <summary>
    /// Tests that the state display updates when checkbox toggles.
    /// </summary>
    [Fact]
    public virtual async Task StateDisplay_UpdatesOnToggle()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox"));

        var checkedState = GetByTestId("checked-state");
        await Assertions.Expect(checkedState).ToHaveTextAsync("false");

        var checkbox = GetCheckbox();
        await checkbox.ClickAsync();
        await WaitForDelayAsync(100);

        await Assertions.Expect(checkedState).ToHaveTextAsync("true");
    }

    #endregion

    #region ARIA Attribute Tests

    /// <summary>
    /// Tests that checkbox has role="checkbox".
    /// </summary>
    [Fact]
    public virtual async Task Checkbox_HasRoleCheckbox()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox"));

        var checkbox = GetCheckbox();
        await Assertions.Expect(checkbox).ToHaveAttributeAsync("role", "checkbox");
    }

    /// <summary>
    /// Tests that disabled checkbox has tabindex="-1" to prevent focus.
    /// Note: The component uses data-disabled for styling, not aria-disabled.
    /// </summary>
    [Fact]
    public virtual async Task DisabledCheckbox_HasNegativeTabindex()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox")
            .WithDisabled(true));

        var checkbox = GetCheckbox();
        await Assertions.Expect(checkbox).ToHaveAttributeAsync("tabindex", "-1");
    }

    /// <summary>
    /// Tests that readonly checkbox has aria-readonly="true".
    /// </summary>
    [Fact]
    public virtual async Task ReadonlyCheckbox_HasAriaReadonly()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox")
            .WithReadOnly(true));

        var checkbox = GetCheckbox();
        await Assertions.Expect(checkbox).ToHaveAttributeAsync("aria-readonly", "true");
    }

    /// <summary>
    /// Tests that required checkbox has aria-required="true".
    /// </summary>
    [Fact]
    public virtual async Task RequiredCheckbox_HasAriaRequired()
    {
        await NavigateAsync(CreateUrl("/tests/checkbox")
            .WithRequired(true));

        var checkbox = GetCheckbox();
        await Assertions.Expect(checkbox).ToHaveAttributeAsync("aria-required", "true");
    }

    #endregion
}
