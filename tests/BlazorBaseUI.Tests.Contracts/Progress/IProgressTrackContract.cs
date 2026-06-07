namespace BlazorBaseUI.Tests.Contracts.Progress;

public interface IProgressTrackContract
{
    // Rendering
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();

    // Data attributes
    Task HasDataStatusAttribute();

    // Context
    Task ThrowsWhenRenderedWithoutRoot();
}
