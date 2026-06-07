namespace BlazorBaseUI.Tests.Contracts.Progress;

public interface IProgressRootContract
{
    // Rendering
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();

    // ARIA attributes
    Task HasRoleProgressbar();
    Task SetsAriaValueMin();
    Task SetsAriaValueMax();
    Task SetsAriaValueNow();
    Task SetsAriaValueText();
    Task SetsAriaLabelledByWhenLabelPresent();
    Task UpdatesAriaValueNowWhenValueChanges();
    Task DoesNotSetAriaValueNowWhenIndeterminate();
    Task SetsIndeterminateAriaValueText();
    Task AllowsAriaAttributesToOverrideDefaults();
    Task RendersNvdaPresentationSpan();

    // Data attributes
    Task HasDataProgressingWhenInProgress();
    Task HasDataCompleteWhenComplete();
    Task HasDataIndeterminateWhenNull();

    // Formatting
    Task FormatsValueWithCustomFormat();
    Task FormatsValueWithFormatProvider();
    Task FormatsValueWithNumberFormatOptionsAndLocale();
    Task GetAriaValueTextCallbackOverridesDefault();

    // Context cascading
    Task CascadesContextToChildren();

    // Element reference
    Task ExposesElementReference();
}
