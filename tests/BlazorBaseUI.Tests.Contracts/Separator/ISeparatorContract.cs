namespace BlazorBaseUI.Tests.Contracts.Separator;

public interface ISeparatorContract
{
    // Rendering
    Task RendersAsDivByDefault();
    Task RendersWithSeparatorRole();
    Task RendersWithCustomRenderFragment();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task RendersChildContent();

    // Orientation
    Task DefaultsToHorizontalOrientation();
    Task SetsVerticalOrientation();

    // Data attributes
    Task SetsDataOrientationHorizontal();
    Task SetsDataOrientationVertical();
}
