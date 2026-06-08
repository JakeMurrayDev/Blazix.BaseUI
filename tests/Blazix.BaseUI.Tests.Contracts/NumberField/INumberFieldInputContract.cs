namespace Blazix.BaseUI.Tests.Contracts.NumberField;

public interface INumberFieldInputContract
{
    // Rendering
    Task RendersAsInputByDefault();
    Task HasTextboxRole();
    Task RendersWithCustomRender();
    Task RendersChildContent();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();

    // Element reference
    Task ExposesElementReference();

    // Character filtering
    Task DoesNotAllowNonNumericCharactersOnChange();
    Task AllowsNumericCharactersOnChange();

    // Keyboard
    Task IncrementsOnKeyDownArrowUp();
    Task DecrementsOnKeyDownArrowDown();
    Task IncrementsToMinOnKeyDownHome();
    Task DecrementsToMaxOnKeyDownEnd();

    // Blur formatting
    Task CommitsFormattedValueOnlyOnBlur();
    Task PreservesRawInputUntilBlur();
    Task CommitsValidatedNumberOnBlur_Min();
    Task CommitsValidatedNumberOnBlur_Max();
    Task DoesNotSnapToStepOnBlur();
    Task CommitsValidatedNumberOnBlur_StepAndMin();

    // Precision preservation
    Task PreservesFullPrecisionOnFirstBlurAfterExternalChange();
    Task UpdatesInputValueAfterIncrementFollowedByExternalChange();
    Task UpdatesInputValueAfterDecrementFollowedByExternalChange();
    Task AllowsTypingAfterPrecisionPreservedOnBlur();
    Task FormatsToCanonicalWhenInputDiffersFromMaxPrecision();
    Task HandlesMultipleBlurCyclesWithPrecisionPreservation();
    Task HandlesEdgeCaseParsedValueEqualsCurrentButInputDiffers();
    Task PreservesPrecisionWhenValueMatchesMaxAfterExternalChange();
    Task RoundsToExplicitMaximumFractionDigitsOnBlur();
    Task RoundsToStepPrecisionOnBlurWhenStepImpliesPrecision();
    Task CommitsParsedValueOnBlurAndNormalizesDisplay();

    // ARIA
    Task HasAriaRoledescriptionNumberField();
    Task HasAriaValueNow();
    Task HasAriaValueMin();
    Task HasAriaValueMax();

    // Data attributes
    Task HasDataDisabledWhenDisabled();
    Task HasDataReadOnlyWhenReadOnly();
    Task HasDataRequiredWhenRequired();

    // Input attributes
    Task HasInputModeAttribute();
    Task HasAutocompleteOff();
    Task HasCorrectValueAttribute();
}
