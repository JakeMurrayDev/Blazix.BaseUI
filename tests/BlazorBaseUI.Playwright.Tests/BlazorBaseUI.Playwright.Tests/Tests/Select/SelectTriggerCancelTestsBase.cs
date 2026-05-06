using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace BlazorBaseUI.Playwright.Tests.Tests.Select;

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
        var triggerBox = await trigger.BoundingBoxAsync()
            ?? throw new InvalidOperationException("Trigger bounding box was null.");

        // Press, drag well outside the trigger bounds, release.
        await Page.Mouse.MoveAsync(triggerBox.X + triggerBox.Width / 2, triggerBox.Y + triggerBox.Height / 2);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(triggerBox.X + triggerBox.Width + 200, triggerBox.Y + triggerBox.Height + 200);
        await Page.Mouse.UpAsync();

        // The popup was opened on mousedown (click-to-open) then cancelled by mouseup outside bounds.
        var positioner = GetByTestId("select-positioner");
        await Assertions.Expect(positioner).ToBeHiddenAsync();
    }

    [Fact]
    public virtual async Task FocusMovingIntoPopup_DoesNotTriggerBlurOnTrigger()
    {
        await NavigateAsync(CreateUrl("/tests/select-trigger-cancel"));

        var trigger = GetByTestId("select-trigger");
        await trigger.ClickAsync();

        // Popup is open; focus moves into an item via keyboard nav.
        await Page.Keyboard.PressAsync("ArrowDown");

        // data-touched must still be "False" because the blur containment check
        // skipped the real-blur callback.
        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-touched", "False");
    }
}
