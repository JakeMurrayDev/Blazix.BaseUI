using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Tests.OtpField;

public abstract class OtpFieldTestsBase : TestBase
{
    protected OtpFieldTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    protected ILocator GetRoot() => GetByTestId("otp-root");
    protected ILocator GetInput(int index) => GetByTestId($"otp-input-{index}");
    protected ILocator GetHiddenInput() => Page.Locator("input[data-blazix-otp-hidden-input]");
    protected ILocator GetCurrentValue() => GetByTestId("current-value");
    protected ILocator GetChangeCount() => GetByTestId("change-count");
    protected ILocator GetInvalidCount() => GetByTestId("invalid-count");
    protected ILocator GetLastInvalidValue() => GetByTestId("last-invalid-value");
    protected ILocator GetCompleteCount() => GetByTestId("complete-count");
    protected ILocator GetLastReason() => GetByTestId("last-reason");
    protected ILocator GetLastCompleteReason() => GetByTestId("last-complete-reason");
    protected ILocator GetFormSubmitted() => GetByTestId("form-submitted");
    protected ILocator GetFormData() => GetByTestId("form-data");

    protected async Task WaitForOtpFieldAsync()
    {
        await Assertions.Expect(GetByTestId("test-container")).ToHaveAttributeAsync("data-interactive", "true");
        await Assertions.Expect(GetInput(0)).ToBeVisibleAsync();
    }

    [Fact]
    public virtual async Task RendersInitialAttributesAndActiveSlot()
    {
        await NavigateAsync(CreateUrl("/tests/otpfield")
            .WithOtpDefaultValue("12")
            .WithOtpLength(6)
            .WithOtpName("otp")
            .Build());
        await WaitForOtpFieldAsync();

        await Assertions.Expect(GetRoot()).ToHaveAttributeAsync("role", "group");
        await Assertions.Expect(GetRoot()).ToHaveAttributeAsync("data-filled", "");
        await Assertions.Expect(GetInput(0)).ToHaveValueAsync("1");
        await Assertions.Expect(GetInput(1)).ToHaveValueAsync("2");
        await Assertions.Expect(GetInput(2)).ToHaveAttributeAsync("tabindex", "0");
        await Assertions.Expect(GetHiddenInput()).ToHaveAttributeAsync("name", "otp");
        await Assertions.Expect(GetHiddenInput()).ToHaveAttributeAsync("autocomplete", "one-time-code");
        await Assertions.Expect(GetHiddenInput()).ToHaveAttributeAsync("pattern", "\\d{6}");

        var hiddenInsideRoot = await GetRoot().Locator("input[data-blazix-otp-hidden-input]").CountAsync();
        Assert.Equal(0, hiddenInsideRoot);
    }

    [Fact]
    public virtual async Task TypingNormalizesInvalidCharactersAndCompletes()
    {
        await NavigateAsync(CreateUrl("/tests/otpfield")
            .WithOtpLength(6)
            .Build());
        await WaitForOtpFieldAsync();

        await GetInput(0).FillAsync("12a345");

        await Assertions.Expect(GetCurrentValue()).ToHaveTextAsync("12345");
        await Assertions.Expect(GetInvalidCount()).ToHaveTextAsync("1");
        await Assertions.Expect(GetLastInvalidValue()).ToHaveTextAsync("12a345");
        await GetInput(5).FillAsync("6");

        await Assertions.Expect(GetCurrentValue()).ToHaveTextAsync("123456");
        await Assertions.Expect(GetInput(5)).ToHaveValueAsync("6");
        await Assertions.Expect(GetCompleteCount()).ToHaveTextAsync("1");
        await Assertions.Expect(GetLastReason()).ToHaveTextAsync("InputChange");
        await Assertions.Expect(GetLastCompleteReason()).ToHaveTextAsync("InputChange");
    }

