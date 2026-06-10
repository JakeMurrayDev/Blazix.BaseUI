namespace Blazix.BaseUI.Tests.Contracts.Slider;

public interface ISliderRootContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();
    Task HasRoleGroup();
    Task HasDataOrientationHorizontalByDefault();
    Task HasDataOrientationVertical();
    Task HasDataDisabledWhenDisabled();
    Task CascadesContextToChildren();
    Task UncontrolledModeUsesDefaultValue();
    Task UncontrolledModeUsesDefaultValues();
    Task ControlledModeRespectsValueParameter();
    Task ControlledModeRespectsValuesParameter();
    Task InvokesOnValueChange();
    Task InvokesOnValueCommitted();
    Task InvokesOnValuesChange();
    Task InvokesOnValuesCommitted();
    Task ClampsValueToMinMax();

    // Step precision tests
    Task SupportsNonIntegerStep();
    Task HandlesVerySmallStepValues();

    // Min/Max tests
    Task UsesMinAsStepOrigin();
    Task ClampsValueBelowMin();

    // Multi-thumb tests
    Task SupportsThreeOrMoreThumbs();
    Task ThumbsHaveCorrectIndices();

    // State tests
    Task ClassValueReceivesCorrectState();
    Task StyleValueReceivesCorrectState();

    // Name attribute for form integration
    Task SetsNameAttribute();

    // ReadOnly behavior
    Task ReadOnlyDoesNotPreventValueDisplay();

    // LargeStep configuration
    Task UsesLargeStepValue();
}
