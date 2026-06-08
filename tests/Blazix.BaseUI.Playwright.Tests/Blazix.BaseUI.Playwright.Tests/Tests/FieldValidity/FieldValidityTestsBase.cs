using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Tests.FieldValidity;

public abstract class FieldValidityTestsBase : TestBase
{
    protected FieldValidityTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    // FV1: onSubmit passes validity data
    [Fact]
    public virtual async Task OnSubmitPassesValidityData()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("field-validity-onsubmit")
                .Build();
            await NavigateAsync(url);

            var submitButton = GetByTestId("submit-button");
            await submitButton.ClickAsync();
            await WaitForDelayAsync(300);

            var validityValid = GetByTestId("validity-valid");
            await Assertions.Expect(validityValid).ToHaveTextAsync("false");

            var validityError = GetByTestId("validity-error");
            await Assertions.Expect(validityError).ToContainTextAsync("required");
        });
    }

    // FV2: onBlur passes validity data
    [Fact]
    public virtual async Task OnBlurPassesValidityData()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("field-validity-onblur")
                .Build();
            await NavigateAsync(url);

            await TriggerBlurValidationAsync();

            var validityValid = GetByTestId("validity-valid");
            await Assertions.Expect(validityValid).ToHaveTextAsync("false");
        });
    }

    // FV3: onBlur correctly passes errors (string)
    [Fact]
    public virtual async Task OnBlurCorrectlyPassesErrorsString()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("field-validity-errors-string")
                .Build();
            await NavigateAsync(url);

            await TriggerBlurValidationAsync();

            var validityError = GetByTestId("validity-error");
            await Assertions.Expect(validityError).ToHaveTextAsync("Single error message");

            var validityErrorsCount = GetByTestId("validity-errors-count");
            await Assertions.Expect(validityErrorsCount).ToHaveTextAsync("1");
        });
    }

    // FV4: onBlur correctly passes errors (array)
    [Fact]
    public virtual async Task OnBlurCorrectlyPassesErrorsArray()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("field-validity-errors-array")
                .Build();
            await NavigateAsync(url);

            await TriggerBlurValidationAsync();

            var validityErrorsCount = GetByTestId("validity-errors-count");
            await Assertions.Expect(validityErrorsCount).ToHaveTextAsync("3");

            var errorItems = Page.GetByTestId("validity-error-item");
            await Assertions.Expect(errorItems).ToHaveCountAsync(3);
        });
    }

    private async Task TriggerBlurValidationAsync()
    {
        var control = GetByTestId("field-control");
        await control.FocusAsync();
        await Assertions.Expect(control).ToBeFocusedAsync(new LocatorAssertionsToBeFocusedOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });

        await control.FillAsync("a");
        await Assertions.Expect(control).ToHaveValueAsync("a", new LocatorAssertionsToHaveValueOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });

        await control.FillAsync("");
        await Assertions.Expect(control).ToHaveValueAsync("", new LocatorAssertionsToHaveValueOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });

        var otherInput = GetByTestId("other-input");
        await otherInput.ClickAsync();
        await Assertions.Expect(otherInput).ToBeFocusedAsync(new LocatorAssertionsToBeFocusedOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
    }
}
