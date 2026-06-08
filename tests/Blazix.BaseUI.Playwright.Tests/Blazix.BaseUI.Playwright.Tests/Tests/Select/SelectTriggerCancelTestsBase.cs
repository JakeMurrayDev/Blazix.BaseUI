using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Tests.Select;

public abstract class SelectTriggerCancelTestsBase : TestBase
{
    protected SelectTriggerCancelTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    [Fact]
    public virtual async Task MouseUpOutsideTriggerBounds_ClosesPopup()
    {
        await NavigateAsync(CreateUrl("/tests/select-trigger-cancel"));

        var trigger = GetByTestId("select-trigger");
        await Page.WaitForFunctionAsync(
            "() => Boolean(document.querySelector('[data-testid=\"select-trigger\"]')?.__blazixBaseUISelectTriggerInitialized)",
            new PageWaitForFunctionOptions { Timeout = 5000 * TimeoutMultiplier });

        var triggerBox = await trigger.BoundingBoxAsync()
            ?? throw new InvalidOperationException("Select trigger bounding box was null.");
        var startX = (double)triggerBox.X + (double)triggerBox.Width / 2;
        var startY = (double)triggerBox.Y + (double)triggerBox.Height / 2;

        await trigger.DispatchEventAsync("mousedown", new
        {
            button = 0,
            buttons = 1,
            clientX = startX,
            clientY = startY
        });

        var positioner = GetByTestId("select-positioner");
        await Assertions.Expect(positioner).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
        await WaitForDelayAsync(50);

        await Page.Locator("body").DispatchEventAsync("mouseup", new
        {
            button = 0,
            clientX = (double)triggerBox.X + (double)triggerBox.Width + 200,
            clientY = (double)triggerBox.Y + (double)triggerBox.Height + 200
        });

        // The popup was opened on mousedown (click-to-open) then cancelled by mouseup outside bounds.
        await Assertions.Expect(positioner).ToBeHiddenAsync(new LocatorAssertionsToBeHiddenOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    [Fact]
    public virtual async Task FocusMovingIntoPopup_DoesNotTriggerBlurOnTrigger()
    {
        await NavigateAsync(CreateUrl("/tests/select-trigger-cancel"));

        var trigger = GetByTestId("select-trigger");
        await trigger.ClickAsync();

        // Popup is open; focus moves into an item via keyboard nav.
        await Page.Keyboard.PressAsync("ArrowDown");

        // The trigger must not become touched because the blur containment check
        // skipped the real-blur callback.
        await Assertions.Expect(trigger).Not.ToHaveAttributeAsync("data-touched", "");
    }
}
