namespace Blazix.BaseUI.Tests.Contracts.Meter;

public interface IMeterTrackContract
{
    // Rendering
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task RendersOutsideRoot();
}
