using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Tests.Button;

/// <summary>
/// Playwright tests for Button component - focused on browser interactions, JS interop, and focus behavior.
/// Static rendering and attribute tests are handled by bUnit.
/// </summary>
public abstract class ButtonTestsBase : TestBase
{
    protected ButtonTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    #region Helper Methods

    protected ILocator GetButton() => GetByTestId("button-under-test");
    protected ILocator GetClickCount() => GetByTestId("click-count");
    protected ILocator GetFocusCount() => GetByTestId("focus-count");
    protected ILocator GetBlurCount() => GetByTestId("blur-count");

    /// <summary>
    /// Waits for the Button JS interop to attach event handlers on the element.
    /// In Server mode, the JS module load + sync call goes over SignalR and may
    /// not be ready immediately after the component renders.
    /// </summary>
    protected async Task WaitForButtonJsAsync()
    {
        await Page.WaitForFunctionAsync(@"() => {
            const el = document.querySelector('[data-testid=""button-under-test""]');
            if (!el) return false;
            const stateKey = Symbol.for('Blazix.BaseUI.Button.State');
            return el[stateKey] !== undefined;
        }", new PageWaitForFunctionOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    #endregion

    #region Non-native keyboard activation

    [Fact]
    public virtual async Task NonNativeButton_ActivatesWithEnterKey()
    {
        await NavigateAsync(CreateUrl("/tests/button")
            .WithNativeButton(false)
            .WithAs("span"));

        await WaitForButtonJsAsync();

        var button = GetButton();
        await button.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await WaitForDelayAsync(100);

        await Assertions.Expect(GetClickCount()).ToHaveTextAsync("1");
    }

    [Fact]
    public virtual async Task NonNativeButton_ActivatesWithSpaceKey()
    {
        await NavigateAsync(CreateUrl("/tests/button")
            .WithNativeButton(false)
            .WithAs("span"));

        await WaitForButtonJsAsync();

        var button = GetButton();
        await button.FocusAsync();
        await Page.Keyboard.PressAsync("Space");
        await WaitForDelayAsync(100);

        await Assertions.Expect(GetClickCount()).ToHaveTextAsync("1");
    }

    #endregion

    #region Disabled focus behavior

    [Fact]
    public virtual async Task DisabledNativeButton_IsNotFocusable()
    {
        await NavigateAsync(CreateUrl("/tests/button")
            .WithDisabled(true)
            .WithNativeButton(true));

        var beforeButton = GetByTestId("before-button");
        await beforeButton.FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await WaitForDelayAsync(100);

        // Should skip the disabled button and land on the after button
        var afterButton = GetByTestId("after-button");
        await Assertions.Expect(afterButton).ToBeFocusedAsync();
    }

    [Fact]
    public virtual async Task DisabledNonNativeButton_IsNotFocusable()
    {
        await NavigateAsync(CreateUrl("/tests/button")
            .WithDisabled(true)
            .WithNativeButton(false)
            .WithAs("span"));

        var beforeButton = GetByTestId("before-button");
        await beforeButton.FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await WaitForDelayAsync(100);

        // Should skip the disabled non-native button (tabindex=-1) and land on the after button
        var afterButton = GetByTestId("after-button");
        await Assertions.Expect(afterButton).ToBeFocusedAsync();
    }

    [Fact]
    public virtual async Task FocusableWhenDisabled_NativeButton_IsFocusable()
    {
        await NavigateAsync(CreateUrl("/tests/button")
            .WithDisabled(true)
            .WithButtonFocusableWhenDisabled(true)
            .WithNativeButton(true));

        var beforeButton = GetByTestId("before-button");
        await beforeButton.FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await WaitForDelayAsync(100);

        var button = GetButton();
        await Assertions.Expect(button).ToBeFocusedAsync();
    }

    [Fact]
    public virtual async Task FocusableWhenDisabled_NonNativeButton_IsFocusable()
    {
        await NavigateAsync(CreateUrl("/tests/button")
            .WithDisabled(true)
            .WithButtonFocusableWhenDisabled(true)
            .WithNativeButton(false)
            .WithAs("span"));

        var beforeButton = GetByTestId("before-button");
        await beforeButton.FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await WaitForDelayAsync(100);

        var button = GetButton();
        await Assertions.Expect(button).ToBeFocusedAsync();
    }

    #endregion

    #region Disabled event suppression

    [Fact]
    public virtual async Task FocusableWhenDisabled_BlocksClick()
    {
        await NavigateAsync(CreateUrl("/tests/button")
            .WithDisabled(true)
            .WithButtonFocusableWhenDisabled(true)
            .WithNativeButton(false)
            .WithAs("span"));

        await WaitForButtonJsAsync();

        // The JS handler calls preventDefault() on click when disabled.
        // Blazor's event delegation still fires @onclick, so we verify the
        // JS-level prevention by checking defaultPrevented on the native event.
        var defaultPrevented = await Page.EvaluateAsync<bool>(@"() => {
            return new Promise(resolve => {
                const el = document.querySelector('[data-testid=""button-under-test""]');
                el.addEventListener('click', e => resolve(e.defaultPrevented), { once: true });
                el.click();
            });
        }");

        Assert.True(defaultPrevented);
    }

    [Fact]
    public virtual async Task FocusableWhenDisabled_BlocksKeyboard()
    {
        await NavigateAsync(CreateUrl("/tests/button")
            .WithDisabled(true)
            .WithButtonFocusableWhenDisabled(true)
            .WithNativeButton(false)
            .WithAs("span"));

        await WaitForButtonJsAsync();

        var button = GetButton();
        await button.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await WaitForDelayAsync(100);
        await Page.Keyboard.PressAsync("Space");
        await WaitForDelayAsync(100);

        await Assertions.Expect(GetClickCount()).ToHaveTextAsync("0");
    }

    [Fact]
    public virtual async Task DisabledNativeButton_BlocksClick()
    {
        await NavigateAsync(CreateUrl("/tests/button")
            .WithDisabled(true)
            .WithNativeButton(true));

        var button = GetButton();
        await button.ClickAsync(new LocatorClickOptions { Force = true });
        await WaitForDelayAsync(100);

        await Assertions.Expect(GetClickCount()).ToHaveTextAsync("0");
    }

    [Fact]
    public virtual async Task DisabledNonNativeButton_BlocksClick()
    {
        await NavigateAsync(CreateUrl("/tests/button")
            .WithDisabled(true)
            .WithNativeButton(false)
            .WithAs("span"));

        await WaitForButtonJsAsync();

        // The JS handler calls preventDefault() on click when disabled.
        // Blazor's event delegation still fires @onclick, so we verify the
        // JS-level prevention by checking defaultPrevented on the native event.
        var defaultPrevented = await Page.EvaluateAsync<bool>(@"() => {
            return new Promise(resolve => {
                const el = document.querySelector('[data-testid=""button-under-test""]');
                el.addEventListener('click', e => resolve(e.defaultPrevented), { once: true });
                el.click();
            });
        }");

        Assert.True(defaultPrevented);
    }

    [Fact]
    public virtual async Task DisabledNativeButton_BlocksKeyboard()
    {
        await NavigateAsync(CreateUrl("/tests/button")
            .WithDisabled(true)
            .WithNativeButton(true));

        var beforeButton = GetByTestId("before-button");
        await beforeButton.FocusAsync();

        // Tab past the disabled native button
        await Page.Keyboard.PressAsync("Tab");
        await WaitForDelayAsync(100);

        // Focus should be on after-button, not on the disabled button
        var afterButton = GetByTestId("after-button");
        await Assertions.Expect(afterButton).ToBeFocusedAsync();

        // Even with force-focus via JS, keyboard activation should not fire
        await Page.EvaluateAsync(@"() => {
            document.querySelector('[data-testid=""button-under-test""]').focus();
        }");
        await Page.Keyboard.PressAsync("Enter");
        await WaitForDelayAsync(100);
        await Page.Keyboard.PressAsync("Space");
        await WaitForDelayAsync(100);

        await Assertions.Expect(GetClickCount()).ToHaveTextAsync("0");
    }

    [Fact]
    public virtual async Task DisabledNonNativeButton_BlocksKeyboard()
    {
        await NavigateAsync(CreateUrl("/tests/button")
            .WithDisabled(true)
            .WithNativeButton(false)
            .WithAs("span"));

        await WaitForButtonJsAsync();

        var button = GetButton();
        await button.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await WaitForDelayAsync(100);
        await Page.Keyboard.PressAsync("Space");
        await WaitForDelayAsync(100);

        await Assertions.Expect(GetClickCount()).ToHaveTextAsync("0");
    }

    [Fact]
    public virtual async Task FocusableWhenDisabled_NativeButton_BlocksClick()
    {
        await NavigateAsync(CreateUrl("/tests/button")
            .WithDisabled(true)
            .WithButtonFocusableWhenDisabled(true)
            .WithNativeButton(true));

        await WaitForButtonJsAsync();

        var defaultPrevented = await Page.EvaluateAsync<bool>(@"() => {
            return new Promise(resolve => {
                const el = document.querySelector('[data-testid=""button-under-test""]');
                el.addEventListener('click', e => resolve(e.defaultPrevented), { once: true });
                el.click();
            });
        }");

        Assert.True(defaultPrevented);
    }

    #endregion

    #region Dynamic state changes

    [Fact]
    public virtual async Task DynamicDisable_NonNativeButton_TogglesAttributes()
    {
        await NavigateAsync(CreateUrl("/tests/button")
            .WithNativeButton(false)
            .WithAs("span"));

        var button = GetButton();

        // Initially not disabled
        await Assertions.Expect(button).Not.ToHaveAttributeAsync("data-disabled", "");
        await Assertions.Expect(button).ToHaveAttributeAsync("tabindex", "0");

        // Toggle disabled on
        var toggleButton = GetByTestId("toggle-disabled");
        await toggleButton.ClickAsync();
        await WaitForDelayAsync(200);

        await Assertions.Expect(button).ToHaveAttributeAsync("data-disabled", "");
        await Assertions.Expect(button).ToHaveAttributeAsync("aria-disabled", "true");
        await Assertions.Expect(button).ToHaveAttributeAsync("role", "button");

        // Toggle disabled off
        await toggleButton.ClickAsync();
        await WaitForDelayAsync(200);

        await Assertions.Expect(button).Not.ToHaveAttributeAsync("data-disabled", "");
        await Assertions.Expect(button).Not.ToHaveAttributeAsync("aria-disabled", "true");
        await Assertions.Expect(button).ToHaveAttributeAsync("role", "button");
    }

    [Fact]
    public virtual async Task DynamicDisable_NativeButton_TogglesAttributes()
    {
        await NavigateAsync(CreateUrl("/tests/button")
            .WithNativeButton(true));

        var button = GetButton();

        // Initially not disabled
        await Assertions.Expect(button).Not.ToHaveAttributeAsync("disabled", "");
        await Assertions.Expect(button).ToHaveAttributeAsync("tabindex", "0");

        // Toggle disabled on
        var toggleButton = GetByTestId("toggle-disabled");
        await toggleButton.ClickAsync();
        await WaitForDelayAsync(200);

        await Assertions.Expect(button).ToHaveAttributeAsync("disabled", "");
        await Assertions.Expect(button).ToHaveAttributeAsync("data-disabled", "");

        // Toggle disabled off
        await toggleButton.ClickAsync();
        await WaitForDelayAsync(200);

        await Assertions.Expect(button).Not.ToHaveAttributeAsync("disabled", "");
        await Assertions.Expect(button).Not.ToHaveAttributeAsync("data-disabled", "");
        await Assertions.Expect(button).ToHaveAttributeAsync("tabindex", "0");
    }

    [Fact]
    public virtual async Task DynamicFocusableWhenDisabled_TogglesAttributes()
    {
        await NavigateAsync(CreateUrl("/tests/button")
            .WithDisabled(true)
            .WithNativeButton(true));

        var button = GetButton();

        // Initially disabled (native) - should have disabled attr, no tabindex
        await Assertions.Expect(button).ToHaveAttributeAsync("disabled", "");

        // Toggle focusableWhenDisabled on
        var toggleFocusable = GetByTestId("toggle-focusable-when-disabled");
        await toggleFocusable.ClickAsync();
        await WaitForDelayAsync(200);

        // Should switch from disabled to aria-disabled + tabindex
        await Assertions.Expect(button).Not.ToHaveAttributeAsync("disabled", "");
        await Assertions.Expect(button).ToHaveAttributeAsync("aria-disabled", "true");
        await Assertions.Expect(button).ToHaveAttributeAsync("tabindex", "0");

        // Toggle focusableWhenDisabled off
        await toggleFocusable.ClickAsync();
        await WaitForDelayAsync(200);

        // Should revert to native disabled
        await Assertions.Expect(button).ToHaveAttributeAsync("disabled", "");
    }

    [Fact]
    public virtual async Task NonNativeButton_HasCorrectRoleInBrowser()
    {
        await NavigateAsync(CreateUrl("/tests/button")
            .WithNativeButton(false)
            .WithAs("span"));

        var button = GetButton();
        await Assertions.Expect(button).ToHaveAttributeAsync("role", "button");

        // Verify the actual element is a span, not a button
        var tagName = await button.EvaluateAsync<string>("el => el.tagName.toLowerCase()");
        Assert.Equal("span", tagName);
    }

    #endregion
}
