using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Tests.Autocomplete;

/// <summary>
/// Browser tests for Autocomplete interaction, DOM attributes, JS preventDefault behavior,
/// focus retention, keyboard navigation, filtering, and inline completion.
/// </summary>
public abstract class AutocompleteTestsBase : TestBase
{
    protected AutocompleteTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    protected async Task WaitForAutocompleteOpenAsync()
    {
        await Assertions.Expect(GetByTestId("open-state")).ToHaveTextAsync("true",
            new LocatorAssertionsToHaveTextOptions { Timeout = 5000 * TimeoutMultiplier });
        await Assertions.Expect(GetByTestId("autocomplete-popup")).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    protected async Task WaitForAutocompleteClosedAsync()
    {
        await Assertions.Expect(GetByTestId("open-state")).ToHaveTextAsync("false",
            new LocatorAssertionsToHaveTextOptions { Timeout = 5000 * TimeoutMultiplier });
        await Assertions.Expect(GetByTestId("autocomplete-popup")).ToBeHiddenAsync(
            new LocatorAssertionsToBeHiddenOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    [Fact]
    public virtual async Task TypingFiltersItemsAndSetsEmptyState()
    {
        await NavigateAsync(CreateUrl("/tests/autocomplete").WithDefaultOpen(true));

        var input = GetByTestId("autocomplete-input");
        await input.FillAsync("zz");

        await Assertions.Expect(GetByTestId("autocomplete-empty")).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 5000 * TimeoutMultiplier });
        await Assertions.Expect(GetByTestId("autocomplete-input")).ToHaveAttributeAsync("data-list-empty", "");
        await Assertions.Expect(GetByTestId("autocomplete-trigger")).ToHaveAttributeAsync("data-list-empty", "");
        await Assertions.Expect(GetByTestId("autocomplete-popup")).ToHaveAttributeAsync("data-empty", "");
        await Assertions.Expect(Page.Locator("[role='option']")).ToHaveCountAsync(0);
    }

    [Fact]
    public virtual async Task ArrowNavigationEnterSelectsItemAndCloses()
    {
        await NavigateAsync(CreateUrl("/tests/autocomplete").WithDefaultOpen(true));
        await WaitForAutocompleteOpenAsync();

        var input = GetByTestId("autocomplete-input");
        await input.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");

        var apple = GetByTestId("autocomplete-item-apple");
        await Assertions.Expect(apple).ToHaveAttributeAsync("data-highlighted", "",
            new LocatorAssertionsToHaveAttributeOptions { Timeout = 5000 * TimeoutMultiplier });
        await Assertions.Expect(input).ToHaveAttributeAsync("aria-activedescendant",
            await apple.GetAttributeAsync("id") ?? "");

        await Page.Keyboard.PressAsync("Enter");

        await Assertions.Expect(input).ToHaveValueAsync("Apple",
            new LocatorAssertionsToHaveValueOptions { Timeout = 5000 * TimeoutMultiplier });
        await Assertions.Expect(GetByTestId("input-value")).ToHaveTextAsync("Apple");
        await Assertions.Expect(GetByTestId("last-reason")).ToHaveTextAsync("ItemPress");
        await WaitForAutocompleteClosedAsync();
    }

    [Fact]
    public virtual async Task BothModeInlineCompletionUsesTypedQueryForFiltering()
    {
        await NavigateAsync(CreateUrl("/tests/autocomplete")
            .WithDefaultOpen(true)
            .WithAutocompleteDefaultValue("Ap")
            .WithAutocompleteMode("both")
            .WithAutocompleteAutoHighlight("always"));
        await WaitForAutocompleteOpenAsync();

        var input = GetByTestId("autocomplete-input");
        await Assertions.Expect(input).ToHaveValueAsync("Apple",
            new LocatorAssertionsToHaveValueOptions { Timeout = 5000 * TimeoutMultiplier });
        await Assertions.Expect(Page.Locator("[role='option']")).ToHaveCountAsync(2);
        await Assertions.Expect(GetByTestId("autocomplete-item-apple")).ToBeVisibleAsync();
        await Assertions.Expect(GetByTestId("autocomplete-item-apricot")).ToBeVisibleAsync();

        await input.EvaluateAsync(
            "element => { element.value = 'Ba'; element.dispatchEvent(new Event('input', { bubbles: true })); }");

        await Assertions.Expect(input).ToHaveValueAsync("Banana",
            new LocatorAssertionsToHaveValueOptions { Timeout = 5000 * TimeoutMultiplier });
        await Assertions.Expect(Page.Locator("[role='option']")).ToHaveCountAsync(1);
        await Assertions.Expect(GetByTestId("autocomplete-item-banana")).ToBeVisibleAsync();
    }

    [Fact]
    public virtual async Task BothModeInlineCompletionPreservesTypedPrefixWhenContinuing()
    {
        await NavigateAsync(CreateUrl("/tests/autocomplete")
            .WithDefaultOpen(true)
            .WithAutocompleteDefaultValue("Ap")
            .WithAutocompleteMode("both")
            .WithAutocompleteAutoHighlight("always"));
        await WaitForAutocompleteOpenAsync();

        var input = GetByTestId("autocomplete-input");
        await Assertions.Expect(input).ToHaveValueAsync("Apple",
            new LocatorAssertionsToHaveValueOptions { Timeout = 5000 * TimeoutMultiplier });

        await input.FocusAsync();
        await Page.Keyboard.TypeAsync("r");

        await Assertions.Expect(GetByTestId("input-value")).ToHaveTextAsync("Apr",
            new LocatorAssertionsToHaveTextOptions { Timeout = 5000 * TimeoutMultiplier });
        await Assertions.Expect(input).ToHaveValueAsync("Apricot",
            new LocatorAssertionsToHaveValueOptions { Timeout = 5000 * TimeoutMultiplier });
        await Assertions.Expect(GetByTestId("autocomplete-item-apricot")).ToHaveAttributeAsync("data-highlighted", "",
            new LocatorAssertionsToHaveAttributeOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    [Fact]
    public virtual async Task FilterDisabledPreservesExternallyFilteredItems()
    {
        await NavigateAsync(CreateUrl("/tests/autocomplete")
            .WithDefaultOpen(true)
            .WithAutocompleteDefaultValue("zz")
            .WithAutocompleteFilterDisabled(true));
        await WaitForAutocompleteOpenAsync();

        await Assertions.Expect(GetByTestId("autocomplete-empty")).ToBeHiddenAsync(
            new LocatorAssertionsToBeHiddenOptions { Timeout = 5000 * TimeoutMultiplier });
        await Assertions.Expect(Page.Locator("[role='option']")).ToHaveCountAsync(4);
        await Assertions.Expect(GetByTestId("autocomplete-item-apple")).ToBeVisibleAsync();
        await Assertions.Expect(GetByTestId("autocomplete-item-apricot")).ToBeVisibleAsync();
        await Assertions.Expect(GetByTestId("autocomplete-item-banana")).ToBeVisibleAsync();
        await Assertions.Expect(GetByTestId("autocomplete-item-cherry")).ToBeVisibleAsync();
    }

    [Fact]
    public virtual async Task DisabledReadonlyRequiredAttributesAreExposed()
    {
        await NavigateAsync(CreateUrl("/tests/autocomplete")
            .WithDefaultOpen(true)
            .WithDisabled(true)
            .WithReadOnly(true)
            .WithRequired(true)
            .WithAutocompleteDefaultValue("zz"));

        var input = GetByTestId("autocomplete-input");
        await Assertions.Expect(input).ToBeDisabledAsync();
        await Assertions.Expect(input).ToHaveAttributeAsync("readonly", "readonly");
        await Assertions.Expect(input).ToHaveAttributeAsync("required", "required");
        await Assertions.Expect(input).ToHaveAttributeAsync("disabled", "disabled");
        await Assertions.Expect(input).ToHaveAttributeAsync("aria-readonly", "true");
        await Assertions.Expect(input).ToHaveAttributeAsync("aria-required", "true");
        await Assertions.Expect(input).ToHaveAttributeAsync("data-disabled", "true");
        await Assertions.Expect(input).ToHaveAttributeAsync("data-readonly", "true");
        await Assertions.Expect(input).ToHaveAttributeAsync("data-list-empty", "");

        var trigger = GetByTestId("autocomplete-trigger");
        await Assertions.Expect(trigger).ToBeDisabledAsync();
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-haspopup", "listbox");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-required", "true");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-disabled", "");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-readonly", "");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-required", "");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-list-empty", "");
    }

