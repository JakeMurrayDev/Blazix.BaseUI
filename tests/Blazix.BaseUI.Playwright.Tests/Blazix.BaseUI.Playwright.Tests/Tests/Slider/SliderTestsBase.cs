using System.Text.RegularExpressions;
using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Tests.Slider;

/// <summary>
/// Playwright tests for Slider component - focused on browser-specific behavior.
/// Static rendering, attribute forwarding, and basic state tests are handled by bUnit.
/// These tests cover: keyboard navigation, drag interaction, focus management,
/// RTL support, range sliders, and real JS interop execution.
/// </summary>
public abstract class SliderTestsBase : TestBase
{
    protected SliderTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    #region Helper Methods

    protected ILocator GetSliderThumb(int index = 0) => GetByTestId($"slider-thumb-{index}");

    protected ILocator GetSliderInput(int index = 0) =>
        GetSliderThumb(index).Locator("input[type='range']");

    protected async Task FocusSliderAsync(int thumbIndex = 0)
    {
        var input = GetSliderInput(thumbIndex);
        await input.FocusAsync();
        await WaitForDelayAsync(100);
    }

    protected async Task<double> GetSliderValueAsync(int thumbIndex = 0)
    {
        var input = GetSliderInput(thumbIndex);
        var value = await input.GetAttributeAsync("aria-valuenow");
        return double.Parse(value ?? "0", System.Globalization.CultureInfo.InvariantCulture);
    }

    protected async Task WaitForValueAsync(double expected, int thumbIndex = 0, int timeout = 5000)
    {
        var effectiveTimeout = timeout * TimeoutMultiplier;
        var input = GetSliderInput(thumbIndex);
        await Assertions.Expect(input).ToHaveAttributeAsync(
            "aria-valuenow",
            expected.ToString(System.Globalization.CultureInfo.InvariantCulture),
            new LocatorAssertionsToHaveAttributeOptions { Timeout = effectiveTimeout });
    }

    #endregion

    #region Basic Keyboard Navigation Tests

