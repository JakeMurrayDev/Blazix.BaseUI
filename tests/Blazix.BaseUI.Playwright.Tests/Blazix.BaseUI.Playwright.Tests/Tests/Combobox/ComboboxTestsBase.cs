using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Tests.Combobox;

/// <summary>
/// Browser tests for Combobox selection, form serialization, keyboard behavior, popup focus,
/// and accessibility attributes.
/// </summary>
public abstract class ComboboxTestsBase : TestBase
{
    protected ComboboxTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    protected async Task WaitForComboboxOpenAsync()
    {
        await Assertions.Expect(GetByTestId("open-state")).ToHaveTextAsync("true",
            new LocatorAssertionsToHaveTextOptions { Timeout = 5000 * TimeoutMultiplier });
        await Assertions.Expect(GetByTestId("combobox-popup")).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    protected async Task WaitForComboboxClosedAsync()
    {
        await Assertions.Expect(GetByTestId("open-state")).ToHaveTextAsync("false",
            new LocatorAssertionsToHaveTextOptions { Timeout = 5000 * TimeoutMultiplier });
        await Assertions.Expect(GetByTestId("combobox-popup")).ToBeHiddenAsync(
            new LocatorAssertionsToBeHiddenOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    [Fact]
    public virtual async Task ItemPressSelectsSingleValueSerializesHiddenInputAndCloses()
    {
        await NavigateAsync(CreateUrl("/tests/combobox").WithDefaultOpen(true));
        await WaitForComboboxOpenAsync();

        await GetByTestId("combobox-item-banana").ClickAsync();

        await Assertions.Expect(GetByTestId("combobox-input")).ToHaveValueAsync("Banana",
            new LocatorAssertionsToHaveValueOptions { Timeout = 5000 * TimeoutMultiplier });
        await Assertions.Expect(GetByTestId("selected-value")).ToHaveTextAsync("Banana",
            new LocatorAssertionsToHaveTextOptions { Timeout = 5000 * TimeoutMultiplier });
        await Assertions.Expect(GetByTestId("selected-values")).ToHaveTextAsync("");
        await Assertions.Expect(GetByTestId("last-reason")).ToHaveTextAsync("ItemPress");
        await Assertions.Expect(Page.Locator("input[aria-hidden='true'][name='fruit']")).ToHaveValueAsync("Banana");
        await WaitForComboboxClosedAsync();
    }

    [Fact]
    public virtual async Task MultipleItemPressTogglesValuesKeepsPopupOpenAndSerializesRepeatedInputs()
    {
        await NavigateAsync(CreateUrl("/tests/combobox")
            .WithDefaultOpen(true)
            .WithComboboxMultiple(true)
            .WithComboboxDefaultValues("Apple"));
        await WaitForComboboxOpenAsync();

        var apple = GetByTestId("combobox-item-apple");
        await Assertions.Expect(apple).ToHaveAttributeAsync("aria-selected", "true");
        await Assertions.Expect(apple).ToHaveAttributeAsync("data-selected", "");
        await Assertions.Expect(GetByTestId("combobox-item-indicator-apple")).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 * TimeoutMultiplier });

        await GetByTestId("combobox-item-banana").ClickAsync();

        await Assertions.Expect(GetByTestId("selected-values")).ToHaveTextAsync("Apple,Banana",
            new LocatorAssertionsToHaveTextOptions { Timeout = 5000 * TimeoutMultiplier });
        await Assertions.Expect(GetByTestId("combobox-chip-apple")).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 * TimeoutMultiplier });
        await Assertions.Expect(GetByTestId("combobox-chip-banana")).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 * TimeoutMultiplier });
        await Assertions.Expect(GetByTestId("combobox-item-banana")).ToHaveAttributeAsync("aria-selected", "true");
        await Assertions.Expect(GetByTestId("combobox-item-indicator-banana")).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 * TimeoutMultiplier });
        await ExpectHiddenInputValuesAsync("Apple", "Banana");
        await WaitForComboboxOpenAsync();

        await Page.Keyboard.PressAsync("Escape");
        await WaitForComboboxClosedAsync();

        await GetByTestId("combobox-chip-remove-apple").ClickAsync();

        await Assertions.Expect(GetByTestId("selected-values")).ToHaveTextAsync("Banana",
            new LocatorAssertionsToHaveTextOptions { Timeout = 5000 * TimeoutMultiplier });
        await Assertions.Expect(GetByTestId("last-reason")).ToHaveTextAsync("ChipRemovePress");
        await ExpectHiddenInputValuesAsync("Banana");
    }

    [Fact]
    public virtual async Task ClearPressClearsSelectedValueAndInputWithoutMovingFocus()
    {
        await NavigateAsync(CreateUrl("/tests/combobox")
            .WithComboboxDefaultValue("Apple")
            .WithDefaultOpen(true));
        await WaitForComboboxOpenAsync();

        var input = GetByTestId("combobox-input");
        await input.FocusAsync();

        await GetByTestId("combobox-clear").ClickAsync();

        await Assertions.Expect(input).ToBeFocusedAsync(
            new LocatorAssertionsToBeFocusedOptions { Timeout = 5000 * TimeoutMultiplier });
        await Assertions.Expect(input).ToHaveValueAsync("");
        await Assertions.Expect(GetByTestId("selected-value")).ToHaveTextAsync("");
        await Assertions.Expect(GetByTestId("input-value")).ToHaveTextAsync("");
        await Assertions.Expect(GetByTestId("last-reason")).ToHaveTextAsync("ClearPress");
        await Assertions.Expect(Page.Locator("input[aria-hidden='true'][name='fruit']")).ToHaveValueAsync("");
    }

    [Fact]
    public virtual async Task InputClickOpensPopupByDefault()
    {
        await NavigateAsync(CreateUrl("/tests/combobox"));
        await WaitForComboboxClosedAsync();

        await GetByTestId("combobox-input").ClickAsync();

        await WaitForComboboxOpenAsync();
    }

    [Fact]
    public virtual async Task OpenedPopupClearsStartingStyleAndBecomesOpaque()
    {
        await NavigateAsync(CreateUrl("/tests/combobox"));
        await WaitForComboboxClosedAsync();

        await GetByTestId("combobox-input").ClickAsync();
        await WaitForComboboxOpenAsync();

        var popup = GetByTestId("combobox-popup");
        await Assertions.Expect(popup).Not.ToHaveAttributeAsync("data-starting-style", "");
        await Assertions.Expect(popup).ToHaveCSSAsync("opacity", "1");
    }

    [Fact]
    public virtual async Task ArrowNavigationEnterSelectsActiveItem()
    {
        await NavigateAsync(CreateUrl("/tests/combobox").WithDefaultOpen(true));
        await WaitForComboboxOpenAsync();

        var input = GetByTestId("combobox-input");
        await input.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");

        var apple = GetByTestId("combobox-item-apple");
        await Assertions.Expect(apple).ToHaveAttributeAsync("data-highlighted", "",
            new LocatorAssertionsToHaveAttributeOptions { Timeout = 5000 * TimeoutMultiplier });
        await Assertions.Expect(input).ToHaveAttributeAsync("aria-activedescendant",
            await apple.GetAttributeAsync("id") ?? "");

        await Page.Keyboard.PressAsync("Enter");

        await Assertions.Expect(input).ToHaveValueAsync("Apple",
            new LocatorAssertionsToHaveValueOptions { Timeout = 5000 * TimeoutMultiplier });
        await Assertions.Expect(GetByTestId("selected-value")).ToHaveTextAsync("Apple");
        await Assertions.Expect(GetByTestId("last-reason")).ToHaveTextAsync("ItemPress");
        await WaitForComboboxClosedAsync();
    }

    [Fact]
    public virtual async Task EnterWithOpenPopupAndNoActiveItemSubmitsForm()
    {
        await NavigateAsync(CreateUrl("/tests/combobox").WithDefaultOpen(true));
        await WaitForComboboxOpenAsync();

        var input = GetByTestId("combobox-input");
        await input.FocusAsync();
        await Assertions.Expect(input).Not.ToHaveAttributeAsync(
            "aria-activedescendant",
            new System.Text.RegularExpressions.Regex(".+"));

        await Page.Keyboard.PressAsync("Enter");

        await Assertions.Expect(GetByTestId("submit-count")).ToHaveTextAsync("1",
            new LocatorAssertionsToHaveTextOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    [Fact]
    public virtual async Task DisabledReadonlyRequiredAttributesAreExposed()
    {
        await NavigateAsync(CreateUrl("/tests/combobox")
            .WithDefaultOpen(true)
            .WithDisabled(true)
            .WithReadOnly(true)
            .WithRequired(true)
            .WithComboboxDefaultValue("Apple"));

        var input = GetByTestId("combobox-input");
        await Assertions.Expect(input).ToBeDisabledAsync();
        await Assertions.Expect(input).ToHaveAttributeAsync("readonly", "readonly");
        await Assertions.Expect(input).ToHaveAttributeAsync("required", "required");
        await Assertions.Expect(input).ToHaveAttributeAsync("disabled", "disabled");
        await Assertions.Expect(input).ToHaveAttributeAsync("aria-readonly", "true");
        await Assertions.Expect(input).ToHaveAttributeAsync("aria-required", "true");
        await Assertions.Expect(input).ToHaveAttributeAsync("data-disabled", "true");
        await Assertions.Expect(input).ToHaveAttributeAsync("data-readonly", "true");

        var trigger = GetByTestId("combobox-trigger");
        await Assertions.Expect(trigger).ToBeDisabledAsync();
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-haspopup", "listbox");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-required", "true");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-disabled", "");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-readonly", "");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-required", "");

        var hidden = Page.Locator("input[aria-hidden='true'][name='fruit']");
        await Assertions.Expect(hidden).ToHaveAttributeAsync("disabled", "");
        await Assertions.Expect(hidden).ToHaveAttributeAsync("readonly", "");
        await Assertions.Expect(hidden).ToHaveAttributeAsync("required", "");
        await Assertions.Expect(hidden).ToHaveValueAsync("Apple");
    }

    [Fact]
    public virtual async Task TriggerOpenFocusesInputRenderedInsidePopupAndPopupPointerDownKeepsFocus()
    {
        await NavigateAsync(CreateUrl("/tests/combobox")
            .WithComboboxInputInsidePopup(true));

        await GetByTestId("outside-button").FocusAsync();
        await GetByTestId("combobox-trigger").ClickAsync();
        await WaitForComboboxOpenAsync();

        var input = GetByTestId("combobox-input");
        await Assertions.Expect(input).ToBeFocusedAsync(
            new LocatorAssertionsToBeFocusedOptions { Timeout = 5000 * TimeoutMultiplier });

        await input.FillAsync("Che");

        await Assertions.Expect(input).ToHaveValueAsync("Che",
            new LocatorAssertionsToHaveValueOptions { Timeout = 5000 * TimeoutMultiplier });
        await Assertions.Expect(GetByTestId("combobox-item-cherry")).ToBeVisibleAsync();

        await GetByTestId("combobox-panel-padding").ClickAsync();

        await Assertions.Expect(input).ToBeFocusedAsync(
            new LocatorAssertionsToBeFocusedOptions { Timeout = 5000 * TimeoutMultiplier });
        await WaitForComboboxOpenAsync();
    }

    private async Task ExpectHiddenInputValuesAsync(params string[] expectedValues)
    {
        var inputs = Page.Locator("input[type='hidden'][name='fruit']");
        await Assertions.Expect(inputs).ToHaveCountAsync(expectedValues.Length,
            new LocatorAssertionsToHaveCountOptions { Timeout = 5000 * TimeoutMultiplier });

        var values = await inputs.EvaluateAllAsync<string[]>("elements => elements.map(element => element.value)");
        Assert.Equal(expectedValues, values);
    }
}
