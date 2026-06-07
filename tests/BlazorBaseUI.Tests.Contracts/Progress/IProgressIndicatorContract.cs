namespace BlazorBaseUI.Tests.Contracts.Progress;

public interface IProgressIndicatorContract
{
    // Rendering
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();

    // Indicator styles
    Task SetsIndicatorStyleForDeterminateValue();
    Task SetsZeroWidthWhenValueIsZero();
    Task NoIndicatorStyleForIndeterminateValue();
    Task CombinesUserStyleWithIndicatorStyle();
    Task AdditionalStyleOverridesIndicatorStyle();
    Task StyleValueOverridesIndicatorStyle();
    Task UsesSourceValueToPercentWhenMinEqualsMax();

    // Data attributes
    Task HasDataStatusAttribute();

    // Context
    Task ThrowsWhenRenderedWithoutRoot();
}