    [Fact]
    public virtual async Task TypingRejectedCharacterIsSuppressedBeforeInsertion()
    {
        await NavigateAsync(CreateUrl("/tests/otpfield")
            .WithOtpLength(6)
            .Build());
        await WaitForOtpFieldAsync();

        await GetInput(0).FocusAsync();

        // Count input events reaching the first slot via a capture-phase listener on the document,
        // which runs before the OTP module's own capture listener stops propagation. A rejected
        // character must be suppressed at keydown so it is never inserted (and never fires input).
        await Page.EvaluateAsync(
            """
            () => {
                window.__otpSlotInputEvents = 0;
                document.addEventListener('input', (event) => {
                    if (event.target?.getAttribute?.('data-testid') === 'otp-input-0') {
                        window.__otpSlotInputEvents += 1;
                    }
                }, true);
            }
            """);

        await Page.Keyboard.PressAsync("a");

        // The character is reported invalid but never committed or inserted into the slot.
        await Assertions.Expect(GetInvalidCount()).ToHaveTextAsync("1");
        await Assertions.Expect(GetLastInvalidValue()).ToHaveTextAsync("a");
        await Assertions.Expect(GetInput(0)).ToHaveValueAsync("");
        await Assertions.Expect(GetCurrentValue()).ToHaveTextAsync("");
        await Assertions.Expect(GetChangeCount()).ToHaveTextAsync("0");
        await Assertions.Expect(GetInput(0)).ToBeFocusedAsync();

        var slotInputEvents = await Page.EvaluateAsync<int>("() => window.__otpSlotInputEvents");
        Assert.Equal(0, slotInputEvents);

        // A valid digit still types normally afterwards.
        await Page.Keyboard.PressAsync("5");
        await Assertions.Expect(GetInput(0)).ToHaveValueAsync("5");
        await Assertions.Expect(GetCurrentValue()).ToHaveTextAsync("5");
        await Assertions.Expect(GetChangeCount()).ToHaveTextAsync("1");
    }

    [Fact]
    public virtual async Task PasteDistributesCharactersAndMovesFocus()
    {
        await NavigateAsync(CreateUrl("/tests/otpfield")
            .WithOtpLength(6)
            .Build());
        await WaitForOtpFieldAsync();

        await GetInput(0).FocusAsync();
        await Page.EvaluateAsync(
            """
            () => {
                const input = document.querySelector('[data-testid="otp-input-0"]');
                const data = new DataTransfer();
                data.setData('text/plain', '98 7a654');
                input.dispatchEvent(new ClipboardEvent('paste', {
                    clipboardData: data,
                    bubbles: true,
                    cancelable: true
                }));
            }
            """);

        await Assertions.Expect(GetCurrentValue()).ToHaveTextAsync("987654");
        await Assertions.Expect(GetInvalidCount()).ToHaveTextAsync("1");
        await Assertions.Expect(GetCompleteCount()).ToHaveTextAsync("1");
        await Assertions.Expect(GetLastReason()).ToHaveTextAsync("InputPaste");
        await Assertions.Expect(GetLastCompleteReason()).ToHaveTextAsync("InputPaste");
        await Assertions.Expect(GetInput(5)).ToBeFocusedAsync();
    }

    [Fact]
    public virtual async Task ControlledParameterChangeSynchronizesVisibleSlots()
    {
        await NavigateAsync(CreateUrl("/tests/otpfield")
            .WithOtpValue("12")
            .WithOtpLength(4)
            .Build());
        await WaitForOtpFieldAsync();

        await GetByTestId("set-value-2468").ClickAsync();

        await Assertions.Expect(GetCurrentValue()).ToHaveTextAsync("2468");
        await Assertions.Expect(GetInput(0)).ToHaveValueAsync("2");
        await Assertions.Expect(GetInput(1)).ToHaveValueAsync("4");
        await Assertions.Expect(GetInput(2)).ToHaveValueAsync("6");
        await Assertions.Expect(GetInput(3)).ToHaveValueAsync("8");
        await Assertions.Expect(GetHiddenInput()).ToHaveValueAsync("2468");
    }

    [Fact]
    public virtual async Task ValidationTypeNoneSynchronizesVisibleSlotsByCodePoint()
    {
        await NavigateAsync(CreateUrl("/tests/otpfield")
            .WithOtpValue("😀A")
            .WithOtpLength(2)
            .WithOtpValidationType("none")
            .Build());
        await WaitForOtpFieldAsync();

        await Assertions.Expect(GetCurrentValue()).ToHaveTextAsync("😀A");
        await Assertions.Expect(GetInput(0)).ToHaveValueAsync("😀");
        await Assertions.Expect(GetInput(1)).ToHaveValueAsync("A");
        await Assertions.Expect(GetHiddenInput()).ToHaveValueAsync("😀A");
    }

