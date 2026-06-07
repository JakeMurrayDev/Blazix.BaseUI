using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace BlazorBaseUI.Playwright.Tests.Tests.Progress;

/// <summary>
/// Playwright tests for Progress component - focused on browser-specific behavior.
/// Static rendering, attribute forwarding, and basic state tests are handled by bUnit.
    /// These tests cover: indicator computed styles, dynamic value updates, ARIA attributes,
    /// presentation helpers, data attribute transitions, and label associations in a real browser.
/// </summary>
public abstract class ProgressTestsBase : TestBase
{
    protected ProgressTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    #region Helper Methods

    protected ILocator GetProgressRoot() => GetByTestId("progress-root");
    protected ILocator GetProgressIndicator() => GetByTestId("progress-indicator");
    protected ILocator GetProgressTrack() => GetByTestId("progress-track");
    protected ILocator GetProgressValue() => GetByTestId("progress-value");
    protected ILocator GetProgressLabel() => GetByTestId("progress-label");

    #endregion

    #region Indicator Style Tests

    [Fact]
    public virtual async Task Indicator_HasCorrectWidthStyle()
    {
        await NavigateAsync(CreateUrl("/tests/progress")
            .WithProgressValue(33));

        var indicator = GetProgressIndicator();
        var style = await indicator.GetAttributeAsync("style");
        Assert.Contains("width:33%", style);
        Assert.Contains("inset-inline-start:0", style);
    }

    [Fact]
    public virtual async Task Indicator_HasZeroWidthWhenValueIsZero()
    {
        await NavigateAsync(CreateUrl("/tests/progress")
            .WithProgressValue(0));

        var indicator = GetProgressIndicator();
        var style = await indicator.GetAttributeAsync("style");
        Assert.Contains("width:0%", style);
    }

    [Fact]
    public virtual async Task Indicator_HasNoWidthStyleWhenIndeterminate()
    {
        await NavigateAsync(CreateUrl("/tests/progress")
            .WithProgressValue(null));

        var indicator = GetProgressIndicator();
        var style = await indicator.GetAttributeAsync("style");

        // Indeterminate should not have width in the inline style
        if (style is not null)
        {
            Assert.DoesNotContain("width:", style);
        }
    }

    #endregion

    #region ARIA Attribute Tests

    [Fact]
    public virtual async Task Root_HasRoleProgressbar()
    {
        await NavigateAsync(CreateUrl("/tests/progress")
            .WithProgressValue(50));

        var root = GetProgressRoot();
        await Assertions.Expect(root).ToHaveAttributeAsync("role", "progressbar");
    }

    [Fact]
    public virtual async Task Root_HasCorrectAriaAttributes()
    {
        await NavigateAsync(CreateUrl("/tests/progress")
            .WithProgressValue(30));

        var root = GetProgressRoot();
        await Assertions.Expect(root).ToHaveAttributeAsync("aria-valuenow", "30");
        await Assertions.Expect(root).ToHaveAttributeAsync("aria-valuemin", "0");
        await Assertions.Expect(root).ToHaveAttributeAsync("aria-valuemax", "100");

        var ariaValueText = await root.GetAttributeAsync("aria-valuetext");
        Assert.NotNull(ariaValueText);
        Assert.NotEmpty(ariaValueText);
    }

    [Fact]
    public virtual async Task Root_RendersNvdaPresentationSpan()
    {
        await NavigateAsync(CreateUrl("/tests/progress")
            .WithProgressValue(50)
            .WithShowProgressLabel(false));

        var hidden = GetProgressRoot().Locator("> span[role='presentation']");
        await Assertions.Expect(hidden).ToHaveTextAsync("x");

        var style = await hidden.GetAttributeAsync("style");
        Assert.NotNull(style);
        Assert.Contains("clip-path:inset(50%)", style);
        Assert.Contains("position:fixed", style);
    }

    #endregion

    #region Dynamic Value Update Tests

