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
        await WaitForSelectTriggerJsAsync();

        var trigger = GetByTestId("select-trigger");
        var triggerBox = await trigger.BoundingBoxAsync()
            ?? throw new InvalidOperationException("Trigger bounding box was null.");

        // Press, drag well outside the trigger bounds, release.
        await Page.Mouse.MoveAsync(triggerBox.X + triggerBox.Width / 2, triggerBox.Y + triggerBox.Height / 2);
        await Page.Mouse.DownAsync();
        await Page.WaitForTimeoutAsync(20 * TimeoutMultiplier);
        var positioner = GetByTestId("select-positioner");
        await Assertions.Expect(positioner).ToBeVisibleAsync();
        var outsideInput = GetByTestId("outside-input");
        var outsideBox = await outsideInput.BoundingBoxAsync()
            ?? throw new InvalidOperationException("Outside input bounding box was null.");
        await Page.Mouse.MoveAsync(outsideBox.X + outsideBox.Width / 2, outsideBox.Y + outsideBox.Height / 2);
        await Page.Mouse.UpAsync();

        // The popup was opened on mousedown (click-to-open) then cancelled by mouseup outside bounds.
        await Assertions.Expect(positioner).ToBeHiddenAsync();
    }

    [Fact]
    public virtual async Task FocusMovingIntoPopup_DoesNotTriggerBlurOnTrigger()
    {
        await NavigateAsync(CreateUrl("/tests/select-trigger-cancel"));
        await WaitForSelectTriggerJsAsync();

        var trigger = GetByTestId("select-trigger");
        await trigger.ClickAsync();

        // Popup is open; focus moves into an item via keyboard nav.
        await Page.Keyboard.PressAsync("ArrowDown");

        // data-touched must remain absent because the blur containment check
        // skipped the real-blur callback.
        await Assertions.Expect(trigger).Not.ToHaveAttributeAsync("data-touched", "");
    }

    private async Task WaitForSelectTriggerJsAsync()
    {
        try
        {
            await Page.WaitForFunctionAsync(
                """
                () => {
                    const state = window[Symbol.for('Blazix.BaseUI.Select.State')];
                    const root = state?.roots?.get('fruit-select');
                    return !!root?.triggerCleanup && !!root?.triggerDotNetRef;
                }
                """,
                new PageWaitForFunctionOptions { Timeout = 5000 * TimeoutMultiplier });
        }
        catch (TimeoutException ex)
        {
            var debug = await Page.EvaluateAsync<string>(
                """
                () => {
                    const state = window[Symbol.for('Blazix.BaseUI.Select.State')];
                    return JSON.stringify({
                        hasState: !!state,
                        rootCount: state?.roots?.size ?? null,
                        roots: state?.roots ? Array.from(state.roots.entries()).map(([id, root]) => ({
                            id,
                            hasTriggerCleanup: !!root.triggerCleanup,
                            hasTriggerDotNetRef: !!root.triggerDotNetRef,
                            hasRootDotNetRef: !!root.dotNetRef,
                            hasTriggerElement: !!root.triggerElement,
                            triggerConnected: root.triggerElement?.isConnected ?? null,
                            triggerTestId: root.triggerElement?.getAttribute?.('data-testid') ?? null
                        })) : null
                    });
                }
                """);

            throw new TimeoutException($"Select trigger JS did not initialize. State: {debug}", ex);
        }
    }
}