    [Fact]
    public virtual async Task KeyboardNavigationAndDeletionUseBrowserInterop()
    {
        await NavigateAsync(CreateUrl("/tests/otpfield")
            .WithOtpDefaultValue("1234")
            .WithOtpLength(4)
            .Build());
        await WaitForOtpFieldAsync();

        await GetInput(2).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Assertions.Expect(GetInput(1)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Backspace");
        await Assertions.Expect(GetCurrentValue()).ToHaveTextAsync("134");
        await Assertions.Expect(GetInput(0)).ToBeFocusedAsync();
        await Assertions.Expect(GetLastReason()).ToHaveTextAsync("Keyboard");
    }

    [Fact]
    public virtual async Task DisabledAndReadOnlyExposeNativeAndDataAttributes()
    {
        await NavigateAsync(CreateUrl("/tests/otpfield")
            .WithOtpDefaultValue("12")
            .WithOtpLength(4)
            .WithOtpDisabled(true)
            .WithOtpReadOnly(true)
            .WithOtpRequired(true)
            .WithOtpMask(true)
            .Build());
        await WaitForOtpFieldAsync();

        await Assertions.Expect(GetRoot()).ToHaveAttributeAsync("data-disabled", "");
        await Assertions.Expect(GetRoot()).ToHaveAttributeAsync("data-readonly", "");
        await Assertions.Expect(GetRoot()).ToHaveAttributeAsync("data-required", "");
        await Assertions.Expect(GetInput(0)).ToHaveAttributeAsync("disabled", "");
        await Assertions.Expect(GetInput(0)).ToHaveAttributeAsync("readonly", "");
        await Assertions.Expect(GetInput(0)).ToHaveAttributeAsync("required", "");
        await Assertions.Expect(GetInput(0)).ToHaveAttributeAsync("type", "password");
        await Assertions.Expect(GetCurrentValue()).ToHaveTextAsync("12");
    }

    [Fact]
    public virtual async Task ReadOnlyDoesNotAdvanceOnSameCharacterKey()
    {
        await NavigateAsync(CreateUrl("/tests/otpfield")
            .WithOtpDefaultValue("12")
            .WithOtpLength(4)
            .WithOtpReadOnly(true)
            .Build());
        await WaitForOtpFieldAsync();

        await GetInput(0).FocusAsync();
        await Page.Keyboard.PressAsync("1");

        await Assertions.Expect(GetInput(0)).ToBeFocusedAsync();
        await Assertions.Expect(GetCurrentValue()).ToHaveTextAsync("12");
        await Assertions.Expect(GetChangeCount()).ToHaveTextAsync("0");
    }

    [Fact]
    public virtual async Task HiddenInputAutofillDispatchesSingleChange()
    {
        await NavigateAsync(CreateUrl("/tests/otpfield")
            .WithOtpLength(4)
            .WithOtpName("otp")
            .Build());
        await WaitForOtpFieldAsync();

        await Page.EvaluateAsync(
            """
            () => {
                const input = document.querySelector('[data-blazix-otp-hidden-input]');
                input.value = '9876';
                input.dispatchEvent(new Event('input', { bubbles: true, cancelable: true }));
            }
            """);

        await Assertions.Expect(GetCurrentValue()).ToHaveTextAsync("9876");
        await Assertions.Expect(GetChangeCount()).ToHaveTextAsync("1");
        await Assertions.Expect(GetCompleteCount()).ToHaveTextAsync("1");
        await Assertions.Expect(GetInput(3)).ToBeFocusedAsync();
    }

    [Fact]
    public virtual async Task AutoSubmitSubmitsOwningFormOnCompletion()
    {
        await NavigateAsync(CreateUrl("/tests/otpfield")
            .WithOtpLength(4)
            .WithOtpName("otp")
            .WithOtpShowForm(true)
            .WithOtpAutoSubmit(true)
            .Build());
        await WaitForOtpFieldAsync();

        await GetInput(0).FillAsync("1234");

        await Assertions.Expect(GetCurrentValue()).ToHaveTextAsync("1234");
        await Assertions.Expect(GetCompleteCount()).ToHaveTextAsync("1");
        var timeout = new LocatorAssertionsToHaveTextOptions { Timeout = 5000 * TimeoutMultiplier };
        await Assertions.Expect(GetFormSubmitted()).ToHaveTextAsync("true", timeout);
        await Assertions.Expect(GetFormData()).ToHaveTextAsync("otp=1234", timeout);
    }
}
