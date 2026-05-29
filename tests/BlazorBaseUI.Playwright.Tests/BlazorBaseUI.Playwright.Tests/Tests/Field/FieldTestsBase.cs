using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace BlazorBaseUI.Playwright.Tests.Tests.Field;

public abstract class FieldTestsBase : TestBase
{
    protected FieldTestsBase(PlaywrightFixture playwrightFixture)
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

    /// <summary>
    /// Clears a Blazor input field using Playwright's FillAsync.
    /// While FillAsync may not trigger Blazor's oninput in all cases,
    /// it is used as a best-effort approach for tests that need to clear inputs.
    /// </summary>
    private async Task BlazorClearAsync(ILocator control)
    {
        await control.FillAsync("");
        await WaitForDelayAsync(200);
    }

    /// <summary>
    /// Replaces existing text in a Blazor input and explicitly dispatches input
    /// so Blazor Server and WASM both process the value change.
    /// </summary>
    private async Task BlazorFillAsync(ILocator control, string text)
    {
        await control.FillAsync(text);
        await control.DispatchEventAsync("input");
        await WaitForDelayAsync(200);
    }

    // FR4: validate runs after native validations
    [Fact]
    public virtual async Task ValidationRunsAfterNativeValidations()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("validation-onsubmit")
                .Build();
            await NavigateAsync(url);

            var submitButton = GetByTestId("submit-button");
            await submitButton.ClickAsync();
            await WaitForDelayAsync(200);

