namespace Blazix.BaseUI.Tests.Contracts.Slider;

public interface ISliderControlContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task HasTouchActionNoneStyle();
    Task HasTabindexMinusOne();
    Task HasDataOrientation();
    Task HasDataDisabledWhenDisabled();
}
