namespace BlazorBaseUI.Tests.Contracts.NumberField;

public interface INumberFieldRootContract
{
    // Rendering
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task RendersChildContent();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();

    // Element reference
    Task ExposesElementReference();

    // defaultValue
    Task DefaultValue_AcceptsNumberValue();
    Task DefaultValue_AcceptsNullValue();

    // value (controlled)
    Task Value_AcceptsNumberThatChangesOverTime();
    Task Value_AcceptsNullValue();
    Task Value_IsNullWhenInputEmptyButNotTrimmed();

    // onValueChange
    Task OnValueChange_CalledWhenValueChanges();
    Task OnValueChange_CalledWithNumberTransitioningFromNull();
    Task OnValueChange_CalledWithNullTransitioningFromNumber();
    Task OnValueChange_IncludesReasonForParseableTyping();
    Task OnValueChange_IncludesReasonWhenClearingValue();
    Task OnValueChange_IncludesReasonForKeyboardIncrements();
    Task OnValueChange_IncludesReasonForIncrementButtonPresses();
    Task OnValueChange_IncludesReasonForDecrementButtonPresses();

    // Typing behavior
    Task Typing_FiresOnValueChangeForEachParseableChange();
    Task Typing_DoesNotFireForNonNumericPartialInput();
    Task Typing_HandlesSignAndDecimalPartials();
    Task Typing_AcceptsGroupingAndParsesProgressively();
    Task Typing_RespectsLocaleDecimalSeparator();
    Task Typing_ParsesPercentAndCommitsCanonicalValue();
    Task Typing_AcceptsCurrencySymbol();
    Task Typing_AllowsDeletingTrailingCurrencySymbols();

    // onValueCommitted
    Task OnValueCommitted_FiresOnBlurAfterManualInput();
    Task OnValueCommitted_FiresNullOnBlurWhenCleared();
    Task OnValueCommitted_FiresOnKeyboardInteractions();
    Task OnValueCommitted_FiresOnIncrementDecrementButtons();

    // Props
    Task Disabled_DisablesInput();
    Task ReadOnly_MarksInputAsReadOnly();
    Task Required_MarksInputAsRequired();
    Task Name_SetsNameOnHiddenInput();
    Task Form_SetsFormOnHiddenInput();

    // Min
    Task Min_PreventsValueBelowMin();
    Task Min_AllowsValueAboveMin();

    // Max
    Task Max_PreventsValueAboveMax();
    Task Max_AllowsValueBelowMax();

    // allowOutOfRange
    Task AllowOutOfRange_AllowsOverflowForDirectInput();
    Task AllowOutOfRange_ClampsStepInteractions();
    Task AllowOutOfRange_ClampsDirectInputWhenFalse();

    // Step
    Task Step_DefaultsToOne();
    Task Step_IncrementsByStepProp();
    Task Step_SnapsOnIncrementToNearestMultiple();
    Task Step_DecrementsByStepProp();
    Task Step_SnapsOnDecrementToNearestMultiple();

    // Step - fractional and floating point (validate.test.ts)
    Task Step_FractionalIncrementHandlesFloatingPoint();
    Task Step_FractionalDecrementHandlesFloatingPoint();
    Task Step_FractionalIncrementWithSmallStep();
    Task Step_FractionalStepWithMinimum();
    Task Step_SnapWithLargerStepIncrement();
    Task Step_SnapWithLargerStepDecrement();
    Task Step_RemovesFloatingPointErrors();

    // Format
    Task Format_FormatsValueUsingProvidedOptions();
    Task Format_ReflectsControlledValueChanges();

    // Field integration
    Task Field_DataTouchedOnBlur();
    Task Field_DataDirtyOnChange();
    Task Field_DataFilledAddsAndRemovesOnChange();
    Task Field_DataFilledWhenAlreadyFilled();

    // InputMode
    Task InputMode_SetsToNumericByDefault();
    Task InputMode_SetsToDecimalWhenMinIsZeroOrAbove();

    // Exotic inputs
    Task ExoticInput_ParsesPersianDigitsAndSeparators();
    Task ExoticInput_ParsesPersianWithArabicSeparators();
    Task ExoticInput_ParsesFullwidthDigitsAndPunctuation();
    Task ExoticInput_ParsesPercentAndPermilleInExoticForms();
    Task ExoticInput_IgnoresPercentWhenNotFormattedAsPercent();
    Task ExoticInput_ParsesTrailingUnicodeMinus();
    Task ExoticInput_TreatsParenthesesNegativesAsInvalid();
    Task ExoticInput_CollapsesExtraDotsFromMixedLocaleInputs();

    // Navigation keys
    Task NavigationKeys_AllowsWithoutPreventingDefault();

    // Locale
    Task Locale_SetsLocaleOfInput();
    Task Locale_UsesDefaultIfNoneProvided();

    // Validation
    Task Validation_ClearsExternalErrorsOnChange();
    Task Validation_ValidatesWithLatestValueOnBlur();

    // Field label and description
    Task Field_LabelForAttribute();
    Task Field_DescriptionAriaDescribedBy();

    // Field disabled integration
    Task Field_DisablesInputWhenFieldDisabledTrue();
    Task Field_DoesNotDisableWhenFieldDisabledFalse();

    // Data attributes
    Task HasDataDisabledWhenDisabled();
    Task HasDataReadOnlyWhenReadOnly();
    Task HasDataRequiredWhenRequired();

    // Hidden input
    Task HiddenInput_HasTypeNumber();
    Task HiddenInput_HasAriaHiddenTrue();
    Task HiddenInput_HasNameAttribute();
    Task HiddenInput_HasMinMaxStepAttributes();
    Task HiddenInput_AllowsStepAny();

    // Parse utility tests (parse.test.ts via component interface)
    Task Parse_HandlesHanNumerals();
    Task Parse_ReturnsNullForEmptyAndWhitespace();
    Task Parse_ReturnsNullForJustASign();
    Task Parse_HandlesLeadingAndTrailingSigns();
    Task Parse_HandlesTrailingAsciiSign();
    Task Parse_HandlesPermilleWhenFormattedAsPercent();
    Task Parse_HandlesDeDeFormattedNumbers();
    Task Parse_ReturnsNullForInfinityLikeInputs();
    Task Parse_CollapsesMultipleConsecutiveDots();

    // Cancellation
    Task OnValueChange_SupportsCancellation();
}