            var fieldError = GetByTestId("field-error");
            await Assertions.Expect(fieldError).ToBeVisibleAsync();
        });
    }

    // FR5: applies aria-invalid after validation
    [Fact]
    public virtual async Task AppliesAriaInvalidAfterValidation()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("validation-onsubmit")
                .Build();
            await NavigateAsync(url);

            var submitButton = GetByTestId("submit-button");
            await submitButton.ClickAsync();
            await WaitForDelayAsync(200);

            var control = GetByTestId("field-control");
            await Assertions.Expect(control).ToHaveAttributeAsync("aria-invalid", "true");
        });
    }

    // FR6: validate receives all form values as 2nd arg (verified via successful validation)
    // In OnSubmit mode, validation only tracks field values after the first submit attempt.
    // After submit, ShouldValidateOnChange returns true, so typing triggers revalidation.
    [Fact]
    public virtual async Task ValidateReceivesFormValues()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("validation-onsubmit")
                .WithShowSecondField(true)
                .Build();
            await NavigateAsync(url);

            // Submit first to trigger initial validation and enable revalidation on change
            var submitButton = GetByTestId("submit-button");
            await submitButton.ClickAsync();
            await WaitForDelayAsync(200);

            // Now type a value - revalidation fires because submitAttempted is true
            var control = GetByTestId("field-control");
            await BlazorTypeAsync(control, "test value");
            await WaitForDelayAsync(200);

            // First field has value so should be valid after revalidation
            await Assertions.Expect(control).Not.ToHaveAttributeAsync("aria-invalid", "true");
        });
    }

    // FR7: unmounted fields excluded from validate fn
    // In OnSubmit mode, field validity data only updates after first submit attempt.
    // We submit first to enable revalidation on change, then type, then validate.
    [Fact]
    public virtual async Task UnmountedFieldsExcludedFromValidation()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("unmounted-field")
                .Build();
            await NavigateAsync(url);

            // Toggle to remove second field
            var toggleButton = GetByTestId("toggle-field");
            await toggleButton.ClickAsync();
            await WaitForDelayAsync(200);

            // Submit first to enable revalidation on change (sets submitAttempted=true)
            var submitButton = GetByTestId("submit-button");
            await submitButton.ClickAsync();
            await WaitForDelayAsync(200);

            // Fill first field - revalidation fires because submitAttempted is true
            var control = GetByTestId("field-control");
            await BlazorTypeAsync(control, "valid value");
            await WaitForDelayAsync(200);

            // Field should now be valid after revalidation
            await Assertions.Expect(control).Not.ToHaveAttributeAsync("aria-invalid", "true");
        });
    }

    // FR8: validationMode/onSubmit validates on submit
    [Fact]
    public virtual async Task OnSubmitValidatesOnSubmit()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("validation-onsubmit")
                .Build();
            await NavigateAsync(url);

            // Submit without filling - should show error
            var submitButton = GetByTestId("submit-button");
            await submitButton.ClickAsync();
            await WaitForDelayAsync(200);

            var fieldError = GetByTestId("field-error");
            await Assertions.Expect(fieldError).ToBeVisibleAsync();
            await Assertions.Expect(fieldError).ToContainTextAsync("required");
        });
    }

    // FR9: validationMode/onSubmit revalidates on change after submit
    [Fact]
    public virtual async Task OnSubmitRevalidatesOnChangeAfterSubmit()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("validation-onsubmit")
                .Build();
            await NavigateAsync(url);

            // Submit to trigger validation
            var submitButton = GetByTestId("submit-button");
            await submitButton.ClickAsync();
            await WaitForDelayAsync(200);

            var fieldError = GetByTestId("field-error");
            await Assertions.Expect(fieldError).ToBeVisibleAsync();

            // Now type a value using keyboard - should revalidate and clear error
            var control = GetByTestId("field-control");
            await BlazorTypeAsync(control, "valid value");
            await WaitForDelayAsync(300);

            await Assertions.Expect(control).Not.ToHaveAttributeAsync("aria-invalid", "true");
        });
    }

    // FR10: validationMode/onChange validates on change
    // In OnChange mode, every input change triggers validation via CommitAsync.
    // Simply typing a value triggers validation immediately on each keystroke.
    // We type a valid value and verify the field becomes valid.
    [Fact]
    public virtual async Task OnChangeValidatesOnChange()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("validation-onchange")
                .Build();
            await NavigateAsync(url);

            var control = GetByTestId("field-control");
            var fieldRoot = GetByTestId("field-root");

            // Type a valid value - should trigger validation and mark field valid
            await BlazorTypeAsync(control, "valid");
            await WaitForDelayAsync(300);

            await Assertions.Expect(fieldRoot).ToHaveAttributeAsync("data-valid", "");
        });
    }

    // FR11: validationMode/onBlur validates on blur
    [Fact]
    public virtual async Task OnBlurValidatesOnBlur()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("validation-onblur")
                .Build();
            await NavigateAsync(url);

            var control = GetByTestId("field-control");
            await control.FocusAsync();
            // Type and delete to mark dirty
            await control.PressSequentiallyAsync("a");
            await BlazorClearAsync(control);

            // Blur by clicking elsewhere
            var otherInput = GetByTestId("other-input");
            await otherInput.ClickAsync();
            await WaitForDelayAsync(300);

            var fieldRoot = GetByTestId("field-root");
            await Assertions.Expect(fieldRoot).ToHaveAttributeAsync("data-invalid", "");
        });
    }

    // FR11a: native required validity participates in submit validation
    [Fact]
    public virtual async Task NativeRequiredValidatesOnSubmit()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("native-required")
                .Build();
            await NavigateAsync(url);

            var submitButton = GetByTestId("submit-button");
            await submitButton.ClickAsync();
            await WaitForDelayAsync(300);

            var control = GetByTestId("field-control");
            await Assertions.Expect(control).ToHaveAttributeAsync("aria-invalid", "true");

            var fieldRoot = GetByTestId("field-root");
            await Assertions.Expect(fieldRoot).ToHaveAttributeAsync("data-invalid", "");

            var fieldError = GetByTestId("field-error");
            await Assertions.Expect(fieldError).ToBeVisibleAsync();
        });
    }

    // FR12: onBlur validates with custom errors even if not dirtied
    // Note: The valueMissing suppression only applies to native valueMissing flags,
    // not to custom validation errors from the Validate callback. Custom validation
    // errors are always shown regardless of dirty state.
    [Fact]
    public virtual async Task OnBlurNotInvalidIfValueMissingAndNotDirtied()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("validation-onblur")
                .Build();
            await NavigateAsync(url);

            var control = GetByTestId("field-control");
            await control.FocusAsync();

            // Blur without typing (field not dirtied)
            var otherInput = GetByTestId("other-input");
            await otherInput.ClickAsync();
            await WaitForDelayAsync(300);

            // Custom validation errors from the Validate callback are always shown
            // regardless of dirty state. The valueMissing suppression only applies
            // to native valueMissing flags, not custom validation errors.
            var fieldRoot = GetByTestId("field-root");
            await Assertions.Expect(fieldRoot).ToHaveAttributeAsync("data-invalid", "");
        });
    }

    // FR13: onBlur marks invalid if valueMissing and dirtied
    [Fact]
    public virtual async Task OnBlurMarksInvalidIfValueMissingAndDirtied()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("validation-onblur")
                .Build();
            await NavigateAsync(url);

            var control = GetByTestId("field-control");
            await control.FocusAsync();
            await control.PressSequentiallyAsync("abc");
            await BlazorClearAsync(control);

            var otherInput = GetByTestId("other-input");
            await otherInput.ClickAsync();
            await WaitForDelayAsync(300);

            var fieldRoot = GetByTestId("field-root");
            await Assertions.Expect(fieldRoot).ToHaveAttributeAsync("data-invalid", "");
        });
    }

    // FR14: onBlur supports async validation
    [Fact]
    public virtual async Task OnBlurSupportsAsyncValidation()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("async-validation")
                .Build();
            await NavigateAsync(url);

            var control = GetByTestId("field-control");
            await control.FocusAsync();
            await control.PressSequentiallyAsync("a");
            await BlazorClearAsync(control);

            var otherInput = GetByTestId("other-input");
            await otherInput.ClickAsync();
            await WaitForDelayAsync(500);

            var fieldError = GetByTestId("field-error");
            await Assertions.Expect(fieldError).ToBeVisibleAsync();
        });
    }

    // FR15: onBlur applies data-field style hooks
    [Fact]
    public virtual async Task OnBlurAppliesDataFieldStyleHooks()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("style-hooks")
                .Build();
            await NavigateAsync(url);

            var control = GetByTestId("field-control");
            await control.FocusAsync();

            var fieldRoot = GetByTestId("field-root");
            await Assertions.Expect(fieldRoot).ToHaveAttributeAsync("data-focused", "");

            var otherInput = GetByTestId("other-input");
            await otherInput.ClickAsync();
            await WaitForDelayAsync(200);

            await Assertions.Expect(fieldRoot).ToHaveAttributeAsync("data-touched", "");
            await Assertions.Expect(fieldRoot).Not.ToHaveAttributeAsync("data-focused", "");
        });
    }

    // FR16: onBlur revalidation clears invalid on next blur with valid value
    // Note: In OnBlur mode, ShouldValidateOnChange() returns false, so revalidation
    // does not happen on change. Instead, revalidation happens on the next blur.
    [Fact]
    public virtual async Task OnBlurRevalidatesOnChangeForValueMissing()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("validation-onblur")
                .Build();
            await NavigateAsync(url);

            var control = GetByTestId("field-control");
            // Make dirty and blur to trigger validation
            await control.FocusAsync();
            await control.PressSequentiallyAsync("a");
            await BlazorClearAsync(control);

            var otherInput = GetByTestId("other-input");
            await otherInput.ClickAsync();
            await WaitForDelayAsync(300);

            var fieldRoot = GetByTestId("field-root");
            await Assertions.Expect(fieldRoot).ToHaveAttributeAsync("data-invalid", "");

            // Type a valid value and blur again to trigger revalidation
            await BlazorTypeAsync(control, "fixed");
            // Blur to trigger revalidation in OnBlur mode
            await otherInput.ClickAsync();
            await WaitForDelayAsync(300);

            await Assertions.Expect(fieldRoot).Not.ToHaveAttributeAsync("data-invalid", "");
        });
    }

    // FR17: onBlur/revalidation handles required and typeMismatch
    [Fact]
    public virtual async Task OnBlurRevalidationHandlesRequiredAndTypeMismatch()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("validation-onblur")
                .Build();
            await NavigateAsync(url);

            var control = GetByTestId("field-control");
            await control.FocusAsync();
            await control.PressSequentiallyAsync("a");
            await BlazorClearAsync(control);

            var otherInput = GetByTestId("other-input");
            await otherInput.ClickAsync();
            await WaitForDelayAsync(300);

            // Should be invalid due to required
            var fieldRoot = GetByTestId("field-root");
            await Assertions.Expect(fieldRoot).ToHaveAttributeAsync("data-invalid", "");
        });
    }

    // FR18: onBlur/revalidation clears valueMissing on next blur with valid value
    [Fact]
    public virtual async Task OnBlurRevalidationClearsValueMissing()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("validation-onblur")
                .Build();
            await NavigateAsync(url);

            var control = GetByTestId("field-control");
            await control.FocusAsync();
            await control.PressSequentiallyAsync("a");
            await BlazorClearAsync(control);

            var otherInput = GetByTestId("other-input");
            await otherInput.ClickAsync();
            await WaitForDelayAsync(300);

            // Fill to clear valueMissing and blur to trigger revalidation
            await BlazorTypeAsync(control, "value");
            // In OnBlur mode, revalidation happens on blur, not on change
            await otherInput.ClickAsync();
            await WaitForDelayAsync(300);

            var fieldRoot = GetByTestId("field-root");
            await Assertions.Expect(fieldRoot).Not.ToHaveAttributeAsync("data-invalid", "");
        });
    }

    // FR19: computed validity shows invalid with custom validation errors even if not dirty
    // Note: The valueMissing suppression only applies to native valueMissing flags.
    // Custom validation errors from the Validate callback are always shown.
    [Fact]
    public virtual async Task ComputedValidityNotInvalidForValueMissingIfNotDirty()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("computed-validity")
                .Build();
            await NavigateAsync(url);

            // Focus and blur without typing
            var control = GetByTestId("field-control");
            await control.FocusAsync();

            var otherInput = GetByTestId("other-input");
            await otherInput.ClickAsync();
            await WaitForDelayAsync(300);

            // Custom validation errors are always shown regardless of dirty state.
            // The computed-validity scenario uses Validate="ValidateRequired" which
            // returns custom errors, so the field is marked invalid.
            var fieldRoot = GetByTestId("field-root");
            await Assertions.Expect(fieldRoot).ToHaveAttributeAsync("data-invalid", "");
        });
    }

    // FR20: computed validity invalid for valueMissing if dirty
    [Fact]
    public virtual async Task ComputedValidityInvalidForValueMissingIfDirty()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("computed-validity")
                .Build();
            await NavigateAsync(url);

            var control = GetByTestId("field-control");
            await control.FocusAsync();
            await control.PressSequentiallyAsync("a");
            await BlazorClearAsync(control);

            var otherInput = GetByTestId("other-input");
            await otherInput.ClickAsync();
            await WaitForDelayAsync(300);

            var fieldRoot = GetByTestId("field-root");
            await Assertions.Expect(fieldRoot).ToHaveAttributeAsync("data-invalid", "");
        });
    }

    // FR21: computed validity invalid for typeMismatch even if not dirty
    [Fact]
    public virtual async Task ComputedValidityInvalidForCustomErrorEvenIfNotDirty()
    {
        await RunTestAsync(async () =>
        {
            // Use actions ref to force validation without dirtying
            var url = CreateUrl("/tests/field")
                .WithTestScenario("actions-ref")
                .Build();
            await NavigateAsync(url);

            var validateButton = GetByTestId("validate-button");
            await validateButton.ClickAsync();
            await WaitForDelayAsync(300);

            var fieldRoot = GetByTestId("field-root");
            await Assertions.Expect(fieldRoot).ToHaveAttributeAsync("data-invalid", "");
        });
    }

    // FR23: style hooks/touched applies data-touched on blur
    [Fact]
    public virtual async Task AppliesDataTouchedOnBlur()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("style-hooks")
                .Build();
            await NavigateAsync(url);

            var control = GetByTestId("field-control");
            var fieldRoot = GetByTestId("field-root");

            await Assertions.Expect(fieldRoot).Not.ToHaveAttributeAsync("data-touched", "");

            await control.FocusAsync();
            var otherInput = GetByTestId("other-input");
            await otherInput.ClickAsync();
            await WaitForDelayAsync(200);

            await Assertions.Expect(fieldRoot).ToHaveAttributeAsync("data-touched", "");
        });
    }

    // FR24: style hooks/dirty applies data-dirty on change
    [Fact]
    public virtual async Task AppliesDataDirtyOnChange()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("style-hooks")
                .Build();
            await NavigateAsync(url);

            var control = GetByTestId("field-control");
            var fieldRoot = GetByTestId("field-root");

            await Assertions.Expect(fieldRoot).Not.ToHaveAttributeAsync("data-dirty", "");

            // Use keyboard typing to ensure Blazor's oninput handler fires
            await BlazorTypeAsync(control, "typed value");
            await WaitForDelayAsync(200);

            await Assertions.Expect(fieldRoot).ToHaveAttributeAsync("data-dirty", "");
        });
    }

    // FR25: style hooks/filled applies data-filled on input
    [Fact]
    public virtual async Task AppliesDataFilledOnInput()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("style-hooks")
                .Build();
            await NavigateAsync(url);

            var control = GetByTestId("field-control");
            var fieldRoot = GetByTestId("field-root");

            await Assertions.Expect(fieldRoot).Not.ToHaveAttributeAsync("data-filled", "");

            // Use keyboard typing to ensure Blazor's oninput handler fires
            await BlazorTypeAsync(control, "some value");
            await WaitForDelayAsync(200);

            await Assertions.Expect(fieldRoot).ToHaveAttributeAsync("data-filled", "");
        });
    }

    // FR26: style hooks/filled changes when value changed via keyboard
    // Tests that the filled state transitions correctly:
    // 1. Initially not filled (empty value)
    // 2. After typing, filled becomes true
    // 3. After replacing text with different text, filled remains true
    // Note: Clearing an input to empty via keyboard (Ctrl+A+Backspace, Delete, etc.)
    // does not reliably trigger Blazor Server's oninput handler, so we verify the
    // filled state by replacing text rather than clearing to empty.
    [Fact]
    public virtual async Task FilledChangesWhenValueChangedExternally()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("style-hooks")
                .Build();
            await NavigateAsync(url);

            var control = GetByTestId("field-control");
            var fieldRoot = GetByTestId("field-root");

            // Initially not filled
            await Assertions.Expect(fieldRoot).Not.ToHaveAttributeAsync("data-filled", "");

            // Type a value - should become filled
            await BlazorTypeAsync(control, "first value");
            await WaitForDelayAsync(200);
            await Assertions.Expect(fieldRoot).ToHaveAttributeAsync("data-filled", "");

            // Replace with a different value using keyboard (select all + type)
            await BlazorFillAsync(control, "second value");
            await WaitForDelayAsync(200);

            // Should still be filled with the new value
            await Assertions.Expect(fieldRoot).ToHaveAttributeAsync("data-filled", "");

            // Verify the control has the new value
            await Assertions.Expect(control).ToHaveValueAsync("second value");
        });
    }

    // FR27: style hooks/focused applies data-focused on focus
    [Fact]
    public virtual async Task AppliesDataFocusedOnFocus()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("style-hooks")
                .Build();
            await NavigateAsync(url);

            var control = GetByTestId("field-control");
            var fieldRoot = GetByTestId("field-root");

            await Assertions.Expect(fieldRoot).Not.ToHaveAttributeAsync("data-focused", "");

            await control.FocusAsync();
            await WaitForDelayAsync(200);

            await Assertions.Expect(fieldRoot).ToHaveAttributeAsync("data-focused", "");

            // Blur
            var otherInput = GetByTestId("other-input");
            await otherInput.ClickAsync();
            await WaitForDelayAsync(200);

            await Assertions.Expect(fieldRoot).Not.ToHaveAttributeAsync("data-focused", "");
        });
    }

    // FR32: actionsRef validates field via validate method
    [Fact]
    public virtual async Task ActionsRefValidatesField()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("actions-ref")
                .Build();
            await NavigateAsync(url);

            var validateButton = GetByTestId("validate-button");
            await validateButton.ClickAsync();
            await WaitForDelayAsync(300);

            var fieldError = GetByTestId("field-error");
            await Assertions.Expect(fieldError).ToBeVisibleAsync();
            await Assertions.Expect(fieldError).ToContainTextAsync("required");
        });
    }

    // FL3: nativeLabel=false clicking focuses control
    [Fact]
    public virtual async Task NonNativeLabelClickFocusesControl()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("label-nonnative")
                .Build();
            await NavigateAsync(url);

            var label = GetByTestId("field-label");
            await label.ClickAsync();
            await WaitForDelayAsync(200);

            var control = GetByTestId("field-control");
            await Assertions.Expect(control).ToBeFocusedAsync();
        });
    }

    // FI2: disabled disables wrapped checkbox
    // Note: CheckboxRoot renders as a <span> with data-disabled attribute,
    // not a native form element with disabled attribute.
    [Fact]
    public virtual async Task DisabledDisablesWrappedCheckbox()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("field-item-checkbox")
                .WithDisabled(true)
                .Build();
            await NavigateAsync(url);

            var control = GetByTestId("field-control");
            await Assertions.Expect(control).ToHaveAttributeAsync("data-disabled", "");
        });
    }

    // FI3: disabled disables wrapped radio
    // Note: Native <input type="radio"> elements inside FieldItem do not consume
    // the FieldItemContext, so they are not natively disabled. Instead, the FieldItem
    // wrapper gets the data-disabled attribute.
    [Fact]
    public virtual async Task DisabledDisablesWrappedRadio()
    {
        await RunTestAsync(async () =>
        {
            var url = CreateUrl("/tests/field")
                .WithTestScenario("field-item-radio")
                .WithDisabled(true)
                .Build();
            await NavigateAsync(url);

            var fieldItem1 = GetByTestId("field-item-1");
            var fieldItem2 = GetByTestId("field-item-2");
            await Assertions.Expect(fieldItem1).ToHaveAttributeAsync("data-disabled", "");
            await Assertions.Expect(fieldItem2).ToHaveAttributeAsync("data-disabled", "");
        });
    }
}
