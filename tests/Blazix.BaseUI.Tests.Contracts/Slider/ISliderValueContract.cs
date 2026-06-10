namespace Blazix.BaseUI.Tests.Contracts.Slider;

public interface ISliderValueContract
{
    Task RendersAsOutputByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task DisplaysSingleValue();
    Task DisplaysRangeValues();
    Task DisplaysMultipleThumbValues();
    Task HasAriaLiveOff();
    Task HasDataOrientation();
    Task HasDataDisabledWhenDisabled();
    Task UsesChildRenderFragment();

    // Formatting tests
    Task DisplaysFormattedValues();
    Task ChildContentReceivesFormattedAndRawValues();
}
