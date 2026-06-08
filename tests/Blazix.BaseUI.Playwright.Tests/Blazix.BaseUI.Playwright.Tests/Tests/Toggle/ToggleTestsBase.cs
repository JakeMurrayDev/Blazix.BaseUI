using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Tests.Toggle;

public abstract class ToggleTestsBase : TestBase
{
    protected ToggleTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    #region Helper Methods

    protected ILocator GetToggle() => GetByTestId("toggle-under-test");
    protected ILocator GetPressedState() => GetByTestId("pressed-state");
    protected ILocator GetChangeCount() => GetByTestId("change-count");
    protected ILocator GetLastPressedValue() => GetByTestId("last-pressed-value");

    protected async Task WaitForToggleJsAsync()
    {
        await Page.WaitForFunctionAsync(@"() => {
            const el = document.querySelector('[data-testid=""toggle-under-test""]');
            if (!el) return false;
            const stateKey = Symbol.for('Blazix.BaseUI.Toggle.State');
            return el[stateKey] !== undefined;
        }", new PageWaitForFunctionOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    #endregion

    #region Non-native keyboard activation

    [Fact]
    public virtual async Task NonNativeToggle_ActivatesWithEnterKey()
    {
        await NavigateAsync(CreateUrl("/tests/toggle")
            .WithNativeButton(false)
            .WithAs("span"));

        await WaitForToggleJsAsync();

        var toggle = GetToggle();
        await toggle.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await WaitForDelayAsync(100);

        await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [Fact]
    public virtual async Task NonNativeToggle_ActivatesWithSpaceKey()
    {
        await NavigateAsync(CreateUrl("/tests/toggle")
            .WithNativeButton(false)
            .WithAs("span"));

        await WaitForToggleJsAsync();

        var toggle = GetToggle();
        await toggle.FocusAsync();
        await Page.Keyboard.PressAsync("Space");
        await WaitForDelayAsync(100);

        await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-pressed", "true");
    }

    #endregion

    #region Disabled focus behavior

    [Fact]
    public virtual async Task DisabledNativeToggle_IsNotFocusable()
    {
        await NavigateAsync(CreateUrl("/tests/toggle")
            .WithDisabled(true)
            .WithNativeButton(true));

        var beforeButton = GetByTestId("before-button");
        await beforeButton.FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await WaitForDelayAsync(100);

        var afterButton = GetByTestId("after-button");
        await Assertions.Expect(afterButton).ToBeFocusedAsync();
    }

    [Fact]
    public virtual async Task DisabledNonNativeToggle_IsNotFocusable()
    {
        await NavigateAsync(CreateUrl("/tests/toggle")
            .WithDisabled(true)
            .WithNativeButton(false)
            .WithAs("span"));

        var beforeButton = GetByTestId("before-button");
        await beforeButton.FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await WaitForDelayAsync(100);

        var afterButton = GetByTestId("after-button");
        await Assertions.Expect(afterButton).ToBeFocusedAsync();
    }

    #endregion

    #region Dynamic state changes

    [Fact]
    public virtual async Task DynamicDisable_TogglesAttributes()
    {
        await NavigateAsync(CreateUrl("/tests/toggle")
            .WithNativeButton(false)
            .WithAs("span"));

        var toggle = GetToggle();

        // Initially not disabled
        await Assertions.Expect(toggle).Not.ToHaveAttributeAsync("data-disabled", "");

        // Toggle disabled on
        var toggleButton = GetByTestId("toggle-disabled");
        await toggleButton.ClickAsync();
        await WaitForDelayAsync(200);

        await Assertions.Expect(toggle).ToHaveAttributeAsync("data-disabled", "");
        await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-disabled", "true");

        // Toggle disabled off
        await toggleButton.ClickAsync();
        await WaitForDelayAsync(200);

        await Assertions.Expect(toggle).Not.ToHaveAttributeAsync("data-disabled", "");
        await Assertions.Expect(toggle).Not.ToHaveAttributeAsync("aria-disabled", "true");
    }

    #endregion

    #region Uncontrolled toggle

    [Fact]
    public virtual async Task UncontrolledToggle_TogglesVisualState()
    {
        await NavigateAsync(CreateUrl("/tests/toggle"));

        var toggle = GetToggle();

        await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-pressed", "false");

        await toggle.ClickAsync();
        await WaitForDelayAsync(100);

        await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-pressed", "true");
        await Assertions.Expect(toggle).ToHaveAttributeAsync("data-pressed", "");

        // Click again to unpress
        await toggle.ClickAsync();
        await WaitForDelayAsync(100);

        await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-pressed", "false");
    }

    #endregion

    #region Controlled toggle

    [Fact]
    public virtual async Task ControlledToggle_ReflectsExternalState()
    {
        await NavigateAsync(CreateUrl("/tests/toggle"));

        var toggle = GetToggle();

        // External control: set pressed true
        var setPressedTrue = GetByTestId("set-pressed-true");
        await setPressedTrue.ClickAsync();
        await WaitForDelayAsync(200);

        await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-pressed", "true");

        // External control: set pressed false
        var setPressedFalse = GetByTestId("set-pressed-false");
        await setPressedFalse.ClickAsync();
        await WaitForDelayAsync(200);

        await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-pressed", "false");
    }

    #endregion

    #region OnPressedChange

    [Fact]
    public virtual async Task OnPressedChange_DisplaysCorrectValue()
    {
        await NavigateAsync(CreateUrl("/tests/toggle"));

        var toggle = GetToggle();
        await toggle.ClickAsync();
        await WaitForDelayAsync(100);

        await Assertions.Expect(GetChangeCount()).ToHaveTextAsync("1");
        await Assertions.Expect(GetLastPressedValue()).ToHaveTextAsync("true");
    }

    #endregion
}