    [Fact]
    public virtual async Task EscapeAndOutsidePressClosePopup()
    {
        await NavigateAsync(CreateUrl("/tests/autocomplete").WithDefaultOpen(true));
        await WaitForAutocompleteOpenAsync();

        var input = GetByTestId("autocomplete-input");
        await input.FocusAsync();
        await Page.Keyboard.PressAsync("Escape");

        await WaitForAutocompleteClosedAsync();

        await GetByTestId("autocomplete-trigger").ClickAsync();
        await WaitForAutocompleteOpenAsync();

        await GetByTestId("outside-button").ClickAsync();
        await WaitForAutocompleteClosedAsync();
    }

    [Fact]
    public virtual async Task TriggerOpenFocusesInputRenderedInsidePopup()
    {
        await NavigateAsync(CreateUrl("/tests/autocomplete")
            .WithAutocompleteInputInsidePopup(true));

        await GetByTestId("outside-button").FocusAsync();
        await GetByTestId("autocomplete-trigger").ClickAsync();
        await WaitForAutocompleteOpenAsync();

        var input = GetByTestId("autocomplete-input");
        await Assertions.Expect(input).ToBeFocusedAsync(
            new LocatorAssertionsToBeFocusedOptions { Timeout = 5000 * TimeoutMultiplier });

        await input.FillAsync("Che");

        await Assertions.Expect(input).ToHaveValueAsync("Che",
            new LocatorAssertionsToHaveValueOptions { Timeout = 5000 * TimeoutMultiplier });
        await Assertions.Expect(GetByTestId("autocomplete-item-cherry")).ToBeVisibleAsync();
    }

