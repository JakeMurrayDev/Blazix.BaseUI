using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Tests.NumberField;

/// <summary>
/// Playwright tests for NumberField component - focused on browser-specific behavior.
/// Static rendering, attribute forwarding, and basic state tests are handled by bUnit.
/// These tests cover: keyboard navigation, wheel scrubbing, button interaction,
/// press-and-hold, clipboard paste, form submission, focus/blur, tab order,
/// disabled/readonly states, and dynamic value updates.
/// </summary>
public abstract class NumberFieldTestsBase : TestBase
{
    protected NumberFieldTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    #region Helper Methods

    protected ILocator GetInput() => Page.Locator("input[type='text']");
    protected ILocator GetHiddenInput() => Page.Locator("input[type='number']");
    protected ILocator GetIncrementButton() => Page.GetByRole(AriaRole.Button, new() { Name = "Increase" });
    protected ILocator GetDecrementButton() => Page.GetByRole(AriaRole.Button, new() { Name = "Decrease" });
    protected ILocator GetGroup() => Page.GetByRole(AriaRole.Group);
    protected ILocator GetScrubArea() => GetByTestId("scrub-area");
    protected ILocator GetValueDisplay() => GetByTestId("current-value");
    protected ILocator GetChangeCount() => GetByTestId("change-count");
    protected ILocator GetLastReason() => GetByTestId("last-reason");
    protected ILocator GetCommittedValue() => GetByTestId("committed-value");

