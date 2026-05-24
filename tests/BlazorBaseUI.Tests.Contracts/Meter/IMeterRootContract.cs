namespace BlazorBaseUI.Tests.Contracts.Meter;

public interface IMeterRootContract
{
    // Rendering
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();
    Task RendersHiddenPresentationSpanForScreenReaders();

    // ARIA attributes
    Task HasRoleMeter();
    Task SetsAriaValueMin();
    Task SetsAriaValueMax();
    Task SetsAriaValueNow();
    Task SetsAriaValueText();
    Task UsesInvariantRawValueForDefaultAriaValueText();
    Task UsesJavaScriptNumberStringForDefaultAriaValueText();
    Task AdditionalAriaValueTextOverridesComputedValue();
    Task SetsAriaLabelledByWhenLabelPresent();
    Task UpdatesAriaValueNowWhenValueChanges();

    // Formatting
    Task FormatsValueWithFormatString();
    Task FormatsValueWithFormatProvider();
    Task FormatsValueWithNumberFormatOptionsAndLocale();
    Task FormatsValueWithSignificantDigitOptions();
    Task FormatsValueWithMinimumIntegerAndSignificantDigitOptions();
    Task GetAriaValueTextCallbackOverridesDefault();
    Task AriaValueTextUsesFormattedValueWhenFormatProvided();

    // Context cascading
    Task CascadesContextToChildren();

    // Element reference
    Task ExposesElementReference();
}
