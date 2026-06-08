using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Tests.Form;

public abstract class FormTestsBase : TestBase
{
    protected FormTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    /// <summary>
    /// Types text into a Blazor input by clicking to focus, then using PressSequentiallyAsync.
    /// This ensures Blazor's oninput handler fires for each keystroke, unlike FillAsync
    /// which may not reliably trigger Blazor Server's event dispatch.
    /// </summary>
    private async Task BlazorTypeAsync(ILocator control, string text)
    {
        await control.ClickAsync();
        await control.PressSequentiallyAsync(text);
        await WaitForDelayAsync(100);
    }

    // F2: does not submit if there are errors
    [Fact]
    public virtual async Task DoesNotSubmitIfThereAreErrors()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/form")
                .WithTestScenario("submit-with-errors")
                .Build();
            await NavigateAsync(url);

            var submitButton = GetByTestId("submit-button");
            await submitButton.ClickAsync();
            await WaitForDelayAsync(300);

            var validSubmitCount = GetByTestId("valid-submit-count");
            await Assertions.Expect(validSubmitCount).ToHaveTextAsync("0");

            var invalidSubmitCount = GetByTestId("invalid-submit-count");
            await Assertions.Expect(invalidSubmitCount).ToHaveTextAsync("1");
        });
    }

    // F3: unmounted fields should be removed from the form
    // In OnSubmit mode, validityData.Value is only updated through CommitAsync.
    // Before the first submit, typing does not trigger validation, so validityData.Value
    // remains stale (empty). On submit, ValidateAllAsync passes the stale value to
    // CommitAsync, causing ValidateRequired to fail. After the first submit
    // (submitAttempted=true), typing triggers revalidation with the actual current value.
    [Fact]
    public virtual async Task UnmountedFieldsShouldBeRemovedFromForm()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/form")
                .WithTestScenario("unmounted-fields")
                .Build();
            await NavigateAsync(url);

            // Remove the optional field
            var toggleButton = GetByTestId("toggle-field");
            await toggleButton.ClickAsync();
            await WaitForDelayAsync(200);

            // First submit to set submitAttempted=true (will fail because name field
            // has ValidateRequired and validityData.Value is stale/empty)
            var submitButton = GetByTestId("submit-button");
            await submitButton.ClickAsync();
            await WaitForDelayAsync(300);

            // Now type - revalidation fires because submitAttempted=true,
            // calling CommitAsync(CurrentValue) with the actual typed value
            var control = GetByTestId("field-control");
            await BlazorTypeAsync(control, "valid value");
            await WaitForDelayAsync(200);

            // Second submit - field is now valid after revalidation
            await submitButton.ClickAsync();
            await WaitForDelayAsync(300);

            var validSubmitCount = GetByTestId("valid-submit-count");
            await Assertions.Expect(validSubmitCount).ToHaveTextAsync("1");
        });
    }

    // F6: prop errors focuses first invalid field only on submit
    [Fact]
    public virtual async Task ErrorsFocusesFirstInvalidFieldOnSubmit()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/form")
                .WithTestScenario("focus-invalid-on-submit")
                .Build();
            await NavigateAsync(url);

            var submitButton = GetByTestId("submit-button");
            await submitButton.ClickAsync();
            await WaitForDelayAsync(300);

            // First invalid field should be focused
            var firstControl = GetByTestId("field-control-first");
            await Assertions.Expect(firstControl).ToBeFocusedAsync();
        });
    }

    // F7: prop errors does not swap focus on change after two submissions
    [Fact]
    public virtual async Task ErrorsDoesNotSwapFocusOnChangeAfterTwoSubmissions()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/form")
                .WithTestScenario("focus-invalid-on-submit")
                .Build();
            await NavigateAsync(url);

            // First submit
            var submitButton = GetByTestId("submit-button");
            await submitButton.ClickAsync();
            await WaitForDelayAsync(300);

            // Type in first field using keyboard to ensure Blazor receives input
            var firstControl = GetByTestId("field-control-first");
            await BlazorTypeAsync(firstControl, "value");
            await WaitForDelayAsync(200);

            // Second submit
            await submitButton.ClickAsync();
            await WaitForDelayAsync(300);

            // Second field should be focused now (first is valid)
            var secondControl = GetByTestId("field-control-second");
            await Assertions.Expect(secondControl).ToBeFocusedAsync();
        });
    }

    // F8: prop errors removes errors upon change
    [Fact]
    public virtual async Task ErrorsRemovedUponChange()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/form")
                .WithTestScenario("errors-removed-on-change")
                .Build();
            await NavigateAsync(url);

            // Set errors
            var setErrorsButton = GetByTestId("set-errors");
            await setErrorsButton.ClickAsync();
            await WaitForDelayAsync(200);

            // Error should be visible
            var fieldError = GetByTestId("field-error");
            await Assertions.Expect(fieldError).ToBeVisibleAsync();

            // Type to clear errors using keyboard
            var control = GetByTestId("field-control");
            await BlazorTypeAsync(control, "new value");
            await WaitForDelayAsync(300);

            // Error should be gone
            await Assertions.Expect(fieldError).Not.ToBeVisibleAsync();
        });
    }

    // F9: prop onFormSubmit runs when submitted
    // In OnSubmit mode, validityData.Value is only updated through CommitAsync.
    // Before the first submit, typing does not trigger validation, so validityData.Value
    // remains stale. After the first submit (submitAttempted=true), typing triggers
    // revalidation via ShouldValidateOnChange, which calls CommitAsync(CurrentValue)
    // and updates validityData.Value. We type a value, then press Enter to submit
    // from within the input field. Enter triggers HandleKeyDown which calls
    // CommitAsync(CurrentValue), ensuring the value is committed before the form
    // submit handler collects values.
    [Fact]
    public virtual async Task OnFormSubmitRunsWhenSubmitted()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/form")
                .WithTestScenario("on-form-submit")
                .Build();
            await NavigateAsync(url);

            // First submit to set submitAttempted=true (field has no Validate,
            // so the form is valid and OnFormSubmit fires with stale empty values)
            var submitButton = GetByTestId("submit-button");
            await submitButton.ClickAsync();
            await WaitForDelayAsync(300);

            // Type a single character and press Enter to submit from within
            // the input. Enter triggers HandleKeyDown which calls
            // CommitAsync(CurrentValue), ensuring the value is committed
            // before the form submit handler collects values via GetValue().
            // Using a single character avoids race conditions with multiple
            // SignalR messages for sequential keystrokes on Blazor Server.
            var control = GetByTestId("field-control");
            await control.ClickAsync();
            await control.PressSequentiallyAsync("a");
            await WaitForDelayAsync(300);

            // Submit via Enter key from within the input field
            await control.PressAsync("Enter");
            await WaitForDelayAsync(500);

            var formSubmitCount = GetByTestId("form-submit-count");
            await Assertions.Expect(formSubmitCount).ToHaveTextAsync("2");

            var lastValues = GetByTestId("last-submit-values");
            await Assertions.Expect(lastValues).ToContainTextAsync("name=a");
        });
    }

    // F10: prop onFormSubmit does not run when invalid
    [Fact]
    public virtual async Task OnFormSubmitDoesNotRunWhenInvalid()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/form")
                .WithTestScenario("submit-with-errors")
                .Build();
            await NavigateAsync(url);

            var submitButton = GetByTestId("submit-button");
            await submitButton.ClickAsync();
            await WaitForDelayAsync(300);

            var validSubmitCount = GetByTestId("valid-submit-count");
            await Assertions.Expect(validSubmitCount).ToHaveTextAsync("0");
        });
    }

    // F13: actionsRef validates form via validate method
    [Fact]
    public virtual async Task ActionsRefValidatesForm()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/form")
                .WithTestScenario("actions-ref-validate")
                .Build();
            await NavigateAsync(url);

            var validateButton = GetByTestId("validate-button");
            await validateButton.ClickAsync();
            await WaitForDelayAsync(300);

            var fieldError = GetByTestId("field-error");
            await Assertions.Expect(fieldError).ToBeVisibleAsync();

            var fieldErrorEmail = GetByTestId("field-error-email");
            await Assertions.Expect(fieldErrorEmail).ToBeVisibleAsync();
        });
    }

    // F14: actionsRef validates specific field by name
    [Fact]
    public virtual async Task ActionsRefValidatesSpecificField()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/form")
                .WithTestScenario("actions-ref-validate-field")
                .Build();
            await NavigateAsync(url);

            // Validate only the name field
            var validateNameButton = GetByTestId("validate-name");
            await validateNameButton.ClickAsync();
            await WaitForDelayAsync(300);

            var fieldError = GetByTestId("field-error");
            await Assertions.Expect(fieldError).ToBeVisibleAsync();

            // Email field should NOT have error since we only validated name
            var fieldErrorEmail = GetByTestId("field-error-email");
            await Assertions.Expect(fieldErrorEmail).Not.ToBeVisibleAsync();
        });
    }
}