    protected async Task WaitForNumberFieldJsAsync()
    {
        await Page.WaitForFunctionAsync(@"() => {
            const input = document.querySelector('input[type=""text""]');
            return input !== null;
        }", new PageWaitForFunctionOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    #endregion

    #region Keyboard Interaction

    /// <summary>
    /// Tests that ArrowUp increments the number field value by one step.
    /// Requires real browser keyboard events.
    /// </summary>
    [Fact]
    public virtual async Task IncrementWithArrowUp()
    {
        await NavigateAsync(CreateUrl("/tests/numberfield")
            .WithNumberFieldDefaultValue(5));

        var input = GetInput();
        await input.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await WaitForDelayAsync(100);

        await Assertions.Expect(GetValueDisplay()).ToHaveTextAsync("6");
    }

    /// <summary>
    /// Tests that ArrowDown decrements the number field value by one step.
    /// Requires real browser keyboard events.
    /// </summary>
    [Fact]
    public virtual async Task DecrementWithArrowDown()
    {
        await NavigateAsync(CreateUrl("/tests/numberfield")
            .WithNumberFieldDefaultValue(5));

        var input = GetInput();
        await input.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await WaitForDelayAsync(100);

        await Assertions.Expect(GetValueDisplay()).ToHaveTextAsync("4");
    }

    /// <summary>
    /// Tests that Home key sets the value to the minimum when min is set.
    /// Requires real browser keyboard events.
    /// </summary>
    [Fact]
    public virtual async Task HomeKeyGoesToMin()
    {
        await NavigateAsync(CreateUrl("/tests/numberfield")
            .WithNumberFieldDefaultValue(50)
            .WithMin(10));

        var input = GetInput();
        await input.FocusAsync();
        await Page.Keyboard.PressAsync("Home");
        await WaitForDelayAsync(100);

        await Assertions.Expect(GetValueDisplay()).ToHaveTextAsync("10");
    }

    /// <summary>
    /// Tests that End key sets the value to the maximum when max is set.
    /// Requires real browser keyboard events.
    /// </summary>
    [Fact]
    public virtual async Task EndKeyGoesToMax()
    {
        await NavigateAsync(CreateUrl("/tests/numberfield")
            .WithNumberFieldDefaultValue(50)
            .WithMax(100));

        var input = GetInput();
        await input.FocusAsync();
        await Page.Keyboard.PressAsync("End");
        await WaitForDelayAsync(100);

        await Assertions.Expect(GetValueDisplay()).ToHaveTextAsync("100");
    }

    /// <summary>
    /// Tests that Shift+ArrowUp increments by the large step (default 10).
    /// Requires real browser modifier key handling.
    /// </summary>
    [Fact]
    public virtual async Task LargeStepWithShiftArrowUp()
    {
        await NavigateAsync(CreateUrl("/tests/numberfield")
            .WithNumberFieldDefaultValue(5));

        var input = GetInput();
        await input.FocusAsync();
        await Page.Keyboard.PressAsync("Shift+ArrowUp");
        await WaitForDelayAsync(100);

        // Default largeStep is 10
        await Assertions.Expect(GetValueDisplay()).ToHaveTextAsync("15");
    }

    /// <summary>
    /// Tests that Shift+ArrowDown decrements by the large step (default 10).
    /// Requires real browser modifier key handling.
    /// </summary>
    [Fact]
    public virtual async Task LargeStepWithShiftArrowDown()
    {
        await NavigateAsync(CreateUrl("/tests/numberfield")
            .WithNumberFieldDefaultValue(15));

        var input = GetInput();
        await input.FocusAsync();
        await Page.Keyboard.PressAsync("Shift+ArrowDown");
        await WaitForDelayAsync(100);

        await Assertions.Expect(GetValueDisplay()).ToHaveTextAsync("5");
    }

    /// <summary>
    /// Tests that Alt+ArrowUp increments by the small step (default 0.1).
    /// Requires real browser modifier key handling.
    /// </summary>
    [Fact]
    public virtual async Task SmallStepWithAltArrowUp()
    {
        await NavigateAsync(CreateUrl("/tests/numberfield")
            .WithNumberFieldDefaultValue(5));

        var input = GetInput();
        await input.FocusAsync();
        await Page.Keyboard.PressAsync("Alt+ArrowUp");
        await WaitForDelayAsync(100);

        // Default smallStep is 0.1
        await Assertions.Expect(GetValueDisplay()).ToHaveTextAsync("5.1");
    }

    /// <summary>
    /// Tests that Alt+ArrowDown decrements by the small step (default 0.1).
    /// Requires real browser modifier key handling.
    /// </summary>
    [Fact]
    public virtual async Task SmallStepWithAltArrowDown()
    {
        await NavigateAsync(CreateUrl("/tests/numberfield")
            .WithNumberFieldDefaultValue(5));

        var input = GetInput();
        await input.FocusAsync();
        await Page.Keyboard.PressAsync("Alt+ArrowDown");
        await WaitForDelayAsync(100);

        await Assertions.Expect(GetValueDisplay()).ToHaveTextAsync("4.9");
    }

    #endregion

    #region Wheel Scrubbing

    /// <summary>
    /// Tests that scrolling the mouse wheel up on a focused input increments the value
    /// when allowWheelScrub is enabled.
    /// </summary>
    [Fact]
    public virtual async Task WheelScrub_IncrementsOnWheelUp()
    {
        await NavigateAsync(CreateUrl("/tests/numberfield")
            .WithNumberFieldDefaultValue(5)
            .WithNumberFieldAllowWheelScrub(true));

        var input = GetInput();
        await input.FocusAsync();
        await input.DispatchEventAsync("wheel", new { deltaY = -100 });
        await WaitForDelayAsync(100);

        await Assertions.Expect(GetValueDisplay()).ToHaveTextAsync("6");
    }

    /// <summary>
    /// Tests that scrolling the mouse wheel down on a focused input decrements the value
    /// when allowWheelScrub is enabled.
    /// </summary>
    [Fact]
    public virtual async Task WheelScrub_DecrementsOnWheelDown()
    {
        await NavigateAsync(CreateUrl("/tests/numberfield")
            .WithNumberFieldDefaultValue(5)
            .WithNumberFieldAllowWheelScrub(true));

        var input = GetInput();
        await input.FocusAsync();
        await input.DispatchEventAsync("wheel", new { deltaY = 100 });
        await WaitForDelayAsync(100);

        await Assertions.Expect(GetValueDisplay()).ToHaveTextAsync("4");
    }

    /// <summary>
    /// Tests that mouse wheel events do not change the value when allowWheelScrub is disabled.
    /// </summary>
    [Fact]
    public virtual async Task WheelScrub_DoesNotWorkWhenDisabled()
    {
        await NavigateAsync(CreateUrl("/tests/numberfield")
            .WithNumberFieldDefaultValue(5)
            .WithNumberFieldAllowWheelScrub(false));

        var input = GetInput();
        await input.FocusAsync();
        await input.DispatchEventAsync("wheel", new { deltaY = -100 });
        await WaitForDelayAsync(100);

        await Assertions.Expect(GetValueDisplay()).ToHaveTextAsync("5");
    }

    #endregion

    #region Clipboard Paste

    /// <summary>
    /// Tests that pasting a valid number into the input updates the value.
    /// Uses document.execCommand for reliable cross-browser paste simulation.
    /// </summary>
    [Fact]
    public virtual async Task Paste_AllowsValidNumber()
    {
        await NavigateAsync(CreateUrl("/tests/numberfield")
            .WithNumberFieldDefaultValue(0));

        var input = GetInput();
        await input.FocusAsync();
        await input.SelectTextAsync();

        // Use clipboard API to paste
        await Page.EvaluateAsync(@"async () => {
            const input = document.querySelector('input[type=""text""]');
            input.focus();
            input.select();
            document.execCommand('insertText', false, '42');
        }");
        await WaitForDelayAsync(100);

        await Assertions.Expect(GetValueDisplay()).ToHaveTextAsync("42");
    }

    [Fact]
    public virtual async Task Paste_EventUsesClipboardTextAndReason()
    {
        await NavigateAsync(CreateUrl("/tests/numberfield")
            .WithNumberFieldDefaultValue(0));

        var input = GetInput();
        await input.FocusAsync();

        await Page.EvaluateAsync(@"() => {
            const input = document.querySelector('input[type=""text""]');
            const event = new Event('paste', { bubbles: true, cancelable: true });
            Object.defineProperty(event, 'clipboardData', {
                value: { getData: (type) => type === 'text/plain' ? '42' : '' }
            });
            input.dispatchEvent(event);
        }");
        await WaitForDelayAsync(100);

        await Assertions.Expect(GetValueDisplay()).ToHaveTextAsync("42");
        await Assertions.Expect(GetLastReason()).ToHaveTextAsync("InputPaste");
    }

    #endregion

    #region Character Filtering

    [Fact]
    public virtual async Task Keyboard_BlocksPercentWhenNotFormattedAsPercent()
    {
        await NavigateAsync(CreateUrl("/tests/numberfield")
            .WithNumberFieldDefaultValue(0));

        var input = GetInput();
        await input.FocusAsync();
        await input.FillAsync("");
        await Page.Keyboard.TypeAsync("%");
        await WaitForDelayAsync(100);

        await Assertions.Expect(input).ToHaveValueAsync("");
        await Assertions.Expect(GetValueDisplay()).ToHaveTextAsync("null");
    }

    [Fact]
    public virtual async Task Keyboard_AllowsPercentWhenFormattedAsPercent()
    {
        await NavigateAsync(CreateUrl("/tests/numberfield")
            .WithNumberFieldDefaultValue(0)
            .WithNumberFieldFormatStyle("percent"));

        var input = GetInput();
        await input.FocusAsync();
        await input.FillAsync("");
        await Page.Keyboard.TypeAsync("12%");
        await WaitForDelayAsync(100);

        await Assertions.Expect(input).ToHaveValueAsync("12%");
        await Assertions.Expect(GetValueDisplay()).ToHaveTextAsync("0.12");
    }

    #endregion

    #region Focus and Blur

    /// <summary>
    /// Tests that the input element can receive focus.
    /// </summary>
    [Fact]
    public virtual async Task Focus_InputIsFocusable()
    {
        await NavigateAsync(CreateUrl("/tests/numberfield")
            .WithNumberFieldDefaultValue(5));

        var input = GetInput();
        await input.FocusAsync();

        await Assertions.Expect(input).ToBeFocusedAsync();
    }

    /// <summary>
    /// Tests that the value is committed when the input loses focus after typing.
    /// </summary>
    [Fact]
    public virtual async Task Blur_CommitsValueOnBlur()
    {
        await NavigateAsync(CreateUrl("/tests/numberfield")
            .WithNumberFieldDefaultValue(5));

        var input = GetInput();
        await input.FocusAsync();
        await input.FillAsync("42");
        await input.BlurAsync();
        await WaitForDelayAsync(100);

        await Assertions.Expect(GetCommittedValue()).ToHaveTextAsync("42");
    }

    /// <summary>
    /// Tests that the committed value is null when the input is cleared and blurred.
    /// </summary>
    [Fact]
    public virtual async Task Blur_CommitsNullWhenCleared()
    {
        await NavigateAsync(CreateUrl("/tests/numberfield")
            .WithNumberFieldDefaultValue(5));

        var input = GetInput();
        await input.FocusAsync();
        await input.FillAsync("");
        await input.BlurAsync();
        await WaitForDelayAsync(100);

        await Assertions.Expect(GetCommittedValue()).ToHaveTextAsync("null");
    }

    #endregion

    #region Button Interaction

    /// <summary>
    /// Tests that clicking the increment button increases the value by one step.
    /// </summary>
    [Fact]
    public virtual async Task IncrementButton_IncrementsValue()
    {
        await NavigateAsync(CreateUrl("/tests/numberfield")
            .WithNumberFieldDefaultValue(5));

        var btn = GetIncrementButton();
        await btn.ClickAsync();
        await WaitForDelayAsync(100);

        await Assertions.Expect(GetValueDisplay()).ToHaveTextAsync("6");
    }

    /// <summary>
    /// Tests that clicking the decrement button decreases the value by one step.
    /// </summary>
    [Fact]
    public virtual async Task DecrementButton_DecrementsValue()
    {
        await NavigateAsync(CreateUrl("/tests/numberfield")
            .WithNumberFieldDefaultValue(5));

        var btn = GetDecrementButton();
        await btn.ClickAsync();
        await WaitForDelayAsync(100);

        await Assertions.Expect(GetValueDisplay()).ToHaveTextAsync("4");
    }

    /// <summary>
    /// Tests that clicking the increment button when the value is at max does not change the value.
    /// </summary>
    [Fact]
    public virtual async Task IncrementButton_DisabledWhenAtMax()
    {
        await NavigateAsync(CreateUrl("/tests/numberfield")
            .WithNumberFieldDefaultValue(10)
            .WithMax(10));

        var btn = GetIncrementButton();
        await Assertions.Expect(btn).ToBeDisabledAsync();
        await Assertions.Expect(GetValueDisplay()).ToHaveTextAsync("10");
    }

    /// <summary>
    /// Tests that clicking the decrement button when the value is at min does not change the value.
    /// </summary>
    [Fact]
    public virtual async Task DecrementButton_DisabledWhenAtMin()
    {
        await NavigateAsync(CreateUrl("/tests/numberfield")
            .WithNumberFieldDefaultValue(0)
            .WithMin(0));

        var btn = GetDecrementButton();
        await Assertions.Expect(btn).ToBeDisabledAsync();
        await Assertions.Expect(GetValueDisplay()).ToHaveTextAsync("0");
    }

    #endregion

    #region Press and Hold

    /// <summary>
    /// Tests that pressing and holding the increment button auto-repeats increments.
    /// Requires real browser pointer events and JS timer interop.
    /// </summary>
    [Fact]
    public virtual async Task PressAndHold_IncrementsContinuously()
    {
        await NavigateAsync(CreateUrl("/tests/numberfield")
            .WithNumberFieldDefaultValue(0));

        var btn = GetIncrementButton();
        // mousedown and hold for ~600ms (400ms delay + 2 ticks of 60ms)
        await btn.DispatchEventAsync("pointerdown", new { button = 0, pointerType = "mouse" });
        await WaitForDelayAsync(600);
        await btn.DispatchEventAsync("pointerup", new { button = 0, pointerType = "mouse" });
        await WaitForDelayAsync(100);

        // Value should be > 1 (at least a few increments from auto-change)
        var valueText = await GetValueDisplay().TextContentAsync();
        var value = double.Parse(valueText!, System.Globalization.CultureInfo.InvariantCulture);
        Assert.True(value > 1, $"Expected value > 1 after press-and-hold, got {value}");
    }

    #endregion

    #region Form Submission

    /// <summary>
    /// Tests that the number field value is included in form submission data.
    /// Requires real browser form handling.
    /// </summary>
    [Fact]
    public virtual async Task Form_IncludesValueInSubmission()
    {
        await NavigateAsync(CreateUrl("/tests/numberfield")
            .WithNumberFieldDefaultValue(42)
            .WithNumberFieldName("quantity")
            .WithNumberFieldShowForm(true));

        var submitBtn = GetByTestId("submit-button");
        await submitBtn.ClickAsync();
        await WaitForDelayAsync(200);

        // Check the form data display
        var formData = GetByTestId("form-data");
        await Assertions.Expect(formData).ToContainTextAsync("quantity=42");
    }

    [Fact]
    public virtual async Task Form_AllowOutOfRangePreservesNativeOverflow()
    {
        await NavigateAsync(CreateUrl("/tests/numberfield")
            .WithNumberFieldDefaultValue(0)
            .WithNumberFieldName("quantity")
            .WithMax(5)
            .WithNumberFieldAllowOutOfRange(true)
            .WithNumberFieldShowForm(true));

        var input = GetInput();
        await input.FillAsync("6");
        await WaitForDelayAsync(100);

        var overflow = await GetHiddenInput().EvaluateAsync<bool>("input => input.validity.rangeOverflow");
        Assert.True(overflow);
        await Assertions.Expect(GetValueDisplay()).ToHaveTextAsync("6");
    }

    [Fact]
    public virtual async Task Form_StepAnyDoesNotCreateStepMismatch()
    {
        await NavigateAsync(CreateUrl("/tests/numberfield")
            .WithNumberFieldDefaultValue(0)
            .WithNumberFieldName("quantity")
            .WithMin(0)
            .WithNumberFieldStepAny(true)
            .WithNumberFieldShowForm(true));

        var input = GetInput();
        await input.FillAsync("0.11");
        await WaitForDelayAsync(100);

        var stepMismatch = await GetHiddenInput().EvaluateAsync<bool>("input => input.validity.stepMismatch");
        Assert.False(stepMismatch);
    }

    #endregion

    #region Scrub Area

    [Fact]
    public virtual async Task ScrubArea_UsesPointerMovementMagnitude()
    {
        await NavigateAsync(CreateUrl("/tests/numberfield")
            .WithNumberFieldDefaultValue(0)
            .WithNumberFieldShowScrubArea(true));

        var scrubArea = GetScrubArea();
        await scrubArea.DispatchEventAsync("pointerdown", new { button = 0, pointerType = "mouse", clientX = 20, clientY = 20 });
        await WaitForDelayAsync(100);
        await Page.EvaluateAsync(@"() => {
            const move = new PointerEvent('pointermove', { bubbles: true, cancelable: true, pointerType: 'mouse' });
            Object.defineProperty(move, 'movementX', { value: 10 });
            Object.defineProperty(move, 'movementY', { value: 0 });
            window.dispatchEvent(move);
            const up = new PointerEvent('pointerup', { bubbles: true, cancelable: true, pointerType: 'mouse' });
            window.dispatchEvent(up);
        }");
        await WaitForDelayAsync(200);

        await Assertions.Expect(GetValueDisplay()).ToHaveTextAsync("10");
        await Assertions.Expect(GetCommittedValue()).ToHaveTextAsync("10");
    }

    #endregion

    #region Tab Order

    /// <summary>
    /// Tests that the number field input receives focus when tabbing from the element before it.
    /// </summary>
    [Fact]
    public virtual async Task TabOrder_InputReceivesFocus()
    {
        await NavigateAsync(CreateUrl("/tests/numberfield")
            .WithNumberFieldDefaultValue(5));

        var beforeBtn = GetByTestId("before-button");
        await beforeBtn.FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await WaitForDelayAsync(100);

        var input = GetInput();
        await Assertions.Expect(input).ToBeFocusedAsync();
    }

    #endregion

    #region Disabled State

    /// <summary>
    /// Tests that the input element has the disabled attribute when the field is disabled.
    /// </summary>
    [Fact]
    public virtual async Task Disabled_InputIsNotFocusable()
    {
        await NavigateAsync(CreateUrl("/tests/numberfield")
            .WithNumberFieldDefaultValue(5)
            .WithDisabled(true));

        var input = GetInput();
        await Assertions.Expect(input).ToBeDisabledAsync();
    }

    /// <summary>
    /// Tests that the value does not change when the field is disabled.
    /// </summary>
    [Fact]
    public virtual async Task Disabled_ButtonsAreNotClickable()
    {
        await NavigateAsync(CreateUrl("/tests/numberfield")
            .WithNumberFieldDefaultValue(5)
            .WithDisabled(true));

        // Value should not change
        await Assertions.Expect(GetValueDisplay()).ToHaveTextAsync("5");
    }

    #endregion

    #region ReadOnly State

    /// <summary>
    /// Tests that the input element has the readonly attribute when the field is read-only.
    /// </summary>
    [Fact]
    public virtual async Task ReadOnly_InputIsReadOnly()
    {
        await NavigateAsync(CreateUrl("/tests/numberfield")
            .WithNumberFieldDefaultValue(5)
            .WithReadOnly(true));

        var input = GetInput();
        var readonlyAttr = await input.GetAttributeAsync("readonly");
        Assert.NotNull(readonlyAttr);
    }

    #endregion

    #region Value Display Updates

    /// <summary>
    /// Tests that clicking a control button dynamically updates the value display.
    /// </summary>
    [Fact]
    public virtual async Task DynamicUpdate_ChangesValueViaControlButton()
    {
        await NavigateAsync(CreateUrl("/tests/numberfield")
            .WithNumberFieldDefaultValue(5));

        var setTo10 = GetByTestId("set-value-10");
        await setTo10.ClickAsync();
        await WaitForDelayAsync(100);

        await Assertions.Expect(GetValueDisplay()).ToHaveTextAsync("10");
    }

    #endregion
}
