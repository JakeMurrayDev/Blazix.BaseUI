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

            var control = GetByTestId("field-control");
            await control.FocusAsync();
            await control.FillAsync("a");
            await control.FillAsync("");

            var otherInput = GetByTestId("other-input");
            await otherInput.ClickAsync();
            await WaitForDelayAsync(300);

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

            var control = GetByTestId("field-control");
            await control.FocusAsync();
            await control.FillAsync("a");
            await control.FillAsync("");

            var otherInput = GetByTestId("other-input");
            await otherInput.ClickAsync();
            await WaitForDelayAsync(300);

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

            var control = GetByTestId("field-control");
            await control.FocusAsync();
            await control.FillAsync("a");
            await control.FillAsync("");

            var otherInput = GetByTestId("other-input");
            await otherInput.ClickAsync();
            await WaitForDelayAsync(300);

            var validityErrorsCount = GetByTestId("validity-errors-count");
            await Assertions.Expect(validityErrorsCount).ToHaveTextAsync("3");

            var errorItems = Page.GetByTestId("validity-error-item");
            await Assertions.Expect(errorItems).ToHaveCountAsync(3);
        });
    }
}
