using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Tests.RadioGroup;

/// <summary>
/// Playwright tests for RadioGroup component - focused on browser-specific behavior.
/// Static rendering, attribute forwarding, and basic state tests are handled by bUnit.
/// These tests cover: click interactions, keyboard navigation, disabled/readonly behavior,
/// value change callbacks, form integration, external state control, and focus management.
/// </summary>
public abstract class RadioGroupTestsBase : TestBase
{
    protected RadioGroupTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    #region Helper Methods

    protected ILocator GetRadioGroup() => GetByTestId("radio-group");

    protected ILocator GetRadio(string value) => GetByTestId($"radio-{value}");

    protected async Task<bool> IsRadioCheckedAsync(string value)
    {
        var radio = GetRadio(value);
        var ariaChecked = await radio.GetAttributeAsync("aria-checked");
        return ariaChecked == "true";
    }

    protected async Task WaitForRadioStateAsync(string value, bool expected, int timeout = 5000)
    {
        var effectiveTimeout = timeout * TimeoutMultiplier;
        var radio = GetRadio(value);
        await Assertions.Expect(radio).ToHaveAttributeAsync(
            "aria-checked",
            expected ? "true" : "false",
            new LocatorAssertionsToHaveAttributeOptions { Timeout = effectiveTimeout });
    }

    protected async Task<string> GetCurrentValueAsync()
    {
        var valueState = GetByTestId("value-state");
        var text = await valueState.TextContentAsync();
        return text ?? "";
    }

    #endregion

    #region Click Interaction Tests

    /// <summary>
    /// Tests that clicking a radio selects it.
    /// </summary>
    [Fact]
    public virtual async Task Click_SelectsRadio()
    {
        await NavigateAsync(CreateUrl("/tests/radiogroup"));

        var radioA = GetRadio("a");
        await WaitForRadioStateAsync("a", false);

        await radioA.ClickAsync();
        await WaitForDelayAsync(100);

        await WaitForRadioStateAsync("a", true);
        var value = await GetCurrentValueAsync();
        Assert.Equal("a", value);
    }

    /// <summary>
    /// Tests that clicking a different radio changes the selection.
    /// </summary>
    [Fact]
    public virtual async Task Click_ChangesSelection()
    {
        await NavigateAsync(CreateUrl("/tests/radiogroup")
            .WithRadioDefaultValue("a"));

        await WaitForRadioStateAsync("a", true);

        var radioB = GetRadio("b");
        await radioB.ClickAsync();
        await WaitForDelayAsync(100);

        await WaitForRadioStateAsync("a", false);
        await WaitForRadioStateAsync("b", true);
    }

    /// <summary>
    /// Tests that clicking an already-selected radio does not deselect it.
    /// </summary>
    [Fact]
    public virtual async Task Click_DoesNotDeselectCurrentRadio()
    {
        await NavigateAsync(CreateUrl("/tests/radiogroup")
            .WithRadioDefaultValue("a"));

        await WaitForRadioStateAsync("a", true);

        var radioA = GetRadio("a");
        await radioA.ClickAsync();
        await WaitForDelayAsync(100);

        // Should still be checked
        await WaitForRadioStateAsync("a", true);
    }

    #endregion

    #region Keyboard Navigation Tests

