using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Tests.NavigationMenu;

/// <summary>
/// Playwright tests for NavigationMenu component - focused on browser-specific behavior.
/// Static rendering, attribute forwarding, and basic state tests are handled by bUnit.
/// These tests cover: trigger click interaction, value switching, keyboard dismiss,
/// outside click dismiss, active link detection, and real JS interop execution.
/// </summary>
public abstract class NavigationMenuTestsBase : TestBase
{
    protected NavigationMenuTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    #region Helper Methods

    protected async Task ClickTriggerAndWaitAsync(string triggerTestId)
    {
        var trigger = GetByTestId(triggerTestId);
        await trigger.ClickAsync();
        await WaitForDelayAsync((int)(200 * TimeoutMultiplier));
    }

    protected async Task WaitForActiveValueAsync(string expectedValue)
    {
        var activeValue = GetByTestId("active-value");
        await Assertions.Expect(activeValue).ToHaveTextAsync(expectedValue, new LocatorAssertionsToHaveTextOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    #endregion

    #region Trigger Click Tests

    /// <summary>
    /// Tests that clicking a trigger opens the corresponding content panel.
    /// Requires real browser to test JS interop for value state management.
    /// </summary>
    [Fact]
    public virtual async Task OpensContentOnTriggerClick()
    {
        await NavigateAsync(CreateUrl("/tests/navigation-menu"));

        var trigger = GetByTestId("nav-trigger-1");
        await trigger.ClickAsync();

        await WaitForActiveValueAsync("item1");

        // Trigger should have aria-expanded="true"
        var triggerButton = Page.Locator("button[id='nav-trigger-item1']");
        await Assertions.Expect(triggerButton).ToHaveAttributeAsync("aria-expanded", "true");
    }

    #endregion

    #region Value Switching Tests

    /// <summary>
    /// Tests switching between different navigation items by clicking triggers.
    /// </summary>
    [Fact]
    public virtual async Task SwitchesBetweenTriggers()
    {
        await NavigateAsync(CreateUrl("/tests/navigation-menu"));

        // Click first trigger
        var trigger1 = GetByTestId("nav-trigger-1");
        await trigger1.ClickAsync();
        await WaitForActiveValueAsync("item1");

        // Click second trigger
        var trigger2 = GetByTestId("nav-trigger-2");
        await trigger2.ClickAsync();
        await WaitForActiveValueAsync("item2");

        // First trigger should now be collapsed
        var trigger1Button = Page.Locator("button[id='nav-trigger-item1']");
        await Assertions.Expect(trigger1Button).ToHaveAttributeAsync("aria-expanded", "false");

        // Second trigger should be expanded
        var trigger2Button = Page.Locator("button[id='nav-trigger-item2']");
        await Assertions.Expect(trigger2Button).ToHaveAttributeAsync("aria-expanded", "true");
    }

    #endregion

    #region Close Tests

    /// <summary>
    /// Tests that clicking the same trigger again closes the content.
    /// </summary>
    [Fact]
    public virtual async Task ClosesOnSameTriggerClick()
    {
        await NavigateAsync(CreateUrl("/tests/navigation-menu"));

        var trigger = GetByTestId("nav-trigger-1");

        // Open
        await trigger.ClickAsync();
        await WaitForActiveValueAsync("item1");

        // Close by clicking the same trigger
        await trigger.ClickAsync();
        await WaitForActiveValueAsync("none");
    }

    /// <summary>
    /// Tests that pressing Escape closes the navigation menu.
    /// </summary>
    [Fact]
    public virtual async Task ClosesOnEscape()
    {
        await NavigateAsync(CreateUrl("/tests/navigation-menu"));

        var trigger = GetByTestId("nav-trigger-1");
        await trigger.ClickAsync();
        await WaitForActiveValueAsync("item1");

        await Page.Keyboard.PressAsync("Escape");
        await WaitForActiveValueAsync("none");
    }

    /// <summary>
    /// Tests that clicking outside the navigation menu closes it.
    /// </summary>
    [Fact]
    public virtual async Task ClosesOnOutsideClick()
    {
        await NavigateAsync(CreateUrl("/tests/navigation-menu"));

        var trigger = GetByTestId("nav-trigger-1");
        await trigger.ClickAsync();
        await WaitForActiveValueAsync("item1");

        var outsideButton = GetByTestId("outside-button");
        await outsideButton.ClickAsync();
        await WaitForActiveValueAsync("none");
    }

    /// <summary>
    /// Tests that moving focus outside the navigation menu closes it through focusout handling.
    /// </summary>
    [Fact]
    public virtual async Task ClosesWhenFocusLeavesMenu()
    {
        await NavigateAsync(CreateUrl("/tests/navigation-menu"));

        var trigger = GetByTestId("nav-trigger-1");
        await trigger.ClickAsync();
        await WaitForActiveValueAsync("item1");

        var outsideButton = GetByTestId("outside-button");
        await outsideButton.FocusAsync();
        await WaitForActiveValueAsync("none");
    }

    #endregion

    #region Disabled Tests

    /// <summary>
    /// Tests that disabled triggers stay focusable, expose aria-disabled, and do not open.
    /// </summary>
    [Fact]
    public virtual async Task DisabledTriggerRemainsFocusableAndDoesNotOpen()
    {
        await NavigateAsync(CreateUrl("/tests/navigation-menu")
            .WithNavDisableItem1(true));

        var trigger = GetByTestId("nav-trigger-1");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-disabled", "true");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("tabindex", "0");

        await trigger.FocusAsync();
        var activeTestId = await Page.EvaluateAsync<string?>("() => document.activeElement?.getAttribute('data-testid')");
        Assert.Equal("nav-trigger-1", activeTestId);

        await trigger.ClickAsync(new LocatorClickOptions { Force = true });
        await WaitForActiveValueAsync("none");
    }

    #endregion

    #region Default Value Tests

    /// <summary>
    /// Tests that defaultValue query param sets the initial active item.
    /// </summary>
    [Fact]
    public virtual async Task DefaultValueRespected()
    {
        await NavigateAsync(CreateUrl("/tests/navigation-menu")
            .WithNavDefaultValue("item2"));

        await WaitForActiveValueAsync("item2");

        // Item 2 trigger should be expanded
        var trigger2Button = Page.Locator("button[id='nav-trigger-item2']");
        await Assertions.Expect(trigger2Button).ToHaveAttributeAsync("aria-expanded", "true");
    }

    #endregion

    #region Value Change Callback Tests

    /// <summary>
    /// Tests that the OnValueChange callback fires and the state display updates.
    /// </summary>
    [Fact]
    public virtual async Task ReturnsValueOnChange()
    {
        await NavigateAsync(CreateUrl("/tests/navigation-menu"));

        var trigger = GetByTestId("nav-trigger-1");
        await trigger.ClickAsync();

        var changeCount = GetByTestId("change-count");
        await Assertions.Expect(changeCount).ToHaveTextAsync("1", new LocatorAssertionsToHaveTextOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });

        var lastChanged = GetByTestId("last-changed-value");
        await Assertions.Expect(lastChanged).ToHaveTextAsync("item1", new LocatorAssertionsToHaveTextOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    #endregion

    #region Active Link Tests

    /// <summary>
    /// Tests that a NavigationMenuLink with Active="true" has aria-current="page".
    /// </summary>
    [Fact]
    public virtual async Task ActiveLinkHasAriaCurrent()
    {
        await NavigateAsync(CreateUrl("/tests/navigation-menu")
            .WithNavDefaultValue("item3"));

        var activeLink = GetByTestId("nav-link-3a-active");
        await Assertions.Expect(activeLink).ToHaveAttributeAsync("aria-current", "page");
    }

    #endregion

    #region Orientation Tests

    /// <summary>
    /// Tests that vertical orientation sets the correct data attribute.
    /// </summary>
    [Fact]
    public virtual async Task VerticalOrientationApplied()
    {
        await NavigateAsync(CreateUrl("/tests/navigation-menu")
            .WithNavOrientation("vertical"));

        var nav = Page.Locator("nav[data-orientation='vertical']");
        await Assertions.Expect(nav).ToBeAttachedAsync();
    }

    #endregion

    #region Nested Navigation Tests

    /// <summary>
    /// Tests that nested NavigationMenuRoot renders as div instead of nav.
    /// </summary>
    [Fact]
    public virtual async Task NestedNavRendersDivInsteadOfNav()
    {
        await NavigateAsync(CreateUrl("/tests/navigation-menu")
            .WithNavDefaultValue("nested")
            .WithShowNestedNav(true));

        // The outer root should be nav
        var navElements = Page.Locator("nav");
        await Assertions.Expect(navElements.First).ToBeAttachedAsync();

        // The nested root should be a div (not nav)
        var nestedRoot = GetByTestId("nested-nav-root");
        var tagName = await nestedRoot.EvaluateAsync<string>("el => el.tagName");
        Assert.Equal("DIV", tagName);
    }

    #endregion
}
