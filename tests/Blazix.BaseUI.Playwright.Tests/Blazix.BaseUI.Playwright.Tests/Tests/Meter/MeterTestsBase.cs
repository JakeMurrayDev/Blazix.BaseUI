using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Tests.Meter;

/// <summary>
/// Playwright tests for Meter component - focused on browser-specific behavior.
/// Static rendering, attribute forwarding, and basic state tests are handled by bUnit.
/// These tests cover: indicator computed styles, dynamic value updates, ARIA attributes
/// in a real browser, label associations, and absence of data attributes.
/// </summary>
public abstract class MeterTestsBase : TestBase
{
    protected MeterTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    #region Helper Methods

    protected ILocator GetMeterRoot() => GetByTestId("meter-root");
    protected ILocator GetMeterIndicator() => GetByTestId("meter-indicator");
    protected ILocator GetMeterTrack() => GetByTestId("meter-track");
    protected ILocator GetMeterValue() => GetByTestId("meter-value");
    protected ILocator GetMeterLabel() => GetByTestId("meter-label");
    protected ILocator GetMeterHiddenPresentationSpan() =>
        Page.Locator("[data-testid='meter-root'] > span[role='presentation']:not([data-testid])");

    private async Task WaitForStyleContainsAsync(ILocator element, string substring, int timeout = 5000)
    {
        var effectiveTimeout = timeout * TimeoutMultiplier;
        var startTime = DateTime.UtcNow;
        while ((DateTime.UtcNow - startTime).TotalMilliseconds < effectiveTimeout)
        {
            var style = await element.GetAttributeAsync("style");
            if (style is not null && style.Contains(substring, StringComparison.Ordinal))
            {
                return;
            }
            await Task.Delay(50);
        }

        var finalStyle = await element.GetAttributeAsync("style");
        throw new TimeoutException(
            $"Style did not contain '{substring}' within {effectiveTimeout}ms. Final style: '{finalStyle}'");
    }

    #endregion

    #region Indicator Style Tests

    [Fact]
    public virtual async Task Indicator_HasCorrectWidthStyle()
    {
        await NavigateAsync(CreateUrl("/tests/meter")
            .WithMeterValue(33));

        var indicator = GetMeterIndicator();
        var style = await indicator.GetAttributeAsync("style");
        Assert.Contains("width:33%", style);
        Assert.Contains("inset-inline-start:0", style);
    }

    [Fact]
    public virtual async Task Indicator_HasZeroWidthWhenValueIsZero()
    {
        await NavigateAsync(CreateUrl("/tests/meter")
            .WithMeterValue(0));

        var indicator = GetMeterIndicator();
        var style = await indicator.GetAttributeAsync("style");
        Assert.Contains("width:0%", style);
    }

    #endregion

    #region ARIA Attribute Tests

    [Fact]
    public virtual async Task Root_HasRoleMeter()
    {
        await NavigateAsync(CreateUrl("/tests/meter")
            .WithMeterValue(50));

        var root = GetMeterRoot();
        await Assertions.Expect(root).ToHaveAttributeAsync("role", "meter");
    }

    [Fact]
    public virtual async Task Root_HasCorrectAriaAttributes()
    {
        await NavigateAsync(CreateUrl("/tests/meter")
            .WithMeterValue(30));

        var root = GetMeterRoot();
        await Assertions.Expect(root).ToHaveAttributeAsync("aria-valuenow", "30");
        await Assertions.Expect(root).ToHaveAttributeAsync("aria-valuemin", "0");
        await Assertions.Expect(root).ToHaveAttributeAsync("aria-valuemax", "100");

        var ariaValueText = await root.GetAttributeAsync("aria-valuetext");
        Assert.NotNull(ariaValueText);
        Assert.NotEmpty(ariaValueText);
    }

    [Fact]
    public virtual async Task Root_RendersHiddenPresentationSpanForScreenReaders()
    {
        await NavigateAsync(CreateUrl("/tests/meter")
            .WithMeterValue(50));

        var hidden = GetMeterHiddenPresentationSpan();
        await Assertions.Expect(hidden).ToHaveTextAsync("x");

        var style = await hidden.GetAttributeAsync("style");
        Assert.Contains("clip-path:inset(50%)", style);
        Assert.Contains("position:fixed", style);
    }

