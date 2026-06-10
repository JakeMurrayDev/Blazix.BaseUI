namespace Blazix.BaseUI.Tests.Contracts.Toolbar;

public interface IToolbarInputContract
{
    // Rendering
    Task RendersAsInputByDefault();
    Task RendersWithCustomRenderFragment();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();

    // ARIA / Disabled
    Task HasAriaDisabledWhenDisabledAndFocusable();
    Task HasDisabledAttributeWhenDisabledAndNotFocusable();
    Task DoesNotHaveDisabledWhenNotDisabled();

    // DefaultValue
    Task RendersWithDefaultValue();
    Task DoesNotRenderValueWhenDefaultValueNull();

    // Data attributes
    Task HasDataOrientationFromRoot();
    Task HasDataDisabledWhenDisabled();
    Task DoesNotHaveDataDisabledWhenNotDisabled();
    Task HasDataFocusableByDefault();
    Task DoesNotHaveDataFocusableWhenFocusableWhenDisabledFalse();

    // Disabled cascading
    Task InheritsDisabledFromRoot();
    Task InheritsDisabledFromGroup();

    // Validation
    Task ThrowsWhenNotInsideToolbarRoot();
}
