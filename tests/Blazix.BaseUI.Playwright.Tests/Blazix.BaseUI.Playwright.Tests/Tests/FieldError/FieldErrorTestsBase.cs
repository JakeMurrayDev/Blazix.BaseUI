using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Tests.FieldError;

public abstract class FieldErrorTestsBase : TestBase
{
    protected FieldErrorTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    // FE3: shows error messages by default on submit
    [Fact]
    public virtual async Task ShowsErrorMessagesByDefaultOnSubmit()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("field-error-submit")
                .Build();
            await NavigateAsync(url);

            var submitButton = GetByTestId("submit-button");
            await submitButton.ClickAsync();
            await WaitForDelayAsync(300);

            var fieldError = GetByTestId("field-error");
            await Assertions.Expect(fieldError).ToBeVisibleAsync();
            await Assertions.Expect(fieldError).ToContainTextAsync("required");
        });
    }

    // FE4: match only renders when match matches validation
    [Fact]
    public virtual async Task MatchOnlyRendersWhenMatchMatchesValidation()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("field-error-match")
                .WithMatchValidity("customError")
                .Build();
            await NavigateAsync(url);

            var submitButton = GetByTestId("submit-button");
            await submitButton.ClickAsync();
            await WaitForDelayAsync(300);

            // The error with MatchValidity="customError" should render because validation produces custom errors
            var matchError = GetByTestId("field-error-match");
            await Assertions.Expect(matchError).ToBeVisibleAsync();
        });
    }

    // FE5: match shows custom errors
    [Fact]
    public virtual async Task MatchShowsCustomErrors()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("field-error-custom")
                .Build();
            await NavigateAsync(url);

            var submitButton = GetByTestId("submit-button");
            await submitButton.ClickAsync();
            await WaitForDelayAsync(300);

            var customError = GetByTestId("field-error-custom");
            await Assertions.Expect(customError).ToBeVisibleAsync();
            await Assertions.Expect(customError).ToContainTextAsync("Custom validation error");
        });
    }
}