    #endregion

    #region Dynamic Value Update Tests

    [Fact]
    public virtual async Task DynamicValueUpdate_ChangesAriaAndStyle()
    {
        await NavigateAsync(CreateUrl("/tests/meter")
            .WithMeterValue(25));

        var root = GetMeterRoot();
        var indicator = GetMeterIndicator();

        // Initial state
        await Assertions.Expect(root).ToHaveAttributeAsync("aria-valuenow", "25");
        var style = await indicator.GetAttributeAsync("style");
        Assert.Contains("width:25%", style);

        // Click button to change to 50%
        var button50 = GetByTestId("set-value-50");
        await button50.ClickAsync();
        await WaitForStyleContainsAsync(indicator, "width:50%");
        await Assertions.Expect(root).ToHaveAttributeAsync("aria-valuenow", "50");

        // Click button to change to 100%
        var button100 = GetByTestId("set-value-100");
        await button100.ClickAsync();
        await WaitForStyleContainsAsync(indicator, "width:100%");
        await Assertions.Expect(root).ToHaveAttributeAsync("aria-valuenow", "100");
    }

    #endregion

    #region Label Association Tests

    [Fact]
    public virtual async Task Label_LinkedToRootViaAriaLabelledBy()
    {
        await NavigateAsync(CreateUrl("/tests/meter")
            .WithMeterValue(50)
            .WithShowMeterLabel(true)
            .WithLabelText("Battery Level"));

        var root = GetMeterRoot();
        var label = GetMeterLabel();

        var labelId = await label.GetAttributeAsync("id");
        Assert.NotNull(labelId);
        Assert.NotEmpty(labelId);

        await Assertions.Expect(root).ToHaveAttributeAsync("aria-labelledby", labelId!);
        await Assertions.Expect(label).ToHaveTextAsync("Battery Level");
        await Assertions.Expect(label).ToHaveAttributeAsync("role", "presentation");
    }

    #endregion

    #region Value Display Tests

    [Fact]
    public virtual async Task Value_DisplaysFormattedPercentage()
    {
        await NavigateAsync(CreateUrl("/tests/meter")
            .WithMeterValue(50)
            .WithShowMeterValue(true));

        var value = GetMeterValue();
        var text = await value.TextContentAsync();
        Assert.NotNull(text);
        Assert.NotEmpty(text);
    }

    [Fact]
    public virtual async Task Value_DisplaysCustomFormat()
    {
        await NavigateAsync(CreateUrl("/tests/meter")
            .WithMeterValue(50)
            .WithShowMeterValue(true)
            .WithMeterFormat("F1"));

        var value = GetMeterValue();
        var text = await value.TextContentAsync();
        Assert.NotNull(text);
        Assert.NotEmpty(text);
    }

    #endregion

    #region No Data Attributes Tests

    [Fact]
    public virtual async Task Root_HasNoDataAttributes()
    {
        await NavigateAsync(CreateUrl("/tests/meter")
            .WithMeterValue(50));

        var root = GetMeterRoot();

        var dataProgressing = await root.GetAttributeAsync("data-progressing");
        Assert.Null(dataProgressing);

        var dataComplete = await root.GetAttributeAsync("data-complete");
        Assert.Null(dataComplete);

        var dataIndeterminate = await root.GetAttributeAsync("data-indeterminate");
        Assert.Null(dataIndeterminate);
    }

    [Fact]
    public virtual async Task Indicator_HasNoDataAttributes()
    {
        await NavigateAsync(CreateUrl("/tests/meter")
            .WithMeterValue(50));

        var indicator = GetMeterIndicator();

        var dataProgressing = await indicator.GetAttributeAsync("data-progressing");
        Assert.Null(dataProgressing);

        var dataComplete = await indicator.GetAttributeAsync("data-complete");
        Assert.Null(dataComplete);

        var dataIndeterminate = await indicator.GetAttributeAsync("data-indeterminate");
        Assert.Null(dataIndeterminate);
    }

    #endregion
}
