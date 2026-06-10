namespace Blazix.BaseUI.Tests.Contracts.Slider;

public interface ISliderThumbContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task ContainsInputTypeRange();
    Task HasTabindexMinusOneOnThumb();
    Task InputHasAriaValuenow();
    Task InputHasAriaOrientation();
    Task InputHasMinMaxStep();
    Task InputHasDisabledAttribute();
    Task GetAriaLabelCallback_SetsAriaLabelOnInput();
    Task GetAriaValueTextCallback_SetsAriaValueTextOnInput();
    Task AdditionalAttributes_AppliedToThumbElement();
    Task HasDataIndexAttribute();
    Task HasDataOrientation();
    Task HasDataDisabledWhenDisabled();
    Task HasPositioningStyle();
    Task InvokesOnFocus();
    Task InvokesOnBlur();
    Task HasAriaValueTextForRangeSlider();

    // Non-integer value handling
    Task HandlesNonIntegerValues();
    Task InputHasCorrectValueAttribute();

    // Vertical orientation positioning
    Task HasVerticalPositioningStyle();

    // Three or more thumbs
    Task SupportsThreeOrMoreThumbs();


    // Input name attribute
    Task InputHasNameAttribute();

    // State in ClassValue
    Task ClassValueReceivesThumbState();

    // Z-index management
    Task RangeThumb_HasZIndex2WhenActive();
    Task RangeThumb_HasZIndex1WhenLastUsed();
    Task SingleThumb_HasNoZIndexByDefault();
}
