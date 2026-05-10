namespace BlazorBaseUI.Tests.Contracts.Slider;

public interface ISliderIndicatorContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task HasPositioningStyleForSingleValue();
    Task HasPositioningStyleForRangeValue();
    Task HasDataOrientation();
    Task HasDataDisabledWhenDisabled();

    // Vertical orientation tests
    Task HasVerticalPositioningStyleForSingleValue();
    Task HasVerticalPositioningStyleForRangeValue();

    // Inset/edge positioning tests
    Task HasInsetPositioningStyleForSingleValue();
    Task HasInsetPositioningStyleForRangeValue();
    Task HasVerticalInsetPositioningStyleForSingleValue();
    Task HasVerticalInsetPositioningStyleForRangeValue();

    // Data attribute coverage
    Task HasDataDragging();
    Task HasDataValid();
    Task HasDataInvalid();
    Task HasDataTouched();
    Task HasDataDirty();
    Task HasDataFocused();
}