    [Fact]
    public virtual async Task DynamicValueUpdate_ChangesAriaAndStyle()
    {
        await NavigateAsync(CreateUrl("/tests/progress")
            .WithProgressValue(25));

        var root = GetProgressRoot();
        var indicator = GetProgressIndicator();

        // Initial state
        await Assertions.Expect(root).ToHaveAttributeAsync("aria-valuenow", "25");
        var style = await indicator.GetAttributeAsync("style");
        Assert.Contains("width:25%", style);

        // Click button to change to 50%
        var button50 = GetByTestId("set-value-50");
        await button50.ClickAsync();
        await WaitForDelayAsync(100);

        await Assertions.Expect(root).ToHaveAttributeAsync("aria-valuenow", "50");
        style = await indicator.GetAttributeAsync("style");
        Assert.Contains("width:50%", style);

        // Click button to change to 100%
        var button100 = GetByTestId("set-value-100");
        await button100.ClickAsync();
        await WaitForDelayAsync(100);

        await Assertions.Expect(root).ToHaveAttributeAsync("aria-valuenow", "100");
        style = await indicator.GetAttributeAsync("style");
        Assert.Contains("width:100%", style);
    }

    #endregion

    #region Data Attribute Tests

    [Fact]
    public virtual async Task DataAttributes_ProgressingState()
    {
        await NavigateAsync(CreateUrl("/tests/progress")
            .WithProgressValue(50));

        var root = GetProgressRoot();
        await Assertions.Expect(root).ToHaveAttributeAsync("data-progressing", "");
    }

    [Fact]
    public virtual async Task DataAttributes_CompleteState()
    {
        await NavigateAsync(CreateUrl("/tests/progress")
            .WithProgressValue(100));

        var root = GetProgressRoot();
        await Assertions.Expect(root).ToHaveAttributeAsync("data-complete", "");
    }

    [Fact]
    public virtual async Task DataAttributes_IndeterminateState()
    {
        await NavigateAsync(CreateUrl("/tests/progress")
            .WithProgressValue(null));

        var root = GetProgressRoot();
        await Assertions.Expect(root).ToHaveAttributeAsync("data-indeterminate", "");
    }

    #endregion

    #region Label Association Tests

    [Fact]
    public virtual async Task Label_LinkedToRootViaAriaLabelledBy()
    {
        await NavigateAsync(CreateUrl("/tests/progress")
            .WithProgressValue(50)
            .WithShowProgressLabel(true)
            .WithLabelText("Downloading"));

        var root = GetProgressRoot();
        var label = GetProgressLabel();

        var labelId = await label.GetAttributeAsync("id");
        Assert.NotNull(labelId);
        Assert.NotEmpty(labelId);

        await Assertions.Expect(root).ToHaveAttributeAsync("aria-labelledby", labelId!);
        await Assertions.Expect(label).ToHaveTextAsync("Downloading");
        await Assertions.Expect(label).ToHaveAttributeAsync("role", "presentation");
    }

    #endregion

    #region Value Display Tests

    [Fact]
    public virtual async Task Value_DisplaysFormattedPercentage()
    {
        await NavigateAsync(CreateUrl("/tests/progress")
            .WithProgressValue(50)
            .WithShowProgressValue(true));

        var value = GetProgressValue();
        await Assertions.Expect(value).ToHaveAttributeAsync("aria-hidden", "true");

        var text = await value.TextContentAsync();
        Assert.NotNull(text);
        Assert.NotEmpty(text);
    }

    [Fact]
    public virtual async Task Value_DisplaysCustomFormat()
    {
        await NavigateAsync(CreateUrl("/tests/progress")
            .WithProgressValue(50)
            .WithShowProgressValue(true)
            .WithProgressFormat("F1"));

        var value = GetProgressValue();
        var text = await value.TextContentAsync();
        Assert.NotNull(text);
        Assert.NotEmpty(text);
    }

    #endregion
}
