namespace Blazix.BaseUI.Tests.Contracts.Slider;

/// <summary>
/// Defines the expected test coverage for the <see cref="Blazix.BaseUI.Slider.SliderLabel"/> component.
/// </summary>
public interface ISliderLabelContract
{
    // Rendering
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();

    // Attribute forwarding
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();

    // ID behavior
    Task HasGeneratedLabelId();
    Task OverridesUserProvidedId();

    // Data attributes
    Task HasDataOrientation();
    Task HasDataDisabledWhenDisabled();
    Task HasDataDragging();

    // Accessibility integration
    Task RegistersLabelIdToRoot();
}