    /// <summary>
    /// Helper to perform an arrow key navigation on a radio element.
    /// Waits for initial state, ensures focus, and uses a generous timeout
    /// since keyboard navigation involves multiple JS interop round-trips.
    /// Uses Locator.PressAsync which atomically focuses and presses the key,
    /// avoiding race conditions with Page.Keyboard.PressAsync in concurrent tests.
    /// </summary>
    private async Task PerformArrowNavigationAsync(string fromRadio, string key, string expectedRadio)
    {
        await WaitForRadioStateAsync(fromRadio, true);

        // Allow OnAfterRenderAsync JS initialization (initializeGroup + registerRadio)
        // to complete before attempting keyboard navigation. The aria-checked attribute
        // is set during render, but the JS radio registration happens asynchronously
        // in OnAfterRenderAsync and may still be in progress under concurrent load.
        await WaitForDelayAsync(500);

        // Under concurrent test load, keyboard events can occasionally be lost.
        // Retry the key press if the expected state isn't reached.
        // Uses Locator.PressAsync which atomically focuses and presses the key.
        var radio = GetRadio(fromRadio);
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            await radio.PressAsync(key);

            try
            {
                await WaitForRadioStateAsync(expectedRadio, true, timeout: 5000);
                return;
            }
            catch when (attempt < 3)
            {
                // Key press was likely lost due to concurrent load. Retry.
                await WaitForDelayAsync(500);
            }
        }
    }

    /// <summary>
    /// Tests that ArrowDown moves to the next radio.
    /// </summary>
    [Fact]
    public virtual async Task ArrowDown_MovesToNextRadio()
    {
        await NavigateAsync(CreateUrl("/tests/radiogroup")
            .WithRadioDefaultValue("a"));

        await PerformArrowNavigationAsync("a", "ArrowDown", "b");
    }

    /// <summary>
    /// Tests that ArrowUp moves to the previous radio.
    /// </summary>
    [Fact]
    public virtual async Task ArrowUp_MovesToPreviousRadio()
    {
        await NavigateAsync(CreateUrl("/tests/radiogroup")
            .WithRadioDefaultValue("b"));

        await PerformArrowNavigationAsync("b", "ArrowUp", "a");
    }

    /// <summary>
    /// Tests that ArrowDown wraps from last to first radio.
    /// </summary>
    [Fact]
    public virtual async Task ArrowDown_WrapsToFirst()
    {
        await NavigateAsync(CreateUrl("/tests/radiogroup")
            .WithRadioDefaultValue("c"));

        await PerformArrowNavigationAsync("c", "ArrowDown", "a");
    }

    /// <summary>
    /// Tests that ArrowUp wraps from first to last radio.
    /// </summary>
    [Fact]
    public virtual async Task ArrowUp_WrapsToLast()
    {
        await NavigateAsync(CreateUrl("/tests/radiogroup")
            .WithRadioDefaultValue("a"));

        await PerformArrowNavigationAsync("a", "ArrowUp", "c");
    }

    /// <summary>
    /// Tests that Space selects a radio.
    /// </summary>
    [Fact]
    public virtual async Task Space_SelectsRadio()
    {
        await NavigateAsync(CreateUrl("/tests/radiogroup"));

        var radioA = GetRadio("a");
        await radioA.PressAsync(" ");

        await WaitForRadioStateAsync("a", true);
    }

    #endregion

    #region Disabled Tests

    /// <summary>
    /// Tests that disabled group prevents interaction.
    /// </summary>
    [Fact]
    public virtual async Task DisabledGroup_PreventsInteraction()
    {
        await NavigateAsync(CreateUrl("/tests/radiogroup")
            .WithDisabled(true));

        var radioA = GetRadio("a");
        await radioA.ClickAsync(new LocatorClickOptions { Force = true });
        await WaitForDelayAsync(100);

        await WaitForRadioStateAsync("a", false);
    }

    /// <summary>
    /// Tests that disabled group propagates data-disabled to all radios.
    /// </summary>
    [Fact]
    public virtual async Task DisabledGroup_PropagatesDataAttribute()
    {
        await NavigateAsync(CreateUrl("/tests/radiogroup")
            .WithDisabled(true));

        var radioA = GetRadio("a");
        var radioB = GetRadio("b");
        var radioC = GetRadio("c");

        await Assertions.Expect(radioA).ToHaveAttributeAsync("data-disabled", "");
        await Assertions.Expect(radioB).ToHaveAttributeAsync("data-disabled", "");
        await Assertions.Expect(radioC).ToHaveAttributeAsync("data-disabled", "");
    }

    /// <summary>
    /// Tests that disabled group propagates aria-disabled to all radio roots.
    /// </summary>
    [Fact]
    public virtual async Task DisabledGroup_PropagatesAriaDisabled()
    {
        await NavigateAsync(CreateUrl("/tests/radiogroup")
            .WithDisabled(true));

        await Assertions.Expect(GetRadio("a")).ToHaveAttributeAsync("aria-disabled", "true");
        await Assertions.Expect(GetRadio("b")).ToHaveAttributeAsync("aria-disabled", "true");
        await Assertions.Expect(GetRadio("c")).ToHaveAttributeAsync("aria-disabled", "true");
    }

    #endregion

    #region ReadOnly Tests

    /// <summary>
    /// Tests that read-only group prevents interaction.
    /// </summary>
    [Fact]
    public virtual async Task ReadOnlyGroup_PreventsInteraction()
    {
        await NavigateAsync(CreateUrl("/tests/radiogroup")
            .WithReadOnly(true));

        var radioA = GetRadio("a");
        await radioA.ClickAsync(new LocatorClickOptions { Force = true });
        await WaitForDelayAsync(100);

        await WaitForRadioStateAsync("a", false);
    }

    /// <summary>
    /// Tests that keyboard navigation may focus a read-only radio but does not select it.
    /// </summary>
    [Fact]
    public virtual async Task ArrowNavigation_FocusesReadOnlyRadioWithoutSelecting()
    {
        await NavigateAsync(CreateUrl("/tests/radiogroup")
            .WithRadioDefaultValue("a")
            .WithRadioReadOnlyB(true));

        await WaitForRadioStateAsync("a", true);
        await WaitForDelayAsync(500);

        var radioA = GetRadio("a");
        var radioB = GetRadio("b");
        var radioC = GetRadio("c");
        await Assertions.Expect(radioB).ToHaveAttributeAsync("data-readonly", "");
        await Assertions.Expect(radioA).ToHaveAttributeAsync(
            "tabindex",
            "0",
            new LocatorAssertionsToHaveAttributeOptions { Timeout = 5000 * TimeoutMultiplier });
        await Assertions.Expect(radioC).ToHaveAttributeAsync(
            "tabindex",
            "-1",
            new LocatorAssertionsToHaveAttributeOptions { Timeout = 5000 * TimeoutMultiplier });

        var focusedReadOnlyTarget = false;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            await radioA.PressAsync("ArrowDown");

            try
            {
                await Assertions.Expect(radioB).ToBeFocusedAsync(
                    new LocatorAssertionsToBeFocusedOptions { Timeout = 5000 * TimeoutMultiplier });
                focusedReadOnlyTarget = true;
                break;
            }
            catch
            {
                if (attempt < 3)
                {
                    await WaitForDelayAsync(500);
                }
            }
        }

        Assert.True(focusedReadOnlyTarget, "Expected read-only radio-b to receive focus after ArrowDown.");
        await WaitForRadioStateAsync("a", true);
        await WaitForRadioStateAsync("b", false);
        await Assertions.Expect(GetByTestId("change-count")).ToHaveTextAsync("0");
    }

    #endregion

    #region OnValueChange Tests

    /// <summary>
    /// Tests that onValueChange is called on click.
    /// </summary>
    [Fact]
    public virtual async Task OnValueChange_CalledOnClick()
    {
        await NavigateAsync(CreateUrl("/tests/radiogroup"));

        var changeCount = GetByTestId("change-count");
        await Assertions.Expect(changeCount).ToHaveTextAsync("0");

        var radioA = GetRadio("a");
        await radioA.ClickAsync();
        await WaitForDelayAsync(100);

        await Assertions.Expect(changeCount).ToHaveTextAsync("1");
    }

    /// <summary>
    /// Tests that onValueChange is called for each interaction.
    /// </summary>
    [Fact]
    public virtual async Task OnValueChange_CalledForEachInteraction()
    {
        await NavigateAsync(CreateUrl("/tests/radiogroup"));

        var changeCount = GetByTestId("change-count");

        var radioA = GetRadio("a");
        var radioB = GetRadio("b");

        await radioA.ClickAsync();
        await WaitForDelayAsync(100);
        await Assertions.Expect(changeCount).ToHaveTextAsync("1");

        await radioB.ClickAsync();
        await WaitForDelayAsync(100);
        await Assertions.Expect(changeCount).ToHaveTextAsync("2");
    }

    #endregion

    #region Form Integration Tests

    /// <summary>
    /// Tests that form submission includes the selected value.
    /// </summary>
    [Fact]
    public virtual async Task FormSubmission_IncludesSelectedValue()
    {
        await NavigateAsync(CreateUrl("/tests/radiogroup")
            .WithShowForm(true)
            .WithRadioName("choice")
            .WithRadioDefaultValue("b"));

        var submitButton = GetByTestId("submit-button");
        await submitButton.ClickAsync();
        await WaitForDelayAsync(100);

        var formData = GetByTestId("form-data");
        var text = await formData.TextContentAsync();
        Assert.Contains("choice=b", text);
    }

    /// <summary>
    /// Tests that native FormData sees exactly one selected radio value and no group-level input.
    /// </summary>
    [Fact]
    public virtual async Task NativeFormData_IncludesOnlySelectedRadioInput()
    {
        await NavigateAsync(CreateUrl("/tests/radiogroup")
            .WithShowForm(true)
            .WithRadioName("choice")
            .WithRadioDefaultValue("b"));

        var inputCount = await Page.EvaluateAsync<int>(
            "() => document.querySelectorAll('form input[type=\"radio\"][name=\"choice\"]').length");
        var values = await Page.EvaluateAsync<string[]>(
            "() => Array.from(new FormData(document.querySelector('form')).getAll('choice')).map(String)");

        Assert.Equal(3, inputCount);
        Assert.Equal(["b"], values);
    }

    /// <summary>
    /// Tests that form submission with no value submits nothing.
    /// </summary>
    [Fact]
    public virtual async Task FormSubmission_NoValueWhenNoneSelected()
    {
        await NavigateAsync(CreateUrl("/tests/radiogroup")
            .WithShowForm(true)
            .WithRadioName("choice"));

        var submitButton = GetByTestId("submit-button");
        await submitButton.ClickAsync();
        await WaitForDelayAsync(100);

        var formData = GetByTestId("form-data");
        var text = await formData.TextContentAsync();
        Assert.True(string.IsNullOrEmpty(text));
    }

    /// <summary>
    /// Tests that native FormData has no value when no radio is selected.
    /// </summary>
    [Fact]
    public virtual async Task NativeFormData_HasNoValueWhenNoneSelected()
    {
        await NavigateAsync(CreateUrl("/tests/radiogroup")
            .WithShowForm(true)
            .WithRadioName("choice"));

        var values = await Page.EvaluateAsync<string[]>(
            "() => Array.from(new FormData(document.querySelector('form')).getAll('choice')).map(String)");

        Assert.Empty(values);
    }

    #endregion

    #region External State Control Tests

    /// <summary>
    /// Tests that external button can set the group value.
    /// </summary>
    [Fact]
    public virtual async Task ExternalSelect_UpdatesGroupValue()
    {
        await NavigateAsync(CreateUrl("/tests/radiogroup"));

        await WaitForRadioStateAsync("a", false);

        var selectA = GetByTestId("select-a");
        await selectA.ClickAsync();
        await WaitForDelayAsync(100);

        await WaitForRadioStateAsync("a", true);
    }

    /// <summary>
    /// Tests that external clear button deselects all radios.
    /// </summary>
    [Fact]
    public virtual async Task ExternalClear_DeselectsAll()
    {
        await NavigateAsync(CreateUrl("/tests/radiogroup")
            .WithRadioDefaultValue("a"));

        await WaitForRadioStateAsync("a", true);

        var clear = GetByTestId("clear");
        await clear.ClickAsync();
        await WaitForDelayAsync(100);

        await WaitForRadioStateAsync("a", false);
        await WaitForRadioStateAsync("b", false);
        await WaitForRadioStateAsync("c", false);
    }

    #endregion

    #region ARIA Tests

    /// <summary>
    /// Tests that the group has role="radiogroup".
    /// </summary>
    [Fact]
    public virtual async Task Group_HasRoleRadiogroup()
    {
        await NavigateAsync(CreateUrl("/tests/radiogroup"));

        var group = GetRadioGroup();
        await Assertions.Expect(group).ToHaveAttributeAsync("role", "radiogroup");
    }

    /// <summary>
    /// Tests that radios in the group have role="radio".
    /// </summary>
    [Fact]
    public virtual async Task Radios_HaveRoleRadio()
    {
        await NavigateAsync(CreateUrl("/tests/radiogroup"));

        var radioA = GetRadio("a");
        var radioB = GetRadio("b");
        var radioC = GetRadio("c");

        await Assertions.Expect(radioA).ToHaveAttributeAsync("role", "radio");
        await Assertions.Expect(radioB).ToHaveAttributeAsync("role", "radio");
        await Assertions.Expect(radioC).ToHaveAttributeAsync("role", "radio");
    }

    /// <summary>
    /// Tests that NativeButton renders a button radio root and keeps the hidden input separate.
    /// </summary>
    [Fact]
    public virtual async Task NativeButton_RendersButtonRootAndSelects()
    {
        await NavigateAsync(CreateUrl("/tests/radiogroup")
            .WithRadioNativeButton(true));

        var radioA = GetRadio("a");
        var tagName = await radioA.EvaluateAsync<string>("el => el.tagName.toLowerCase()");
        var hiddenInputId = await Page.EvaluateAsync<string?>(
            "() => document.querySelector('input[type=\"radio\"][value=\"a\"]')?.getAttribute('id')");

        Assert.Equal("button", tagName);
        await Assertions.Expect(radioA).ToHaveAttributeAsync("id", "radio-a-control");
        Assert.Null(hiddenInputId);

        await radioA.ClickAsync();
        await WaitForRadioStateAsync("a", true);
    }

    #endregion

    #region State Display Tests

    /// <summary>
    /// Tests that value state display updates correctly.
    /// </summary>
    [Fact]
    public virtual async Task ValueStateDisplay_UpdatesOnChange()
    {
        await NavigateAsync(CreateUrl("/tests/radiogroup"));

        var valueState = GetByTestId("value-state");
        await Assertions.Expect(valueState).ToHaveTextAsync("");

        var radioA = GetRadio("a");
        await radioA.ClickAsync();
        await WaitForDelayAsync(100);

        await Assertions.Expect(valueState).ToContainTextAsync("a");
    }

    #endregion

    #region Focus Management Tests

    /// <summary>
    /// Tests that Tab focuses the selected radio.
    /// </summary>
    [Fact]
    public virtual async Task Tab_FocusesSelectedRadio()
    {
        await NavigateAsync(CreateUrl("/tests/radiogroup")
            .WithRadioDefaultValue("b"));

        await WaitForRadioStateAsync("b", true);

        // Focus an element before the radio group
        var outsideButton = GetByTestId("outside-button");
        await outsideButton.FocusAsync();
        await WaitForDelayAsync(100);

        // Tab into the radio group
        await Page.Keyboard.PressAsync("Tab");

        // The selected radio (b) should receive focus
        var radioB = GetRadio("b");
        await Assertions.Expect(radioB).ToBeFocusedAsync(
            new LocatorAssertionsToBeFocusedOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    /// <summary>
    /// Tests that Tab focuses the first radio when none is selected.
    /// </summary>
    [Fact]
    public virtual async Task Tab_FocusesFirstRadioWhenNoneSelected()
    {
        await NavigateAsync(CreateUrl("/tests/radiogroup"));

        // Focus an element before the radio group
        var outsideButton = GetByTestId("outside-button");
        await outsideButton.FocusAsync();
        await WaitForDelayAsync(100);

        // Tab into the radio group
        await Page.Keyboard.PressAsync("Tab");

        // The first radio (a) should receive focus
        var radioA = GetRadio("a");
        await Assertions.Expect(radioA).ToBeFocusedAsync(
            new LocatorAssertionsToBeFocusedOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    #endregion
}