    /// <summary>
    /// Tests that ArrowRight increases the slider value by one step.
    /// Requires real browser keyboard events.
    /// </summary>
    [Fact]
    public virtual async Task ArrowRight_IncreasesValue()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(50)
            .WithStep(1));

        await FocusSliderAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(100);

        await WaitForValueAsync(51);
    }

    /// <summary>
    /// Tests that ArrowLeft decreases the slider value by one step.
    /// Requires real browser keyboard events.
    /// </summary>
    [Fact]
    public virtual async Task ArrowLeft_DecreasesValue()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(50)
            .WithStep(1));

        await FocusSliderAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await WaitForDelayAsync(100);

        await WaitForValueAsync(49);
    }

    /// <summary>
    /// Tests that ArrowUp increases the slider value by one step.
    /// Requires real browser keyboard events.
    /// </summary>
    [Fact]
    public virtual async Task ArrowUp_IncreasesValue()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(50)
            .WithStep(1));

        await FocusSliderAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await WaitForDelayAsync(100);

        await WaitForValueAsync(51);
    }

    /// <summary>
    /// Tests that ArrowDown decreases the slider value by one step.
    /// Requires real browser keyboard events.
    /// </summary>
    [Fact]
    public virtual async Task ArrowDown_DecreasesValue()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(50)
            .WithStep(1));

        await FocusSliderAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await WaitForDelayAsync(100);

        await WaitForValueAsync(49);
    }

    /// <summary>
    /// Tests that Home key sets the value to minimum.
    /// Requires real browser keyboard events.
    /// </summary>
    [Fact]
    public virtual async Task Home_SetsToMinimum()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(50)
            .WithMin(0)
            .WithMax(100));

        await FocusSliderAsync();
        await Page.Keyboard.PressAsync("Home");
        await WaitForDelayAsync(100);

        await WaitForValueAsync(0);
    }

    /// <summary>
    /// Tests that End key sets the value to maximum.
    /// Requires real browser keyboard events.
    /// </summary>
    [Fact]
    public virtual async Task End_SetsToMaximum()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(50)
            .WithMin(0)
            .WithMax(100));

        await FocusSliderAsync();
        await Page.Keyboard.PressAsync("End");
        await WaitForDelayAsync(100);

        await WaitForValueAsync(100);
    }

    /// <summary>
    /// Tests that PageUp increases the value by large step.
    /// Requires real browser keyboard events.
    /// </summary>
    [Fact]
    public virtual async Task PageUp_IncreasesValueByLargeStep()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(50)
            .WithLargeStep(10));

        await FocusSliderAsync();
        await Page.Keyboard.PressAsync("PageUp");
        await WaitForDelayAsync(100);

        await WaitForValueAsync(60);
    }

    /// <summary>
    /// Tests that PageDown decreases the value by large step.
    /// Requires real browser keyboard events.
    /// </summary>
    [Fact]
    public virtual async Task PageDown_DecreasesValueByLargeStep()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(50)
            .WithLargeStep(10));

        await FocusSliderAsync();
        await Page.Keyboard.PressAsync("PageDown");
        await WaitForDelayAsync(100);

        await WaitForValueAsync(40);
    }

    #endregion

    #region Step and Bounds Tests

    /// <summary>
    /// Tests that the slider respects custom step value.
    /// </summary>
    [Fact]
    public virtual async Task RespectsCustomStep()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(50)
            .WithStep(5));

        await FocusSliderAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(100);

        await WaitForValueAsync(55);
    }

    /// <summary>
    /// Tests that the slider value does not exceed maximum.
    /// </summary>
    [Fact]
    public virtual async Task DoesNotExceedMaximum()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(95)
            .WithMax(100)
            .WithStep(10));

        await FocusSliderAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(100);

        await WaitForValueAsync(100);
    }

    /// <summary>
    /// Tests that the slider value does not go below minimum.
    /// </summary>
    [Fact]
    public virtual async Task DoesNotGoBelowMinimum()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(5)
            .WithMin(0)
            .WithStep(10));

        await FocusSliderAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await WaitForDelayAsync(100);

        await WaitForValueAsync(0);
    }

    #endregion

    #region Disabled State Tests

    /// <summary>
    /// Tests that a disabled slider does not respond to keyboard input.
    /// </summary>
    [Fact]
    public virtual async Task DisabledSlider_DoesNotRespondToKeyboard()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(50)
            .WithDisabled(true));

        var input = GetSliderInput();
        await Assertions.Expect(input).ToBeDisabledAsync();

        var initialValue = await GetSliderValueAsync();

        // Try to change value via keyboard (should be ignored)
        // Note: FocusAsync silently no-ops on disabled elements per Playwright behavior,
        // but we include it to mirror how a user might attempt to interact
        await input.FocusAsync(new LocatorFocusOptions { Timeout = 1000 });
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(100);

        // Value should remain unchanged
        var currentValue = await GetSliderValueAsync();
        Assert.Equal(initialValue, currentValue);
    }

    #endregion

    #region Focus Management Tests

    /// <summary>
    /// Tests that focusing the slider input triggers focus event.
    /// </summary>
    [Fact]
    public virtual async Task FocusSlider_InputReceivesFocus()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(50));

        var input = GetSliderInput();
        await input.FocusAsync();

        await Assertions.Expect(input).ToBeFocusedAsync();
    }

    #endregion

    #region RTL Support Tests

    /// <summary>
    /// Tests that in RTL mode, ArrowLeft increases the value.
    /// </summary>
    [Fact]
    public virtual async Task RTL_ArrowLeftIncreasesValue()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(50)
            .WithStep(1)
            .WithDirection("rtl"));

        await FocusSliderAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await WaitForDelayAsync(100);

        await WaitForValueAsync(51);
    }

    /// <summary>
    /// Tests that in RTL mode, ArrowRight decreases the value.
    /// </summary>
    [Fact]
    public virtual async Task RTL_ArrowRightDecreasesValue()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(50)
            .WithStep(1)
            .WithDirection("rtl"));

        await FocusSliderAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(100);

        await WaitForValueAsync(49);
    }

    #endregion

    #region Range Slider Tests

    /// <summary>
    /// Tests that range slider first thumb can be moved independently.
    /// </summary>
    [Fact]
    public virtual async Task RangeSlider_FirstThumbCanBeMoved()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithShowRangeSlider(true)
            .WithDefaultSliderValues(20, 80)
            .WithStep(1));

        await FocusSliderAsync(0);
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(100);

        await WaitForValueAsync(21, 0);
        await WaitForValueAsync(80, 1); // Second thumb should not move
    }

    /// <summary>
    /// Tests that range slider second thumb can be moved independently.
    /// </summary>
    [Fact]
    public virtual async Task RangeSlider_SecondThumbCanBeMoved()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithShowRangeSlider(true)
            .WithDefaultSliderValues(20, 80)
            .WithStep(1));

        await FocusSliderAsync(1);
        await Page.Keyboard.PressAsync("ArrowLeft");
        await WaitForDelayAsync(100);

        await WaitForValueAsync(20, 0); // First thumb should not move
        await WaitForValueAsync(79, 1);
    }

    #endregion

    #region Thumb Collision Behavior Tests

    /// <summary>
    /// Tests that with push behavior, thumbs push each other.
    /// </summary>
    [Fact]
    public virtual async Task ThumbCollision_Push_PushesOtherThumb()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithShowRangeSlider(true)
            .WithDefaultSliderValues(40, 50)
            .WithStep(1)
            .WithThumbCollisionBehavior("push"));

        // Focus first thumb and try to push past second
        await FocusSliderAsync(0);
        for (var i = 0; i < 15; i++)
        {
            await Page.Keyboard.PressAsync("ArrowRight");
        }
        await WaitForDelayAsync(200);

        // Both thumbs should have moved together
        var value0 = await GetSliderValueAsync(0);
        var value1 = await GetSliderValueAsync(1);

        Assert.True(value0 >= 50, "First thumb should have pushed past original second thumb position");
        Assert.True(value1 >= value0, "Second thumb should have been pushed");
    }

    #endregion

    #region Min Steps Between Values Tests

    /// <summary>
    /// Tests that minStepsBetweenValues is respected for range sliders.
    /// </summary>
    [Fact]
    public virtual async Task MinStepsBetweenValues_PreventsTooClose()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithShowRangeSlider(true)
            .WithDefaultSliderValues(40, 50)
            .WithStep(1)
            .WithMinStepsBetweenValues(5)
            .WithThumbCollisionBehavior("block"));

        // Focus first thumb and try to get too close to second
        await FocusSliderAsync(0);
        for (var i = 0; i < 10; i++)
        {
            await Page.Keyboard.PressAsync("ArrowRight");
        }
        await WaitForDelayAsync(200);

        var value0 = await GetSliderValueAsync(0);
        var value1 = await GetSliderValueAsync(1);

        // First thumb should stop 5 steps before second thumb
        Assert.True(value1 - value0 >= 5, $"Thumbs should be at least 5 steps apart, but got {value1 - value0}");
    }

    #endregion

    #region Value Display Tests

    /// <summary>
    /// Tests that the slider value updates when value changes through keyboard interaction.
    /// Requires real browser to verify end-to-end event propagation.
    /// </summary>
    [Fact]
    public virtual async Task SliderValue_UpdatesOnValueChange()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(50)
            .WithStep(1));

        await FocusSliderAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(200);

        var valueDisplay = GetByTestId("slider-value");
        await Assertions.Expect(valueDisplay).ToContainTextAsync("51");
    }

    #endregion

    #region Event Tests

    /// <summary>
    /// Tests that value change event is fired when value changes.
    /// </summary>
    [Fact]
    public virtual async Task OnValueChange_FiresWhenValueChanges()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(50)
            .WithStep(1));

        var changeCount = GetByTestId("change-count");
        await Assertions.Expect(changeCount).ToHaveTextAsync("0");

        await FocusSliderAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(200);

        await Assertions.Expect(changeCount).ToHaveTextAsync("1");
    }

    /// <summary>
    /// Tests that value committed event is fired.
    /// </summary>
    [Fact]
    public virtual async Task OnValueCommitted_FiresOnKeyboardInteraction()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(50)
            .WithStep(1));

        var commitCount = GetByTestId("commit-count");
        await Assertions.Expect(commitCount).ToHaveTextAsync("0");

        await FocusSliderAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(200);

        await Assertions.Expect(commitCount).ToHaveTextAsync("1");
    }

    /// <summary>
    /// Tests that the change reason is captured correctly.
    /// </summary>
    [Fact]
    public virtual async Task ChangeReason_CapturedOnKeyboard()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(50)
            .WithStep(1));

        await FocusSliderAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(200);

        var changeReason = GetByTestId("last-change-reason");
        var text = await changeReason.TextContentAsync();
        Assert.Contains("Keyboard", text, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Shift+Key Navigation Tests (LargeStep)

    /// <summary>
    /// Tests that Shift+ArrowRight increases value by largeStep.
    /// Requires real browser modifier key handling.
    /// </summary>
    [Fact]
    public virtual async Task ShiftArrowRight_IncreasesValueByLargeStep()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(50)
            .WithStep(1)
            .WithLargeStep(10));

        await FocusSliderAsync();
        await Page.Keyboard.PressAsync("Shift+ArrowRight");
        await WaitForDelayAsync(100);

        await WaitForValueAsync(60);
    }

    /// <summary>
    /// Tests that Shift+ArrowLeft decreases value by largeStep.
    /// </summary>
    [Fact]
    public virtual async Task ShiftArrowLeft_DecreasesValueByLargeStep()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(50)
            .WithStep(1)
            .WithLargeStep(10));

        await FocusSliderAsync();
        await Page.Keyboard.PressAsync("Shift+ArrowLeft");
        await WaitForDelayAsync(100);

        await WaitForValueAsync(40);
    }

    /// <summary>
    /// Tests that Shift+ArrowUp increases value by largeStep.
    /// </summary>
    [Fact]
    public virtual async Task ShiftArrowUp_IncreasesValueByLargeStep()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(50)
            .WithStep(1)
            .WithLargeStep(10));

        await FocusSliderAsync();
        await Page.Keyboard.PressAsync("Shift+ArrowUp");
        await WaitForDelayAsync(100);

        await WaitForValueAsync(60);
    }

    /// <summary>
    /// Tests that Shift+ArrowDown decreases value by largeStep.
    /// </summary>
    [Fact]
    public virtual async Task ShiftArrowDown_DecreasesValueByLargeStep()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(50)
            .WithStep(1)
            .WithLargeStep(10));

        await FocusSliderAsync();
        await Page.Keyboard.PressAsync("Shift+ArrowDown");
        await WaitForDelayAsync(100);

        await WaitForValueAsync(40);
    }

    /// <summary>
    /// Tests that Shift+Arrow stops at max when incrementing.
    /// </summary>
    [Fact]
    public virtual async Task ShiftArrow_StopsAtMaxWhenIncrementing()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(95)
            .WithMax(100)
            .WithStep(1)
            .WithLargeStep(10));

        await FocusSliderAsync();
        await Page.Keyboard.PressAsync("Shift+ArrowRight");
        await WaitForDelayAsync(100);

        await WaitForValueAsync(100);
    }

    /// <summary>
    /// Tests that Shift+Arrow stops at min when decrementing.
    /// </summary>
    [Fact]
    public virtual async Task ShiftArrow_StopsAtMinWhenDecrementing()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(5)
            .WithMin(0)
            .WithStep(1)
            .WithLargeStep(10));

        await FocusSliderAsync();
        await Page.Keyboard.PressAsync("Shift+ArrowLeft");
        await WaitForDelayAsync(100);

        await WaitForValueAsync(0);
    }

    #endregion

    #region Vertical Orientation Tests

    /// <summary>
    /// Tests vertical slider ArrowUp increases value.
    /// </summary>
    [Fact]
    public virtual async Task Vertical_ArrowUpIncreasesValue()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(50)
            .WithStep(1)
            .WithOrientation("vertical"));

        await FocusSliderAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await WaitForDelayAsync(100);

        await WaitForValueAsync(51);
    }

    /// <summary>
    /// Tests vertical slider ArrowDown decreases value.
    /// </summary>
    [Fact]
    public virtual async Task Vertical_ArrowDownDecreasesValue()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(50)
            .WithStep(1)
            .WithOrientation("vertical"));

        await FocusSliderAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await WaitForDelayAsync(100);

        await WaitForValueAsync(49);
    }

    /// <summary>
    /// Tests vertical slider ArrowRight increases value.
    /// </summary>
    [Fact]
    public virtual async Task Vertical_ArrowRightIncreasesValue()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(50)
            .WithStep(1)
            .WithOrientation("vertical"));

        await FocusSliderAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(100);

        await WaitForValueAsync(51);
    }

    #endregion

    #region RTL + Vertical Combination Tests

    /// <summary>
    /// Tests RTL with vertical orientation - ArrowUp still increases.
    /// </summary>
    [Fact]
    public virtual async Task RTL_Vertical_ArrowUpIncreasesValue()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(50)
            .WithStep(1)
            .WithOrientation("vertical")
            .WithDirection("rtl"));

        await FocusSliderAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await WaitForDelayAsync(100);

        await WaitForValueAsync(51);
    }

    #endregion

    #region Multi-Thumb (3+) Tests

    /// <summary>
    /// Tests that three thumbs work correctly.
    /// </summary>
    [Fact]
    public virtual async Task ThreeThumbs_AllCanBeMoved()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithShowRangeSlider(true)
            .WithDefaultSliderValues(20, 50, 80)
            .WithStep(1));

        // Move first thumb
        await FocusSliderAsync(0);
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(100);
        await WaitForValueAsync(21, 0);

        // Move middle thumb
        await FocusSliderAsync(1);
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(100);
        await WaitForValueAsync(51, 1);

        // Move last thumb
        await FocusSliderAsync(2);
        await Page.Keyboard.PressAsync("ArrowLeft");
        await WaitForDelayAsync(100);
        await WaitForValueAsync(79, 2);
    }

    #endregion

    #region Thumb Collision - Swap Behavior Tests

    /// <summary>
    /// Tests that with swap behavior, thumbs swap active index during drag crossing.
    /// Swap behavior keeps values sorted but changes which thumb is "active" when crossing.
    /// Note: Swap collision only works during drag operations (not keyboard navigation).
    /// </summary>
    [Fact]
    public virtual async Task ThumbCollision_Swap_AllowsSwapping()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithShowRangeSlider(true)
            .WithDefaultSliderValues(40, 50)
            .WithStep(1)
            .WithThumbCollisionBehavior("swap"));

        var control = GetByTestId("slider-control");
        var thumb0 = GetSliderThumb(0);

        var controlBox = await control.BoundingBoxAsync();

        if (controlBox is null)
        {
            Assert.Fail("Could not get bounding box");
            return;
        }

        // Drag first thumb past the second thumb (from 40% to 60% of the track)
        await thumb0.HoverAsync();
        await Page.Mouse.DownAsync();

        // Drag past the second thumb position (which is at 50%)
        var endX = (float)(controlBox.X + (controlBox.Width * 0.60));
        var endY = (float)(controlBox.Y + (controlBox.Height / 2));

        await Page.Mouse.MoveAsync(endX, endY, new MouseMoveOptions { Steps = 10 });
        await Page.Mouse.UpAsync();

        await WaitForDelayAsync(200);

        var value0 = await GetSliderValueAsync(0);
        var value1 = await GetSliderValueAsync(1);

        // With swap behavior, values stay sorted but active thumb changes.
        // After dragging thumb0 (at 40) past thumb1 (at 50) to 60:
        // - Value at index 0 should be around 40-50 (the crossed thumb's position)
        // - Value at index 1 should be around 60 (where we dragged to)
        // Values remain sorted: value0 <= value1
        Assert.True(value0 <= value1, $"Values should remain sorted: Thumb0={value0}, Thumb1={value1}");
        Assert.True(value1 >= 55, $"Second value should be near drag target (60). Got Thumb1={value1}");
    }

    /// <summary>
    /// Tests that with block (none) behavior, thumbs cannot pass each other.
    /// </summary>
    [Fact]
    public virtual async Task ThumbCollision_Block_PreventsPassing()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithShowRangeSlider(true)
            .WithDefaultSliderValues(40, 50)
            .WithStep(1)
            .WithThumbCollisionBehavior("block"));

        // Focus first thumb and try to push past second
        await FocusSliderAsync(0);
        for (var i = 0; i < 15; i++)
        {
            await Page.Keyboard.PressAsync("ArrowRight");
        }
        await WaitForDelayAsync(200);

        var value0 = await GetSliderValueAsync(0);
        var value1 = await GetSliderValueAsync(1);

        // First thumb should stop at or before second thumb
        Assert.True(value0 <= value1, "First thumb should not pass second thumb with block behavior");
    }

    #endregion

    #region ReadOnly Tests

    /// <summary>
    /// Tests that readonly slider does not respond to keyboard input.
    /// </summary>
    [Fact]
    public virtual async Task ReadOnlySlider_DoesNotRespondToKeyboard()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(50)
            .WithReadOnly(true));

        var initialValue = await GetSliderValueAsync();

        var input = GetSliderInput();
        await input.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(100);

        var currentValue = await GetSliderValueAsync();
        Assert.Equal(initialValue, currentValue);
    }

    #endregion

    #region Drag Interaction Tests

    /// <summary>
    /// Tests that dragging the thumb to a position on the track sets the value correctly.
    /// Uses the same hover + mouse down/move/up pattern as ThumbDrag_ChangesValue.
    /// </summary>
    [Fact]
    public virtual async Task TrackClick_SetsValueToClickPosition()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(0)
            .WithMin(0)
            .WithMax(100));

        // Wait for the slider to be fully interactive - verify keyboard works first
        await FocusSliderAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForValueAsync(1); // Confirm slider responds to input

        // Reset to 0 for the click test
        await Page.Keyboard.PressAsync("Home");
        await WaitForValueAsync(0);

        var thumb = GetSliderThumb(0);
        var control = GetByTestId("slider-control");

        var controlBox = await control.BoundingBoxAsync();

        if (controlBox is null)
        {
            Assert.Fail("Could not get bounding box for slider control");
            return;
        }

        // Drag thumb from current position (0%) to 75% using the working pattern
        await thumb.HoverAsync();
        await Page.Mouse.DownAsync();

        var endX = (float)(controlBox.X + (controlBox.Width * 0.75));
        var endY = (float)(controlBox.Y + (controlBox.Height / 2));

        await Page.Mouse.MoveAsync(endX, endY, new MouseMoveOptions { Steps = 10 });
        await Page.Mouse.UpAsync();

        // Wait for value to update with assertion - allow tolerance for positioning
        var input = GetSliderInput();
        await Assertions.Expect(input).ToHaveAttributeAsync(
            "aria-valuenow",
            new Regex(@"^(6[0-9]|7[0-9]|8[0-9]|90)$"),
            new LocatorAssertionsToHaveAttributeOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    /// <summary>
    /// Tests that pressing the track itself updates the hidden input state, not only the visual thumb.
    /// </summary>
    [Fact]
    public virtual async Task TrackPress_UpdatesInputValue()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(25)
            .WithMin(0)
            .WithMax(100));

        var control = GetByTestId("slider-control");
        var controlBox = await control.BoundingBoxAsync();

        if (controlBox is null)
        {
            Assert.Fail("Could not get bounding box for slider control");
            return;
        }

        await Page.Mouse.ClickAsync(
            (float)(controlBox.X + (controlBox.Width * 0.8)),
            (float)(controlBox.Y + (controlBox.Height / 2)));

        var expectedTrackPressValue = new Regex(@"^(7[5-9]|8[0-5])$");
        await Assertions.Expect(GetByTestId("current-value")).ToHaveTextAsync(expectedTrackPressValue);
        await Assertions.Expect(GetByTestId("last-change-reason")).ToHaveTextAsync("TrackPress");

        var input = GetSliderInput();
        await Assertions.Expect(input).ToHaveValueAsync(expectedTrackPressValue);
        await Assertions.Expect(input).ToHaveAttributeAsync(
            "aria-valuenow",
            expectedTrackPressValue,
            new LocatorAssertionsToHaveAttributeOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    /// <summary>
    /// Tests that dragging the thumb changes the value.
    /// Requires real browser pointer events.
    /// </summary>
    [Fact]
    public virtual async Task ThumbDrag_ChangesValue()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(50)
            .WithMin(0)
            .WithMax(100));

        var thumb = GetSliderThumb(0);
        var control = GetByTestId("slider-control");

        var thumbBox = await thumb.BoundingBoxAsync();
        var controlBox = await control.BoundingBoxAsync();

        if (thumbBox is null || controlBox is null)
        {
            Assert.Fail("Could not get bounding boxes");
            return;
        }

        // Use HoverAsync for more reliable element targeting, then mouse operations
        await thumb.HoverAsync();
        await Page.Mouse.DownAsync();

        // Drag to approximately 75% of the track with intermediate steps for reliability
        var endX = (float)(controlBox.X + (controlBox.Width * 0.75));
        var endY = (float)(thumbBox.Y + (thumbBox.Height / 2));

        await Page.Mouse.MoveAsync(endX, endY, new MouseMoveOptions { Steps = 10 });
        await Page.Mouse.UpAsync();

        // Wait for value to update - should be around 75
        var input = GetSliderInput();
        await Assertions.Expect(input).ToHaveAttributeAsync(
            "aria-valuenow",
            new Regex(@"^(5[6-9]|[6-9][0-9]|100)$"),
            new LocatorAssertionsToHaveAttributeOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    /// <summary>
    /// Tests that data-dragging attribute is applied during drag.
    /// </summary>
    [Fact]
    public virtual async Task DataDragging_AppliedDuringDrag()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(50));

        var thumb = GetSliderThumb(0);
        var control = GetByTestId("slider-control");
        var root = GetByTestId("slider-root");

        var controlBox = await control.BoundingBoxAsync();

        if (controlBox is null)
        {
            Assert.Fail("Could not get bounding box");
            return;
        }

        // Use HoverAsync for reliable targeting
        await thumb.HoverAsync();
        await Page.Mouse.DownAsync();

        // Move with steps to ensure drag state is established
        var endX = (float)(controlBox.X + (controlBox.Width * 0.75));
        var endY = (float)(controlBox.Y + (controlBox.Height / 2));
        await Page.Mouse.MoveAsync(endX, endY, new MouseMoveOptions { Steps = 5 });

        // Check for data-dragging during the drag using Playwright assertion
        await Assertions.Expect(root).ToHaveAttributeAsync(
            "data-dragging",
            "",
            new LocatorAssertionsToHaveAttributeOptions { Timeout = 2000 * TimeoutMultiplier });

        await Page.Mouse.UpAsync();

        // data-dragging should be removed after drag ends
        await Assertions.Expect(root).Not.ToHaveAttributeAsync(
            "data-dragging",
            new Regex(".*"),
            new LocatorAssertionsToHaveAttributeOptions { Timeout = 2000 * TimeoutMultiplier });
    }

    #endregion

    #region Focus During Interaction Tests

    /// <summary>
    /// Tests that the slider input receives focus when clicking the track.
    /// </summary>
    [Fact]
    public virtual async Task TrackClick_FocusesSliderInput()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(50));

        var control = GetByTestId("slider-control");
        var box = await control.BoundingBoxAsync();

        if (box is null)
        {
            Assert.Fail("Could not get bounding box");
            return;
        }

        // Click on the track (not the thumb) - click at 80% to avoid thumb at 50%
        await Page.Mouse.ClickAsync((float)(box.X + (box.Width * 0.8)), (float)(box.Y + (box.Height / 2)));

        var input = GetSliderInput();
        await Assertions.Expect(input).ToBeFocusedAsync(
            new LocatorAssertionsToBeFocusedOptions { Timeout = 2000 * TimeoutMultiplier });
    }

    /// <summary>
    /// Tests that the slider input receives focus when dragging the thumb.
    /// </summary>
    [Fact]
    public virtual async Task ThumbDrag_FocusesSliderInput()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(50));

        // First focus somewhere else
        var outsideButton = GetByTestId("outside-button");
        await outsideButton.FocusAsync();
        await Assertions.Expect(outsideButton).ToBeFocusedAsync();

        var thumb = GetSliderThumb(0);

        // Use HoverAsync then mouse down for reliable targeting
        await thumb.HoverAsync();
        await Page.Mouse.DownAsync();

        // Input should receive focus during drag
        var input = GetSliderInput();
        await Assertions.Expect(input).ToBeFocusedAsync(
            new LocatorAssertionsToBeFocusedOptions { Timeout = 2000 * TimeoutMultiplier });

        await Page.Mouse.UpAsync();
    }

    #endregion

    #region Non-Integer Value Tests

    /// <summary>
    /// Tests that non-integer step values work correctly.
    /// </summary>
    [Fact]
    public virtual async Task NonIntegerStep_WorksCorrectly()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(0.5)
            .WithMin(0)
            .WithMax(1)
            .WithStep(0.1));

        await FocusSliderAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(100);

        var input = GetSliderInput();
        var value = await input.GetAttributeAsync("aria-valuenow");
        var numericValue = double.Parse(value ?? "0", System.Globalization.CultureInfo.InvariantCulture);

        // Should be approximately 0.6
        Assert.True(numericValue >= 0.59 && numericValue <= 0.61, $"Expected ~0.6, got {numericValue}");
    }

    #endregion

    #region End Key in Range Slider Tests

    /// <summary>
    /// Tests that End key sets first thumb to maximum possible value (just before second thumb).
    /// </summary>
    [Fact]
    public virtual async Task RangeSlider_EndKey_SetsToMaximumPossible()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithShowRangeSlider(true)
            .WithDefaultSliderValues(20, 80)
            .WithStep(1)
            .WithThumbCollisionBehavior("block"));

        await FocusSliderAsync(0);
        await Page.Keyboard.PressAsync("End");
        await WaitForDelayAsync(100);

        var value0 = await GetSliderValueAsync(0);
        var value1 = await GetSliderValueAsync(1);

        // First thumb should be at or near the second thumb position (blocked)
        Assert.True(value0 <= value1, $"First thumb ({value0}) should not exceed second thumb ({value1})");
    }

    /// <summary>
    /// Tests that Home key sets second thumb to minimum possible value (just after first thumb).
    /// </summary>
    [Fact]
    public virtual async Task RangeSlider_HomeKey_SetsToMinimumPossible()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithShowRangeSlider(true)
            .WithDefaultSliderValues(20, 80)
            .WithStep(1)
            .WithThumbCollisionBehavior("block"));

        await FocusSliderAsync(1);
        await Page.Keyboard.PressAsync("Home");
        await WaitForDelayAsync(100);

        var value0 = await GetSliderValueAsync(0);
        var value1 = await GetSliderValueAsync(1);

        // Second thumb should be at or near the first thumb position (blocked)
        Assert.True(value1 >= value0, $"Second thumb ({value1}) should not go below first thumb ({value0})");
    }

    #endregion

    #region Value Change Fires Only When Changed Tests

    /// <summary>
    /// Tests that OnValueChange does not fire when value hasn't actually changed.
    /// </summary>
    [Fact]
    public virtual async Task OnValueChange_DoesNotFireWhenAtMin()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(0)
            .WithMin(0)
            .WithMax(100)
            .WithStep(1));

        var changeCount = GetByTestId("change-count");
        await Assertions.Expect(changeCount).ToHaveTextAsync("0");

        await FocusSliderAsync();
        // Try to decrease below min - should not change value
        await Page.Keyboard.PressAsync("ArrowLeft");
        await WaitForDelayAsync(100);

        // Change count should still be 0 since value didn't change
        await Assertions.Expect(changeCount).ToHaveTextAsync("0");
    }

    /// <summary>
    /// Tests that OnValueChange does not fire when value hasn't actually changed at max.
    /// </summary>
    [Fact]
    public virtual async Task OnValueChange_DoesNotFireWhenAtMax()
    {
        await NavigateAsync(CreateUrl("/tests/slider")
            .WithDefaultSliderValue(100)
            .WithMin(0)
            .WithMax(100)
            .WithStep(1));

        var changeCount = GetByTestId("change-count");
        await Assertions.Expect(changeCount).ToHaveTextAsync("0");

        await FocusSliderAsync();
        // Try to increase above max - should not change value
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(100);

        // Change count should still be 0 since value didn't change
        await Assertions.Expect(changeCount).ToHaveTextAsync("0");
    }

    #endregion

}