    [Fact]
    public virtual async Task TriggerKeyboardActivationTogglesPopup()
    {
        await NavigateAsync(CreateUrl("/tests/autocomplete"));

        var trigger = GetByTestId("autocomplete-trigger");
        await trigger.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await WaitForAutocompleteOpenAsync();

        await Page.Keyboard.PressAsync("Escape");
        await WaitForAutocompleteClosedAsync();

        await trigger.FocusAsync();
        await Page.Keyboard.PressAsync("Space");
        await WaitForAutocompleteOpenAsync();
    }

    [Fact]
    public virtual async Task PopupPointerDownKeepsInputRenderedInsidePopupFocused()
    {
        await NavigateAsync(CreateUrl("/tests/autocomplete")
            .WithAutocompleteInputInsidePopup(true));

        await GetByTestId("autocomplete-trigger").ClickAsync();
        await WaitForAutocompleteOpenAsync();

        var input = GetByTestId("autocomplete-input");
        await input.FocusAsync();
        await Assertions.Expect(input).ToBeFocusedAsync();

        await GetByTestId("autocomplete-panel-padding").ClickAsync();

        await Assertions.Expect(input).ToBeFocusedAsync(
            new LocatorAssertionsToBeFocusedOptions { Timeout = 5000 * TimeoutMultiplier });
        await WaitForAutocompleteOpenAsync();
    }

    [Fact]
    public virtual async Task TabClosesPopupAndAllowsFocusNavigation()
    {
        await NavigateAsync(CreateUrl("/tests/autocomplete"));

        var input = GetByTestId("autocomplete-input");
        await input.FocusAsync();
        await input.FillAsync("a");
        await WaitForAutocompleteOpenAsync();

        await Page.Keyboard.PressAsync("Tab");

        await WaitForAutocompleteClosedAsync();
        await Assertions.Expect(GetByTestId("autocomplete-popup")).ToBeHiddenAsync(
            new LocatorAssertionsToBeHiddenOptions { Timeout = 5000 * TimeoutMultiplier });
        await Assertions.Expect(GetByTestId("autocomplete-trigger")).ToBeFocusedAsync(
            new LocatorAssertionsToBeFocusedOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    [Fact]
    public virtual async Task TabFromTriggerClosesPopupAndAllowsFocusNavigation()
    {
        await NavigateAsync(CreateUrl("/tests/autocomplete").WithDefaultOpen(true));
        await WaitForAutocompleteOpenAsync();

        var trigger = GetByTestId("autocomplete-trigger");
        await trigger.FocusAsync();

        await Page.Keyboard.PressAsync("Tab");

        await WaitForAutocompleteClosedAsync();
        await Assertions.Expect(GetByTestId("outside-button")).ToBeFocusedAsync(
            new LocatorAssertionsToBeFocusedOptions { Timeout = 5000 * TimeoutMultiplier });
    }
}
