namespace Blazix.BaseUI.Tests.Contracts.Toolbar;

public interface IToolbarSeparatorContract
{
    // Rendering
    Task RendersAsSeparatorComponent();
    Task RendersWithCustomRenderFragment();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task RendersChildContent();

    // Orientation inversion
    Task InvertsOrientationFromHorizontalToVertical();
    Task InvertsOrientationFromVerticalToHorizontal();

    // Validation
    Task ThrowsWhenNotInsideToolbarRoot();

    // Style
    Task AppliesStyleValue();
}
