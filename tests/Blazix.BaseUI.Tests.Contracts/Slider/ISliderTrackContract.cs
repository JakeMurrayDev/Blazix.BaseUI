namespace Blazix.BaseUI.Tests.Contracts.Slider;

public interface ISliderTrackContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task HasPositionRelativeStyle();
    Task HasDataOrientation();
    Task HasDataDisabledWhenDisabled();
    Task HasDataDraggingWhenDragging();
    Task HasDataValidWhenValid();
    Task HasDataInvalidWhenInvalid();
    Task DoesNotHaveDataValidOrInvalidWhenValidIsNull();
    Task HasDataTouchedWhenTouched();
    Task HasDataDirtyWhenDirty();
    Task HasDataFocusedWhenFocused();
}
